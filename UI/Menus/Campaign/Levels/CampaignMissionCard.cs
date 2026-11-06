// Lightweight reusable campaign mission card: display real catalog metadata and launch its real mission.
using Godot;
using System;

public static class CampaignMissionCard
{
    // =========================================================
    // A single Button owns input/focus; its noninteractive children provide the sci-fi card layout.
    public static Button Create(MissionDefinition mission, UIMasterConfig palette, Action launch)
    {
        Button card = UIButtonFactory.Create("", launch, UIButtonStyle.Menu, 168f, palette);
        card.Name = $"MissionLevel{mission.Number:000}";
        card.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        card.FocusMode = Control.FocusModeEnum.All;
        card.TooltipText = mission.Description;
        card.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        card.AddThemeStyleboxOverride("normal", Surface(new Color("#142b3b"), new Color("#527f92")));
        card.AddThemeStyleboxOverride("hover", Surface(new Color("#213f52"), palette.AccentColor));
        card.AddThemeStyleboxOverride("pressed", Surface(new Color("#244e61"), palette.AccentColor));
        card.AddThemeStyleboxOverride("focus", Outline(palette.AccentColor));

        MarginContainer pad = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        card.AddChild(pad);
        pad.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        foreach (string edge in new[] { "left", "right", "top", "bottom" })
            pad.AddThemeConstantOverride("margin_" + edge, 11);

        VBoxContainer content = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        content.AddThemeConstantOverride("separation", 7);
        pad.AddChild(content);

        Label number = Text($"LEVEL {mission.Number}", 23, palette.TextColor);
        content.AddChild(number);
        PanelContainer sectorStrip = new() { MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(0, 30) };
        StyleBoxFlat sectorFill = Surface(new Color("#1b394c"), new Color("#315366"));
        sectorFill.ContentMarginLeft = 8;
        sectorFill.ContentMarginTop = sectorFill.ContentMarginBottom = 5;
        sectorStrip.AddThemeStyleboxOverride("panel", sectorFill);
        content.AddChild(sectorStrip);
        Label sector = Text($"SECTOR  {mission.Number:000}  //  OPERATIONS", 10, palette.MutedTextColor);
        sectorStrip.AddChild(sector);
        Label title = Text(mission.Title, 14, palette.TextColor);
        title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        title.CustomMinimumSize = new Vector2(0, 38);
        content.AddChild(title);

        // A gentle visual separator distinguishes mission identity from its launch state.
        content.AddChild(new Control { MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill });
        content.AddChild(new HSeparator { MouseFilter = Control.MouseFilterEnum.Ignore });
        Label action = Text("▷  READY TO DEPLOY", 12, palette.AccentColor);
        content.AddChild(action);
        return card;
    }

    private static Label Text(string content, int size, Color color)
    {
        Label label = new() { Text = content, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static StyleBoxFlat Surface(Color fill, Color border)
    {
        StyleBoxFlat box = UIButtonFactory.Box(fill, border);
        box.ContentMarginLeft = box.ContentMarginRight = 0;
        box.ContentMarginTop = box.ContentMarginBottom = 0;
        return box;
    }

    private static StyleBoxFlat Outline(Color border)
    {
        StyleBoxFlat box = Surface(Colors.Transparent, border);
        box.DrawCenter = false;
        return box;
    }
}
