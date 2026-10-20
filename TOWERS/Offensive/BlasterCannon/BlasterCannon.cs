// Blaster Cannon reuses shared targeting/aiming; splash delivery owns the instant impact, not this tower.
using Godot;

public partial class BlasterCannon : TowerBase
{
    // =========================================================
    // Generic disabling clears this weapon's live lock/continuous presentation immediately.
    protected override void OnDisableChanged() { if (IsDisabled) { _target = null; _searchTimer = 0; } }
    // =========================================================
    // Keep firing/aiming cadence intact; only invalidate the target search deadline.
    protected override void OnTargetPriorityChanged() => _searchTimer = 0;

    #region Settings and State
    private BlasterCannonConfig _config;
    private bool _initialized;
    public override System.Collections.Generic.List<TowerLevel> Levels => Config?.Levels;
    [Export] public BlasterCannonConfig Config
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
    // Blaster opts into the weapon config and pivot layout.
    public override bool AcceptsConfig(TowerConfig config) => config is BlasterCannonConfig;
    // =========================================================
    // Reject malformed Blaster scenes before occupancy/payment, without imposing this on passive types.
    public override bool ValidateScene() => GetNodeOrNull<Sprite2D>("BaseSprite") != null
        && GetNodeOrNull<TowerRecoilController>("Recoil") != null && GetNodeOrNull<Node2D>("TurretPivot") != null && GetNodeOrNull<Sprite2D>("TurretPivot/RecoilRoot/Sprite") != null
        && GetNodeOrNull<Marker2D>("TurretPivot/RecoilRoot/Muzzles/MuzzleA") != null;
    // =========================================================
    // Per-instance initialization preserves the exported Blaster Config and its change subscriptions.
    protected override void ApplyConfig(TowerConfig config) => Config = (BlasterCannonConfig)config;
    private Sprite2D _sprite, _baseSprite;
    private TowerRecoilController _recoil;
    private Marker2D _muzzle;
    private WeaponVisualRenderer _visuals;
    private Node2D _turret;
    private EnemyRegistry _registry;
    private Node2D _projectiles, _effects;
    private Enemy _target;
    private float _searchTimer, _shotTimer;
    #endregion

    #region Lifecycle
    // =========================================================
    // Cache visuals and world services once; stagger periodic target acquisition.
    public override void _Ready()
    {
        _turret = GetNode<Node2D>("TurretPivot");
        _baseSprite = GetNode<Sprite2D>("BaseSprite");
        _recoil = GetNode<TowerRecoilController>("Recoil");
        _sprite = _turret.GetNode<Sprite2D>("RecoilRoot/Sprite");
        _muzzle = GetNode<Marker2D>("TurretPivot/RecoilRoot/Muzzles/MuzzleA");
        _visuals = WeaponVisuals.Attach(_turret);
        SetProcess(false);
        _initialized = true;
        if (Config != null) Config.Changed += RebuildStats;
        RebuildStats();
        _registry = GetNodeOrNull<EnemyRegistry>("../../EnemySystems/Registry");
        _projectiles = GetNodeOrNull<Node2D>("../../Projectiles");
        _effects = GetNodeOrNull<Node2D>("../../Effects");
        _searchTimer = (GetInstanceId() % 97) / 97f * (FinalStats?.TargetSearchInterval ?? 0.15f);
        SetOperationalPhysicsProcess(_registry != null && _projectiles != null);
    }
    // =========================================================
    // Refresh First periodically to allow overtaking, checking cached range between searches.
    public override void _PhysicsProcess(double delta)
    {
        if (!CanAttack) return;
        if (FinalStats?.Area == null || !double.IsFinite(delta) || delta <= 0 || !IsInsideTree()) return;
        _searchTimer -= (float)delta; _shotTimer = Mathf.Max(0f, _shotTimer - (float)delta);
        if (_searchTimer <= 0f)
        {
            _target = TowerTargeting.Find(_registry, GlobalPosition, Mathf.Max(0f, FinalStats.RangePixels), Config.TargetPriority, detectsStealth: Config.CanDetectStealth);
            _searchTimer = Mathf.Max(0.05f, FinalStats.TargetSearchInterval);
        }
        if (!TowerTargeting.Valid(_target, GlobalPosition, Mathf.Max(0f, FinalStats.RangePixels), Config.CanDetectStealth)) return;
        bool aligned = TurretAiming.Track(_turret, _target.GlobalPosition, FinalStats.RotationSpeedDegrees,
            FinalStats.AimToleranceDegrees, Config.RotateWhileTracking, delta);
        if (!aligned || _shotTimer > 0f || FinalStats.ShotsPerSecond <= 0f) return;
        if (Ammunition?.TryFire() == false) return;
        DamageInfo hit = Config.CreateHit(FinalStats, DamageSource);
        SplashProjectile bullet = new();
        bullet.Initialize(_target, _registry, _target.GlobalPosition, FinalStats.ProjectileSpeed,
            hit, FinalStats.Area.Value, _effects, style: Config.ImpactStyle);
        _projectiles.AddChild(bullet); bullet.GlobalPosition = _muzzle.GlobalPosition;
        _visuals.FlashAt(_muzzle, Config.MuzzleFlash);
        _recoil.Kick(); // Projectile and flash snapshot the barrel before cosmetic recoil.
        _shotTimer = 1f / FinalStats.ShotsPerSecond;
        RecordAttackHeat();
    }
    #endregion

    #region Stat Resolution
    // =========================================================
    // Both paid application and preview resolve the same source/context with a different tier.
    public override TowerFinalStats ResolveLevel(int level) => ResolveRankedLevel(level);

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
        base._ExitTree();
        if (GodotObject.IsInstanceValid(_config)) _config.Changed -= RebuildStats;
        _initialized = false;
    }
    #endregion

    #region Placement and Selection
    // =========================================================
    // Modulate the existing sprite instead of instantiating selection effects.
    public override void SetSelected(bool selected)
    {
        Color tint = selected ? new Color(1f, .9f, .68f) : Colors.White;
        _sprite.Modulate = tint; _baseSprite.Modulate = tint;
    }
    #endregion
}
