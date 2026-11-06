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
    private EnemyRegistry _registry;
    private Enemy _weaponTarget;
    private int _targetGeneration;
    private float _scan, _hold;
    private bool _replacementReady = true;

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
        _source = new DamageSource(_fighter); _scan = _fighter.Slot * .04f;
    }

    internal void Bind(FighterDrone fighter, WorldGrid grid, Node2D projectiles)
    { _fighter = fighter; _grid = grid; _projectiles = projectiles;
        _registry = grid.GetNodeOrNull<EnemyRegistry>("../EnemySystems/Registry"); }

    // =========================================================
    // No turret tracking: the full configurable firing arc permits tracking across the forward hemisphere.
    internal bool Step(float dt)
    {
        if (!float.IsFinite(dt) || dt <= 0 || !GodotObject.IsInstanceValid(_fighter) ||
            !GodotObject.IsInstanceValid(_grid) || !_grid.IsInsideTree() ||
            !GodotObject.IsInstanceValid(_projectiles) || !_projectiles.IsInsideTree() ||
            _fighter.IsQueuedForDeletion() || _fighter.Health <= 0 || _fighter.Config == null || !_fighter.IsFlying || _fighter.State is FighterDroneState.Returning or FighterDroneState.Launching ||
            _fighter.GetTree().Paused) return false;

        _cooldown = Math.Max(-Math.Min(dt, .05f), _cooldown - dt);
        if (_fighter.Ammo <= 0) { ClearTarget(); return false; }
        _scan -= dt; _hold = Mathf.Max(0, _hold - dt);
        bool valid = GodotObject.IsInstanceValid(_weaponTarget) && _weaponTarget.SpawnGeneration == _targetGeneration &&
            TryAim(_weaponTarget, out _, out _);
        bool invalidated = _weaponTarget != null && !valid;
        if (!valid) _weaponTarget = null;
        bool regular = _scan <= 0;
        if (regular || invalidated && _replacementReady)
        {
            if (regular) { _scan = FighterDroneConfig.Safe(_fighter.Config.WeaponSearchSeconds, .2f, .1f, 2); _replacementReady = true; }
            else _replacementReady = false;
            if (!valid || _hold <= 0)
            {
                var visitor = new WeaponVisitor { Weapon = this, Origin = NextMuzzle.GlobalPosition,
                    BestDistance = float.PositiveInfinity, BestOrder = ulong.MaxValue };
                if (GodotObject.IsInstanceValid(_registry))
                    _registry.VisitEnemiesInRadius(visitor.Origin,
                        FighterDroneConfig.Safe(_fighter.Config.WeaponRangePixels, 275, 16, 1600), ref visitor);
                if (_weaponTarget != visitor.Best)
                    _hold = FighterDroneConfig.Safe(_fighter.Config.WeaponTargetHoldSeconds, .25f, 0, 2);
                _weaponTarget = visitor.Best; _targetGeneration = _weaponTarget?.SpawnGeneration ?? 0;
            }
        }
        Enemy target = _weaponTarget;
        if (_cooldown > 0 || !TryAim(target, out Marker2D muzzle, out Vector2 direction)) return false;
        FighterDroneConfig config = _fighter.Config;
        float speed = FighterDroneConfig.Safe(config.BulletSpeed, 1200, 40, 3000);
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

    private Marker2D NextMuzzle => (_fighter.RoundsFired & 1) == 0 ? _leftMuzzle : _rightMuzzle;
    // =========================================================
    // One eligibility rule is shared by acquisition, cached-lock validation and the actual funded shot.
    private bool TryAim(Enemy target, out Marker2D muzzle, out Vector2 direction)
    {
        muzzle = NextMuzzle; direction = Vector2.Zero;
        if (!TowerDetection.CanDetect(target) || !target.IsInsideTree() ||
            !GodotObject.IsInstanceValid(_registry) || !_registry.Contains(target)) return false;
        var config = _fighter.Config;
        Vector2 delta = target.GlobalPosition - muzzle.GlobalPosition;
        float range = FighterDroneConfig.Safe(config.WeaponRangePixels, 275, 16, 1600);
        if (!delta.IsFinite() || delta.LengthSquared() > range * range) return false;
        float speed = FighterDroneConfig.Safe(config.BulletSpeed, 1200, 40, 3000);
        Vector2 aim = target.GlobalPosition + Vector2.FromAngle(target.Rotation) * target.EffectiveMoveSpeed *
            Mathf.Min(delta.Length() / speed, FighterDroneConfig.Safe(config.InterceptLeadSeconds, .8f, 0, 3));
        direction = (aim - muzzle.GlobalPosition).Normalized();
        float angle = Mathf.DegToRad(FighterDroneConfig.Safe(config.FiringArcDegrees, 180, 0, 360) * .5f);
        return direction.IsFinite() && direction.LengthSquared() > .5f &&
            (-_hull.GlobalTransform.Y.Normalized()).Dot(direction) >= Mathf.Cos(angle) &&
            _grid.HasWeaponLineOfSight(muzzle.GlobalPosition, target.GlobalPosition) &&
            _grid.HasWeaponLineOfSight(muzzle.GlobalPosition, aim);
    }
    private struct WeaponVisitor : EnemySpatialIndex.INeighborVisitor
    {
        internal FighterDroneWeapon Weapon;
        internal Vector2 Origin;
        internal Enemy Best;
        internal float BestDistance;
        internal ulong BestOrder;
        public void Accept(EnemySpatialIndex.Neighbor neighbor)
        {
            float distance = Origin.DistanceSquaredTo(neighbor.Position);
            if (distance > BestDistance || distance == BestDistance && neighbor.Order >= BestOrder ||
                !Weapon.TryAim(neighbor.Enemy, out _, out _)) return;
            Best = neighbor.Enemy; BestDistance = distance; BestOrder = neighbor.Order;
        }
    }
    private void ClearTarget() { _weaponTarget = null; _targetGeneration = 0; _hold = 0; }
    // =========================================================
    // Recall/dock stop transient muzzle flash but never replenish ammunition or reset cooldown for free.
    internal void Suspend() { ClearTarget(); _scan = _fighter.Slot * .04f; _replacementReady = true; _visuals?.ClearFlashes(); }
    internal double CaptureCooldown() => _cooldown > 0 ? _cooldown : 0;
    internal void RestoreCooldown(double seconds) { Suspend(); _cooldown = double.IsFinite(seconds) ? Math.Clamp(seconds, 0, 60) : 0; }

    public override void _ExitTree()
    {
        ClearTarget(); _registry = null; _visuals?.ClearFlashes();
        _fighter = null; _grid = null; _projectiles = null; _hull = null; _leftMuzzle = null; _rightMuzzle = null;
        _visuals = null; _source = null;
    }
}
