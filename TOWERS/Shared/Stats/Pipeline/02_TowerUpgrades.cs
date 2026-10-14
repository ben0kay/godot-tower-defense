// Stage 02: neutral until TowerUpgrades gameplay is implemented in a later pass.
public static class TowerUpgradesStage
{
    // =========================================================
    // Pass through unchanged; this stage owns no managers, timers or payment operations.
    public static TowerStatBlock Apply(TowerStatBlock stats, TowerStatContext context) => stats;
}
