// Dormant terrain-edge foundation: rebuild a material ID cache only after explicitly attaching to WorldGrid.
using Godot;
using System;
using System.Collections.Generic;

public partial class TerrainEdgeController : Node2D
{
    [Export] public TerrainEdgeMasterConfig Config { get; set; }
    private WorldGrid _grid;
    private readonly Dictionary<Vector2I, string> _materials = new();
    public IReadOnlyDictionary<Vector2I, string> Materials => _materials;
    public event Action MaterialsRebuilt;

    // =========================================================
    // Codex will attach one world-scoped instance after placing the visual renderer in the scene.
    // No instances are added to current gameplay scenes by this foundation pass.
    public void Attach(WorldGrid grid)
    {
        if (_grid == grid) return;
        Detach(); _grid = grid;
        if (_grid == null) return;
        _grid.TerrainRebuilt += Rebuild;
        Rebuild();
    }

    // =========================================================
    // Read only on explicit rebuild, honouring the same Passable -> Interactive -> Impassable precedence as WorldGrid.
    // Unrecognized tiles on a higher layer clear the lower layer's visual identity.
    public void Rebuild()
    {
        _materials.Clear();
        if (Config != null && Config.Enabled && GodotObject.IsInstanceValid(_grid) && _grid.Level != null)
        {
            ReadLayer(_grid.Level.Passable, TerrainEdgeLayer.Passable);
            ReadLayer(_grid.Level.Interactive, TerrainEdgeLayer.Interactive);
            ReadLayer(_grid.Level.Impassable, TerrainEdgeLayer.Impassable);
        }
        MaterialsRebuilt?.Invoke();
    }

    // =========================================================
    // Convert TileMap coordinates to WorldGrid coordinates, including each layer's scene transform.
    private void ReadLayer(TileMapLayer layer, TerrainEdgeLayer category)
    {
        if (layer == null) return;
        foreach (Vector2I authored in layer.GetUsedCells())
        {
            Vector2I cell = _grid.WorldToCell(layer.ToGlobal(layer.MapToLocal(authored)));
            if (!_grid.IsInsideMap(cell)) continue;
            _materials.Remove(cell);
            TerrainEdgeProfile profile = Config.Find(category, layer.GetCellSourceId(authored),
                layer.GetCellAtlasCoords(authored));
            if (profile != null) _materials[cell] = profile.MaterialId;
        }
    }

    // =========================================================
    // Future visual renderer uses these masks; no pathfinding, occupancy or construction flags are changed.
    public TerrainEdgeNeighbours GetNeighbours(Vector2I cell) =>
        _materials.TryGetValue(cell, out string material)
            ? TerrainEdgeNeighbourhood.Matching(cell, material, _materials) : TerrainEdgeNeighbours.None;

    // =========================================================
    // Avoid retaining an unloaded world or duplicate terrain-rebuild subscribers.
    public void Detach()
    {
        if (GodotObject.IsInstanceValid(_grid)) _grid.TerrainRebuilt -= Rebuild;
        _grid = null; _materials.Clear();
    }

    public override void _ExitTree() => Detach();
}
