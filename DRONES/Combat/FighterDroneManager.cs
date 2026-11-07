// Station-local fighter ownership, launch/recall and active-only flight; world threat work is shared.
using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public partial class FighterDroneManager : Node
{
    private DroneStation _station;
    private WorldGrid _grid;
    private Node2D _projectiles;
    private FighterDroneTargeting _targeting;
    private Func<Vector2, Vector2> _clamp;
    private readonly FighterDrone[] _slots = new FighterDrone[4];
    private readonly Dictionary<FighterDrone, Action> _exitHandlers = new();
    private bool _shutdown;
    public int Count { get; private set; }
    public int Capacity => Mathf.Clamp(_station.Config.FighterCapacity, 1, 4);
    public FighterDrone At(int slot) => slot >= 0 && slot < _slots.Length ? _slots[slot] : null;
    public override void _Ready()
    { ProcessMode = ProcessModeEnum.Pausable; SetProcess(false); SetPhysicsProcess(false); }
    internal void Bind(DroneStation station, WorldGrid grid)
    {
        _station = station; _grid = grid; _clamp = ClampToMap;
        _projectiles = grid.GetNodeOrNull<Node2D>("../Projectiles");
        _targeting = FighterDroneTargeting.For(grid.GetNodeOrNull<EnemyRegistry>("../EnemySystems/Registry"));
        BindService();
    }
    public int FreeSlot()
    { for (int i = 0; i < Capacity; i++) if (_slots[i] == null) return i; return -1; }

    // =========================================================
    // Atomic-wallet commit calls this synchronously. Roll back a failed scene before any Credits are deducted.
    internal bool TryCreate(out FighterDrone fighter)
    {
        fighter = null;
        int slot = FreeSlot(); if (slot < 0) return false;
        try
        {
            Node node = _station.Config.FighterScene.Instantiate();
            if (node is not FighterDrone body || body.GetNodeOrNull<DroneBody>("Body") == null ||
                body.GetNodeOrNull<Sprite2D>("Body/Sprite") == null) { node.Free(); return false; }
            fighter = body;
            fighter.Initialize(_station.Config.FighterConfig, slot, DockPoint(slot));
            AddChild(fighter); fighter.BindWeapon(_grid, _projectiles);
            Register(fighter); return true;
        }
        catch (Exception error)
        {
            if (GodotObject.IsInstanceValid(fighter)) fighter.Free(); fighter = null;
            GD.PushWarning("Fighter creation failed: " + error.Message); return false;
        }
    }
    private void Register(FighterDrone fighter)
    {
        _slots[fighter.Slot] = fighter; Count++;
        Action exit = () => Removed(fighter);
        _exitHandlers.Add(fighter, exit); fighter.TreeExiting += exit;
        RegisterService(fighter);
    }
    private void Removed(FighterDrone fighter)
    {
        fighter.ClearTarget(_targeting);
        _service[fighter.Slot] = null; _meters[fighter.Slot] = null; _activeBays[fighter.Slot] = false;
        if (_exitHandlers.Remove(fighter, out var exit)) fighter.TreeExiting -= exit;
        if (_slots[fighter.Slot] == fighter) { _slots[fighter.Slot] = null; Count--; }
        if (!_shutdown && GodotObject.IsInstanceValid(_station) && !_station.IsQueuedForDeletion())
        { RefreshProcessing(); _station.NotifyRuntimeChanged(); }
    }

    // =========================================================
    // Markers are owned by the station, not invented enemy/tower registry entries.
    private Vector2 DockPoint(int slot) => _station.GetNode<Marker2D>("DockPoints/Dock" + slot).GlobalPosition;
    private Vector2 ClampToMap(Vector2 point)
    {
        Vector2 a = _grid.CellToWorldCenter(Vector2I.Zero);
        Vector2 b = _grid.CellToWorldCenter(new(_grid.Config.WidthCells - 1, _grid.Config.HeightCells - 1));
        return new Vector2(Mathf.Clamp(point.X, Mathf.Min(a.X, b.X), Mathf.Max(a.X, b.X)),
            Mathf.Clamp(point.Y, Mathf.Min(a.Y, b.Y), Mathf.Max(a.Y, b.Y)));
    }
    private Vector2 LaunchPoint(int slot) => ClampToMap(DockPoint(slot) + Vector2.Up *
        FighterDroneConfig.Safe(_station.Config.FighterConfig.LaunchDistance, 180, 64, 600));
    internal WorldGrid PatrolGrid => _grid;

    internal FighterPatrolRoute PreviewPatrol(FighterDrone fighter, Vector2 a, Vector2 b) =>
        FighterPatrolRoute.Create(a, b, fighter.Config, _grid);

    internal bool SetPatrolMode(FighterDrone fighter, bool patrol, FighterPatrolRoute replacement = null)
    {
        if (_shutdown || !_station.CanCommand || !GodotObject.IsInstanceValid(fighter) ||
            At(fighter.Slot) != fighter || (patrol && replacement == null && fighter.PatrolRoute == null)) return false;
        fighter.SetPatrolMode(patrol, replacement, _targeting);
        _station.NotifyRuntimeChanged();
        return true;
    }

    internal bool Launch(int slot, bool automatic = false)
    {
        FighterDrone fighter = At(slot);
        if (fighter == null || fighter.State != FighterDroneState.Docked || fighter.Ammo <= 0 || !_station.CanOperate || _station.IsBusy) return false;
        ResetVisit(slot);
        _service[slot].ManualResourceOverride = !automatic &&
            (fighter.Ammo <= fighter.AmmoCapacity * FighterDroneConfig.Safe(fighter.Config.AutoReturnAmmoFraction, .25f, 0, .9f) ||
             fighter.Health <= fighter.MaxHealth * FighterDroneConfig.Safe(fighter.Config.AutoReturnHealthFraction, .35f, 0, .9f));
        fighter.BeginTransit(LaunchPoint(slot), false); RefreshProcessing(); _station.NotifyRuntimeChanged(); return true;
    }
    internal bool Recall(int slot, FighterReturnReason reason = FighterReturnReason.ManualRecall)
    {
        FighterDrone fighter = At(slot); if (fighter == null) return false;
        var job = _service[slot];
        if (fighter.State == FighterDroneState.Docked || fighter.State == FighterDroneState.Returning)
        {
            if (job.Reason == FighterReturnReason.None || job.Reason == reason) return false;
            job.Reason = reason; job.ManualResourceOverride = false;
            WakeService(); _station.NotifyRuntimeChanged(); return true;
        }
        ResetVisit(slot); _service[slot].Reason = reason;
        fighter.ClearTarget(_targeting); fighter.BeginTransit(DockPoint(slot), true);
        RefreshProcessing(); _station.NotifyRuntimeChanged(); return true;
    }
    internal void RecallAll()
    { for (int i = 0; i < _slots.Length; i++) Recall(i, FighterReturnReason.StationDisabled); }
    private void RefreshProcessing()
    {
        bool flying = false;
        for (int i = 0; i < _slots.Length; i++) flying |= _slots[i]?.IsFlying == true;
        SetPhysicsProcess((flying || HasServiceWork()) && !_shutdown);
    }

    // =========================================================
    // Deployed fighters steer each physics step; one shared budget ranks threats, docked fighters remain dormant.
    public override void _PhysicsProcess(double delta)
    {
        if (_shutdown || GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        if (!GodotObject.IsInstanceValid(_station) || _station.IsDestroyed || _station.IsQueuedForDeletion()) { SetPhysicsProcess(false); return; }
        float dt = (float)Math.Min(delta, .1);
        bool changed = false;
        for (int i = 0; i < _slots.Length; i++)
        {
            FighterDrone fighter = _slots[i]; if (fighter == null) continue;
            if (!_station.CanOperate && fighter.IsFlying) Recall(i, FighterReturnReason.StationDisabled);
            else if (fighter.IsFlying && fighter.State != FighterDroneState.Returning && WantsAutoReturn(fighter))
                Recall(i, FighterReturnReason.AutoService);
            if (fighter.IsFlying)
            {
                bool arrived = fighter.Step(dt); changed |= arrived;
                if (fighter.NeedsTargeting) _targeting?.Tick(dt);
                if (arrived && fighter.State == FighterDroneState.Docked) BeginDockService(fighter);
                changed |= fighter.IsPatrolling
                    ? fighter.StepPatrol(dt, _targeting?.HasLivingEnemies == true, _clamp)
                    : fighter.StepAutoPilot(dt, _targeting, LaunchPoint(i), _clamp);
            }
        }
        changed |= ServiceStep(dt);
        RefreshProcessing(); if (changed) { RefreshMeters(); _station.NotifyRuntimeChanged(); }
    }

    // =========================================================
    // A tower snapshot owns its roster, so loading/selling can never leave a second orphaned copy in the world.
    internal FighterStationSaveData Capture()
    {
        var result = new FighterStationSaveData { ServiceElapsed = _serviceElapsed, ServiceSerial = _serviceSerial };
        foreach (var fighter in _slots) if (fighter != null) result.Fighters.Add(fighter.Capture() with { Service = _service[fighter.Slot] with { } });
        return result;
    }
    internal void Restore(FighterStationSaveData data)
    {
        if (Count != 0) throw new InvalidDataException("Fighter restore requires an empty station.");
        if (data == null) return; // Legacy stations contain no fighters; never mint free replacements.
        ValidateSnapshot(data);
        _serviceElapsed = data.ServiceElapsed; _serviceSerial = data.ServiceSerial;
        foreach (var saved in data.Fighters)
        {
            if (saved.Slot >= Capacity || _station.IsBusy) throw new InvalidDataException("Saved fighter station is not ready or lacks its slot.");
            Vector2 position = saved.Position.Vector();
            if (ClampToMap(position).DistanceSquaredTo(position) > .01f) throw new InvalidDataException("Saved fighter is outside the map.");
            Vector2 expected = saved.State is FighterDroneState.Docked or FighterDroneState.Returning ? DockPoint(saved.Slot) : LaunchPoint(saved.Slot);
            if (saved.State == FighterDroneState.Docked)
            { if (position.DistanceSquaredTo(expected) > .01f) throw new InvalidDataException("Saved fighter dock/hold position changed."); }
            else if (saved.Motion != null && saved.Motion.End.Vector().DistanceSquaredTo(expected) > .01f)
                throw new InvalidDataException("Saved fighter endpoint changed.");
            var fighter = _station.Config.FighterScene.Instantiate<FighterDrone>();
            fighter.Initialize(_station.Config.FighterConfig, saved.Slot, position, saved.Id, purchased: false);
            AddChild(fighter); fighter.BindWeapon(_grid, _projectiles);
            Register(fighter); fighter.Restore(saved);
            _service[saved.Slot] = saved.Service is null ? new() : saved.Service with { };
            ValidateServiceCapacity(fighter, _service[saved.Slot]);
            if (!_station.CanOperate && _service[saved.Slot].Reason == FighterReturnReason.AutoService)
                throw new InvalidDataException("Disabled station cannot retain an automatic relaunch order.");
        }
        WakeService(); _station.NotifyRuntimeChanged();
    }
    internal static void ValidateSnapshot(FighterStationSaveData data)
    {
        if (data?.Fighters == null || data.Fighters.Count > 4) throw new InvalidDataException("Invalid fighter roster.");
        if (!double.IsFinite(data.ServiceElapsed) || data.ServiceElapsed < 0 || data.ServiceElapsed >= .1 ||
            data.ServiceSerial < 0 || data.ServiceSerial > 1000000000000) throw new InvalidDataException("Invalid station service clock.");
        var ids = new HashSet<string>(); var slots = new HashSet<int>(); var serviceOrders = new HashSet<long>();
        foreach (var f in data.Fighters)
        {
            if (f == null || string.IsNullOrEmpty(f.Id)) throw new InvalidDataException("Missing fighter identity.");
            CampaignStore.ValidateId(f.Id);
            if (!ids.Add(f.Id) || !slots.Add(f.Slot) || f.Slot < 0 || f.Slot >= 4 || !Enum.IsDefined(f.State) ||
                !float.IsFinite(f.Health) || f.Health <= 0 || f.Health > 10000 || f.Ammo < 0 || f.Ammo > 10000 ||
                !float.IsFinite(f.Heading) || Mathf.Abs(f.Heading) > 1e9f ||
                !float.IsFinite(f.Position.X) || !float.IsFinite(f.Position.Y) ||
                Mathf.Abs(f.Position.X) > 1e9f || Mathf.Abs(f.Position.Y) > 1e9f)
                throw new InvalidDataException("Invalid saved fighter state.");
            if (!float.IsFinite(f.Velocity.X) || !float.IsFinite(f.Velocity.Y) || f.Velocity.Vector().LengthSquared() > 4000000 ||
                !float.IsFinite(f.ManoeuvrePhase) || f.ManoeuvrePhase < 0 || f.ManoeuvrePhase > Mathf.Tau * 3 + .01f ||
                !double.IsFinite(f.WeaponCooldown) || f.WeaponCooldown < 0 || f.WeaponCooldown > 60)
                throw new InvalidDataException("Invalid fighter steering state.");
            ValidateServiceSnapshot(f, data.ServiceSerial);
            if (f.Service?.VisitStarted == true && !serviceOrders.Add(f.Service.QueueOrder)) throw new InvalidDataException("Duplicate service queue order.");
            bool flying = f.State is FighterDroneState.Launching or FighterDroneState.Returning;
            if (flying != (f.Motion != null)) throw new InvalidDataException("Invalid fighter flight phase.");
            f.Motion?.ValidateFlight();
        }
    }
    public override void _ExitTree()
    {
        _shutdown = true; SetPhysicsProcess(false); UnbindService();
        foreach (var pair in _exitHandlers) if (GodotObject.IsInstanceValid(pair.Key)) pair.Key.TreeExiting -= pair.Value;
        foreach (var fighter in _slots) if (GodotObject.IsInstanceValid(fighter)) fighter.ClearTarget(_targeting);
        _exitHandlers.Clear(); Array.Clear(_slots); Count = 0;
    }
}
