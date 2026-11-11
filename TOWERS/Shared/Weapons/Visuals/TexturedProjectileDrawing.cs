// Bake layered tracer styles into shared textures once, then draw one quad per tracer.
using Godot;
using System.Collections.Generic;

internal static class TexturedProjectileDrawing
{
    private const int CacheCapacity = 64;
    private const float PixelsPerUnit = 2f;
    private sealed record TracerTexture(Texture2D Texture, Rect2 Bounds);
    private static readonly Dictionary<ProjectileVisualSnapshot, TracerTexture> Cache = new();

    internal static int CachedStyleCount => Cache.Count;

    // =========================================================
    // Authored textures and disabled styles continue through their original drawing path.
    internal static bool TryDraw(CanvasItem canvas, ProjectileVisualSnapshot style,
        Vector2 head, Vector2 forward, Transform2D drawTransform)
    {
        if (!style.Enabled || GodotObject.IsInstanceValid(style.Texture)) return false;
        if (!Cache.TryGetValue(style, out TracerTexture tracer))
        {
            if (Cache.Count >= CacheCapacity) return false;
            tracer = Bake(style);
            Cache.Add(style, tracer);
        }
        if (tracer == null) return false;
        canvas.DrawSetTransformMatrix(drawTransform * new Transform2D(forward.Angle(), head));
        canvas.DrawTextureRect(tracer.Texture, tracer.Bounds, false, Colors.White);
        canvas.DrawSetTransformMatrix(drawTransform);
        return true;
    }

    // =========================================================
    // All allocation and rasterization is per immutable style, never per flying round or frame.
    private static TracerTexture Bake(ProjectileVisualSnapshot style)
    {
        Rect2 bounds = ProjectileVisualBounds.ForTracer(Vector2.Zero, Vector2.Right, style).Grow(1f);
        int width = Mathf.CeilToInt(bounds.Size.X * PixelsPerUnit);
        int height = Mathf.CeilToInt(bounds.Size.Y * PixelsPerUnit);
        bounds.Size = new Vector2(width, height) / PixelsPerUnit;
        var pixels = new Color[width * height];
        var layer = new Color[pixels.Length];
        if (style.Tapered)
        {
            if (style.Glow.A > 0 && !Polygon(style, 1f, style.GlowWidth, style.Glow, bounds, width, height, pixels, layer)) return null;
            if (!Polygon(style, 1f, 1f, style.Body, bounds, width, height, pixels, layer)) return null;
            if (!Polygon(style, .85f, .55f, style.Core, bounds, width, height, pixels, layer)) return null;
        }
        else
        {
            Line(style.Length, style.Width * style.GlowWidth, style.Glow, bounds, width, height, pixels);
            Line(style.Length, style.Width * 1.6f, style.Body, bounds, width, height, pixels);
            Line(style.Length * .75f, style.Width, style.Core, bounds, width, height, pixels);
        }
        if (style.BrightTip)
            Line(style.Width * 1.5f, style.Width * 1.2f, style.Core, bounds, width, height, pixels);

        using Image image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++) image.SetPixel(x, y, pixels[y * width + x]);
        return new TracerTexture(ImageTexture.CreateFromImage(image), bounds);
    }

    // =========================================================
    // Subpixel coverage retains thin lines. Straight layers have their original length and width.
    private static void Line(float length, float thickness, Color color, Rect2 bounds,
        int width, int height, Color[] pixels)
    {
        if (color.A <= 0f) return;
        float half = thickness * .5f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 point = bounds.Position + new Vector2(x + .5f, y + .5f) / PixelsPerUnit;
                float distance = Mathf.Max(Mathf.Abs(point.Y) - half, Mathf.Max(-length - point.X, point.X));
                float coverage = Mathf.Clamp(.5f - distance * PixelsPerUnit, 0f, 1f);
                if (coverage <= 0f) continue;
                Color source = color;
                source.A *= coverage;
                Blend(pixels, y * width + x, source);
            }
        }
    }

    // =========================================================
    // Reuse the authored tapered vertices and interpolate their colors through the baked triangles.
    private static bool Polygon(ProjectileVisualSnapshot style, float lengthScale, float widthScale,
        Color color, Rect2 bounds, int width, int height, Color[] pixels, Color[] layer)
    {
        var vertices = new Vector2[EnemyEnergyStreakGeometry.VertexCount];
        var colors = new Color[vertices.Length];
        EnemyEnergyStreakGeometry.Fill(Vector2.Zero, Vector2.Right, style, lengthScale, widthScale, color, vertices, colors);
        int[] triangles = Geometry2D.TriangulatePolygon(vertices);
        if (triangles.Length == 0) return false;
        System.Array.Clear(layer, 0, layer.Length);
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int ia = triangles[i], ib = triangles[i + 1], ic = triangles[i + 2];
            Vector2 a = vertices[ia], b = vertices[ib], c = vertices[ic];
            float area = (b - a).Cross(c - a);
            if (Mathf.IsZeroApprox(area)) continue;
            Vector2 min = (a.Min(b).Min(c) - bounds.Position) * PixelsPerUnit;
            Vector2 max = (a.Max(b).Max(c) - bounds.Position) * PixelsPerUnit;
            int left = Mathf.Max(0, Mathf.FloorToInt(min.X)), right = Mathf.Min(width - 1, Mathf.CeilToInt(max.X));
            int top = Mathf.Max(0, Mathf.FloorToInt(min.Y)), bottom = Mathf.Min(height - 1, Mathf.CeilToInt(max.Y));
            for (int y = top; y <= bottom; y++)
            {
                for (int x = left; x <= right; x++)
                {
                    Vector2 point = bounds.Position + new Vector2(x + .5f, y + .5f) / PixelsPerUnit;
                    float u = (b - point).Cross(c - point) / area;
                    float v = (c - point).Cross(a - point) / area;
                    float w = 1f - u - v;
                    if (u < 0f || v < 0f || w < 0f) continue;
                    layer[y * width + x] = colors[ia] * u + colors[ib] * v + colors[ic] * w;
                }
            }
        }
        // Composite each layer once so shared triangle edges cannot double its opacity.
        for (int i = 0; i < pixels.Length; i++)
            if (layer[i].A > 0f) Blend(pixels, i, layer[i]);
        return true;
    }

    private static void Blend(Color[] pixels, int index, Color source)
    {
        Color destination = pixels[index];
        float alpha = source.A + destination.A * (1f - source.A);
        if (alpha <= 0f) return;
        Color mixed = (source * source.A + destination * (destination.A * (1f - source.A))) / alpha;
        mixed.A = alpha;
        pixels[index] = mixed;
    }
}
