// Twin-muzzle sequential Kinetic turret; shared projectile loop owns targeting, ammo and cooldown.
public partial class MachineGunTower : ProjectileTurretBase
{
    // =========================================================
    // Reject mismatched configs before wallet payment or placement.
    public override bool AcceptsConfig(TowerConfig config) => config is MachineGunTowerConfig;
}
