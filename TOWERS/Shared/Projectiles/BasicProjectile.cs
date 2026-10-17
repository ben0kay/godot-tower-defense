// Minimal guided test bullet; swept target contact prevents tunneling at high bullet speeds.
using Godot;

public partial class BasicProjectile : Node2D
{
    private Enemy _target;
    private float _speed, _life = 5f;
    private DamageInfo _damage;
    private StatusEffectPayload? _effect;
    private bool _impacted;

    // =========================================================
    // Explicit initialization keeps this helper independent of any enemy/tower type.
    public void Initialize(Enemy target, float speed, float damage) => Initialize(target, speed, new DamageInfo(damage, DamageType.Kinetic));

    // =========================================================
    // Future projectile types can supply a typed base payload without duplicating defense math.
    public void Initialize(Enemy target, float speed, DamageInfo damage, StatusEffectPayload? effect = null)
    { _target = target; _speed = Mathf.Max(1f, speed); _damage = damage; _effect = effect; _impacted = false; }

    // =========================================================
    // Bullets expire cleanly if their target dies/arrives, avoiding invalid native references.
    public override void _PhysicsProcess(double delta)
    {
        if (_impacted || IsQueuedForDeletion()) return;
        _life -= (float)delta;
        if (_life <= 0f || !GodotObject.IsInstanceValid(_target) || !_target.IsAlive)
        { QueueFree(); return; }
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
    // Cheap visible placeholder; no texture, physics body or particle system required.
    public override void _Draw() => DrawCircle(Vector2.Zero, 3f, _effect.HasValue ? new Color(.35f, .9f, 1f) : new Color(1f, 0.85f, 0.35f));
}
