// Stage 07: finite, safe combat values while preserving valid baseline balance and zero-rate firing.
using Godot;

public static class SafetyClampsStage
{
    // =========================================================
    // Nonfinite values resolve to safe minima; authored Resources are never changed.
    public static TowerStatBlock Apply(TowerStatBlock stats, TowerStatContext context) => stats with
    {
        RangePixels = FiniteMinimum(stats.RangePixels, 0),
        ShotsPerSecond = FiniteMinimum(stats.ShotsPerSecond, 0),
        DamagePerShot = Mathf.Max(0, stats.DamagePerShot),
        ProjectileSpeed = FiniteMinimum(stats.ProjectileSpeed, 1),
        RotationSpeedDegrees = FiniteMinimum(stats.RotationSpeedDegrees, 0),
        AimToleranceDegrees = Mathf.Min(180, FiniteMinimum(stats.AimToleranceDegrees, 0)),
        TargetSearchInterval = FiniteMinimum(stats.TargetSearchInterval, 0.05f)
    };

    // =========================================================
    // Reject NaN/infinity before applying a stat's supported lower bound.
    private static float FiniteMinimum(float value, float minimum) =>
        float.IsFinite(value) ? Mathf.Max(minimum, value) : minimum;
}
