// Shared world buffer and drawing surface for standard friendly rounds. Splash shots remain independent children.
using Godot;
using System;

public partial class FriendlyProjectileSystem : Node2D
{
    [Export] public bool UseBufferedTracers { get; set; } = true;
    [Export(PropertyHint.Range, "1,16384,1")] public int MaximumBufferedRounds { get; set; } = 4096;
    public int ActiveCount => _count;

    private FriendlyProjectileFlight[] _shots = Array.Empty<FriendlyProjectileFlight>();
    private readonly FriendlyProjectileDrawing _drawing = new();
    private RicochetController _ricochets;
    private HullHitSparkController _hullSparks;
    private WorldLightingController _lighting;
    private int _count, _revision;
    private bool _advancing;

    public override void _Ready()
    {
        SetPhysicsProcess(false);
        _ricochets = GetNodeOrNull<RicochetController>("../Effects/Ricochets");
        _hullSparks = GetNodeOrNull<HullHitSparkController>("../Effects/HullHitSparks");
        _lighting = GetNodeOrNull<WorldLightingController>("../Lighting");
    }

    // =========================================================
    // Full buffers and reentrant launches fall back to the standalone adapter without discarding funded rounds.
    internal bool TryLaunch(Enemy target, float speed, DamageInfo damage, Vector2 origin, Vector2 forward,
        StatusEffectPayload? effect, ProjectileVisualStyle visual, ProjectileHandlingStats? handling,
        bool shieldRicochet, int hullHitSparkCount)
    {
        if (!UseBufferedTracers || _advancing || !IsInsideTree() || IsQueuedForDeletion() || !origin.IsFinite()) return false;
        if (_shots.Length == 0) _shots = new FriendlyProjectileFlight[Math.Clamp(MaximumBufferedRounds, 1, 16384)];
        if (_count == _shots.Length) return false;
        ref FriendlyProjectileFlight shot = ref _shots[_count];
        shot = new(target, speed, damage, effect, visual, handling, shieldRicochet, hullHitSparkCount);
        shot.Launch(origin, forward, null, _lighting);
        _count++;
        SetPhysicsProcess(true);
        QueueRedraw();
        return true;
    }

    // =========================================================
    // Stable compaction preserves launch order for hits and status effects within a tick.
    public override void _PhysicsProcess(double delta)
    {
        if (_advancing || _count == 0 || !IsInsideTree() || IsQueuedForDeletion() ||
            GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        using var profile = SubsystemProfiler.Measure(this, ProfileCategory.Projectiles);
        _advancing = true;
        int revision = _revision, write = 0;
        try
        {
            for (int read = 0; read < _count; read++)
            {
                FriendlyProjectileFlight shot = _shots[read];
                bool live;
                try { live = shot.Advance(delta, _ricochets, _hullSparks); }
                catch
                {
                    // A failed hit callback must not replay the round on a later tick.
                    shot.ReleaseLight();
                    if (revision == _revision) _shots[read] = default;
                    throw;
                }
                if (revision != _revision || !GodotObject.IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion())
                {
                    shot.ReleaseLight();
                    return;
                }
                if (live)
                {
                    _shots[write] = shot;
                    if (write++ != read) _shots[read] = default;
                }
                else
                {
                    shot.ReleaseLight();
                    _shots[read] = default;
                }
            }
            _count = write;
        }
        finally { _advancing = false; }
        if (_count == 0) SetPhysicsProcess(false);
        QueueRedraw();
    }

    // =========================================================
    // A single draw callback reuses tracer scratch arrays across every buffered round.
    public override void _Draw()
    {
        if (_count == 0) return;
        Transform2D transform = GlobalTransform;
        if (!transform.X.IsFinite() || !transform.Y.IsFinite() || !transform.Origin.IsFinite() || transform.Determinant() == 0) return;
        Transform2D worldToLocal = transform.AffineInverse();
        Rect2 visibleWorld = VisibleWorldBounds();
        DrawSetTransformMatrix(worldToLocal);
        for (int i = 0; i < _count; i++)
        {
            ref readonly FriendlyProjectileFlight shot = ref _shots[i];
            if (visibleWorld.HasPoint(shot.Position))
                _drawing.Draw(this, shot, shot.Position, shot.Direction, worldToLocal);
        }
        DrawSetTransformMatrix(Transform2D.Identity);
    }

    // =========================================================
    // Pad by the maximum tracer extent so the tail remains visible when its head leaves the screen.
    private Rect2 VisibleWorldBounds()
    {
        Transform2D screenToWorld = GetCanvasTransform().AffineInverse();
        Rect2 viewport = GetViewportRect();
        Vector2 halfSize = viewport.Size * .5f;
        Vector2 centre = screenToWorld * (viewport.Position + halfSize);
        Vector2 extent = screenToWorld.X.Abs() * halfSize.X + screenToWorld.Y.Abs() * halfSize.Y;
        return new Rect2(centre - extent, 2 * extent).Grow(160);
    }

    // =========================================================
    // Explicit fixture/reset cleanup and scene exit share the same light retirement path.
    internal void Clear()
    {
        _revision++;
        for (int i = 0; i < _count; i++) _shots[i].ReleaseLight();
        Array.Clear(_shots, 0, _count);
        _count = 0;
        SetPhysicsProcess(false);
        QueueRedraw();
    }

    public override void _ExitTree()
    {
        Clear();
        _ricochets = null;
        _hullSparks = null;
        _lighting = null;
    }
}
