// Full-screen, non-interactive tactical tint and grid shown only while a tower is selected.
using Godot;

public partial class TowerSelectionScreen : Control
{
    private TowerSelectionStyle _style;
    private Color _colour;

    // =========================================================
    // Stay behind existing HUD controls; selection labels are drawn in world space instead.
    public void Configure(TowerSelectionStyle style)
    {
        _style = style;
        MouseFilter = MouseFilterEnum.Ignore;
        AnchorLeft = AnchorTop = 0f; AnchorRight = AnchorBottom = 1f;
        OffsetLeft = OffsetTop = OffsetRight = OffsetBottom = 0f;
        Resized += QueueRedraw;
        Hide(); SetProcess(false);
    }

    // =========================================================
    // State changes redraw only on selection/colour changes and viewport resize.
    public void Present(Color colour)
    {
        _colour = colour;
        Show(); QueueRedraw();
    }

    public void Clear() => Hide();

    // =========================================================
    // Stronger yet translucent aqua/red tint; faint screen grid never consumes mouse input.
    public override void _Draw()
    {
        if (_style == null || Size.X <= 0f || Size.Y <= 0f) return;

        if (_style.ScreenTintEnabled)
            DrawRect(new Rect2(Vector2.Zero, Size), WithAlpha(_colour, _style.TintOpacity));
        if (!_style.FaintGridEnabled || _style.GridSpacingPixels <= 0) return;

        Color grid = WithAlpha(_colour, _style.GridOpacity);
        int spacing = Mathf.Max(32, _style.GridSpacingPixels);
        for (int x = spacing; x < Size.X; x += spacing)
            DrawLine(new Vector2(x, 0), new Vector2(x, Size.Y), grid);
        for (int y = spacing; y < Size.Y; y += spacing)
            DrawLine(new Vector2(0, y), new Vector2(Size.X, y), grid);
    }

    private static Color WithAlpha(Color color, float alpha) =>
        new(color.R, color.G, color.B, Mathf.Clamp(alpha, 0f, 1f));
}
