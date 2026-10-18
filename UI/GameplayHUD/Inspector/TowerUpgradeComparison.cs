// Typed presentational comparison of two already-resolved, rank-aware tower snapshots.
// No stat pipeline, wallet, or gameplay changes occur when building Inspector rows.
using System;
using System.Collections.Generic;
using System.Globalization;

public enum TowerUpgradeGain { Neutral, Better, Worse }
public readonly record struct TowerUpgradeRow(string Name, string Current, string Next, string Gain, TowerUpgradeGain Tone);

public static class TowerUpgradeComparison
{
    // =========================================================
    // Only show attributes actually supported by the weapon's capabilities.
    public static IReadOnlyList<TowerUpgradeRow> Build(TowerFinalStats current, TowerFinalStats next)
    {
        List<TowerUpgradeRow> rows = new();
        if (current == null || next == null) return rows;
        if (current.MaxHealth != next.MaxHealth) Add(rows, "Max HP", current.MaxHealth, next.MaxHealth, 1);
        bool beam = current.Beam.HasValue && next.Beam.HasValue;
        bool wave = current.Shockwave.HasValue && next.Shockwave.HasValue;
        bool projectile = !beam && !wave;
        if (beam) Add(rows, "DPS", current.Beam.Value.DamagePerSecond, next.Beam.Value.DamagePerSecond, 2);
        else if (wave) Add(rows, "Wave Damage", current.Shockwave.Value.DamagePerWave, next.Shockwave.Value.DamagePerWave, 2);
        else if (projectile) Add(rows, "Damage", current.DamagePerShot, next.DamagePerShot, 0);
        Add(rows, "Range (px)", current.RangePixels, next.RangePixels, 1);
        if (projectile)
        {
            Add(rows, "Fire Rate (/s)", current.ShotsPerSecond, next.ShotsPerSecond, 2);
            Add(rows, "Projectile (px/s)", current.ProjectileSpeed, next.ProjectileSpeed, 1);
        }
        if (current.Slow.HasValue && next.Slow.HasValue) Add(rows, "Slow (%)", current.Slow.Value.Fraction * 100d, next.Slow.Value.Fraction * 100d, 1);
        if (current.Area.HasValue && next.Area.HasValue)
        {
            Add(rows, "Blast Radius (px)", current.Area.Value.RadiusPixels, next.Area.Value.RadiusPixels, 1);
            Add(rows, "Blast Edge (%)", current.Area.Value.EdgeDamageFraction * 100d, next.Area.Value.EdgeDamageFraction * 100d, 1);
        }
        if (wave)
        {
            Add(rows, "Wave Speed (px/s)", current.Shockwave.Value.ExpansionSpeedPixelsPerSecond, next.Shockwave.Value.ExpansionSpeedPixelsPerSecond, 1);
            Add(rows, "Wave Interval (s)", current.Shockwave.Value.IntervalSeconds, next.Shockwave.Value.IntervalSeconds, 2, false);
            Add(rows, "Wave Edge (%)", current.Shockwave.Value.EdgeDamageFraction * 100d, next.Shockwave.Value.EdgeDamageFraction * 100d, 1);
        }
        Add(rows, "Shield Pen. (%)", current.ShieldPenetration * 100d, next.ShieldPenetration * 100d, 1);
        if (current.Weapon.HasValue && next.Weapon.HasValue)
        {
            WeaponDamageStats before = current.Weapon.Value, after = next.Weapon.Value;
            if (before.Type != after.Type)
                rows.Add(new("Damage Type", before.Type.ToString(), after.Type.ToString(), "Changed", TowerUpgradeGain.Neutral));
            var oldDefaults = DamageResolver.DefaultProfile.Coefficients(before.Type);
            var newDefaults = DamageResolver.DefaultProfile.Coefficients(after.Type);
            double oldShield = before.OverrideShield ? before.ShieldCoefficient : oldDefaults.Shield;
            double newShield = after.OverrideShield ? after.ShieldCoefficient : newDefaults.Shield;
            double oldHull = before.OverrideHull ? before.HullCoefficient : oldDefaults.Hull;
            double newHull = after.OverrideHull ? after.HullCoefficient : newDefaults.Hull;
            if (Math.Abs(oldShield - newShield) > 0.001)
                Add(rows, "Shield Mult. (x)", oldShield, newShield, 2);
            if (Math.Abs(oldHull - newHull) > 0.001)
                Add(rows, "Hull Mult. (x)", oldHull, newHull, 2);
        }
        if (current.Ammunition.HasValue && next.Ammunition.HasValue)
        {
            AmmunitionStats a = current.Ammunition.Value, b = next.Ammunition.Value;
            if (a.PerShot > 0 || b.PerShot > 0) Add(rows, "Ammo / Shot", a.PerShot, b.PerShot, 0, false);
            if (a.PerSecond > 0 || b.PerSecond > 0) Add(rows, "Ammo / s", a.PerSecond, b.PerSecond, 2, false);
        }
        return rows;
    }

    // =========================================================
    // Delta uses the precision of the two visible values; unchanged renders an em dash.
    private static void Add(List<TowerUpgradeRow> rows, string name, double a, double b, int precision, bool higherIsBetter = true)
    {
        if (!double.IsFinite(a) || !double.IsFinite(b)) return;
        double left = Math.Round(a, precision, MidpointRounding.AwayFromZero);
        double right = Math.Round(b, precision, MidpointRounding.AwayFromZero);
        double delta = Math.Round(right - left, precision, MidpointRounding.AwayFromZero);
        string format = precision switch { 0 => "0", 1 => "0.#", _ => "0.##" };
        string gain = delta == 0 ? "—" : "(" + (delta > 0 ? "+" : "−")
            + Math.Abs(delta).ToString(format, CultureInfo.InvariantCulture) + ")";
        TowerUpgradeGain tone = delta == 0 ? TowerUpgradeGain.Neutral
            : ((delta > 0) == higherIsBetter ? TowerUpgradeGain.Better : TowerUpgradeGain.Worse);
        rows.Add(new(name, left.ToString(format, CultureInfo.InvariantCulture),
            right.ToString(format, CultureInfo.InvariantCulture), gain, tone));
    }
}
