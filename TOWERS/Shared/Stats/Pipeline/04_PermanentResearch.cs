// Stage04: apply captured permanent tower research after the selected level and earned rank.
using System;

public static class PermanentResearchStage
{
    // =========================================================
    // Additive research percentages apply once; integer damage uses the same nearest/ties-to-even rule as ranks.
    public static TowerStatBlock Apply(TowerStatBlock stats, TowerStatContext context)
    {
        double percent = context.Research.DamagePercent;
        if (!double.IsFinite(percent) || percent < 0) throw new ArgumentOutOfRangeException(nameof(context), "Invalid research damage percentage.");
        if (percent == 0) return stats;
        double multiplier = 1 + percent / 100;
        return stats with
        {
            DamagePerShot = (int)Math.Clamp(Math.Round(stats.DamagePerShot * multiplier), 0, int.MaxValue),
            Beam = stats.Beam.HasValue ? new BeamStats(Scale(stats.Beam.Value.DamagePerSecond, multiplier)) : null,
            Shockwave = stats.Shockwave.HasValue ? stats.Shockwave.Value with { DamagePerWave = Scale(stats.Shockwave.Value.DamagePerWave, multiplier) } : null
        };
    }

    // =========================================================
    // Optional damage capabilities retain fractional values; Stage07 continues to own final safety bounds.
    private static float Scale(float value, double multiplier) => (float)Math.Clamp(value * multiplier, -float.MaxValue, float.MaxValue);
}
