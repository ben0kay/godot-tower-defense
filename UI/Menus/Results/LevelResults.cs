// Reusable mission debrief presentation: styled overlay, ordered stat reveals, no gameplay/outcome ownership.
using Godot;
using System;
using System.Collections.Generic;

public partial class LevelResults : CanvasLayer
{
    [Signal] public delegate void RetryRequestedEventHandler();
    [Signal] public delegate void MenuRequestedEventHandler();
    [Signal] public delegate void NextLevelRequestedEventHandler();
    [Signal] public delegate void ExitGameRequestedEventHandler();
    [Signal] public delegate void RewardRetryRequestedEventHandler();

    [ExportGroup("Presentation")]
    [Export(PropertyHint.Range, "0.05,1,0.05")] public float StatRevealSeconds { get; set; } = 0.24f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float StatGapSeconds { get; set; } = 0.10f;
    [Export] public bool PreviewOnReady { get; set; } = false;

    public bool IsOpen => _screen?.Visible == true;
    public bool ActionsAvailable { get; private set; }
    public Button PrimaryButton => _primary;
    public Button DetailsButton => _details;
    private bool _won, _hasNext;
    private UIMasterConfig _palette;
    private PanelContainer _frame;
    private AcceptDialog _error;

    private Control _screen;
    private Label _heading, _subtitle;
    private VBoxContainer _experience;
    private Label _xpHeading, _xpNote, _xpLines;
    private ProgressBar _xpBar;
    private Button _retryXP;
    private HBoxContainer _actions;
    private Button _primary, _details, _menu, _exit;
    private Button _skip;
    private Tween _reveal;
    private readonly List<(PanelContainer Panel, Label Value)> _stats = new();

    #region Layout
    // =========================================================
    // Self-contained UI; stays hidden until a session provides a final result.
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 150;
        UIMasterConfig palette = GD.Load<UIMasterConfig>("res://UI/DefaultUIConfig.tres") ?? new UIMasterConfig();
        _palette = palette;
        _screen = new Control { Name = "Screen", MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(_screen);
        _screen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        VBoxContainer content = MenuLayout.Create(_screen, "MISSION DEBRIEF", "LEVEL RESULTS", 660f);
        _screen.GetChild<ColorRect>(0).Color = new Color(0.01f, 0.03f, 0.05f, 0.87f);
        _frame = (PanelContainer)content.GetParent();
        _heading = content.GetChild<Label>(0);
        _subtitle = content.GetChild<Label>(1);

        _skip = UIButtonFactory.Create("SKIP REVEAL", SkipReveal, UIButtonStyle.Compact, 30f, palette);
        content.AddChild(_skip);

        ScrollContainer statScroll = new() { CustomMinimumSize = new Vector2(0, 220),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto };
        content.AddChild(statScroll);
        VBoxContainer rows = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        rows.AddThemeConstantOverride("separation", 7);
        statScroll.AddChild(rows);
        foreach (string heading in new[] { "ENEMIES DESTROYED", "ENEMIES BREACHED", "TOWERS PLACED",
            "TOWERS CONSTRUCTED", "UPGRADES PURCHASED",
            "MISSION DURATION", "WAVES COMPLETED", "CITADEL INTEGRITY", "CREDITS EARNED", "CREDITS SPENT" })
            AddStat(rows, heading, palette);

        _experience = new VBoxContainer();
        _experience.AddThemeConstantOverride("separation", 7);
        content.AddChild(_experience);
        _xpHeading = UIButtonFactory.Label("MISSION XP", 14, palette); _xpHeading.Name = "MissionXPHeading";
        _xpHeading.AddThemeColorOverride("font_color", palette.AccentColor);
        _experience.AddChild(_xpHeading);
        _xpLines = UIButtonFactory.Label("", 12, palette); _xpLines.Name = "MissionXPLines";
        _xpLines.AutowrapMode = TextServer.AutowrapMode.WordSmart; _experience.AddChild(_xpLines);
        _xpBar = new() { Name = "PlayerXPBar", MinValue = 0, MaxValue = 100, Value = 0, ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 12), MouseFilter = Control.MouseFilterEnum.Ignore };
        _experience.AddChild(_xpBar);
        _xpNote = UIButtonFactory.Label("", 12, palette); _xpNote.Name = "PlayerXPSummary";
        _xpNote.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _xpNote.AddThemeColorOverride("font_color", palette.MutedTextColor); _experience.AddChild(_xpNote);
        _retryXP = UIButtonFactory.Create("RETRY XP SAVE", () => EmitSignal(SignalName.RewardRetryRequested), UIButtonStyle.Compact, 30f, palette);
        _experience.AddChild(_retryXP); _retryXP.Hide();

        _actions = new HBoxContainer();
        _actions.AddThemeConstantOverride("separation", 12);
        content.AddChild(_actions);
        _primary = UIButtonFactory.Create("RETRY MISSION", RequestPrimary, UIButtonStyle.Action, 46f, palette);
        _details = UIButtonFactory.Create("DETAILS", null, UIButtonStyle.Action, 46f, palette);
        _details.Disabled = true; _details.TooltipText = "Detailed mission statistics — coming soon.";
        _menu = UIButtonFactory.Create("RETURN TO CAMPAIGN", () => { if (ActionsAvailable) EmitSignal(SignalName.MenuRequested); }, UIButtonStyle.Action, 46f, palette);
        _exit = UIButtonFactory.Create("EXIT GAME", () => { if (ActionsAvailable) EmitSignal(SignalName.ExitGameRequested); }, UIButtonStyle.Action, 46f, palette);
        foreach (Button button in new[] { _primary, _details, _menu, _exit }) _actions.AddChild(button);
        _error = new AcceptDialog { Title = "Navigation error" }; AddChild(_error);
        UIButtonFactory.Apply(_error.GetOkButton(), UIButtonStyle.Action);

        _screen.Hide();
        if (PreviewOnReady) ShowPreview();
    }

    // =========================================================
    // One standard compact row, ready to reveal without rebuilding the layout.
    private void AddStat(VBoxContainer parent, string caption, UIMasterConfig palette)
    {
        PanelContainer panel = new();
        panel.AddThemeStyleboxOverride("panel", UIButtonFactory.Box(palette.TopBarColor, palette.ButtonColor));
        parent.AddChild(panel);
        HBoxContainer line = new();
        panel.AddChild(line);
        Label name = UIButtonFactory.Label(caption, 13, palette);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        name.AddThemeColorOverride("font_color", palette.MutedTextColor);
        line.AddChild(name);
        Label value = UIButtonFactory.Label("—", 17, palette);
        value.HorizontalAlignment = HorizontalAlignment.Right;
        value.AddThemeColorOverride("font_color", palette.AccentColor);
        line.AddChild(value);
        _stats.Add((panel, value));
    }
    #endregion

    #region Display
    // =========================================================
    // Called by the session controller with a frozen end-of-level snapshot.
    public void ShowResults(LevelResultSnapshot snapshot, bool hasNextLevel = false)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        _reveal?.Kill();
        SetMissionReward(null, "", true);
        bool won = snapshot.Outcome == LevelOutcome.Victory;
        _won = won; _hasNext = hasNextLevel; ActionsAvailable = false;
        _primary.Text = won ? "NEXT LEVEL" : "RETRY MISSION";
        _primary.TooltipText = won && !hasNextLevel ? "No next level available." : "";
        foreach (Button button in new[] { _primary, _menu, _exit }) button.Disabled = true;
        _error.Hide();
        Color accent = won ? new Color("#76e3bd") : new Color("#ef7777");
        _frame.AddThemeStyleboxOverride("panel", UIButtonFactory.Box(_palette.PanelColor, accent));
        UIMasterConfig actionPalette = (UIMasterConfig)_palette.Duplicate(); actionPalette.AccentColor = accent;
        foreach (Button button in new[] { _primary, _details, _menu, _exit }) UIButtonFactory.Apply(button, UIButtonStyle.Action, actionPalette);
        foreach (var stat in _stats) stat.Value.AddThemeColorOverride("font_color", accent);
        _heading.Text = won ? "VICTORY" : "MISSION FAILED";
        _heading.AddThemeColorOverride("font_color", won ? new Color("#76e3bd") : new Color("#ef7777"));
        _subtitle.Text = snapshot.LevelName + "  //  MISSION DEBRIEF";

        string[] values =
        {
            snapshot.EnemiesKilled.ToString("N0"), snapshot.EnemiesBreached.ToString("N0"),
            CountOrDash(snapshot.TowersPlaced), CountOrDash(snapshot.TowersConstructed),
            CountOrDash(snapshot.UpgradesPurchased), DurationOrDash(snapshot.MissionSeconds),
            snapshot.WavesCompleted + " / " + snapshot.TotalWaves,
            snapshot.CitadelIntegrityPercent is int integrity ? integrity + "%" : "—",
            CountOrDash(snapshot.CreditsEarned), CountOrDash(snapshot.CreditsSpent)
        };
        for (int i = 0; i < _stats.Count; i++)
        { _stats[i].Value.Text = values[i]; _stats[i].Panel.Hide(); _stats[i].Panel.Modulate = new Color(1f, 1f, 1f, 0f); }
        _experience.Hide(); _experience.Modulate = new Color(1f, 1f, 1f, 0f);
        _actions.Hide();
        _skip.Show();
        _screen.Show(); _skip.GrabFocus();

        _reveal = CreateTween();
        _reveal.TweenInterval(0.20f);
        foreach (var stat in _stats)
        {
            PanelContainer panel = stat.Panel;
            _reveal.TweenCallback(Callable.From(() => panel.Show()));
            _reveal.TweenProperty(panel, "modulate:a", 1.0f, StatRevealSeconds);
            _reveal.TweenInterval(StatGapSeconds);
        }
        _reveal.TweenCallback(Callable.From(() => _experience.Show()));
        _reveal.TweenProperty(_experience, "modulate:a", 1.0f, 0.3f);
        _reveal.TweenCallback(Callable.From(FinishReveal));
    }

    // =========================================================
    // Display a saved immutable receipt only; showing, skipping or reopening this UI cannot grant XP.
    public void SetMissionReward(MissionRewardResult reward, string error, bool profileless)
    {
        _retryXP.Visible = !profileless && reward == null && !string.IsNullOrEmpty(error);
        _xpBar.Visible = reward != null; _xpLines.Visible = reward != null;
        if (reward == null)
        {
            _xpHeading.Text = profileless ? "MISSION XP  //  NOT SAVED" : "MISSION XP  //  SAVE FAILED";
            _xpNote.Text = profileless ? "No deployment profile. Select a profile before starting a mission to earn XP."
                : "XP has not been saved. " + error + " Retry before leaving this mission.";
            _xpHeading.AddThemeColorOverride("font_color", profileless ? _palette.MutedTextColor : _palette.HealthCriticalColor);
            return;
        }
        PlayerProgression before = reward.Before, after = reward.After;
        _xpHeading.Text = $"MISSION XP  //  +{reward.Breakdown.TotalXP:N0} XP SAVED";
        _xpHeading.AddThemeColorOverride("font_color", _palette.AccentColor);
        var rows = new List<string>();
        foreach (var line in reward.Breakdown.Lines) rows.Add($"{line.Label}: {line.Amount:+#,0;-#,0;0} XP");
        _xpLines.Text = string.Join("\n", rows);
        long points = after.UnspentResearchPoints - before.UnspentResearchPoints;
        _xpNote.Text = (after.Level > before.Level ? $"LEVEL UP · {before.Level} → {after.Level} · +{points:N0} Research Points\n" : $"Player Level {after.Level}\n")
            + $"{after.CurrentXP:N0} / {after.RequiredXP:N0} XP toward Level {after.Level + 1}. Research is available from Campaign.";
        _xpBar.MaxValue = after.RequiredXP; _xpBar.Value = after.CurrentXP;
        _xpBar.TooltipText = $"Lifetime player XP: {before.LifetimePlayerXP:N0} → {after.LifetimePlayerXP:N0}";
    }

    // =========================================================
    // Explicit preview is opt-in: never masquerades as actual earned mission rewards.
    public void ShowPreview() => ShowResults(new LevelResultSnapshot
    {
        Outcome = LevelOutcome.Victory, LevelName = "UI PREVIEW — EXAMPLE VALUES",
        EnemiesKilled = 348, EnemiesBreached = 12, TowersPlaced = 26, TowersConstructed = 24, UpgradesPurchased = 9, MissionSeconds = 1122,
        WavesCompleted = 12, TotalWaves = 12, CitadelHealth = 780, CitadelMaxHealth = 1000
    });

    // =========================================================
    // One click immediately shows all rows, XP placeholder and navigation buttons.
    public void SkipReveal()
    {
        if (!IsOpen) return;
        _reveal?.Kill();
        foreach (var stat in _stats) { stat.Panel.Show(); stat.Panel.Modulate = Colors.White; }
        _experience.Show(); _experience.Modulate = Colors.White;
        FinishReveal();
    }

    // =========================================================
    // Session controller handles retry/menu navigation; this overlay only emits intents.
    private void FinishReveal()
    {
        ActionsAvailable = true; _skip.Hide(); _actions.Show();
        _primary.Disabled = _won && !_hasNext; _menu.Disabled = false; _exit.Disabled = false;
        if (_primary.Disabled) _menu.GrabFocus(); else _primary.GrabFocus();
    }

    // =========================================================
    // Do not unpause or change scenes here; that belongs to the session lifecycle.
    public void HideResults()
    {
        _reveal?.Kill();
        _screen.Hide(); ActionsAvailable = false; _error.Hide();
    }

    // =========================================================
    // Buttons emit guarded intent only; all scene changes and unpause handling belong to the session.
    private void RequestPrimary()
    {
        if (!ActionsAvailable || _primary.Disabled) return;
        if (_won) EmitSignal(SignalName.NextLevelRequested); else EmitSignal(SignalName.RetryRequested);
    }
    public void ShowNavigationError(string message) { _error.DialogText = message; _error.PopupCentered(new Vector2I(460, 180)); }
    public bool DismissError() { if (!_error.Visible) return false; _error.Hide(); return true; }
    public override void _ExitTree() => _reveal?.Kill();

    // =========================================================
    // Unknown future metrics remain visibly unavailable, never invented as zero.
    private static string CountOrDash(int? value) => value.HasValue ? value.Value.ToString("N0") : "—";

    // =========================================================
    // Mission time excludes paused time once the session clock is implemented.
    private static string DurationOrDash(double? seconds)
    {
        if (!seconds.HasValue || !double.IsFinite(seconds.Value)) return "—";
        TimeSpan value = TimeSpan.FromSeconds(Math.Max(0, seconds.Value));
        return (int)value.TotalHours > 0 ? $"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}"
            : $"{(int)value.TotalMinutes:00}:{value.Seconds:00}";
    }
    #endregion
}
