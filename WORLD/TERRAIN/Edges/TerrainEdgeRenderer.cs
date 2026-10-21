// Cached alpha masks on existing tile layers, plus recorded inward-corner fill quads; no per-tile nodes or frame scans.
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class TerrainEdgeRenderer : Node2D
{
    #region Ownership
    private TerrainEdgeController _controller;
    private readonly List<Surface> _surfaces = new();
    public int SurfaceCount => _surfaces.Count;
    public int RebuildCount { get; private set; }
    public double LastBuildMilliseconds { get; private set; }
    public long MaskBytes { get; private set; }
    private sealed class Surface
    {
        public TileMapLayer Layer;
        public Material Original;
        public bool ParentMaterial;
        public ShaderMaterial Mask;
        public ImageTexture Texture, EnabledCells;
        public Node2D Fill;
    }
    // =========================================================
    // One world instance attaches after Grid startup; ordinary occupancy changes never trigger this subscription.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        var controller = GetParent<TerrainEdgeController>(); Bind(controller);
        controller.Attach(GetNode<WorldGrid>("../../Grid"));
    }
    // =========================================================
    // Explicit binding also supports isolated fixtures and later shared consumers.
    public void Bind(TerrainEdgeController controller)
    {
        if (_controller == controller) return;
        Unbind(); _controller = controller;
        if (_controller != null) _controller.MaterialsRebuilt += Rebuild;
        Rebuild();
    }
    #endregion

    #region Generation
    // =========================================================
    // Generate once per terrain event. Unsupported/custom-material layers remain completely unchanged.
    public void Rebuild()
    {
        Clear(); LastBuildMilliseconds = 0; RebuildCount++; long started = Stopwatch.GetTimestamp();
        if (_controller?.Config?.Enabled != true || !GodotObject.IsInstanceValid(_controller.Grid)) return;
        HashSet<TileMapLayer> layers = new();
        foreach (var tile in _controller.Tiles.Values) if (tile.Profile.RenderEdges && tile.Profile.Shape == TerrainEdgeShape.Soft) layers.Add(tile.Layer);
        foreach (TileMapLayer layer in layers)
        {
            if (layer.Material != null || layer.UseParentMaterial || layer.YSortEnabled || layer.TileSet.TileSize != new Vector2I(64, 64) || _controller.Grid.Config.CellSize != 64)
            { GD.PushWarning("Terrain edges skipped unsupported/custom-material layer: " + layer.Name); continue; }
            Transform2D relative = _controller.Grid.GlobalTransform.AffineInverse() * layer.GlobalTransform;
            Vector2 offset = (relative * layer.MapToLocal(Vector2I.Zero) - Vector2.One * 32) / 64;
            bool aligned = relative.X.IsEqualApprox(Vector2.Right) && relative.Y.IsEqualApprox(Vector2.Down)
                && offset.IsEqualApprox(offset.Round());
            bool atlasSafe = true;
            foreach (var tile in _controller.Tiles.Values)
                if (tile.Layer == layer && tile.Profile.RenderEdges
                    && (layer.TileSet.GetSource(layer.GetCellSourceId(tile.Authored)) is not TileSetAtlasSource atlas
                        || atlas.GetTileTextureRegion(layer.GetCellAtlasCoords(tile.Authored)).Size != new Vector2I(64, 64)
                        || atlas.GetTileAnimationFramesCount(layer.GetCellAtlasCoords(tile.Authored)) > 1
                        || layer.GetCellTileData(tile.Authored).TextureOrigin != Vector2I.Zero
                        || layer.GetCellTileData(tile.Authored).Material != null
                        || layer.GetCellTileData(tile.Authored).Modulate != Colors.White
                        || layer.GetCellTileData(tile.Authored).ZIndex != 0)) atlasSafe = false;
            if (!aligned || !atlasSafe) { GD.PushWarning("Terrain edges require grid-aligned static 64px artwork: " + layer.Name); continue; }
            Build(layer);
        }
        LastBuildMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }
    // =========================================================
    // A bounded L8 mask preserves unknown painted tiles and includes one empty-cell margin for inward fillets.
    private void Build(TileMapLayer layer)
    {
        WorldGrid grid = _controller.Grid; var config = _controller.Config;
        Rect2I bounds = default; bool first = true;
        foreach (Vector2I authored in layer.GetUsedCells())
        {
            Vector2I cell = grid.WorldToCell(layer.ToGlobal(layer.MapToLocal(authored)));
            if (first) { bounds = new(cell, Vector2I.One); first = false; } else bounds = bounds.Expand(cell);
        }
        if (first) return;
        bounds = bounds.Grow(1); int resolution = Mathf.Clamp(config.MaskPixelsPerCell, 8, 32);
        long count = (long)bounds.Size.X * bounds.Size.Y * resolution * resolution;
        if (count > Mathf.Clamp(config.MaximumMaskMegapixels, 1, 64) * 1048576L)
        { GD.PushWarning("Terrain edge mask budget exceeded; original tiles retained."); return; }
        int width = bounds.Size.X * resolution, height = bounds.Size.Y * resolution;
        byte[] pixels = new byte[(int)count], enabled = new byte[bounds.Size.X * bounds.Size.Y]; List<(Vector2I Cell, TerrainEdgeController.Tile Tile, Vector2I Direction)> fills = new();
        for (int y = bounds.Position.Y; y < bounds.End.Y; y++) for (int x = bounds.Position.X; x < bounds.End.X; x++)
        {
            Vector2I cell = new(x, y);
            bool painted = _controller.Painted.Contains(cell);
            bool selected = _controller.Tiles.TryGetValue(cell, out var tile) && tile.Layer == layer && tile.Profile.RenderEdges && tile.Profile.Shape == TerrainEdgeShape.Soft;
            var occupied = Occupied(cell);
            var corners = painted ? new List<(Vector2I Direction, TerrainEdgeController.Tile Tile)>() : Corners(cell, layer);
            foreach (var corner in corners) fills.Add((cell, corner.Tile, corner.Direction));
            if (selected || corners.Count > 0) enabled[(y - bounds.Position.Y) * bounds.Size.X + x - bounds.Position.X] = 255;
            if (!painted && corners.Count == 0) continue;
            if (painted && (!selected || ((int)occupied & 15) == 15))
            {
                for (int py = 0; py < resolution; py++)
                    Array.Fill(pixels, (byte)255, ((y - bounds.Position.Y) * resolution + py) * width + (x - bounds.Position.X) * resolution, resolution);
                continue;
            }
            for (int py = 0; py < resolution; py++) for (int px = 0; px < resolution; px++)
            {
                Vector2 p = new((px + .5f) / resolution, (py + .5f) / resolution);
                float alpha = painted ? selected ? TerrainEdgeMask.Coverage(p, occupied, tile.Profile, cell, config.VariationSeed) : 1 : 0;
                foreach (var corner in corners)
                {
                    Vector2 q = new(corner.Direction.X < 0 ? p.X : 1 - p.X, corner.Direction.Y < 0 ? p.Y : 1 - p.Y);
                    alpha = Mathf.Max(alpha, TerrainEdgeMask.InnerCorner(q, corner.Tile.Profile));
                }
                pixels[((y - bounds.Position.Y) * resolution + py) * width + (x - bounds.Position.X) * resolution + px] = (byte)Mathf.RoundToInt(alpha * 255);
            }
        }
        using Image image = Image.CreateFromData(width, height, false, Image.Format.L8, pixels);
        ImageTexture texture = ImageTexture.CreateFromImage(image);
        using Image enabledImage = Image.CreateFromData(bounds.Size.X, bounds.Size.Y, false, Image.Format.L8, enabled);
        ImageTexture enabledTexture = ImageTexture.CreateFromImage(enabledImage);
        ShaderMaterial material = new() { Shader = GD.Load<Shader>("res://WORLD/TERRAIN/Edges/TerrainEdgeMask.gdshader") };
        material.SetShaderParameter("apply_cells", enabledTexture); material.SetShaderParameter("edge_mask", texture); material.SetShaderParameter("world_to_grid", grid.GlobalTransform.AffineInverse());
        material.SetShaderParameter("mask_origin", (Vector2)bounds.Position); material.SetShaderParameter("mask_extent", (Vector2)bounds.Size);
        material.SetShaderParameter("cell_size", grid.Config.CellSize);
        TerrainEdgeFillRenderer fill = new() { Name = "CornerFill", Material = material };
        layer.AddChild(fill); fill.GlobalTransform = grid.GlobalTransform;
        foreach (var entry in fills)
        {
            TileMapLayer source = entry.Tile.Layer; Vector2I at = entry.Tile.Authored;
            if (source.TileSet.GetSource(source.GetCellSourceId(at)) is not TileSetAtlasSource atlas) continue;
            Rect2I region = atlas.GetTileTextureRegion(source.GetCellAtlasCoords(at));
            Vector2 cornerOffset = new(entry.Direction.X < 0 ? 0 : 32, entry.Direction.Y < 0 ? 0 : 32);
            Rect2 destination = new((Vector2)entry.Cell * grid.Config.CellSize + cornerOffset, Vector2.One * 32);
            fill.Add(atlas.Texture, destination, new Rect2((Vector2)region.Position + cornerOffset, Vector2.One * 32));
        }
        _surfaces.Add(new() { Layer = layer, Original = layer.Material, ParentMaterial = layer.UseParentMaterial, Texture = texture, EnabledCells = enabledTexture, Mask = material, Fill = fill });
        layer.Material = material; layer.UseParentMaterial = false; MaskBytes += count + enabled.Length;
    }
    // =========================================================
    // Any painted material protects a closed join; empty terrain is the only transition target in E2.
    private TerrainEdgeNeighbours Occupied(Vector2I cell)
    {
        TerrainEdgeNeighbours mask = 0;
        if (_controller.Painted.Contains(cell + Vector2I.Up)) mask |= TerrainEdgeNeighbours.North;
        if (_controller.Painted.Contains(cell + Vector2I.Right)) mask |= TerrainEdgeNeighbours.East;
        if (_controller.Painted.Contains(cell + Vector2I.Down)) mask |= TerrainEdgeNeighbours.South;
        if (_controller.Painted.Contains(cell + Vector2I.Left)) mask |= TerrainEdgeNeighbours.West;
        return mask;
    }
    // =========================================================
    // Only three neighbours with the same material and source layer may fill an empty inward corner.
    private List<(Vector2I Direction, TerrainEdgeController.Tile Tile)> Corners(Vector2I cell, TileMapLayer layer)
    {
        List<(Vector2I, TerrainEdgeController.Tile)> found = new();
        foreach (Vector2I d in new[] { new Vector2I(-1, -1), new(1, -1), new(1, 1), new(-1, 1) })
            if (_controller.Tiles.TryGetValue(cell + new Vector2I(d.X, 0), out var a)
                && _controller.Tiles.TryGetValue(cell + new Vector2I(0, d.Y), out var b)
                && _controller.Tiles.TryGetValue(cell + d, out var c)
                && a.Layer == layer && b.Layer == layer && c.Layer == layer && a.Profile.RenderEdges && a.Profile.Shape == TerrainEdgeShape.Soft
                && a.Profile.MaterialId == b.Profile.MaterialId && a.Profile.MaterialId == c.Profile.MaterialId)
                found.Add((d, a));
        return found;
    }
    #endregion

    #region Teardown
    // =========================================================
    // Disabling restores original material references exactly, never repainting or clearing saved cells.
    private void Clear()
    {
        foreach (Surface surface in _surfaces)
        {
            if (GodotObject.IsInstanceValid(surface.Layer)) { surface.Layer.Material = surface.Original; surface.Layer.UseParentMaterial = surface.ParentMaterial; }
            if (GodotObject.IsInstanceValid(surface.Fill)) { surface.Fill.GetParent()?.RemoveChild(surface.Fill); surface.Fill.Free(); }
            surface.Mask.Dispose(); surface.Texture.Dispose(); surface.EnabledCells.Dispose();
        }
        _surfaces.Clear(); MaskBytes = 0;
    }
    // =========================================================
    // World teardown releases cached GPU resources and terrain subscriptions.
    public void Unbind()
    { if (GodotObject.IsInstanceValid(_controller)) _controller.MaterialsRebuilt -= Rebuild; Clear(); _controller = null; }
    // =========================================================
    // Explicit cleanup is independent of node exit ordering.
    public override void _ExitTree() => Unbind();
    #endregion
}
