// Stage 02: select an absolute level snapshot; no cumulative modifiers or prior final values.
public static class TowerUpgradesStage
{
    // =========================================================
    // Later ranks/research/conditions/effects receive the selected level as their starting point.
    public static TowerStatBlock Apply(TowerStatBlock stats, TowerStatContext context) => context.SelectedLevel ?? stats;
}
