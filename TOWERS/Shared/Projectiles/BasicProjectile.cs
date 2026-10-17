// Shared guided projectile; swept target contact prevents tunneling at high bullet speeds.
using Godot;

public partial class BasicProjectile : Node2D
{
    private Enemy _target;
    private float _speed, _life = 5f;
    private DamageInfo _damage;
    private StatusEffectPayload? _effect;
    private bool _impacted, _headingSet;
    private ProjectileVisualSnapshot? _visual;
    private Vector2 _travelDirection = Vector2.Up;
    public ProjectileVisualSnapshot? VisualSnapshot => _visual;
    public Vector2 TravelDirection => _travelDirection;

    // =========================================================
    // Explicit initialization keeps this helper independent of any enemy/tower type.
    public void Initialize(Enemy target, float speed, float damage) => Initialize(target, speed, new DamageInfo(damage, DamageType.Kinetic));

    // =========================================================
    // Future projectile types can supply a typed base payload without duplicating defense math.
    public void Initialize(Enemy target, float speed, DamageInfo damage, StatusEffectPayload? effect = null, ProjectileVisualStyle visual = null)
    { _target = target; _speed = Mathf.Max(1f, speed); _damage = damage; _effect = effect; _impacted = false; _visual = visual?.Capture(); _life = 5; _headingSet = false; }

    // =========================================================
    // Set appearance heading immediately at the real muzzle, including the first zero-motion rendered frame.
    public void LaunchFrom(Vector2 worldPosition)
    { GlobalPosition = worldPosition; UpdateHeading(); if (!_headingSet) GlobalRotation = _travelDirection.Angle(); }

    // =========================================================
    // Guided visual direction follows actual travel; zero-distance impacts retain their last finite heading.
    private void UpdateHeading()
    {
        if (!GodotObject.IsInstanceValid(_target)) return;
        Vector2 direction = _target.GlobalPosition - GlobalPosition;
        if (!direction.IsFinite() || direction.LengthSquared() <= .0001f) return;
        direction = direction.Normalized();
        if (!_headingSet || direction != _travelDirection)
        { _travelDirection = direction; _headingSet = true; GlobalRotation = direction.Angle(); }
    }

    // =========================================================
    // Bullets expire cleanly if their target dies/arrives, avoiding invalid native references.
    public override void _PhysicsProcess(double delta)
    {
        if (_impacted || IsQueuedForDeletion() || !double.IsFinite(delta) || delta <= 0) return;
        _life -= (float)delta;
        if (_life <= 0f || !GodotObject.IsInstanceValid(_target) || !_target.IsAlive)
        { QueueFree(); return; }
        UpdateHeading();
        float radius = _target.Definition.RadiusPixels + 3f;
        if (GlobalPosition.DistanceTo(_target.GlobalPosition) <= _speed * (float)delta + radius)
        {
            _impacted = true;
            _target.ApplyDamage(_damage);
            // A lethal impact can invoke callbacks that remove the native target immediately.
            if (_effect.HasValue && GodotObject.IsInstanceValid(_target) && _target.IsAlive)
                _target.ApplyStatus(_effect.Value);
            QueueFree(); return;
        }
        GlobalPosition = GlobalPosition.MoveToward(_target.GlobalPosition, _speed * (float)delta);
    }

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
