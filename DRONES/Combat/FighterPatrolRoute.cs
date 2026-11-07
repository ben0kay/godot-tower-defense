// Immutable sampled figure-eight with a distance lookup shared by patrol flight and route preview.
using Godot;
using System;

internal sealed class FighterPatrolRoute
{
    private const int Segments = 96;
    private readonly float[] _distances = new float[Segments + 1];
    internal readonly Vector2[] Points = new Vector2[Segments + 1];
    internal Vector2 A => Points[0];
    internal Vector2 B => Points[Segments / 2];
    internal float Length => _distances[^1];

    // =========================================================
    // A/B are the outer tips. Reduce the width near edges while retaining a usable rounded circuit.
    internal static FighterPatrolRoute Create(Vector2 a, Vector2 b, FighterDroneConfig config, WorldGrid grid)
    {
        if (!a.IsFinite() || !b.IsFinite() || a.DistanceSquaredTo(b) < 160 * 160) return null;
        Vector2 half = (b - a) * .5f, centre = a + half;
        Vector2 side = half.Normalized().Orthogonal();
        float width = Mathf.Min(FighterDroneConfig.Safe(config.PatrolWidth, 96, 32, 500), half.Length() * .8f);
        var route = new FighterPatrolRoute();
        for (int attempt = 0; attempt < 6 && width >= 20; attempt++, width *= .70f)
        {
            bool fits = true;
            for (int i = 0; i <= Segments; i++)
            {
                float angle = i * Mathf.Tau / Segments;
                Vector2 point = centre - half * Mathf.Cos(angle) + side * (width * Mathf.Sin(2 * angle));
                if (i == 0 || i == Segments) point = a;
                else if (i == Segments / 2) point = b;
                if (!grid.IsInsideMap(grid.WorldToCell(point))) { fits = false; break; }
                route.Points[i] = point;
                if (i > 0) route._distances[i] = route._distances[i - 1] + point.DistanceTo(route.Points[i - 1]);
            }
            if (fits) return route;
        }
        return null;
    }

    // =========================================================
    // Advance in world pixels so A/B spacing does not change the configured cruise speed.
    internal Vector2 PointAt(float distance)
    {
        distance = Mathf.PosMod(distance, Length);
        int next = Array.BinarySearch(_distances, distance);
        if (next >= 0) return Points[next];
        next = ~next;
        float fraction = (distance - _distances[next - 1]) / (_distances[next] - _distances[next - 1]);
        return Points[next - 1].Lerp(Points[next], fraction);
    }

    internal float NearestDistance(Vector2 point)
    {
        int best = 0;
        for (int i = 1; i < Segments; i++)
            if (Points[i].DistanceSquaredTo(point) < Points[best].DistanceSquaredTo(point)) best = i;
        return _distances[best];
    }
}
