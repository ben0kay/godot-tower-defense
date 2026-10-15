// Single explicit coordinator for all eight stat stages; explicit purchased levels are active; earned ranks are active; research/conditions/effects remain neutral.
using System;

public static class TowerStatPipeline
{
    // =========================================================
    // Battle callers and tooling retain the shared coordinator entry point.
    public static TowerFinalStats Resolve(BattleTowerConfig config, TowerStatTrace trace = null) => Resolve(config, 1, trace);

    // =========================================================
    // Select exactly one row; resolving level 3 never reads level 2's combat values.
    public static TowerFinalStats Resolve(TowerConfig config, int level, TowerStatTrace trace = null, TowerRankDefinition? rank = null)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (level < 1 || level > config.Levels.Count) throw new ArgumentOutOfRangeException(nameof(level));
        if (level > 1) TowerUpgradeController.Validate(config.Levels);
        return Resolve(new TowerStatContext(config.Levels[0].Stats, config.Levels[level - 1].Stats, rank), trace);
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
