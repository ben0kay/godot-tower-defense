// Placed Battle Tower with staggered reusable targeting and lightweight guided test bullets.
using Godot;

public partial class BattleTower : TowerBase
{
    #region Settings and State
    private BattleTowerConfig _config;
    private bool _initialized;
    public override Godot.Collections.Array<TowerUpgradeTier> UpgradeTiers => Config?.UpgradeTiers;
    [Export] public BattleTowerConfig Config
    {
        get => _config;
        set
        {
            if (_config == value) return;
            if (_initialized && GodotObject.IsInstanceValid(_config)) _config.Changed -= RebuildStats;
            _config = value;
            if (!_initialized) return;
            if (_config != null) _config.Changed += RebuildStats;
            RebuildStats();
        }
    }
    public override TowerConfig AuthoredConfig => Config;
    public override float? InspectionRange => FinalStats?.RangePixels;
    // =========================================================
    // Battle alone opts into the weapon config and pivot layout.
    public override bool AcceptsConfig(TowerConfig config) => config is BattleTowerConfig;
    // =========================================================
    // Reject malformed Battle scenes before occupancy/payment, without imposing this on passive types.
    public override bool ValidateScene() => GetNodeOrNull<Node2D>("TurretPivot") != null && GetNodeOrNull<Sprite2D>("TurretPivot/Sprite") != null;
    // =========================================================
    // Per-instance initialization preserves the exported Battle Config and its change subscriptions.
    protected override void ApplyConfig(TowerConfig config) => Config = (BattleTowerConfig)config;
    private Sprite2D _sprite;
    private Node2D _turret;
    private EnemyRegistry _registry;
    private Node2D _projectiles;
    private Enemy _target;
    private float _searchTimer, _shotTimer;
    #endregion

    #region Lifecycle
    // =========================================================
    // Cache visuals and world services once; stagger periodic target acquisition.
    public override void _Ready()
    {
        _turret = GetNode<Node2D>("TurretPivot");
        _sprite = _turret.GetNode<Sprite2D>("Sprite");
        SetProcess(false);
        _initialized = true;
        if (Config != null) Config.Changed += RebuildStats;
        RebuildStats();
        _registry = GetNodeOrNull<EnemyRegistry>("../../EnemySystems/Registry");
        _projectiles = GetNodeOrNull<Node2D>("../../Projectiles");
        _searchTimer = (GetInstanceId() % 97) / 97f * (FinalStats?.TargetSearchInterval ?? 0.15f);
        SetPhysicsProcess(_registry != null && _projectiles != null);
    }
    // =========================================================
    // Refresh First periodically to allow overtaking, checking cached range between searches.
    public override void _PhysicsProcess(double delta)
    {
        if (FinalStats == null) return;
        _searchTimer -= (float)delta; _shotTimer = Mathf.Max(0f, _shotTimer - (float)delta);
        if (_searchTimer <= 0f)
        {
            _target = TowerTargeting.Find(_registry, GlobalPosition, Mathf.Max(0f, FinalStats.RangePixels), Config.TargetPriority);
            _searchTimer = Mathf.Max(0.05f, FinalStats.TargetSearchInterval);
        }
        if (!TowerTargeting.Valid(_target, GlobalPosition, Mathf.Max(0f, FinalStats.RangePixels))) return;
        bool aligned = TurretAiming.Track(_turret, _target.GlobalPosition, FinalStats.RotationSpeedDegrees,
            FinalStats.AimToleranceDegrees, Config.RotateWhileTracking, delta);
        if (!aligned || _shotTimer > 0f || FinalStats.ShotsPerSecond <= 0f) return;
        BasicProjectile bullet = new();
        bullet.Initialize(_target, FinalStats.ProjectileSpeed, FinalStats.DamagePerShot);
        _projectiles.AddChild(bullet); bullet.GlobalPosition = GlobalPosition;
        _shotTimer = 1f / FinalStats.ShotsPerSecond;
    }
    #endregion

    #region Stat Resolution
    // =========================================================
    // Both paid application and preview resolve the same source/context with a different tier.
    public override TowerFinalStats ResolveLevel(int level) => TowerStatPipeline.Resolve(Config, level);

    // =========================================================
    // Explicit rebuild is also available for future input changes that do not emit Resource.Changed.
    public void RebuildStats()
    {
        FinalStats = Config == null ? null : ResolveLevel(UpgradeLevel);
        NotifyStatsChanged();
    }

    // =========================================================
    // Release Resource subscriptions when this placed tower leaves the world.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_config)) _config.Changed -= RebuildStats;
        _initialized = false;
    }
    #endregion

    #region Placement and Selection
    // =========================================================
    // Modulate the existing sprite instead of instantiating selection effects.
    public override void SetSelected(bool selected) =>
        _sprite.Modulate = selected ? new Color(1f, 0.9f, 0.68f) : Colors.White;
    #endregion
}
