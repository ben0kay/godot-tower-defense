// Rocket Tower uses the existing five-rank earned-XP curve independently of paid levels.
using System.Collections.Generic;
using static TowerRank;

public static class RocketTowerRanks
{
    public static IReadOnlyList<TowerRankDefinition> Rows { get; } = TowerRankController.Define(
        new(Standard,      0, 0.00f, 0.00f, 0.00f, ProjectileSpreadReductionFraction: 0f),
        new(Veteran,    1000, 0.02f, 0.01f, 0.02f, ProjectileSpreadReductionFraction: 0.05f),
        new(Enhanced,   3000, 0.04f, 0.02f, 0.04f, ProjectileSpreadReductionFraction: 0.1f),
        new(Superior,   7000, 0.06f, 0.03f, 0.06f, ProjectileSpreadReductionFraction: 0.15f),
        new(Legendary, 15000, 0.08f, 0.04f, 0.08f, ProjectileSpreadReductionFraction: 0.2f));
}
