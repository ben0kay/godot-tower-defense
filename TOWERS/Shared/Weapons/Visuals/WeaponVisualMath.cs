// Finite presentation-only sanitizers; no combat calculations belong in visual styles.
using Godot;
public static class WeaponVisualMath
{
    // =========================================================
    // Malformed authored numbers fall back to a safe minimum.
    public static float Safe(float value, float minimum, float maximum) => float.IsFinite(value) ? Mathf.Clamp(value, minimum, maximum) : minimum;
    // =========================================================
    // Colors are bounded independently, including opacity; no NaN reaches a native draw command.
    public static Color Color(Color value) => new(Safe(value.R, 0, 1), Safe(value.G, 0, 1), Safe(value.B, 0, 1), Safe(value.A, 0, 1));
}
