// Absolute purchased levels; high single-hit power is balanced by slow rate and higher Credits cost.
using System.Collections.Generic;

public static class SniperTowerLevels
{
    private readonly record struct Row(int Level, int Credits, double WorkSeconds, int Damage, float Rate, float Range, float Speed, float Turn, int AmmoCapacity, int AmmoPerShot, HeatStats? Heat = null)
    {
        // =========================================================
        // All levels share Kinetic damage/weapon behavior, with numeric snapshots replaced in full.
        public TowerLevel Create() => new(Level, Credits, new TowerStatBlock(Range, Rate, Damage, Speed, Turn, 8f, .18f,
            ShieldPenetration: .15f, Ammunition: new(AmmoCapacity, AmmoPerShot)) with { Heat = Heat }, WorkSeconds);
    }

    // Level | Cost | WorkSeconds | Damage | TotalShotsPerSecond | RangePx | ProjectilePxS | TurnDegS | AmmoCapacity | AmmoPerBullet
    // Optional final named column: Heat: new(Capacity, PerShot, PerBeamSecond, CoolingPerSecond, RestartFraction).
    private static readonly Row[] Rows =
    {
        new(1, 325, 4, 80, .35f, 515, 1300, 135, 600, 1),
        new(2, 225, 5, 88, .37f, 524, 1300, 135, 600, 1),
        new(3, 325, 6, 96, .40f, 534, 1300, 135, 600, 1),
    };

    // =========================================================
    // Private per-instance level copies use the same TowerLevel contract as every offensive tower.
    public static List<TowerLevel> Create()
    {
        List<TowerLevel> result = new(Rows.Length);
        foreach (Row row in Rows) result.Add(row.Create());
        return result;
    }
}
