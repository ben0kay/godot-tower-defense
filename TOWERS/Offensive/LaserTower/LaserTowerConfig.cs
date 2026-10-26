// Non-level Inspector policy and presentation; all level defaults live in LaserTowerLevels.cs.
using Godot;

[GlobalClass]
public partial class LaserTowerConfig : TowerConfig, ITowerTargetingPolicy
{
    #region Authored Settings
    public override System.Collections.Generic.IReadOnlyList<TowerRankDefinition> Ranks => LaserTowerRanks.Rows;

    #region Heat
    // Runtime copies retain per-type Inspector timing; Rocket inherits this policy with its own preset.
    private bool _heatEnabled;
    private double _overheatMinutes = 3.5, _recoverySeconds = 10;
    [ExportGroup("Heat")]
    [Export] public bool HeatEnabled
    { get => _heatEnabled; set { if (_heatEnabled == value) return; _heatEnabled = value; EmitChanged(); } }
    [Export(PropertyHint.Range, "0.1,30,0.1,or_greater,suffix:min")]
    public double OverheatAfterMinutes
    { get => _overheatMinutes; set { if (_overheatMinutes == value) return; _overheatMinutes = value; EmitChanged(); } }
    [Export(PropertyHint.Range, "1,120,0.5,or_greater,suffix:s")]
    public double OverheatRecoverySeconds
    { get => _recoverySeconds; set { if (_recoverySeconds == value) return; _recoverySeconds = value; EmitChanged(); } }
    public override HeatTiming? SustainedHeatTiming => HeatEnabled ? new(OverheatAfterMinutes * 60, OverheatRecoverySeconds) : null;
    #endregion
    // =========================================================
    // C# tables are the sole authored source of combat values and construction/upgrade prices.
    public LaserTowerConfig()
    {
        DisplayName = "Laser Tower";
        Description = "Sustained Energy beam; efficient against shields.";
        Levels = LaserTowerLevels.Create();
    }

    [ExportGroup("Aiming")]
    [Export] public bool RotateWhileTracking { get; set; } = true;
    [ExportGroup("Targeting")]
    [Export] public TowerTargeting.Priority TargetPriority { get; set; } = TowerTargeting.Priority.First;
    [Export] public TowerTargeting.Preference TargetPreference { get; set; } = TowerTargeting.Preference.Shielded;
    public override string InspectorTargetPriorityLabel => TargetPriority.ToString();

    // Level-one C# tuning accessors forward directly to its snapshot; they are not Inspector sources.
    public float RangePixels { get => Levels[0].Stats.RangePixels; set => SetLevelStats(1, Levels[0].Stats with { RangePixels = value }); }
    public float ShieldPenetration { get => Levels[0].Stats.ShieldPenetration; set => SetLevelStats(1, Levels[0].Stats with { ShieldPenetration = value }); }
    public float RotationSpeedDegrees { get => Levels[0].Stats.RotationSpeedDegrees; set => SetLevelStats(1, Levels[0].Stats with { RotationSpeedDegrees = value }); }
    public float AimToleranceDegrees { get => Levels[0].Stats.AimToleranceDegrees; set => SetLevelStats(1, Levels[0].Stats with { AimToleranceDegrees = value }); }
    public float TargetSearchInterval { get => Levels[0].Stats.TargetSearchInterval; set => SetLevelStats(1, Levels[0].Stats with { TargetSearchInterval = value }); }
    public float DamagePerSecond { get => Levels[0].Stats.Beam.Value.DamagePerSecond; set => SetLevelStats(1, Levels[0].Stats with { Beam = new BeamStats(value) }); }
    public DamageType DamageType { get => Levels[0].Stats.Weapon.Value.Type; set => SetLevelStats(1, Levels[0].Stats with { Weapon = Levels[0].Stats.Weapon.Value with { Type = value } }); }
    public bool OverrideShieldCoefficient { get => Levels[0].Stats.Weapon.Value.OverrideShield; set => SetLevelStats(1, Levels[0].Stats with { Weapon = Levels[0].Stats.Weapon.Value with { OverrideShield = value } }); }
    public float ShieldCoefficient { get => Levels[0].Stats.Weapon.Value.ShieldCoefficient; set => SetLevelStats(1, Levels[0].Stats with { Weapon = Levels[0].Stats.Weapon.Value with { ShieldCoefficient = value } }); }
    public bool OverrideHullCoefficient { get => Levels[0].Stats.Weapon.Value.OverrideHull; set => SetLevelStats(1, Levels[0].Stats with { Weapon = Levels[0].Stats.Weapon.Value with { OverrideHull = value } }); }
    public float HullCoefficient { get => Levels[0].Stats.Weapon.Value.HullCoefficient; set => SetLevelStats(1, Levels[0].Stats with { Weapon = Levels[0].Stats.Weapon.Value with { HullCoefficient = value } }); }

    #endregion

    #region Resolution and Presentation
    // =========================================================
    // Explicitly clone runtime rows/recipes while keeping authored defaults separate.
    public override LaserTowerConfig CreateInstanceCopy() => (LaserTowerConfig)base.CreateInstanceCopy();

    // =========================================================
    // Every weapon selects its full row through the same eight stages.
    public override TowerFinalStats ResolveStats(int level) => TowerStatPipeline.Resolve(this, level);

    // =========================================================
    // Active beams use their cached selected-level response, including penetration.
    public DamageInfo CreateHit(float amount, TowerFinalStats final, DamageSource source = null) => final.Weapon.Value.CreateHit(amount, final.ShieldPenetration, source);

    // =========================================================
    // Default-level tooling uses the same final snapshot as gameplay.
    public DamageInfo CreateHit(float amount) => CreateHit(amount, ResolveStats(1));

    // =========================================================
    // Inspector exposes effective beam values, not invented shot/projectile numbers.
    public override string DescribeStats(TowerFinalStats final)
    {
        var defaults = DamageResolver.DefaultProfile.Coefficients(final.Weapon.Value.Type);
        float shield = final.Weapon.Value.OverrideShield ? final.Weapon.Value.ShieldCoefficient : defaults.Shield;
        float hull = final.Weapon.Value.OverrideHull ? final.Weapon.Value.HullCoefficient : defaults.Hull;
        return $"Range: {final.RangePixels:0} px\nDPS: {final.Beam.Value.DamagePerSecond:0.##} base/s\nDamage type: {final.Weapon.Value.Type}\nShield: {shield:0.00}x / Hull: {hull:0.00}x\nShield Penetration: {final.ShieldPenetration * 100:0.##}%\nTargeting: {TargetPriority} · Preference: {TargetPreference}";
    }
    // =========================================================
    // Paid next-tier preview compares the exact cached/resolved capability values.
    public override string DescribeChanges(TowerFinalStats now, TowerFinalStats future)
    {
        string changes = DescribeWeaponChanges(now, future);
        if (now.ShieldPenetration != future.ShieldPenetration) changes += $"Shield Penetration: {now.ShieldPenetration * 100:0.##}% → {future.ShieldPenetration * 100:0.##}%\n";
        if (now.Beam != future.Beam) changes += $"DPS: {now.Beam.Value.DamagePerSecond:0.##} → {future.Beam.Value.DamagePerSecond:0.##} base/s\n";
        if (now.RangePixels != future.RangePixels) changes += $"Range: {now.RangePixels:0} → {future.RangePixels:0} px\n";
        if (now.RotationSpeedDegrees != future.RotationSpeedDegrees) changes += $"Turn: {now.RotationSpeedDegrees:0} → {future.RotationSpeedDegrees:0}°/s\n";
        if (now.AimToleranceDegrees != future.AimToleranceDegrees) changes += $"Aim tolerance: {now.AimToleranceDegrees:0} → {future.AimToleranceDegrees:0}°\n";
        if (now.TargetSearchInterval != future.TargetSearchInterval) changes += $"Search: {now.TargetSearchInterval:0.##} → {future.TargetSearchInterval:0.##}s\n";
        return changes;
    }
    #endregion
}
