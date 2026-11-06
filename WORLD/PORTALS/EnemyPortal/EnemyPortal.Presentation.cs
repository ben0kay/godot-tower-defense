// GPU-only artwork rotation and a bounded reusable spawn burst; no portal CPU animation loop.
using Godot;

public partial class EnemyPortal
{
    [ExportGroup("Portal Feedback")]
    [Export(PropertyHint.Range, "0,360,1")] public float RotationDegreesPerSecond { get; set; } = 120f;
    [Export] public bool SpawnParticlesEnabled { get; set; } = true;
    private EnergyFieldVisual _energy;
    private GpuParticles2D _spawnParticles;
    private VisibleOnScreenNotifier2D _onScreen;
    private ulong _nextBurstMs;
    private static ImageTexture _rimPoints;
    private const int RimPointCount = 64;

    // =========================================================
    // Copy per-portal materials; reuse one immutable 64-point rim texture across all portals.
    private void SetupPresentation()
    {
        _energy = GetNodeOrNull<EnergyFieldVisual>("Visuals/EnergyField");
        _spawnParticles = GetNodeOrNull<GpuParticles2D>("Visuals/SpawnParticles");
        _onScreen = GetNodeOrNull<VisibleOnScreenNotifier2D>("Visuals/OnScreen");
        if (GetNodeOrNull<Sprite2D>("Visuals/PortalArt") is Sprite2D art && art.Material is ShaderMaterial material)
        {
            var own = (ShaderMaterial)material.Duplicate(); art.Material = own;
            own.SetShaderParameter("radians_per_second", Mathf.DegToRad(float.IsFinite(RotationDegreesPerSecond)
                ? Mathf.Clamp(RotationDegreesPerSecond, 0, 360) : 120f));
        }
        if (_spawnParticles?.ProcessMaterial is ParticleProcessMaterial source)
        {
            // Shared immutable float positions, generated once: every particle starts on the visible rim.
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
        }
        SetupSpawnFeedback();
        if (_onScreen != null) _onScreen.ScreenExited += StopSpawnFeedback;
    }

    // =========================================================
    // One 48-particle emitter per portal; dense waves coalesce to at most one burst per 750ms.
    public void PlaySpawnFeedback()
    {
        if (!IsInsideTree() || !IsSpawnReady || !IsVisibleInTree() || GetTree().Paused || _onScreen?.IsOnScreen() != true) return;
        _energy?.PlaySpawnPulse();
        ulong now = Time.GetTicksMsec();
        if (now < _nextBurstMs) return;
        // Keep one cadence for all costly responses; let the retained smoke population finish before reuse.
        float lifetime = SpawnFeedbackStyle.SmokeEnabled ? FeedbackValue(SpawnFeedbackStyle.LifetimeSeconds, 1.2f, .9f, 1.4f) : .75f;
        _nextBurstMs = now + (ulong)Mathf.Max(750, lifetime * 1000);
        PlayArtworkPulse(); PlaySmoke();
        if (SpawnParticlesEnabled && _spawnParticles != null) _spawnParticles.Restart();
    }
    private void StopSpawnFeedback()
    {
        if (_spawnParticles != null) _spawnParticles.Emitting = false;
        ResetSpawnMotion();
        StopOpeningParticles();
    }
}
