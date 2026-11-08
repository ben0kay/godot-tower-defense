// Bakes one maximum-coverage halo around the lava silhouette at terrain rebuild time.
using Godot;
using System.Collections.Generic;

public partial class WorldLightingController
{
    private const int LavaSamplesPerCell = 4;
    private const int LavaChunkSamples = GlowChunkCells * LavaSamplesPerCell;

    // =========================================================
    // Nonoverlapping draw regions share padded samples so filtering crosses chunk boundaries.
    private void BuildLavaHalo(CanvasItemMaterial material, float cellSize)
    {
        LightSourceConfig profile = Config.LavaSpill;
        if (!HasOutput(profile) || Config.LavaGlowOpacity <= 0 || Grid.InteractiveTiles is not { } terrain
            || terrain.LavaCellCount == 0) return;

        HashSet<Vector2I> lava = new(terrain.EnumerateLavaCells());
        float radius = Mathf.Clamp(profile.RadiusPixels - cellSize * .5f, cellSize * .25f, 512f);
        int reach = Mathf.CeilToInt(radius / cellSize);
        HashSet<Vector2I> chunks = new();
        foreach (Vector2I cell in lava)
        {
            Vector2I first = LavaChunkFor(cell - Vector2I.One * reach);
            Vector2I last = LavaChunkFor(cell + Vector2I.One * reach);
            for (int y = first.Y; y <= last.Y; y++)
                for (int x = first.X; x <= last.X; x++) chunks.Add(new Vector2I(x, y));
        }

        Color tint = profile.Tint;
        tint.A = Mathf.Clamp(Config.LavaGlowOpacity * profile.Energy, 0, 1);
        float size = GlowChunkCells * cellSize;
        float sampleStep = cellSize / LavaSamplesPerCell;
        foreach (Vector2I key in chunks)
        {
            Vector2I firstCell = key * GlowChunkCells;
            ImageTexture texture = BakeLavaHalo(lava, firstCell, radius, reach, cellSize, sampleStep);
            Vector2 corner = Grid.CellToWorldCenter(firstCell) - Vector2.One * (cellSize * .5f);
            Rect2 area = new(_terrainGlow.ToLocal(corner), Vector2.One * size);
            Rect2 samples = new(1, 1, LavaChunkSamples, LavaChunkSamples);
            var chunk = new Node2D { Material = material, TextureFilter = CanvasItem.TextureFilterEnum.Linear };
            chunk.Draw += () => chunk.DrawTextureRectRegion(texture, area, samples, tint, false, false);
            _terrainGlow.AddChild(chunk);
        }
    }

    private static Vector2I LavaChunkFor(Vector2I cell) =>
        new(Mathf.FloorToInt((float)cell.X / GlowChunkCells), Mathf.FloorToInt((float)cell.Y / GlowChunkCells));

    // =========================================================
    // Distance to the union of cell rectangles gives one strength inside the lava and a soft exterior falloff.
    private static ImageTexture BakeLavaHalo(HashSet<Vector2I> lava, Vector2I firstCell, float radius,
        int reach, float cellSize, float step)
    {
        int width = LavaChunkSamples + 2;
        float radiusSquared = radius * radius;
        var pixels = new byte[width * width * 4];
        for (int y = 0; y < width; y++)
        for (int x = 0; x < width; x++)
        {
            Vector2 sample = new((x - .5f) * step, (y - .5f) * step);
            Vector2I cell = new(Mathf.FloorToInt(sample.X / cellSize), Mathf.FloorToInt(sample.Y / cellSize));
            float nearest = 0;
            if (!lava.Contains(firstCell + cell))
            {
                nearest = radiusSquared;
                Vector2 local = sample - (Vector2)cell * cellSize;
                for (int dy = -reach; dy <= reach; dy++)
                for (int dx = -reach; dx <= reach; dx++)
                {
                    float left = dx * cellSize, top = dy * cellSize;
                    float sx = local.X - Mathf.Clamp(local.X, left, left + cellSize);
                    float sy = local.Y - Mathf.Clamp(local.Y, top, top + cellSize);
                    float distance = sx * sx + sy * sy;
                    if (distance < nearest && lava.Contains(firstCell + cell + new Vector2I(dx, dy)))
                        nearest = distance;
                }
            }

            float coverage = 1f - Mathf.SmoothStep(0, radius, Mathf.Sqrt(nearest));
            // Bias the spill toward the immediate shoreline while retaining a soft outer tail.
            int index = (y * width + x) * 4;
            pixels[index] = pixels[index + 1] = pixels[index + 2] = 255;
            pixels[index + 3] = (byte)Mathf.RoundToInt(255 * coverage * coverage);
        }
        using Image image = Image.CreateFromData(width, width, false, Image.Format.Rgba8, pixels);
        return ImageTexture.CreateFromImage(image);
    }
}
