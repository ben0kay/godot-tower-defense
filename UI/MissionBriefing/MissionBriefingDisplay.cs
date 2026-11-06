// Reusable load-time mission briefing: one modal per fresh battlefield, with no wave-spawning authority.
using Godot;

public partial class MissionBriefingDisplay : CanvasLayer
{
    private MissionBriefingDefinition _briefing;
    private Control _screen;
    private PanelContainer _panel;
    private ScrollContainer _scroll;
    private Button _continue;
    private GameInput _input;
    private Control _previousFocus;
    private bool _pausedBefore;
    public bool IsOpen => _screen?.Visible == true;

    #region Lifecycle

    // =========================================================
    // The shared battlefield's World/Level has its per-map settings before child Ready.
    // Checkpoint restoration never replays the pre-wave briefing.
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 80;
        if (GetParent() is BattlefieldLauncher launcher && launcher.RestoringCampaign?.Checkpoint != null)
            return;

        _briefing = GetNodeOrNull<WorldLevel>("../World/Level")?.Settings?.MissionBriefing;
        if (_briefing == null || !_briefing.Enabled || string.IsNullOrWhiteSpace(_briefing.Message))
            return;

        BuildView();
        _input = GetNodeOrNull<GameInput>("/root/GameInput");
        _input?.Bind(this, GameInput.BackAction, CloseFromShortcut, 150);
        _input?.Bind(this, GameInput.BuildAction, () => IsOpen, 150);
        Callable.From(ShowOnEntry).CallDeferred();
    }

    // =========================================================
    // Run after all battlefield Ready callbacks; no wave scheduler events are emitted.
    private void ShowOnEntry()
    {
        if (!IsInsideTree() || IsOpen || _screen == null) return;
        _previousFocus = GetViewport().GuiGetFocusOwner();
        _pausedBefore = GetTree().Paused;
        _screen.Show();
        GetTree().Paused = true;
        _continue.GrabFocus();
    }

    // =========================================================
    // Escape dismisses the briefing rather than opening Pause while its modal is active.
    private bool CloseFromShortcut()
    {
        if (!IsOpen) return false;
        Dismiss();
        return true;
    }

    // =========================================================
    // Closing does not emit WaveStartRequested: the existing START WAVE button remains manual.
    public void Dismiss()
    {
        if (!IsOpen) return;
        _screen.Hide();
        GetTree().Paused = _pausedBefore;
        if (GodotObject.IsInstanceValid(_previousFocus) && _previousFocus.IsVisibleInTree())
            _previousFocus.GrabFocus();
        else _continue.ReleaseFocus();
        _previousFocus = null;
    }

    // =========================================================
    // A scene transition must not leave the next screen paused or keep scoped shortcuts alive.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_input)) _input.Unbind(this);
        if (IsOpen) GetTree().Paused = _pausedBefore;
        if (GodotObject.IsInstanceValid(_screen)) _screen.Resized -= FitToViewport;
        _previousFocus = null;
    }
    #endregion

    #region View construction

    // =========================================================
    // Reuse shared menu presentation and button styles; the map remains visible beneath a tinted veil.
    private void BuildView()
    {
        UIMasterConfig theme = GD.Load<UIMasterConfig>("res://UI/DefaultUIConfig.tres") ?? new();
        _screen = new Control { Name = "Screen", MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(_screen);
        _screen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        VBoxContainer content = MenuLayout.Create(_screen, "MISSION INTELLIGENCE", "PRE-DEPLOYMENT INTEL REPORT", 720f);
        _screen.GetChild<ColorRect>(0).Color = new Color(0.015f, 0.035f, 0.06f, 0.84f);
        _panel = (PanelContainer)content.GetParent();
        content.AddThemeConstantOverride("separation", 12);

        Label title = UIButtonFactory.Label(_briefing.Title, 23, theme);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(title);

        Label speaker = UIButtonFactory.Label(_briefing.Speaker, 12, theme);
        speaker.HorizontalAlignment = HorizontalAlignment.Center;
        speaker.AddThemeColorOverride("font_color", theme.AccentColor);
        speaker.Visible = !string.IsNullOrWhiteSpace(_briefing.Speaker);
        content.AddChild(speaker);

        _scroll = new ScrollContainer
        {
            Name = "IntelTextScroll",
            CustomMinimumSize = new Vector2(0, 260),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        content.AddChild(_scroll);
        VBoxContainer report = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        report.AddThemeConstantOverride("separation", 12);
        _scroll.AddChild(report);

        Label body = UIButtonFactory.Label(_briefing.Message, 17, theme);
        body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        report.AddChild(body);

        if (!string.IsNullOrWhiteSpace(_briefing.Objectives))
        {
            report.AddChild(new HSeparator());
            Label heading = UIButtonFactory.Label("MISSION OBJECTIVES", 14, theme);
            heading.AddThemeColorOverride("font_color", theme.AccentColor);
            report.AddChild(heading);
            Label objectives = UIButtonFactory.Label(_briefing.Objectives, 16, theme);
            objectives.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            objectives.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            report.AddChild(objectives);
        }

        Label note = UIButtonFactory.Label("Wave 1 begins only when you press START WAVE.", 12, theme);
        note.HorizontalAlignment = HorizontalAlignment.Center;
        note.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        note.AddThemeColorOverride("font_color", theme.MutedTextColor);
        content.AddChild(note);
        _continue = UIButtonFactory.Create("CONTINUE TO DEPLOYMENT", Dismiss, UIButtonStyle.Action, 46, theme);
        content.AddChild(_continue);

        _screen.Resized += FitToViewport;
        FitToViewport();
        _screen.Hide();
    }

    // =========================================================
    // Keep the panel within smaller game windows; long authored intel remains scrollable.
    private void FitToViewport()
    {
        if (!GodotObject.IsInstanceValid(_panel) || !GodotObject.IsInstanceValid(_scroll)) return;
        Vector2 size = GetViewport().GetVisibleRect().Size;
        _panel.CustomMinimumSize = new Vector2(Mathf.Min(720f, Mathf.Max(170f, size.X - 68f)), 0f);
        _scroll.CustomMinimumSize = new Vector2(0f, Mathf.Min(310f, Mathf.Max(75f, size.Y - 285f)));
    }
    #endregion
}
