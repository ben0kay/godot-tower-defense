// Shared integration assertion for the real rendered four-column paid upgrade comparison grid.
using Godot;

public static class UpgradeTableChecks
{
    // =========================================================
    // The first four cells are the table headers; subsequent cells are stat/current/next/gain.
    public static bool HasRow(GameplayHUD hud, string name, string current, string next, string gain = null)
    {
        GridContainer grid = hud.GetNode<GridContainer>("BottomArea/Inspector/Insets/Body/UpgradeComparison");
        int length = grid.GetChildCount();
        for (int i = 4; i + 3 < length; i += 4)
        {
            if (grid.GetChild<Label>(i).Text != name || grid.GetChild<Label>(i + 1).Text != current
                || grid.GetChild<Label>(i + 2).Text != next) continue;
            if (gain == null || grid.GetChild<Label>(i + 3).Text == gain) return true;
        }
        return false;
    }
}
