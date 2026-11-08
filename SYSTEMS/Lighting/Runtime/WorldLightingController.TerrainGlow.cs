// Retained tile glow chunks for the cyan passable foundation tile and every playable lava cell.
using Godot;
using System.Collections.Generic;

public partial class WorldLightingController
{
    private const int GlowChunkCells = 16;
    private Node2D _terrainGlow;
    private readonly record struct TileGlow(Vector2 Position, float Radius, Color Tint);

    // =========================================================
    // Draw static emissive accents in chunks. Give every lava cell the same base glow without regional light hot spots.
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
        foreach (Vector2I cell in EnumerateCyanFoundationCells())
            AddTileGlow(chunks, cell, size * .50f, new Color(.22f, .80f, 1f, .44f));
        if (HasOutput(Config.Lava) && Grid.InteractiveTiles is { } terrain)
        {
            Color lavaTint = Config.Lava.Tint;
            lavaTint.A = Mathf.Clamp(Config.LavaGlowOpacity, 0f, 1f);
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

    // =========================================================
    // Only the authored cyan foundation emits. Buildability alone also matches solid walls.
    private IEnumerable<Vector2I> EnumerateCyanFoundationCells()
    {
        TileMapLayer passable = Grid.Level.Passable;
        Vector2I atlas = new(3, 0);
        foreach (Vector2I painted in passable.GetUsedCells())
        {
            if (passable.GetCellSourceId(painted) != 1 || passable.GetCellAtlasCoords(painted) != atlas ||
                passable.GetCellAlternativeTile(painted) != 0) continue;
            Vector2I cell = Grid.WorldToCell(passable.ToGlobal(passable.MapToLocal(painted)));
            if (!Grid.TerrainBlocksEnemies(cell) && Grid.InteractiveTiles?.IsLava(Grid.CellToWorldCenter(cell)) != true)
                yield return cell;
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
