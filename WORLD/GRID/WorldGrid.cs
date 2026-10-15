// Cached terrain, object reservations and tower occupancy for the live hand-painted level.
// One transparent canvas draws only visible grid lines; no per-cell nodes or physics.
using Godot;
using System;
using System.Collections.Generic;

public partial class WorldGrid : Node2D
{
    #region Settings and State
    [Export] public WorldMasterConfig Config { get; set; }
    public Rect2 WorldBounds => new(Vector2.Zero,
        new Vector2(Config.WidthCells * Config.CellSize, Config.HeightCells * Config.CellSize));
    private Vector2 _lastCamera = new(float.NaN, float.NaN);
    private Vector2 _lastZoom = new(float.NaN, float.NaN);
    private Vector2I _lastHover = new(int.MinValue, int.MinValue);
    #endregion

    #region Grid Coordinates
    // =========================================================
    // Cell coordinates start at (0,0) in the upper left and increase to the right/down.
    public Vector2I WorldToCell(Vector2 worldPosition)
    {
        Vector2 local = ToLocal(worldPosition);
        return new Vector2I(Mathf.FloorToInt(local.X / Config.CellSize),
            Mathf.FloorToInt(local.Y / Config.CellSize));
    }

    // =========================================================
    // Convert a cell coordinate into its visual center, respecting the grid transform.
    public Vector2 CellToWorldCenter(Vector2I cell)
    {
        float s = Config.CellSize;
        return ToGlobal(new Vector2((cell.X + 0.5f) * s, (cell.Y + 0.5f) * s));
    }

    // =========================================================
    // Shared bounds check for placement previews and cursor diagnostics.
    public bool IsInsideMap(Vector2I cell) =>
        cell.X >= 0 && cell.Y >= 0 && cell.X < Config.WidthCells && cell.Y < Config.HeightCells;
    #endregion

    #region Terrain and Occupancy
    private readonly List<Vector2I> _approaches = new();
    private bool[] _buildable, _terrainBlocked, _occupied, _reserved, _painted;
    public int NavigationRevision { get; private set; }
    public event Action<Vector2I?, bool> NavigationChanged;
    public WorldLevel Level { get; private set; }
    public InteractiveTileManager InteractiveTiles { get; internal set; }
    public event Action TerrainRebuilt;

    // =========================================================
    // Read authored terrain once, after the preceding Level scene has entered the tree.
    public override void _Ready()
    {
        Level = GetNode<WorldLevel>("../Level");
        int count = Config.WidthCells * Config.HeightCells;
        _buildable = new bool[count]; _terrainBlocked = new bool[count];
        _occupied = new bool[count]; _reserved = new bool[count]; _painted = new bool[count];
        RebuildTerrain();
    }

    // =========================================================
    // Rebuild static rules on an explicit map edit/load; retain runtime tower occupancy.
    public void RebuildTerrain()
    {
        bool[] previous = (bool[])_terrainBlocked.Clone();
        Array.Clear(_buildable); Array.Clear(_terrainBlocked); Array.Clear(_reserved); Array.Clear(_painted);
        ValidateTileSet(Level.Decorations, false);
        ReadLayer(Level.Passable, false);
        if (Level.Interactive != null) ReadLayer(Level.Interactive, null);
        ReadLayer(Level.Impassable, true); // Impassable overrides Passable, including build permission.
        ReadFootprints(Level);
        List<Vector2I> approaches = new();
        Node points = Level.Citadel?.GetNodeOrNull<Node>("ApproachPoints");
        if (points != null) foreach (Node child in points.GetChildren())
            if (child is Marker2D marker) approaches.Add(WorldToCell(marker.GlobalPosition));
        bool navigationChanged = NavigationRevision == 0 || approaches.Count != _approaches.Count;
        for (int i = 0; i < approaches.Count && !navigationChanged; i++) navigationChanged = approaches[i] != _approaches[i];
        _approaches.Clear(); _approaches.AddRange(approaches);
        for (int i = 0; i < previous.Length && !navigationChanged; i++) navigationChanged = previous[i] != _terrainBlocked[i];
        if (navigationChanged) { NavigationRevision++; NavigationChanged?.Invoke(null, true); }
        TerrainRebuilt?.Invoke();
        QueueRedraw();
    }

    // =========================================================
    // Custom data defines behavior; Decorations deliberately never enter this cache.
    private void ReadLayer(TileMapLayer layer, bool? expectedBlocked)
    {
        ValidateTileSet(layer);
        int mismatches = 0, overlaps = 0, invalidTiles = 0;
        foreach (Vector2I painted in layer.GetUsedCells())
        {
            TileData tile = layer.GetCellTileData(painted);
            if (tile == null) { invalidTiles++; continue; }
            Vector2I cell = WorldToCell(layer.ToGlobal(layer.MapToLocal(painted)));
            if (!IsInsideMap(cell)) { GD.PushWarning($"Ignoring terrain outside map: {cell}"); continue; }
            int index = Index(cell);
            if (_painted[index] && expectedBlocked != null) overlaps++;
            _painted[index] = true;
            if (expectedBlocked.HasValue && tile.GetCustomData("blocks_enemies").AsBool() != expectedBlocked.Value) mismatches++;
            _buildable[index] = tile.GetCustomData("can_build").AsBool();
            _terrainBlocked[index] = tile.GetCustomData("blocks_enemies").AsBool();
        }
        if (mismatches > 0) GD.PushWarning($"{layer.GetPath()}: {mismatches} tiles have the wrong movement category; check the assigned TileSet.");
        if (invalidTiles > 0) GD.PushWarning($"{layer.GetPath()}: {invalidTiles} cells reference missing tiles.");
        if (overlaps > 0) GD.PushWarning($"Terrain has {overlaps} gameplay overlaps. Impassable wins; erase the old layer before repainting.");
    }

    // =========================================================
    // Reject broken palette setup at load/rebuild, never through a per-frame scan.
    private void ValidateTileSet(TileMapLayer layer, bool gameplay = true)
    {
        if (layer.TileSet == null || layer.TileSet.TileSize != Vector2I.One * Config.CellSize)
            throw new InvalidOperationException($"{layer.GetPath()}: TileSet tile size must match WorldMasterConfig.CellSize.");
        if (!gameplay) return;
        foreach (string key in new[] { "can_build", "blocks_enemies" })
        {
            int index = layer.TileSet.GetCustomDataLayerByName(key);
            if (index < 0 || layer.TileSet.GetCustomDataLayerType(index) != Variant.Type.Bool)
                throw new InvalidOperationException($"{layer.GetPath()}: TileSet requires Boolean {key} metadata.");
        }
    }

    // =========================================================
    // Portal and objective cells reject towers, independent of painted terrain.
    private void ReadFootprints(Node parent)
    {
        if (parent is WorldObjectFootprint footprint)
        {
            Vector2I start = WorldToCell(footprint.GlobalPosition);
            for (int y = 0; y < footprint.SizeCells.Y; y++)
                for (int x = 0; x < footprint.SizeCells.X; x++)
                {
                    Vector2I cell = start + new Vector2I(x, y);
                    if (!IsInsideMap(cell)) continue;
                    int index = Index(cell);
                    _reserved[index] = true;
                    _terrainBlocked[index] |= footprint.BlocksEnemies;
                }
        }
        foreach (Node child in parent.GetChildren()) ReadFootprints(child);
    }

    // =========================================================
    // Constant-time shared queries; outside-map cells cannot be traversed or built on.
    private int Index(Vector2I cell) => cell.Y * Config.WidthCells + cell.X;
    public bool CanBuild(Vector2I cell) => IsInsideMap(cell) && _buildable[Index(cell)] && !_reserved[Index(cell)];
    public bool IsOccupied(Vector2I cell) => IsInsideMap(cell) && _occupied[Index(cell)];
    public bool TerrainBlocksEnemies(Vector2I cell) => !IsInsideMap(cell) || _terrainBlocked[Index(cell)];
    public bool BlocksEnemies(Vector2I cell) => !IsInsideMap(cell) || _terrainBlocked[Index(cell)] || _occupied[Index(cell)];
    public string PlacementFailure(Vector2I cell)
    {
        if (!IsInsideMap(cell)) return "Outside the map.";
        if (_reserved[Index(cell)]) return "Reserved for a Portal or the Citadel.";
        if (!_buildable[Index(cell)]) return "This terrain does not allow towers.";
        if (_occupied[Index(cell)]) return "A tower already occupies this cell.";
        return "";
    }

    // =========================================================
    // Commit occupancy once; navigation revision changes only for new obstacles.
    public bool TryOccupy(Vector2I cell)
    {
        if (PlacementFailure(cell).Length != 0) return false;
        _occupied[Index(cell)] = true;
        if (!_terrainBlocked[Index(cell)])
        { NavigationRevision++; NavigationChanged?.Invoke(cell, _occupied[Index(cell)]); }
        return true;
    }

    // =========================================================
    // Removal is ready for future selling/destruction mechanics.
    public void ReleaseOccupancy(Vector2I cell)
    {
        if (!IsOccupied(cell)) return;
        _occupied[Index(cell)] = false;
        if (!_terrainBlocked[Index(cell)])
        { NavigationRevision++; NavigationChanged?.Invoke(cell, _occupied[Index(cell)]); }
    }
    #endregion

    #region Redraw
    // =========================================================
    // Queue new drawing only if the visible area or hovered square has changed.
    public override void _Process(double delta)
    {
        Camera2D camera = GetViewport().GetCamera2D();
        if (camera == null) return;
        Vector2I hover = WorldToCell(GetGlobalMousePosition());
        if (_lastCamera == camera.GlobalPosition && _lastZoom == camera.Zoom
            && (!Config.ShowHoverCell || _lastHover == hover)) return;

        _lastCamera = camera.GlobalPosition;
        _lastZoom = camera.Zoom;
        _lastHover = hover;
        QueueRedraw();
    }

    // =========================================================
    // Use the inverse viewport transform to limit line drawing to visible cells.
    public override void _Draw()
    {
        Rect2 map = WorldBounds;
        // The level owns its background; keep this overlay transparent.

        Transform2D localFromViewport = GetGlobalTransformWithCanvas().AffineInverse();
        Vector2 a = localFromViewport * Vector2.Zero;
        Vector2 b = localFromViewport * GetViewportRect().Size;
        float left = Mathf.Max(0f, Mathf.Min(a.X, b.X));
        float top = Mathf.Max(0f, Mathf.Min(a.Y, b.Y));
        float right = Mathf.Min(map.End.X, Mathf.Max(a.X, b.X));
        float bottom = Mathf.Min(map.End.Y, Mathf.Max(a.Y, b.Y));
        if (left > right || top > bottom) return;

        float zoom = GetViewport().GetCamera2D()?.Zoom.X ?? 1f;
        float lineWidth = 1f / zoom;
        int size = Config.CellSize;
        if (Config.ShowGrid)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(left / size));
            int x1 = Mathf.Min(Config.WidthCells, Mathf.CeilToInt(right / size));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(top / size));
            int y1 = Mathf.Min(Config.HeightCells, Mathf.CeilToInt(bottom / size));
            for (int x = x0; x <= x1; x++)
                DrawLine(new Vector2(x * size, top), new Vector2(x * size, bottom), Config.GridColor, lineWidth);
            for (int y = y0; y <= y1; y++)
                DrawLine(new Vector2(left, y * size), new Vector2(right, y * size), Config.GridColor, lineWidth);
        }

        if (Config.ShowHoverCell && IsInsideMap(_lastHover))
            DrawRect(new Rect2(_lastHover.X * size, _lastHover.Y * size, size, size), Config.HoverColor);

        DrawRect(map, Config.BorderColor, filled: false, width: 2f / zoom);
    }
    #endregion
}
