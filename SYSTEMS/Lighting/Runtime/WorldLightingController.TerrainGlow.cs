// Retained tile glow chunks for buildable foundations and every playable lava cell.
using Godot;
using System.Collections.Generic;

public partial class WorldLightingController
{
    private const int GlowChunkCells = 16;
    private Node2D _terrainGlow;
    private readonly record struct TileGlow(Vector2 Position, float Radius, Color Tint);

    // =========================================================
    // Draw static emissive accents in chunks. Every lava cell glows even if its regional lamp has no pool slot.
    private void RebuildTerrainGlow()
    {
        if (_terrainGlow != null)
        {
            RemoveChild(_terrainGlow);
            _terrainGlow.QueueFree();
        }
        _terrainGlow = new Node2D { Name = "TerrainGlow", ZIndex = 2, Visible = _lightingEnabled };
        AddChild(_terrainGlow);

        Dictionary<Vector2I, List<TileGlow>> chunks = new();
        float size = Grid.Config.CellSize;
        foreach (Vector2I cell in Grid.EnumerateBuildableTerrain())
            AddTileGlow(chunks, cell, size * .60f, new Color(.29f, .82f, 1f, .11f));
        if (HasOutput(Config.Lava) && Grid.InteractiveTiles is { } terrain)
        {
            Color lavaTint = Config.Lava.Tint;
            lavaTint.A = .28f;
            foreach (Vector2I cell in terrain.EnumerateLavaCells())
                AddTileGlow(chunks, cell, size * .85f, lavaTint);
        }

        CanvasItemMaterial material = new()
        {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
            LightMode = CanvasItemMaterial.LightModeEnum.Unshaded
        };
        foreach (List<TileGlow> glows in chunks.Values)
        {
            Node2D chunk = new() { Material = material, TextureFilter = CanvasItem.TextureFilterEnum.Linear };
            chunk.Draw += () => DrawTileGlow(chunk, glows);
            _terrainGlow.AddChild(chunk);
        }
    }

    private void AddTileGlow(Dictionary<Vector2I, List<TileGlow>> chunks, Vector2I cell, float radius, Color tint)
    {
        Vector2I key = new(cell.X / GlowChunkCells, cell.Y / GlowChunkCells);
        if (!chunks.TryGetValue(key, out var glows)) chunks.Add(key, glows = new());
        glows.Add(new TileGlow(_terrainGlow.ToLocal(Grid.CellToWorldCenter(cell)), radius, tint));
    }

    private void DrawTileGlow(Node2D chunk, List<TileGlow> glows)
    {
        foreach (TileGlow glow in glows)
        {
            Vector2 extent = Vector2.One * glow.Radius;
            chunk.DrawTextureRect(Falloff, new Rect2(glow.Position - extent, 2 * extent), false, glow.Tint);
        }
    }
}
