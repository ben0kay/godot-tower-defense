// Minimal guided test bullet; swept target contact prevents tunneling at high bullet speeds.
using Godot;

public partial class BasicProjectile : Node2D
{
    private Enemy _target;
    private float _speed, _damage, _life = 5f;

    // =========================================================
    // Explicit initialization keeps this helper independent of any enemy/tower type.
    public void Initialize(Enemy target, float speed, float damage)
    { _target = target; _speed = Mathf.Max(1f, speed); _damage = damage; }

    // =========================================================
    // Bullets expire cleanly if their target dies/arrives, avoiding invalid native references.
    public override void _PhysicsProcess(double delta)
    {
        _life -= (float)delta;
        if (_life <= 0f || !GodotObject.IsInstanceValid(_target) || !_target.IsAlive)
        { QueueFree(); return; }
        float radius = _target.Definition.RadiusPixels + 3f;
        if (GlobalPosition.DistanceTo(_target.GlobalPosition) <= _speed * (float)delta + radius)
        { _target.TakeDamage(_damage); QueueFree(); return; }
        GlobalPosition = GlobalPosition.MoveToward(_target.GlobalPosition, _speed * (float)delta);
    }

    // =========================================================
    // Cheap visible placeholder; no texture, physics body or particle system required.
    public override void _Draw() => DrawCircle(Vector2.Zero, 3f, new Color(1f, 0.85f, 0.35f));
}
