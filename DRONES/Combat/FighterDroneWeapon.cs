// Fixed twin hull guns alternate accepted rounds; only forward firing passes spend real ammo.
using Godot;
using System;

public partial class FighterDroneWeapon : Node
{
    private FighterDrone _fighter;
    private WorldGrid _grid;
    private Node2D _projectiles, _hull;
    private Marker2D _leftMuzzle, _rightMuzzle;
    private WeaponVisualRenderer _visuals;
    private DamageSource _source;
    private double _cooldown;

    // =========================================================
    // Parent fighter owns tick and lifetime; no independent physics/target scans or cooldown timers.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _fighter = GetParent<FighterDrone>();
        _hull = GetNode<Node2D>("../Body/Sprite");
        _leftMuzzle = GetNode<Marker2D>("../Body/Sprite/LeftMuzzle");
        _rightMuzzle = GetNode<Marker2D>("../Body/Sprite/RightMuzzle");
        _visuals = WeaponVisuals.Attach(_hull);
        _source = new DamageSource(_fighter);
    }

    internal void Bind(FighterDrone fighter, WorldGrid grid, Node2D projectiles)
    { _fighter = fighter; _grid = grid; _projectiles = projectiles; }

    // =========================================================
    // No turret tracking: a small forward cone allows modest aim correction after the hull lines up.
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
        Marker2D muzzle = ((_fighter.AmmoCapacity - _fighter.Ammo) & 1) == 0 ? _leftMuzzle : _rightMuzzle;
        Vector2 delta = target.GlobalPosition - muzzle.GlobalPosition;
        if (!delta.IsFinite() || delta.LengthSquared() > range * range) return false;
        float speed = FighterDroneConfig.Safe(config.BulletSpeed, 1200, 40, 3000);
        Vector2 aimPoint = target.GlobalPosition + Vector2.FromAngle(target.Rotation) * target.EffectiveMoveSpeed *
            Mathf.Min(delta.Length() / speed, FighterDroneConfig.Safe(config.InterceptLeadSeconds, .8f, 0, 3));
        Vector2 direction = (aimPoint - muzzle.GlobalPosition).Normalized();
        Vector2 forward = -_hull.GlobalTransform.Y.Normalized();
        float tolerance = Mathf.DegToRad(FighterDroneConfig.Safe(config.AimToleranceDegrees, 8, 0, 45));
        bool aligned = forward.Dot(direction) >= Mathf.Cos(tolerance);
        if (!aligned || _fighter.Ammo <= 0 || _cooldown > 0 || !TowerDetection.CanDetect(target) ||
            !_grid.HasWeaponLineOfSight(muzzle.GlobalPosition, target.GlobalPosition) ||
            !_grid.HasWeaponLineOfSight(muzzle.GlobalPosition, aimPoint)) return false;

        float damage = FighterDroneConfig.Safe(config.DamagePerShot, 4, 0, 1000);
        float rate = FighterDroneConfig.Safe(config.ShotsPerSecond, 9, .1f, 30);
        if (!direction.IsFinite() || direction.LengthSquared() < .5f || damage <= 0 || rate <= 0) return false;

        // =========================================================
        // Same swept contact/shield/Kinetic rules as friendly tower rounds, not the hostile tower-hit system.
        BasicProjectile bullet = new();
        bullet.Initialize(target, speed, new DamageInfo(damage, DamageType.Kinetic, source: _source),
            visual: config.ProjectileVisual);
        _projectiles.AddChild(bullet);
        bullet.LaunchFrom(muzzle.GlobalPosition, direction);
        _visuals?.FlashAt(muzzle, config.MuzzleFlash);
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
        _fighter = null; _grid = null; _projectiles = null; _hull = null; _leftMuzzle = null; _rightMuzzle = null;
        _visuals = null; _source = null;
    }
}
