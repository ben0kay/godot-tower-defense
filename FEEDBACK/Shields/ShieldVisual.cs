// One reusable GPU shader orb for enemies now and optional damageable towers later.
// Event-driven damage/break alpha transitions; idle shields do no C# per-frame work.
using Godot;

public partial class ShieldVisual : Node2D
{
    #region Presentation
    [Export] public ShieldVisualStyle Style { get; set; }
    public bool HasShieldVisual { get; private set; }
    private ColorRect _aura;
    private Tween _feedbackTween;
    #endregion

    #region Lifecycle
    // =========================================================
    // Share one static shader material and a lightweight quad; no per-frame geometry or CPU processing.
    public override void _Ready()
    {
        _aura = GetNode<ColorRect>("Aura");
        SetProcess(false); SetPhysicsProcess(false);
        ResetVisual();
    }

    // =========================================================
    // Fit the circular shader orb to any source visual size once at creation.
    public void SetVisualSize(Vector2 sourceDimensions)
    {
        if (_aura == null) return;
        Vector2 baseSize = _aura.Size;
        float multiplier = Safe(Style?.SizeMultiplier ?? 1.65f, 1.65f, 1f, 2f);
        Vector2 size = new(
            Mathf.Max(8f, Safe(sourceDimensions.X, 32f, 0f, 2048f) * multiplier),
            Mathf.Max(8f, Safe(sourceDimensions.Y, 32f, 0f, 2048f) * multiplier));
        _aura.Scale = new Vector2(size.X / Mathf.Max(1f, baseSize.X), size.Y / Mathf.Max(1f, baseSize.Y));
    }
    #endregion

    #region Event-driven shield display
    // =========================================================
    // Caller passes only old/new points: damage and restoration are determined outside this component.
    public void SetShield(float previous, float current, bool animate = true)
    {
        if (_aura == null) return;
        // Sustained beams may damage the same shield at 60+Hz: do not restart the same flash each tick.
        if (animate && current > 0f && current < previous && HasShieldVisual &&
            _feedbackTween != null && _feedbackTween.IsValid() && _feedbackTween.IsRunning()) return;
        StopFeedback();
        if (current > 0f)
        {
            HasShieldVisual = true;
            Visible = true; _aura.Visible = true;
            if (animate && current < previous)
            {
                SetAlpha(Safe(Style?.HitOpacity ?? 1.0f, 1.0f, 0f, 1f));
                FadeTo(IdleAlpha(), Safe(Style?.HitFadeSeconds ?? .28f, .28f, .05f, 2f));
            }
            else SetAlpha(IdleAlpha());
            return;
        }

        HasShieldVisual = false;
        if (animate && previous > 0f)
        {
            Visible = true; _aura.Visible = true;
            SetAlpha(Safe(Style?.BreakOpacity ?? 1.0f, 1.0f, 0f, 1f));
            _feedbackTween = CreateTween();
            _feedbackTween.TweenProperty(_aura, "modulate:a", 0f,
                Safe(Style?.BreakFadeSeconds ?? .38f, .38f, .05f, 2f));
            _feedbackTween.TweenCallback(Callable.From(() => { if (!HasShieldVisual) ResetVisual(); }));
        }
        else ResetVisual();
    }

    // =========================================================
    // Unload/respawn immediately cancels active tweens without leaving an old flash visible.
    public void ResetVisual()
    {
        StopFeedback();
        HasShieldVisual = false;
        if (_aura != null) { _aura.Visible = false; SetAlpha(0f); }
        Visible = false;
    }

    // =========================================================
    // Damage flashes decay automatically; no _Process() is needed on shielded or unshielded units.
    private void FadeTo(float opacity, float duration)
    {
        _feedbackTween = CreateTween();
        _feedbackTween.TweenProperty(_aura, "modulate:a", opacity, duration)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
    }

    private void SetAlpha(float alpha)
    {
        Color tint = Style?.Tint ?? new Color("#ff7352");
        _aura.SelfModulate = new Color(tint.R, tint.G, tint.B, tint.A);
        _aura.Modulate = new Color(1f, 1f, 1f, alpha);
    }

    private float IdleAlpha() => Safe(Style?.IdleOpacity ?? .82f, .82f, 0f, 1f);

    private void StopFeedback()
    {
        if (_feedbackTween != null && _feedbackTween.IsValid()) _feedbackTween.Kill();
        _feedbackTween = null;
    }

    private static float Safe(float value, float fallback, float min, float max) =>
        float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
    #endregion

    #region Teardown
    // =========================================================
    // Shield effects never survive their owner or retain game-state callbacks.
    public override void _ExitTree() => StopFeedback();
    #endregion
}
