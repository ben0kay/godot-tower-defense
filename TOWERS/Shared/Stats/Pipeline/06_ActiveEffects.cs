// Stage 06: neutral until ActiveEffects gameplay is implemented in a later pass.
public static class ActiveEffectsStage
{
    // =========================================================
    // Pass through unchanged; this stage owns no managers, timers or payment operations.
    public static TowerStatBlock Apply(TowerStatBlock stats, TowerStatContext context) => stats;
}
