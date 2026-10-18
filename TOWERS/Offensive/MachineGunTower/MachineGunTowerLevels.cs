// Complete absolute purchased levels; gentle increases to bullet damage, cadence and range.
using System.Collections.Generic;

public static class MachineGunTowerLevels
{
    private readonly record struct Row(int Level, int Credits, double WorkSeconds, int Damage, float Rate, float Range, float Speed, float Turn, int AmmoCapacity, int AmmoPerShot, HeatStats? Heat = null, float MaxHealth = 100)
    {
        // =========================================================
        // One bullet per sequential attack; the two muzzles never double total authored fire rate.
        public TowerLevel Create() => new(Level, Credits, new TowerStatBlock(Range, Rate, Damage, Speed, Turn, 12f, .12f,
            ShieldPenetration: 0f, Ammunition: new(AmmoCapacity, AmmoPerShot)) with { Heat = Heat, MaxHealth = MaxHealth }, WorkSeconds);
    }

    // Level | Cost | WorkSeconds | Damage | TotalBulletsPerSecond | RangePx | ProjectilePxS | TurnDegS | AmmoCapacity | AmmoPerBullet
    // Optional final named column: Heat: new(Capacity, PerShot, PerBeamSecond, CoolingPerSecond, RestartFraction).
    // Optional named MaxHealth column is absolute per level; 100 is provisional baseline balance.
    private static readonly Row[] Rows =
    {
        new(1, 300, 4, 7, 14.4f, 262.9f, 2443.75f, 288, 1200, 1),
        new(2, 200, 5, 8, 14.88f, 272.8f, 2443.75f, 288, 1200, 1),
        new(3, 300, 6, 9, 15.36f, 283.8f, 2443.75f, 288, 1200, 1),
    };

    // =========================================================
    // Each placed tower receives independent authored level snapshots.
    public static List<TowerLevel> Create()
    {
        List<TowerLevel> result = new(Rows.Length);
        foreach (Row row in Rows) result.Add(row.Create());
        return result;
    }
}
