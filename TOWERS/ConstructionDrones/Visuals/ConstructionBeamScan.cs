// Thin moving scan lines share the cached cone's exact transform; only scan geometry refreshes at 24 Hz.
// The construction manager provides time and pose; this node has no _Process/_PhysicsProcess callback.
using Godot;

public partial class ConstructionBeamScan : Node2D
{
    #region Scan State
    private const float RefreshSeconds = 1f / 24f;
    private float _width, _time, _strength, _lastTime = -1f, _lastStrength = -1f;
    private float _cycles, _pulseHz, _pulseDepth;
    private int _bands = -1;
    private Color _tint;
    public int RedrawRequests { get; private set; }

    // =========================================================
    // Move the entire cached scan canvas each hover tick; geometry updates only at the cosmetic rate.
    public void Present(float halfWidth, Color tint, float strength, float beamAge, ConstructionDroneMasterConfig config, Transform2D pose)
    {
        if (Transform != pose) Transform = pose;
        float time = float.IsFinite(beamAge) ? Mathf.Max(0f, beamAge) : 0f;
        int bands = Mathf.Clamp(config.BeamScanBands, 0, 8);
        float cycles = ConstructionDroneMasterConfig.Safe(config.BeamScanCyclesPerSecond, 1.5f, 0, 6);
        float hz = ConstructionDroneMasterConfig.Safe(config.BeamPulseHz, 2, 0, 6);
        float depth = ConstructionDroneMasterConfig.Safe(config.BeamPulseDepth, .13f, 0, .35f);
        bool styleChanged = bands != _bands || !Mathf.IsEqualApprox(_width, halfWidth) ||
            _tint != tint || !Mathf.IsEqualApprox(_cycles, cycles) ||
            !Mathf.IsEqualApprox(_pulseHz, hz) || !Mathf.IsEqualApprox(_pulseDepth, depth);
        _width = halfWidth; _tint = tint; _strength = Mathf.Clamp(strength, 0f, 1f); _time = time;
        _bands = bands; _cycles = cycles; _pulseHz = hz; _pulseDepth = depth;
        bool initial = _lastTime < 0 || _time < _lastTime;
        if (styleChanged || initial || (_bands > 0 &&
            (_time - _lastTime >= RefreshSeconds || Mathf.Abs(_strength - _lastStrength) >= .12f)))
        {
            _lastTime = _time; _lastStrength = _strength;
            QueueRedraw(); RedrawRequests++;
        }
    }

    // =========================================================
    // Next assignment starts a fresh scan without running any timer while pooled/hidden.
    public void Reset() { _lastTime = _lastStrength = -1; }
    #endregion

    #region Animated Scan Lines
    // =========================================================
    // The cone and scans both use normalized y=0..1. Inset soft strokes avoid visually cutting the cone.
    public override void _Draw()
    {
        if (_bands <= 0 || _strength <= 0) return;
        float pulse = 1f + _pulseDepth * Mathf.Sin(_time * Mathf.Tau * _pulseHz);
        for (int i = 0; i < _bands; i++)
        {
            float t = Mathf.PosMod(_time * _cycles + (float)i / _bands, 1f);
            float fade = Mathf.Sin(t * Mathf.Pi);
            float width = Mathf.Lerp(3f, _width, t) * .83f;
            Vector2 a = new(-width, t), b = new(width, t);
            Color soft = new(_tint.R, _tint.G, _tint.B, .08f * _strength * pulse * fade);
            Color bright = new(_tint.R, _tint.G, _tint.B, .26f * _strength * pulse * fade);
            DrawLine(a, b, soft, 2.8f, true);
            DrawLine(a, b, bright, .9f, true);
        }
    }
    #endregion
}
