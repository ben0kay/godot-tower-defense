// Cached refined waypoints and cell dependencies; no additional pathfinding for offsets.
using Godot;
using System.Collections.Generic;

public sealed class EnemyRoute
{
    public int CorridorSamples { get; private set; }
    public bool ReusedTemplate { get; private set; }
    public bool CorridorVariationApplied { get; private set; }
    private Vector2I[][] _corridorCorners;
    private int _cornerRevision = -1;
    private readonly Dictionary<int, bool> _cornerSafety = new();
    private RouteSegmentSafety _safetyPool;
    private long _safetyVersion = -1;
    private int _safetyIndex = -1;
    private float _safetyRadius;
    private bool _segmentCertified;
    public Vector2[] Points { get; private set; } = System.Array.Empty<Vector2>();
    public float[] Suffix { get; private set; } = System.Array.Empty<float>();
    private Vector2I[] _logicalCells = System.Array.Empty<Vector2I>();
    public readonly Dictionary<Vector2I, List<int>> Dependencies = new();

    #region Shared Template Adoption
    // =========================================================
    // Only the first origin differs; reject an unsafe/backwards connector before using a shared tail.
    public bool TryUseShared(EnemyRoute template, Vector2[] logical, Vector2 start, NavigationGrid nav, float radius, Vector2 forward)
    {
        Vector2 next = template.Points[1];
        if (forward != Vector2.Zero && (next - start).Dot(forward) < -0.01f) return false;
        if (!CorridorClearance.ConnectionClear(nav, start, next, radius)) return false;
        UseShared(template, logical, start, nav, radius, reused: true); return true;
    }
    // =========================================================
    // Mutable per-ship geometry/dependencies remain private; expensive lateral probes are not repeated.
    public void UseShared(EnemyRoute template, Vector2[] logical, Vector2 start, NavigationGrid nav, float radius, bool reused = false)
    {
        Points = (Vector2[])template.Points.Clone(); Points[0] = start;
        Suffix = new float[Points.Length]; _logicalCells = new Vector2I[logical.Length];
        for (int i = 0; i < logical.Length; i++) _logicalCells[i] = nav.Grid.WorldToCell(logical[i]);
        ReusedTemplate = reused; CorridorSamples = reused ? 0 : template.CorridorSamples; CorridorVariationApplied = template.CorridorVariationApplied; _corridorCorners = null;
        CacheGeometry(start, nav, radius);
        if (template._corridorCorners != null) CacheCorners(start, nav, radius);
    }
    #endregion

    // =========================================================
    // Apply one stable lateral bias, tapering safely at corners/walls and preserving endpoints.
    public void Build(Vector2[] logical, Vector2 start, NavigationGrid nav, float radius, float offset, Vector2 forward = default)
    {
        ReusedTemplate = false; CorridorSamples = 0; CorridorVariationApplied = false; _corridorCorners = null;
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
        CacheGeometry(start, nav, radius);
    }

    // =========================================================
    // Only selected ships call the optional sampler; ordinary Build remains the original offset path.
    public void BuildCorridor(Vector2[] logical, Vector2 start, NavigationGrid nav, float radius, float offset,
        uint seed, EnemyMasterConfig config, Vector2 forward = default)
    {
        Build(logical, start, nav, radius, offset, forward);
        bool changed = CorridorRouteRefinement.TryBuild(logical, Points, start, nav, radius, seed,
            config.CorridorVariationStrength, config.CorridorVariationSpacingCells, forward, out Vector2[] varied, out int samples);
        CorridorSamples = samples;
        if (changed)
        { Points = varied; CorridorVariationApplied = true; CacheGeometry(start, nav, radius); }
        else
        {
            // The legacy tiny-offset fallback also needs a strict check at a newly blocked grazing corner.
            Vector2 from = start; bool safe = true;
            foreach (Vector2 point in Points)
            { CorridorSamples++; if (!CorridorClearance.ConnectionClear(nav, from, point, radius)) { safe = false; break; } from = point; }
            if (!safe) { Build(logical, start, nav, radius, 0, forward); CorridorSamples = samples; }
        }
        CacheCorners(start, nav, radius);
    }

    // =========================================================
    // Cache physical distance and swept dependencies from the actual traveled chain, not its old centerline.
    private void CacheGeometry(Vector2 start, NavigationGrid nav, float radius)
    {
        _safetyIndex = -1; _safetyPool = null; _cornerRevision = -1; _cornerSafety.Clear(); Dependencies.Clear();
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
    // Cache actual grid-corner side cells once so immediate movement checks need no new geometric sampling.
    private void CacheCorners(Vector2 start, NavigationGrid nav, float radius)
    {
        _corridorCorners = new Vector2I[Points.Length][];
        Vector2 from = start;
        for (int index = 0; index < Points.Length; index++)
        {
            Vector2 to = Points[index];
            int steps = Mathf.Max(1, Mathf.CeilToInt(from.DistanceTo(to) / Mathf.Max(0.5f,
                Mathf.Min(nav.Grid.Config.CellSize * 0.1f, radius * 0.5f))));
            Vector2I previous = nav.Grid.WorldToCell(from); List<Vector2I> corners = null;
            for (int i = 1; i <= steps; i++)
            {
                Vector2I cell = nav.Grid.WorldToCell(from.Lerp(to, (float)i / steps)), step = cell - previous;
                if (step.X != 0 && step.Y != 0)
                {
                    corners ??= new();
                    corners.Add(previous + new Vector2I(step.X, 0)); corners.Add(previous + new Vector2I(0, step.Y));
                }
                previous = cell;
            }
            _corridorCorners[index] = corners?.ToArray(); from = to;
        }
    }

    // =========================================================
    // Only motion on the certified segment skips sampling; teleports/deviations keep the original sweep.
    public bool MovementClear(int index, Vector2 from, Vector2 to, NavigationGrid nav, float radius)
    {
        if (nav.Config.EnableRouteSafetyCache && index > 0 && index < Points.Length)
        {
            Vector2 a = Points[index - 1], b = Points[index];
            if (Geometry2D.GetClosestPointToSegment(from, a, b).DistanceSquaredTo(from) <= .000001f
                && Geometry2D.GetClosestPointToSegment(to, a, b).DistanceSquaredTo(to) <= .000001f)
            {
                RouteSegmentSafety pool = nav.SegmentSafety;
                if (_safetyIndex != index || _safetyRadius != radius || _safetyPool != pool || _safetyVersion != pool.Version)
                {
                    _segmentCertified = pool.Certified(nav, a, b, radius);
                    _safetyIndex = index; _safetyRadius = radius; _safetyPool = pool; _safetyVersion = pool.Version;
                }
                if (_segmentCertified) return true;
            }
        }
        return nav.SegmentClear(from, to, radius);
    }

    // =========================================================
    // Cached logical corner links stay strict even for tiny-radius ships with lateral offsets.
    public bool CornerBlocked(int index, NavigationGrid nav)
    {
        if (!nav.Config.EnableRouteSafetyCache) return ComputeCornerBlocked(index, nav);
        if (_cornerRevision != nav.Revision) { _cornerSafety.Clear(); _cornerRevision = nav.Revision; }
        if (!_cornerSafety.TryGetValue(index, out bool blocked)) _cornerSafety[index] = blocked = ComputeCornerBlocked(index, nav);
        return blocked;
    }
    // =========================================================
    // Preserve existing strict logical/corridor corner rules on every navigation revision.
    private bool ComputeCornerBlocked(int index, NavigationGrid nav)
    {
        if (_corridorCorners != null)
        {
            if (index < 0 || index >= _corridorCorners.Length || _corridorCorners[index] == null) return false;
            foreach (Vector2I cell in _corridorCorners[index]) if (nav.Grid.BlocksEnemies(cell)) return true;
            return false;
        }
        if (index <= 0 || index >= _logicalCells.Length) return false;
        Vector2I from = _logicalCells[index - 1], step = _logicalCells[index] - from;
        return Mathf.Abs(step.X) == 1 && Mathf.Abs(step.Y) == 1
            && (nav.Grid.BlocksEnemies(from + new Vector2I(step.X, 0))
                || nav.Grid.BlocksEnemies(from + new Vector2I(0, step.Y)));
    }

}
