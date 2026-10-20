// Complete independent travelling-wave levels; prices are Credits and falloff/penetration are fractions.
using System.Collections.Generic;

public static class ShockwaveTowerLevels
{
    private readonly record struct Row(int Level, int Cost, double WorkSeconds, float Damage, float Radius, float Speed, float Interval,
        float Edge, float Penetration, DamageType Type, bool ShieldOverride, float Shield, bool HullOverride, float Hull, int AmmoCapacity, int AmmoPerShot, HeatStats? Heat = null, float MaxHealth = 1000)
    {
        // =========================================================
        // Radius uses shared RangePixels; pulse expansion speed is independent of projectile speed.
        public TowerLevel Create() => new(Level, Cost, new TowerStatBlock(Radius, 0, 0, 1, 0, 0, .15f,
            ShieldPenetration: Penetration, Weapon: new(Type, ShieldOverride, Shield, HullOverride, Hull),
            Shockwave: new(Damage, Speed, Interval, Edge), Ammunition: new(AmmoCapacity, AmmoPerShot)) with { Heat = Heat, MaxHealth = MaxHealth }, WorkSeconds);
    }

    // Level | Cost | WorkSeconds | Damage | RadiusPx | SpeedPxS | IntervalS | Edge | Penetration | Type | ShieldOverride | Shield | HullOverride | Hull | AmmoCapacity | AmmoPerShot
    // Optional final named column: Heat: new(Capacity, PerShot, PerBeamSecond, CoolingPerSecond, RestartFraction).
    // Optional named MaxHealth column is absolute per level; Level 1 is 1,000 HP; each paid tier adds 5% to the previous absolute HP.
    private static readonly Row[] Rows =
    {
        new(1, 350, 5, 40, 253, 250, 4.0f, .25f, 0, DamageType.Energy, false, 1, false, 1, 400, 5),
        new(2, 225, 6, 44, 262.9f, 260, 3.9f, .25f, 0, DamageType.Energy, false, 1, false, 1, 400, 5, MaxHealth: 1050f),
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
