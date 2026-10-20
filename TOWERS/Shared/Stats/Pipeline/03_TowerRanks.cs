// Stage03 applies only the selected named rank's TOTAL bonuses to the complete purchased level.
using System;

public static class TowerRanksStage
{
    // =========================================================
    // Integer projectiles round nearest/ties-to-even; beams/waves retain fractional damage. HP uses the same total rank bonus.
    public static TowerStatBlock Apply(TowerStatBlock stats, TowerStatContext context)
    {
        if (!context.Rank.HasValue) return stats;
        TowerRankDefinition rank = context.Rank.Value;
        if (rank.DamageBonus == 0 && rank.RangeBonus == 0 && rank.HpBonus == 0 && rank.ProjectileSpreadReductionFraction == 0) return stats;
        double damage = 1 + (double)rank.DamageBonus, range = 1 + (double)rank.RangeBonus;
        return stats with
        {
            Handling = stats.Handling.HasValue ? stats.Handling.Value with { SpreadDegrees = stats.Handling.Value.SpreadDegrees * (1 - ProjectileHandlingStats.Fraction(rank.ProjectileSpreadReductionFraction)) } : null,
            DamagePerShot = (int)Math.Clamp(Math.Round(stats.DamagePerShot * damage), 0, int.MaxValue),
            RangePixels = Scale(stats.RangePixels, range),
            MaxHealth = Scale(stats.MaxHealth, 1 + (double)rank.HpBonus),
            Beam = stats.Beam.HasValue ? new BeamStats(Scale(stats.Beam.Value.DamagePerSecond, damage)) : null,
            Shockwave = stats.Shockwave.HasValue ? stats.Shockwave.Value with { DamagePerWave = Scale(stats.Shockwave.Value.DamagePerWave, damage) } : null
        };
    }

    // =========================================================
    // Bound overflow before conversion; Stage07 still owns the existing finite/minimum safety clamps.
    private static float Scale(float value, double multiplier) => (float)Math.Clamp(value * multiplier, -float.MaxValue, float.MaxValue);
}
