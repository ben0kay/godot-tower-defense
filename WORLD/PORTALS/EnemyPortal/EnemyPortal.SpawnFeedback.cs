// Retained GPU smoke and a single bound artwork-only breathing tween, driven by accepted spawn feedback.
using Godot;
public partial class EnemyPortal
{
    [Export] public PortalSpawnFeedbackStyle SpawnFeedbackStyle { get; set; }
    private readonly RandomNumberGenerator _feedbackRandom = new();
    private bool _waveSmoke;
    private GpuParticles2D _smoke, _largeSmoke;
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
        _largeSmoke = GetNodeOrNull<GpuParticles2D>("Visuals/LargeSmoke");
        foreach (var emitter in new[] { _smoke, _largeSmoke })
        {
            if (emitter == null) continue;
            // Simulate in native portal space: Visuals applies SizeMultiplier once to positions, plume size and drift.
            emitter.LocalCoords = true;
            if (emitter.ProcessMaterial is ParticleProcessMaterial source)
            {
                var particles = (ParticleProcessMaterial)source.Duplicate();
                particles.EmissionPointTexture = _rimPoints; particles.EmissionPointCount = RimPointCount;
                emitter.ProcessMaterial = particles;
            }
            if (emitter.Material is ShaderMaterial shader) emitter.Material = (ShaderMaterial)shader.Duplicate();
            ConfigureSmoke(emitter, emitter == _largeSmoke); emitter.Hide();
        }
    }

    // =========================================================
    // Each accepted event gets a short, asymmetrical bubble/double-bubble shape with independent peaks.
    // One bounded active tween per portal; repeated spawns never accumulate scale or start extra processors.
    private void PlayArtworkPulse()
    {
        var style = SpawnFeedbackStyle;
        if (!style.PulseEnabled || MaterializationStyle?.ReducedMotion == true || _pulseArt == null ||
            (_spawnPulse != null && _spawnPulse.IsValid())) return;
        _restScale = _pulseArt.Scale; _restModulate = _pulseArt.Modulate;
        float duration = Mathf.Clamp(FeedbackValue(style.PulseSeconds, .56f, .2f, 2) * FeedbackVariation(), .30f, .76f);
        float expansion = FeedbackValue(style.ExpansionFraction, .16f, 0, .3f) * FeedbackVariation();
        float brightness = FeedbackValue(style.BrightnessStrength, .22f, 0, 1) * FeedbackVariation();
        bool doubleBubble = _feedbackRandom.Randf() < .7f;
        float secondaryPeak = _feedbackRandom.RandfRange(.4f, .82f);

        _spawnPulse = CreateTween().SetPauseMode(Tween.TweenPauseMode.Bound);
        _spawnPulse.TweenMethod(Callable.From<float>(SetSpawnPulse), 0f, expansion, duration * (doubleBubble ? .24f : .32f))
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        if (doubleBubble)
        {
            _spawnPulse.TweenMethod(Callable.From<float>(SetSpawnPulse), expansion, expansion * .15f, duration * .24f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            _spawnPulse.TweenMethod(Callable.From<float>(SetSpawnPulse), expansion * .15f, expansion * secondaryPeak, duration * .17f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
            _spawnPulse.TweenMethod(Callable.From<float>(SetSpawnPulse), expansion * secondaryPeak, 0f, duration * .35f)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        }
        else _spawnPulse.TweenMethod(Callable.From<float>(SetSpawnPulse), expansion, 0f, duration * .68f)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _pulseBrightness = brightness;
        _spawnPulse.TweenCallback(Callable.From(() => { SetSpawnPulse(0); _spawnPulse = null; }));
    }
    private float _pulseBrightness;

    private void SetSpawnPulse(float scaleBonus)
    {
        if (_pulseArt == null) return;
        _pulseArt.Scale = _restScale * (1 + scaleBonus);
        float brightness = 1 + _pulseBrightness * Mathf.Clamp(scaleBonus * 6, 0, 1);
        _pulseArt.Modulate = new Color(_restModulate.R * brightness, _restModulate.G * brightness,
            _restModulate.B * brightness, _restModulate.A);
    }

    // =========================================================
    // One continuous retained emitter; authored population caps live clouds regardless of spawn count.
    private void ConfigureSmoke(GpuParticles2D emitter, bool large)
    {
        var style = SpawnFeedbackStyle;
        if (!style.SmokeEnabled || MaterializationStyle?.ReducedMotion == true || emitter == null || large && !style.LargeCloudsEnabled) return;
        emitter.Amount = large ? Mathf.Clamp(style.LargeCloudCount, 2, 12) : Mathf.Clamp(style.PuffCount, 4, 40);
        emitter.Lifetime = large ? FeedbackValue(style.LargeCloudLifetime, 9, 4, 12) : FeedbackValue(style.LifetimeSeconds, 5.5f, 2, 8);
        if (emitter.ProcessMaterial is ParticleProcessMaterial particles)
        {
            float size = FeedbackValue(style.PuffSize, 125, 48, 240) / 64 * (large ? FeedbackValue(style.LargeCloudSizeMultiplier, 2.7f, 1.5f, 4) : 1);
            particles.ScaleMin = size * (1 - FeedbackValue(style.PuffSizeSpread, .2f, 0, .5f)); particles.ScaleMax = size;
            float speed = FeedbackValue(style.OutwardSpeed, 26, 4, 80) * (large ? .72f : 1);
            particles.RadialVelocityMin = speed * .75f; particles.RadialVelocityMax = speed * 1.25f;
            float swirl = FeedbackValue(style.SwirlAcceleration, 6, 0, 20) * (large ? .55f : 1);
            particles.TangentialAccelMin = swirl * .5f; particles.TangentialAccelMax = swirl;
        }
        if (emitter.Material is ShaderMaterial shader)
        {
            shader.SetShaderParameter("smoke_tint", large ? style.LargeCloudTint : style.Tint);
            shader.SetShaderParameter("opacity", large ? FeedbackValue(style.LargeCloudOpacity, .12f, 0, .3f) : FeedbackValue(style.Opacity, .38f, 0, 1));
        }
        emitter.OneShot = false; emitter.Explosiveness = 0; emitter.Randomness = .95f;
    }
    // =========================================================
    // Wave events and visibility wake one continuous, fixed-budget emitter; completion lets existing clouds dissolve.
    internal void SetWaveSmoke(bool active)
    {
        _waveSmoke = active;
        if (!active) { if (_smoke != null) _smoke.Emitting = false; if (_largeSmoke != null) _largeSmoke.Emitting = false; return; }
        RefreshWaveSmoke();
    }
    private void RefreshWaveSmoke()
    {
        if (_smoke == null || !_waveSmoke || !IsSpawnReady || _onScreen?.IsOnScreen() != true ||
            !SpawnFeedbackStyle.SmokeEnabled || MaterializationStyle?.ReducedMotion == true) return;
        if (!_smoke.Visible) { _smoke.Show(); _smoke.Restart(); }
        _smoke.Emitting = true;
        if (_largeSmoke != null && SpawnFeedbackStyle.LargeCloudsEnabled)
        {
            if (!_largeSmoke.Visible) { _largeSmoke.Show(); _largeSmoke.Restart(); }
            _largeSmoke.Emitting = true;
        }
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
        if (_largeSmoke != null) { _largeSmoke.Emitting = false; _largeSmoke.Hide(); }
        _nextBurstMs = 0;
    }

    // =========================================================
    // GPU phase also drives the smoke shader; explicitly freeze simulation while the scene tree is paused.
    public override void _Notification(int what)
    {
        base._Notification(what);
        if (_smoke == null || Engine.IsEditorHint()) return;
        if (what == NotificationPaused || what == NotificationUnpaused)
        {
            float speed = what == NotificationPaused ? 0 : 1;
            _smoke.SpeedScale = speed;
            if (_largeSmoke != null) _largeSmoke.SpeedScale = speed;
        }
    }
}
