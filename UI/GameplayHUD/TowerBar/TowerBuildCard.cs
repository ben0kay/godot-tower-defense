// Consistent image-and-text card for the existing catalog-driven build tray.
// All thumbnails use the authored level-one TowerDefinition.Icon; no scene instances or runtime captures.
using Godot;
using System;

public static class TowerBuildCard
{
    public const float Height = 62f;

    // =========================================================
    // Child controls ignore mouse input so the original button owns hover, focus and clicks.
    public static Button Create(TowerDefinition definition, Action onPressed, UIMasterConfig config)
    {
        string cost = TowerPresentation.DescribeResourceCosts(definition.DefaultConfig.ConstructionCosts);
        Button button = UIButtonFactory.Create("", onPressed, UIButtonStyle.TowerTile, Height, config);
        button.CustomMinimumSize = new Vector2(config.TowerButtonWidth, Height);
        button.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        button.TooltipText = definition.Description + "\n" + cost;

        HBoxContainer row = new() { Name = "Content", MouseFilter = Control.MouseFilterEnum.Ignore };
        button.AddChild(row);
        row.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        row.OffsetLeft = 7f;
        row.OffsetRight = -7f;
        row.OffsetTop = 5f;
        row.OffsetBottom = -5f;
        row.AddThemeConstantOverride("separation", 7);

        TextureRect icon = new()
        {
            Name = "Icon",
            Texture = definition.Icon,
            CustomMinimumSize = new Vector2(40f, 40f),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        row.AddChild(icon);

        VBoxContainer text = new()
        {
            Name = "Text",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        text.AddThemeConstantOverride("separation", 0);
        row.AddChild(text);

        Label title = new()
        {
            Name = "Title",
            Text = definition.DisplayName,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 32),
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MaxLinesVisible = 2,
            ClipText = true,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        title.AddThemeFontSizeOverride("font_size", 12);
        title.AddThemeColorOverride("font_color", config.TextColor);
        text.AddChild(title);

        Label details = new()
        {
            Name = "Details",
            Text = $"{definition.Category} · {cost}",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ClipText = true,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        details.AddThemeFontSizeOverride("font_size", 10);
        details.AddThemeColorOverride("font_color", config.MutedTextColor);
        text.AddChild(details);
        return button;
    }
}
