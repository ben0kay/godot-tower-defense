// Stage 05: neutral until LevelModifiers gameplay is implemented in a later pass.
public static class LevelModifiersStage
{
    // =========================================================
    // Pass through unchanged; this stage owns no managers, timers or payment operations.
    public static TowerStatBlock Apply(TowerStatBlock stats, TowerStatContext context) => stats;
}
