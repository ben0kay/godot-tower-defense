// Shared hostile target policy; only nearby living world-member towers, with deterministic closest-center ties.
using Godot;
using System.Collections.Generic;

public static class EnemyTowerTargeting
{
    #region Closest Target
    // =========================================================
    // Range remains measured from the pivot; optional muzzle origin supplies the shared terrain sight check.
    public static bool Valid(TowerRegistry registry, TowerBase tower, Vector2 origin, float radius, Vector2? sightOrigin = null)
        => InRange(registry, tower, origin, radius) && ClearSight(registry, sightOrigin ?? origin, tower.GlobalPosition);
    private static bool InRange(TowerRegistry registry, TowerBase tower, Vector2 origin, float radius)
        => GodotObject.IsInstanceValid(registry) && registry.IsInsideTree() && !registry.IsQueuedForDeletion()
        && origin.IsFinite() && float.IsFinite(radius) && radius > 0 && registry.Contains(tower)
        && DistanceSquared(origin, tower.GlobalPosition) <= (double)radius * radius;
    private static bool ClearSight(TowerRegistry registry, Vector2 from, Vector2 to) => from.IsFinite() && to.IsFinite()
        && (!GodotObject.IsInstanceValid(registry.TerrainGrid) ||
            (registry.TerrainGrid.Lighting?.IsRevealed(to) != false && registry.TerrainGrid.HasWeaponLineOfSight(from, to)));
    // =========================================================
    // A caller-owned scratch list prevents transient result allocations; registration order resolves exact ties.
    public static TowerBase Closest(TowerRegistry registry, Vector2 origin, float radius, List<TowerBase> scratch, Vector2? sightOrigin = null)
    {
        if (!GodotObject.IsInstanceValid(registry) || !registry.IsInsideTree() || !origin.IsFinite()
            || !float.IsFinite(radius) || radius <= 0) { scratch.Clear(); return null; }
        registry.GetNearby(origin, radius, scratch);
        TowerBase best = null; double distance = double.PositiveInfinity; long order = long.MaxValue;
        foreach (TowerBase tower in scratch)
        {
            if (!InRange(registry, tower, origin, radius)) continue;
            double candidate = DistanceSquared(origin, tower.GlobalPosition); long serial = registry.RegistrationOrder(tower);
            if (candidate < distance || (candidate == distance && serial < order))
            {
                // Only score contenders traverse terrain; a blocked nearer tower never hides an exposed fallback.
                if (!ClearSight(registry, sightOrigin ?? origin, tower.GlobalPosition)) continue;
                best = tower; distance = candidate; order = serial;
            }
        }
        scratch.Clear(); return best;
    }
    // =========================================================
    // Double arithmetic keeps inclusive radius boundaries stable and avoids float-square overflow.
    private static double DistanceSquared(Vector2 a, Vector2 b)
    { double x = (double)a.X - b.X, y = (double)a.Y - b.Y; return x * x + y * y; }
    #endregion
}
