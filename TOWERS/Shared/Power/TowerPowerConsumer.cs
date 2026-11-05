// One plain runtime object per opted-in tower; the existing grid owns all power ticking.
using Godot;
using System;

public sealed partial class TowerPowerConsumer : IDisposable
{
    private readonly TowerBase _tower;
    private TowerPowerConfig _config;
    private PowerGridManager _grid;
    private double _lastTime, _chargingSeconds;
    private bool _idleAllowed, _chargeAllowed, _publishing, _disposed;
    private int _priority = 50;
    private double _publishedEnergy = double.NaN, _publishedRefillRate = double.NaN;
    private bool _publishedRecovery;
    private Vector2I _anchor;
    private int _deliveryRevision = -1, _distance = -1, _publishedDistance = int.MinValue;
    private double _efficiency, _publishedEfficiency = double.NaN, _publishedCeiling = double.NaN;
    public int ConductiveDistanceCells { get { RefreshDelivery(); return _distance; } }
    public double DeliveryEfficiency { get { RefreshDelivery(); return _efficiency; } }
    public double RechargeCeilingPerSecond { get { RefreshDelivery(); return Capacitor.Settings.Recharge * _efficiency; } }
    public PowerCapacitor Capacitor { get; }
    public TowerDisableHandle NoPowerHandle { get; private set; }
    public int Region { get; private set; }
    internal double DesiredRefill { get; private set; }
    internal int CachedConductiveDistance => _distance;
    internal string AllocationSaveId => _tower.SaveId;
    // Latest completed service interval, distinct from the theoretical PW4-B recharge ceiling.
    public double LastDeliveredRefillPerSecond { get; private set; }
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
    // Durable compatibility metadata only; PW4-C allocates by distance, never this value.
    public void SetPriority(int value)
    {
        if (value < 0 || value > 100) throw new ArgumentOutOfRangeException(nameof(value));
        if (_priority == value) return; _priority = value; _tower.NotifyRuntimeChanged();
    }
    // =========================================================
    // Only this consumer settles on a shot; no scan/sort of the other towers is performed.
    public bool TryFundAttack(double seconds, TowerAmmunition ammo, int shots = 1)
    {
        Settle();
        if (Capacitor.RecoveryRequired) { Publish(); return false; }
        PowerSaveData before = Capacitor.Capture(_priority);
        if (!Capacitor.TryPayAttack(seconds)) { Publish(); return false; }
        if (ammo?.TryFire(shots) == false) { Capacitor.Restore(before); return false; }
        RecordEnergySpent(before.Energy - Capacitor.Energy);
        // Publication waits until the projectile exists: disabling clears Battle Tower's target.
        _grid?.WakeCapacitors(); return true;
    }
    // =========================================================
    // Continuous work buys only a deliverable slice; cap by energy before consuming beam ammunition.
    public double FundWorkSeconds(double seconds, TowerAmmunition ammo = null)
    {
        if (!double.IsFinite(seconds) || seconds <= 0) return 0;
        Settle();
        if (Capacitor.RecoveryRequired) { Publish(); return 0; }
        double rate = Capacitor.Settings.Active;
        double funded = rate > 0 ? Math.Min(seconds, Capacitor.Energy / rate) : seconds;
        if (ammo != null) funded = ammo.FundBeamSeconds(funded);
        if (funded <= 0) return 0;
        double before = Capacitor.Energy;
        Capacitor.PayWork(funded);
        RecordEnergySpent(before - Capacitor.Energy);
        // As with shots, defer NoPower callbacks until the already-paid work has been delivered.
        _grid?.WakeCapacitors(); return funded;
    }
    // =========================================================
    // Capture at the shared service boundary; source energy and elapsed idle spend cannot be replayed.
    public PowerSaveData Capture()
    { _grid?.FlushCapacitors(); Settle(); return Capacitor.Capture(_priority); }
    public void Restore(PowerSaveData? data)
    {
        _tower.RemoveDisable(NoPowerHandle); NoPowerHandle = default;
        Capacitor.Restore(data ?? new PowerSaveData(0, 50, false, 0, 0));
        LastDeliveredRefillPerSecond = 0; ResetInspectionSample();
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
        // Work eligibility changes settle the prior interval before disabling/enabling requests.
        // Ordinary hull/disable updates still settle only this consumer, not the entire island.
        if (_chargeAllowed != (Live && !_tower.IsBusy)) _grid?.FlushCapacitors();
        Settle(); ReadState(); Publish(); _grid?.WakeCapacitors();
    }
    private void ConfigChanged()
    {
        _grid?.FlushCapacitors(); Settle(); Capacitor.Configure(_config.Settings()); ReadState(); Publish(); _grid?.WakeCapacitors();
    }
    private void Moved(TowerBase tower) { _grid?.FlushCapacitors(); Remap(); _grid?.WakeCapacitors(); }
    private void Remap()
    {
        _deliveryRevision = -1; _hasInspectionSample = false; _grid?.InvalidateCapacitorOrder();
        if (_grid?.Grid == null) { Region = 0; _distance = -1; _efficiency = 0; return; }
        WorldGrid grid = _grid.Grid;
        Vector2 first = grid.ToLocal(_tower.GlobalPosition) - (Vector2)(_tower.Footprint - Vector2I.One) * (grid.Config.CellSize * .5f);
        _anchor = grid.WorldToCell(grid.ToGlobal(first));
        Region = _grid.RegionForFootprint(_anchor, _tower.Footprint);
    }
    // =========================================================
    // Cached per revision/placement: source batches rebuild the shared field once, not once per consumer/tick.
    internal void RefreshDelivery()
    {
        if (!GodotObject.IsInstanceValid(_grid) || !_grid.IsInsideTree()) { _distance = -1; _efficiency = 0; return; }
        int revision = _grid.DeliveryRevision;
        if (_deliveryRevision == revision) return;
        PowerDelivery delivery = Region > 0 ? _grid.DeliveryForFootprint(_anchor, _tower.Footprint) : new(-1, 0);
        _hasInspectionSample = false;
        _distance = delivery.DistanceCells; _efficiency = delivery.Efficiency; _deliveryRevision = revision;
    }
    // =========================================================
    // Paused game time is owned by the manager; event boundaries retain old-state time exactly once.
    private void Settle()
    {
        double now = _grid?.CapacitorTime ?? _lastTime;
        double elapsed = Math.Max(0, now - _lastTime); _lastTime = now;
        if (_chargeAllowed) _chargingSeconds += elapsed;
        double before = Capacitor.Energy;
        Capacitor.Advance(elapsed, _idleAllowed);
        RecordEnergySpent(before - Capacitor.Energy);
    }
    internal void PrepareRefill()
    {
        RefreshDelivery(); Settle();
        DesiredRefill = Live && _chargeAllowed && Region > 0 && _distance >= 0
            ? Math.Max(0, Math.Min(Capacitor.Settings.Capacity - Capacitor.Energy,
                RechargeCeilingPerSecond * _chargingSeconds)) : 0;
        _chargingSeconds = 0;
    }
    // Return actual credit so unused headroom is immediately available to farther consumers.
    internal double CreditRefill(double amount, double elapsed)
    {
        double before = Capacitor.Energy;
        Capacitor.Credit(Math.Max(0, Math.Min(amount, Capacitor.Settings.Capacity - before)));
        double credited = Math.Max(0, Capacitor.Energy - before);
        LastDeliveredRefillPerSecond = elapsed > 0 ? credited / elapsed : 0;
        CompleteInspectionSample(credited, elapsed);
        return credited;
    }
    internal bool NeedsService => Live && (_energySpent > 0 || LastConsumedPerSecond > 0 || LastDeliveredRefillPerSecond > 0
        || _idleAllowed && Capacitor.Settings.Idle > 0 && Capacitor.Energy > 0
        || _chargeAllowed && RechargeCeilingPerSecond > 0 && Capacitor.Energy < Capacitor.Settings.Capacity
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
            // Do not warm/rebuild a dirty field from every constructor/attack notification during source batches.
            double ceiling = Capacitor.Settings.Recharge * _efficiency;
            PowerDeliveryState state = InspectionState();
            if (_publishedState != state || _publishedConsumed != LastConsumedPerSecond || _publishedEnergy != Capacitor.Energy || _publishedRefillRate != LastDeliveredRefillPerSecond || _publishedRecovery != Capacitor.RecoveryRequired
                || _publishedEfficiency != _efficiency || _publishedCeiling != ceiling || _publishedDistance != _distance)
            {
                _publishedState = state; _publishedConsumed = LastConsumedPerSecond;
                _publishedEnergy = Capacitor.Energy; _publishedRefillRate = LastDeliveredRefillPerSecond; _publishedRecovery = Capacitor.RecoveryRequired;
                _publishedEfficiency = _efficiency; _publishedCeiling = ceiling; _publishedDistance = _distance;
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
        _grid = null; Region = 0; _chargingSeconds = 0; LastDeliveredRefillPerSecond = 0; ResetInspectionSample(); _deliveryRevision = -1; _distance = -1; _efficiency = 0;
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


