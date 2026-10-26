// CD3 shared, bounded completion glints: one world-space renderer for all construction sites.
// Brief cosmetic pulses never delay work completion or follow the drone on its return flight.
using Godot;

public partial class ConstructionCompletionFlash : Node2D
{
    #region Retained Pulse Buffer
    private const int Capacity = 64;
    private struct Pulse
    {
        public Vector2 Position;
        public Color Tint;
        public float Radius, Age, Duration, Opacity;
    }
    private readonly Pulse[] _pulses = new Pulse[Capacity];
    private int _count;
    private ConstructionDroneManager _manager;
    public int ActiveCount => _count;
    // =========================================================
    // Cache the one manager reference so turning visuals off hides any in-flight glints.
    public override void _Ready()
    {
        _manager = GetParent() as ConstructionDroneManager;
        SetProcess(false); SetPhysicsProcess(false);
    }
    #endregion

    #region One-Shot Trigger and Cleanup
    // =========================================================
    // Visual cap drops excess effects, never jobs. The ring stays at the completed site
    // while the drone independently flies home or is reused.
    public void Spawn(Vector2 position, float halfWidth, ConstructionDroneMasterConfig config)
    {
        if (config?.Enabled != true || !config.ShowVisuals || !config.CompletionFlashEnabled ||
            _count >= Capacity || !IsInsideTree() || !float.IsFinite(position.X) || !float.IsFinite(position.Y)) return;
        _pulses[_count++] = new Pulse
        {
            Position = ToLocal(position), Tint = config.BeamColor,
            Radius = Mathf.Clamp(halfWidth * .85f, 12, 128),
            Duration = ConstructionDroneMasterConfig.Safe(config.CompletionFlashSeconds, .28f, .1f, 1),
            Opacity = ConstructionDroneMasterConfig.Safe(config.CompletionFlashOpacity, .65f, 0, 1)
        };
        SetProcess(true); QueueRedraw();
    }
    // =========================================================
    // One active-only update for all flashes, zero per-completion scene/timer allocations.
    public override void _Process(double delta)
    {
        if (_count == 0 || !IsInsideTree() || GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        if (_manager?.Config?.Enabled != true || !_manager.Config.ShowVisuals || !_manager.Config.CompletionFlashEnabled)
        { Clear(); return; }
        float dt = (float)Mathf.Min((float)delta, 3600f);
        for (int i = 0; i < _count; i++)
        {
            Pulse flash = _pulses[i]; flash.Age += dt;
            if (flash.Age >= flash.Duration) { _pulses[i--] = _pulses[--_count]; continue; }
            _pulses[i] = flash;
        }
        if (_count == 0) SetProcess(false);
        QueueRedraw();
    }
    // =========================================================
    // Feature-off or map cleanup discards presentation without touching construction.
    public void Clear()
    {
        _count = 0; SetProcess(false); QueueRedraw();
    }
    #endregion

    #region Draw
    // =========================================================
    // Small cyan ring and soft core burst; all opacity vanishes in under one second.
    public override void _Draw()
    {
        for (int i = 0; i < _count; i++)
        {
            Pulse flash = _pulses[i];
            float t = Mathf.Clamp(flash.Age / flash.Duration, 0, 1);
            float alpha = flash.Opacity * (1 - t) * (1 - t);
            float radius = Mathf.Lerp(flash.Radius * .3f, flash.Radius, t);
            Color core = new(flash.Tint.R, flash.Tint.G, flash.Tint.B, .18f * alpha);
            Color glow = new(flash.Tint.R, flash.Tint.G, flash.Tint.B, .22f * alpha);
            Color rim = new(.8f, 1, 1, .9f * alpha);
            DrawCircle(flash.Position, flash.Radius * .45f, core);
            DrawArc(flash.Position, radius, 0, Mathf.Tau, 36, glow, 5, true);
            DrawArc(flash.Position, radius, 0, Mathf.Tau, 36, rim, 1.5f, true);
        }
    }
    #endregion
}
