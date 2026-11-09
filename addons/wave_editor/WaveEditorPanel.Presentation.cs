// Shared visual styles for the Wave Editor's sections, metric cards and timeline.
#if TOOLS
using Godot;

public partial class WaveEditorPanel
{
    private static readonly Color Accent = new("#8cd9eb");
    private static readonly Color Muted = new("#aebdcd");

    // =========================================================
    // Use a separate background and a clear heading while preserving the existing split and scroll layout.
    private static VBoxContainer Pane(Container parent, string heading, float minWidth = 0, bool expandHeight = false)
    {
        var card = new PanelContainer
        {
            CustomMinimumSize = new Vector2(minWidth, 0),
            SizeFlagsVertical = expandHeight ? SizeFlags.ExpandFill : SizeFlags.Fill
        };
        card.AddThemeStyleboxOverride("panel", Box(new Color("#232b36"), 9));
        parent.AddChild(card);
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 7);
        card.AddChild(content);
        content.AddChild(StyledLabel(heading.ToUpperInvariant(), Accent));
        return content;
    }

    private static StyleBoxFlat Box(Color color, float padding, int corner = 5)
    {
        var style = new StyleBoxFlat { BgColor = color };
        style.SetCornerRadiusAll(corner);
        style.ContentMarginLeft = style.ContentMarginRight = padding;
        style.ContentMarginTop = style.ContentMarginBottom = padding;
        return style;
    }

    private static Label StyledLabel(string text, Color color)
    {
        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static Label AddMetric(GridContainer grid, string heading)
    {
        var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        card.AddThemeStyleboxOverride("panel", Box(new Color("#19222d"), 7));
        grid.AddChild(card);
        var content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 2);
        card.AddChild(content);
        var caption = StyledLabel(heading, Muted);
        caption.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(caption);
        var value = StyledLabel("—", Colors.White);
        value.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(value);
        value.AddThemeFontSizeOverride("font_size", value.GetThemeDefaultFontSize() + 3);
        return value;
    }
}
#endif
