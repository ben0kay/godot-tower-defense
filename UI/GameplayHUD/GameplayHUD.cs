// Permanent top HUD, Ctrl-toggle tower tray, and independent bottom-right inspector.
// Build entries come from the validated tower catalog; gameplay belongs to its controllers.
using Godot;
using System;
using System.Collections.Generic;

public partial class GameplayHUD : Control
{
    #region Configuration
    [Export] public UIMasterConfig Config { get; set; }

    // =========================================================
    // Placement owns validation and building; the HUD emits only a registered tower ID.
    [Signal] public delegate void TowerChoiceRequestedEventHandler(string towerId);
    // =========================================================
    // The wave service owns scheduling; this button only requests a manual start.
    [Signal] public delegate void WaveStartRequestedEventHandler();

    // =========================================================
    // The placement/upgrade controller validates the selected tower and payment.
    [Signal] public delegate void TowerUpgradeRequestedEventHandler();
    #endregion

    public bool IsInspectionVisible => _inspector?.Visible == true;

    #region Cached Nodes
    private PanelContainer _topBar, _towerBar, _inspector;
    private HBoxContainer _bottomArea;
    private Control _traySlot;
    private Label _wave, _credits, _core, _towerName, _towerCategory, _towerStats, _towerNotes;
    private Button _startWave, _upgradeButton;
    private Label _upgradeInfo;
    private CreditGainIndicator _creditGain;
    private ProgressBar _healthBar;
    private Tween _citadelBarTween, _citadelFlashTween, _wavePulseTween;
    private int _targetCredits, _previousCitadelHealth = -1, _previousCitadelMax, _previousWave;
    private double _displayCredits, _creditStart, _creditElapsed;
    private bool _creditsInitialized, _creditCounting, _snapNextCredits;
    private StyleBoxFlat _healthFill;
    private Tween _trayTween;
    private bool _trayOpen;
    #endregion

    private readonly Dictionary<string, Button> _buildButtons = new();

    #region Lifecycle
    // =========================================================
    // Cache child scenes once and prepare a HUD that works without a player actor.
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _topBar = GetNode<PanelContainer>("TopBar");
        _bottomArea = GetNode<HBoxContainer>("BottomArea");
        _towerBar = GetNode<PanelContainer>("BottomArea/TraySlot/TowerBar");
        _traySlot = GetNode<Control>("BottomArea/TraySlot");
        _traySlot.Resized += FitTrayToSlot;
        _inspector = GetNode<PanelContainer>("BottomArea/Inspector");

        _wave = GetNode<Label>("TopBar/Status/Wave");
        _credits = GetNode<Label>("TopBar/Status/Credits");
        SetProcess(false);
        _creditGain = GetNode<CreditGainIndicator>("TopBar/Status/CreditGain");
        _creditGain.Configure(Config);
        _core = GetNode<Label>("TopBar/Status/Citadel/Health");
        _towerName = GetNode<Label>("BottomArea/Inspector/Insets/Body/TowerName");
        _towerCategory = GetNode<Label>("BottomArea/Inspector/Insets/Body/TowerCategory");
        _towerStats = GetNode<Label>("BottomArea/Inspector/Insets/Body/TowerStats");
        _towerNotes = GetNode<Label>("BottomArea/Inspector/Insets/Body/Notes");

        _startWave = GetNode<Button>("TopBar/Status/StartWave");
        _startWave.Pressed += () => EmitSignal(SignalName.WaveStartRequested);
        _healthBar = GetNode<ProgressBar>("TopBar/Status/Citadel/Bar");
        _topBar.GetNode<Control>("Status").Resized += LayoutStatus;
        _upgradeInfo = GetNode<Label>("BottomArea/Inspector/Insets/Body/UpgradeInfo");
        _upgradeButton = GetNode<Button>("BottomArea/Inspector/Insets/Body/UpgradeButton");
        _upgradeButton.Pressed += () => EmitSignal(SignalName.TowerUpgradeRequested);
        ApplyLayout();
        ApplyAppearance();
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
    // Render registered available metadata only; no tower scenes or combat loops are instantiated.
    // Call after Catalog.Rebuild when registration/availability changes, never each physics frame.
    public void BindCatalog(TowerCatalog catalog)
    {
        HBoxContainer entries = GetNode<HBoxContainer>("BottomArea/TraySlot/TowerBar/Insets/Tray/Scroll/Buttons");
        foreach (Node child in entries.GetChildren()) { entries.RemoveChild(child); child.QueueFree(); }
        _buildButtons.Clear();
        foreach (TowerDefinition definition in catalog.Definitions)
        {
            if (!definition.Available) continue;
            string id = definition.TowerId;
            Button button = UIButtonFactory.Create(definition.DisplayName + "\n" + definition.Category + " · " +
                TowerPresentation.DescribeResourceCosts(definition.DefaultConfig.ConstructionCosts),
                () => EmitSignal(SignalName.TowerChoiceRequested, id), UIButtonStyle.TowerTile, height: 65f, config: Config);
            button.Name = "Tower_" + entries.GetChildCount();
            button.CustomMinimumSize = new Vector2(Config.TowerButtonWidth, 65f);
            button.TooltipText = definition.Description;
            button.Icon = definition.Icon;
            button.ExpandIcon = true;
            button.AddThemeConstantOverride("icon_max_width", 32);
            entries.AddChild(button);
            _buildButtons.Add(id, button);
        }
        if (_buildButtons.Count == 0)
            entries.AddChild(new Label { Text = "No towers available.", MouseFilter = MouseFilterEnum.Ignore });
    }

    // =========================================================
    // Stable ID lookup for keyboard/tool integration independent of generated scene-node names.
    public Button GetBuildButton(string towerId) => _buildButtons.GetValueOrDefault(towerId);

    // =========================================================
    // Placement supplies actual instance details and build previews to the independent Inspector.
    public void ShowTowerDetails(string name, string category, string stats, string notes = "")
    {
        SetUpgradeDetails("", "", false);
        _towerName.Text = name;
        _towerCategory.Text = category;
        _towerStats.Text = stats;
        _towerNotes.Text = notes;
        _inspector.Show();
    }

    // =========================================================
    // Display an authoritative controller preview; buttons carry no payment or stat logic.
    public void SetUpgradeDetails(string preview, string button, bool affordable)
    {
        _upgradeButton.TooltipText = "";
        _upgradeInfo.Text = preview; _upgradeInfo.Visible = preview.Length > 0;
        _upgradeButton.Text = button; _upgradeButton.Visible = button.Length > 0;
        _upgradeButton.Disabled = !affordable;
    }

    // =========================================================
    // Keep a refreshed retry button available while reporting rejected upgrade attempts.
    public void SetUpgradeError(string message) => _upgradeInfo.Text = message + "\n" + _upgradeInfo.Text;

    // =========================================================
    // Hide the inspector independently from the build tray.
    public void ClearInspection() => _inspector.Hide();
    #endregion

    #region Future Gameplay Status API
    // =========================================================
    // Bind the actual wave system here when waves are implemented.
    public void SetWave(int current, int total = 0)
    {
        _wave.Text = total > 0 ? $"WAVE {current:00} / {total:00}" : $"WAVE {current:00}";
        if (current > 0 && current > _previousWave) PulseWave();
        _previousWave = current;
    }

    // =========================================================
    // Event-driven wave state presentation; no countdown or wave ownership lives in the HUD.
    public void SetWaveControls(int current, int total, string button, bool canStart, string message)
    {
        if (total > 0) SetWave(current, total);
        else { _wave.Text = "WAVE --"; _previousWave = 0; }
        _startWave.Text = button; _startWave.Disabled = !canStart;
        _startWave.TooltipText = message; _wave.TooltipText = message;
        _startWave.Visible = total > 0 || button == "SETUP ERROR";
    }

    // =========================================================
    // Bind the actual economy here when the resource system exists.
    public int TargetCredits => _targetCredits;

    // =========================================================
    // The wallet supplies the true amount immediately; only the displayed counter eases toward it.
    public void SetCredits(int amount, bool immediate = false)
    {
        _targetCredits = amount;
        if (immediate || !_creditsInitialized || _snapNextCredits ||
            !Config.AnimateCreditBalance || Config.CreditCountSeconds <= 0f)
        {
            _displayCredits = amount;
            _creditCounting = false;
            SetProcess(false);
        }
        else if (Math.Abs(_displayCredits - amount) > 0.1d)
        {
            _creditStart = _displayCredits;
            _creditElapsed = 0d;
            _creditCounting = true;
            SetProcess(true);
        }
        _creditsInitialized = true;
        _snapNextCredits = false;
        UpdateCreditsText();
    }

    // =========================================================
    // Show green earned credits, separate from the permanent wallet total.
    public void ShowCreditReward(int amount)
    {
        if (Config.ShowCreditRewards) _creditGain.AddReward(amount);
        LayoutCreditFeedback();
    }

    // =========================================================
    // Show amber spending only for transactions that actually committed.
    public void ShowCreditExpense(int amount)
    {
        if (Config.ShowCreditExpenses) _creditGain.AddExpense(amount);
        LayoutCreditFeedback();
    }

    // =========================================================
    // Initial balances and new sessions snap to their starting value without reward animations.
    public void ResetCreditReward()
    {
        _creditGain.ResetRewards();
        _snapNextCredits = true;
    }

    // =========================================================
    // Credit counting runs only during transitions; never on every idle HUD frame.
    public override void _Process(double delta)
    {
        if (!_creditCounting) { SetProcess(false); return; }
        _creditElapsed += Math.Max(0d, delta);
        float t = Mathf.Clamp((float)(_creditElapsed / Math.Max(0.01f, Config.CreditCountSeconds)), 0f, 1f);
        float eased = 1f - (1f - t) * (1f - t) * (1f - t);
        _displayCredits = t >= 1f ? _targetCredits : _creditStart + (_targetCredits - _creditStart) * eased;
        if (t >= 1f) { _creditCounting = false; SetProcess(false); }
        UpdateCreditsText();
    }

    // =========================================================
    // Keep currency formatting in one place for initialization and animated changes.
    private void UpdateCreditsText()
    {
        _credits.Text = $"Credits: {Math.Round(_displayCredits):N0}";
        LayoutCreditFeedback();
    }

    // =========================================================
    // Measure the displayed balance so (+N)/(-N) sits immediately to its right.
    private void LayoutCreditFeedback()
    {
        if (_credits == null || _creditGain == null || Config == null) return;
        Font font = _credits.GetThemeFont("font");
        float textWidth = font.GetStringSize(_credits.Text, HorizontalAlignment.Left, -1,
            _credits.GetThemeFontSize("font_size")).X;
        float maxX = _credits.Position.X + _credits.Size.X;
        float x = _credits.Position.X + textWidth + 9f;
        float available = Mathf.Max(0f, maxX - x);
        _creditGain.SetOrigin(new Vector2(x, _credits.Position.Y + (_credits.Size.Y - _creditGain.GetCombinedMinimumSize().Y) * 0.5f));
        _creditGain.Size = new Vector2(available, _credits.Size.Y);
        if (available < 34f) _creditGain.Hide();
        else if (_creditGain.IsDisplaying) _creditGain.Show();
    }

    // =========================================================
    // Preserve existing callers while naming the physical objective correctly.
    public void SetCoreHealth(int current, int maximum) => SetCitadelHealth(current, maximum);

    // =========================================================
    // Display authoritative health only; absent maximum leaves the status unknown.
    public void SetCitadelHealth(int current, int maximum, bool animate = true)
    {
        bool wasVisible = _healthBar.Visible;
        _healthBar.Visible = maximum > 0;
        _citadelBarTween?.Kill();
        if (maximum <= 0)
        {
            _core.Text = "CITADEL --"; _healthBar.Value = 0;
            _previousCitadelHealth = -1; _previousCitadelMax = 0;
            return;
        }
        int health = Mathf.Clamp(current, 0, maximum);
        float fraction = (float)health / maximum;
        _core.Text = $"CITADEL {health:N0} / {maximum:N0}";
        bool damage = wasVisible && _previousCitadelHealth >= 0 &&
            maximum == _previousCitadelMax && health < _previousCitadelHealth;
        _previousCitadelHealth = health; _previousCitadelMax = maximum;
        if (damage && animate && Config.CitadelBarTweenSeconds > 0f)
        {
            _citadelBarTween = CreateTween();
            _citadelBarTween.TweenProperty(_healthBar, "value", (double)(fraction * 100f),
                Config.CitadelBarTweenSeconds).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        }
        else _healthBar.Value = fraction * 100f;

        _citadelFlashTween?.Kill();
        _core.Modulate = damage && animate ? Config.CitadelDamageFlashColor : Colors.White;
        if (damage && animate && Config.CitadelDamageFlashSeconds > 0f)
        {
            _citadelFlashTween = CreateTween();
            _citadelFlashTween.TweenProperty(_core, "modulate", Colors.White, Config.CitadelDamageFlashSeconds)
                .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        }
        else _core.Modulate = Colors.White;
        _healthFill.BgColor = fraction <= Config.HealthCriticalThreshold ? Config.HealthCriticalColor
            : fraction <= Config.HealthWarningThreshold ? Config.HealthWarningColor : Config.HealthHealthyColor;
    }

    // =========================================================
    // A new wave brightens and grows briefly, but repeated status updates do not restart it.
    private void PulseWave()
    {
        _wavePulseTween?.Kill();
        _wave.PivotOffset = new Vector2(_wave.Size.X, _wave.Size.Y * 0.5f);
        _wave.Scale = Vector2.One * Mathf.Max(1f, Config.WavePulseScale);
        _wave.Modulate = new Color(1.25f, 1.25f, 1.25f, 1f);
        if (Config.WavePulseSeconds <= 0f) { _wave.Scale = Vector2.One; _wave.Modulate = Colors.White; return; }
        _wavePulseTween = CreateTween().SetParallel(true);
        _wavePulseTween.TweenProperty(_wave, "scale", Vector2.One, Config.WavePulseSeconds)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _wavePulseTween.TweenProperty(_wave, "modulate", Colors.White, Config.WavePulseSeconds);
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
        Control slot = GetNode<Control>("BottomArea/TraySlot");
        slot.CustomMinimumSize = new Vector2(0, Config.BottomBarHeight);
        slot.SizeFlagsVertical = SizeFlags.ShrinkEnd;
        _inspector.CustomMinimumSize = new Vector2(Config.InspectorWidth, 0f);
    }

    // =========================================================
    // Remove startup minimum-size offsets after the parent container assigns the tray's bounds.
    private void FitTrayToSlot() => _towerBar.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

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
        LayoutCreditFeedback();
        Control center = status.GetNode<Control>("Citadel");
        center.Position = new Vector2((width - centerWidth) * 0.5f, 0);
        center.Size = new Vector2(centerWidth, height);
        int fontSize = Mathf.Clamp((int)(Config.StatusFontSize * Mathf.Min(1f, width / 900f)), 10, Config.StatusFontSize);
        foreach (Label label in new[] { _credits, _core, _wave }) label.AddThemeFontSizeOverride("font_size", fontSize);
        _creditGain.AddThemeFontSizeOverride("font_size", Mathf.Max(10, fontSize - 2));
        LayoutCreditFeedback();
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

        _upgradeInfo.AddThemeColorOverride("font_color", Config.AccentColor);
        UIButtonFactory.Apply(_upgradeButton, UIButtonStyle.Action, Config);
        UIButtonFactory.Apply(_startWave, UIButtonStyle.Action, Config);
        _healthFill = new StyleBoxFlat { BgColor = Config.HealthHealthyColor };
        _healthBar.AddThemeStyleboxOverride("fill", _healthFill);
        _healthBar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = Config.ButtonColor });

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
        UIButtonFactory.Apply(close, UIButtonStyle.Compact, Config);
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

    #endregion
}
