// Complete independent travelling-wave levels; prices are Credits and falloff/penetration are fractions.
using System.Collections.Generic;

public static class ShockwaveTowerLevels
{
    private readonly record struct Row(int Level, int Cost, float Damage, float Radius, float Speed, float Interval,
        float Edge, float Penetration, DamageType Type, bool ShieldOverride, float Shield, bool HullOverride, float Hull, int AmmoCapacity, int AmmoPerShot)
    {
        // =========================================================
        // Radius uses shared RangePixels; only this weapon's optional wave capability carries timing/damage.
        public TowerLevel Create() => new(Level, Cost, new(Radius, 0, 0, 1, 0, 0, .15f,
            ShieldPenetration: Penetration, Weapon: new(Type, ShieldOverride, Shield, HullOverride, Hull),
            Shockwave: new(Damage, Speed, Interval, Edge), Ammunition: new(AmmoCapacity, AmmoPerShot)));
    }

    // Level | Cost | Damage | RadiusPx | SpeedPxS | IntervalS | Edge | Penetration | Type | ShieldOverride | Shield | HullOverride | Hull | AmmoCapacity | AmmoPerShot
    private static readonly Row[] Rows =
    {
        new(1, 350, 40, 200, 250, 4.0f, .25f, 0, DamageType.Energy, false, 1, false, 1, 400, 5),
        new(2, 225, 55, 240, 300, 3.5f, .25f, 0, DamageType.Energy, false, 1, false, 1, 400, 5),
    };

    // =========================================================
    // Add a consecutive full row to extend the tower; previous levels never contribute modifiers.
    public static List<TowerLevel> Create()
    {
        var levels = new List<TowerLevel>(Rows.Length);
        foreach (Row row in Rows) levels.Add(row.Create());
        return levels;
    }
}
