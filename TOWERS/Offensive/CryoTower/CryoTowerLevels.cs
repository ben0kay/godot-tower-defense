// Complete independent purchased levels: permanent slowdown strength is an explicit pipeline capability.
using System.Collections.Generic;

public static class CryoTowerLevels
{
    private readonly record struct Row(int Level, int Credits, double WorkSeconds, int Damage, float Rate,
        float Range, float Speed, float Turn, float Slow, int AmmoCapacity, int AmmoPerShot, HeatStats? Heat = null, float MaxHealth = 100)
    {
        // =========================================================
        // Rank modifies damage/range later; Slow remains the selected row's absolute strength.
        public TowerLevel Create() => new(Level, Credits, new TowerStatBlock(Range, Rate, Damage, Speed, Turn, 8, .15f,
            Weapon: new(DamageType.Energy, false, 1, false, 1), Ammunition: new(AmmoCapacity, AmmoPerShot),
            Slow: new(Slow)) with { Heat = Heat, MaxHealth = MaxHealth }, WorkSeconds);
    }
    // Level | Cost | WorkSeconds | Damage | Shots/s | RangePx | ProjectilePx/s | TurnDeg/s | Slow | AmmoCapacity | Ammo/shot
    // Optional final named column: Heat: new(Capacity, PerShot, PerBeamSecond, CoolingPerSecond, RestartFraction).
    // Optional named MaxHealth column is absolute per level; 100 is provisional baseline balance.
    private static readonly Row[] Rows =
    {
        new(1, 250, 4, 10, .50f, 239, 650, 180, .33f, 600, 1),
        new(2, 175, 5, 11, .52f, 248, 650, 180, .33f, 600, 1),
        new(3, 250, 6, 12, .54f, 258, 650, 180, .33f, 600, 1),
    };
    // =========================================================
    // Changing one purchased level never modifies another level or another placed tower.
    public static List<TowerLevel> Create()
    {
        List<TowerLevel> result = new(Rows.Length);
        foreach (Row row in Rows) result.Add(row.Create());
        return result;
    }
}
