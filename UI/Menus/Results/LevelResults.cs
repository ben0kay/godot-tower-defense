// Reusable mission debrief presentation: styled overlay, ordered stat reveals, no gameplay/outcome ownership.
using Godot;
using System;
using System.Collections.Generic;

public partial class LevelResults : CanvasLayer
{
    [Signal] public delegate void RetryRequestedEventHandler();
    [Signal] public delegate void MenuRequestedEventHandler();

    [ExportGroup("Presentation")]
    [Export(PropertyHint.Range, "0.05,1,0.05")] public float StatRevealSeconds { get; set; } = 0.24f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float StatGapSeconds { get; set; } = 0.10f;
    [Export] public bool PreviewOnReady { get; set; } = false;

    public bool IsOpen => _screen?.Visible == true;

    private Control _screen;
    private Label _heading, _subtitle;
    private VBoxContainer _experience;
    private HBoxContainer _actions;
    private Button _retry;
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
        _screen = new Control { Name = "Screen", MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(_screen);
        _screen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        VBoxContainer content = MenuLayout.Create(_screen, "MISSION DEBRIEF", "LEVEL RESULTS", 660f);
        _screen.GetChild<ColorRect>(0).Color = new Color(0.01f, 0.03f, 0.05f, 0.87f);
        _heading = content.GetChild<Label>(0);
        _subtitle = content.GetChild<Label>(1);

        _skip = UIButtonFactory.Create("SKIP REVEAL", SkipReveal, UIButtonStyle.Compact, 30f, palette);
        content.AddChild(_skip);

        VBoxContainer rows = new();
        rows.AddThemeConstantOverride("separation", 7);
        content.AddChild(rows);
        foreach (string heading in new[] { "ENEMIES DESTROYED", "ENEMIES BREACHED", "TOWERS CONSTRUCTED",
            "MISSION DURATION", "WAVES COMPLETED", "CITADEL INTEGRITY", "CREDITS EARNED", "CREDITS SPENT" })
            AddStat(rows, heading, palette);

        _experience = new VBoxContainer();
        _experience.AddThemeConstantOverride("separation", 7);
        content.AddChild(_experience);
        Label xpHeading = UIButtonFactory.Label("PLAYER EXPERIENCE  //  COMING SOON", 14, palette);
        xpHeading.AddThemeColorOverride("font_color", palette.MutedTextColor);
        _experience.AddChild(xpHeading);
        ProgressBar xpBar = new() { MinValue = 0, MaxValue = 100, Value = 0, ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 12), MouseFilter = Control.MouseFilterEnum.Ignore };
        _experience.AddChild(xpBar);
        Label xpNote = UIButtonFactory.Label("Player XP and bonuses have not been implemented.", 12, palette);
        xpNote.AddThemeColorOverride("font_color", palette.MutedTextColor);
        _experience.AddChild(xpNote);

        _actions = new HBoxContainer();
        _actions.AddThemeConstantOverride("separation", 12);
        content.AddChild(_actions);
        _retry = UIButtonFactory.Create("RETRY MISSION", () => EmitSignal(SignalName.RetryRequested), UIButtonStyle.Action, 46f, palette);
        _actions.AddChild(_retry);
        _actions.AddChild(UIButtonFactory.Create("RETURN TO MENU", () => EmitSignal(SignalName.MenuRequested), UIButtonStyle.Action, 46f, palette));

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
    // Called by the future session controller with a frozen end-of-level snapshot.
    public void ShowResults(LevelResultSnapshot snapshot)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        _reveal?.Kill();
        bool won = snapshot.Outcome == LevelOutcome.Victory;
        _heading.Text = won ? "VICTORY" : "MISSION FAILED";
        _heading.AddThemeColorOverride("font_color", won ? new Color("#76e3bd") : new Color("#ef7777"));
        _subtitle.Text = snapshot.LevelName + "  //  MISSION DEBRIEF";

        string[] values =
        {
            snapshot.EnemiesKilled.ToString("N0"), snapshot.EnemiesBreached.ToString("N0"),
            CountOrDash(snapshot.TowersPlaced), DurationOrDash(snapshot.MissionSeconds),
            snapshot.WavesCompleted + " / " + snapshot.TotalWaves,
            snapshot.CitadelIntegrityPercent is int integrity ? integrity + "%" : "—",
            CountOrDash(snapshot.CreditsEarned), CountOrDash(snapshot.CreditsSpent)
        };
        for (int i = 0; i < _stats.Count; i++)
        { _stats[i].Value.Text = values[i]; _stats[i].Panel.Hide(); _stats[i].Panel.Modulate = new Color(1f, 1f, 1f, 0f); }
        _experience.Hide(); _experience.Modulate = new Color(1f, 1f, 1f, 0f);
        _actions.Hide();
        _skip.Show();
        _screen.Show();

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
    // Explicit preview is opt-in: never masquerades as actual earned mission rewards.
    public void ShowPreview() => ShowResults(new LevelResultSnapshot
    {
        Outcome = LevelOutcome.Victory, LevelName = "UI PREVIEW — EXAMPLE VALUES",
        EnemiesKilled = 348, EnemiesBreached = 12, TowersPlaced = 26, MissionSeconds = 1122,
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
        _skip.Hide(); _actions.Show(); _retry.GrabFocus();
    }

    // =========================================================
    // Do not unpause or change scenes here; that belongs to the session lifecycle.
    public void HideResults()
    {
        _reveal?.Kill();
        _screen.Hide();
    }

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
