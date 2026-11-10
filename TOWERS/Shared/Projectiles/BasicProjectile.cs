// Standalone and overflow scene adapter for the same friendly flight used by the shared buffer.
using Godot;

public partial class BasicProjectile : Node2D
{
    private RicochetController _ricochets;
    private HullHitSparkController _hullSparks;
    private FriendlyProjectileFlight _flight;
    private readonly FriendlyProjectileDrawing _drawing = new();
    private bool _launched, _finished, _advancing;

    public ProjectileVisualSnapshot? VisualSnapshot => _flight.Visual;
    public Vector2 TravelDirection => _flight.Direction;
    public ProjectileHandlingStats Handling => _flight.Handling;
    internal StatusEffectPayload? EffectSnapshot => _flight.Effect;

    // =========================================================
    // Retain the existing node API for standalone use and as a safe overflow path for funded rounds.
    public void Initialize(Enemy target, float speed, float damage) => Initialize(target, speed, new DamageInfo(damage, DamageType.Kinetic));

    public void Initialize(Enemy target, float speed, DamageInfo damage, StatusEffectPayload? effect = null,
        ProjectileVisualStyle visual = null, ProjectileHandlingStats? handling = null, bool shieldRicochet = false,
        int hullHitSparkCount = 0)
    {
        _flight.ReleaseLight();
        _flight = new(target, speed, damage, effect, visual, handling, shieldRicochet, hullHitSparkCount);
        _launched = _finished = false;
        ProjectileVisualBounds.SetCachedBounds(this,
            ProjectileVisualBounds.ForTracer(Vector2.Zero, Vector2.Right, _flight.Visual), _flight.Visual.HasValue ? 20f : 0f);
        QueueRedraw();
    }

    // =========================================================
    // Compatibility fixtures may omit forward and aim at the captured initial target point.
    public void LaunchFrom(Vector2 worldPosition, Vector2? forward = null, float? spreadSample = null)
    {
        if (_launched || _finished) return;
        if (!worldPosition.IsFinite()) { Finish(); return; }
        _ricochets = GetNodeOrNull<RicochetController>("../../Effects/Ricochets");
        _hullSparks = GetNodeOrNull<HullHitSparkController>("../../Effects/HullHitSparks");
        _flight.Launch(worldPosition, forward, spreadSample, GetNodeOrNull<WorldLightingController>("../../Lighting"));
        GlobalPosition = worldPosition;
        GlobalRotation = _flight.Direction.Angle();
        _launched = true;
    }

    // =========================================================
    // One fallback node drives the shared flight state using the normal Godot physics lifecycle.
    public override void _PhysicsProcess(double delta)
    {
        using var profile = SubsystemProfiler.Measure(this, ProfileCategory.Projectiles);
        if (_finished || _advancing || IsQueuedForDeletion() || !IsInsideTree() || GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        if (!_flight.RegistryIsLive) { Finish(); return; }
        if (!_launched) LaunchFrom(GlobalPosition);
        if (_finished) return;
        _flight.Position = GlobalPosition;
        _advancing = true;
        bool live;
        try { live = _flight.Advance(delta, _ricochets, _hullSparks); }
        catch
        {
            if (GodotObject.IsInstanceValid(this) && IsInsideTree()) Finish();
            throw;
        }
        finally { _advancing = false; }
        if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
        GlobalPosition = _flight.Position;
        GlobalRotation = _flight.Direction.Angle();
        if (!live) Finish();
    }

    private void Finish()
    {
        _finished = true;
        _flight.ReleaseLight();
        QueueFree();
    }

    public override void _ExitTree()
    {
        _flight.ReleaseLight();
        _flight = default;
        _finished = true;
        _ricochets = null;
        _hullSparks = null;
    }

    public override void _Draw() => _drawing.Draw(this, _flight, Vector2.Zero, Vector2.Right, Transform2D.Identity);
}
