// One seamless, softly feathered projection cone. Canvas commands are cached between style/footprint changes.
// The parent changes its transform while a drone hovers; the cone never rebuilds because an endpoint moves.
using Godot;

public partial class ConstructionBeamCone : Node2D
{
    #region Cached Beam Shape
    private readonly Vector2[] _fringe = new Vector2[4], _body = new Vector2[4], _core = new Vector2[4];
    private readonly Color[] _fringeColors = new Color[4], _bodyColors = new Color[4], _coreColors = new Color[4];
    private float _width = -1, _opacity = -1;
    private Color _tint;
    public int GeometryRefreshes { get; private set; }

    // =========================================================
    // Unit-height geometry uses exactly the same endpoint basis as animated scan lines.
    // The body stays legible at the ground instead of fading to almost zero halfway through.
    public void Configure(float halfWidth, Color tint, float opacity)
    {
        if (Mathf.IsEqualApprox(_width, halfWidth) && Mathf.IsEqualApprox(_opacity, opacity) && _tint == tint) return;
        _width = halfWidth; _tint = tint; _opacity = opacity;
        SetQuad(_fringe, 5f, _width + 6f);
        SetQuad(_body, 3f, _width);
        SetQuad(_core, 1.5f, Mathf.Max(3f, _width * .30f));
        SetColors(_fringeColors, .20f * opacity, .13f * opacity);
        SetColors(_bodyColors, .75f * opacity, .58f * opacity);
        SetColors(_coreColors, .65f * opacity, .43f * opacity);
        GeometryRefreshes++;
        QueueRedraw();
    }

    // =========================================================
    // A single continuous tapered quadrilateral at each opacity, with no stacked lengthwise segments.
    private static void SetQuad(Vector2[] points, float top, float bottom)
    {
        points[0] = new(-top, 0); points[1] = new(top, 0);
        points[2] = new(bottom, 1); points[3] = new(-bottom, 1);
    }

    // =========================================================
    // Independent vertex alpha creates a shallow vertical fade without a ground-level transparency gap.
    private void SetColors(Color[] colors, float top, float bottom)
    {
        colors[0] = colors[1] = new(_tint.R, _tint.G, _tint.B, top);
        colors[2] = colors[3] = new(_tint.R, _tint.G, _tint.B, bottom);
    }
    #endregion

    #region Cached Rendering
    // =========================================================
    // Wide feather, uninterrupted main fill and restrained inner energy channel; no hard neon wedge borders.
    public override void _Draw()
    {
        if (_width <= 0) return;
        DrawPolygon(_fringe, _fringeColors);
        DrawPolygon(_body, _bodyColors);
        DrawPolygon(_core, _coreColors);
        Color rim = new(_tint.R, _tint.G, _tint.B, .17f);
        DrawLine(_body[0], _body[3], rim, .9f, true);
        DrawLine(_body[1], _body[2], rim, .9f, true);
    }
    #endregion
}
