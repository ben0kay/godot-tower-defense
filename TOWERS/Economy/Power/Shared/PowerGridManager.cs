// World-owned power topology foundation; listens only to terrain changes and never gates tower gameplay in PW1.
using Godot;
using System;

public partial class PowerGridManager : Node
{
    private WorldGrid _grid;
    private readonly PowerIslandMap _islands = new();
    private int _terrainRevision = -1;
    public WorldGrid Grid => _grid;
    public int IslandCount => _islands.IslandCount;
    public int ConductingCellCount => _islands.ConductingCellCount;
    public int Revision { get; private set; }
    public int RebuildCount { get; private set; }
    public double LastRebuildMilliseconds { get; private set; }
    public event Action NetworksChanged;

    // =========================================================
    // Grid precedes this node in World.tscn; no deferred tick, timer, autoload or per-building node is required.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _grid = GetNode<WorldGrid>("../Grid");
        _grid.TerrainRebuilt += RefreshTerrain;
        RefreshTerrain();
    }

    // =========================================================
    // Occupancy does not emit TerrainRebuilt; unchanged conductivity returns before flood-filling or allocating.
    private void RefreshTerrain()
    {
        if (_terrainRevision == _grid.PowerTerrainRevision) return;
        ulong start = Time.GetTicksUsec();
        _islands.Rebuild(_grid.Config.WidthCells, _grid.Config.HeightCells, _grid.PowerConductivity);
        _terrainRevision = _grid.PowerTerrainRevision; Revision++; RebuildCount++;
        LastRebuildMilliseconds = (Time.GetTicksUsec() - start) / 1000.0;
        NetworksChanged?.Invoke();
    }

    // =========================================================
    // Shared O(1) cell queries; future source/consumer registration will use the same terrain authority.
    public int RegionAt(Vector2I cell) => _islands.RegionAt(cell.X, cell.Y);
    public int CellCount(int region) => _islands.CellCount(region);
    public int RegionForFootprint(Vector2I anchor, Vector2I size) =>
        _islands.RegionForFootprint(anchor.X, anchor.Y, size.X, size.Y);

    // =========================================================
    // Release the world subscription whichever subtree exits first; no cached node survives a level reload.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_grid)) _grid.TerrainRebuilt -= RefreshTerrain;
        _grid = null;
    }
}
