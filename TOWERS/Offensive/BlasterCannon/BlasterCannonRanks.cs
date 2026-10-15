// Explicit earned rank totals for BlasterCannon; independent of the purchased *Levels.cs rows.
using System.Collections.Generic;
using static TowerRank;

public static class BlasterCannonRanks
{
    // Rank       | Total XP | Damage bonus | Range bonus | Future HP bonus (inactive)
    public static IReadOnlyList<TowerRankDefinition> Rows { get; } = TowerRankController.Define(
        new(Standard,      0, 0.00f, 0.00f, 0.00f),
        new(Veteran,     100, 0.02f, 0.01f, 0.02f),
        new(Enhanced,    300, 0.04f, 0.02f, 0.04f),
        new(Superior,    700, 0.06f, 0.03f, 0.06f),
        new(Legendary,  1500, 0.08f, 0.04f, 0.08f));
}
