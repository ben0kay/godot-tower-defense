// One reusable, event-driven shield sprite for enemies now and optional damageable towers later.
// This component receives shield changes; it owns no health, damage, healing or targeting rules.
using Godot;

public partial class ShieldVisual : Node2D
{
    #region Presentation
    [Export] public ShieldVisualStyle Style { get; set; }
    public bool HasShieldVisual { get; private set; }
    private Sprite2D _aura;
    private Tween _feedbackTween;
    #endregion

    #region Lifecycle
    // =========================================================
    // One baked sprite and no frame-based visual calculations for idle shields.
    public override void _Ready()
    {
        _aura = GetNode<Sprite2D>("Aura");
        SetProcess(false); SetPhysicsProcess(false);
        ResetVisual();
    }

    // =========================================================
    // Fit the same ellipse/halo artwork to any source visual size once at creation.
    public void SetVisualSize(Vector2 sourceDimensions)
    {
        if (_aura?.Texture == null) return;
        Vector2 baseSize = _aura.Texture.GetSize();
        float multiplier = Safe(Style?.SizeMultiplier ?? 1.23f, 1.23f, 1f, 2f);
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
        StopFeedback();
        if (current > 0f)
        {
            HasShieldVisual = true;
            Visible = true; _aura.Visible = true;
            if (animate && current < previous)
            {
                SetAlpha(Safe(Style?.HitOpacity ?? 0.82f, 0.82f, 0f, 1f));
                FadeTo(IdleAlpha(), Safe(Style?.HitFadeSeconds ?? .24f, .24f, .05f, 2f));
            }
            else SetAlpha(IdleAlpha());
            return;
        }

        HasShieldVisual = false;
        if (animate && previous > 0f)
        {
            Visible = true; _aura.Visible = true;
            SetAlpha(Safe(Style?.BreakOpacity ?? .95f, .95f, 0f, 1f));
            _feedbackTween = CreateTween();
            _feedbackTween.TweenProperty(_aura, "modulate:a", 0f,
                Safe(Style?.BreakFadeSeconds ?? .32f, .32f, .05f, 2f));
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
        Color tint = Style?.Tint ?? new Color("#ff666d");
        _aura.SelfModulate = new Color(tint.R, tint.G, tint.B, tint.A);
        _aura.Modulate = new Color(1f, 1f, 1f, alpha);
    }

    private float IdleAlpha() => Safe(Style?.IdleOpacity ?? .32f, .32f, 0f, 1f);

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
