// Corruption payload and opt-in affected fallback; aiming, firing, funding and projectiles remain shared.
public partial class CorruptorTower : ProjectileTurretBase
{
    protected override StatusEffectId? RequiredEffect => StatusEffectId.Corruption;
    protected override bool AffectedStatusFallback => true;
    protected override StatusEffectPayload? CreateEffect(TowerFinalStats stats) => stats.Corruption is { } dot
        ? new(StatusEffectId.Corruption, dot.DamagePerSecond, dot.DurationSeconds, DamageSource) : null;
    public override bool AcceptsConfig(TowerConfig config) => config is CorruptorTowerConfig;
}
