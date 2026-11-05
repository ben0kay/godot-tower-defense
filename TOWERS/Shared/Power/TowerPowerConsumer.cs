// One plain runtime object per opted-in tower; the existing grid owns all power ticking.
using Godot;
using System;

public sealed class TowerPowerConsumer : IDisposable
{
    private readonly TowerBase _tower;
    private TowerPowerConfig _config;
    private PowerGridManager _grid;
    private double _lastTime, _chargingSeconds;
    private bool _idleAllowed, _chargeAllowed, _publishing, _disposed;
    private int _priority = 50;
    private double _publishedEnergy = double.NaN;
    private bool _publishedRecovery;
    public PowerCapacitor Capacitor { get; }
    public TowerDisableHandle NoPowerHandle { get; private set; }
    public int Region { get; private set; }
    internal double DesiredRefill { get; private set; }
    public int PowerPriority => _priority;
    // =========================================================
    // Replacing an authored profile preserves this instance's energy instead of recreating/filling it.
    internal void Synchronize(TowerPowerConfig config)
    {
        if (_config != config) { _config.Changed -= ConfigChanged; _config = config; _config.Changed += ConfigChanged; }
        ConfigChanged();
    }
    internal bool Live => !_disposed && GodotObject.IsInstanceValid(_tower) && _tower.IsInsideTree() && !_tower.IsQueuedForDeletion() && !_tower.IsDestroyed;
    // =========================================================
    // A direct F6 scene without world services may use initial charge, but cannot invent a refill source.
    public TowerPowerConsumer(TowerBase tower, TowerPowerConfig config)
    {
        _tower = tower; _config = config; Capacitor = new(config.Settings());
        _grid = tower.GetNodeOrNull<PowerGridManager>("../../PowerNetworks");
        _lastTime = _grid?.CapacitorTime ?? 0;
        tower.WorkStarted += StateChanged; tower.WorkCompleted += StateChanged;
        tower.DisableStateChanged += StateChanged; tower.HealthChanged += StateChanged;
        tower.SpatialChanged += Moved; tower.ObserveSpatialChanges(true); config.Changed += ConfigChanged;
        if (_grid != null) { _grid.NetworksChanged += Remap; Remap(); _grid.RegisterCapacitor(this); }
        ReadState(); Publish();
    }
    // =========================================================
    // Priority is a durable input for PW4-C; PW4-A intentionally does not sort/allocate by priority.
    public void SetPriority(int value)
    {
        if (value < 0 || value > 100) throw new ArgumentOutOfRangeException(nameof(value));
        if (_priority == value) return; _priority = value; _tower.NotifyRuntimeChanged();
    }
    // =========================================================
    // Only this consumer settles on a shot; no scan/sort of the other towers is performed.
    public bool TryFundAttack(double seconds, TowerAmmunition ammo)
    {
        Settle();
        if (Capacitor.RecoveryRequired) { Publish(); return false; }
        PowerSaveData before = Capacitor.Capture(_priority);
        if (!Capacitor.TryPayAttack(seconds)) { Publish(); return false; }
        if (ammo?.TryFire() == false) { Capacitor.Restore(before); return false; }
        // Publication waits until the projectile exists: disabling clears Battle Tower's target.
        _grid?.WakeCapacitors(); return true;
    }
    // =========================================================
    // Capture at the shared service boundary; source energy and elapsed idle spend cannot be replayed.
    public PowerSaveData Capture()
    { _grid?.FlushCapacitors(); Settle(); return Capacitor.Capture(_priority); }
    public void Restore(PowerSaveData? data)
    {
        _tower.RemoveDisable(NoPowerHandle); NoPowerHandle = default;
        Capacitor.Restore(data ?? new PowerSaveData(0, 50, false, 0, 0));
        _priority = data?.Priority ?? 50; _chargingSeconds = 0; _lastTime = _grid?.CapacitorTime ?? 0;
        ReadState(); Publish(); _grid?.WakeCapacitors();
    }
    // =========================================================
    // Cache work/disable eligibility on events, keeping service steps independent of wave state.
    private void ReadState()
    { _chargeAllowed = Live && !_tower.IsBusy; _idleAllowed = _chargeAllowed && _tower.CanOperate; }
    private void StateChanged()
    {
        if (_disposed || _publishing) return;
        Settle(); ReadState(); Publish(); _grid?.WakeCapacitors();
    }
    private void ConfigChanged()
    {
        _grid?.FlushCapacitors(); Settle(); Capacitor.Configure(_config.Settings()); ReadState(); Publish(); _grid?.WakeCapacitors();
    }
    private void Moved(TowerBase tower) { _grid?.FlushCapacitors(); Remap(); _grid?.WakeCapacitors(); }
    private void Remap()
    {
        if (_grid?.Grid == null) { Region = 0; return; }
        WorldGrid grid = _grid.Grid;
        Vector2 first = grid.ToLocal(_tower.GlobalPosition) - (Vector2)(_tower.Footprint - Vector2I.One) * (grid.Config.CellSize * .5f);
        Region = _grid.RegionForFootprint(grid.WorldToCell(grid.ToGlobal(first)), _tower.Footprint);
    }
    // =========================================================
    // Paused game time is owned by the manager; event boundaries retain old-state time exactly once.
    private void Settle()
    {
        double now = _grid?.CapacitorTime ?? _lastTime;
        double elapsed = Math.Max(0, now - _lastTime); _lastTime = now;
        if (_chargeAllowed) _chargingSeconds += elapsed;
        Capacitor.Advance(elapsed, _idleAllowed);
    }
    internal void PrepareRefill()
    {
        Settle();
        DesiredRefill = Live && Region > 0 ? Math.Min(Capacitor.Settings.Capacity - Capacitor.Energy,
            Capacitor.Settings.Recharge * _chargingSeconds) : 0;
        _chargingSeconds = 0;
    }
    internal void CreditRefill(double amount) => Capacitor.Credit(amount);
    internal bool NeedsService => Live && (_idleAllowed && Capacitor.Settings.Idle > 0 && Capacitor.Energy > 0
        || _chargeAllowed && Capacitor.Settings.Recharge > 0 && Capacitor.Energy < Capacitor.Settings.Capacity
        && Region > 0 && _grid.TotalsForRegion(Region).Supply > 0);
    // =========================================================
    // One owned NoPower handle; heat, EMP, upgrades and other NoPower sources remain independent.
    public void Publish()
    {
        if (!Live || _publishing) return;
        _publishing = true;
        try
        {
            if (Capacitor.RecoveryRequired && NoPowerHandle.Token == 0)
                NoPowerHandle = _tower.ApplyDisable(TowerDisableReason.NoPower);
            else if (!Capacitor.RecoveryRequired && NoPowerHandle.Token != 0)
            { TowerDisableHandle old = NoPowerHandle; NoPowerHandle = default; _tower.RemoveDisable(old); }
            ReadState();
            if (_publishedEnergy != Capacitor.Energy || _publishedRecovery != Capacitor.RecoveryRequired)
            {
                _publishedEnergy = Capacitor.Energy; _publishedRecovery = Capacitor.RecoveryRequired;
                _tower.NotifyRuntimeChanged();
            }
        }
        finally { _publishing = false; }
    }
    // =========================================================
    // Either the world service or tower may exit first; neither retains the other after detachment.
    internal void DetachGrid()
    {
        if (_grid != null) _grid.NetworksChanged -= Remap;
        _grid = null; Region = 0; _chargingSeconds = 0;
    }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        _config.Changed -= ConfigChanged; _tower.WorkStarted -= StateChanged; _tower.WorkCompleted -= StateChanged;
        _tower.DisableStateChanged -= StateChanged; _tower.HealthChanged -= StateChanged; _tower.SpatialChanged -= Moved;
        _grid?.UnregisterCapacitor(this); DetachGrid();
        _tower.RemoveDisable(NoPowerHandle); NoPowerHandle = default;
    }
}
