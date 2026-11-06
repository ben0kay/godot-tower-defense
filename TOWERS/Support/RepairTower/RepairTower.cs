// Aimed friendly hull repair with shared funded power and sustained-beam heat; no separate repair clock.
using Godot;
using System.Collections.Generic;

public partial class RepairTower : TowerBase
{
    public override bool SupportsPowerCapacitor => true;
    #region Configuration and Cached State
    private RepairTowerConfig _config;
    private bool _ready, _repairing;
    [Export] public RepairTowerConfig Config
    {
        get => _config;
        set
        {
            if (_config == value) return;
            if (_ready && GodotObject.IsInstanceValid(_config)) _config.Changed -= RebuildStats;
            _config = value;
            if (!_ready) return;
            if (_config != null) _config.Changed += RebuildStats;
            RebuildStats();
        }
    }
    public override TowerConfig AuthoredConfig => Config;
    public override List<TowerLevel> Levels => Config?.Levels;
    public override float? InspectionRange => FinalStats?.RangePixels;
    public override bool AcceptsConfig(TowerConfig config) => config is RepairTowerConfig;
    protected override void ApplyConfig(TowerConfig config) => Config = (RepairTowerConfig)config;
    public override TowerFinalStats ResolveLevel(int level) => ResolveRankedLevel(level);
    public override bool ValidateScene() => GetNodeOrNull<Sprite2D>("BaseSprite") != null
        && GetNodeOrNull<Node2D>("TurretPivot") != null && GetNodeOrNull<Sprite2D>("TurretPivot/Sprite") != null
        && GetNodeOrNull<Marker2D>("TurretPivot/Muzzle") != null && GetNodeOrNull<BeamRenderer>("BeamRenderer") != null;
    public override string DescribeRuntime() => !CanOperate || IsBusy ? "Repair inactive"
        : _repairing ? "Repairing friendly hull" : _target != null ? "Aligning repair beam" : "Seeking damaged towers";
    private TowerRegistry _registry;
    private readonly List<TowerBase> _scratch = new();
    private TowerBase _target;
    private Node2D _pivot;
    private Marker2D _muzzle;
    private BeamRenderer _beam;
    private Sprite2D _baseSprite, _headSprite, _constructionSprite;
    private double _searchRemaining;
    #endregion

    #region Source Lifecycle
    // =========================================================
    // One retained scratch list and staggered acquisition; no new registry, health system or per-query scene scan.
    public override void _Ready()
    {
        _registry = GetNodeOrNull<TowerRegistry>("../../TowerRegistry");
        if (_registry != null) _registry.TreeExiting += RegistryExiting;
        _pivot = GetNode<Node2D>("TurretPivot"); _muzzle = GetNode<Marker2D>("TurretPivot/Muzzle");
        _beam = GetNode<BeamRenderer>("BeamRenderer"); _baseSprite = GetNode<Sprite2D>("BaseSprite");
        _headSprite = GetNode<Sprite2D>("TurretPivot/Sprite"); _constructionSprite = GetNodeOrNull<Sprite2D>("ConstructionSprite");
        WorkStarted += Synchronize; WorkCompleted += Synchronize; Destroyed += Synchronize;
        StatsChanged += Synchronize; SpatialChanged += SourceMoved; ObserveSpatialChanges(true);
        _ready = true; if (Config != null) Config.Changed += RebuildStats;
        SetProcess(false); RebuildStats();
        _searchRemaining = (GetInstanceId() % 97) / 97.0 * (FinalStats?.TargetSearchInterval ?? .3f);
    }
    // =========================================================
    // Config edits resolve once; zero rate/inactive sources sleep and clear only their own lock/presentation.
    private void RebuildStats() { FinalStats = Config == null ? null : ResolveLevel(UpgradeLevel); NotifyStatsChanged(); }
    private void Synchronize()
    {
        if (!_ready) return;
        bool active = CanOperate && !IsBusy && !IsQueuedForDeletion() && FinalStats?.Repair?.HullPerSecond > 0
            && GodotObject.IsInstanceValid(_registry) && _registry.IsInsideTree() && !_registry.IsQueuedForDeletion();
        if (!active || (_target != null && !FriendlyTowerTargeting.Valid(_registry, this, _target, FinalStats.RangePixels))) SetTarget(null);
        SetOperationalPhysicsProcess(active);
    }
    protected override void OnDisableChanged() { if (_ready) Synchronize(); }
    private void SourceMoved(TowerBase tower) { if (ValidateLock()) StopBeam(); }
    private void RegistryExiting() { SetTarget(null); SetOperationalPhysicsProcess(false); }
    public override void SetSelected(bool selected)
    {
        Color tint = selected ? new Color(1, .9f, .68f) : Colors.White;
        if (GodotObject.IsInstanceValid(_baseSprite)) _baseSprite.Modulate = tint;
        if (GodotObject.IsInstanceValid(_headSprite)) _headSprite.Modulate = tint;
        if (GodotObject.IsInstanceValid(_constructionSprite)) _constructionSprite.Modulate = tint;
    }
    #endregion

    #region Repair and Lock Lifetime
    // =========================================================
    // Missing hull and thermal headroom cap funded repair time; searching and turning produce no active heat.
    public override void _PhysicsProcess(double delta)
    {
        if (!CanAttack || IsBusy || !double.IsFinite(delta) || delta <= 0) return;
        if (FinalStats?.Repair is not RepairStats repair || repair.HullPerSecond <= 0) { SetTarget(null); return; }
        // Retain a valid lock until full or invalid; relative health changes must not cause ping-pong.
        bool hasLock = ValidateLock();
        _searchRemaining = System.Math.Max(0, _searchRemaining - delta);
        if (!hasLock && _searchRemaining <= 0)
        {
            SetTarget(FriendlyTowerTargeting.Find(_registry, this, FinalStats.RangePixels, _scratch));
            _searchRemaining = FinalStats.TargetSearchInterval;
        }
        if (!ValidateLock()) return;
        if (!TurretAiming.Track(_pivot, _target.GlobalPosition, FinalStats.RotationSpeedDegrees,
            FinalStats.AimToleranceDegrees, true, delta)) { StopBeam(); return; }
        TowerBase locked = _target;
        double missing = (double)locked.MaxHealth - locked.Health;
        double seconds = System.Math.Min(delta, missing / repair.HullPerSecond);
        if (FinalStats.Heat is HeatStats heat && heat.PerBeamSecond > 0)
            seconds = System.Math.Min(seconds, System.Math.Max(0, (heat.Capacity - CurrentHeat) / heat.PerBeamSecond));
        seconds = FundPoweredWork(seconds);
        if (seconds <= 0) { StopBeam(); return; }
        float before = locked.Health;
        locked.RestoreHealth((float)System.Math.Min(missing, repair.HullPerSecond * seconds));
        // Only delivered repair time heats the emitter. Full targets and aborted work remain free.
        if (GodotObject.IsInstanceValid(locked) && locked.Health > before) RecordAttackHeat(0, seconds);
        CompletePowerAttack();
        // HealthChanged can clear a full lock or remove either participant synchronously.
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion()) return;
        if (!CanOperate || _target != locked || !ValidateLock() || locked.Health <= before) { StopBeam(); return; }
        _beam.SetBeam(_muzzle.GlobalPosition, locked.GlobalPosition, true); SetRepairing(true);
    }
    // =========================================================
    // Cached lock revalidation is O(1); full/work/move/death/exit callbacks never force an extra acquisition.
    private bool ValidateLock()
    {
        if (!_ready || FinalStats == null || !FriendlyTowerTargeting.Valid(_registry, this, _target, FinalStats.RangePixels))
        { SetTarget(null); return false; }
        return true;
    }
    private void SetTarget(TowerBase target)
    {
        if (_target == target) { if (target == null) StopBeam(); return; }
        if (GodotObject.IsInstanceValid(_target))
        {
            _target.HealthChanged -= TargetChanged; _target.WorkStarted -= TargetChanged;
            _target.Destroyed -= TargetChanged; _target.TreeExiting -= TargetExiting; _target.SpatialChanged -= TargetMoved;
        }
        StopBeam(); _target = target;
        if (GodotObject.IsInstanceValid(_target))
        {
            _target.HealthChanged += TargetChanged; _target.WorkStarted += TargetChanged;
            _target.Destroyed += TargetChanged; _target.TreeExiting += TargetExiting; _target.SpatialChanged += TargetMoved;
        }
        NotifyRuntimeChanged();
    }
    private void TargetChanged() => ValidateLock();
    private void TargetMoved(TowerBase target) { if (ValidateLock()) StopBeam(); }
    private void TargetExiting() => SetTarget(null);
    private void StopBeam()
    { if (GodotObject.IsInstanceValid(_beam) && _beam.IsActive) _beam.SetBeam(Vector2.Zero, Vector2.Zero, false); SetRepairing(false); }
    private void SetRepairing(bool active)
    { if (_repairing == active) return; _repairing = active; NotifyRuntimeChanged(); }
    // =========================================================
    // No saved runtime target or presentation references; normal checkpoint construction restores authored capability.
    public override void _ExitTree()
    {
        _ready = false; SetTarget(null); SetOperationalPhysicsProcess(false); _scratch.Clear();
        WorkStarted -= Synchronize; WorkCompleted -= Synchronize; Destroyed -= Synchronize;
        StatsChanged -= Synchronize; SpatialChanged -= SourceMoved;
        if (GodotObject.IsInstanceValid(_config)) _config.Changed -= RebuildStats;
        if (GodotObject.IsInstanceValid(_registry)) _registry.TreeExiting -= RegistryExiting;
        _registry = null; base._ExitTree();
    }
    #endregion
}

