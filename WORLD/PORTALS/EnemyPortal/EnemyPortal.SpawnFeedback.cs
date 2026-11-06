// Retained GPU smoke and a single bound artwork-only breathing tween, driven by accepted spawn feedback.
using Godot;
public partial class EnemyPortal
{
    [Export] public PortalSpawnFeedbackStyle SpawnFeedbackStyle { get; set; }
    private readonly RandomNumberGenerator _feedbackRandom = new();
    private bool _waveSmoke;
    private GpuParticles2D _smoke;
    private Sprite2D _pulseArt;
    private Tween _spawnPulse;
    private Vector2 _restScale = Vector2.One;
    private Color _restModulate = Colors.White;
    private static float FeedbackValue(float value, float fallback, float min, float max) =>
        PortalMaterializationStyle.Safe(value, fallback, min, max);

    // =========================================================
    // Materials are private to each emitter; rim positions and the shader stay shared.
    private void SetupSpawnFeedback()
    {
        SpawnFeedbackStyle ??= GD.Load<PortalSpawnFeedbackStyle>("res://WORLD/PORTALS/EnemyPortal/Visuals/DefaultPortalSpawnFeedbackStyle.tres") ?? new();
        _feedbackRandom.Randomize();
        _pulseArt = GetNodeOrNull<Sprite2D>("Visuals/PortalArt");
        _smoke = GetNodeOrNull<GpuParticles2D>("Visuals/SpawnSmoke");
        if (_smoke?.ProcessMaterial is ParticleProcessMaterial source)
        {
            var particles = (ParticleProcessMaterial)source.Duplicate();
            particles.EmissionPointTexture = _rimPoints; particles.EmissionPointCount = RimPointCount;
            _smoke.ProcessMaterial = particles;
        }
        if (_smoke?.Material is ShaderMaterial shader) _smoke.Material = (ShaderMaterial)shader.Duplicate();
        ConfigureSmoke(); _smoke?.Hide();
    }

    // =========================================================
    // Busy pulses are coalesced, never restarted into indefinite growth.
    private void PlayArtworkPulse()
    {
        var style = SpawnFeedbackStyle;
        if (!style.PulseEnabled || MaterializationStyle?.ReducedMotion == true || _pulseArt == null ||
            (_spawnPulse != null && _spawnPulse.IsValid())) return;
        _restScale = _pulseArt.Scale; _restModulate = _pulseArt.Modulate;
        float duration = FeedbackValue(style.PulseSeconds, .8f, .2f, 2) * FeedbackVariation();
        float expansion = FeedbackValue(style.ExpansionFraction, .12f, 0, .3f) * FeedbackVariation();
        float brightness = FeedbackValue(style.BrightnessStrength, .15f, 0, 1) * FeedbackVariation();
        _spawnPulse = CreateTween().SetPauseMode(Tween.TweenPauseMode.Bound);
        _spawnPulse.TweenMethod(Callable.From<float>(SetSpawnPulse), 0f, 1f, duration * .3f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _spawnPulse.TweenMethod(Callable.From<float>(SetSpawnPulse), 1f, 0f, duration * .7f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _spawnPulse.TweenCallback(Callable.From(() => { SetSpawnPulse(0); _spawnPulse = null; }));
        void SetSpawnPulse(float p)
        {
            _pulseArt.Scale = _restScale * (1 + expansion * p);
            _pulseArt.Modulate = new Color(_restModulate.R * (1 + brightness * p),
                _restModulate.G * (1 + brightness * p), _restModulate.B * (1 + brightness * p), _restModulate.A);
        }
    }

    // =========================================================
    // One continuous retained emitter; authored population caps live clouds regardless of spawn count.
    private void ConfigureSmoke()
    {
        var style = SpawnFeedbackStyle;
        if (!style.SmokeEnabled || MaterializationStyle?.ReducedMotion == true || _smoke == null) return;
        _smoke.Amount = Mathf.Clamp(style.PuffCount, 4, 40);
        _smoke.Lifetime = FeedbackValue(style.LifetimeSeconds, 5, 2, 8);
        if (_smoke.ProcessMaterial is ParticleProcessMaterial particles)
        {
            float size = FeedbackValue(style.PuffSize, 110, 48, 180) / 64;
            particles.ScaleMin = size * (1 - FeedbackValue(style.PuffSizeSpread, .2f, 0, .5f)); particles.ScaleMax = size;
            float speed = FeedbackValue(style.OutwardSpeed, 18, 4, 60);
            particles.RadialVelocityMin = speed * .75f; particles.RadialVelocityMax = speed * 1.25f;
            float swirl = FeedbackValue(style.SwirlAcceleration, 3, 0, 15);
            particles.TangentialAccelMin = swirl * .5f; particles.TangentialAccelMax = swirl;
        }
        if (_smoke.Material is ShaderMaterial shader)
        {
            shader.SetShaderParameter("smoke_tint", SpawnFeedbackStyle.Tint);
            shader.SetShaderParameter("opacity", FeedbackValue(style.Opacity, .38f, 0, 1));
        }
        _smoke.OneShot = false; _smoke.Explosiveness = 0; _smoke.Randomness = .8f;
    }
    // =========================================================
    // Wave events and visibility wake one continuous, fixed-budget emitter; completion lets existing clouds dissolve.
    internal void SetWaveSmoke(bool active)
    {
        _waveSmoke = active;
        if (!active) { if (_smoke != null) _smoke.Emitting = false; return; }
        RefreshWaveSmoke();
    }
    private void RefreshWaveSmoke()
    {
        if (_smoke == null || !_waveSmoke || !IsSpawnReady || _onScreen?.IsOnScreen() != true ||
            !SpawnFeedbackStyle.SmokeEnabled || MaterializationStyle?.ReducedMotion == true) return;
        if (!_smoke.Visible) { _smoke.Show(); _smoke.Restart(); }
        _smoke.Emitting = true;
    }
    private float FeedbackVariation() => _feedbackRandom.RandfRange(
        1 - FeedbackValue(SpawnFeedbackStyle.Irregularity, .4f, 0, .65f),
        1 + FeedbackValue(SpawnFeedbackStyle.Irregularity, .4f, 0, .65f));
    private void ResetSpawnMotion()
    {
        if (_spawnPulse != null && _spawnPulse.IsValid())
        {
            _spawnPulse.Kill();
            if (_pulseArt != null) { _pulseArt.Scale = _restScale; _pulseArt.Modulate = _restModulate; }
        }
        _spawnPulse = null;
        if (_smoke != null) { _smoke.Emitting = false; _smoke.Hide(); }
        _nextBurstMs = 0;
    }

    // =========================================================
    // GPU phase also drives the smoke shader; explicitly freeze simulation while the scene tree is paused.
    public override void _Notification(int what)
    {
        base._Notification(what);
        if (_smoke == null || Engine.IsEditorHint()) return;
        if (what == NotificationPaused) _smoke.SpeedScale = 0;
        else if (what == NotificationUnpaused) _smoke.SpeedScale = 1;
    }
}
