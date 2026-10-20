// Placed Battle Tower with staggered reusable targeting and lightweight guided test bullets.
using Godot;

public partial class BattleTower : TowerBase
{
    public const string GunSpritePath = "TurretPivot/RecoilRoot/Sprite";
    public const string MuzzlePath = "TurretPivot/RecoilRoot/Muzzles/MuzzleA";
    public const string ConstructionSpritePath = "TurretPivot/ConstructionSprite";
    // =========================================================
    // Generic disabling clears this weapon's live lock/continuous presentation immediately.
    protected override void OnDisableChanged() { if (IsDisabled) { _target = null; _searchTimer = 0; CancelRecoil(); } }
    // =========================================================
    // Keep firing/aiming cadence intact; only invalidate the target search deadline.
    protected override void OnTargetPriorityChanged() => _searchTimer = 0;

    #region Settings and State
    private BattleTowerConfig _config;
    private bool _initialized;
    public override System.Collections.Generic.List<TowerLevel> Levels => Config?.Levels;
    [Export] public BattleTowerConfig Config
    {
        get => _config;
        set
        {
            if (_config == value) return;
            if (_initialized && GodotObject.IsInstanceValid(_config)) { _config.Changed -= RebuildStats; _config.RecoilStyleChanged -= SynchronizeRecoil; }
            _config = value;
            if (!_initialized) return;
            if (_config != null) { _config.Changed += RebuildStats; _config.RecoilStyleChanged += SynchronizeRecoil; }
            RebuildStats(); SynchronizeRecoil();
        }
    }
    public override TowerConfig AuthoredConfig => Config;
    public override float? InspectionRange => FinalStats?.RangePixels;
    // =========================================================
    // Battle alone opts into the weapon config and pivot layout.
    public override bool AcceptsConfig(TowerConfig config) => config is BattleTowerConfig;
    // =========================================================
    // Reject malformed Battle scenes before occupancy/payment, without imposing this on passive types.
    public override bool ValidateScene() => GetNodeOrNull<Node2D>("TurretPivot") != null && GetNodeOrNull<Sprite2D>(GunSpritePath) != null
        && GetNodeOrNull<Sprite2D>("BaseSprite") != null && GetNodeOrNull<Sprite2D>(ConstructionSpritePath) != null
        && GetNodeOrNull<TowerRecoilController>("Recoil") != null && GetNodeOrNull<Marker2D>(MuzzlePath) != null;
    // =========================================================
    // Per-instance initialization preserves the exported Battle Config and its change subscriptions.
    protected override void ApplyConfig(TowerConfig config) => Config = (BattleTowerConfig)config;
    private Sprite2D _sprite, _baseSprite, _constructionSprite;
    private Node2D _recoilRoot;
    private TowerRecoilController _recoil;
    private TowerConstructionVisuals _constructionVisuals;
    private Node2D _turret;
    private Marker2D _muzzle;
    private WeaponVisualRenderer _visuals;
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
        _sprite = GetNode<Sprite2D>(GunSpritePath); _baseSprite = GetNode<Sprite2D>("BaseSprite");
        _constructionSprite = GetNode<Sprite2D>(ConstructionSpritePath);
        _recoilRoot = GetNode<Node2D>("TurretPivot/RecoilRoot");
        _recoil = GetNode<TowerRecoilController>("Recoil");
        _constructionVisuals = GetNode<TowerConstructionVisuals>("ConstructionVisuals");
        _constructionVisuals.PresentationChanged += OnPresentationChanged;
        Destroyed += CancelRecoil;
        _muzzle = GetNode<Marker2D>(MuzzlePath);
        _visuals = WeaponVisuals.Attach(_turret);
        SetProcess(false);
        _initialized = true;
        if (Config != null) { Config.Changed += RebuildStats; Config.RecoilStyleChanged += SynchronizeRecoil; }
        RebuildStats(); SynchronizeRecoil();
        _registry = GetNodeOrNull<EnemyRegistry>("../../EnemySystems/Registry");
        _projectiles = GetNodeOrNull<Node2D>("../../Projectiles");
        _searchTimer = (GetInstanceId() % 97) / 97f * (FinalStats?.TargetSearchInterval ?? 0.15f);
        SetOperationalPhysicsProcess(_registry != null && _projectiles != null);
    }
    // =========================================================
    // Refresh First periodically to allow overtaking, checking cached range between searches.
    public override void _PhysicsProcess(double delta)
    {
        using var pf2 = SubsystemProfiler.Measure(this, ProfileCategory.Towers);
        if (!CanAttack) return;
        if (FinalStats == null) return;
        _searchTimer -= (float)delta; _shotTimer = Mathf.Max(-Mathf.Min((float)delta, .05f), _shotTimer - (float)delta);
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
        BasicProjectile bullet = new();
        bullet.Initialize(_target, FinalStats.ProjectileSpeed,
            new DamageInfo(FinalStats.DamagePerShot, DamageType.Kinetic, shieldPenetration: FinalStats.ShieldPenetration, source: DamageSource), visual: Config.ProjectileVisual, handling: FinalStats.Handling);
        _projectiles.AddChild(bullet); WeaponVisuals.Present(bullet, _muzzle, Config, _visuals);
        _recoil.Kick(); // Snapshot the real barrel-tip launch and flash before starting visual motion.
        _shotTimer = Mathf.Max(0, 1f / FinalStats.ShotsPerSecond + _shotTimer);
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
        if (GodotObject.IsInstanceValid(_recoil)) _recoil.Unbind();
        if (GodotObject.IsInstanceValid(_constructionVisuals)) _constructionVisuals.PresentationChanged -= OnPresentationChanged;
        Destroyed -= CancelRecoil;
        base._ExitTree();
        if (GodotObject.IsInstanceValid(_config)) { _config.Changed -= RebuildStats; _config.RecoilStyleChanged -= SynchronizeRecoil; }
        _initialized = false;
    }
    #endregion

    #region Recoil Presentation
    // =========================================================
    // Replacing presentation data rebinds/restores pose only; it does not rebuild combat snapshots or cooldowns.
    private void SynchronizeRecoil()
    { if (GodotObject.IsInstanceValid(_recoil)) _recoil.Bind(_recoilRoot, Config?.Recoil); }
    // =========================================================
    // Legacy custom composite artwork stays supported; no invisible gun remains displaced behind an overlay.
    private void OnPresentationChanged()
    { if (GodotObject.IsInstanceValid(_recoilRoot) && !_recoilRoot.IsVisibleInTree()) CancelRecoil(); }
    // =========================================================
    // Durability/disable cleanup can occur immediately after the last funded shot's heat is recorded.
    private void CancelRecoil() { if (GodotObject.IsInstanceValid(_recoil)) _recoil.Cancel(); }
    #endregion

    #region Placement and Selection
    // =========================================================
    // Modulate the existing sprite instead of instantiating selection effects.
    public override void SetSelected(bool selected)
    {
        Color tint = selected ? new Color(1f, .9f, .68f) : Colors.White;
        _sprite.Modulate = tint; _baseSprite.Modulate = tint; _constructionSprite.Modulate = tint;
    }
    #endregion
}
