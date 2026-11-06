// Faster shader-driven portal spin with short accepted-spawn surges and bounded irregular sparks.
using Godot;
using System;

public partial class EnemyPortal
{
    [ExportGroup("Portal Feedback")]
    [Export(PropertyHint.Range, "0,540,5")] public float RotationDegreesPerSecond { get; set; } = 180f;
    [Export] public bool SpawnParticlesEnabled { get; set; } = true;
    [ExportGroup("Spawn Spin Surge")]
    [Export(PropertyHint.Range, "0,480,5")] public float SpawnSpinBoostDegreesPerSecond { get; set; } = 210f;
    [Export(PropertyHint.Range, ".2,1.5,.05,suffix:s")] public float SpawnSpinBoostSeconds { get; set; } = .7f;

    private EnergyFieldVisual _energy;
    private GpuParticles2D _spawnParticles;
    private VisibleOnScreenNotifier2D _onScreen;
    private ulong _nextBurstMs;
    private static ImageTexture _rimPoints;
    private const int RimPointCount = 64;
    private const float SpinCycleSeconds = 3600f; // Godot's shader TIME default rollover.
    private ShaderMaterial _spinMaterial;
    private float _spinAnchorTime, _spinAnchorAngle, _spinBoostVelocity, _spinBoostDuration, _idleSpinVelocity;

    // =========================================================
    // Share immutable spawn points but keep rotation/material state per portal. Idle spin stays GPU-only.
    private void SetupPresentation()
    {
        _energy = GetNodeOrNull<EnergyFieldVisual>("Visuals/EnergyField");
        _spawnParticles = GetNodeOrNull<GpuParticles2D>("Visuals/SpawnParticles");
        _onScreen = GetNodeOrNull<VisibleOnScreenNotifier2D>("Visuals/OnScreen");
        if (GetNodeOrNull<Sprite2D>("Visuals/PortalArt") is Sprite2D art && art.Material is ShaderMaterial material)
        {
            _spinMaterial = (ShaderMaterial)material.Duplicate(); art.Material = _spinMaterial;
            _idleSpinVelocity = Mathf.DegToRad(float.IsFinite(RotationDegreesPerSecond)
                ? Mathf.Clamp(RotationDegreesPerSecond, 0, 540) : 180f);
            _spinAnchorTime = SpinClock();
            _spinAnchorAngle = _spinBoostVelocity = _spinBoostDuration = 0;
            _spinMaterial.SetShaderParameter("radians_per_second", _idleSpinVelocity);
            _spinMaterial.SetShaderParameter("anchor_time", _spinAnchorTime);
            _spinMaterial.SetShaderParameter("anchor_angle", 0f);
            _spinMaterial.SetShaderParameter("boost_radians_per_second", 0f);
            _spinMaterial.SetShaderParameter("boost_duration", .7f);
            _spinMaterial.SetShaderParameter("noise_offset", (float)(GetInstanceId() % 997UL) / 997f * Mathf.Tau);
        }
        if (_spawnParticles?.ProcessMaterial is ParticleProcessMaterial source)
        {
            if (_rimPoints == null)
            {
                using Image image = Image.CreateEmpty(RimPointCount, 1, false, Image.Format.Rgbaf);
                for (int i = 0; i < RimPointCount; i++)
                {
                    Vector2 point = Vector2.FromAngle(Mathf.Tau * i / RimPointCount) * 46f;
                    image.SetPixel(i, 0, new Color(point.X, point.Y, 0, 1));
                }
                _rimPoints = ImageTexture.CreateFromImage(image);
            }
            var burst = (ParticleProcessMaterial)source.Duplicate();
            burst.EmissionPointTexture = _rimPoints;
            burst.EmissionPointCount = RimPointCount;
            _spawnParticles.ProcessMaterial = burst;
            _spawnParticles.LocalCoords = true;
        }
        SetupSpawnFeedback();
        if (_onScreen != null) { _onScreen.ScreenExited += StopSpawnFeedback; _onScreen.ScreenEntered += RefreshWaveSmoke; }
    }

    // =========================================================
    // Rebase an integrated speed-up only on an accepted burst: no visible rotation jump when boosted repeatedly.
    // Shader TIME and Time.GetTicksMsec both track engine uptime; handle the default one-hour shader rollover.
    private static float SpinClock() => (Time.GetTicksMsec() % 3600000UL) * .001f;
    private void SurgeRotation()
    {
        if (_spinMaterial == null) return;
        float now = SpinClock(), elapsed = now - _spinAnchorTime;
        if (elapsed < 0) elapsed += SpinCycleSeconds;
        float boostTime = Mathf.Min(elapsed, _spinBoostDuration);
        float boostAngle = _spinBoostDuration > 0
            ? _spinBoostVelocity * (boostTime - boostTime * boostTime / (2 * _spinBoostDuration)) : 0;
        _spinAnchorAngle = Mathf.PosMod(_spinAnchorAngle + _idleSpinVelocity * elapsed + boostAngle, Mathf.Tau);
        _spinAnchorTime = now;
        _spinBoostVelocity = Mathf.DegToRad(float.IsFinite(SpawnSpinBoostDegreesPerSecond)
            ? Mathf.Clamp(SpawnSpinBoostDegreesPerSecond, 0, 480) : 210f) * FeedbackVariation();
        _spinBoostDuration = FeedbackValue(SpawnSpinBoostSeconds, .7f, .2f, 1.5f) * FeedbackVariation();
        _spinMaterial.SetShaderParameter("anchor_time", _spinAnchorTime);
        _spinMaterial.SetShaderParameter("anchor_angle", _spinAnchorAngle);
        _spinMaterial.SetShaderParameter("boost_radians_per_second", _spinBoostVelocity);
        _spinMaterial.SetShaderParameter("boost_duration", _spinBoostDuration);
    }

    // =========================================================
    // Real spawns drive irregular bursts; no cosmetic callbacks can delay or reject an enemy.
    public void PlaySpawnFeedback()
    {
        if (!IsInsideTree() || !IsSpawnReady || !IsVisibleInTree() || GetTree().Paused || _onScreen?.IsOnScreen() != true) return;
        _energy?.PlaySpawnPulse();
        ulong now = Time.GetTicksMsec();
        if (now < _nextBurstMs) return;
        _nextBurstMs = now + (ulong)Mathf.Clamp(400f * FeedbackVariation(), 230f, 630f);
        SurgeRotation();
        PlayArtworkPulse();
        if (SpawnParticlesEnabled && _spawnParticles != null)
        {
            // Vary burst density, flight speed, spread and timing without creating new emitter nodes.
            _spawnParticles.Amount = Mathf.Clamp((int)(44 * FeedbackVariation()), 22, 64);
            _spawnParticles.Rotation = _feedbackRandom.RandfRange(-Mathf.Pi, Mathf.Pi);
            _spawnParticles.Explosiveness = _feedbackRandom.RandfRange(.64f, 1f);
            _spawnParticles.Randomness = _feedbackRandom.RandfRange(.55f, .98f);
            _spawnParticles.SpeedScale = _feedbackRandom.RandfRange(.78f, 1.25f);
            _spawnParticles.Restart();
        }
    }

    private void StopSpawnFeedback()
    {
        if (_spawnParticles != null) _spawnParticles.Emitting = false;
        ResetSpawnMotion();
        StopOpeningParticles();
    }
}
