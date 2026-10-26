// CD3 cached static cone/contact/nozzle, with only scan bars redrawn at a capped cosmetic rate.
// The central construction-drone manager supplies positions and age; no per-beam ticking or shaders.
using Godot;

public partial class ConstructionBeam : Node2D
{
    #region Cached Visual Layers
    private const float ScanRefreshSeconds = 1f / 24f;
    private ConstructionBeamCone _cone;
    private ConstructionBeamGlow _ground, _nozzle;
    private Vector2 _target;
    private float _width, _strength, _time, _lastScanTime = -1, _lastScanStrength = -1;
    private float _scanCycles, _pulseHz, _pulseDepth;
    private int _scanBands = -1;
    private Color _tint;
    public float AnimationSeconds => _time;
    public int ConeGeometryRefreshes => _cone?.GeometryRefreshes ?? 0;
    public int ScanRedrawRequests { get; private set; }

    // =========================================================
    // Look up the three static cached canvases once. They do not run a process loop.
    public override void _Ready()
    {
        _cone = GetNode<ConstructionBeamCone>("Cone");
        _ground = GetNode<ConstructionBeamGlow>("GroundContact");
        _nozzle = GetNode<ConstructionBeamGlow>("NozzleGlow");
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Cone geometry is authored in unit-height space: changing its y-axis moves the beam's endpoint
    // to the fixed tower while keeping its x-axis/width in world pixels. No QueueRedraw on hover.
    public void Present(Vector2 target, float halfWidth, float strength, ConstructionDroneMasterConfig config, float beamAge)
    {
        bool visible = config.ShowVisuals && strength > 0;
        if (Visible != visible) Visible = visible;
        if (!visible) { _lastScanTime = _lastScanStrength = -1; return; }

        _target = ToLocal(target);
        _width = Mathf.Max(1f, halfWidth);
        _strength = Mathf.Clamp(strength, 0, 1);
        _time = float.IsFinite(beamAge) ? Mathf.Max(0, beamAge) : 0;
        _tint = config.BeamColor;
        float opacity = ConstructionDroneMasterConfig.Safe(config.BeamOpacity, .26f, .05f, .5f);
        int bands = Mathf.Clamp(config.BeamScanBands, 0, 8);
        bool scanStyleChanged = bands != _scanBands;
        _scanBands = bands;
        _scanCycles = ConstructionDroneMasterConfig.Safe(config.BeamScanCyclesPerSecond, 1.5f, 0, 6);
        _pulseHz = ConstructionDroneMasterConfig.Safe(config.BeamPulseHz, 2, 0, 6);
        _pulseDepth = ConstructionDroneMasterConfig.Safe(config.BeamPulseDepth, .13f, 0, .35f);

        _cone.Configure(_width, _tint, opacity);
        _ground.Configure(_width, _tint);
        _nozzle.Configure(_width, _tint);

        // A sheared affine transform connects the retained unit-height cone to the moving nozzle/site.
        Transform2D pose = new(Vector2.Right, _target, Vector2.Zero);
        if (_cone.Transform != pose) _cone.Transform = pose;
        if (_ground.Position != _target) _ground.Position = _target;

        // During deployment only, alpha fades the cached layers; normal beam strength is constant.
        Color alpha = new(1, 1, 1, _strength);
        if (_cone.Modulate != alpha) _cone.Modulate = alpha;
        if (_ground.Modulate != alpha) _ground.Modulate = alpha;
        if (_nozzle.Modulate != alpha) _nozzle.Modulate = alpha;

        // Only the thin scan bands use _Draw(). At most 24 Hz; don't refresh a static cone.
        bool needsScan = _scanBands > 0;
        bool initial = _lastScanTime < 0 || _time < _lastScanTime;
        if (scanStyleChanged || initial || (needsScan && (_time - _lastScanTime >= ScanRefreshSeconds ||
            Mathf.Abs(_strength - _lastScanStrength) >= .12f)))
        {
            _lastScanTime = _time; _lastScanStrength = _strength;
            QueueRedraw(); ScanRedrawRequests++;
        }
    }
    #endregion

    #region Animated Scan Bands Only
    // =========================================================
    // The cone, edges, ground halo and nozzle glow are drawn on their own cached CanvasItems.
    // Only moving scan lines make this draw method run, at most once per 24-Hz visual interval.
    public override void _Draw()
    {
        if (!Visible || _scanBands <= 0 || _strength <= 0) return;
        float pulse = 1f + _pulseDepth * Mathf.Sin(_time * Mathf.Tau * _pulseHz);
        float strength = _strength * pulse;
        Vector2 sourceLeft = new(-3, 0), sourceRight = new(3, 0);
        Vector2 siteLeft = _target - Vector2.Right * _width, siteRight = _target + Vector2.Right * _width;
        for (int i = 0; i < _scanBands; i++)
        {
            float t = Mathf.PosMod(_time * _scanCycles + (float)i / _scanBands, 1f);
            float fade = Mathf.Sin(t * Mathf.Pi);
            Color soft = new(_tint.R, _tint.G, _tint.B, .10f * strength * fade);
            Color bright = new(_tint.R, _tint.G, _tint.B, .36f * strength * fade);
            Vector2 a = sourceLeft.Lerp(siteLeft, t), b = sourceRight.Lerp(siteRight, t);
            DrawLine(a, b, soft, 3.4f, true);
            DrawLine(a, b, bright, 1.1f, true);
        }
    }
    #endregion
}
