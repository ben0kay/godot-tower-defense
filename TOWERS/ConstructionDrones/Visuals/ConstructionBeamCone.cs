// One cached four-vertex construction-beam cone with a shared lightweight CanvasItem gradient.
// The shader feathers edges per pixel; only the parent transform moves during hovering, not its geometry.
using Godot;

public partial class ConstructionBeamCone : Node2D
{
    #region Cached Beam Geometry
    private readonly Vector2[] _vertices = new Vector2[4];
    private readonly Color[] _colors = new Color[4];
    private readonly Vector2[] _uvs = { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };
    private float _width = -1, _opacity = -1;
    private Color _tint;
    public int GeometryRefreshes { get; private set; }

    // =========================================================
    // The continuous full-length shape remains cached while the drone moves.
    // Vertex alpha keeps the beam gently tapered in brightness from nozzle to target.
    public void Configure(float halfWidth, Color tint, float opacity)
    {
        if (Mathf.IsEqualApprox(_width, halfWidth) && Mathf.IsEqualApprox(_opacity, opacity) && _tint == tint) return;
        _width = halfWidth; _tint = tint; _opacity = opacity;
        _vertices[0] = new(-5f, 0); _vertices[1] = new(5f, 0);
        _vertices[2] = new(_width + 6f, 1); _vertices[3] = new(-_width - 6f, 1);
        _colors[0] = _colors[1] = new(_tint.R, _tint.G, _tint.B, _opacity * 1.45f);
        _colors[2] = _colors[3] = new(_tint.R, _tint.G, _tint.B, _opacity * 1.12f);
        GeometryRefreshes++;
        QueueRedraw();
    }
    #endregion

    #region One Cached Draw Command
    // =========================================================
    // UVs feed the shared shader's smooth horizontal falloff. No overlapped hard-edged polygons,
    // animated shader uniforms, CPU recomposition, or per-tick draw commands.
    public override void _Draw()
    {
        if (_width <= 0) return;
        DrawPolygon(_vertices, _colors, _uvs);
    }
    #endregion
}
