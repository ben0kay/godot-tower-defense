// Complete independent purchased levels: temporary freeze duration is an explicit pipeline capability.
using System.Collections.Generic;

public static class StasisTowerLevels
{
    #region Stasis Definition

    private readonly record struct Row(int Level, int Credits, double WorkSeconds, int Damage, float Rate,
        float Range, float Speed, float Turn, float Duration, int AmmoCapacity, int AmmoPerShot, HeatStats? Heat = null, float MaxHealth = 100)
    {
        // =========================================================
        // Rank modifies damage/range later; Freeze remains the selected row's absolute duration.
        public TowerLevel Create() => new(Level, Credits, new TowerStatBlock(Range, Rate, Damage, Speed, Turn, 8, .15f,
            Weapon: new(DamageType.Energy, false, 1, false, 1), Ammunition: new(AmmoCapacity, AmmoPerShot),
            Freeze: new(Duration)) with { Heat = Heat, MaxHealth = MaxHealth }, WorkSeconds);
    }
    // Level | Cost | WorkSeconds | Damage | Shots/s | RangePx | ProjectilePx/s | TurnDeg/s | Freeze duration seconds | AmmoCapacity | Ammo/shot
    // Optional final named column: Heat: new(Capacity, PerShot, PerBeamSecond, CoolingPerSecond, RestartFraction).
    // Optional named MaxHealth column is absolute per level; 100 is provisional baseline balance.
    private static readonly Row[] Rows =
    {
        new(1, 350, 4, 10, .20f, 239, 650, 180, 3f, 600, 1),
        new(2, 225, 5, 11, .21f, 248, 650, 180, 3.25f, 600, 1),
        new(3, 300, 6, 12, .22f, 258, 650, 180, 3.5f, 600, 1),
    };
    // =========================================================
    // Changing one purchased level never modifies another level or another placed tower.
    public static List<TowerLevel> Create()
    {
        List<TowerLevel> result = new(Rows.Length);
        foreach (Row row in Rows) result.Add(row.Create());
        return result;
    }
    #endregion
}
