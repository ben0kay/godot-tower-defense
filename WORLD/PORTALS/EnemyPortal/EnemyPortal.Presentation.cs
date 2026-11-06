// GPU-only artwork rotation and a bounded reusable spawn burst; no portal CPU animation loop.
using Godot;

public partial class EnemyPortal
{
    [ExportGroup("Portal Feedback")]
    [Export(PropertyHint.Range, "0,10,0.1")] public float RotationDegreesPerSecond { get; set; } = 2f;
    [Export] public bool SpawnParticlesEnabled { get; set; } = true;
    private EnergyFieldVisual _energy;
    private GpuParticles2D _spawnParticles;
    private VisibleOnScreenNotifier2D _onScreen;
    private ulong _nextBurstMs;

    // =========================================================
    // Copy only the tiny artwork material; particle resources remain shared immutable scene data.
    private void SetupPresentation()
    {
        _energy = GetNodeOrNull<EnergyFieldVisual>("Visuals/EnergyField");
        _spawnParticles = GetNodeOrNull<GpuParticles2D>("Visuals/SpawnParticles");
        _onScreen = GetNodeOrNull<VisibleOnScreenNotifier2D>("Visuals/OnScreen");
        if (GetNodeOrNull<Sprite2D>("Visuals/PortalArt") is Sprite2D art && art.Material is ShaderMaterial material)
        {
            var own = (ShaderMaterial)material.Duplicate(); art.Material = own;
            own.SetShaderParameter("radians_per_second", Mathf.DegToRad(float.IsFinite(RotationDegreesPerSecond)
                ? Mathf.Clamp(RotationDegreesPerSecond, 0, 10) : 2f));
        }
        if (_onScreen != null) _onScreen.ScreenExited += StopSpawnFeedback;
    }

    // =========================================================
    // One 24-particle emitter per portal; dense waves coalesce to at most one burst per 750ms.
    public void PlaySpawnFeedback()
    {
        if (!IsInsideTree() || !IsSpawnActive || !IsVisibleInTree() || GetTree().Paused || _onScreen?.IsOnScreen() != true) return;
        _energy?.PlaySpawnPulse();
        ulong now = Time.GetTicksMsec();
        if (!SpawnParticlesEnabled || _spawnParticles == null || now < _nextBurstMs) return;
        _nextBurstMs = now + 750;
        _spawnParticles.Restart();
    }
    private void StopSpawnFeedback()
    {
        if (_spawnParticles != null) _spawnParticles.Emitting = false;
    }
}
