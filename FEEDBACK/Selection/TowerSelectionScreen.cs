// Non-interactive HUD wash, faint grid and selected-tower caption behind existing HUD panels.
using Godot;

public partial class TowerSelectionScreen : Control
{
    private TowerSelectionStyle _style;
    private Control _bottomArea;
    private string _name = "";
    private bool _disabled;
    private Color _colour;

    // =========================================================
    // Called once by the shared world selection presenter; existing HUD controls stay on top.
    public void Configure(TowerSelectionStyle style, Control bottomArea)
    {
        _style = style; _bottomArea = bottomArea;
        MouseFilter = MouseFilterEnum.Ignore;
        AnchorLeft = AnchorTop = 0f; AnchorRight = AnchorBottom = 1f;
        OffsetLeft = OffsetTop = OffsetRight = OffsetBottom = 0f;
        if (_bottomArea != null) _bottomArea.Resized += QueueRedraw;
        Resized += QueueRedraw;
        Hide(); SetProcess(false);
    }

    // =========================================================
    // No input or per-frame processing. State changes only redraw this one Control.
    public void Present(string name, bool disabled, Color colour)
    {
        _name = (name ?? "").ToUpperInvariant(); _disabled = disabled; _colour = colour;
        Visible = true; QueueRedraw();
    }

    public void Clear() => Hide();

    // =========================================================
    // Extremely low opacity tint/grid for a tactical display without obscuring gameplay.
    public override void _Draw()
    {
        if (_style == null || _name.Length == 0) return;
        float width = Size.X, height = Size.Y;
        if (width <= 0 || height <= 0) return;

        if (_style.ScreenTintEnabled)
            DrawRect(new Rect2(Vector2.Zero, Size), WithAlpha(_colour, _style.TintOpacity));
        if (_style.FaintGridEnabled && _style.GridSpacingPixels > 0)
        {
            Color grid = WithAlpha(_colour, _style.GridOpacity);
            int spacing = Mathf.Max(32, _style.GridSpacingPixels);
            for (int x = spacing; x < width; x += spacing)
                DrawLine(new Vector2(x, 0), new Vector2(x, height), grid);
            for (int y = spacing; y < height; y += spacing)
                DrawLine(new Vector2(0, y), new Vector2(width, y), grid);
        }

        Font font = ThemeDB.FallbackFont;
        if (font == null) return;
        float barTop = _bottomArea != null && _bottomArea.Visible ? _bottomArea.Position.Y : height;
        float nameY = Mathf.Clamp(barTop - _style.NameAboveBottomPixels, 72f, height - 45f);
        Color headline = WithAlpha(_colour, 0.82f);
        Color primary = new(0.88f, 0.97f, 0.98f, 0.96f);
        Color shadow = new(0.01f, 0.04f, 0.08f, 0.7f);
        string heading = _disabled ? "TOWER DISABLED" : "TOWER SELECTED";
        DrawString(font, new Vector2(0, nameY - 13f), heading,
            HorizontalAlignment.Center, width, 11, headline);
        int fontSize = Mathf.Clamp(_style.NameFontSize, 10, 28);
        DrawString(font, new Vector2(1, nameY + 7f), _name,
            HorizontalAlignment.Center, width, fontSize, shadow);
        DrawString(font, new Vector2(0, nameY + 6f), _name,
            HorizontalAlignment.Center, width, fontSize, primary);
        float center = width * 0.5f;
        DrawLine(new Vector2(center - 68f, nameY + 18f), new Vector2(center + 68f, nameY + 18f),
            WithAlpha(_colour, 0.35f), 1f);
    }

    private static Color WithAlpha(Color color, float alpha) => new(color.R, color.G, color.B, Mathf.Clamp(alpha, 0f, 1f));
}
