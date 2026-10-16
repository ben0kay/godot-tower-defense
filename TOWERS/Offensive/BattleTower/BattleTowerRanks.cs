// Explicit earned rank totals for BattleTower; independent of the purchased *Levels.cs rows.
using System.Collections.Generic;
using static TowerRank;

public static class BattleTowerRanks
{
    // Rank       | Total XP | Damage bonus | Range bonus | Future HP bonus (inactive)
    public static IReadOnlyList<TowerRankDefinition> Rows { get; } = TowerRankController.Define(
        new(Standard,      0, 0.00f, 0.00f, 0.00f),
        new(Veteran,    1000, 0.02f, 0.01f, 0.02f),
        new(Enhanced,   3000, 0.04f, 0.02f, 0.04f),
        new(Superior,   7000, 0.06f, 0.03f, 0.06f),
        new(Legendary, 15000, 0.08f, 0.04f, 0.08f));
}
