// Opt-in mixed/weighted source selection and cached named boundary sectors; no clocks or additional path requests.
using Godot;
using System;
using System.Collections.Generic;
public partial class SpawnManager
{
    private readonly List<LevelConfig.MapEdge> _edgeSides = new();
    private readonly Dictionary<string, HashSet<int>> _sectorSources = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SpawnEdgeSector> _sectors = new(StringComparer.Ordinal);
    private string _sectorError = "";
    private readonly List<Rect2> _edgePortalBounds = new();
    private readonly List<int> _policyPortals = new();
    private readonly List<int>[] _policyEdges = { new(), new(), new(), new() };
    private static readonly LevelConfig.MapEdge[] Sides =
        { LevelConfig.MapEdge.Top, LevelConfig.MapEdge.Bottom, LevelConfig.MapEdge.Left, LevelConfig.MapEdge.Right };
    private void AddEdgeSource(Vector2I cell, LevelConfig.MapEdge side)
    { _entries.Add(Paths.Navigation.Grid.CellToWorldCenter(cell)); _edgeSides.Add(side); }

    // =========================================================
    // Every portal has one share; each eligible side has its authored weight, independent of boundary length.
    private int SelectPolicySource(SpawnSourceOptions source, float radius, RandomNumberGenerator random)
    {
        _policyPortals.Clear(); foreach (var list in _policyEdges) list.Clear();
        bool edges = source.Policy is SpawnSourcePolicy.AnyEligible or SpawnSourcePolicy.EdgesOnly;
        bool portals = source.Policy is SpawnSourcePolicy.AnyEligible or SpawnSourcePolicy.PortalsOnly;
        HashSet<int> sector = null;
        if (source.EdgeSector.Length > 0 && !_sectorSources.TryGetValue(source.EdgeSector, out sector)) return -1;
        for (int i = 0; i < _entries.Count; i++)
        {
            bool portal = i < _portalCount;
            if (portal ? !portals : !edges) continue;
            if (!portal && sector != null && (!sector.Contains(i) || !SectorClearance(i, _sectors[source.EdgeSector], radius))) continue;
            if (!SourceUsable(i, radius) || !Paths.Navigation.HasClearance(_entries[i], radius)) continue;
            if (portal) _policyPortals.Add(i);
            else
            {
                int side = Array.IndexOf(Sides, _edgeSides[i - _portalCount]);
                if (side >= 0 && source.Weight(Sides[side]) > 0) _policyEdges[side].Add(i);
            }
        }
        double total = _policyPortals.Count;
        for (int i = 0; i < 4; i++) if (_policyEdges[i].Count > 0) total += source.Weight(Sides[i]);
        if (total <= 0) return -1;
        double roll = random.Randf() * total;
        if (roll < _policyPortals.Count)
            return _policyPortals[random.RandiRange(0, _policyPortals.Count - 1)];
        roll -= _policyPortals.Count;
        int fallback = -1;
        for (int i = 0; i < 4; i++)
        {
            if (_policyEdges[i].Count == 0) continue;
            fallback = i; double weight = source.Weight(Sides[i]);
            if (roll < weight) return _policyEdges[i][random.RandiRange(0, _policyEdges[i].Count - 1)];
            roll -= weight;
        }
        return fallback < 0 ? _policyPortals[^1] :
            _policyEdges[fallback][random.RandiRange(0, _policyEdges[fallback].Count - 1)];
    }

    // =========================================================
    // Geometry is cached alongside the existing navigation-revision source cache, not once per enemy.
    private void CacheEdgeSectors()
    {
        _sectorSources.Clear(); _sectors.Clear(); _edgePortalBounds.Clear(); _sectorError = "";
        var grid = Paths.Navigation.Grid; var settings = grid.Level.Settings;
        try
        {
            if (settings.EdgeSectors == null) throw new ArgumentException("Missing EdgeSectors array.");
            if (settings.EdgeSectors.Count == 0) return;
            var portalBounds = _edgePortalBounds;
            foreach (var path in settings.ActivePortals)
            {
                var portal = grid.Level.GetNodeOrNull<Node2D>(path);
                if (portal == null) continue;
                Vector2I anchor = grid.WorldToCell(portal.GlobalPosition);
                Vector2I size = portal is WorldObjectFootprint footprint ? footprint.SizeCells : Vector2I.One;
                portalBounds.Add(new((Vector2)anchor * grid.Config.CellSize, (Vector2)size * grid.Config.CellSize));
            }
            foreach (var sector in settings.EdgeSectors)
            {
                if (sector == null) throw new ArgumentException("Missing edge sector resource.");
                sector.Validate();
                if (!_sectors.TryAdd(sector.SectorId, sector)) throw new ArgumentException("Duplicate edge sector: " + sector.SectorId);
                var indices = new HashSet<int>(); _sectorSources.Add(sector.SectorId, indices);
                for (int i = _portalCount; i < _entries.Count; i++)
                {
                    if (_edgeSides[i - _portalCount] != sector.Side) continue;
                    Vector2 point = grid.ToLocal(_entries[i]);
                    bool horizontal = sector.Side is LevelConfig.MapEdge.Top or LevelConfig.MapEdge.Bottom;
                    float fraction = horizontal ? point.X / (grid.Config.WidthCells * grid.Config.CellSize)
                        : point.Y / (grid.Config.HeightCells * grid.Config.CellSize);
                    if (fraction < sector.StartFraction || fraction >= sector.EndFraction) continue;
                    bool excluded = false;
                    foreach (var rect in portalBounds)
                    {
                        Vector2 nearest = new(Mathf.Clamp(point.X, rect.Position.X, rect.End.X), Mathf.Clamp(point.Y, rect.Position.Y, rect.End.Y));
                        if (sector.ExcludePortalFootprints && rect.HasPoint(point) ||
                            point.DistanceTo(nearest) < sector.MinimumPortalDistanceCells * grid.Config.CellSize) { excluded = true; break; }
                    }
                    if (!excluded) indices.Add(i);
                }
            }
        }
        catch (ArgumentException error) { _sectorError = error.Message; }
    }

    private bool SectorClearance(int index, SpawnEdgeSector sector, float radius)
    {
        float minimum = sector.MinimumPortalDistanceCells * Paths.Navigation.Grid.Config.CellSize +
            (sector.ExcludePortalFootprints ? radius : 0);
        if (minimum <= 0) return true;
        Vector2 point = Paths.Navigation.Grid.ToLocal(_entries[index]);
        foreach (var rect in _edgePortalBounds)
        {
            Vector2 nearest = new(Mathf.Clamp(point.X, rect.Position.X, rect.End.X), Mathf.Clamp(point.Y, rect.Position.Y, rect.End.Y));
            if (point.DistanceTo(nearest) < minimum) return false;
        }
        return true;
    }

    // =========================================================
    // Authoring errors fail before scheduling; blocked geometry retries explicitly rather than falling back.
    private void ValidateSourcePolicies(WaveDefinition[] waves)
    {
        if (_sectorError.Length > 0) throw new ArgumentException(_sectorError);
        var settings = Paths.Navigation.Grid.Level.Settings;
        foreach (var wave in waves) foreach (var batch in wave.Batches) foreach (var entry in batch.Entries)
        {
            var source = entry.Source ?? batch.Source ?? wave.Source ?? SpawnSourceOptions.Legacy;
            source.Validate(entry.Portal);
            string context = $"Wave {wave.Number}: ";
            if ((source.Policy is SpawnSourcePolicy.PortalsOnly or SpawnSourcePolicy.RequiredPortal) &&
                settings.SpawnMode == LevelConfig.SpawnSourceMode.MapEdges)
                throw new ArgumentException(context + "Portal policy is forbidden by level SpawnMode.");
            if (source.Policy == SpawnSourcePolicy.EdgesOnly &&
                (settings.SpawnMode == LevelConfig.SpawnSourceMode.Portals || Paths.Navigation.Destinations.Count != 1))
                throw new ArgumentException(context + "EdgesOnly needs permitted edges and exactly one Citadel.");
            if (source.EdgeSector.Length == 0) continue;
            if (!_sectors.TryGetValue(source.EdgeSector, out var sector))
                throw new ArgumentException(context + "Unknown required edge sector: " + source.EdgeSector);
            if (_sectorSources[source.EdgeSector].Count == 0 || source.Weight(sector.Side) <= 0)
                throw new ArgumentException(context + "Required edge sector has no allowed cells or positive side weight: " + source.EdgeSector);
        }
    }
}
