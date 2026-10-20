// Lazy plain helper: event-only opacity, composed with status SelfModulate and untouched owner transforms.
using Godot;

public sealed class StealthVisual
{
    private static StealthVisualStyle _style;
    private readonly Sprite2D _sprite;
    private readonly DefenseBar _bar;
    private readonly Color _spriteTint, _barTint;
    private ShieldVisual _shield;
    private Color _shieldTint;
    // =========================================================
    // Ordinary enemies allocate no helper or style; authored alpha is preserved multiplicatively.
    public StealthVisual(Sprite2D sprite, DefenseBar bar)
    {
        _style ??= GD.Load<StealthVisualStyle>("res://FEEDBACK/Stealth/DefaultStealthVisualStyle.tres");
        _sprite = sprite; _bar = bar; _spriteTint = sprite?.Modulate ?? Colors.White; _barTint = bar?.Modulate ?? Colors.White;
    }
    // =========================================================
    // Shield feedback may be attached later by restoration; its own hit/break animation remains independent.
    public void Apply(bool hidden, ShieldVisual shield)
    {
        float configured = hidden ? _style.HiddenAlpha : _style.RevealedAlpha;
        float alpha = float.IsFinite(configured) ? Mathf.Clamp(configured, 0, 1) : hidden ? .3f : 1;
        Set(_sprite, _spriteTint, alpha);
        Set(_bar, _barTint, _style.DimDefenseFeedback ? alpha : 1);
        if (_shield != shield) { _shield = shield; _shieldTint = shield?.Modulate ?? Colors.White; }
        Set(_shield, _shieldTint, _style.DimDefenseFeedback ? alpha : 1);
    }
    // =========================================================
    // Reinitialization/teardown restores captured presentation without resetting Cryo/Stasis color channels.
    public void Reset() { Set(_sprite, _spriteTint, 1); Set(_bar, _barTint, 1); Set(_shield, _shieldTint, 1); }
    // =========================================================
    // Never dereference visuals already removed with their owner.
    private static void Set(CanvasItem item, Color tint, float alpha)
    { if (GodotObject.IsInstanceValid(item)) { tint.A *= alpha; item.Modulate = tint; } }
}
