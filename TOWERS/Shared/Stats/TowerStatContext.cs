// Immutable inputs: baseline, selected purchased level, earned rank, heat timing and captured research bonuses.
public readonly record struct TowerStatContext(TowerStatBlock BaseStats, TowerStatBlock? SelectedLevel = null, TowerRankDefinition? Rank = null, HeatTiming? HeatTiming = null, TowerResearchBonuses Research = default);
