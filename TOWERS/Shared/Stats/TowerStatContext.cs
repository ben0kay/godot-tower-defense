// Immutable inputs: baseline, complete selected purchased level and the current earned rank row.
public readonly record struct TowerStatContext(TowerStatBlock BaseStats, TowerStatBlock? SelectedLevel = null, TowerRankDefinition? Rank = null, HeatTiming? HeatTiming = null);
