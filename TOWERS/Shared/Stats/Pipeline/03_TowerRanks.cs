// Stage 03: neutral until TowerRanks gameplay is implemented in a later pass.
public static class TowerRanksStage
{
    // =========================================================
    // Pass through unchanged; this stage owns no managers, timers or payment operations.
    public static TowerStatBlock Apply(TowerStatBlock stats, TowerStatContext context) => stats;
}
