// Cached four-vertex cyan projection. No materials, particles, timers, collisions or gameplay state.
using Godot;
public partial class ConstructionBeam : Node2D
{
    #region Geometry
    private readonly Vector2[] _cone = new Vector2[4];
    private readonly Color[] _colors = new Color[4];
    private Vector2 _target;
    private float _width, _amount;
    private Color _tint;
    // =========================================================
    // One retained polygon per pooled drone; the tower center stays fixed as its nozzle hovers.
    public void Present(Vector2 target, float halfWidth, float strength, Color color, float opacity)
    {
        _target = ToLocal(target); _width = halfWidth; _amount = strength; _tint = color;
        Visible = strength > 0;
        _cone[0] = new(-3, 0); _cone[1] = new(3, 0);
        _cone[2] = _target + Vector2.Right * halfWidth; _cone[3] = _target - Vector2.Right * halfWidth;
        _colors[0] = _colors[1] = new(color.R, color.G, color.B, opacity * strength);
        _colors[2] = _colors[3] = new(color.R, color.G, color.B, opacity * strength * .15f);
        if (Visible) QueueRedraw();
    }
    // =========================================================
    // Translucent fill leaves stage art legible; thin edges/nozzle and three scan lines sell a holographic cone.
    public override void _Draw()
    {
        if (_amount <= 0) return;
        DrawPolygon(_cone, _colors);
        Color edge = new(_tint.R, _tint.G, _tint.B, .55f * _amount);
        DrawLine(_cone[0], _cone[3], edge, 1, true); DrawLine(_cone[1], _cone[2], edge, 1, true);
        for (int i = 1; i <= 3; i++)
        {
            float t = i / 4f; Color scan = edge; scan.A *= .35f;
            DrawLine(_cone[0].Lerp(_cone[3], t), _cone[1].Lerp(_cone[2], t), scan, 1, true);
        }
        DrawLine(_target - Vector2.Right * _width, _target + Vector2.Right * _width, edge, 1, true);
        DrawCircle(Vector2.Zero, 3, new Color(.7f, 1, 1, .9f * _amount));
    }
    #endregion
}
