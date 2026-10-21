// Level-owned certificates for unchanged straight route segments; uncertain geometry keeps swept checks.
using Godot;
using System.Collections.Generic;

public sealed class RouteSegmentSafety
{
    #region Certificates
    private readonly record struct Key(Vector2 From, Vector2 To, float Radius);
    private readonly record struct Entry(Rect2I Cells, bool Safe);
    private readonly Dictionary<Key, Entry> _entries = new();
    public long Hits { get; private set; }
    public long Builds { get; private set; }
    public int Count => _entries.Count;
    // =========================================================
    // Both safe and uncertain results are bounded; uncertain results never authorize movement.
    public bool Certified(NavigationGrid nav, Vector2 from, Vector2 to, float radius)
    {
        if (!nav.Config.EnableRouteSafetyCache || !from.IsFinite() || !to.IsFinite() || !float.IsFinite(radius) || radius < 0) return false;
        Key key = new(from, to, radius);
        if (_entries.TryGetValue(key, out Entry entry)) { Hits++; return entry.Safe; }
        float padding = radius + .02f;
        Vector2I low = nav.Grid.WorldToCell(from.Min(to) - Vector2.One * padding);
        Vector2I high = nav.Grid.WorldToCell(from.Max(to) + Vector2.One * padding);
        Rect2I cells = new(low, high - low + Vector2I.One);
        Vector2 a = nav.Grid.ToLocal(from), b = nav.Grid.ToLocal(to);
        float size = nav.Grid.Config.CellSize; bool safe = true;
        for (int y = low.Y; y <= high.Y && safe; y++) for (int x = low.X; x <= high.X; x++)
        {
            if (!nav.Grid.BlocksEnemies(new(x, y))) continue;
            // Expanded rectangles conservatively contain the swept circle, including diagonal corners.
            Rect2 wall = new Rect2(new Vector2(x * size, y * size), Vector2.One * size).Grow(padding);
            if (Intersects(a, b, wall)) { safe = false; break; }
        }
        int capacity = Mathf.Clamp(nav.Config.SharedRouteCacheEntries, 16, 2048) * 32;
        if (_entries.Count >= capacity)
        { using var cursor = _entries.Keys.GetEnumerator(); if (cursor.MoveNext()) _entries.Remove(cursor.Current); }
        Builds++; _entries[key] = new(cells, safe); return safe;
    }
    // =========================================================
    // Inclusive slab intersection rejects touching walls; no sampling gaps can create a certificate.
    private static bool Intersects(Vector2 a, Vector2 b, Rect2 wall)
    {
        float enter = 0, exit = 1; Vector2 direction = b - a;
        for (int axis = 0; axis < 2; axis++)
        {
            float origin = a[axis], delta = direction[axis], min = wall.Position[axis], max = wall.End[axis];
            if (Mathf.Abs(delta) < .000001f) { if (origin < min || origin > max) return false; continue; }
            float first = (min - origin) / delta, last = (max - origin) / delta;
            enter = Mathf.Max(enter, Mathf.Min(first, last)); exit = Mathf.Min(exit, Mathf.Max(first, last));
            if (enter > exit) return false;
        }
        return true;
    }
    #endregion

    #region Invalidation
    // =========================================================
    // Navigation events already exclude towers placed entirely on solid terrain.
    public void Invalidate(Vector2I cell)
    {
        List<Key> removed = null;
        foreach (var pair in _entries) if (pair.Value.Cells.HasPoint(cell)) (removed ??= new()).Add(pair.Key);
        if (removed != null) foreach (Key key in removed) _entries.Remove(key);
    }
    // =========================================================
    // Full terrain rebuild and world teardown release certificates together.
    public void Clear() => _entries.Clear();
    #endregion
}
