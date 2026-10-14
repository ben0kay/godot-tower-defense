// Single explicit coordinator for all eight stat stages; future stages remain neutral in Phase 3A.
using System;

public static class TowerStatPipeline
{
    // =========================================================
    // Copy current authored inputs once; priority, tracking policy, identity and costs stay outside.
    public static TowerFinalStats Resolve(BattleTowerConfig config, TowerStatTrace trace = null)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        TowerStatContext context = new(new TowerStatBlock(config.RangePixels, config.ShotsPerSecond,
            config.DamagePerShot, config.ProjectileSpeed, config.RotationSpeedDegrees,
            config.AimToleranceDegrees, config.TargetSearchInterval));
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
