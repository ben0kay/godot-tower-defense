// Stage 08: publish a fresh immutable final snapshot for this resolution.
public static class FinalStatsStage
{
    // =========================================================
    // Values, not Resource references, reach gameplay consumers.
    public static TowerFinalStats Apply(TowerStatBlock stats, TowerStatContext context) =>
        new(stats.RangePixels, stats.ShotsPerSecond, stats.DamagePerShot, stats.ProjectileSpeed,
            stats.RotationSpeedDegrees, stats.AimToleranceDegrees, stats.TargetSearchInterval, stats.Beam, stats.ShieldPenetration, stats.Area, stats.Weapon, stats.Shockwave, stats.Ammunition, stats.Slow);
}
