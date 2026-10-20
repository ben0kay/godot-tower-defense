// Shared straight-by-default projectile: immutable launch spread, spatial swept contact, optional explicit homing.
using Godot;

public partial class BasicProjectile : Node2D
{
    #region Launch State
    private Enemy _target;
    private EnemyRegistry _registry;
    private Vector2 _initialTarget;
    private float _speed;
    private double _life = 5;
    private DamageInfo _damage;
    private StatusEffectPayload? _effect;
    private bool _impacted, _launched;
    private ProjectileHandlingStats _handling;
    private ProjectileVisualSnapshot? _visual;
    private Vector2 _travelDirection = Vector2.Up;
    public ProjectileVisualSnapshot? VisualSnapshot => _visual;
    public Vector2 TravelDirection => _travelDirection;
    public ProjectileHandlingStats Handling => _handling;
    // =========================================================
    // Compatibility payload overload retains zero-spread straight delivery unless a caller explicitly opts into homing.
    public void Initialize(Enemy target, float speed, float damage) => Initialize(target, speed, new DamageInfo(damage, DamageType.Kinetic));
    // =========================================================
    // Capture value payloads once; straight shots keep no original-target reference after initialization.
    public void Initialize(Enemy target, float speed, DamageInfo damage, StatusEffectPayload? effect = null,
        ProjectileVisualStyle visual = null, ProjectileHandlingStats? handling = null)
    {
        if (!float.IsFinite(speed) || speed <= 0) throw new System.ArgumentOutOfRangeException(nameof(speed));
        _handling = ProjectileHandlingStats.Sanitize(handling ?? new());
        _registry = GodotObject.IsInstanceValid(target) ? target.Registry : null;
        _initialTarget = GodotObject.IsInstanceValid(target) ? target.GlobalPosition : Vector2.Zero;
        _target = _handling.Guidance == ProjectileGuidance.Homing ? target : null;
        _speed = speed; _damage = damage; _effect = effect; _visual = visual?.Capture();
        _life = 5; _impacted = false; _launched = false;
    }
    // =========================================================
    // Production supplies actual barrel forward; legacy deterministic fixtures may aim at the captured initial point.
    public void LaunchFrom(Vector2 worldPosition, Vector2? forward = null, float? spreadSample = null)
    {
        if (_launched) return;
        if (!worldPosition.IsFinite()) { _impacted = true; QueueFree(); return; }
        GlobalPosition = worldPosition;
        _travelDirection = ProjectileLaunch.Direction(forward ?? (_initialTarget - worldPosition), _handling.SpreadDegrees, spreadSample);
        GlobalRotation = _travelDirection.Angle(); _launched = true;
    }
    #endregion

    #region Independent Travel
    // =========================================================
    // Straight rounds ignore former target death/range; homing remains an explicit capability with target-loss cleanup.
    public override void _PhysicsProcess(double delta)
    {
        if (_impacted || IsQueuedForDeletion() || !IsInsideTree() || GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        if (!GodotObject.IsInstanceValid(_registry) || !_registry.IsInsideTree() || _registry.IsQueuedForDeletion()) { QueueFree(); return; }
        if (!_launched) LaunchFrom(GlobalPosition);
        if (_impacted) return;
        if (_handling.Guidance == ProjectileGuidance.Homing)
        {
            if (!GodotObject.IsInstanceValid(_target) || !_target.IsAlive || _target.IsQueuedForDeletion()) { QueueFree(); return; }
            Vector2 direction = _target.GlobalPosition - GlobalPosition;
            if (direction.IsFinite() && direction.LengthSquared() > .0001f)
            { _travelDirection = direction.Normalized(); GlobalRotation = _travelDirection.Angle(); }
        }
        double seconds = System.Math.Min(delta, _life);
        Vector2 from = GlobalPosition;
        Vector2 end = new((float)(from.X + (double)_travelDirection.X * _speed * seconds),
            (float)(from.Y + (double)_travelDirection.Y * _speed * seconds));
        if (!end.IsFinite()) { QueueFree(); return; }
        Enemy hit = ProjectileEnemyContact.First(_registry, from, end, out double entry);
        if (hit != null)
        {
            _impacted = true; GlobalPosition = from.Lerp(end, (float)entry); QueueFree();
            hit.ApplyDamage(_damage);
            if (_effect.HasValue && GodotObject.IsInstanceValid(hit) && hit.IsAlive && !hit.IsQueuedForDeletion()) hit.ApplyStatus(_effect.Value);
            return;
        }
        GlobalPosition = end; _life -= seconds;
        if (_life <= 0) QueueFree();
    }
    // =========================================================
    // World unload is terminal even if a caller manually ticks a retained managed wrapper.
    public override void _ExitTree() { _impacted = true; _target = null; _registry = null; }
    #endregion

    // =========================================================
    // Layered tracer or legacy circle fallback; local +X follows the cached guided travel rotation.
    public override void _Draw()
    {
        if (!_visual.HasValue)
        { DrawCircle(Vector2.Zero, 3f, _effect.HasValue ? new Color(.35f, .9f, 1f) : new Color(1f, .85f, .35f)); return; }
        var style = _visual.Value; if (!style.Enabled) return;
        Vector2 forward = Vector2.Right;
        Vector2 tail = -forward * style.Length;
        if (GodotObject.IsInstanceValid(style.Texture))
        {
            DrawSetTransform(Vector2.Zero, forward.Angle());
            DrawTextureRect(style.Texture, new Rect2(-style.Length, -style.Width * .5f, style.Length, style.Width), false, style.Body);
            DrawSetTransform(Vector2.Zero);
        }
        else
        {
            DrawLine(tail, Vector2.Zero, style.Glow, style.Width * style.GlowWidth, true);
            DrawLine(tail, Vector2.Zero, style.Body, style.Width * 1.6f, true);
            DrawLine(tail * .75f, Vector2.Zero, style.Core, style.Width, true);
        }
        if (style.BrightTip) DrawLine(-forward * style.Width * 1.5f, Vector2.Zero, style.Core, style.Width * 1.2f, true);
    }
}
