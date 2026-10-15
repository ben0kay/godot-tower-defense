// Short cosmetic impact flash; no enemies, hit payloads, registry queries or damage timers.
using Godot;

public partial class BlastFlash : Node2D
{
    private float _radius, _elapsed;
    private const float Duration = 0.22f;

    // =========================================================
    // Optional world Effects child; missing presentation never prevents combat resolution.
    public static void Spawn(Node2D effects, Vector2 position, float radius)
    {
        if (!GodotObject.IsInstanceValid(effects) || !effects.IsInsideTree() || effects.IsQueuedForDeletion()) return;
        BlastFlash flash = new() { _radius = radius };
        effects.AddChild(flash); flash.GlobalPosition = position;
    }

    // =========================================================
    // Animation and lifetime are purely cosmetic; damage has already completed at impact.
    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_elapsed >= Duration) { QueueFree(); return; }
        QueueRedraw();
    }

    // =========================================================
    // Expanding/fading ring is a visual only, never an expanding damage wavefront.
    public override void _Draw()
    {
        float progress = Mathf.Clamp(_elapsed / Duration, 0, 1), radius = Mathf.Max(2, _radius);
        DrawCircle(Vector2.Zero, radius, new Color(1, 0.55f, 0.12f, (1 - progress) * 0.13f));
        DrawArc(Vector2.Zero, radius * (0.55f + 0.45f * progress), 0, Mathf.Tau, 48,
            new Color(1, 0.7f, 0.25f, 1 - progress), 2f);
    }
}
