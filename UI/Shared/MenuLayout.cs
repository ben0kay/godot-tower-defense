// Reusable responsive backdrop and center panel used by Boot/Main/Campaign screens.
using Godot;

public static class MenuLayout
{
    // =========================================================
    // One standard panel layout; screen controllers own their contents and button callbacks.
    public static VBoxContainer Create(Control root, string heading, string subtitle, float panelWidth = 560f)
    {
        UIMasterConfig config = GD.Load<UIMasterConfig>("res://UI/DefaultUIConfig.tres") ?? new UIMasterConfig();
        ColorRect backdrop = new() { Color = new Color("#08141f"), MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(backdrop);
        backdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        MarginContainer margins = new() { MouseFilter = Control.MouseFilterEnum.Pass };
        foreach (string edge in new[] { "left", "right", "top", "bottom" })
            margins.AddThemeConstantOverride("margin_" + edge, 24);
        root.AddChild(margins);
        margins.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        CenterContainer center = new() { MouseFilter = Control.MouseFilterEnum.Pass };
        margins.AddChild(center);
        PanelContainer panel = new() { CustomMinimumSize = new Vector2(panelWidth, 0f) };
        panel.AddThemeStyleboxOverride("panel", UIButtonFactory.Box(config.PanelColor, config.AccentColor));
        center.AddChild(panel);
        VBoxContainer stack = new();
        stack.AddThemeConstantOverride("separation", 12);
        panel.AddChild(stack);

        Label title = UIButtonFactory.Label(heading, 30, config);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        stack.AddChild(title);
        Label caption = UIButtonFactory.Label(subtitle, 14, config);
        caption.HorizontalAlignment = HorizontalAlignment.Center;
        caption.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        caption.AddThemeColorOverride("font_color", config.MutedTextColor);
        stack.AddChild(caption);
        stack.AddChild(new HSeparator());
        return stack;
    }
}
