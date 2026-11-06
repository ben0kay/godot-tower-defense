// Retained GPU smoke and a single bound artwork-only breathing tween, driven by accepted spawn feedback.
using Godot;
public partial class EnemyPortal
{
    [Export] public PortalSpawnFeedbackStyle SpawnFeedbackStyle { get; set; }
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
        _pulseArt = GetNodeOrNull<Sprite2D>("Visuals/PortalArt");
        _smoke = GetNodeOrNull<GpuParticles2D>("Visuals/SpawnSmoke");
        if (_smoke?.ProcessMaterial is ParticleProcessMaterial source)
        {
            var particles = (ParticleProcessMaterial)source.Duplicate();
            particles.EmissionPointTexture = _rimPoints; particles.EmissionPointCount = RimPointCount;
            _smoke.ProcessMaterial = particles;
        }
        if (_smoke?.Material is ShaderMaterial shader) _smoke.Material = (ShaderMaterial)shader.Duplicate();
        _smoke?.Hide();
    }

    // =========================================================
    // Busy pulses are coalesced, never restarted into indefinite growth.
    private void PlayArtworkPulse()
    {
        var style = SpawnFeedbackStyle;
        if (!style.PulseEnabled || MaterializationStyle?.ReducedMotion == true || _pulseArt == null ||
            (_spawnPulse != null && _spawnPulse.IsValid())) return;
        _restScale = _pulseArt.Scale; _restModulate = _pulseArt.Modulate;
        float duration = FeedbackValue(style.PulseSeconds, .8f, .2f, 2);
        float expansion = FeedbackValue(style.ExpansionFraction, .12f, 0, .3f);
        float brightness = FeedbackValue(style.BrightnessStrength, .15f, 0, 1);
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
    // One retained one-shot emitter, capped at eight puffs; restarting never stacks GPU populations.
    private void PlaySmoke()
    {
        var style = SpawnFeedbackStyle;
        if (!style.SmokeEnabled || MaterializationStyle?.ReducedMotion == true || _smoke == null) return;
        _smoke.Amount = Mathf.Clamp(style.PuffCount, 1, 8);
        _smoke.Lifetime = FeedbackValue(style.LifetimeSeconds, 1.2f, .9f, 1.4f);
        if (_smoke.ProcessMaterial is ParticleProcessMaterial particles)
        {
            float size = FeedbackValue(style.PuffSize, 44, 24, 60) / 64;
            particles.ScaleMin = size * (1 - FeedbackValue(style.PuffSizeSpread, .2f, 0, .5f)); particles.ScaleMax = size;
            float speed = FeedbackValue(style.OutwardSpeed, 34, 8, 100);
            particles.RadialVelocityMin = speed * .75f; particles.RadialVelocityMax = speed * 1.25f;
            float swirl = FeedbackValue(style.SwirlAcceleration, 18, 0, 60);
            particles.TangentialAccelMin = swirl * .5f; particles.TangentialAccelMax = swirl;
        }
        if (_smoke.Material is ShaderMaterial shader)
        {
            shader.SetShaderParameter("smoke_tint", SpawnFeedbackStyle.Tint);
            shader.SetShaderParameter("opacity", FeedbackValue(style.Opacity, .55f, 0, 1));
        }
        _smoke.Show(); _smoke.Restart(); _smoke.Emitting = true;
    }
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
