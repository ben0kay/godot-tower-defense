// Straight-by-default splash delivery: one sampled destination, original flight distance, optional explicit guided tracking.
using Godot;
using System;

public partial class SplashProjectile : Node2D
{
    #region Splash Delivery
    private Enemy _target;
    private EnemyRegistry _registry;
    private Node2D _effects;
    private Vector2 _impact;
    private DamageInfo _hit;
    private AreaBlastStats _blast;
    private ShockwaveStyle _style;
    private float _speed;
    private double _life;
    private ProjectileHandlingStats _handling;
    private bool _launched;
    public Vector2 ImpactPoint => _impact;
    public ProjectileHandlingStats Handling => _handling;
    private bool _initialized, _finished, _trackTarget = true;
    public Color ProjectileColor { get; set; } = new(1f, 0.55f, 0.15f);
    public bool HasDetonated { get; private set; }
    public int AppliedHits { get; private set; }

    // =========================================================
    // Values are immutable copies; the projectile retains no tower or mutable config reference.
    public void Initialize(Enemy target, EnemyRegistry registry, Vector2 lastKnownImpact, float speed,
        DamageInfo hit, AreaBlastStats blast, Node2D effects = null, float lifetime = 5f, ShockwaveStyle style = null, bool trackTarget = false, ProjectileHandlingStats? handling = null)
    {
        if (_initialized) throw new InvalidOperationException("Projectile is already initialized.");
        AreaDamage.Validate(blast.RadiusPixels, blast.EdgeDamageFraction);
        if (!GodotObject.IsInstanceValid(registry)) throw new ArgumentNullException(nameof(registry));
        if (!float.IsFinite(speed) || speed <= 0 || !float.IsFinite(lifetime) || lifetime <= 0
            || !float.IsFinite(lastKnownImpact.X) || !float.IsFinite(lastKnownImpact.Y))
            throw new ArgumentOutOfRangeException(nameof(speed));
        _registry = registry; _effects = effects; _impact = lastKnownImpact; _speed = speed; _life = lifetime;
        _hit = hit; _blast = blast; _style = style == null ? null : (ShockwaveStyle)style.Duplicate(true); _initialized = true;
        _handling = ProjectileHandlingStats.Sanitize(handling ?? new(0, trackTarget ? ProjectileGuidance.Homing : ProjectileGuidance.Straight));
        _trackTarget = _handling.Guidance == ProjectileGuidance.Homing;
        if (_trackTarget && GodotObject.IsInstanceValid(target) && target.IsAlive && target.IsInsideTree() && !target.IsQueuedForDeletion())
        { _target = target; _target.Removed += OnTargetRemoved; _target.TreeExiting += OnTargetExiting; }
    }

    // =========================================================
    // Fixed-distance splash: rotate the initial target-point vector once; zero spread preserves the exact old destination.
    public void LaunchFrom(Vector2 position, float? spreadSample = null)
    {
        if (_launched) return;
        if (!position.IsFinite()) { Finish(); return; }
        GlobalPosition = position;
        Vector2 vector = _impact - position;
        Vector2 direction = ProjectileLaunch.Direction(vector, _handling.SpreadDegrees, spreadSample);
        if (!_trackTarget && _handling.SpreadDegrees > 0) _impact = position + direction * vector.Length();
        GlobalRotation = direction.Angle(); _launched = true;
    }
    // =========================================================
    // No registry queries during travel. Contact is the exact destination point, not an extra direct hit.
    public override void _PhysicsProcess(double delta)
    {
        using var pf2 = SubsystemProfiler.Measure(this, ProfileCategory.Projectiles);
        if (!_initialized || _finished || !IsInsideTree() || IsQueuedForDeletion() || GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        if (!_launched) LaunchFrom(GlobalPosition);
        if (_finished) return;
        if (_life <= 0 || !GodotObject.IsInstanceValid(_registry) || !_registry.IsInsideTree() || _registry.IsQueuedForDeletion())
        { Finish(); return; }
        if (_trackTarget) CaptureTargetPosition();
        double seconds = Math.Min(delta, _life);
        float step = (float)Math.Min(float.MaxValue, _speed * seconds);
        if (GlobalPosition.DistanceTo(_impact) <= step)
        {
            GlobalPosition = _impact; _finished = true; HasDetonated = true; DetachTarget();
            try
            {
                AppliedHits = AreaDamage.ApplyInstant(_impact, _blast.RadiusPixels, _hit, _blast.EdgeDamageFraction, _registry);
                ShockwaveEffect.Spawn(_effects, _impact, _style, _blast.RadiusPixels);
            }
            finally { QueueFree(); }
            return;
        }
        Vector2 direction = _impact - GlobalPosition;
        if (direction.LengthSquared() > .0001f) GlobalRotation = direction.Angle();
        GlobalPosition = GlobalPosition.MoveToward(_impact, step); _life -= seconds;
        if (_life <= 0) Finish();
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
    { DrawCircle(Vector2.Zero, 5f, ProjectileColor); DrawCircle(Vector2.Zero, 2f, Colors.White); }
    #endregion
}
