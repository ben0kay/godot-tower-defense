// Absolute rocket purchase rows; total shots/s alternate between the two barrels.
using System.Collections.Generic;

public static class RocketTowerLevels
{
    private readonly record struct Row(int Level, int Credits, double WorkSeconds, int Damage, float Rate, float Range,
        float Speed, float Radius, float Edge, int AmmoCapacity, int AmmoPerShot, float MaxHealth = 100)
    {
        // =========================================================
        // Reuse typed Explosive damage and the same AreaBlastStats snapshot as Blaster Cannon.
        public TowerLevel Create() => new(Level, Credits, new TowerStatBlock(Range, Rate, Damage, Speed, 264, 12, .12f,
            Area: new(Radius, Edge), Weapon: new(DamageType.Explosive, false, 1, false, 1),
            Ammunition: new(AmmoCapacity, AmmoPerShot), MaxHealth: MaxHealth), WorkSeconds);
    }

    // Level | Credits | BuildSeconds | Damage | TotalRockets/s | RangePx | RocketSpeedPx/s | SplashRadiusPx | EdgeDamage | AmmoCapacity | AmmoPerRocket
    private static readonly Row[] Rows =
    {
        new(1, 350, 5, 12, 2.0f, 440, 575, 42, 0.25f, 800, 1),
        new(2, 230, 6, 14, 2.2f, 462, 598, 45, 0.25f, 800, 1),
        new(3, 320, 7, 16, 2.4f, 484, 621, 48, 0.25f, 800, 1),
    };

    // =========================================================
    // Every tower receives independent row and ammunition values.
    public static List<TowerLevel> Create()
    {
        var result = new List<TowerLevel>(Rows.Length);
        foreach (Row row in Rows) result.Add(row.Create());
        return result;
    }
}
