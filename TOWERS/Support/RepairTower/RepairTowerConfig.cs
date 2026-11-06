// Restoration support configuration: absolute repair tiers plus optional Inspector-authored sustained-beam heat.
using Godot;

[GlobalClass]
public partial class RepairTowerConfig : TowerConfig
{
    // =========================================================
    // Shared power and heat use the existing tower pipeline; no ammunition or offensive policy.
    public RepairTowerConfig()
    {
        DisplayName = "Restoration Tower";
        Description = "Rotating beam restores damaged friendly tower hulls.";
        Levels = RepairTowerLevels.Create();
    }
    #region Heat
    // =========================================================
    // Match other continuous-beam towers: timed overheating is derived once from actual repair seconds.
    private bool _heatEnabled;
    private double _overheatMinutes = 3, _recoverySeconds = 10;
    [ExportGroup("Heat")]
    [Export] public bool HeatEnabled
    { get => _heatEnabled; set { if (_heatEnabled == value) return; _heatEnabled = value; EmitChanged(); } }
    [Export(PropertyHint.Range, "0.1,30,0.1,or_greater,suffix:min")]
    public double OverheatAfterMinutes
    { get => _overheatMinutes; set { if (_overheatMinutes == value) return; _overheatMinutes = value; EmitChanged(); } }
    [Export(PropertyHint.Range, "1,120,0.5,or_greater,suffix:s")]
    public double OverheatRecoverySeconds
    { get => _recoverySeconds; set { if (_recoverySeconds == value) return; _recoverySeconds = value; EmitChanged(); } }
    public override HeatTiming? SustainedHeatTiming =>
        HeatEnabled ? new(OverheatAfterMinutes * 60, OverheatRecoverySeconds) : null;
    #endregion

    [ExportGroup("Hull Repair")]
    [Export(PropertyHint.Range, "0,100,0.5,or_greater")] public float HullPerSecond
    { get => Levels[0].Stats.Repair?.HullPerSecond ?? 0; set => SetLevelStats(1, Levels[0].Stats with { Repair = new(value) }); }
    [Export(PropertyHint.Range, "0,1024,1,or_greater")] public float RangePixels
    { get => Levels[0].Stats.RangePixels; set => SetLevelStats(1, Levels[0].Stats with { RangePixels = value }); }
    [ExportGroup("Level 2 Hull Repair")]
    [Export(PropertyHint.Range, "0,100,0.5,or_greater")] public float Level2HullPerSecond
    { get => Levels[1].Stats.Repair?.HullPerSecond ?? 0; set => SetLevelStats(2, Levels[1].Stats with { Repair = new(value) }); }
    [Export(PropertyHint.Range, "0,1024,1,or_greater")] public float Level2RangePixels
    { get => Levels[1].Stats.RangePixels; set => SetLevelStats(2, Levels[1].Stats with { RangePixels = value }); }

    [ExportGroup("Level 3 Hull Repair")]
    [Export(PropertyHint.Range, "0,100,0.5,or_greater")] public float Level3HullPerSecond
    { get => Levels[2].Stats.Repair?.HullPerSecond ?? 0; set => SetLevelStats(3, Levels[2].Stats with { Repair = new(value) }); }
    [Export(PropertyHint.Range, "0,1024,1,or_greater")] public float Level3RangePixels
    { get => Levels[2].Stats.RangePixels; set => SetLevelStats(3, Levels[2].Stats with { RangePixels = value }); }

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
    public override string DescribeStats(TowerFinalStats stats) => $"Repair: {stats.Repair?.HullPerSecond ?? 0:0.##} hull HP/s\nRange: {stats.RangePixels:0}px\nTargeting: Lowest Integrity\nPowered hull repair; no ammunition";

    // =========================================================
    // Upgrade Inspector: show the paid repair/range gain without inventing offensive damage stats.
    public override string DescribeChanges(TowerFinalStats now, TowerFinalStats future)
    {
        string changes = "";
        if (now.Repair?.HullPerSecond != future.Repair?.HullPerSecond)
            changes += $"Repair: {now.Repair?.HullPerSecond ?? 0:0.##} → {future.Repair?.HullPerSecond ?? 0:0.##} HP/s\n";
        if (now.RangePixels != future.RangePixels)
            changes += $"Range: {now.RangePixels:0} → {future.RangePixels:0} px\n";
        return changes;
    }
}
