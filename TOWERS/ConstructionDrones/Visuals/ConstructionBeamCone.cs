// Static four-point builder cone, cached by CanvasItem; only the transform follows a hovering drone.
// Shape and edge commands are rebuilt on new beam footprint/style, never per manager physics tick.
using Godot;

public partial class ConstructionBeamCone : Node2D
{
    private readonly Vector2[] _vertices = new Vector2[4];
    private readonly Color[] _colors = new Color[4];
    private float _width = -1, _opacity = -1;
    private Color _tint;
    public int GeometryRefreshes { get; private set; }

    // =========================================================
    // Normalized endpoint y=1 is mapped onto the actual construction site with Transform2D.
    // Cached shape/edge commands remain untouched when the drone moves.
    public void Configure(float halfWidth, Color tint, float opacity)
    {
        if (Mathf.IsEqualApprox(_width, halfWidth) && Mathf.IsEqualApprox(_opacity, opacity) && _tint == tint) return;
        _width = halfWidth; _tint = tint; _opacity = opacity;
        _vertices[0] = new(-3, 0); _vertices[1] = new(3, 0);
        _vertices[2] = new(_width, 1); _vertices[3] = new(-_width, 1);
        _colors[0] = _colors[1] = new(tint.R, tint.G, tint.B, opacity);
        _colors[2] = _colors[3] = new(tint.R, tint.G, tint.B, opacity * .15f);
        GeometryRefreshes++;
        QueueRedraw();
    }

    // =========================================================
    // The renderer caches these commands between style/footprint changes.
    public override void _Draw()
    {
        if (_width <= 0) return;
        DrawPolygon(_vertices, _colors);
        Color edge = new(_tint.R, _tint.G, _tint.B, .54f);
        DrawLine(_vertices[0], _vertices[3], edge, 1.1f, true);
        DrawLine(_vertices[1], _vertices[2], edge, 1.1f, true);
    }
}
