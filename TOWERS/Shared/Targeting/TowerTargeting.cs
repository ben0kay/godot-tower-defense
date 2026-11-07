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
    public static Enemy Find(EnemyRegistry registry, Vector2 position, float range, Priority priority, StatusEffectId? requiredEffect = null, Preference preference = Preference.None, bool detectsStealth = false, bool affectedFallback = false)
    {
        using var pf2 = SubsystemProfiler.Measure(registry, ProfileCategory.Targeting);
        if (!Enum.IsDefined(preference)) throw new ArgumentOutOfRangeException(nameof(preference));
        if (!Enum.IsDefined(priority)) throw new ArgumentOutOfRangeException(nameof(priority));
        if (!float.IsFinite(range) || range < 0) throw new ArgumentOutOfRangeException(nameof(range));
        TargetVisitor visitor = new() { Grid = registry.TerrainGrid, Position = position, RangeSquared = range * range, Mode = priority,
            Score = Largest(priority) ? float.NegativeInfinity : float.PositiveInfinity, Order = ulong.MaxValue, RequiredEffect = requiredEffect, PreferredGroup = preference, DetectsStealth = detectsStealth, AffectedFallback = affectedFallback };
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
        public WorldGrid Grid;
        public Vector2 Position;
        public float RangeSquared, Score;
        public Priority Mode;
        public ulong Order;
        public Enemy Best;
        public StatusEffectId? RequiredEffect;
        public Preference PreferredGroup;
        public bool BestPreferred, DetectsStealth, AffectedFallback;
        // =========================================================
        // Strict finite scoring; Strongest ties use First before the stable registration-order fallback.
        public void Accept(EnemySpatialIndex.Neighbor nearby)
        {
            if (!TowerDetection.CanTargetFromTower(nearby.Enemy, DetectsStealth) || !Eligible(nearby.Enemy, RequiredEffect, AffectedFallback)) return;
            float distance = Position.DistanceSquaredTo(nearby.Position);
            if (distance > RangeSquared) return;
            float candidate = Mode switch
            {
                Priority.First or Priority.Last => nearby.Enemy.RemainingJourneyDistance,
                Priority.Strongest => nearby.Enemy.Definition.MaxHealth,
                Priority.Weakest => nearby.Enemy.Health + nearby.Enemy.CurrentShield,
                _ => distance
            };
            if (!float.IsFinite(candidate)) return;
            bool preferred = AffectedFallback && RequiredEffect.HasValue ? !nearby.Enemy.HasStatus(RequiredEffect.Value)
                : PreferredGroup == Preference.Shielded && nearby.Enemy.CurrentShield > 0;
            // Preferences rank eligible groups first; ordinary mode scoring/ties apply within each group.
            if (Best == null || (preferred && !BestPreferred) || (preferred == BestPreferred &&
                ((Largest(Mode) ? candidate > Score : candidate < Score) || (candidate == Score && WinsTie(nearby)))))
            {
                // Only contenders pay for a line traversal; no allocations, raycast objects or full-map scans.
                if (GodotObject.IsInstanceValid(Grid) && !Grid.HasWeaponLineOfSight(Position, nearby.Position)) return;
                Score = candidate; Best = nearby.Enemy; Order = nearby.Order; BestPreferred = preferred;
            }
        }

        // =========================================================
        // Equal maximum hull HP uses First's cached full journey; unavailable routes fall back to stable order.
        private bool WinsTie(EnemySpatialIndex.Neighbor nearby)
        {
            if (Mode != Priority.Strongest) return nearby.Order < Order;
            float candidateJourney = nearby.Enemy.RemainingJourneyDistance;
            float bestJourney = Best.RemainingJourneyDistance;
            if (!float.IsFinite(candidateJourney)) candidateJourney = float.PositiveInfinity;
            if (!float.IsFinite(bestJourney)) bestJourney = float.PositiveInfinity;
            return candidateJourney < bestJourney || (candidateJourney == bestJourney && nearby.Order < Order);
        }
    }

    #endregion

    #region Eligibility
    // =========================================================
    // Eligibility is shared by spatial acquisition and cached-target checks; no damage-only fallback.
    public static bool Eligible(Enemy enemy, StatusEffectId? effect, bool affectedFallback = false) => !effect.HasValue
        || (enemy.CanReceiveStatus(effect.Value) && (affectedFallback || !enemy.HasStatus(effect.Value)));

    // =========================================================
    // Cached targets are checked cheaply between staggered searches.
    public static bool Valid(Enemy enemy, Vector2 position, float range, bool detectsStealth = false) =>
        TowerDetection.CanTargetFromTower(enemy, detectsStealth)
        && position.DistanceSquaredTo(enemy.GlobalPosition) <= range * range
        && (!GodotObject.IsInstanceValid(enemy.Registry?.TerrainGrid)
            || enemy.Registry.TerrainGrid.HasWeaponLineOfSight(position, enemy.GlobalPosition));
    #endregion
}

