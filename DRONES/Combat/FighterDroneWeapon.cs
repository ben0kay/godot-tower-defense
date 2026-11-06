// FD4 fighter's independently aimed blue gun: shared physical friendly projectiles, one cooldown and finite real ammo.
using Godot;
using System;

public partial class FighterDroneWeapon : Node
{
    private FighterDrone _fighter;
    private WorldGrid _grid;
    private Node2D _projectiles, _pivot;
    private Marker2D _muzzle;
    private WeaponVisualRenderer _visuals;
    private DamageSource _source;
    private double _cooldown;

    // =========================================================
    // Parent fighter owns tick and lifetime; no independent physics/target scans or cooldown timers.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _fighter = GetParent<FighterDrone>();
        _pivot = GetNode<Node2D>("../Body/GunPivot");
        _muzzle = GetNode<Marker2D>("../Body/GunPivot/Muzzle");
        _visuals = WeaponVisuals.Attach(_pivot);
        _source = new DamageSource(_fighter);
    }

    internal void Bind(FighterDrone fighter, WorldGrid grid, Node2D projectiles)
    { _fighter = fighter; _grid = grid; _projectiles = projectiles; }

    // =========================================================
    // Continue steering the gun regardless of cooldown; only an aligned, unobstructed shot spends ammo.
    internal bool Step(float dt, Enemy target)
    {
        if (!float.IsFinite(dt) || dt <= 0 || !GodotObject.IsInstanceValid(_fighter) ||
            !GodotObject.IsInstanceValid(_grid) || !_grid.IsInsideTree() ||
            !GodotObject.IsInstanceValid(_projectiles) || !_projectiles.IsInsideTree() ||
            _fighter.IsQueuedForDeletion() || !_fighter.IsFlying || _fighter.State is FighterDroneState.Returning or FighterDroneState.Launching ||
            _fighter.GetTree().Paused) return false;

        _cooldown = Math.Max(-Math.Min(dt, .05f), _cooldown - dt);
        if (!GodotObject.IsInstanceValid(target) || !target.IsAlive || target.IsQueuedForDeletion() ||
            !target.IsInsideTree() || !GodotObject.IsInstanceValid(target.Registry) ||
            !target.Registry.Contains(target) || _fighter.Config == null) return false;

        FighterDroneConfig config = _fighter.Config;
        float range = FighterDroneConfig.Safe(config.WeaponRangePixels, 275, 16, 1600);
        Vector2 delta = target.GlobalPosition - _muzzle.GlobalPosition;
        if (!delta.IsFinite() || delta.LengthSquared() > range * range) return false;
        bool aligned = TurretAiming.Track(_pivot, target.GlobalPosition,
            FighterDroneConfig.Safe(config.GunTurnDegreesPerSecond, 540, 0, 1440),
            FighterDroneConfig.Safe(config.AimToleranceDegrees, 10, 0, 45), true, dt);
        if (!aligned || _fighter.Ammo <= 0 || _cooldown > 0 ||
            !_grid.HasWeaponLineOfSight(_muzzle.GlobalPosition, target.GlobalPosition)) return false;

        Vector2 direction = -_muzzle.GlobalTransform.Y.Normalized();
        float speed = FighterDroneConfig.Safe(config.BulletSpeed, 1200, 40, 3000);
        float damage = FighterDroneConfig.Safe(config.DamagePerShot, 4, 0, 1000);
        float rate = FighterDroneConfig.Safe(config.ShotsPerSecond, 5, .1f, 30);
        if (!direction.IsFinite() || direction.LengthSquared() < .5f || damage <= 0 || rate <= 0) return false;

        // =========================================================
        // Same swept contact/shield/Kinetic rules as friendly tower rounds, not the hostile tower-hit system.
        BasicProjectile bullet = new();
        bullet.Initialize(target, speed, new DamageInfo(damage, DamageType.Kinetic, source: _source),
            visual: config.ProjectileVisual);
        _projectiles.AddChild(bullet);
        bullet.LaunchFrom(_muzzle.GlobalPosition, direction);
        _visuals?.FlashAt(_muzzle, config.MuzzleFlash);
        _fighter.RecordFiredRound();
        _cooldown = Math.Max(0, 1.0 / rate + _cooldown);
        return true;
    }

    // =========================================================
    // Recall/dock stop transient muzzle flash but never replenish ammunition or reset cooldown for free.
    internal void Suspend() => _visuals?.ClearFlashes();
    internal double CaptureCooldown() => _cooldown > 0 ? _cooldown : 0;
    internal void RestoreCooldown(double seconds) => _cooldown = double.IsFinite(seconds) ? Math.Clamp(seconds, 0, 60) : 0;

    public override void _ExitTree()
    {
        _visuals?.ClearFlashes();
        _fighter = null; _grid = null; _projectiles = null; _pivot = null; _muzzle = null;
        _visuals = null; _source = null;
    }
}
