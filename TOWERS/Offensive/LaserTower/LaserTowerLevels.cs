// Authoritative absolute stats and credit prices. Row 1 builds; later rows buy that level.
using System.Collections.Generic;

public static class LaserTowerLevels
{
    private readonly record struct Row(int Level, int Credits, double WorkSeconds, float Dps, float Range, float Turn, float Aim, float Search, float Penetration, DamageType Type, bool ShieldOverride, float Shield, bool HullOverride, float Hull, int AmmoCapacity, float AmmoPerSecond, HeatStats? Heat = null, float MaxHealth = 1000)
    {
        // =========================================================
        // Adapt only this weapon's capabilities into the common eight-stage working snapshot.
        public TowerLevel Create() => new(Level, Credits, new TowerStatBlock(Range, 0, 0, 1, Turn, Aim, Search, new BeamStats(Dps), Penetration, Weapon: new(Type, ShieldOverride, Shield, HullOverride, Hull), Ammunition: new(AmmoCapacity, 0, AmmoPerSecond)) with { Heat = Heat, MaxHealth = MaxHealth }, WorkSeconds);
    }

    // Level | Cost | WorkSeconds | Dps | RangePx | TurnDegS | AimDeg | SearchS | Pen | Type | ShieldOn | Shield | HullOn | Hull | AmmoCapacity | AmmoPerSecond
    // Cost is Credits; Pen/EdgeFrac are fractions. Level 1 builds; other rows are upgrade prices.
    // Optional final named column: Heat: new(Capacity, PerShot, PerBeamSecond, CoolingPerSecond, RestartFraction).
    // Optional named MaxHealth column is absolute per level; Level 1 is 1,000 HP; each paid tier adds 5% to the previous absolute HP.
    private static readonly Row[] Rows =
    {
        new(1, 250, 4, 12, 323.4f, 216, 12, 0.15f, 0, DamageType.Energy, true, 6, true, 0.15f, 1000, 2f),
        new(2, 175, 5, 14, 334.4f, 216, 12, 0.15f, 0, DamageType.Energy, true, 6, true, 0.15f, 1000, 2f, MaxHealth: 1050f),
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
