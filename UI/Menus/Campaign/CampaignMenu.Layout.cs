// Campaign UI composition only: two-thirds scrollable mission grid, one-third stationary command panel.
using Godot;
using System;

public partial class CampaignMenu
{
    // =========================================================
    // Build everything from real mission and profile data; no fabricated unlock or completion state.
    private void BuildCampaignLayout(out Button resume, out Button firstMission)
    {
        UIMasterConfig style = GD.Load<UIMasterConfig>("res://UI/DefaultUIConfig.tres") ?? new UIMasterConfig();
        ColorRect background = new() { Name = "Background", Color = new Color("#08141f"), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(background);
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        MarginContainer safe = new() { Name = "ScreenMargins", MouseFilter = MouseFilterEnum.Pass };
        AddChild(safe);
        safe.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (string edge in new[] { "left", "right", "top", "bottom" })
            safe.AddThemeConstantOverride("margin_" + edge, ScreenMarginPixels);

        VBoxContainer page = new() { Name = "CampaignLayout", SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill };
        page.AddThemeConstantOverride("separation", 14);
        safe.AddChild(page);

        Label heading = UIButtonFactory.Label("C A M P A I G N", 34, style);
        heading.Name = "CampaignTitle";
        heading.HorizontalAlignment = HorizontalAlignment.Center;
        heading.CustomMinimumSize = new Vector2(0, 52);
        heading.VerticalAlignment = VerticalAlignment.Center;
        page.AddChild(heading);

        HBoxContainer columns = new() { Name = "MainColumns", SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill };
        columns.AddThemeConstantOverride("separation", 14);
        page.AddChild(columns);

        PanelContainer missionPanel = Panel("MissionPanel", style, 2f);
        columns.AddChild(missionPanel);
        VBoxContainer left = Stack(missionPanel, 12);

        HBoxContainer section = new() { Name = "MissionHeader" };
        section.AddThemeConstantOverride("separation", 8);
        left.AddChild(section);
        section.AddChild(SectionLabel("MISSIONS", 23, style));
        Control gap = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        section.AddChild(gap);
        Label total = SectionLabel("ACTIVE OPERATIONS", 12, style);
        total.AddThemeColorOverride("font_color", style.MutedTextColor);
        section.AddChild(total);

        Label guide = SectionLabel("SELECT A MISSION TO DEPLOY", 13, style);
        guide.AddThemeColorOverride("font_color", style.MutedTextColor);
        left.AddChild(guide);
        left.AddChild(new HSeparator());

        ScrollContainer scroll = new() { Name = "MissionScroll", SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto, FollowFocus = true };
        left.AddChild(scroll);
        // Inset the grid so cyan shadow halos are visible rather than clipped at scroll edges.
        MarginContainer missionInsets = new() { Name = "MissionCardInsets",
            SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (string edge in new[] { "left", "right", "top", "bottom" })
            missionInsets.AddThemeConstantOverride("margin_" + edge, 18);
        scroll.AddChild(missionInsets);
        GridContainer grid = new() { Name = "MissionGrid", Columns = 3,
            SizeFlagsHorizontal = SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 18);
        grid.AddThemeConstantOverride("v_separation", 18);
        missionInsets.AddChild(grid);

        firstMission = null;
        try
        {
            MissionDefinition[] missions = PlayableLevelCatalog.Entries;
            total.Text = $"{missions.Length} MISSIONS";
            foreach (MissionDefinition mission in missions)
            {
                MissionDefinition selected = mission;
                Button card = CampaignMissionCard.Create(selected, style, () => Navigate(selected.LaunchPath));
                grid.AddChild(card);
                firstMission ??= card;
            }
            if (missions.Length == 0) grid.AddChild(SectionLabel("No missions are currently available.", 15, style));
        }
        catch (Exception error)
        {
            total.Text = "CATALOG UNAVAILABLE";
            grid.AddChild(SectionLabel(error.Message, 14, style));
        }

        PanelContainer commandPanel = Panel("CommandPanel", style, 1f);
        columns.AddChild(commandPanel);
        VBoxContainer right = Stack(commandPanel, 9);
        right.AddChild(SectionLabel("COMMAND", 22, style));
        right.AddChild(new HSeparator());

        resume = UIButtonFactory.Create("CONTINUE", ContinueCheckpoint, UIButtonStyle.Menu, 72f, style);
        resume.Name = "ContinueButton";
        resume.AddThemeFontSizeOverride("font_size", 22);
        right.AddChild(resume);
        Label resumeHint = SectionLabel("RESUME THE LAST SAVED BATTLEFIELD", 12, style);
        resumeHint.AddThemeColorOverride("font_color", style.MutedTextColor);
        resumeHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        right.AddChild(resumeHint);

        try
        {
            var saved = CampaignSession.ReadCurrent();
            string unavailable = CampaignSession.LoadUnavailableReason(saved);
            resume.Disabled = unavailable.Length > 0;
            resume.TooltipText = resume.Disabled ? unavailable :
                "Resume " + saved.Checkpoint.LevelId + ". " + CampaignSession.RecoveryMessage;
            resumeHint.Text = resume.Disabled ? "NO RESUMABLE CHECKPOINT" :
                "RESUME " + saved.Checkpoint.LevelId.ToUpperInvariant();
        }
        catch (Exception error)
        {
            resume.Disabled = true;
            resume.TooltipText = error.Message;
            resumeHint.Text = "NO RESUMABLE CHECKPOINT";
        }

        Button research = UIButtonFactory.Create("RESEARCH", () => Navigate(MenuNavigation.ResearchScene),
            UIButtonStyle.Menu, 72f, style);
        research.Name = "ResearchButton";
        research.AddThemeFontSizeOverride("font_size", 22);
        right.AddChild(research);
        Label researchHint = SectionLabel("SPEND RESEARCH POINTS ON UPGRADES", 12, style);
        researchHint.AddThemeColorOverride("font_color", style.MutedTextColor);
        researchHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        right.AddChild(researchHint);
        right.AddChild(new HSeparator());

        // Keep primary actions and Back stationary. Only long profile details scroll on short windows.
        ScrollContainer detailsScroll = new() { Name = "CommandDetailsScroll",
            SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto };
        right.AddChild(detailsScroll);
        VBoxContainer details = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        details.AddThemeConstantOverride("separation", 10);
        detailsScroll.AddChild(details);

        Label commander = SectionLabel("COMMANDER", 13, style);
        commander.AddThemeColorOverride("font_color", style.MutedTextColor);
        details.AddChild(commander);
        Label name = SectionLabel(ProfileStore.Selected.Name.ToUpperInvariant(), 19, style);
        name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        details.AddChild(name);
        details.AddChild(new HSeparator());

        PlayerProgressionPanel progression = new() { Name = "PlayerProgression",
            AllowDebugGrant = ShowProgressionDebugGrant, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        details.AddChild(progression);
        Button back = UIButtonFactory.Create("BACK TO MAIN MENU", () => Navigate(MenuNavigation.MainScene),
            UIButtonStyle.Compact, 50f, style, backNavigation: true);
        back.Name = "BackToMainMenu";
        back.AddThemeFontSizeOverride("font_size", 14);
        right.AddChild(back);
    }

    // =========================================================
    // Menu surfaces share the project's cyan palette; only their relative width differs.
    private static PanelContainer Panel(string name, UIMasterConfig style, float ratio)
    {
        PanelContainer panel = new()
        {
            Name = name, SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsStretchRatio = ratio
        };
        StyleBoxFlat box = UIButtonFactory.Box(new Color("#10232f"), style.AccentColor);
        box.ContentMarginLeft = box.ContentMarginRight = 18;
        box.ContentMarginTop = box.ContentMarginBottom = 17;
        panel.AddThemeStyleboxOverride("panel", box);
        return panel;
    }

    private static VBoxContainer Stack(PanelContainer parent, int separation)
    {
        VBoxContainer content = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill };
        content.AddThemeConstantOverride("separation", separation);
        parent.AddChild(content);
        return content;
    }

    private static Label SectionLabel(string text, int size, UIMasterConfig style)
    {
        Label label = UIButtonFactory.Label(text, size, style);
        label.MouseFilter = MouseFilterEnum.Ignore;
        return label;
    }
}
