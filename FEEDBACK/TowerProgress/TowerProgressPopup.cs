// Shared, short-lived progression popup. No processing or draw calls while idle.
using Godot;

public partial class TowerProgressPopup : Node2D
{
    private static readonly Color Aqua = new("#4beadf");
    private static readonly Color Gold = new("#ffd36b");
    private const float Duration = 1.35f;
    // =========================================================
    // Reuse immutable polygon points on every visible frame; no redraw-time array allocations.
    private static readonly Vector2[] Arrow =
    {
        new(0f, 0f), new(9f, -10f), new(18f, 0f),
        new(13f, 0f), new(13f, 12f), new(5f, 12f), new(5f, 0f)
    };
    private float _remaining;
    private string _message = "";
    private Color _color;

    // =========================================================
    // Reset the existing popup rather than allocating a label/tween for each promotion.
    public void ShowUpgrade(int level) => Show($"LEVEL {level}", Aqua);
    public void ShowRank(string rank) => Show($"RANK UP  {rank}", Gold);

    // =========================================================
    // Start at the tower's right side; float upward, then fade away.
    private void Show(string message, Color color)
    {
        _message = message; _color = color; _remaining = Duration;
        Visible = true; SetProcess(true); QueueRedraw();
    }

    public override void _Ready()
    {
        Visible = false; SetProcess(false);
        ZIndex = 50;
    }

    public override void _Process(double delta)
    {
        _remaining -= (float)delta;
        if (_remaining <= 0f) { _remaining = 0f; Visible = false; SetProcess(false); }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_remaining <= 0f) return;
        float progress = 1f - _remaining / Duration;
        float alpha = Mathf.Clamp(_remaining / .4f, 0f, 1f);
        float rise = 20f * progress;
        Color color = new(_color.R, _color.G, _color.B, alpha);
        Color shadow = new(0f, .05f, .08f, alpha * .85f);
        // Thick filled arrow and text share the same float offset, without allocating geometry.
        DrawSetTransform(new Vector2(21f, 1f - rise));
        DrawColoredPolygon(Arrow, shadow);
        Font font = ThemeDB.FallbackFont;
        if (font != null)
            DrawString(font, new Vector2(23f, 7f), _message, HorizontalAlignment.Left, -1, 13, shadow);

        DrawSetTransform(new Vector2(20f, -rise));
        DrawColoredPolygon(Arrow, color);
        if (font != null)
            DrawString(font, new Vector2(23f, 7f), _message, HorizontalAlignment.Left, -1, 13, color);
    }
}
