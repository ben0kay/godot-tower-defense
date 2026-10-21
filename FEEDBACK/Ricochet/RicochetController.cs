// One world-owned cosmetic buffer and renderer. No collision, enemy references, timers or per-round nodes.
using Godot;
public partial class RicochetController : Node2D
{
    #region State
    [Export] public RicochetStyle Style { get; set; }
    [Export] public FeedbackMasterConfig Master { get; set; }
    private struct Round
    {
        public Vector2 Position, Direction;
        public float Age, Life, Speed, Deceleration, Turn, Length, Opacity;
        public ProjectileVisualSnapshot Visual;
    }
    private readonly Round[] _rounds = new Round[512];
    private readonly RandomNumberGenerator _random = new();
    public int ActiveCount { get; private set; }
    public ulong SkippedCapacity { get; private set; }
    public bool Available => Master?.ShieldRicochetsEnabled == true && Style?.Enabled == true && IsInsideTree() && !IsQueuedForDeletion();
    #endregion
    #region Lifecycle and Spawn
    // =========================================================
    // Resources are loaded once by the shared World scene; the empty controller sleeps.
    public override void _Ready() { _random.Randomize(); SetPhysicsProcess(false); }
    // =========================================================
    // Accept only value snapshots after confirmed shield absorption; overload never queues future effects.
    public bool Spawn(Vector2 position, Vector2 normal, Vector2 direction, float incomingSpeed, ProjectileVisualSnapshot visual)
    {
        if (!Available || GetTree().Paused || !visual.Enabled || !position.IsFinite() || !normal.IsFinite() || !direction.IsFinite() ||
            direction.LengthSquared() < .0001f || !float.IsFinite(incomingSpeed) || incomingSpeed <= 0) return false;
        if (ActiveCount >= Mathf.Clamp(Style.MaxActiveRicochets, 1, _rounds.Length)) { SkippedCapacity++; return false; }
        if (_random.Randf() * 100 >= RicochetStyle.Safe(Style.ChancePercent, 35, 0, 100)) return false;
        direction = direction.Normalized(); normal = normal.Normalized();
        Vector2 jittered = direction.Rotated(Mathf.DegToRad(_random.RandfRange(-1, 1) * RicochetStyle.Safe(Style.AngleJitterDegrees, 2, 0, 3)));
        if (jittered.Dot(normal) > 0) direction = jittered;
        float angleToNormal = direction.AngleTo(normal);
        _rounds[ActiveCount++] = new Round { Position = position + normal * 1.5f, Direction = direction,
            Life = RicochetStyle.Safe(Style.LifetimeSeconds, .55f, .05f, 2),
            Speed = Mathf.Min(incomingSpeed, 100000) * RicochetStyle.Safe(Style.SpeedRetention, .45f, 0, 1),
            Deceleration = RicochetStyle.Safe(Style.Deceleration, .85f, 0, 1),
            Turn = Mathf.Clamp(angleToNormal, -Mathf.DegToRad(RicochetStyle.Safe(Style.CurveDegrees, 12, 0, 20)),
                Mathf.DegToRad(RicochetStyle.Safe(Style.CurveDegrees, 12, 0, 20))) * _random.Randf() * (1 - direction.Dot(normal)),
            Length = RicochetStyle.Safe(Style.LengthMultiplier, .8f, .1f, 2), Opacity = RicochetStyle.Safe(Style.Opacity, .65f, 0, 1), Visual = visual };
        SetPhysicsProcess(true); QueueRedraw(); return true;
    }
    // =========================================================
    // Pausing freezes physics; unloading releases textures and all slots immediately.
    public void Clear() { System.Array.Clear(_rounds); ActiveCount = 0; SetPhysicsProcess(false); QueueRedraw(); }
    // =========================================================
    // No static world reference survives unloading or retry.
    public override void _ExitTree() => Clear();
    #endregion
    #region Motion and Drawing
    // =========================================================
    // Midpoint integration is bounded by the fixed cap; expired slots compact without allocations.
    public override void _PhysicsProcess(double delta)
    {
        using var pf2 = SubsystemProfiler.Measure(this, ProfileCategory.Projectiles);
        if (!Available) { Clear(); return; }
        if (GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        for (int i = ActiveCount - 1; i >= 0; i--)
        {
            ref Round round = ref _rounds[i];
            float dt = Mathf.Min((float)delta, round.Life - round.Age);
            float progress = (round.Age + dt * .5f) / round.Life;
            float decay = 1 - round.Deceleration * (2 * progress - progress * progress);
            round.Position += round.Direction.Rotated(round.Turn * dt / round.Life * .5f) * round.Speed * decay * dt;
            round.Direction = round.Direction.Rotated(round.Turn * dt / round.Life); round.Age += dt;
            if (round.Age >= round.Life) { _rounds[i] = _rounds[--ActiveCount]; _rounds[ActiveCount] = default; }
        }
        SetPhysicsProcess(ActiveCount > 0); QueueRedraw();
    }
    // =========================================================
    // One CanvasItem draws thin source-colored streaks in world space, with no lights or particles.
    public override void _Draw()
    {
        if (!Available) return;
        Transform2D local = GetGlobalTransform().AffineInverse();
        for (int i = 0; i < ActiveCount; i++)
        {
            ref Round r = ref _rounds[i]; float fade = 1 - r.Age / r.Life;
            Vector2 head = local * r.Position, tail = local * (r.Position - r.Direction * r.Visual.Length * r.Length * (.25f + .75f * fade));
            Color body = r.Visual.Body, core = r.Visual.Core; body.A *= fade * r.Opacity; core.A *= fade * r.Opacity;
            DrawLine(tail, head, body, r.Visual.Width * 1.6f, true);
            DrawLine(tail.Lerp(head, .25f), head, core, r.Visual.Width, true);
        }
    }
    #endregion
}
