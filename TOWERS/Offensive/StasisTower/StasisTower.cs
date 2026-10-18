// Single-barrel Energy support weapon; targeting and projectile movement remain shared.
public partial class StasisTower : ProjectileTurretBase
{
    #region Stasis Definition

    protected override StatusEffectId? RequiredEffect => StatusEffectId.Freeze;
    // =========================================================
    // Snapshot the resolved level capability at launch, independent of later upgrades or tower removal.
    protected override StatusEffectPayload? CreateEffect(TowerFinalStats stats) =>
        stats.Freeze is { DurationSeconds: > 0 } freeze ? new(StatusEffectId.Freeze, 0, freeze.DurationSeconds) : null;
    // =========================================================
    // Placement must reject another tower's configuration before charging Credits.
    public override bool AcceptsConfig(TowerConfig config) => config is StasisTowerConfig;
    #endregion
}
