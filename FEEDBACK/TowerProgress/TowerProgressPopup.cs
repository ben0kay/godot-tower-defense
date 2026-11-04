// Shared, short-lived progression popup. No processing or draw calls while idle.
using Godot;

public partial class TowerProgressPopup : Node2D
{
    private static readonly Color Aqua = new("#4beadf");
    private static readonly Color Gold = new("#ffd36b");
    private const float Duration = 1.35f;
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
        // Thick, filled upward arrow followed by a compact readable label.
        Vector2[] arrow =
        {
            new(0, 0), new(9, -10), new(18, 0),
            new(13, 0), new(13, 12), new(5, 12), new(5, 0)
        };
        Vector2 offset = new(20f, -rise);
        DrawColoredPolygon(System.Array.ConvertAll(arrow, p => p + offset + new Vector2(1, 1)), shadow);
        DrawColoredPolygon(System.Array.ConvertAll(arrow, p => p + offset), color);
        Font font = ThemeDB.FallbackFont;
        if (font == null) return;
        Vector2 textPosition = offset + new Vector2(23f, 7f);
        DrawString(font, textPosition + new Vector2(1, 1), _message, HorizontalAlignment.Left, -1, 13, shadow);
        DrawString(font, textPosition, _message, HorizontalAlignment.Left, -1, 13, color);
    }
}
