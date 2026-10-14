// Reusable target queries: First compares remaining route distance, Closest compares tower distance.
using Godot;

public static class TowerTargeting
{
    public enum Priority { First, Closest }

    // =========================================================
    // Search only living enemies in firing range; stale/failed paths cannot win First.
    public static Enemy Find(EnemyRegistry registry, Vector2 position, float range, Priority priority)
    {
        Enemy best = null; float score = float.PositiveInfinity;
        foreach (Enemy enemy in registry.Living)
        {
            if (!enemy.IsAlive) continue;
            float distance = position.DistanceSquaredTo(enemy.GlobalPosition);
            if (distance > range * range) continue;
            float candidate = priority == Priority.First ? enemy.RemainingPathDistance : distance;
            if (candidate < score) { score = candidate; best = enemy; }
        }
        return best;
    }

    // =========================================================
    // Cached targets are checked cheaply between staggered searches.
    public static bool Valid(Enemy enemy, Vector2 position, float range) =>
        GodotObject.IsInstanceValid(enemy) && enemy.IsAlive
        && position.DistanceSquaredTo(enemy.GlobalPosition) <= range * range;
}
