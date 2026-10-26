// Cached ground-contact or drone-nozzle artwork. Moving the contact uses Node2D.Position, not redraws.
// Both variants share the beam's color/strength; the manager remains their only update source.
using Godot;

public partial class ConstructionBeamGlow : Node2D
{
    [Export] public bool GroundContact { get; set; }
    private Color _tint;
    private float _width = -1;

    // =========================================================
    // Ground footprint changes only between builds; nozzle retains constant artwork.
    public void Configure(float halfWidth, Color tint)
    {
        if (Mathf.IsEqualApprox(_width, halfWidth) && _tint == tint) return;
        _width = halfWidth; _tint = tint;
        QueueRedraw();
    }

    // =========================================================
    // Draw once per style/size instead of repeating circles and 32-segment arcs every beam tick.
    public override void _Draw()
    {
        if (_width <= 0) return;
        if (GroundContact)
        {
            DrawCircle(Vector2.Zero, _width * .6f, new Color(_tint.R, _tint.G, _tint.B, .075f));
            DrawArc(Vector2.Zero, _width * .72f, 0, Mathf.Tau, 32,
                new Color(_tint.R, _tint.G, _tint.B, .28f), 1.3f, true);
            DrawLine(Vector2.Left * _width, Vector2.Right * _width,
                new Color(_tint.R, _tint.G, _tint.B, .54f), 1, true);
            return;
        }
        DrawCircle(Vector2.Zero, 7, new Color(_tint.R, _tint.G, _tint.B, .12f));
        DrawCircle(Vector2.Zero, 3.6f, new Color(_tint.R, _tint.G, _tint.B, .38f));
        DrawCircle(Vector2.Zero, 1.6f, new Color(.8f, 1, 1, .84f));
    }
}
