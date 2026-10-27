// Shared construction beam: cached continuous projection, cached contact glows and aligned animated scan lines.
// The active-only construction drone manager supplies pose/time; no per-beam process, timers or shaders.
using Godot;

public partial class ConstructionBeam : Node2D
{
    #region Cached Visual Layers
    private ConstructionBeamCone _cone;
    private ConstructionBeamScan _scan;
    private ConstructionBeamGlow _ground, _nozzle;
    private float _time;
    public float AnimationSeconds => _time;
    public int ConeGeometryRefreshes => _cone?.GeometryRefreshes ?? 0;
    public int ScanRedrawRequests => _scan?.RedrawRequests ?? 0;

    // =========================================================
    // Static and dynamic layers are siblings so their transforms agree throughout hovering.
    public override void _Ready()
    {
        _cone = GetNode<ConstructionBeamCone>("Cone");
        _scan = GetNode<ConstructionBeamScan>("ScanLines");
        _ground = GetNode<ConstructionBeamGlow>("GroundContact");
        _nozzle = GetNode<ConstructionBeamGlow>("NozzleGlow");
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Map normalized y=1 onto the target via a cheap affine transform. No beam re-tessellation
    // when the drone hovers, and scan lines follow the identical pose every physics tick.
    public void Present(Vector2 target, float halfWidth, float strength, ConstructionDroneMasterConfig config, float beamAge)
    {
        bool visible = config.ShowVisuals && strength > 0;
        if (Visible != visible) Visible = visible;
        if (!visible) { _scan.Reset(); return; }

        Vector2 site = ToLocal(target);
        float width = Mathf.Max(1f, halfWidth), amount = Mathf.Clamp(strength, 0f, 1f);
        _time = float.IsFinite(beamAge) ? Mathf.Max(0f, beamAge) : 0f;
        Color tint = config.BeamColor;
        float opacity = ConstructionDroneMasterConfig.Safe(config.BeamOpacity, .26f, .05f, .5f);
        _cone.Configure(width, tint, opacity);
        _ground.Configure(width, tint);
        _nozzle.Configure(width, tint);

        Transform2D pose = new(Vector2.Right, site, Vector2.Zero);
        if (_cone.Transform != pose) _cone.Transform = pose;
        _scan.Present(width, tint, amount, _time, config, pose);
        if (_ground.Position != site) _ground.Position = site;

        // Only the short deployment animates cached static alpha, not the cone's geometry.
        Color alpha = new(1, 1, 1, amount);
        if (_cone.Modulate != alpha) _cone.Modulate = alpha;
        if (_ground.Modulate != alpha) _ground.Modulate = alpha;
        if (_nozzle.Modulate != alpha) _nozzle.Modulate = alpha;
    }
    #endregion
}
