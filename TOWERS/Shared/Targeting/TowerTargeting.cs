// Reusable target queries: First compares remaining route distance, Closest compares tower distance.
using Godot;
using System;

public static class TowerTargeting
{
    public enum Priority { First, Closest }

    // =========================================================
    // Search only living enemies in firing range; stale/failed paths cannot win First.
    public static Enemy Find(EnemyRegistry registry, Vector2 position, float range, Priority priority, StatusEffectId? requiredEffect = null)
    {
        if (!float.IsFinite(range) || range < 0) throw new ArgumentOutOfRangeException(nameof(range));
        TargetVisitor visitor = new() { Position = position, RangeSquared = range * range, Mode = priority,
            Score = float.PositiveInfinity, Order = ulong.MaxValue, RequiredEffect = requiredEffect };
        // Preserve the old float-distance inclusive boundary; narrow scoring still uses its exact comparison.
        float queryRange = float.IsFinite(MathF.BitIncrement(range)) ? MathF.BitIncrement(range) : range;
        registry.VisitEnemiesInRadius(position, queryRange, ref visitor);
        return visitor.Best;
    }

    private struct TargetVisitor : EnemySpatialIndex.INeighborVisitor
    {
        public Vector2 Position;
        public float RangeSquared, Score;
        public Priority Mode;
        public ulong Order;
        public Enemy Best;
        public StatusEffectId? RequiredEffect;
        // =========================================================
        // Strict finite best-score selection plus the original earlier-living-entry tie-break.
        public void Accept(EnemySpatialIndex.Neighbor nearby)
        {
            if (!Eligible(nearby.Enemy, RequiredEffect)) return;
            float distance = Position.DistanceSquaredTo(nearby.Position);
            if (distance > RangeSquared) return;
            float candidate = Mode == Priority.First ? nearby.Enemy.RemainingPathDistance : distance;
            if (candidate < Score || (Best != null && candidate == Score && nearby.Order < Order))
            { Score = candidate; Best = nearby.Enemy; Order = nearby.Order; }
        }
    }

    // =========================================================
    // Eligibility is shared by spatial acquisition and cached-target checks; no damage-only fallback.
    public static bool Eligible(Enemy enemy, StatusEffectId? effect) => !effect.HasValue
        || (enemy.CanReceiveStatus(effect.Value) && !enemy.HasStatus(effect.Value));

    // =========================================================
    // Cached targets are checked cheaply between staggered searches.
    public static bool Valid(Enemy enemy, Vector2 position, float range) =>
        GodotObject.IsInstanceValid(enemy) && enemy.IsAlive
        && position.DistanceSquaredTo(enemy.GlobalPosition) <= range * range;
}
