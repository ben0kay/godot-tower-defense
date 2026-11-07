// One visibility rule shared by initial acquisition and cached locks; stealth is not damage immunity.
using Godot;

public static class TowerDetection
{
    // =========================================================
    // Independent tower capability permits hidden targets without revealing them for other towers.
    public static bool CanDetect(Enemy enemy, bool detectsStealth = false) =>
        Detectable(enemy, detectsStealth) && IsLit(enemy);

    internal static bool CanDetectAt(Enemy enemy, Vector2 position, bool detectsStealth) =>
        Detectable(enemy, detectsStealth) && (enemy.Registry?.TerrainGrid?.Lighting?.IsRevealed(position) ?? true);

    private static bool Detectable(Enemy enemy, bool detectsStealth) =>
        GodotObject.IsInstanceValid(enemy) && enemy.IsAlive && !enemy.IsQueuedForDeletion()
        && (!enemy.IsEffectivelyStealthed || detectsStealth);

    internal static bool IsLit(Enemy enemy)
    {
        WorldLightingController lighting = enemy.Registry?.TerrainGrid?.Lighting;
        return lighting == null || lighting.IsRevealed(enemy.GlobalPosition);
    }
}
