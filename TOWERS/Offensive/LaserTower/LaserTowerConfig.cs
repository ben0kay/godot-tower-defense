// Authored sustained beam weapon; canonical costs/tiers inherited, no projectile or fire-rate exports.
using Godot;
using System.Collections.Generic;
[GlobalClass]
public partial class LaserTowerConfig : TowerConfig
{
    private float _range = 256, _dps = 20, _turn = 180, _aim = 12, _search = 0.15f;
    #region Inspector
    [ExportGroup("Beam Combat")]
    [Export] public DamageType DamageType { get; set; } = DamageType.Energy;
    [Export] public float RangePixels { get => _range; set => Change(ref _range, value); }
    [Export] public float DamagePerSecond { get => _dps; set => Change(ref _dps, value); }
    [ExportGroup("Aiming")]
    [Export] public float RotationSpeedDegrees { get => _turn; set => Change(ref _turn, value); }
    [Export] public float AimToleranceDegrees { get => _aim; set => Change(ref _aim, value); }
    [Export] public bool RotateWhileTracking { get; set; } = true;
    [ExportGroup("Targeting")]
    [Export] public TowerTargeting.Priority TargetPriority { get; set; } = TowerTargeting.Priority.First;
    [Export] public float TargetSearchInterval { get => _search; set => Change(ref _search, value); }
    #endregion
    #region Source and Resolution
    // =========================================================
    // The shared identity source remains on Config, referenced rather than duplicated by definition.
    public LaserTowerConfig() { DisplayName = "Laser Tower"; Description = "Sustained Energy beam; efficient against shields."; }
    // =========================================================
    // Numeric edits invalidate the instance's effective cache, never on every beam tick.
    private void Change<T>(ref T field, T value)
    { if (EqualityComparer<T>.Default.Equals(field, value)) return; field = value; EmitChanged(); }
    // =========================================================
    // Reuse explicit nested tier/cost copies and retain a typed config accessor.
    public override LaserTowerConfig CreateInstanceCopy() => (LaserTowerConfig)base.CreateInstanceCopy();
    // =========================================================
    // Optional beam capability enters the same eight-stage pipeline as Battle Tower.
    public override TowerFinalStats ResolveStats(int level) => TowerStatPipeline.Resolve(
        new TowerStatBlock(RangePixels, 0, 0, 1, RotationSpeedDegrees, AimToleranceDegrees, TargetSearchInterval,
            new BeamStats(DamagePerSecond)), UpgradeTiers, level);
    // =========================================================
    // Inspector exposes effective beam values, not invented shot/projectile numbers.
    public override string DescribeStats(TowerFinalStats final) =>
        $"Range: {final.RangePixels:0} px\nDPS: {final.Beam.Value.DamagePerSecond:0.##} base/s\nDamage type: {DamageType}\nTargeting: {TargetPriority}";
    // =========================================================
    // Paid next-tier preview compares the exact cached/resolved capability values.
    public override string DescribeChanges(TowerFinalStats now, TowerFinalStats future)
    {
        string changes = "";
        if (now.Beam != future.Beam) changes += $"DPS: {now.Beam.Value.DamagePerSecond:0.##} → {future.Beam.Value.DamagePerSecond:0.##} base/s\n";
        if (now.RangePixels != future.RangePixels) changes += $"Range: {now.RangePixels:0} → {future.RangePixels:0} px\n";
        if (now.RotationSpeedDegrees != future.RotationSpeedDegrees) changes += $"Turn: {now.RotationSpeedDegrees:0} → {future.RotationSpeedDegrees:0}°/s\n";
        if (now.AimToleranceDegrees != future.AimToleranceDegrees) changes += $"Aim tolerance: {now.AimToleranceDegrees:0} → {future.AimToleranceDegrees:0}°\n";
        if (now.TargetSearchInterval != future.TargetSearchInterval) changes += $"Search: {now.TargetSearchInterval:0.##} → {future.TargetSearchInterval:0.##}s\n";
        return changes;
    }
    #endregion
}
