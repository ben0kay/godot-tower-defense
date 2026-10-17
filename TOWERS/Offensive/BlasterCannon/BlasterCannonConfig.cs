// Non-level Inspector policy and presentation; all level defaults live in BlasterCannonLevels.cs.
using Godot;

[GlobalClass]
public partial class BlasterCannonConfig : TowerConfig, ITowerTargetingPolicy
{
    public override System.Collections.Generic.IReadOnlyList<TowerRankDefinition> Ranks => BlasterCannonRanks.Rows;
    // =========================================================
    // C# tables are the sole authored source of combat values and construction/upgrade prices.
    public BlasterCannonConfig()
    {
        DisplayName = "Blaster Cannon";
        Description = "Explosive cannon: instant circular blast with weapon-owned edge retention.";
        Levels = BlasterCannonLevels.Create();
    }

    [ExportGroup("Weapon Visuals")]
    [Export] public MuzzleFlashStyle MuzzleFlash { get; set; }

    [ExportGroup("Effects")]
    [Export] public ShockwaveStyle ImpactStyle { get; set; }

    [ExportGroup("Aiming")]
    [Export] public bool RotateWhileTracking { get; set; } = true;
    [ExportGroup("Targeting")]
    [Export] public TowerTargeting.Priority TargetPriority { get; set; } = TowerTargeting.Priority.First;
    public override string InspectorTargetPriorityLabel => TargetPriority.ToString();

    // Level-one C# tuning accessors forward directly to its snapshot; they are not Inspector sources.
    public float RangePixels { get => Levels[0].Stats.RangePixels; set => SetLevelStats(1, Levels[0].Stats with { RangePixels = value }); }
    public float ShotsPerSecond { get => Levels[0].Stats.ShotsPerSecond; set => SetLevelStats(1, Levels[0].Stats with { ShotsPerSecond = value }); }
    public int DamagePerShot { get => Levels[0].Stats.DamagePerShot; set => SetLevelStats(1, Levels[0].Stats with { DamagePerShot = value }); }
    public float ProjectileSpeed { get => Levels[0].Stats.ProjectileSpeed; set => SetLevelStats(1, Levels[0].Stats with { ProjectileSpeed = value }); }
    public float ShieldPenetration { get => Levels[0].Stats.ShieldPenetration; set => SetLevelStats(1, Levels[0].Stats with { ShieldPenetration = value }); }
    public float RotationSpeedDegrees { get => Levels[0].Stats.RotationSpeedDegrees; set => SetLevelStats(1, Levels[0].Stats with { RotationSpeedDegrees = value }); }
    public float AimToleranceDegrees { get => Levels[0].Stats.AimToleranceDegrees; set => SetLevelStats(1, Levels[0].Stats with { AimToleranceDegrees = value }); }
    public float TargetSearchInterval { get => Levels[0].Stats.TargetSearchInterval; set => SetLevelStats(1, Levels[0].Stats with { TargetSearchInterval = value }); }
    public float BlastRadiusPixels { get => Levels[0].Stats.Area.Value.RadiusPixels; set => SetLevelStats(1, Levels[0].Stats with { Area = Levels[0].Stats.Area.Value with { RadiusPixels = value } }); }
    public float EdgeDamageFraction { get => Levels[0].Stats.Area.Value.EdgeDamageFraction; set => SetLevelStats(1, Levels[0].Stats with { Area = Levels[0].Stats.Area.Value with { EdgeDamageFraction = value } }); }
    public DamageType DamageType { get => Levels[0].Stats.Weapon.Value.Type; set => SetLevelStats(1, Levels[0].Stats with { Weapon = Levels[0].Stats.Weapon.Value with { Type = value } }); }
    public bool OverrideShieldCoefficient { get => Levels[0].Stats.Weapon.Value.OverrideShield; set => SetLevelStats(1, Levels[0].Stats with { Weapon = Levels[0].Stats.Weapon.Value with { OverrideShield = value } }); }
    public float ShieldCoefficient { get => Levels[0].Stats.Weapon.Value.ShieldCoefficient; set => SetLevelStats(1, Levels[0].Stats with { Weapon = Levels[0].Stats.Weapon.Value with { ShieldCoefficient = value } }); }
    public bool OverrideHullCoefficient { get => Levels[0].Stats.Weapon.Value.OverrideHull; set => SetLevelStats(1, Levels[0].Stats with { Weapon = Levels[0].Stats.Weapon.Value with { OverrideHull = value } }); }
    public float HullCoefficient { get => Levels[0].Stats.Weapon.Value.HullCoefficient; set => SetLevelStats(1, Levels[0].Stats with { Weapon = Levels[0].Stats.Weapon.Value with { HullCoefficient = value } }); }

    // =========================================================
    // Explicitly clone runtime rows/recipes while keeping authored defaults separate.
    public override BlasterCannonConfig CreateInstanceCopy()
    {
        var copy = (BlasterCannonConfig)base.CreateInstanceCopy();
        copy.ImpactStyle = ImpactStyle == null ? null : (ShockwaveStyle)ImpactStyle.Duplicate(true);
        return copy;
    }

    // =========================================================
    // Every weapon selects its full row through the same eight stages.
    public override TowerFinalStats ResolveStats(int level) => TowerStatPipeline.Resolve(this, level);

    // =========================================================
    // Launch payload takes every damage input from the selected cached level.
    public DamageInfo CreateHit(TowerFinalStats final, DamageSource source = null) => final.Weapon.Value.CreateHit(final.DamagePerShot, final.ShieldPenetration, source);

    // =========================================================
    // Weapon-specific detail belongs to the weapon config, not generic placement or HUD.
    public override string DescribeStats(TowerFinalStats final) =>
        $"Range: {final.RangePixels:0} px\nRate: {final.ShotsPerSecond:0.##} shots/s\nBase damage: {final.DamagePerShot}\nBlast radius: {final.Area.Value.RadiusPixels:0.##} px\nEdge damage: {final.Area.Value.EdgeDamageFraction * 100:0.##}%\nDamage type: {final.Weapon.Value.Type}\nShield Penetration: {final.ShieldPenetration * 100:0.##}%\nTargeting: {TargetPriority}";

    // =========================================================
    // Describe the same resolved snapshot used by upgrade application.
    public override string DescribeChanges(TowerFinalStats now, TowerFinalStats future)
    {
        string changes = DescribeWeaponChanges(now, future);
        if (now.Area.Value.RadiusPixels != future.Area.Value.RadiusPixels) changes += $"Blast radius: {now.Area.Value.RadiusPixels:0.##} → {future.Area.Value.RadiusPixels:0.##} px\n";
        if (now.Area.Value.EdgeDamageFraction != future.Area.Value.EdgeDamageFraction) changes += $"Edge damage: {now.Area.Value.EdgeDamageFraction * 100:0.##}% → {future.Area.Value.EdgeDamageFraction * 100:0.##}%\n";
        if (now.ShieldPenetration != future.ShieldPenetration) changes += $"Shield Penetration: {now.ShieldPenetration * 100:0.##}% → {future.ShieldPenetration * 100:0.##}%\n";
        if (now.DamagePerShot != future.DamagePerShot) changes += $"Damage: {now.DamagePerShot} → {future.DamagePerShot}\n";
        if (now.ShotsPerSecond != future.ShotsPerSecond) changes += $"Rate: {now.ShotsPerSecond:0.##} → {future.ShotsPerSecond:0.##} shots/s\n";
        if (now.RangePixels != future.RangePixels) changes += $"Range: {now.RangePixels:0} → {future.RangePixels:0} px\n";
        if (now.ProjectileSpeed != future.ProjectileSpeed) changes += $"Projectile: {now.ProjectileSpeed:0} → {future.ProjectileSpeed:0} px/s\n";
        if (now.RotationSpeedDegrees != future.RotationSpeedDegrees) changes += $"Turn: {now.RotationSpeedDegrees:0} → {future.RotationSpeedDegrees:0}°/s\n";
        if (now.AimToleranceDegrees != future.AimToleranceDegrees) changes += $"Aim tolerance: {now.AimToleranceDegrees:0} → {future.AimToleranceDegrees:0}°\n";
        if (now.TargetSearchInterval != future.TargetSearchInterval) changes += $"Target search: {now.TargetSearchInterval:0.##} → {future.TargetSearchInterval:0.##}s\n";
        return changes;
    }
}
