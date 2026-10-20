// Absolute purchased levels; high single-hit power is balanced by slow rate and higher Credits cost.
using System.Collections.Generic;

public static class SniperTowerLevels
{
    private readonly record struct Row(int Level, int Credits, double WorkSeconds, int Damage, float Rate, float Range, float Speed, float Turn, int AmmoCapacity, int AmmoPerShot, HeatStats? Heat = null, float MaxHealth = 1000, float SpreadDegrees = 0)
    {
        // =========================================================
        // All levels share Kinetic damage/weapon behavior, with numeric snapshots replaced in full.
        public TowerLevel Create() => new(Level, Credits, new TowerStatBlock(Range, Rate, Damage, Speed, Turn, 8f, .18f,
            ShieldPenetration: .15f, Ammunition: new(AmmoCapacity, AmmoPerShot)) with { Heat = Heat, MaxHealth = MaxHealth, Handling = new(SpreadDegrees) }, WorkSeconds);
    }

    // Level | Cost | WorkSeconds | Damage | TotalShotsPerSecond | RangePx | ProjectilePxS | TurnDegS | AmmoCapacity | AmmoPerBullet
    // Optional final named column: Heat: new(Capacity, PerShot, PerBeamSecond, CoolingPerSecond, RestartFraction).
    // Optional named MaxHealth column is absolute per level; Level 1 is 1,000 HP; each paid tier adds 5% to the previous absolute HP.
    private static readonly Row[] Rows =
    {
        new(1, 325, 4, 80, .35f, 566.5f, 2990, 162, 600, 1, SpreadDegrees: 0.5f),
        new(2, 225, 5, 88, .37f, 576.4f, 2990, 162, 600, 1, MaxHealth: 1050f, SpreadDegrees: 0.4f),
        new(3, 325, 6, 96, .40f, 587.4f, 2990, 162, 600, 1, MaxHealth: 1102.5f, SpreadDegrees: 0.25f),
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
