// Inspector identity/style only; all combat and purchase values live in ShockwaveTowerLevels.cs.
using Godot;

[GlobalClass]
public partial class ShockwaveTowerConfig : TowerConfig
{
    public override System.Collections.Generic.IReadOnlyList<TowerRankDefinition> Ranks => ShockwaveTowerRanks.Rows;
    [ExportGroup("Effects")]
    [Export] public ShockwaveStyle WaveStyle { get; set; }

    // =========================================================
    // Initialize complete C# levels, without target/projectile/aiming settings on an omnidirectional pulse.
    public ShockwaveTowerConfig()
    {
        DisplayName = "Shockwave Tower";
        Description = "Purple energy emitter: damage travels outward with each expanding wavefront.";
        Levels = ShockwaveTowerLevels.Create();
    }

    // =========================================================
    // Separate rows/recipes and live styles for every placed instance.
    public override ShockwaveTowerConfig CreateInstanceCopy()
    {
        var copy = (ShockwaveTowerConfig)base.CreateInstanceCopy();
        copy.WaveStyle = WaveStyle == null ? null : (ShockwaveStyle)WaveStyle.Duplicate(true);
        return copy;
    }

    // =========================================================
    // Live state and upgrade preview use the existing eight-stage coordinator.
    public override TowerFinalStats ResolveStats(int level) => TowerStatPipeline.Resolve(this, level);

    // =========================================================
    // The selected cached capability supplies float base damage and independent defense/penetration inputs.
    public DamageInfo CreateHit(TowerFinalStats stats, DamageSource source = null) => stats.Weapon.Value.CreateHit(stats.Shockwave.Value.DamagePerWave, stats.ShieldPenetration, source);

    // =========================================================
    // The Inspector reports wave timing/range/falloff, never a projectile speed or fictitious fire rate.
    public override string DescribeStats(TowerFinalStats final)
    {
        ShockwaveStats wave = final.Shockwave.Value;
        WeaponDamageStats response = final.Weapon.Value;
        var fallback = DamageResolver.DefaultProfile.Coefficients(response.Type);
        float shield = response.OverrideShield ? response.ShieldCoefficient : fallback.Shield;
        float hull = response.OverrideHull ? response.HullCoefficient : fallback.Hull;
        return $"Damage per wave: {wave.DamagePerWave:0.##}\nRange: {final.RangePixels:0.##} px\nExpansion speed: {wave.ExpansionSpeedPixelsPerSecond:0.##} px/s\nInterval: {wave.IntervalSeconds:0.##} s\nEdge damage: {wave.EdgeDamageFraction * 100:0.##}%\nShield Penetration: {final.ShieldPenetration * 100:0.##}%\nDamage type: {response.Type}\nShield: {shield:0.00}x / Hull: {hull:0.00}x";
    }

    // =========================================================
    // Every tunable wave input is compared from the same snapshots the paid upgrade will commit.
    public override string DescribeChanges(TowerFinalStats now, TowerFinalStats future)
    {
        ShockwaveStats a = now.Shockwave.Value, b = future.Shockwave.Value;
        string changes = DescribeWeaponChanges(now, future);
        if (a.DamagePerWave != b.DamagePerWave) changes += $"Damage per wave: {a.DamagePerWave:0.##} → {b.DamagePerWave:0.##}\n";
        if (now.RangePixels != future.RangePixels) changes += $"Range: {now.RangePixels:0.##} → {future.RangePixels:0.##} px\n";
        if (a.ExpansionSpeedPixelsPerSecond != b.ExpansionSpeedPixelsPerSecond) changes += $"Expansion speed: {a.ExpansionSpeedPixelsPerSecond:0.##} → {b.ExpansionSpeedPixelsPerSecond:0.##} px/s\n";
        if (a.IntervalSeconds != b.IntervalSeconds) changes += $"Interval: {a.IntervalSeconds:0.##} → {b.IntervalSeconds:0.##} s\n";
        if (a.EdgeDamageFraction != b.EdgeDamageFraction) changes += $"Edge damage: {a.EdgeDamageFraction * 100:0.##}% → {b.EdgeDamageFraction * 100:0.##}%\n";
        if (now.ShieldPenetration != future.ShieldPenetration) changes += $"Shield Penetration: {now.ShieldPenetration * 100:0.##}% → {future.ShieldPenetration * 100:0.##}%\n";
        return changes;
    }
}
