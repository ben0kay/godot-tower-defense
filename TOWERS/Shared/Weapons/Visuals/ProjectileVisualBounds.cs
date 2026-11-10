// Conservative visual footprints shared by friendly, hostile and standalone projectile rendering.
using Godot;

internal static class ProjectileVisualBounds
{
    // =========================================================
    // Include every layer, including tips longer than a short tracer and wide tapered heads.
    internal static Rect2 ForTracer(Vector2 head, Vector2 direction, ProjectileVisualSnapshot? visual)
    {
        if (!visual.HasValue) return Circle(head, 3f);
        ProjectileVisualSnapshot style = visual.Value;
        float length = Mathf.Max(style.Length, style.BrightTip ? style.Width * 1.5f : 0f);
        float width = Mathf.Max(style.Width * 1.6f, style.Width * style.GlowWidth);
        if (style.Tapered) width = Mathf.Max(width, style.Width * style.HeadWidthMultiplier * style.GlowWidth);
        if (style.BrightTip) width = Mathf.Max(width, style.Width * 1.2f);
        Vector2 tail = head - direction * length;
        Vector2 start = head.Min(tail), end = head.Max(tail);
        return new Rect2(start, end - start).Grow(width * .5f);
    }

    internal static Rect2 Circle(Vector2 centre, float radius) =>
        new(centre - Vector2.One * radius, Vector2.One * (2f * radius));

    // =========================================================
    // Transform the full footprint into viewport pixels, retaining antialiasing at any zoom.
    internal static bool OverlapsViewport(Rect2 bounds, Transform2D toScreen, Rect2 viewport)
    {
        Vector2 centre = toScreen * bounds.GetCenter();
        Vector2 half = bounds.Size * .5f;
        Vector2 extent = toScreen.X.Abs() * half.X + toScreen.Y.Abs() * half.Y;
        return viewport.Grow(2f).Intersects(new Rect2(centre - extent, extent * 2f), true);
    }

    // =========================================================
    // Standalone nodes retain their draw list. Native culling follows camera and node transforms.
    // A conservative local margin covers AA at the camera's supported minimum 0.1x zoom.
    internal static void SetCachedBounds(CanvasItem canvas, Rect2 localBounds, float antialiasMargin = 0f) =>
        RenderingServer.CanvasItemSetCustomRect(canvas.GetCanvasItem(), true, localBounds.Grow(antialiasMargin));
}
