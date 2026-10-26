// CD3 construction hologram: retained cone with time-scrolled scan bars, subtle pulse and endpoint glows.
// The shared drone manager supplies visual time; no particle nodes, shader or per-drone process callbacks.
using Godot;

public partial class ConstructionBeam : Node2D
{
    #region Retained Presentation
    private readonly Vector2[] _cone = new Vector2[4];
    private readonly Color[] _colors = new Color[4];
    private Vector2 _target;
    private float _width, _strength, _time, _scanCycles, _pulseHz, _pulseDepth;
    private int _scanBands;
    private Color _tint;
    public float AnimationSeconds => _time;

    // =========================================================
    // All coordinates are local, including the nozzle offset on the shared drone scene.
    // Visual time is driven by the existing manager (paused work/flight never animates).
    public void Present(Vector2 target, float halfWidth, float strength, ConstructionDroneMasterConfig config, float beamAge)
    {
        _strength = Mathf.Clamp(strength, 0, 1);
        Visible = config.ShowVisuals && _strength > 0;
        if (!Visible) return;
        _target = ToLocal(target);
        _width = Mathf.Max(1f, halfWidth);
        _tint = config.BeamColor;
        _time = float.IsFinite(beamAge) ? Mathf.Max(0, beamAge) : 0;
        _scanBands = Mathf.Clamp(config.BeamScanBands, 0, 8);
        _scanCycles = ConstructionDroneMasterConfig.Safe(config.BeamScanCyclesPerSecond, 1.5f, 0, 6);
        _pulseHz = ConstructionDroneMasterConfig.Safe(config.BeamPulseHz, 2, 0, 6);
        _pulseDepth = ConstructionDroneMasterConfig.Safe(config.BeamPulseDepth, .13f, 0, .35f);
        float baseAlpha = ConstructionDroneMasterConfig.Safe(config.BeamOpacity, .26f, .05f, .5f);
        float pulse = 1f + _pulseDepth * Mathf.Sin(_time * Mathf.Tau * _pulseHz);
        _cone[0] = new(-3, 0); _cone[1] = new(3, 0);
        _cone[2] = _target + Vector2.Right * _width; _cone[3] = _target - Vector2.Right * _width;
        _colors[0] = _colors[1] = new(_tint.R, _tint.G, _tint.B, baseAlpha * _strength * pulse);
        _colors[2] = _colors[3] = new(_tint.R, _tint.G, _tint.B, baseAlpha * _strength * .15f * pulse);
        QueueRedraw();
    }
    #endregion

    #region One-Shot Drawing Per Active Manager Tick
    // =========================================================
    // Scan bands slide from drone toward site. Faint layered highlights enhance the existing
    // polygon without covering stage artwork or allocating nodes/arrays each frame.
    public override void _Draw()
    {
        if (_strength <= 0 || !Visible) return;
        DrawPolygon(_cone, _colors);
        float pulse = 1f + _pulseDepth * Mathf.Sin(_time * Mathf.Tau * _pulseHz);
        float strength = _strength * pulse;
        Color edge = new(_tint.R, _tint.G, _tint.B, .54f * strength);
        DrawLine(_cone[0], _cone[3], edge, 1.1f, true);
        DrawLine(_cone[1], _cone[2], edge, 1.1f, true);

        for (int i = 0; i < _scanBands; i++)
        {
            float t = Mathf.PosMod(_time * _scanCycles + (float)i / _scanBands, 1f);
            float fade = Mathf.Sin(t * Mathf.Pi);
            Color soft = new(_tint.R, _tint.G, _tint.B, .10f * strength * fade);
            Color bright = new(_tint.R, _tint.G, _tint.B, .36f * strength * fade);
            Vector2 a = _cone[0].Lerp(_cone[3], t), b = _cone[1].Lerp(_cone[2], t);
            DrawLine(a, b, soft, 3.4f, true);
            DrawLine(a, b, bright, 1.1f, true);
        }

        // Restrained cyan ground contact, scaled with footprint width (including factories).
        DrawCircle(_target, _width * .6f, new Color(_tint.R, _tint.G, _tint.B, .075f * strength));
        DrawArc(_target, _width * .72f, 0, Mathf.Tau, 32,
            new Color(_tint.R, _tint.G, _tint.B, .28f * strength), 1.3f, true);
        DrawLine(_target - Vector2.Right * _width, _target + Vector2.Right * _width, edge, 1, true);

        // Nozzle glow follows the hovering drone but never becomes a real Light2D.
        DrawCircle(Vector2.Zero, 7, new Color(_tint.R, _tint.G, _tint.B, .12f * strength));
        DrawCircle(Vector2.Zero, 3.6f, new Color(_tint.R, _tint.G, _tint.B, .38f * strength));
        DrawCircle(Vector2.Zero, 1.6f, new Color(.8f, 1, 1, .84f * strength));
    }
    #endregion
}
