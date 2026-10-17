// Authoritative absolute stats and credit prices. Row 1 builds; later rows buy that level.
using System.Collections.Generic;

public static class BlasterCannonLevels
{
    private readonly record struct Row(int Level, int Credits, double WorkSeconds, int Damage, float Rate, float Range, float Speed, float Turn, float Aim, float Search, float Penetration, float Radius, float Edge, DamageType Type, bool ShieldOverride, float Shield, bool HullOverride, float Hull, int AmmoCapacity, int AmmoPerShot, HeatStats? Heat = null)
    {
        // =========================================================
        // Adapt only this weapon's capabilities into the common eight-stage working snapshot.
        public TowerLevel Create() => new(Level, Credits, new TowerStatBlock(Range, Rate, Damage, Speed, Turn, Aim, Search, ShieldPenetration: Penetration, Area: new(Radius, Edge), Weapon: new(Type, ShieldOverride, Shield, HullOverride, Hull), Ammunition: new(AmmoCapacity, AmmoPerShot)) with { Heat = Heat }, WorkSeconds);
    }

    // Level | Cost | WorkSeconds | Damage | Rate | RangePx | SpeedPxS | TurnDegS | AimDeg | SearchS | Pen | RadiusPx | EdgeFrac | Type | ShieldOn | Shield | HullOn | Hull | AmmoCapacity | AmmoPerShot
    // Cost is Credits; Pen/EdgeFrac are fractions. Level 1 builds; other rows are upgrade prices.
    // Optional final named column: Heat: new(Capacity, PerShot, PerBeamSecond, CoolingPerSecond, RestartFraction).
    private static readonly Row[] Rows =
    {
        new(1, 300, 5, 60, 0.5f, 294, 500, 180, 12, 0.15f, 0, 80, 0.25f, DamageType.Explosive, false, 1, false, 1, 600, 3),
        new(2, 200, 6, 66, 0.5f, 304, 500, 180, 12, 0.15f, 0, 88, 0.25f, DamageType.Explosive, false, 1, false, 1, 600, 3),
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
