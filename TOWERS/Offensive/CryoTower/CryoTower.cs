// Single-barrel Energy support weapon; targeting and projectile movement remain shared.
public partial class CryoTower : ProjectileTurretBase
{
    protected override StatusEffectId? RequiredEffect => StatusEffectId.Slow;
    // =========================================================
    // Snapshot the resolved level capability at launch, independent of later upgrades or tower removal.
    protected override StatusEffectPayload? CreateEffect(TowerFinalStats stats) =>
        stats.Slow is { Fraction: > 0 } slow ? new(StatusEffectId.Slow, slow.Fraction) : null;
    // =========================================================
    // Placement must reject another tower's configuration before charging Credits.
    public override bool AcceptsConfig(TowerConfig config) => config is CryoTowerConfig;
}
