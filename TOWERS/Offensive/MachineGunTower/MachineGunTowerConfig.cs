// Machine Gun identity and alternating two-barrel policy, with per-type C# level/rank tables.
using Godot;

[GlobalClass]
public partial class MachineGunTowerConfig : ProjectileTurretConfig
{
    public override System.Collections.Generic.IReadOnlyList<TowerRankDefinition> Ranks => MachineGunTowerRanks.Rows;

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
