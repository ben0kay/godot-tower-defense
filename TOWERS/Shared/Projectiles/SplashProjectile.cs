// Guided impact delivery with launch-snapshotted payload; lost targets retain their final known point.
using Godot;
using System;

public partial class SplashProjectile : Node2D
{
    private Enemy _target;
    private EnemyRegistry _registry;
    private Node2D _effects;
    private Vector2 _impact;
    private DamageInfo _hit;
    private AreaBlastStats _blast;
    private float _speed, _life;
    private bool _initialized, _finished;
    public bool HasDetonated { get; private set; }
    public int AppliedHits { get; private set; }

    // =========================================================
    // Values are immutable copies; the projectile retains no tower or mutable config reference.
    public void Initialize(Enemy target, EnemyRegistry registry, Vector2 lastKnownImpact, float speed,
        DamageInfo hit, AreaBlastStats blast, Node2D effects = null, float lifetime = 5f)
    {
        if (_initialized) throw new InvalidOperationException("Projectile is already initialized.");
        AreaDamage.Validate(blast.RadiusPixels, blast.EdgeDamageFraction);
        if (!GodotObject.IsInstanceValid(registry)) throw new ArgumentNullException(nameof(registry));
        if (!float.IsFinite(speed) || speed <= 0 || !float.IsFinite(lifetime) || lifetime <= 0
            || !float.IsFinite(lastKnownImpact.X) || !float.IsFinite(lastKnownImpact.Y))
            throw new ArgumentOutOfRangeException(nameof(speed));
        _registry = registry; _effects = effects; _impact = lastKnownImpact; _speed = speed; _life = lifetime;
        _hit = hit; _blast = blast; _initialized = true;
        if (GodotObject.IsInstanceValid(target) && target.IsAlive && target.IsInsideTree() && !target.IsQueuedForDeletion())
        { _target = target; _target.Removed += OnTargetRemoved; _target.TreeExiting += OnTargetExiting; }
    }

    // =========================================================
    // No registry queries during travel. Contact is the exact destination point, not an extra direct hit.
    public override void _PhysicsProcess(double delta)
    {
        if (!_initialized || _finished || !IsInsideTree() || IsQueuedForDeletion() || !double.IsFinite(delta) || delta <= 0) return;
        _life -= (float)delta;
        if (_life <= 0 || !GodotObject.IsInstanceValid(_registry) || !_registry.IsInsideTree() || _registry.IsQueuedForDeletion())
        { Finish(); return; }
        CaptureTargetPosition();
        float step = (float)Math.Min(float.MaxValue, _speed * delta);
        if (GlobalPosition.DistanceTo(_impact) <= step)
        {
            GlobalPosition = _impact; _finished = true; HasDetonated = true; DetachTarget();
            try
            {
                AppliedHits = AreaDamage.ApplyInstant(_impact, _blast.RadiusPixels, _hit, _blast.EdgeDamageFraction, _registry);
                BlastFlash.Spawn(_effects, _impact, _blast.RadiusPixels);
            }
            finally { QueueFree(); }
            return;
        }
        GlobalPosition = GlobalPosition.MoveToward(_impact, step);
    }

    // =========================================================
    // Ignore nonfinite transforms and retain the last usable point even during target deletion.
    private void CaptureTargetPosition()
    {
        if (!GodotObject.IsInstanceValid(_target)) return;
        Vector2 position = _target.GlobalPosition;
        if (float.IsFinite(position.X) && float.IsFinite(position.Y)) _impact = position;
    }

    // =========================================================
    // Both normal death/arrival and external removal preserve the latest destination.
    private void OnTargetRemoved(Enemy enemy) { CaptureTargetPosition(); DetachTarget(); }
    // =========================================================
    // TreeExiting fires before the target's native position becomes inaccessible.
    private void OnTargetExiting() { CaptureTargetPosition(); DetachTarget(); }

    // =========================================================
    // Lifetime expiration removes delivery without creating an impact or damage.
    private void Finish() { _finished = true; DetachTarget(); QueueFree(); }

    // =========================================================
    // Release subscriptions on detonation, expiry and scene unload.
    private void DetachTarget()
    {
        if (GodotObject.IsInstanceValid(_target))
        { _target.Removed -= OnTargetRemoved; _target.TreeExiting -= OnTargetExiting; }
        _target = null;
    }

    // =========================================================
    // Unloaded projectiles cannot detonate later, even if manually ticked by a caller.
    public override void _ExitTree() { _finished = true; DetachTarget(); }

    // =========================================================
    // Lightweight native projectile artwork; presentation has no effect on blast selection.
    public override void _Draw()
    { DrawCircle(Vector2.Zero, 5f, new Color(1f, 0.55f, 0.15f)); DrawCircle(Vector2.Zero, 2f, Colors.White); }
}
