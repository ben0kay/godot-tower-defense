// Owns one transient camera offset and arbitrates world-space requests for the current view.
using Godot;

public partial class CameraShakeManager : Node
{
    [Export] public Camera2D Camera { get; set; }
    [Export] public CameraShakeSettings Settings { get; set; }

    private Viewport _viewport;
    private ulong _windowStart;
    private float _intensity, _duration, _elapsed;
    private Vector2 _blendFrom;

    public override void _Ready()
    {
        _viewport = Camera?.GetViewport();
        SetProcess(false);
    }

    // =========================================================
    // Resolve distance once per request, then keep the strongest visible hit in the current window.
    public void RequestShake(Vector2 worldPosition, float? intensity = null, float? duration = null, float? radius = null)
    {
        if (Settings?.Enabled != true || _viewport == null || !CanProcess() || !Camera.IsCurrent()) return;

        float pixels = intensity ?? Settings.DefaultIntensity;
        float seconds = duration ?? Settings.DefaultDuration;
        float influence = radius ?? Settings.DefaultRadius;
        if (!worldPosition.IsFinite() || !float.IsFinite(pixels) || pixels <= 0f
            || !float.IsFinite(seconds) || seconds <= 0f || !float.IsFinite(influence) || influence < 0f)
            return;

        Transform2D toWorld = _viewport.GetCanvasTransform().AffineInverse();
        Rect2 viewportRect = _viewport.GetVisibleRect();
        // The RTS camera is axis-aligned. Use the displayed view, including any current offset.
        Vector2 first = toWorld * viewportRect.Position;
        Vector2 last = toWorld * viewportRect.End;
        Vector2 nearest = worldPosition.Clamp(first, last);
        float distance = worldPosition.DistanceTo(nearest);
        if (distance > 0f)
        {
            if (distance >= influence) return;
            float t = 1f - distance / influence;
            float weight = Settings.Falloff == CameraShakeSettings.DistanceFalloff.Smootherstep
                ? t * t * t * (t * (6f * t - 15f) + 10f)
                : t * t * (3f - 2f * t);
            pixels *= weight;
        }

        if (pixels <= 0f || Settings.MaximumDisplacement <= 0f) return;
        ulong now = Time.GetTicksUsec();
        bool inWindow = _intensity > 0f && (now - _windowStart) / 1_000_000.0 < Settings.SuppressionSeconds;
        if (inWindow && pixels <= _intensity) return;
        if (!inWindow) _windowStart = now;

        _blendFrom = Camera.Offset * PixelsPerWorldUnit();
        _intensity = pixels;
        _duration = seconds;
        _elapsed = 0f;
        SetProcess(true);
    }

    // =========================================================
    // Blend from the existing offset before damping to zero. The bounded ellipse never adds hits together.
    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (Settings?.Enabled != true || Settings.MaximumDisplacement <= 0f || !Camera.IsCurrent())
        {
            ResetShake();
            return;
        }
        if (_elapsed >= _duration)
        {
            Camera.Offset = Vector2.Zero;
            SetProcess(false);
            return;
        }

        float progress = _elapsed / _duration;
        float attack = Mathf.Min(Settings.AttackSeconds, _duration * .25f);
        float blend = attack > 0f ? Mathf.SmoothStep(0f, attack, _elapsed) : 1f;
        float fade = 1f - Mathf.SmoothStep(0f, 1f, progress);
        float phase = Mathf.Tau * Settings.FrequencyHz * _elapsed;
        float amplitude = Mathf.Min(_intensity, Settings.MaximumDisplacement);
        Vector2 wave = new Vector2(Mathf.Cos(phase), .65f * Mathf.Sin(phase)) * amplitude;
        Vector2 screenOffset = (_blendFrom.Lerp(wave, blend) * fade).LimitLength(Settings.MaximumDisplacement);
        Camera.Offset = screenOffset / PixelsPerWorldUnit();
    }

    private Vector2 PixelsPerWorldUnit() => Camera.Zoom * _viewport.GetFinalTransform().Scale;

    // =========================================================
    // Pause and scene transitions discard both the visual offset and the arbitration window.
    public void ResetShake()
    {
        if (IsProcessing() && GodotObject.IsInstanceValid(Camera)) Camera.Offset = Vector2.Zero;
        SetProcess(false);
        _intensity = 0f;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPaused || what == NotificationDisabled) ResetShake();
    }

    public override void _ExitTree() => ResetShake();
}
