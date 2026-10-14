// Cached refined waypoints and cell dependencies; no additional pathfinding for offsets.
using Godot;
using System.Collections.Generic;

public sealed class EnemyRoute
{
    public Vector2[] Points { get; private set; } = System.Array.Empty<Vector2>();
    public float[] Suffix { get; private set; } = System.Array.Empty<float>();
    private Vector2I[] _logicalCells = System.Array.Empty<Vector2I>();
    public readonly Dictionary<Vector2I, List<int>> Dependencies = new();

    // =========================================================
    // Apply one stable lateral bias, tapering safely at corners/walls and preserving endpoints.
    public void Build(Vector2[] logical, Vector2 start, NavigationGrid nav, float radius, float offset, Vector2 forward = default)
    {
        _logicalCells = new Vector2I[logical.Length];
        for (int i = 0; i < logical.Length; i++) _logicalCells[i] = nav.Grid.WorldToCell(logical[i]);
        Points = (Vector2[])logical.Clone(); Dependencies.Clear(); Suffix = new float[Points.Length];
        for (int i = 1; i < Points.Length - 1 && offset != 0f; i++)
        {
            Vector2 direction = (logical[i] - logical[i - 1]).Normalized() + (logical[i + 1] - logical[i]).Normalized();
            if (direction.LengthSquared() < 0.01f) continue;
            Vector2 lateral = direction.Normalized().Orthogonal() * offset;
            for (float scale = 1f; scale >= 0.125f; scale *= 0.5f)
            {
                Vector2 candidate = logical[i] + lateral * scale;
                if (i == 1 && forward != Vector2.Zero && (candidate - start).Dot(forward) < -0.01f) continue;
                if (!nav.HasClearance(candidate, radius) || !nav.SegmentClear(Points[i - 1], candidate, radius)
                    || !nav.SegmentClear(candidate, logical[i + 1], radius)) continue;
                Points[i] = candidate; break;
            }
        }
        for (int i = Points.Length - 2; i >= 0; i--) Suffix[i] = Suffix[i + 1] + Points[i].DistanceTo(Points[i + 1]);
        for (int i = 0; i < Points.Length; i++)
        {
            Vector2 from = i == 0 ? start : Points[i - 1], to = Points[i];
            Vector2I min = nav.Grid.WorldToCell(new Vector2(Mathf.Min(from.X, to.X), Mathf.Min(from.Y, to.Y)) - Vector2.One * radius);
            Vector2I max = nav.Grid.WorldToCell(new Vector2(Mathf.Max(from.X, to.X), Mathf.Max(from.Y, to.Y)) + Vector2.One * radius);
            for (int y = min.Y; y <= max.Y; y++)
                for (int x = min.X; x <= max.X; x++)
                {
                    Vector2I cell = new(x, y);
                    if (!Dependencies.TryGetValue(cell, out List<int> segments)) Dependencies[cell] = segments = new();
                    segments.Add(i);
                }
        }
    }

    // =========================================================
    // Passed segments are ignored, so placing behind a ship cannot invalidate its remaining route.
    public bool UsesCell(Vector2I cell, int next) => Dependencies.TryGetValue(cell, out List<int> segments)
        && segments.Count > 0 && segments[^1] >= next;

    // =========================================================
    // Only segments near a changed cell need geometric verification, including diagonal clearance.
    public int BlockedSegment(Vector2I cell, int next, Vector2 position, NavigationGrid nav, float radius)
    {
        if (!Dependencies.TryGetValue(cell, out List<int> segments)) return -1;
        foreach (int index in segments)
            if (index >= next)
            {
                if (CornerBlocked(index, nav) || !nav.SegmentClear(index == next ? position : Points[index - 1], Points[index], radius)) return index;
            }
        return -1;
    }
    // =========================================================
    // Cached logical corner links stay strict even for tiny-radius ships with lateral offsets.
    public bool CornerBlocked(int index, NavigationGrid nav)
    {
        if (index <= 0 || index >= _logicalCells.Length) return false;
        Vector2I from = _logicalCells[index - 1], step = _logicalCells[index] - from;
        return Mathf.Abs(step.X) == 1 && Mathf.Abs(step.Y) == 1
            && (nav.Grid.BlocksEnemies(from + new Vector2I(step.X, 0))
                || nav.Grid.BlocksEnemies(from + new Vector2I(0, step.Y)));
    }

}
