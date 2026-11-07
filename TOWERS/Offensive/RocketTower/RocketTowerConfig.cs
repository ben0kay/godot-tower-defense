// Rocket Tower policy and appearance reuse Blaster's area-weapon config without shared mutable balance.
using Godot;

[GlobalClass]
public partial class RocketTowerConfig : BlasterCannonConfig
{
    // LGT0-A.2 optional light for this splash projectile type.
    [Export] public bool ProjectileWorldLight { get; set; } = true;
    public override System.Collections.Generic.IReadOnlyList<TowerRankDefinition> Ranks => RocketTowerRanks.Rows;

    // =========================================================
    // The only authored numeric rows belong to this rocket weapon.
    public RocketTowerConfig()
    {
        DisplayName = "Rocket Tower";
        Description = "Long-range twin-launcher: alternating unguided rockets with compact Explosive splash.";
        Levels = RocketTowerLevels.Create();
    }

    // =========================================================
    // Keep a correctly typed per-instance config with private paid-level rows and impact visuals.
    public override RocketTowerConfig CreateInstanceCopy() => (RocketTowerConfig)base.CreateInstanceCopy();
}
