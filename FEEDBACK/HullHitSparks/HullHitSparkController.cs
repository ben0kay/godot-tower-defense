// HI1: one fixed-capacity world-space renderer for tiny hull-only kinetic hit sparks.
// No projectile/particle nodes, collision checks, allocations per hit, or activity while idle.
using Godot;

public partial class HullHitSparkController : Node2D
{
    #region Settings and Fixed Storage
    [Export] public HullHitSparkStyle Style { get; set; }
    [Export] public FeedbackMasterConfig Master { get; set; }

    private struct Fragment
    {
        public Vector2 Position, Velocity;
        public float Age, Lifetime, Length, Width;
        public Color Tint;
    }

    private readonly Fragment[] _fragments = new Fragment[512];
    private readonly RandomNumberGenerator _random = new();
    private static readonly Color HotWhite = new(1f, .99f, .85f);
    private static readonly Color Yellow = new(1f, .83f, .36f);
    private static readonly Color Orange = new(1f, .43f, .12f);

    public int ActiveCount { get; private set; }
    public ulong SkippedCapacity { get; private set; }
    public bool Available => Style?.Enabled == true && Master?.HullHitSparksEnabled == true &&
        IsInsideTree() && !IsQueuedForDeletion();
    #endregion

    #region Lifecycle and Impact Requests
    // =========================================================
    // Empty means no animation, physics or redraw callbacks.
    public override void _Ready()
    {
        _random.Randomize();
        SetProcess(false);
        SetPhysicsProcess(false);
    }

    // =========================================================
    // Called only after confirmed positive hull-health reduction by an opted-in kinetic weapon.
    // The capped compact array holds individual flecks; an overloaded volley silently drops cosmetics.
    public bool Spawn(Vector2 point, Vector2 incoming, int count)
    {
        if (!Available || GetTree().Paused || !point.IsFinite() || !incoming.IsFinite() ||
            incoming.LengthSquared() < .00001f || count <= 0) return false;

        int limit = Mathf.Clamp(Style.MaxActiveFragments, 1, _fragments.Length);
        int requested = Mathf.Clamp(count, 1, 8);
        if (ActiveCount + requested > limit) { SkippedCapacity++; return false; }

        float lowLife = Mathf.Clamp(Style.MinLifetime, .05f, .5f);
        float highLife = Mathf.Clamp(Style.MaxLifetime, lowLife, .5f);
        float lowSpeed = Mathf.Clamp(Style.MinSpeed, 10f, 250f);
        float highSpeed = Mathf.Clamp(Style.MaxSpeed, lowSpeed, 250f);
        float length = Mathf.Clamp(Style.FragmentLength, 1f, 12f);
        float width = Mathf.Clamp(Style.FragmentThickness, .5f, 3f);
        float opacity = Mathf.Clamp(Style.Opacity, 0f, 1f);
        float reflectedAngle = (-incoming).Angle();

        for (int i = 0; i < requested; i++)
        {
            float angle = reflectedAngle + _random.RandfRange(-1.25f, 1.25f);
            float speed = _random.RandfRange(lowSpeed, highSpeed);
            Color tint = i % 3 == 0 ? HotWhite : i % 3 == 1 ? Yellow : Orange;
            tint.A = opacity;
            _fragments[ActiveCount++] = new Fragment
            {
                Position = point, Velocity = Vector2.FromAngle(angle) * speed,
                Age = 0f, Lifetime = _random.RandfRange(lowLife, highLife),
                Length = length * _random.RandfRange(.65f, 1.15f), Width = width,
                Tint = tint
            };
        }

        SetPhysicsProcess(true);
        QueueRedraw();
        return true;
    }

    // =========================================================
    // Clear all owned effects at world unload without retaining enemies, projectiles or towers.
    public void Clear()
    {
        System.Array.Clear(_fragments);
        ActiveCount = 0;
        SetPhysicsProcess(false);
        QueueRedraw();
    }

    public override void _ExitTree() => Clear();
    #endregion

    #region Update and Batched Drawing
    // =========================================================
    // Move only occupied slots. Expired fragments swap with the last active slot; no allocations.
    public override void _PhysicsProcess(double delta)
    {
        using var profile = SubsystemProfiler.Measure(this, ProfileCategory.Projectiles);
        if (!Available) { Clear(); return; }
        if (GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        float dt = (float)Mathf.Min(delta, .25);
        for (int i = ActiveCount - 1; i >= 0; i--)
        {
            ref Fragment spark = ref _fragments[i];
            spark.Age += dt;
            if (spark.Age >= spark.Lifetime)
            {
                _fragments[i] = _fragments[--ActiveCount];
                _fragments[ActiveCount] = default;
                continue;
            }
            // Fast hot-metal flicker, slight deceleration; no gravity simulation.
            float fade = 1f - spark.Age / spark.Lifetime;
            spark.Position += spark.Velocity * (dt * (.45f + .55f * fade));
        }
        SetPhysicsProcess(ActiveCount > 0);
        QueueRedraw();
    }

    // =========================================================
    // One CanvasItem draws tiny fading line fragments in world space.
    public override void _Draw()
    {
        if (!Available) return;
        Transform2D local = GetGlobalTransform().AffineInverse();
        for (int i = 0; i < ActiveCount; i++)
        {
            ref Fragment spark = ref _fragments[i];
            float fade = 1f - spark.Age / spark.Lifetime;
            Vector2 head = local * spark.Position;
            Vector2 direction = spark.Velocity.Normalized();
            Vector2 tail = local * (spark.Position - direction * spark.Length * (.4f + .6f * fade));
            Color color = spark.Tint;
            color.A *= fade;
            DrawLine(tail, head, color, spark.Width, true);
        }
    }
    #endregion
}
