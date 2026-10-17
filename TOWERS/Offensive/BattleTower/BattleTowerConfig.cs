// Non-level Inspector policy and presentation; all level defaults live in BattleTowerLevels.cs.
using Godot;

[GlobalClass]
public partial class BattleTowerConfig : TowerConfig, ITowerTargetingPolicy
{
    public override System.Collections.Generic.IReadOnlyList<TowerRankDefinition> Ranks => BattleTowerRanks.Rows;
    // =========================================================
    // C# tables are the sole authored source of combat values and construction/upgrade prices.
    public BattleTowerConfig()
    {
        DisplayName = "Battle Tower";
        Description = "Versatile baseline turret: one barrel and balanced range/fire rate.";
        Levels = BattleTowerLevels.Create();
    }

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

    // =========================================================
    // Explicitly clone runtime rows/recipes while keeping authored defaults separate.
    public override BattleTowerConfig CreateInstanceCopy() => (BattleTowerConfig)base.CreateInstanceCopy();

    // =========================================================
    // Every weapon selects its full row through the same eight stages.
    public override TowerFinalStats ResolveStats(int level) => TowerStatPipeline.Resolve(this, level);

    // =========================================================
    // Weapon-specific detail belongs to the weapon config, not generic placement or HUD.
    public override string DescribeStats(TowerFinalStats final) =>
        $"Barrels: 1\nRange: {final.RangePixels:0} px\nRate: {final.ShotsPerSecond:0.##} shots/s\nDamage: {final.DamagePerShot}\nShield Penetration: {final.ShieldPenetration * 100:0.##}%\nTargeting: {TargetPriority}";

    // =========================================================
    // Describe the same resolved snapshot used by upgrade application.
    public override string DescribeChanges(TowerFinalStats now, TowerFinalStats future)
    {
        string changes = "";
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
