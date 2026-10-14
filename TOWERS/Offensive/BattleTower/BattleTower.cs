// Placed Battle Tower with staggered reusable targeting and lightweight guided test bullets.
using Godot;

public partial class BattleTower : Node2D
{
    #region Settings and State
    private BattleTowerConfig _config;
    private bool _initialized;
    public TowerFinalStats FinalStats { get; private set; }
    public event System.Action StatsChanged;
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
    public Vector2I GridCell { get; private set; }
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
    // Explicit rebuild is also available for future input changes that do not emit Resource.Changed.
    public void RebuildStats()
    {
        FinalStats = Config == null ? null : TowerStatPipeline.Resolve(Config);
        StatsChanged?.Invoke();
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
    // Initialize a placed tower's logical cell and world position.
    public void SetPlacement(Vector2I cell, Vector2 center)
    {
        GridCell = cell;
        GlobalPosition = center;
    }

    // =========================================================
    // Modulate the existing sprite instead of instantiating selection effects.
    public void SetSelected(bool selected) =>
        _sprite.Modulate = selected ? new Color(1f, 0.9f, 0.68f) : Colors.White;
    #endregion
}
