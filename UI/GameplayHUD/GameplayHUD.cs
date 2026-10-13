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
    #endregion

    #region Cached Nodes
    private PanelContainer _topBar, _towerBar, _inspector;
    private HBoxContainer _bottomArea;
    private Label _wave, _credits, _core, _towerName, _towerCategory, _towerStats, _towerNotes;
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

        _wave = GetNode<Label>("TopBar/Insets/Status/Wave");
        _credits = GetNode<Label>("TopBar/Insets/Status/Credits");
        _core = GetNode<Label>("TopBar/Insets/Status/Core");
        _towerName = GetNode<Label>("BottomArea/Inspector/Insets/Body/TowerName");
        _towerCategory = GetNode<Label>("BottomArea/Inspector/Insets/Body/TowerCategory");
        _towerStats = GetNode<Label>("BottomArea/Inspector/Insets/Body/TowerStats");
        _towerNotes = GetNode<Label>("BottomArea/Inspector/Insets/Body/Notes");

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
    // Bind the actual economy here when the resource system exists.
    public void SetCredits(int amount) => _credits.Text = $"CREDITS {amount:N0}";

    // =========================================================
    // Bind core durability here when the defended objective exists.
    public void SetCoreHealth(int current, int maximum)
    {
        float percent = maximum > 0 ? 100f * current / maximum : 0f;
        _core.Text = $"CORE {Mathf.Clamp(percent, 0f, 100f):0}%";
    }
    #endregion

    #region Layout and Appearance
    // =========================================================
    // Size both HUD rows from the shared Inspector-editable config.
    private void ApplyLayout()
    {
        _topBar.OffsetBottom = Config.TopBarHeight;
        _bottomArea.OffsetLeft = Config.EdgeMargin;
        _bottomArea.OffsetRight = -Config.EdgeMargin;
        _bottomArea.OffsetTop = -(Config.BottomBarHeight + Config.EdgeMargin);
        _bottomArea.OffsetBottom = -Config.EdgeMargin;
        _inspector.CustomMinimumSize = new Vector2(Config.InspectorWidth, 0f);
    }

    // =========================================================
    // Apply a restrained sci-fi palette without hardcoding StyleBox resources in every scene.
    private void ApplyAppearance()
    {
        _topBar.AddThemeStyleboxOverride("panel", MakePanel(Config.TopBarColor, Config.AccentColor));
        _towerBar.AddThemeStyleboxOverride("panel", MakePanel(Config.PanelColor, Config.AccentColor));
        _inspector.AddThemeStyleboxOverride("panel", MakePanel(Config.PanelColor, Config.AccentColor));

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
            GetNode<Label>("TopBar/Insets/Status/Shortcut"),
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
