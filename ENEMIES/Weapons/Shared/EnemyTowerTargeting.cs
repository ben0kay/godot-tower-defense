// Shared hostile target policy; only nearby living world-member towers, with deterministic closest-center ties.
using Godot;
using System.Collections.Generic;

public static class EnemyTowerTargeting
{
    #region Closest Target
    // =========================================================
    // Cached locks recheck membership/range each active tick without reacquiring or scanning the tower container.
    public static bool Valid(TowerRegistry registry, TowerBase tower, Vector2 origin, float radius)
        => GodotObject.IsInstanceValid(registry) && registry.IsInsideTree() && !registry.IsQueuedForDeletion()
        && origin.IsFinite() && float.IsFinite(radius) && radius > 0 && registry.Contains(tower)
        && DistanceSquared(origin, tower.GlobalPosition) <= (double)radius * radius;
    // =========================================================
    // A caller-owned scratch list prevents transient result allocations; registration order resolves exact ties.
    public static TowerBase Closest(TowerRegistry registry, Vector2 origin, float radius, List<TowerBase> scratch)
    {
        if (!GodotObject.IsInstanceValid(registry) || !registry.IsInsideTree() || !origin.IsFinite()
            || !float.IsFinite(radius) || radius <= 0) { scratch.Clear(); return null; }
        registry.GetNearby(origin, radius, scratch);
        TowerBase best = null; double distance = double.PositiveInfinity; long order = long.MaxValue;
        foreach (TowerBase tower in scratch)
        {
            if (!Valid(registry, tower, origin, radius)) continue;
            double candidate = DistanceSquared(origin, tower.GlobalPosition); long serial = registry.RegistrationOrder(tower);
            if (candidate < distance || (candidate == distance && serial < order))
            { best = tower; distance = candidate; order = serial; }
        }
        scratch.Clear(); return best;
    }
    // =========================================================
    // Double arithmetic keeps inclusive radius boundaries stable and avoids float-square overflow.
    private static double DistanceSquared(Vector2 a, Vector2 b)
    { double x = (double)a.X - b.X, y = (double)a.Y - b.Y; return x * x + y * y; }
    #endregion
}
