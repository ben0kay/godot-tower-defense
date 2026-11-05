// RT1 non-offensive support config; private level rows and existing construction/Inspector contracts.
using Godot;

[GlobalClass]
public partial class RepairTowerConfig : TowerConfig
{
    // =========================================================
    // No offensive policy, ammo, heat, power or combat-rank progression is invented for this baseline.
    public RepairTowerConfig()
    {
        DisplayName = "Restoration Tower";
        Description = "Rotating beam restores damaged friendly tower hulls.";
        Levels = RepairTowerLevels.Create();
    }
    [ExportGroup("Hull Repair")]
    [Export(PropertyHint.Range, "0,100,0.5,or_greater")] public float HullPerSecond
    { get => Levels[0].Stats.Repair?.HullPerSecond ?? 0; set => SetLevelStats(1, Levels[0].Stats with { Repair = new(value) }); }
    [Export(PropertyHint.Range, "0,1024,1,or_greater")] public float RangePixels
    { get => Levels[0].Stats.RangePixels; set => SetLevelStats(1, Levels[0].Stats with { RangePixels = value }); }
    [ExportGroup("Aiming")]
    [Export(PropertyHint.Range, "0,720,1,or_greater")] public float RotationSpeedDegrees
    { get => Levels[0].Stats.RotationSpeedDegrees; set => SetLevelStats(1, Levels[0].Stats with { RotationSpeedDegrees = value }); }
    [Export(PropertyHint.Range, "0,180,1")] public float AimToleranceDegrees
    { get => Levels[0].Stats.AimToleranceDegrees; set => SetLevelStats(1, Levels[0].Stats with { AimToleranceDegrees = value }); }
    [Export(PropertyHint.Range, "0.05,2,0.05")] public float TargetSearchInterval
    { get => Levels[0].Stats.TargetSearchInterval; set => SetLevelStats(1, Levels[0].Stats with { TargetSearchInterval = value }); }
    public override string InspectorTargetPriorityLabel => "Lowest Integrity";
    // =========================================================
    // Pipeline source copies keep level/rate edits local to one placed tower.
    public override RepairTowerConfig CreateInstanceCopy() => (RepairTowerConfig)base.CreateInstanceCopy();
    public override TowerFinalStats ResolveStats(int level) => TowerStatPipeline.Resolve(this, level);
    public override string DescribeStats(TowerFinalStats stats) => $"Repair: {stats.Repair?.HullPerSecond ?? 0:0.##} hull HP/s\nRange: {stats.RangePixels:0}px\nTargeting: Lowest Integrity\nNo operating resource cost";
}
