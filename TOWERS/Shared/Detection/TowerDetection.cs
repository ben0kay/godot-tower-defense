// One visibility rule shared by initial acquisition and cached locks; stealth is not damage immunity.
using Godot;

public static class TowerDetection
{
    // =========================================================
    // Independent tower capability permits hidden targets without revealing them for other towers.
    public static bool CanDetect(Enemy enemy, bool detectsStealth = false) =>
        Detectable(enemy, detectsStealth) && IsLit(enemy);

    // Friendly weapons can track radar contacts through darkness. Stealth still needs a detector.
    internal static bool CanTargetRadarContact(Enemy enemy, bool detectsStealth = false) =>
        Detectable(enemy, detectsStealth);

    private static bool Detectable(Enemy enemy, bool detectsStealth) =>
        GodotObject.IsInstanceValid(enemy) && enemy.IsAlive && !enemy.IsQueuedForDeletion()
        && (!enemy.IsEffectivelyStealthed || detectsStealth);

    private static bool IsLit(Enemy enemy)
    {
        WorldLightingController lighting = enemy.Registry?.TerrainGrid?.Lighting;
        return lighting == null || lighting.IsRevealed(enemy.GlobalPosition);
    }
}
