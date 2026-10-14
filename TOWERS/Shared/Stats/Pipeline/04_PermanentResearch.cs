// Stage 04: neutral until PermanentResearch gameplay is implemented in a later pass.
public static class PermanentResearchStage
{
    // =========================================================
    // Pass through unchanged; this stage owns no managers, timers or payment operations.
    public static TowerStatBlock Apply(TowerStatBlock stats, TowerStatContext context) => stats;
}
