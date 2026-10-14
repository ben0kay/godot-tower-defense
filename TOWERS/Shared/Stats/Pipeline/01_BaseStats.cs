// Stage 01: begin from the original authored values, never the previous final cache.
public static class BaseStatsStage
{
    // =========================================================
    // The context contains a copied snapshot, independent of the mutable Inspector Resource.
    public static TowerStatBlock Apply(TowerStatContext context) => context.BaseStats;
}
