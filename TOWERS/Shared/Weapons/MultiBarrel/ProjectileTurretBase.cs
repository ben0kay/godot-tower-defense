// Shared aimed projectile behavior for Sniper/MachineGun and future N-muzzle Kinetic turrets.
using Godot;
using System.Collections.Generic;

public abstract partial class ProjectileTurretBase : TowerBase
{
    // =========================================================
    // Optional status weapons reuse the same bounded target search and launch-time projectile delivery.
    protected virtual StatusEffectId? RequiredEffect => null;
    protected virtual StatusEffectPayload? CreateEffect(TowerFinalStats stats) => null;
    #region Config and Runtime
    private ProjectileTurretConfig _config;
    private bool _initialized;
    private Node2D _pivot, _projectiles;
    private Sprite2D _sprite;
    private EnemyRegistry _registry;
    private Enemy _target;
    private float _searchTimer, _shotTimer;
    private readonly List<Marker2D> _muzzles = new();
    private BarrelFireController _barrels;

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
        Node2D muzzles = GetNodeOrNull<Node2D>("TurretPivot/Muzzles");
        if (pivot == null || GetNodeOrNull<Sprite2D>("TurretPivot/Sprite") == null || muzzles == null) return false;
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
        _sprite = GetNode<Sprite2D>("TurretPivot/Sprite");
        Node2D holder = GetNode<Node2D>("TurretPivot/Muzzles");
        foreach (Node node in holder.GetChildren()) if (node is Marker2D muzzle) _muzzles.Add(muzzle);
        _barrels = new BarrelFireController(_muzzles.Count);
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
        if (!IsOperational || Config == null || FinalStats == null || _registry == null || _projectiles == null
            || !double.IsFinite(delta) || delta <= 0) return;
        _searchTimer -= (float)delta;
        // Retain at most one small elapsed-frame overshoot: fractional attack rates should not round down to 60Hz ticks.
        _shotTimer = Mathf.Max(-Mathf.Min((float)delta, .05f), _shotTimer - (float)delta);
        if (_searchTimer <= 0)
        {
            _target = TowerTargeting.Find(_registry, GlobalPosition, Mathf.Max(0, FinalStats.RangePixels), Config.TargetPriority, RequiredEffect);
            _searchTimer = Mathf.Max(.05f, FinalStats.TargetSearchInterval);
        }
        if (!TowerTargeting.Valid(_target, GlobalPosition, Mathf.Max(0, FinalStats.RangePixels))
            || !TowerTargeting.Eligible(_target, RequiredEffect)) return;
        bool aligned = TurretAiming.Track(_pivot, _target.GlobalPosition, FinalStats.RotationSpeedDegrees,
            FinalStats.AimToleranceDegrees, Config.RotateWhileTracking, delta);
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
            bullet.Initialize(_target, FinalStats.ProjectileSpeed, hit, CreateEffect(FinalStats));
            _projectiles.AddChild(bullet);
            bullet.GlobalPosition = muzzle.GlobalPosition;
        }
        _barrels.Commit(mode);
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
    // Keep selection visual on the rotating sprite while leaving placement coordinates fixed.
    public override void SetSelected(bool selected)
    {
        if (GodotObject.IsInstanceValid(_sprite))
            _sprite.Modulate = selected ? new Color(1f, .9f, .68f) : Colors.White;
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
}
