// Stage 01 carries the baseline; Stage 02 substitutes one complete purchased level.
public readonly record struct TowerStatContext(TowerStatBlock BaseStats, TowerStatBlock? SelectedLevel = null);
