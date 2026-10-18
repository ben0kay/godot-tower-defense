// Stage 07: finite, safe combat values while preserving valid baseline balance and zero-rate firing.
using Godot;

public static class SafetyClampsStage
{
    // =========================================================
    // Nonfinite values resolve to safe minima; authored Resources are never changed.
    public static TowerStatBlock Apply(TowerStatBlock stats, TowerStatContext context) => stats with
    {
        Freeze = stats.Freeze.HasValue ? new FreezeStats(FiniteMinimum(stats.Freeze.Value.DurationSeconds, 0)) : null,
        MaxHealth = FiniteMinimum(stats.MaxHealth, 1),
        Heat = HeatStats.Sanitize(stats.Heat ?? context.HeatTiming?.Resolve(stats.ShotsPerSecond)),
        ShieldPenetration = Mathf.Min(1, FiniteMinimum(stats.ShieldPenetration, 0)),
        RangePixels = FiniteMinimum(stats.RangePixels, 0),
        ShotsPerSecond = FiniteMinimum(stats.ShotsPerSecond, 0),
        DamagePerShot = Mathf.Max(0, stats.DamagePerShot),
        ProjectileSpeed = FiniteMinimum(stats.ProjectileSpeed, 1),
        RotationSpeedDegrees = FiniteMinimum(stats.RotationSpeedDegrees, 0),
        AimToleranceDegrees = Mathf.Min(180, FiniteMinimum(stats.AimToleranceDegrees, 0)),
        TargetSearchInterval = FiniteMinimum(stats.TargetSearchInterval, 0.05f),
        Area = stats.Area.HasValue ? new AreaBlastStats(FiniteMinimum(stats.Area.Value.RadiusPixels, 0),
            Mathf.Min(1, FiniteMinimum(stats.Area.Value.EdgeDamageFraction, 0))) : null,
        Shockwave = stats.Shockwave.HasValue ? new ShockwaveStats(
            FiniteMinimum(stats.Shockwave.Value.DamagePerWave, 0),
            FiniteMinimum(stats.Shockwave.Value.ExpansionSpeedPixelsPerSecond, 1),
            FiniteMinimum(stats.Shockwave.Value.IntervalSeconds, .05f),
            Mathf.Min(1, FiniteMinimum(stats.Shockwave.Value.EdgeDamageFraction, 0))) : null,
        Ammunition = stats.Ammunition.HasValue ? new AmmunitionStats(Mathf.Max(0, stats.Ammunition.Value.Capacity),
            Mathf.Max(0, stats.Ammunition.Value.PerShot), FiniteMinimum(stats.Ammunition.Value.PerSecond, 0)) : null,
        Slow = stats.Slow.HasValue ? new SlowStats(Mathf.Min(.99f, FiniteMinimum(stats.Slow.Value.Fraction, 0))) : null,
        Beam = stats.Beam.HasValue ? new BeamStats(FiniteMinimum(stats.Beam.Value.DamagePerSecond, 0)) : null
    };

    // =========================================================
    // Reject NaN/infinity before applying a stat's supported lower bound.
    private static float FiniteMinimum(float value, float minimum) =>
        float.IsFinite(value) ? Mathf.Max(minimum, value) : minimum;
}
