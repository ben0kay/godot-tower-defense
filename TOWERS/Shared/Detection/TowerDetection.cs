// One visibility rule shared by initial acquisition and cached locks; stealth is not damage immunity.
using Godot;

public static class TowerDetection
{
    // =========================================================
    // Independent tower capability permits hidden targets without revealing them for other towers.
    public static bool CanDetect(Enemy enemy, bool detectsStealth = false) =>
        GodotObject.IsInstanceValid(enemy) && enemy.IsAlive && !enemy.IsQueuedForDeletion()
        && (!enemy.IsEffectivelyStealthed || detectsStealth);
}
