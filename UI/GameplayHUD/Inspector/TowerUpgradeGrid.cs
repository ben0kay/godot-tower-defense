// Content-sized comparison columns with thin vector dividers; no separator nodes or per-frame work.
using Godot;

public partial class TowerUpgradeGrid : GridContainer
{
    [Export] public Color DividerColor { get; set; } = new(.4f, .7f, .8f, .28f);

    // =========================================================
    // Container sorting already responds to content changes and resizing; refresh cached drawing only then.
    public override void _Notification(int what)
    { if (what == NotificationSortChildren || what == NotificationResized) QueueRedraw(); }

    // =========================================================
    // Actual sorted cell boundaries keep lines aligned for both upgrade and current-only maximum-tier views.
    public override void _Draw()
    {
        if (GetChildCount() < Columns) return;
        float halfGap = GetThemeConstant("h_separation") * .5f;
        for (int column = 1; column < Columns; column++)
        {
            float x = GetChild<Control>(column).Position.X - halfGap;
            DrawLine(new(x, 0), new(x, Size.Y), DividerColor, 1);
        }
    }
}
