// Reusable catalog-driven campaign card: existing mission launch plus optional faded mission art.
using Godot;
using System;

public static class CampaignMissionCard
{
    // =========================================================
    // One Button handles the usual hover, keyboard focus and click; child artwork is noninteractive.
    public static Button Create(MissionDefinition mission, UIMasterConfig palette, Action launch)
    {
        Button card = UIButtonFactory.Create("", launch, UIButtonStyle.Menu, 168f, palette);
        card.Name = $"MissionLevel{mission.Number:000}";
        card.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        card.FocusMode = Control.FocusModeEnum.All;
        card.TooltipText = mission.Description;
        card.MouseDefaultCursorShape = Control.CursorShape.PointingHand;

        // Strong cyan focus/hover halo is reusable but opt-in; no idle animation or callbacks.
        UIButtonFactory.ApplySelectionGlow(card, new Color("#142b3b"), new Color("#1c3f51"), palette.AccentColor);

        // Mission image is assigned later in the MissionDefinition Inspector. The card works without one.
        // Clipping and low opacity let artwork sit behind the text without changing card height or input.
        if (mission.CardPreview != null)
        {
            Control photoClip = new() { Name = "PreviewClip", MouseFilter = Control.MouseFilterEnum.Ignore,
                ClipContents = true };
            card.AddChild(photoClip);
            photoClip.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
            photoClip.OffsetLeft = 2;
            photoClip.OffsetRight = -2;
            photoClip.OffsetTop = 2;
            photoClip.OffsetBottom = 130;

            TextureRect preview = new()
            {
                Name = "MissionPreview", Texture = mission.CardPreview,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                Modulate = new Color(0.73f, 0.88f, 1f, 0.25f),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            photoClip.AddChild(preview);
            preview.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }

        MarginContainer pad = new() { Name = "Content", MouseFilter = Control.MouseFilterEnum.Ignore };
        card.AddChild(pad);
        pad.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        foreach (string edge in new[] { "left", "right", "top", "bottom" })
            pad.AddThemeConstantOverride("margin_" + edge, 11);

        VBoxContainer content = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        content.AddThemeConstantOverride("separation", 9);
        pad.AddChild(content);

        Label number = Text($"LEVEL {mission.Number}", 23, palette.TextColor);
        content.AddChild(number);
        Label title = Text(mission.Title, 14, palette.TextColor);
        title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        title.CustomMinimumSize = new Vector2(0, 38);
        content.AddChild(title);

        // Preserve the previous direct mission launch, with no fictional completion/lock state.
        content.AddChild(new Control { MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill });
        content.AddChild(new HSeparator { MouseFilter = Control.MouseFilterEnum.Ignore });
        content.AddChild(Text("▷  READY TO DEPLOY", 12, palette.AccentColor));
        return card;
    }

    private static Label Text(string content, int size, Color color)
    {
        Label label = new() { Text = content, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }
}
