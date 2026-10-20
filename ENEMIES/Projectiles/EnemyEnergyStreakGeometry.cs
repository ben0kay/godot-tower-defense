// Retained tapered tracer geometry: narrow fading tail and rounded wider nose aligned with actual projectile travel.
using Godot;

public static class EnemyEnergyStreakGeometry
{
    public const int VertexCount = 9;
    #region Reusable Polygon
    // =========================================================
    // Caller owns both buffers; the nose ends exactly at the real bullet position, with no per-draw arrays.
    public static void Fill(Vector2 head, Vector2 forward, ProjectileVisualSnapshot style, float lengthScale,
        float widthScale, Color color, Vector2[] vertices, Color[] colors)
    {
        float length = style.Length * lengthScale;
        float half = Mathf.Min(style.Width * style.HeadWidthMultiplier * .5f * widthScale, length * .3f);
        Vector2 side = forward.Orthogonal();
        Put(0, -length, half * style.TailWidthFraction, style.TailOpacity, head, forward, side, color, vertices, colors);
        Put(1, -length * .35f, half * .8f, .7f, head, forward, side, color, vertices, colors);
        Put(2, -half * .8f, half, 1, head, forward, side, color, vertices, colors);
        Put(3, -half * .2f, half * .6f, 1, head, forward, side, color, vertices, colors);
        Put(4, 0, 0, 1, head, forward, side, color, vertices, colors);
        Put(5, -half * .2f, -half * .6f, 1, head, forward, side, color, vertices, colors);
        Put(6, -half * .8f, -half, 1, head, forward, side, color, vertices, colors);
        Put(7, -length * .35f, -half * .8f, .7f, head, forward, side, color, vertices, colors);
        Put(8, -length, -half * style.TailWidthFraction, style.TailOpacity, head, forward, side, color, vertices, colors);
    }
    // =========================================================
    // Vertex colors interpolate the tail fade without textures, materials or extra projectile objects.
    private static void Put(int index, float x, float y, float alpha, Vector2 head, Vector2 forward, Vector2 side,
        Color color, Vector2[] vertices, Color[] colors)
    {
        vertices[index] = head + forward * x + side * y;
        color.A *= alpha; colors[index] = color;
    }
    #endregion
}
