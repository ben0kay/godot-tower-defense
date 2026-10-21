// Level-owned bounded route reuse. Only immutable templates are retained; ships keep private progress/geometry.
using Godot;
using System;
using System.Collections.Generic;

public sealed class SharedRoutePool
{
    #region Cache State
    private readonly record struct RawKey(Vector2I Start, Vector2I Goal, int Profile, int Crowd);
    private readonly record struct RefinedKey(ulong Hash, int Count, Vector2I StartCell, float Radius, float Offset,
        bool Corridor, uint Seed, float Strength, int Spacing);
    private sealed record Raw(Vector2[] Points, HashSet<Vector2I> Cells);
    private sealed record Refined(Vector2[] Logical, EnemyRoute Template);
    private readonly Dictionary<RawKey, Raw> _raw = new();
    private readonly Dictionary<RefinedKey, Refined> _refined = new();
    private readonly List<RawKey> _rawRemoval = new();
    private readonly List<RefinedKey> _refinedRemoval = new();
    private int _costRevision = -1;
    public long RawHits { get; private set; }
    public long RefinedHits { get; private set; }
    public long RefinedBuilds { get; private set; }
    public int RawCount => _raw.Count;
    public int RefinedCount => _refined.Count;
    // =========================================================
    // Full terrain/profile resets release templates, but never mutate arrays already used by ships.
    public void Clear() { _raw.Clear(); _refined.Clear(); }
    // =========================================================
    // Cost profile edits change the meaning of every cached base route.
    private void Refresh(NavigationGrid nav)
    { if (_costRevision != nav.CostRevision) { Clear(); _costRevision = nav.CostRevision; } }
    // =========================================================
    // A full pool evicts one template rather than dropping all warmed paths.
    private static void EvictOne<TKey, TValue>(Dictionary<TKey, TValue> entries)
    { var iterator = entries.GetEnumerator(); if (!iterator.MoveNext()) return; TKey key = iterator.Current.Key; iterator.Dispose(); entries.Remove(key); }
    #endregion

    #region Raw Paths
    // =========================================================
    // Requests keep consuming the existing query budget even on hits; no synchronous startup flood is added.
    public bool TryRaw(NavigationGrid nav, Vector2 position, Vector2I goal, int profile, out Vector2[] points)
    {
        Refresh(nav); points = null;
        if (!nav.Config.EnableSharedRoutePool) return false;
        RawKey key = new(nav.Grid.WorldToCell(position), goal, profile,
            nav.Config.CongestionInfluence > 0 ? nav.CongestionRevision : 0);
        if (!_raw.TryGetValue(key, out Raw route)) return false;
        RawHits++; points = (Vector2[])route.Points.Clone(); return true;
    }
    // =========================================================
    // Adjacent cells cover diagonal corner dependencies; failures are not retained across obstacle removal.
    public void StoreRaw(NavigationGrid nav, Vector2 position, Vector2I goal, int profile, Vector2[] points)
    {
        if (!nav.Config.EnableSharedRoutePool || points.Length == 0) return;
        int limit = Math.Clamp(nav.Config.SharedRouteCacheEntries, 16, 2048);
        if (_raw.Count >= limit) EvictOne(_raw);
        HashSet<Vector2I> cells = new();
        foreach (Vector2 point in points)
        {
            Vector2I cell = nav.Grid.WorldToCell(point);
            for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) cells.Add(cell + new Vector2I(x, y));
        }
        _raw[new(nav.Grid.WorldToCell(position), goal, profile,
            nav.Config.CongestionInfluence > 0 ? nav.CongestionRevision : 0)] = new((Vector2[])points.Clone(), cells);
    }
    #endregion

    #region Finished Variations
    // =========================================================
    // Stable finite variants reuse expensive corridor probes. Exact logical tails guard hash collisions and checkpoint identities.
    public void Build(NavigationGrid nav, EnemyRoute destination, Vector2[] logical, Vector2 start, float radius,
        float offset, bool corridor, uint seed, Vector2 forward)
    {
        Refresh(nav);
        int variants = Math.Clamp(nav.Config.SharedRouteVariants, 1, 32);
        if (!nav.Config.EnableSharedRoutePool || logical.Length < 3 || logical[0] != start)
        { BuildOriginal(destination, logical, start, nav, radius, offset, corridor, seed, forward); return; }
        // Quantize only optional lateral variation. Zero stays zero, including types that disallow offsets.
        int slot = (int)(seed % (uint)variants);
        float maximum = Math.Max(0, nav.Config.PathOffsetMaxPixels);
        if (offset != 0 && maximum > 0) offset = variants == 1 ? 0
            : (slot * 2f / (variants - 1) - 1) * maximum;
        uint variantSeed = CorridorPreference.Seed(seed % (uint)variants, nav.Config.RoutePreferenceSeed);
        ulong hash = 1469598103934665603UL;
        for (int i = 1; i < logical.Length; i++)
        { hash = unchecked((hash ^ (uint)BitConverter.SingleToInt32Bits(logical[i].X)) * 1099511628211UL); hash = unchecked((hash ^ (uint)BitConverter.SingleToInt32Bits(logical[i].Y)) * 1099511628211UL); }
        RefinedKey key = new(hash, logical.Length, nav.Grid.WorldToCell(start), radius, offset, corridor,
            corridor ? variantSeed : 0, nav.Config.CorridorVariationStrength, nav.Config.CorridorVariationSpacingCells);
        if (_refined.TryGetValue(key, out Refined cached) && SameTail(logical, cached.Logical)
            && destination.TryUseShared(cached.Template, logical, start, nav, radius, forward))
        { RefinedHits++; return; }
        EnemyRoute template = new();
        BuildOriginal(template, logical, start, nav, radius, offset, corridor, variantSeed, forward); RefinedBuilds++;
        if (_refined.Count >= Math.Clamp(nav.Config.SharedRouteCacheEntries, 16, 2048)) EvictOne(_refined);
        _refined[key] = new((Vector2[])logical.Clone(), template);
        // Fresh templates use this exact origin/heading and need no speculative connector validation.
        destination.UseShared(template, logical, start, nav, radius);
    }
    // =========================================================
    // Keep the original builder as the authoritative miss/opt-out implementation.
    private static void BuildOriginal(EnemyRoute route, Vector2[] logical, Vector2 start, NavigationGrid nav,
        float radius, float offset, bool corridor, uint seed, Vector2 forward)
    {
        if (corridor) route.BuildCorridor(logical, start, nav, radius, offset, seed, nav.Config, forward);
        else route.Build(logical, start, nav, radius, offset, forward);
    }
    // =========================================================
    // A hash match alone never authorizes a different route or endpoint.
    private static bool SameTail(Vector2[] a, Vector2[] b)
    { if (a.Length != b.Length) return false; for (int i = 1; i < a.Length; i++) if (a[i] != b[i]) return false; return true; }
    // =========================================================
    // Tower edits affect only intersecting templates. Unblocking clears raw searches so new shorter routes can be discovered.
    public void Invalidate(Vector2I cell, bool blocked)
    {
        _rawRemoval.Clear(); _refinedRemoval.Clear();
        if (!blocked) _raw.Clear();
        else foreach (var entry in _raw) if (entry.Value.Cells.Contains(cell)) _rawRemoval.Add(entry.Key);
        foreach (var key in _rawRemoval) _raw.Remove(key);
        foreach (var entry in _refined) if (entry.Value.Template.Dependencies.ContainsKey(cell)) _refinedRemoval.Add(entry.Key);
        foreach (var key in _refinedRemoval) _refined.Remove(key);
    }
    #endregion
}
