// Reusable target queries: First compares remaining route distance, Closest compares tower distance.
using Godot;
using System;

public static class TowerTargeting
{
    #region Target Policies
    public enum Preference { None = 0, Shielded = 1 }
    public enum Priority { First = 0, Closest = 1, Last = 2, Farthest = 3, Strongest = 4, Weakest = 5 }

    #endregion

    #region Spatial Acquisition
    // =========================================================
    // Search only living enemies in firing range; stale/failed paths cannot win First.
    public static Enemy Find(EnemyRegistry registry, Vector2 position, float range, Priority priority, StatusEffectId? requiredEffect = null, Preference preference = Preference.None)
    {
        if (!Enum.IsDefined(preference)) throw new ArgumentOutOfRangeException(nameof(preference));
        if (!Enum.IsDefined(priority)) throw new ArgumentOutOfRangeException(nameof(priority));
        if (!float.IsFinite(range) || range < 0) throw new ArgumentOutOfRangeException(nameof(range));
        TargetVisitor visitor = new() { Position = position, RangeSquared = range * range, Mode = priority,
            Score = Largest(priority) ? float.NegativeInfinity : float.PositiveInfinity, Order = ulong.MaxValue, RequiredEffect = requiredEffect, PreferredGroup = preference };
        // Preserve the old float-distance inclusive boundary; narrow scoring still uses its exact comparison.
        float queryRange = float.IsFinite(MathF.BitIncrement(range)) ? MathF.BitIncrement(range) : range;
        registry.VisitEnemiesInRadius(position, queryRange, ref visitor);
        return visitor.Best;
    }

    // =========================================================
    // Compare scores within a preference tier using the selected priority.
    private static bool Largest(Priority mode) => mode is Priority.Last or Priority.Farthest or Priority.Strongest;

    private struct TargetVisitor : EnemySpatialIndex.INeighborVisitor
    {
        public Vector2 Position;
        public float RangeSquared, Score;
        public Priority Mode;
        public ulong Order;
        public Enemy Best;
        public StatusEffectId? RequiredEffect;
        public Preference PreferredGroup;
        public bool BestPreferred;
        // =========================================================
        // Strict finite best-score selection plus the original earlier-living-entry tie-break.
        public void Accept(EnemySpatialIndex.Neighbor nearby)
        {
            if (!Eligible(nearby.Enemy, RequiredEffect)) return;
            float distance = Position.DistanceSquaredTo(nearby.Position);
            if (distance > RangeSquared) return;
            float candidate = Mode switch
            {
                Priority.First or Priority.Last => nearby.Enemy.RemainingJourneyDistance,
                Priority.Strongest or Priority.Weakest => nearby.Enemy.Health + nearby.Enemy.CurrentShield,
                _ => distance
            };
            if (!float.IsFinite(candidate)) return;
            bool preferred = PreferredGroup == Preference.Shielded && nearby.Enemy.CurrentShield > 0;
            // Preferences rank eligible groups first; ordinary mode scoring/ties apply within each group.
            if (Best == null || (preferred && !BestPreferred) || (preferred == BestPreferred &&
                ((Largest(Mode) ? candidate > Score : candidate < Score) || (candidate == Score && nearby.Order < Order))))
            { Score = candidate; Best = nearby.Enemy; Order = nearby.Order; BestPreferred = preferred; }
        }
    }

    #endregion

    #region Eligibility
    // =========================================================
    // Eligibility is shared by spatial acquisition and cached-target checks; no damage-only fallback.
    public static bool Eligible(Enemy enemy, StatusEffectId? effect) => !effect.HasValue
        || (enemy.CanReceiveStatus(effect.Value) && !enemy.HasStatus(effect.Value));

    // =========================================================
    // Cached targets are checked cheaply between staggered searches.
    public static bool Valid(Enemy enemy, Vector2 position, float range) =>
        GodotObject.IsInstanceValid(enemy) && enemy.IsAlive
        && position.DistanceSquaredTo(enemy.GlobalPosition) <= range * range;
    #endregion
}
