// Value-state flight shared by buffered friendly rounds and the standalone projectile adapter.
using Godot;

internal struct FriendlyProjectileFlight
{
    private Enemy _target;
    private EnemyRegistry _registry;
    private Vector2 _initialTarget;
    private float _speed;
    private double _life;
    private DamageInfo _damage;
    private StatusEffectPayload? _effect;
    private bool _shieldRicochet, _lostReveal;
    private int _hullSparkCount;
    private WorldLightingController.ProjectileLight _light;
    internal Vector2 Position { get; set; }
    internal Vector2 Direction { get; private set; }
    internal ProjectileVisualSnapshot? Visual { get; private set; }
    internal ProjectileHandlingStats Handling { get; private set; }
    internal StatusEffectPayload? Effect => _effect;
    internal bool RegistryIsLive => GodotObject.IsInstanceValid(_registry) && _registry.IsInsideTree() && !_registry.IsQueuedForDeletion();

    // =========================================================
    // Snapshot all authored inputs once. Straight rounds keep no target reference.
    internal FriendlyProjectileFlight(Enemy target, float speed, DamageInfo damage, StatusEffectPayload? effect,
        ProjectileVisualStyle visual, ProjectileHandlingStats? handling, bool shieldRicochet, int hullHitSparkCount)
    {
        if (!float.IsFinite(speed) || speed <= 0) throw new System.ArgumentOutOfRangeException(nameof(speed));
        Handling = ProjectileHandlingStats.Sanitize(handling ?? new());
        bool valid = GodotObject.IsInstanceValid(target);
        _registry = valid ? target.Registry : null;
        _initialTarget = valid ? target.GlobalPosition : Vector2.Zero;
        _target = Handling.Guidance == ProjectileGuidance.Homing ? target : null;
        _speed = speed; _damage = damage; _effect = effect; Visual = visual?.Capture();
        _shieldRicochet = shieldRicochet; _hullSparkCount = Mathf.Clamp(hullHitSparkCount, 0, 8);
        _life = 5; Direction = Vector2.Up;
    }

    // =========================================================
    // Use the original launch sampler and borrow a cosmetic light only when the style opts in.
    internal void Launch(Vector2 position, Vector2? forward, float? spreadSample, WorldLightingController lighting)
    {
        Position = position;
        Direction = ProjectileLaunch.Direction(forward ?? (_initialTarget - position), Handling.SpreadDegrees, spreadSample);
        if (Visual is { Enabled: true, EmitsWorldLight: true } style && GodotObject.IsInstanceValid(lighting))
            _light = lighting.TryTrackProjectile(position, style.Body, false);
    }

    // =========================================================
    // A step either survives or retires once. Hit callbacks receive the swept first contact.
    internal bool Advance(double delta, RicochetController ricochets, HullHitSparkController hullSparks)
    {
        if (!RegistryIsLive) return false;
        if (Handling.Guidance == ProjectileGuidance.Homing && !_lostReveal)
        {
            if (!GodotObject.IsInstanceValid(_target) || !_target.IsAlive || _target.IsQueuedForDeletion()) return false;
            if (!TowerDetection.IsLit(_target)) { _lostReveal = true; _target = null; }
            else
            {
                Vector2 direction = _target.GlobalPosition - Position;
                if (direction.IsFinite() && direction.LengthSquared() > .0001f) Direction = direction.Normalized();
            }
        }
        double seconds = System.Math.Min(delta, _life);
        Vector2 end = new((float)(Position.X + (double)Direction.X * _speed * seconds),
            (float)(Position.Y + (double)Direction.Y * _speed * seconds));
        if (!end.IsFinite()) return false;
        Enemy hit = ProjectileEnemyContact.First(_registry, Position, end, out double entry);
        if (hit != null)
        {
            Position = Position.Lerp(end, (float)entry);
            ReleaseLight();
            ResolveHit(hit, ricochets, hullSparks);
            return false;
        }
        Position = end;
        _life -= seconds;
        if (_life <= 0) return false;
        _light?.Move(end);
        return true;
    }

    // =========================================================
    // Reuse the same shield absorption, damage attribution, feedback and status ordering.
    private void ResolveHit(Enemy hit, RicochetController ricochets, HullHitSparkController hullSparks)
    {
        Vector2 point = default, normal = default, reflected = default;
        // Extend the incoming line backwards: a prior physics tick may already be inside the larger visual rim.
        bool bounce = _shieldRicochet && _damage.Type == DamageType.Kinetic && Visual.HasValue &&
            GodotObject.IsInstanceValid(ricochets) && ricochets.Available && hit.TryGetShieldRim(out var ellipse) &&
            RicochetGeometry.Reflect(ellipse, Position - Direction * (2 * (ellipse.X.Length() + ellipse.Y.Length()) + 1),
                Position, Direction, out point, out normal, out reflected);
        // Hull-only feedback compares authoritative HP before and after this single hit.
        float hullBefore = hit.Health;
        float absorbed = hit.ApplyDamageWithShieldAbsorption(_damage);
        if (bounce && absorbed > 0 && GodotObject.IsInstanceValid(ricochets))
            ricochets.Spawn(point, normal, reflected, _speed, Visual.Value);
        if (_damage.Type == DamageType.Kinetic && _hullSparkCount > 0 &&
            GodotObject.IsInstanceValid(hit) && hit.Health < hullBefore &&
            GodotObject.IsInstanceValid(hullSparks))
            hullSparks.Spawn(Position, Direction, _hullSparkCount);
        if (_effect.HasValue && GodotObject.IsInstanceValid(hit) && hit.IsAlive && !hit.IsQueuedForDeletion()) hit.ApplyStatus(_effect.Value.WithSource(_damage.Source));
    }

    internal void ReleaseLight() { _light?.Release(); _light = null; }
}
