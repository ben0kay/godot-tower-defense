// Long-range single-barrel Kinetic turret; all shared aiming/projectile logic lives in ProjectileTurretBase.
public partial class SniperTower : ProjectileTurretBase
{
    // =========================================================
    // Reject incompatible tower definitions before placement and wallet payment.
    public override bool AcceptsConfig(TowerConfig config) => config is SniperTowerConfig;
}
