// Authoritative absolute stats and credit prices. Row 1 builds; later rows buy that level.
using System.Collections.Generic;

public static class BattleTowerLevels
{
    private readonly record struct Row(int Level, int Credits, double WorkSeconds, int Damage, float Rate, float Range, float Speed, float Turn, float Aim, float Search, float Penetration, int AmmoCapacity, int AmmoPerShot, HeatStats? Heat = null, float MaxHealth = 100)
    {
        // =========================================================
        // Adapt only this weapon's capabilities into the common eight-stage working snapshot.
        public TowerLevel Create() => new(Level, Credits, new TowerStatBlock(Range, Rate, Damage, Speed, Turn, Aim, Search, ShieldPenetration: Penetration, Ammunition: new(AmmoCapacity, AmmoPerShot)) with { Heat = Heat, MaxHealth = MaxHealth }, WorkSeconds);
    }

    // Level | Cost | WorkSeconds | Damage | Rate | RangePx | SpeedPxS | TurnDegS | AimDeg | SearchS | Pen | AmmoCapacity | AmmoPerShot
    // Cost is Credits; Pen/EdgeFrac are fractions. Level 1 builds; other rows are upgrade prices.
    // Optional final named column: Heat: new(Capacity, PerShot, PerBeamSecond, CoolingPerSecond, RestartFraction).
    // Optional named MaxHealth column is absolute per level; 100 is provisional baseline balance.
    private static readonly Row[] Rows =
    {
        new(1, 200, 3, 30, 1.68f, 323.4f, 1150, 216, 12, 0.15f, 0.05f, 1000, 1),
        new(2, 150, 4, 33, 1.764f, 334.4f, 1150, 216, 12, 0.15f, 0.05f, 1000, 1),
        new(3, 250, 5, 36, 1.848f, 344.3f, 1150, 216, 12, 0.15f, 0.05f, 1000, 1),
    };

    // =========================================================
    // Each config receives independent runtime rows and recipes; defaults stay in this file alone.
    public static List<TowerLevel> Create()
    {
        var levels = new List<TowerLevel>(Rows.Length);
        foreach (Row row in Rows) levels.Add(row.Create());
        return levels;
    }
}
