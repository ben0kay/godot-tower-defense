// One pausable opening tween per forming portal; bounded GPU details sleep permanently after Open.
using Godot;
using System;
using System.Collections.Generic;

public partial class EnemyPortal
{
    public enum PortalState { Dormant, Forming, Open }
    public PortalState OpeningState { get; private set; } = PortalState.Dormant;
    public bool IsSpawnReady => IsSpawnActive && OpeningState == PortalState.Open;
    public event Action<EnemyPortal> SpawnReady;
    [ExportGroup("Materialization")]
    [Export] public PortalMaterializationStyle MaterializationStyle { get; set; }
    private Tween _openingTween;
    private Node2D _opening;
    private Sprite2D _openingArt;
    private ColorRect _vortex;
    private ShaderMaterial _vortexMaterial;
    private GpuParticles2D _inward, _ignition;
    private bool _detailHeld, _ignited;
    private static readonly HashSet<EnemyPortal> DetailedOpenings = new();
    private static ImageTexture _inwardPoints;

    // =========================================================
    // Setup never starts an animation; only configured deployment/activation may do that.
    private void SetupMaterialization()
    {
        MaterializationStyle ??= GD.Load<PortalMaterializationStyle>("res://WORLD/PORTALS/EnemyPortal/Visuals/DefaultPortalMaterializationStyle.tres") ?? new();
        _opening = GetNodeOrNull<Node2D>("Visuals/Materialization");
        _openingArt = GetNodeOrNull<Sprite2D>("Visuals/PortalArt");
        _vortex = GetNodeOrNull<ColorRect>("Visuals/Materialization/Vortex");
        _inward = GetNodeOrNull<GpuParticles2D>("Visuals/Materialization/Inward");
        _ignition = GetNodeOrNull<GpuParticles2D>("Visuals/Materialization/Ignition");
        if (_vortex?.Material is ShaderMaterial shader)
        { _vortexMaterial = (ShaderMaterial)shader.Duplicate(); _vortex.Material = _vortexMaterial; }
        if (_inward?.ProcessMaterial is ParticleProcessMaterial source)
        {
            if (_inwardPoints == null)
            {
                using Image image = Image.CreateEmpty(64, 1, false, Image.Format.Rgbaf);
                for (int i = 0; i < 64; i++)
                {
                    Vector2 point = Vector2.FromAngle(Mathf.Tau * i / 64) * 90;
                    image.SetPixel(i, 0, new Color(point.X, point.Y, 0, 1));
                }
                _inwardPoints = ImageTexture.CreateFromImage(image);
            }
            var material = (ParticleProcessMaterial)source.Duplicate();
            material.EmissionPointTexture = _inwardPoints; material.EmissionPointCount = 64;
            _inward.ProcessMaterial = material;
        }
        if (_ignition?.ProcessMaterial is ParticleProcessMaterial ignitionSource)
        {
            var ignitionMaterial = (ParticleProcessMaterial)ignitionSource.Duplicate();
            ignitionMaterial.EmissionPointTexture = _rimPoints;
            ignitionMaterial.EmissionPointCount = RimPointCount;
            _ignition.ProcessMaterial = ignitionMaterial;
        }
        _opening?.Hide();
    }

    // =========================================================
    // Force the absent pose synchronously before any gameplay frame; stagger uses the same bound tween.
    internal void BeginMaterialization(float delay = 0)
    {
        StopSpawnFeedback(); CancelMaterialization();
        SetLightActivation(true); OpeningState = PortalState.Forming;
        GetNode<Node2D>("Visuals").Show();
        _opening?.Show();
        if (_energy != null) _energy.Hide();
        var style = MaterializationStyle;
        bool detailed = style.Enabled && !style.ReducedMotion && style.ParticleAmount > 0 &&
            DetailedOpenings.Count < Mathf.Clamp(style.MaxDetailedOpenings, 0, 6) && OpeningOnScreen();
        if (detailed) { DetailedOpenings.Add(this); _detailHeld = true; }
        _vortexMaterial?.SetShaderParameter("intensity", PortalMaterializationStyle.Safe(style.VortexIntensity, 1, 0, 2));
        _vortexMaterial?.SetShaderParameter("ring_radius", PortalMaterializationStyle.Safe(style.RingRadius, 46, 30, 70));
        _vortexMaterial?.SetShaderParameter("flash_strength", PortalMaterializationStyle.Safe(style.FlashStrength, 1, 0, 2));
        _vortexMaterial?.SetShaderParameter("reduced_motion", !style.Enabled || style.ReducedMotion);
        float duration = PortalMaterializationStyle.Safe(style.DurationSeconds, 2.2f, .2f, 6);
        // Let opening particles drain before readiness even when the effect duration is shortened.
        if (_inward != null) _inward.Lifetime = Mathf.Min(.8f, duration * .38f);
        if (_ignition != null) _ignition.Lifetime = Mathf.Min(.42f, duration * .22f);
        SetOpeningProgress(0);
        _openingTween = CreateTween().SetPauseMode(Tween.TweenPauseMode.Bound);
        if (float.IsFinite(delay) && delay > 0) _openingTween.TweenInterval(delay);
        _openingTween.TweenCallback(Callable.From(StartOpeningParticles));
        _openingTween.TweenMethod(Callable.From<float>(SetOpeningProgress), 0f, 1f,
            duration);
        _openingTween.TweenCallback(Callable.From(FinishMaterialization));
    }

    // =========================================================
    // No physics scans: one canvas-space bounds test when opening begins, and screen-exit shutdown.
    private bool OpeningOnScreen()
    {
        Vector2 center = GetGlobalTransformWithCanvas() * Vector2.Zero;
        float extent = 112 * SizeMultiplier * Mathf.Max(GlobalScale.X, GlobalScale.Y);
        return GetViewportRect().Intersects(new Rect2(center - Vector2.One * extent, Vector2.One * extent * 2));
    }
    private void StartOpeningParticles()
    {
        if (!_detailHeld || !OpeningOnScreen()) { StopOpeningParticles(); return; }
        if (_inward != null)
        { _inward.Amount = Mathf.Clamp(MaterializationStyle.ParticleAmount, 1, 96); _inward.Restart(); _inward.Emitting = true; }
    }

    // =========================================================
    // Bounded local shader/pose work only during formation. Pausing freezes this progress and readiness together.
    private void SetOpeningProgress(float progress)
    {
        float p = Mathf.Clamp(progress, 0, 1);
        _vortexMaterial?.SetShaderParameter("progress", p);
        float reveal = Mathf.SmoothStep(0, 1, Mathf.Clamp((p - .24f) / .60f, 0, 1));
        if (_openingArt != null)
        {
            _openingArt.Modulate = new Color(1, 1, 1, reveal);
            _openingArt.Scale = Vector2.One * Mathf.Lerp(.12f, 1f, reveal);
        }
        if (p >= .60f && _inward != null) _inward.Emitting = false;
        if (p >= .77f && !_ignited)
        {
            _ignited = true;
            if (_detailHeld && _ignition != null) _ignition.Restart();
        }
    }

    // =========================================================
    // Also used by checkpoint reconstruction. Completion emits exactly once only for a live formation.
    internal void FinishMaterialization()
    {
        bool notify = OpeningState == PortalState.Forming;
        CancelMaterialization();
        SetLightActivation(true); OpeningState = PortalState.Open;
        GetNode<Node2D>("Visuals").Show();
        if (_openingArt != null) { _openingArt.Modulate = Colors.White; _openingArt.Scale = Vector2.One; }
        if (_energy != null) _energy.Visible = _energy.Style?.Enabled == true;
        RefreshWaveSmoke();
        if (notify) SpawnReady?.Invoke(this);
    }
    internal void RestoreActivation(bool active)
    {
        _waveSmoke = false;
        CancelMaterialization(); SetLightActivation(active);
        OpeningState = active ? PortalState.Open : PortalState.Dormant;
        GetNode<Node2D>("Visuals").Visible = active;
        if (_openingArt != null) { _openingArt.Modulate = Colors.White; _openingArt.Scale = Vector2.One; }
        if (_energy != null) _energy.Visible = _energy.Style?.Enabled == true;
        StopSpawnFeedback();
    }
    private void StopOpeningParticles()
    {
        if (_inward != null) _inward.Emitting = false;
        if (_ignition != null) _ignition.Emitting = false;
        if (_detailHeld) { DetailedOpenings.Remove(this); _detailHeld = false; }
    }
    private void CancelMaterialization()
    {
        if (_openingTween != null && _openingTween.IsValid()) _openingTween.Kill();
        _openingTween = null; _ignited = false;
        StopOpeningParticles(); _opening?.Hide();
    }
    // =========================================================
    // No retained budget slots, emissions, subscriptions or tween callbacks after unload/reentry.
    public override void _ExitTree()
    {
        CancelMaterialization(); StopSpawnFeedback();
        if (GodotObject.IsInstanceValid(_onScreen)) { _onScreen.ScreenExited -= StopSpawnFeedback; _onScreen.ScreenEntered -= RefreshWaveSmoke; }
    }
}
