// Sustained Energy attack; targeting/aiming/damage stay independent of its endpoint-only renderer.
using Godot;
public partial class LaserTower : TowerBase
{
    #region Configuration and Cached State
    private LaserTowerConfig _config;
    private bool _ready, _enabled = true;
    [Export] public LaserTowerConfig Config
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
    [Export] public bool Enabled
    {
        get => _enabled;
        set { _enabled = value; if (!value) ClearBeam(); }
    }
    public override TowerConfig AuthoredConfig => Config;
    public override float? InspectionRange => FinalStats?.RangePixels;
    public override System.Collections.Generic.List<TowerLevel> Levels => Config?.Levels;
    private EnemyRegistry _registry;
    private Enemy _target;
    private Node2D _pivot;
    private Sprite2D _sprite;
    private Marker2D _muzzle;
    private BeamRenderer _beam;
    private float _searchTimer;
    #endregion
    #region Generic Tower Contract
    // =========================================================
    // Scene/config compatibility belongs to the type, never generic placement.
    public override bool AcceptsConfig(TowerConfig config) => config is LaserTowerConfig;
    // =========================================================
    // Preflight required presentation references before occupancy/payment.
    public override bool ValidateScene() => GetNodeOrNull<Node2D>("TurretPivot") != null &&
        GetNodeOrNull<Sprite2D>("TurretPivot/Sprite") != null && GetNodeOrNull<BeamRenderer>("BeamRenderer") != null;
    // =========================================================
    // Placement supplies a private canonical config copy before Ready.
    protected override void ApplyConfig(TowerConfig config) => Config = (LaserTowerConfig)config;
    // =========================================================
    // Both purchase and preview use the one shared stat pipeline.
    public override TowerFinalStats ResolveLevel(int level) => Config.ResolveStats(level);
    // =========================================================
    // Numeric input changes rebuild once and immediately shut down zero-DPS attacks.
    public void RebuildStats()
    {
        FinalStats = Config == null ? null : ResolveLevel(UpgradeLevel);
        if (FinalStats?.Beam?.DamagePerSecond > 0 != true) ClearBeam();
        NotifyStatsChanged();
    }
    // =========================================================
    // Selection tint affects artwork without rotating placement/grid identity.
    public override void SetSelected(bool selected) => _sprite.Modulate = selected ? new Color(1, 0.9f, 0.68f) : Colors.White;
    #endregion
    #region Attack Lifecycle
    // =========================================================
    // Cache services/visuals once and spread periodic acquisition across instances.
    public override void _Ready()
    {
        _pivot = GetNode<Node2D>("TurretPivot"); _sprite = GetNode<Sprite2D>("TurretPivot/Sprite");
        _muzzle = GetNodeOrNull<Marker2D>("TurretPivot/Muzzle"); _beam = GetNode<BeamRenderer>("BeamRenderer");
        _registry = GetNodeOrNull<EnemyRegistry>("../../EnemySystems/Registry");
        _ready = true; if (Config != null) Config.Changed += RebuildStats; RebuildStats();
        _searchTimer = (GetInstanceId() % 97) / 97f * (FinalStats?.TargetSearchInterval ?? 0.15f);
        SetProcess(false); SetPhysicsProcess(_registry != null);
    }
    // =========================================================
    // Periodically choose First/Closest; aligned valid lock deals float base DPS times physics delta.
    public override void _PhysicsProcess(double delta)
    {
        if (!Enabled || FinalStats == null || _registry == null || !double.IsFinite(delta) || delta <= 0)
        { ClearBeam(); return; }
        _searchTimer -= (float)delta;
        if (_searchTimer <= 0)
        {
            SetTarget(TowerTargeting.Find(_registry, GlobalPosition, FinalStats.RangePixels, Config.TargetPriority));
            _searchTimer = FinalStats.TargetSearchInterval;
        }
        if (!TowerTargeting.Valid(_target, GlobalPosition, FinalStats.RangePixels) || FinalStats.Beam.Value.DamagePerSecond <= 0)
        { ClearBeam(); return; }
        bool aligned = TurretAiming.Track(_pivot, _target.GlobalPosition, FinalStats.RotationSpeedDegrees,
            FinalStats.AimToleranceDegrees, Config.RotateWhileTracking, delta);
        if (!aligned) { ClearBeam(); return; }
        Enemy locked = _target;
        double amount = FinalStats.Beam.Value.DamagePerSecond * delta;
        locked.ApplyDamage(Config.CreateHit((float)System.Math.Min(float.MaxValue, amount), FinalStats));
        // Removal callbacks may clear/change the lock; never apply this tick to a replacement enemy.
        if (locked != _target || !TowerTargeting.Valid(locked, GlobalPosition, FinalStats.RangePixels))
        { ClearBeam(); return; }
        _beam.SetBeam(_muzzle?.GlobalPosition ?? GlobalPosition, locked.GlobalPosition, true);
    }
    // =========================================================
    // Retarget only at the configured periodic search; preserve a valid lock between searches.
    private void SetTarget(Enemy target)
    {
        if (_target == target) return;
        if (GodotObject.IsInstanceValid(_target)) { _target.Removed -= OnTargetRemoved; _target.TreeExiting -= OnTargetExiting; }
        ClearBeam(); _target = target;
        if (GodotObject.IsInstanceValid(_target)) { _target.Removed += OnTargetRemoved; _target.TreeExiting += OnTargetExiting; }
    }
    // =========================================================
    // Death/arrival hides immediately, before the next tower tick or deferred enemy free.
    private void OnTargetRemoved(Enemy target) { if (_target == target) SetTarget(null); }
    // =========================================================
    // External target unload also hides presentation without waiting for the next attack tick.
    private void OnTargetExiting() => SetTarget(null);
    // =========================================================
    // Beam endpoints are presentation data; no damage state lives in the renderer.
    private void ClearBeam() => _beam?.SetBeam(Vector2.Zero, Vector2.Zero, false);
    // =========================================================
    // Scene unload removes all subscriptions and stale presentation state.
    public override void _ExitTree()
    {
        SetTarget(null);
        if (GodotObject.IsInstanceValid(_config)) _config.Changed -= RebuildStats;
        _ready = false;
    }
    #endregion
}
