// Sniper identity, one-muzzle policy and complete authored purchased-level data.
using Godot;

[GlobalClass]
public partial class SniperTowerConfig : ProjectileTurretConfig
{
    public override System.Collections.Generic.IReadOnlyList<TowerRankDefinition> Ranks => SniperTowerRanks.Rows;

    // =========================================================
    // The shared ProjectileTurretBase handles firing without a dedicated sniper physics loop.
    public SniperTowerConfig()
    {
        DisplayName = "Sniper Tower";
        Description = "Long-range precision Kinetic turret; high damage, slow cadence, fast guided shot.";
        FiringMode = BarrelFireMode.Single;
        Levels = SniperTowerLevels.Create();
    }

    // =========================================================
    // Return the actual derived type for independent runtime tuning.
    public override SniperTowerConfig CreateInstanceCopy() => (SniperTowerConfig)base.CreateInstanceCopy();
}
