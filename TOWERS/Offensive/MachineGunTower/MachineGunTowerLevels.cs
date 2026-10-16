// Complete absolute purchased levels; gentle increases to bullet damage, cadence and range.
using System.Collections.Generic;

public static class MachineGunTowerLevels
{
    private readonly record struct Row(int Level, int Credits, int Damage, float Rate, float Range, float Speed, float Turn, int AmmoCapacity, int AmmoPerShot)
    {
        // =========================================================
        // One bullet per sequential attack; the two muzzles never double total authored fire rate.
        public TowerLevel Create() => new(Level, Credits, new(Range, Rate, Damage, Speed, Turn, 12f, .12f,
            ShieldPenetration: 0f, Ammunition: new(AmmoCapacity, AmmoPerShot)));
    }

    // Level | Cost | Damage | TotalBulletsPerSecond | RangePx | ProjectilePxS | TurnDegS | AmmoCapacity | AmmoPerBullet
    private static readonly Row[] Rows =
    {
        new(1, 300, 7, 6.0f, 208, 850, 240, 1200, 1),
        new(2, 200, 8, 6.2f, 216, 850, 240, 1200, 1),
        new(3, 300, 9, 6.4f, 224, 850, 240, 1200, 1),
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
