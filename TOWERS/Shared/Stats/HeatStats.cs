// Optional absolute per-level heat capability; null means the weapon has no heat system.
using System;
public readonly record struct HeatStats(float Capacity, float PerShot, float PerBeamSecond, float CoolingPerSecond, float RestartBelowFraction)
{
    // =========================================================
    // Invalid capabilities become absent; invalid rates cannot inject NaNs or invert cooling.
    public static HeatStats? Sanitize(HeatStats? value)
    {
        if (!value.HasValue || !float.IsFinite(value.Value.Capacity) || value.Value.Capacity <= 0) return null;
        HeatStats s = value.Value;
        return new(s.Capacity, Safe(s.PerShot), Safe(s.PerBeamSecond), Safe(s.CoolingPerSecond),
            float.IsFinite(s.RestartBelowFraction) ? Math.Clamp(s.RestartBelowFraction, 0, .99f) : .35f);
    }
    // =========================================================
    // Nonfinite rates are neutral instead of creating unbounded heat or cooling.
    private static float Safe(float value) => float.IsFinite(value) ? Math.Max(0, value) : 0;
}
