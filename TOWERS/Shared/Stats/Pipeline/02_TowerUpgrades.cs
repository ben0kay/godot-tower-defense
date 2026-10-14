// Stage 02: cumulative purchased tiers; combine operations per stat within this stage.
using Godot;
using System;

public static class TowerUpgradesStage
{
    // =========================================================
    // Recompute from the incoming base, never from an old final result or by mutating Config.
    public static TowerStatBlock Apply(TowerStatBlock stats, TowerStatContext context)
    {
        if (context.UpgradeModifiers.IsDefaultOrEmpty) return stats;
        return stats with
        {
            RangePixels = Resolve(stats.RangePixels, TowerStatId.Range, context),
            ShotsPerSecond = Resolve(stats.ShotsPerSecond, TowerStatId.FireRate, context),
            DamagePerShot = (int)Math.Clamp(Math.Round(Resolve(stats.DamagePerShot, TowerStatId.Damage, context)), 0, int.MaxValue),
            ProjectileSpeed = Resolve(stats.ProjectileSpeed, TowerStatId.ProjectileSpeed, context),
            RotationSpeedDegrees = Resolve(stats.RotationSpeedDegrees, TowerStatId.RotationSpeed, context),
            AimToleranceDegrees = Resolve(stats.AimToleranceDegrees, TowerStatId.AimTolerance, context),
            TargetSearchInterval = Resolve(stats.TargetSearchInterval, TowerStatId.SearchInterval, context)
        };
    }

    // =========================================================
    // Flat and additive-percent bonuses sum; explicit multipliers multiply, independent of entry order.
    private static float Resolve(float input, TowerStatId stat, TowerStatContext context)
    {
        double flat = 0, percent = 0, multiplier = 1;
        foreach (TowerStatModifier entry in context.UpgradeModifiers)
        {
            if (entry.Stat != stat) continue;
            switch (entry.Operation)
            {
                case TowerStatOperation.Flat: flat += entry.Amount; break;
                case TowerStatOperation.AdditivePercent: percent += entry.Amount; break;
                case TowerStatOperation.Multiplier: multiplier *= entry.Amount; break;
            }
        }
        double value = (input + flat) * (1 + percent) * multiplier;
        return double.IsFinite(value) && Math.Abs(value) <= float.MaxValue ? (float)value : 0;
    }
}
