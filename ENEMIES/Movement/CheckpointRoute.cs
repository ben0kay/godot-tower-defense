// Shared point-target adapter for live checkpoint stages and previews; exactly one production A* query per call.
using Godot;
using System;
public static class CheckpointRoute
{
    // =========================================================
    // Preview stage B starts at physical circle entry, matching live swept movement rather than the sampled endpoint.
    public static Vector2[] ToEntry(RouteCheckpoint checkpoint, Vector2 start, Vector2[] points)
    {
        var prefix = new System.Collections.Generic.List<Vector2>(); Vector2 from = start;
        foreach (Vector2 point in points)
        {
            if (checkpoint.Entry(from, point, out float fraction))
            { prefix.Add(from.Lerp(point, fraction)); return prefix.ToArray(); }
            prefix.Add(point); from = point;
        }
        return prefix.ToArray();
    }

    // =========================================================
    // Route into an actual circle target, including an unsnapped terminal connector with physical clearance.
    public static Vector2[] Find(NavigationGrid nav, Vector2 origin, Vector2 target, int profile, float radius, out float cost, bool preview = false)
    {
        Vector2[] points = preview ? nav.FindPreviewPath(origin, nav.Grid.WorldToCell(target), profile, out cost)
            : nav.FindPath(origin, nav.Grid.WorldToCell(target), profile, out cost);
        if (points.Length == 0) return points;
        if (!nav.ConnectionClear(points[^1], target, radius)) { cost = float.PositiveInfinity; return Array.Empty<Vector2>(); }
        if (points[^1].DistanceSquaredTo(target) > 0.0001f)
        { Array.Resize(ref points, points.Length + 1); points[^1] = target; cost = nav.RouteCost(origin, points, profile); }
        return points;
    }
}
