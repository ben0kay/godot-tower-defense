// Authoritative absolute stats and credit prices. Row 1 builds; later rows buy that level.
using System.Collections.Generic;

public static class BattleTowerLevels
{
    private readonly record struct Row(int Level, int Credits, int Damage, float Rate, float Range, float Speed, float Turn, float Aim, float Search, float Penetration, int AmmoCapacity, int AmmoPerShot)
    {
        // =========================================================
        // Adapt only this weapon's capabilities into the common eight-stage working snapshot.
        public TowerLevel Create() => new(Level, Credits, new(Range, Rate, Damage, Speed, Turn, Aim, Search, ShieldPenetration: Penetration, Ammunition: new(AmmoCapacity, AmmoPerShot)));
    }

    // Level | Cost | Damage | Rate | RangePx | SpeedPxS | TurnDegS | AimDeg | SearchS | Pen | AmmoCapacity | AmmoPerShot
    // Cost is Credits; Pen/EdgeFrac are fractions. Level 1 builds; other rows are upgrade prices.
    private static readonly Row[] Rows =
    {
        new(1, 200, 30, 1.0f, 256, 800, 180, 12, 0.15f, 0.05f, 1000, 1),
        new(2, 150, 33, 1.05f, 264, 800, 180, 12, 0.15f, 0.05f, 1000, 1),
        new(3, 250, 36, 1.1f, 272, 800, 180, 12, 0.15f, 0.05f, 1000, 1),
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
