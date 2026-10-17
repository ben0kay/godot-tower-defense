// Reusable beam visuals: animated core/halo, stable muzzle, wandering impact and optional owned GPU emitters.
// The weapon supplies world endpoints only; damage, aim and enemy identity never enter this renderer.
using Godot;

public partial class BeamRenderer : Node2D
{
    #region Visual configuration / cached nodes
    private const int MaximumPoints = 28;
    private BeamStyle _style;
    private Line2D _outer, _core;
    private GpuParticles2D _impactParticles, _muzzleParticles;
    private Vector2 _start, _end, _visualEnd;
    private float _time, _phase;
    private bool _ready;

    [Export] public BeamStyle Style
    {
        get => _style;
        set
        {
            if (_style == value) return;
            if (_ready && GodotObject.IsInstanceValid(_style)) _style.Changed -= RefreshStyle;
            _style = value ?? new BeamStyle();
            if (!_ready) return;
            // A remote Inspector replacement must not leak shared mutable .tres data.
            _style = (BeamStyle)_style.Duplicate(true);
            _style.Changed += RefreshStyle;
            RefreshStyle();
        }
    }

    public bool IsActive { get; private set; }
    public Vector2 VisualEndWorld => IsActive ? ToGlobal(_visualEnd) : Vector2.Zero;
    #endregion

    #region Lifecycle and API
    // =========================================================
    // Keep a single scene-owned core, halo and two optional particle emitters.
    public override void _Ready()
    {
        _outer = GetNode<Line2D>("Outer"); _core = GetNode<Line2D>("Core");
        _impactParticles = GetNodeOrNull<GpuParticles2D>("ImpactParticles");
        _muzzleParticles = GetNodeOrNull<GpuParticles2D>("MuzzleParticles");
        _outer.AddPoint(Vector2.Zero); _outer.AddPoint(Vector2.Zero);
        _core.AddPoint(Vector2.Zero); _core.AddPoint(Vector2.Zero);
        _style = _style == null ? new BeamStyle() : (BeamStyle)_style.Duplicate(true);
        _phase = (float)(GetInstanceId() % 997UL) * Mathf.Tau / 997f;
        _ready = true; _style.Changed += RefreshStyle;
        RefreshStyle(); SetBeam(Vector2.Zero, Vector2.Zero, false);
    }

    // =========================================================
    // No combat changes: the target point is untouched; only its visible location wanders.
    public void SetBeam(Vector2 fromWorld, Vector2 toWorld, bool active)
    {
        IsActive = active && fromWorld.IsFinite() && toWorld.IsFinite();
        if (!_ready) return;
        _start = IsActive ? ToLocal(fromWorld) : Vector2.Zero;
        _end = IsActive ? ToLocal(toWorld) : Vector2.Zero;
        Visible = IsActive;
        UpdateProcessing(); UpdateGeometry(); UpdateEmitters();
    }

    // =========================================================
    // Animate only while visible and only when style settings require CPU updates.
    public override void _Process(double delta)
    {
        if (!IsActive || !double.IsFinite(delta) || delta <= 0) return;
        _time += (float)delta;
        if (_time > 10000f) _time -= 10000f;
        ApplyWidths(); UpdateGeometry();
    }

    // =========================================================
    // Draw a small inexpensive soft contact glow behind the line endpoints.
    public override void _Draw()
    {
        if (!_ready || !IsActive || Style == null) return;
        float radius = Safe(Style.ImpactGlowRadius);
        float opacity = Mathf.Clamp(Safe(Style.ImpactGlowOpacity), 0f, 1f);
        if (radius <= 0f || opacity <= 0f) return;
        float flicker = Mathf.Clamp(Safe(Style.ImpactGlowFlicker), 0f, 0.5f);
        float alpha = opacity * (1f - flicker + flicker * Mathf.Sin(_time * 8f + _phase));
        Color tint = Style.CoreColor;
        DrawCircle(_visualEnd, radius * 2f, new Color(tint.R, tint.G, tint.B, alpha * 0.13f));
        DrawCircle(_visualEnd, radius, new Color(tint.R, tint.G, tint.B, alpha * 0.32f));
        DrawCircle(_visualEnd, Mathf.Max(0.7f, radius * 0.32f),
            new Color(tint.R, tint.G, tint.B, alpha * 0.75f));
    }
    #endregion

    #region Lightweight presentation helpers
    // =========================================================
    // Resize only when beam length crosses a point-spacing threshold, never allocate arrays per frame.
    private void EnsurePoints(int count)
    {
        while (_core.GetPointCount() < count)
        { _core.AddPoint(Vector2.Zero); _outer.AddPoint(Vector2.Zero); }
        while (_core.GetPointCount() > count)
        { int last = _core.GetPointCount() - 1; _core.RemovePoint(last); _outer.RemovePoint(last); }
    }

    // =========================================================
    // Ripple is transverse to the visible beam and fades at its ends, keeping the muzzle fixed.
    private void UpdateGeometry()
    {
        if (!_ready) return;
        if (!IsActive)
        {
            _visualEnd = Vector2.Zero; EnsurePoints(2);
            _core.SetPointPosition(0, Vector2.Zero); _core.SetPointPosition(1, Vector2.Zero);
            _outer.SetPointPosition(0, Vector2.Zero); _outer.SetPointPosition(1, Vector2.Zero);
            QueueRedraw(); return;
        }

        float radius = Safe(Style.ImpactWanderRadius), frequency = Safe(Style.ImpactWanderFrequency);
        float angle = _time * Mathf.Tau * frequency + _phase;
        Vector2 wander = radius > 0 && frequency > 0
            ? new Vector2(Mathf.Cos(angle), Mathf.Sin(angle * 0.79f)) * (radius * 0.7f)
            : Vector2.Zero;
        _visualEnd = _end + wander;

        Vector2 axis = _visualEnd - _start;
        float length = axis.Length();
        Vector2 normal = length > 0.0001f ? new Vector2(-axis.Y, axis.X) / length : Vector2.Zero;
        float amplitude = Mathf.Min(12f, Safe(Style.RippleAmplitude));
        int count = amplitude > 0 && length > 0.0001f
            ? Mathf.Clamp(Mathf.CeilToInt(length / Mathf.Max(10f, Safe(Style.PointSpacing))) + 1, 2, MaximumPoints)
            : 2;
        EnsurePoints(count);

        float wavelength = Mathf.Max(8f, Safe(Style.RippleWavelength));
        float progress = _time * Safe(Style.RippleTravelSpeed);
        float fadeSpan = Mathf.Clamp(Safe(Style.RippleEndFadeFraction), 0f, 0.45f);
        float irregular = Mathf.Clamp(Safe(Style.RippleIrregularity), 0f, 1f);
        float haloScale = Mathf.Min(3f, Safe(Style.OuterRippleScale));
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            Vector2 centre = _start + axis * t;
            float fade = i == 0 || i == count - 1 ? 0f :
                (fadeSpan <= 0f ? 1f : Mathf.Min(1f, Mathf.Min(t, 1f - t) / fadeSpan));
            float phase = Mathf.Tau * (length * t - progress) / wavelength + _phase;
            float ripple = amplitude * fade * (Mathf.Sin(phase) +
                irregular * 0.3f * Mathf.Sin(phase * 1.83f + _time * 2.4f));
            _core.SetPointPosition(i, centre + normal * ripple);
            _outer.SetPointPosition(i, centre + normal * ripple * haloScale);
        }

        if (_impactParticles != null) _impactParticles.Position = _visualEnd;
        if (_muzzleParticles != null) _muzzleParticles.Position = _start;
        if (Safe(Style.ImpactGlowRadius) > 0f) QueueRedraw();
    }

    // =========================================================
    // Keep the original independent pulse and resource-driven colour/width editing.
    private void ApplyWidths()
    {
        if (!_ready || Style == null) return;
        float factor = 1f + Mathf.Min(0.5f, Safe(Style.PulseAmount)) *
            Mathf.Sin(_time * Mathf.Tau * Safe(Style.PulseFrequency));
        _core.Width = Safe(Style.CoreWidth) * factor;
        _outer.Width = Safe(Style.OuterWidth) * factor;
    }

    // =========================================================
    // Only emit while active and opted in; particles belong to the renderer scene.
    private void UpdateEmitters()
    {
        if (_impactParticles != null)
        {
            bool emit = IsActive && Style.ImpactParticlesEnabled;
            if (_impactParticles.Emitting != emit) _impactParticles.Emitting = emit;
        }
        if (_muzzleParticles != null)
        {
            bool emit = IsActive && Style.MuzzleParticlesEnabled;
            if (_muzzleParticles.Emitting != emit) _muzzleParticles.Emitting = emit;
        }
    }

    // =========================================================
    // Optional animation switches are independent; disabling every effect makes the renderer idle.
    private void UpdateProcessing()
    {
        if (!_ready) return;
        bool pulse = Safe(Style.PulseAmount) > 0 && Safe(Style.PulseFrequency) > 0;
        bool ripple = Safe(Style.RippleAmplitude) > 0 && Safe(Style.RippleTravelSpeed) > 0;
        bool wander = Safe(Style.ImpactWanderRadius) > 0 && Safe(Style.ImpactWanderFrequency) > 0;
        bool glow = Safe(Style.ImpactGlowRadius) > 0 && Safe(Style.ImpactGlowFlicker) > 0;
        SetProcess(IsActive && (pulse || ripple || wander || glow));
    }

    // =========================================================
    // Styles are private to the placed renderer and never alter effective tower stats.
    private void RefreshStyle()
    {
        if (!_ready || Style == null) return;
        _core.DefaultColor = Style.CoreColor;
        _outer.DefaultColor = Style.OuterColor;
        if (_impactParticles != null) _impactParticles.Modulate = Style.CoreColor;
        if (_muzzleParticles != null) _muzzleParticles.Modulate = Style.CoreColor;
        ApplyWidths(); UpdateProcessing(); UpdateGeometry(); UpdateEmitters();
        QueueRedraw();
    }

    private static float Safe(float value) => float.IsFinite(value) ? Mathf.Max(0f, value) : 0f;
    #endregion

    #region Teardown
    // =========================================================
    // Do not leave shared subscriptions or GPU emission alive on tower unload.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_style)) _style.Changed -= RefreshStyle;
        IsActive = false;
        if (_impactParticles != null) _impactParticles.Emitting = false;
        if (_muzzleParticles != null) _muzzleParticles.Emitting = false;
        Visible = false; _ready = false;
    }
    #endregion
}
