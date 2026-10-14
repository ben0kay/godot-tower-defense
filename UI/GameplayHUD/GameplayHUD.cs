// Permanent top HUD, Ctrl-toggle tower tray, and independent bottom-right inspector.
// The Battle Tower button now enters placement mode; other options stay informational.
using Godot;

public partial class GameplayHUD : Control
{
    #region Configuration
    [Export] public UIMasterConfig Config { get; set; }

    // =========================================================
    // Placement subscribes to Battle Tower selections; other IDs are prototypes.
    [Signal] public delegate void TowerChoiceRequestedEventHandler(string towerId);
    // =========================================================
    // The wave service owns scheduling; this button only requests a manual start.
    [Signal] public delegate void WaveStartRequestedEventHandler();
    #endregion

    #region Cached Nodes
    private PanelContainer _topBar, _towerBar, _inspector;
    private HBoxContainer _bottomArea;
    private Label _wave, _credits, _core, _towerName, _towerCategory, _towerStats, _towerNotes;
    private Button _startWave;
    private ProgressBar _healthBar;
    private StyleBoxFlat _healthFill;
    private Tween _trayTween;
    private bool _trayOpen;
    #endregion

    #region Prototype Tower List
    private readonly record struct PrototypeTower(string Id, string Name, string Category,
        string Stats, string Notes);

    // These entries are just HUD samples. No towers are placed or instantiated by this class.
    private static readonly PrototypeTower[] SampleTowers =
    {
        new("battle", "Battle Tower", "Offensive", "Footprint: 1 x 1\nBarrels: 1\nRange: 4 cells", "Balanced starter tower. Click to place on the grid."),
        new("laser", "Laser Tower", "Offensive", "Footprint: 1 x 1\nCost: TBD\nDamage: TBD\nRange: TBD", "Planned energy weapon."),
        new("missile", "Missile Tower", "Offensive", "Footprint: 1 x 1\nCost: TBD\nSplash: TBD", "Planned explosive defense."),
        new("repair", "Repair Tower", "Support", "Footprint: 1 x 1\nCost: TBD\nRepair rate: TBD", "Repairs nearby friendly towers."),
        new("solar", "Solar Tower", "Economy / Hybrid", "Footprint: 1 x 1\nEnergy: TBD\nLaser upgrade: Future", "Energy production; can gain a defensive laser later."),
        new("resource", "Resource Tower", "Economy", "Footprint: 2 x 2\nCredits: TBD\nPayout: TBD", "Generates income over time.")
    };
    #endregion

    #region Lifecycle
    // =========================================================
    // Cache child scenes once and prepare a HUD that works without a player actor.
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _topBar = GetNode<PanelContainer>("TopBar");
        _bottomArea = GetNode<HBoxContainer>("BottomArea");
        _towerBar = GetNode<PanelContainer>("BottomArea/TraySlot/TowerBar");
        _inspector = GetNode<PanelContainer>("BottomArea/Inspector");

        _wave = GetNode<Label>("TopBar/Status/Wave");
        _credits = GetNode<Label>("TopBar/Status/Credits");
        _core = GetNode<Label>("TopBar/Status/Citadel/Health");
        _towerName = GetNode<Label>("BottomArea/Inspector/Insets/Body/TowerName");
        _towerCategory = GetNode<Label>("BottomArea/Inspector/Insets/Body/TowerCategory");
        _towerStats = GetNode<Label>("BottomArea/Inspector/Insets/Body/TowerStats");
        _towerNotes = GetNode<Label>("BottomArea/Inspector/Insets/Body/Notes");

        _startWave = GetNode<Button>("TopBar/Status/StartWave");
        _startWave.Pressed += () => EmitSignal(SignalName.WaveStartRequested);
        _healthBar = GetNode<ProgressBar>("TopBar/Status/Citadel/Bar");
        _topBar.GetNode<Control>("Status").Resized += LayoutStatus;
        ApplyLayout();
        ApplyAppearance();
        BindTowerButtons();
        GetNode<Button>("BottomArea/Inspector/Insets/Body/Heading/CloseButton").Pressed += ClearInspection;
        _inspector.Hide();
        SetTowerBarOpen(Config.StartWithTowerBarOpen, true);
    }
    #endregion

    #region Input and Tray
    // =========================================================
    // Press Ctrl once to open or close the tower tray; no holding is necessary.
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo || key.Keycode != Key.Ctrl) return;
        SetTowerBarOpen(!_trayOpen);
        GetViewport().SetInputAsHandled();
    }

    // =========================================================
    // Slide/fade the tray without moving the inspector; suppress stale close tweens.
    public void SetTowerBarOpen(bool open, bool immediate = false)
    {
        _trayOpen = open;
        _trayTween?.Kill();
        if (open) _towerBar.Show();

        if (immediate || Config.TowerBarTransitionSeconds <= 0f)
        {
            _towerBar.Modulate = new Color(1f, 1f, 1f, open ? 1f : 0f);
            _towerBar.Position = new Vector2(0f, open ? 0f : 12f);
            _towerBar.Visible = open;
            return;
        }

        _trayTween = CreateTween().SetParallel(true);
        _trayTween.TweenProperty(_towerBar, "modulate:a", open ? 1f : 0f, Config.TowerBarTransitionSeconds)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _trayTween.TweenProperty(_towerBar, "position:y", open ? 0f : 12f, Config.TowerBarTransitionSeconds)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        if (!open)
            _trayTween.Chain().TweenCallback(Callable.From(() => { if (!_trayOpen) _towerBar.Hide(); }));
    }
    #endregion

    #region Tower Selection and Inspector
    // =========================================================
    // Populate the prototype bar. Names and IDs are temporary until tower definitions exist.
    private void BindTowerButtons()
    {
        string[] buttonNames = { "BattleButton", "LaserButton", "MissileButton",
            "RepairButton", "SolarButton", "ResourceButton" };
        for (int i = 0; i < buttonNames.Length; i++)
        {
            PrototypeTower tower = SampleTowers[i];
            Button button = GetNode<Button>($"BottomArea/TraySlot/TowerBar/Insets/Tray/Buttons/{buttonNames[i]}");
            button.Pressed += () => SelectPrototypeTower(tower);
        }
    }

    // =========================================================
    // Selecting a build-menu button previews stats and emits an integration signal.
    // Nothing is instantiated in the world until the next placement implementation.
    private void SelectPrototypeTower(PrototypeTower tower)
    {
        ShowTowerDetails(tower.Name, tower.Category, tower.Stats, tower.Notes);
        EmitSignal(SignalName.TowerChoiceRequested, tower.Id);
    }

    // =========================================================
    // This public entry point will also display clicked world towers in later passes.
    public void ShowTowerDetails(string name, string category, string stats, string notes = "")
    {
        _towerName.Text = name;
        _towerCategory.Text = category;
        _towerStats.Text = stats;
        _towerNotes.Text = notes;
        _inspector.Show();
    }

    // =========================================================
    // Hide the inspector independently from the build tray.
    public void ClearInspection() => _inspector.Hide();
    #endregion

    #region Future Gameplay Status API
    // =========================================================
    // Bind the actual wave system here when waves are implemented.
    public void SetWave(int current, int total = 0) =>
        _wave.Text = total > 0 ? $"WAVE {current:00} / {total:00}" : $"WAVE {current:00}";

    // =========================================================
    // Event-driven wave state presentation; no countdown or wave ownership lives in the HUD.
    public void SetWaveControls(int current, int total, string button, bool canStart, string message)
    {
        if (total > 0) SetWave(current, total); else _wave.Text = "WAVE --";
        _startWave.Text = button; _startWave.Disabled = !canStart;
        _startWave.TooltipText = message; _wave.TooltipText = message;
        _startWave.Visible = total > 0 || button == "SETUP ERROR";
    }

    // =========================================================
    // Bind the actual economy here when the resource system exists.
    public void SetCredits(int amount) => _credits.Text = $"CREDITS {amount:N0}";

    // =========================================================
    // Preserve existing callers while naming the physical objective correctly.
    public void SetCoreHealth(int current, int maximum) => SetCitadelHealth(current, maximum);

    // =========================================================
    // Display authoritative health only; absent maximum leaves the status unknown.
    public void SetCitadelHealth(int current, int maximum)
    {
        _healthBar.Visible = maximum > 0;
        if (maximum <= 0) { _core.Text = "CITADEL --"; _healthBar.Value = 0; return; }
        int health = Mathf.Clamp(current, 0, maximum);
        float fraction = (float)health / maximum;
        _core.Text = $"CITADEL {health:N0} / {maximum:N0}";
        _healthBar.Value = fraction * 100f;
        _healthFill.BgColor = fraction <= Config.HealthCriticalThreshold ? Config.HealthCriticalColor
            : fraction <= Config.HealthWarningThreshold ? Config.HealthWarningColor : Config.HealthHealthyColor;
    }
    #endregion

    #region Layout and Appearance
    // =========================================================
    // Size both HUD rows from the shared Inspector-editable config.
    private void ApplyLayout()
    {
        _topBar.OffsetBottom = Config.TopBarHeight;
        LayoutStatus();
        _bottomArea.OffsetLeft = Config.EdgeMargin;
        _bottomArea.OffsetRight = -Config.EdgeMargin;
        _bottomArea.OffsetTop = -(Config.BottomBarHeight + Config.EdgeMargin);
        _bottomArea.OffsetBottom = -Config.EdgeMargin;
        _inspector.CustomMinimumSize = new Vector2(Config.InspectorWidth, 0f);
    }

    // =========================================================
    // Reserve non-overlapping side zones around an exactly viewport-centered objective.
    private void LayoutStatus()
    {
        if (_core == null) return;
        Control status = _topBar.GetNode<Control>("Status");
        float width = status.Size.X, height = status.Size.Y;
        float margin = Mathf.Min(Config.EdgeMargin, width * 0.05f);
        float centerWidth = Mathf.Min(Config.CitadelBlockWidth, width * 0.46f);
        float sideWidth = Mathf.Max(0f, (width - centerWidth) * 0.5f - margin - Config.StatusGapPixels);
        _credits.Position = new Vector2(margin, 0); _credits.Size = new Vector2(sideWidth, height);
        _wave.Position = new Vector2(width - margin - sideWidth, 0); _wave.Size = new Vector2(sideWidth, height);
        Control center = status.GetNode<Control>("Citadel");
        center.Position = new Vector2((width - centerWidth) * 0.5f, 0);
        center.Size = new Vector2(centerWidth, height);
        int fontSize = Mathf.Clamp((int)(Config.StatusFontSize * Mathf.Min(1f, width / 900f)), 10, Config.StatusFontSize);
        foreach (Label label in new[] { _credits, _core, _wave }) label.AddThemeFontSizeOverride("font_size", fontSize);
        float buttonWidth = Mathf.Min(140f, sideWidth);
        _startWave.Position = new Vector2(width - margin - buttonWidth, height + 6);
        _startWave.Size = new Vector2(buttonWidth, 26);
        _startWave.AddThemeFontSizeOverride("font_size", Mathf.Max(10, fontSize - 2));
        _healthBar.CustomMinimumSize = new Vector2(Mathf.Min(Config.CitadelBarWidth, centerWidth), Config.CitadelBarHeight);
    }

    // =========================================================
    // Apply a restrained sci-fi palette without hardcoding StyleBox resources in every scene.
    private void ApplyAppearance()
    {
        _topBar.AddThemeStyleboxOverride("panel", MakePanel(Config.TopBarColor, Config.AccentColor));
        _towerBar.AddThemeStyleboxOverride("panel", MakePanel(Config.PanelColor, Config.AccentColor));
        _inspector.AddThemeStyleboxOverride("panel", MakePanel(Config.PanelColor, Config.AccentColor));

        _startWave.AddThemeColorOverride("font_color", Config.TextColor);
        _startWave.AddThemeColorOverride("font_disabled_color", Config.MutedTextColor);
        _startWave.AddThemeStyleboxOverride("normal", MakePanel(Config.ButtonColor, Config.AccentColor));
        _startWave.AddThemeStyleboxOverride("hover", MakePanel(Config.ButtonColor.Lightened(0.2f), Config.AccentColor));
        _startWave.AddThemeStyleboxOverride("disabled", MakePanel(Config.TopBarColor, Config.MutedTextColor));
        _startWave.AddThemeStyleboxOverride("focus", MakeFocusPanel(Config.AccentColor));
        _healthFill = new StyleBoxFlat { BgColor = Config.HealthHealthyColor };
        _healthBar.AddThemeStyleboxOverride("fill", _healthFill);
        _healthBar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = Config.ButtonColor });

        string[] buttons = { "BattleButton", "LaserButton", "MissileButton",
            "RepairButton", "SolarButton", "ResourceButton" };
        foreach (string name in buttons)
        {
            Button button = GetNode<Button>($"BottomArea/TraySlot/TowerBar/Insets/Tray/Buttons/{name}");
            button.CustomMinimumSize = new Vector2(Config.TowerButtonWidth, 65f);
            button.AddThemeColorOverride("font_color", Config.TextColor);
            button.AddThemeColorOverride("font_hover_color", Colors.White);
            button.AddThemeStyleboxOverride("normal", MakePanel(Config.ButtonColor, Config.ButtonColor.Lightened(0.30f)));
            button.AddThemeStyleboxOverride("hover", MakePanel(Config.ButtonColor.Lightened(0.2f), Config.AccentColor));
            button.AddThemeStyleboxOverride("pressed", MakePanel(Config.ButtonColor.Lightened(0.32f), Config.AccentColor));
            button.AddThemeStyleboxOverride("focus", MakeFocusPanel(Config.AccentColor));
        }

        Label[] primary = { _wave, _credits, _core, _towerName };
        foreach (Label label in primary) label.AddThemeColorOverride("font_color", Config.TextColor);

        Label[] muted = { _towerCategory, _towerStats, _towerNotes,
            GetNode<Label>("BottomArea/TraySlot/TowerBar/Insets/Tray/Header/Hint") };
        foreach (Label label in muted) label.AddThemeColorOverride("font_color", Config.MutedTextColor);

        Label[] accents = {
            GetNode<Label>("BottomArea/TraySlot/TowerBar/Insets/Tray/Header/Title"),
            GetNode<Label>("BottomArea/Inspector/Insets/Body/Heading/Title") };
        foreach (Label label in accents) label.AddThemeColorOverride("font_color", Config.AccentColor);

        Button close = GetNode<Button>("BottomArea/Inspector/Insets/Body/Heading/CloseButton");
        close.AddThemeColorOverride("font_color", Config.AccentColor);
        close.AddThemeColorOverride("font_hover_color", Colors.White);
    }

    // =========================================================
    // Common panel border geometry for HUD pieces and tower buttons.
    private static StyleBoxFlat MakePanel(Color fill, Color border) => new()
    {
        BgColor = fill, BorderColor = border,
        BorderWidthLeft = 1, BorderWidthTop = 1,
        BorderWidthRight = 1, BorderWidthBottom = 1,
        CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
        CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
    };

    // =========================================================
    // Keep keyboard focus visible without painting over selected tower buttons.
    private static StyleBoxFlat MakeFocusPanel(Color border) => new()
    {
        DrawCenter = false, BorderColor = border,
        BorderWidthLeft = 1, BorderWidthTop = 1,
        BorderWidthRight = 1, BorderWidthBottom = 1
    };
    #endregion
}
