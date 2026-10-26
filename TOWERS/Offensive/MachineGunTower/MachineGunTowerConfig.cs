// Machine Gun identity and alternating two-barrel policy, with per-type C# level/rank tables.
using Godot;

[GlobalClass]
public partial class MachineGunTowerConfig : ProjectileTurretConfig
{
    public override System.Collections.Generic.IReadOnlyList<TowerRankDefinition> Ranks => MachineGunTowerRanks.Rows;

    #region Heat
    // Runtime copies retain per-type Inspector timing; Rocket inherits this policy with its own preset.
    private bool _heatEnabled;
    private double _overheatMinutes = 2, _recoverySeconds = 10;
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
    // Sequential mode alternates MuzzleA and MuzzleB; total rate is bullets/s across both.
    public MachineGunTowerConfig()
    {
        DisplayName = "Machine Gun Tower";
        Description = "Rapid Kinetic suppression turret: two barrels alternate each projectile.";
        FiringMode = BarrelFireMode.Sequential;
        Levels = MachineGunTowerLevels.Create();
    }

    // =========================================================
    // Preserve derived config type in private per-tower runtime copies.
    public override MachineGunTowerConfig CreateInstanceCopy() => (MachineGunTowerConfig)base.CreateInstanceCopy();
}
