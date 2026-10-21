// Static mouse-transparent HUD vector grid for mission results; redraws only on resize or changed outcome.
using Godot;

public partial class ResultsVectorBackdrop : Control
{
    #region Vector Presentation
    public bool IsDefeat { get; private set; }
    private static readonly Color Victory = new("#48bacc"), Defeat = new("#e85b68");
    // =========================================================
    // Switch the visual palette only; do not touch gameplay or start update loops.
    public void SetOutcome(bool defeat) { IsDefeat = defeat; QueueRedraw(); }
    // =========================================================
    // Follow the viewport geometry without creating timers, shaders or physical objects.
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Resized += QueueRedraw;
        SetProcess(false); SetPhysicsProcess(false);
    }
    // =========================================================
    // One static grid, bright corner brackets and restrained side telemetry marks.
    public override void _Draw()
    {
        Vector2 size = Size;
        if (size.X <= 0 || size.Y <= 0) return;
        Color accent = IsDefeat ? Defeat : Victory;
        Color grid = new(accent.R, accent.G, accent.B, .055f);
        Color detail = new(accent.R, accent.G, accent.B, .34f);
        const int step = 64;
        for (float x = step; x < size.X; x += step) DrawLine(new Vector2(x, 0), new Vector2(x, size.Y), grid, 1f);
        for (float y = step; y < size.Y; y += step) DrawLine(new Vector2(0, y), new Vector2(size.X, y), grid, 1f);
        float inset = Mathf.Min(34f, Mathf.Min(size.X, size.Y) * .06f), arm = Mathf.Min(96f, Mathf.Min(size.X, size.Y) * .16f);
        for (int i = 0; i < 4; i++)
        {
            float x = (i & 1) == 0 ? inset : size.X - inset;
            float y = (i & 2) == 0 ? inset : size.Y - inset;
            float dx = (i & 1) == 0 ? 1f : -1f, dy = (i & 2) == 0 ? 1f : -1f;
            DrawLine(new Vector2(x, y), new Vector2(x + dx * arm, y), detail, 2f);
            DrawLine(new Vector2(x, y), new Vector2(x, y + dy * arm), detail, 2f);
            for (int j = 1; j <= 3; j++)
                DrawRect(new Rect2(new Vector2(x + dx * (arm + j * 14f), y - 2f), new Vector2(5f, 3f)), detail, true);
        }
        float centerY = size.Y * .5f, halfWidth = Mathf.Min(180f, size.X * .16f);
        DrawLine(new Vector2(18f, centerY), new Vector2(halfWidth, centerY), detail, 1f);
        DrawLine(new Vector2(size.X - 18f, centerY), new Vector2(size.X - halfWidth, centerY), detail, 1f);
    }
    #endregion
}
