// Single explicit coordinator for all eight stat stages; purchased tiers are active; ranks/research/conditions/effects remain neutral.
using System;
using System.Collections.Immutable;

public static class TowerStatPipeline
{
    // =========================================================
    // Copy current authored inputs once; priority, tracking policy, identity and costs stay outside.
    public static TowerFinalStats Resolve(BattleTowerConfig config, TowerStatTrace trace = null)
        => Resolve(config, 1, trace);

    // =========================================================
    // Snapshot only the purchased tier operations; later stages stay neutral.
    public static TowerFinalStats Resolve(BattleTowerConfig config, int level, TowerStatTrace trace = null)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
        var modifiers = ImmutableArray.CreateBuilder<TowerStatModifier>();
        if (level > 1)
        {
            TowerUpgradeController.Validate(config.UpgradeTiers);
            if (level > config.UpgradeTiers.Count + 1) throw new ArgumentOutOfRangeException(nameof(level));
            for (int i = 0; i < level - 1; i++)
                foreach (TowerUpgradeModifier entry in config.UpgradeTiers[i].Modifiers)
                    modifiers.Add(new TowerStatModifier(entry.Stat, entry.Operation, entry.Amount, $"tier:{i + 2}"));
        }
        TowerStatContext context = new(new TowerStatBlock(config.RangePixels, config.ShotsPerSecond,
            config.DamagePerShot, config.ProjectileSpeed, config.RotationSpeedDegrees,
            config.AimToleranceDegrees, config.TargetSearchInterval), modifiers.ToImmutable());
        return Resolve(context, trace);
    }

    // =========================================================
    // The complete execution order is visible here; no competing pipeline lives in a tower.
    public static TowerFinalStats Resolve(TowerStatContext context, TowerStatTrace trace = null)
    {
        trace?.Clear();
        TowerStatBlock stats = BaseStatsStage.Apply(context); trace?.Add("01 Base Stats", stats);
        stats = TowerUpgradesStage.Apply(stats, context); trace?.Add("02 Tower Upgrades", stats);
        stats = TowerRanksStage.Apply(stats, context); trace?.Add("03 Tower Ranks", stats);
        stats = PermanentResearchStage.Apply(stats, context); trace?.Add("04 Permanent Research", stats);
        stats = LevelModifiersStage.Apply(stats, context); trace?.Add("05 Level Modifiers", stats);
        stats = ActiveEffectsStage.Apply(stats, context); trace?.Add("06 Active Effects", stats);
        stats = SafetyClampsStage.Apply(stats, context); trace?.Add("07 Safety Clamps", stats);
        TowerFinalStats final = FinalStatsStage.Apply(stats, context); trace?.Add("08 Final Stats", stats);
        return final;
    }
}
