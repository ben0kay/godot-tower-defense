// Shared aimed projectile behavior for Sniper/MachineGun and future N-muzzle Kinetic turrets.
using Godot;
using System.Collections.Generic;

public abstract partial class ProjectileTurretBase : TowerBase
{
    // =========================================================
    // Generic disabling clears this weapon's live lock/continuous presentation immediately.
    protected override void OnDisableChanged() { if (IsDisabled) { _target = null; _searchTimer = 0; _visuals?.SetAim(null, Vector2.Zero, 0, null, false); } }
    // =========================================================
    // Keep firing/aiming cadence intact; only invalidate the target search deadline.
    protected override void OnTargetPriorityChanged() => _searchTimer = 0;

    // =========================================================
    // Optional status weapons reuse the same bounded target search and launch-time projectile delivery.
    protected virtual StatusEffectId? RequiredEffect => null;
    protected virtual StatusEffectPayload? CreateEffect(TowerFinalStats stats) => null;
    #region Config and Runtime
    private ProjectileTurretConfig _config;
    private bool _initialized;
    private Node2D _pivot, _projectiles;
    private Sprite2D _sprite, _baseSprite, _constructionSprite;
    private TowerRecoilController _recoil;
    private EnemyRegistry _registry;
    private Enemy _target;
    private float _searchTimer, _shotTimer;
    private readonly List<Marker2D> _muzzles = new();
    private BarrelFireController _barrels;
    private WeaponVisualRenderer _visuals;

    [Export] public ProjectileTurretConfig Config
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
    public override List<TowerLevel> Levels => Config?.Levels;
    public override float? InspectionRange => FinalStats?.RangePixels;
    public int MuzzleCount => _muzzles.Count;
    #endregion

    #region Construction
    // =========================================================
    // A configured tower must have a shared aiming pivot, sprite and at least one Marker2D muzzle.
    public override bool ValidateScene()
    {
        Node2D pivot = GetNodeOrNull<Node2D>("TurretPivot");
        Node2D muzzles = GetNodeOrNull<Node2D>("TurretPivot/RecoilRoot/Muzzles");
        if (pivot == null || GetNodeOrNull<Sprite2D>("TurretPivot/RecoilRoot/Sprite") == null || muzzles == null) return false;
        int count = 0;
        foreach (Node node in muzzles.GetChildren())
        {
            if (node is not Marker2D) return false;
            count++;
        }
        return count > 0;
    }

    // =========================================================
    // Each tower retains a private instance of its own compatible authored configuration.
    protected override void ApplyConfig(TowerConfig config) => Config = (ProjectileTurretConfig)config;

    // =========================================================
    // Only a real placed tower binds world systems and runs one shared attack loop.
    public override void _Ready()
    {
        _pivot = GetNode<Node2D>("TurretPivot");
        _baseSprite = GetNodeOrNull<Sprite2D>("BaseSprite");
        _constructionSprite = GetNodeOrNull<Sprite2D>("TurretPivot/ConstructionSprite");
        _recoil = GetNodeOrNull<TowerRecoilController>("Recoil");
        _sprite = GetNode<Sprite2D>("TurretPivot/RecoilRoot/Sprite");
        Node2D holder = GetNode<Node2D>("TurretPivot/RecoilRoot/Muzzles");
        foreach (Node node in holder.GetChildren()) if (node is Marker2D muzzle) _muzzles.Add(muzzle);
        _barrels = new BarrelFireController(_muzzles.Count);
        _visuals = WeaponVisuals.Attach(_pivot);
        SetProcess(false);
        _initialized = true;
        if (Config != null) Config.Changed += RebuildStats;
        RebuildStats();
        _registry = GetNodeOrNull<EnemyRegistry>("../../EnemySystems/Registry");
        _projectiles = GetNodeOrNull<Node2D>("../../Projectiles");
        _searchTimer = (GetInstanceId() % 97) / 97f * (FinalStats?.TargetSearchInterval ?? .15f);
        SetOperationalPhysicsProcess(_registry != null && _projectiles != null);
    }
    #endregion

    #region Combat
    // =========================================================
    // One target, one turret rotation, one cooldown; no per-barrel searches or running muzzle nodes.
    public override void _PhysicsProcess(double delta)
    {
        using var pf2 = SubsystemProfiler.Measure(this, ProfileCategory.Towers);
        if (!CanAttack || Config == null || FinalStats == null || _registry == null || _projectiles == null
            || !double.IsFinite(delta) || delta <= 0)
        { _visuals?.SetAim(null, Vector2.Zero, 0, null, false); return; }
        _searchTimer -= (float)delta;
        // Retain at most one small elapsed-frame overshoot: fractional attack rates should not round down to 60Hz ticks.
        _shotTimer = Mathf.Max(-Mathf.Min((float)delta, .05f), _shotTimer - (float)delta);
        if (_searchTimer <= 0)
        {
            _target = TowerTargeting.Find(_registry, GlobalPosition, Mathf.Max(0, FinalStats.RangePixels), Config.TargetPriority, RequiredEffect, detectsStealth: Config.CanDetectStealth);
            _searchTimer = Mathf.Max(.05f, FinalStats.TargetSearchInterval);
        }
        if (!TowerTargeting.Valid(_target, GlobalPosition, Mathf.Max(0, FinalStats.RangePixels), Config.CanDetectStealth)
            || !TowerTargeting.Eligible(_target, RequiredEffect))
        { _visuals.SetAim(null, Vector2.Zero, 0, null, false); return; }
        bool aligned = TurretAiming.Track(_pivot, _target.GlobalPosition, FinalStats.RotationSpeedDegrees,
            FinalStats.AimToleranceDegrees, Config.RotateWhileTracking, delta);
        _visuals.SetAim(_muzzles[0], _target.GlobalPosition, FinalStats.RangePixels, Config.AimLaser,
            Ammunition == null || Ammunition.Inventory.Amount > 0);
        if (!aligned || _shotTimer > 0 || FinalStats.ShotsPerSecond <= 0) return;
        FireVolley();
    }

    // =========================================================
    // Total ShotsPerSecond means total bullets per second. Simultaneous volleys cost/pay all rounds atomically.
    private void FireVolley()
    {
        BarrelFireMode mode = Config.FiringMode;
        int count = _barrels.ShotsPerVolley(mode);
        int perBullet = Ammunition?.Stats.PerShot ?? 0;
        if (perBullet > 0 && (long)Ammunition.Inventory.Amount < (long)count * perBullet) return;
        DamageInfo hit = Config.CreateHit(FinalStats, DamageSource);
        for (int i = 0; i < count; i++)
        {
            if (Ammunition?.TryFire() == false) return;
            Marker2D muzzle = _muzzles[_barrels.MuzzleIndex(mode, i)];
            BasicProjectile bullet = new();
            bullet.Initialize(_target, FinalStats.ProjectileSpeed, hit, CreateEffect(FinalStats), Config.ProjectileVisual, FinalStats.Handling, Config.ShieldRicochet);
            _projectiles.AddChild(bullet);
            WeaponVisuals.Present(bullet, muzzle, Config, _visuals);
        }
        _recoil?.Kick(); // All funded volley origins are captured before the assembly moves.
        _barrels.Commit(mode);
        RecordAttackHeat(count);
        // Preserve fractional frame remainder; never schedule multiple catch-up volleys after a long hitch.
        _shotTimer = Mathf.Max(0f, count / FinalStats.ShotsPerSecond + _shotTimer);
    }
    #endregion

    #region Progression and Selection
    // =========================================================
    // The same ranked complete-level snapshot supplies both upgrades and live attack values.
    public override TowerFinalStats ResolveLevel(int level) => ResolveRankedLevel(level);

    // =========================================================
    // All effects of a purchased level or earned promotion flow through one cached stat rebuild.
    public void RebuildStats()
    {
        FinalStats = Config == null ? null : ResolveLevel(UpgradeLevel);
        NotifyStatsChanged();
    }

    // =========================================================
    // Keep selection tint on the optional construction overlay as well as the finished gun/base.
    public override void SetSelected(bool selected)
    {
        Color tint = selected ? new Color(1f, .9f, .68f) : Colors.White;
        if (GodotObject.IsInstanceValid(_sprite)) _sprite.Modulate = tint;
        if (GodotObject.IsInstanceValid(_baseSprite)) _baseSprite.Modulate = tint;
        if (GodotObject.IsInstanceValid(_constructionSprite)) _constructionSprite.Modulate = tint;
    }

    // =========================================================
    // Release resource listeners when the placed tower leaves the battlefield.
    public override void _ExitTree()
    {
        base._ExitTree();
        if (GodotObject.IsInstanceValid(_config)) _config.Changed -= RebuildStats;
        _initialized = false; _target = null; _muzzles.Clear(); _barrels = null;
    }
    #endregion

    // =========================================================
    // Checkpoint restore sets source state directly; callers validate before publishing the battlefield.
    internal override double CaptureWeaponCooldown() => _shotTimer;
    internal override void RestoreWeaponCooldown(double seconds) => _shotTimer = (float)seconds;
}
