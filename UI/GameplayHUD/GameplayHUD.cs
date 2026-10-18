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
    [Signal] public delegate void TowerPreviewRequestedEventHandler(string towerId);
    // =========================================================
    // The wave service owns scheduling; this button only requests a manual start.
    [Signal] public delegate void WaveStartRequestedEventHandler();

    // =========================================================
    // The placement/upgrade controller validates the selected tower and payment.
    [Signal] public delegate void TowerUpgradeRequestedEventHandler();
    [Signal] public delegate void TargetPriorityRequestedEventHandler(int priority);
    #endregion

    // =========================================================
    // Closing the build tray also ends any active placement in its owning controller.
    [Signal] public delegate void TowerBarClosedEventHandler();
    [Signal] public delegate void TowerBarOpenedEventHandler();
    [Signal] public delegate void BuildCategoryChangedEventHandler();
    [Signal] public delegate void InspectionClosedEventHandler();

    // An empty reserved shell is visible UI, but is not an active tower inspection.
    public bool IsInspectionVisible => _inspector?.Visible == true && _hasInspectionDetails;

    #region Cached Nodes
    private PanelContainer _topBar, _towerBar, _inspector;
    private HBoxContainer _bottomArea;
    private Control _traySlot;
    private Label _wave, _credits, _core, _towerName, _towerCategory, _towerStats, _towerNotes;
    private Button _startWave, _upgradeButton;
    private Label _upgradeInfo, _runtimeStats, _upgradeTitle, _targetValue;
    private VBoxContainer _targetSection;
    private GridContainer _upgradeComparison;
    private Control _upgradeDivider;
    private readonly Dictionary<TowerTargeting.Priority, Button> _targetButtons = new();
    public Button GetTargetPriorityButton(TowerTargeting.Priority priority) => _targetButtons.GetValueOrDefault(priority);
    private bool _buildPreview, _hasInspectionDetails;
    private Control _inspectorBody;
    private TowerFinalStats _comparisonNow, _comparisonNext;
    private CreditGainIndicator _creditGain;
    private ProgressBar _healthBar, _experienceBar, _towerHealthBar, _towerAmmoBar;
    private Label _towerHealthText, _towerAmmoText;
    private VBoxContainer _defenseMeters;
    private VBoxContainer _rankSection, _workSection, _conditionSection;
    private Label _heatText, _disableReasons;
    private ProgressBar _heatBar;
    private StyleBoxFlat _heatFill;
    private ProgressBar _workBar;
    private Label _workStatus, _workTime;
    private Label _rankLabel, _experienceText, _rankBonuses;
    private CitadelHudRow _citadelRow;
    private Tween _citadelBarTween, _citadelFlashTween, _wavePulseTween;
    private int _targetCredits, _previousCitadelHealth = -1, _previousCitadelMax, _previousWave;
    private double _displayCredits, _creditStart, _creditElapsed;
    private bool _creditsInitialized, _creditCounting, _snapNextCredits;
    private StyleBoxFlat _healthFill;
    private Tween _trayTween;
    private bool _trayOpen;
    private GameInput _input;
    public bool IsTowerBarOpen => _trayOpen;
    public bool IsPlacementPresentation { get; private set; }
    private bool _suppressRestoredHover;
    #endregion

    private readonly Dictionary<string, Button> _buildButtons = new();
    private readonly Dictionary<string, TowerDefinition.TowerCategory> _buildCategories = new();
    private readonly Dictionary<string, Button> _categoryButtons = new();
    public TowerDefinition.TowerCategory? BuildCategory { get; private set; }
    public Button GetBuildCategoryButton(string category) => _categoryButtons.GetValueOrDefault(category);

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
        _inspectorBody = _inspector.GetNode<Control>("Insets/Body");

        _wave = GetNode<Label>("TopBar/Status/Wave");
        _credits = GetNode<Label>("TopBar/Status/Credits");
        SetProcess(false);
        _creditGain = GetNode<CreditGainIndicator>("TopBar/Status/CreditGain");
        _creditGain.Configure(Config);
        _core = GetNode<Label>("TopBar/Status/Citadel/Health");
        _towerName = GetNode<Label>("BottomArea/Inspector/Insets/Body/Heading/TowerName");
        _towerCategory = GetNode<Label>("BottomArea/Inspector/Insets/Body/Columns/Side/TowerCategory");
        _towerStats = GetNode<Label>("BottomArea/Inspector/Insets/Body/Columns/Main/TowerStats");
        _runtimeStats = GetNode<Label>("BottomArea/Inspector/Insets/Body/Columns/Side/RuntimeStats");
        _towerNotes = GetNode<Label>("BottomArea/Inspector/Insets/Body/Columns/Main/Notes");
        BuildTargetChoices();
        _targetSection = GetNode<VBoxContainer>("BottomArea/Inspector/Insets/Body/Columns/Side/TargetSection");
        _targetValue = _targetSection.GetNode<Label>("TargetValue");
        _upgradeTitle = GetNode<Label>("BottomArea/Inspector/Insets/Body/Columns/Main/UpgradeTitle");
        _upgradeDivider = GetNode<Control>("BottomArea/Inspector/Insets/Body/Columns/Main/UpgradeDivider");
        _upgradeComparison = GetNode<GridContainer>("BottomArea/Inspector/Insets/Body/Columns/Main/UpgradeComparison");
        _workSection = GetNode<VBoxContainer>("BottomArea/Inspector/Insets/Body/Heading/WorkSection");
        _workStatus = _workSection.GetNode<Label>("Status"); _workTime = _workSection.GetNode<Label>("Time");
        _workBar = _workSection.GetNode<ProgressBar>("Progress");
        _conditionSection = GetNode<VBoxContainer>("BottomArea/Inspector/Insets/Body/Columns/Side/Conditions");
        _heatText = _conditionSection.GetNode<Label>("HeatText"); _heatBar = _conditionSection.GetNode<ProgressBar>("HeatBar");
        _disableReasons = GetNode<Label>("BottomArea/Inspector/Insets/Body/Columns/Main/DisableReasons");
        _defenseMeters = GetNode<VBoxContainer>("BottomArea/Inspector/Insets/Body/Columns/Side/DefenseMeters");
        _towerHealthText = _defenseMeters.GetNode<Label>("HealthText"); _towerHealthBar = _defenseMeters.GetNode<ProgressBar>("HealthBar");
        _towerAmmoText = _defenseMeters.GetNode<Label>("AmmoText"); _towerAmmoBar = _defenseMeters.GetNode<ProgressBar>("AmmoBar");
        _rankSection = GetNode<VBoxContainer>("BottomArea/Inspector/Insets/Body/Columns/Side/RankSection");
        _rankLabel = _rankSection.GetNode<Label>("Rank");
        _experienceText = _rankSection.GetNode<Label>("ExperienceText");
        _rankBonuses = _rankSection.GetNode<Label>("RankBonuses");
        _experienceBar = _rankSection.GetNode<ProgressBar>("ExperienceBar");

        _startWave = GetNode<Button>("TopBar/Status/StartWave");
        _startWave.Pressed += () => EmitSignal(SignalName.WaveStartRequested);
        _healthBar = GetNode<ProgressBar>("TopBar/Status/Citadel/Bar");
        _topBar.GetNode<Control>("Status").Resized += LayoutStatus;
        _upgradeInfo = GetNode<Label>("BottomArea/Inspector/Insets/Body/Columns/Main/UpgradeInfo");
        _upgradeButton = GetNode<Button>("BottomArea/Inspector/Insets/Body/Columns/Main/UpgradeButton");
        _upgradeButton.Pressed += () => EmitSignal(SignalName.TowerUpgradeRequested);
        Resized += ApplyLayout;
        ApplyLayout();
        ApplyAppearance();
        GetNode<Button>("BottomArea/Inspector/Insets/Body/Heading/CloseButton").Pressed += ClearInspection;
        _inspector.Hide();
        SetTowerBarOpen(Config.StartWithTowerBarOpen, true);
        _input = GetNode<GameInput>("/root/GameInput");
        _input.Bind(this, GameInput.BuildAction, () => { SetTowerBarOpen(!_trayOpen); return true; });
    }
    #endregion

    #region Input and Tray
    // =========================================================
    // Retire scoped controls when this HUD leaves the active battlefield.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_input)) _input.Unbind(this);
    }

    // =========================================================
    // Temporary placement visibility preserves the logical Ctrl-open state and keeps top status usable.
    public void SetPlacementPresentation(bool active)
    {
        if (!active && IsPlacementPresentation) _suppressRestoredHover = true;
        IsPlacementPresentation = active;
        if (active)
        {
            Control focus = GetViewport().GuiGetFocusOwner();
            if (focus != null && _bottomArea.IsAncestorOf(focus)) focus.ReleaseFocus();
            _bottomArea.Hide();
        }
        else { _bottomArea.Show(); _towerBar.Visible = _trayOpen; _inspector.Visible = _trayOpen || _hasInspectionDetails; }
    }

    // =========================================================
    // Restoring a tray beneath a stationary cursor is not a new browsing action after placement/cancel.
    public override void _Input(InputEvent input)
    {
        if (!_suppressRestoredHover || IsPlacementPresentation || input is not InputEventMouseMotion motion || motion.Relative.IsZeroApprox()) return;
        _suppressRestoredHover = false;
        if (!_trayOpen) return;
        Control hovered = GetViewport().GuiGetHoveredControl();
        if (hovered == null) return;
        foreach (var pair in _buildButtons)
            if (pair.Value == hovered || pair.Value.IsAncestorOf(hovered))
            { EmitSignal(SignalName.TowerPreviewRequested, pair.Key); break; }
    }

    // =========================================================
    // Slide/fade the tray without moving the inspector; suppress stale close tweens.
    public void SetTowerBarOpen(bool open, bool immediate = false)
    {
        if (IsPlacementPresentation && !open) immediate = true;
        bool opening = open && !_trayOpen;
        _trayOpen = open;
        if (opening)
        {
            _suppressRestoredHover = true;
            EmitSignal(SignalName.TowerBarOpened); ClearInspection();
        }
        if (!open && !_hasInspectionDetails) _inspector.Hide();
        if (!open) EmitSignal(SignalName.TowerBarClosed);
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
        GridContainer entries = GetNode<GridContainer>("BottomArea/TraySlot/TowerBar/Insets/Tray/Scroll/Buttons");
        foreach (Node child in entries.GetChildren()) { entries.RemoveChild(child); child.QueueFree(); }
        _buildButtons.Clear(); _buildCategories.Clear();
        foreach (TowerDefinition definition in catalog.Definitions)
        {
            if (!definition.Available) continue;
            string id = definition.TowerId;
            Button button = UIButtonFactory.Create(definition.DisplayName + "\n" + definition.Category + " · " +
                TowerPresentation.DescribeResourceCosts(definition.DefaultConfig.ConstructionCosts),
                () => EmitSignal(SignalName.TowerChoiceRequested, id), UIButtonStyle.TowerTile, height: 65f, config: Config);
            button.MouseEntered += () => { if (!IsPlacementPresentation && _trayOpen && !_suppressRestoredHover) EmitSignal(SignalName.TowerPreviewRequested, id); };
            button.FocusEntered += () => { if (!IsPlacementPresentation && _trayOpen) EmitSignal(SignalName.TowerPreviewRequested, id); };
            button.Name = "Tower_" + entries.GetChildCount();
            button.CustomMinimumSize = new Vector2(Config.TowerButtonWidth, 65f);
            button.TooltipText = definition.Description;
            button.Icon = definition.Icon;
            button.ExpandIcon = true;
            button.AddThemeConstantOverride("icon_max_width", 32);
            entries.AddChild(button);
            _buildButtons.Add(id, button); _buildCategories.Add(id, definition.Category);
        }
        if (_buildButtons.Count == 0)
            entries.AddChild(new Label { Text = "No towers available.", MouseFilter = MouseFilterEnum.Ignore });
        RebuildCategories();
    }

    // =========================================================
    // Rebuild only on catalog binding; categories/counts derive from the same available definitions as the tray.
    private void RebuildCategories()
    {
        HBoxContainer row = GetNode<HBoxContainer>("BottomArea/TraySlot/TowerBar/Insets/Tray/CategoryScroll/Categories");
        foreach (Node child in row.GetChildren()) { row.RemoveChild(child); child.QueueFree(); }
        _categoryButtons.Clear();
        AddCategory("All", null, _buildButtons.Count);
        foreach (TowerDefinition.TowerCategory category in Enum.GetValues<TowerDefinition.TowerCategory>())
        {
            int count = 0; foreach (var value in _buildCategories.Values) if (value == category) count++;
            if (count > 0) AddCategory(category.ToString(), category, count);
        }
        if (BuildCategory.HasValue && !_categoryButtons.ContainsKey(BuildCategory.Value.ToString()))
        { BuildCategory = null; EmitSignal(SignalName.BuildCategoryChanged); }
        FilterBuildButtons();
        void AddCategory(string name, TowerDefinition.TowerCategory? category, int count)
        {
            Button tab = UIButtonFactory.Create($"{name} ({count})", () => SelectBuildCategory(category), UIButtonStyle.Compact, 26, Config);
            tab.Name = name; tab.ToggleMode = true; tab.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
            row.AddChild(tab); _categoryButtons.Add(name, tab);
        }
    }

    // =========================================================
    // Category changes retire only pending placement; existing placed-tower inspection stays independent.
    public bool SelectBuildCategory(TowerDefinition.TowerCategory? category)
    {
        if (!_categoryButtons.ContainsKey(category?.ToString() ?? "All")) return false;
        if (BuildCategory == category) { FilterBuildButtons(); return true; }
        BuildCategory = category; EmitSignal(SignalName.BuildCategoryChanged); FilterBuildButtons(); return true;
    }
    // =========================================================
    // Toggle visibility/focus states without rebuilding controls or polling the catalog.
    private void FilterBuildButtons()
    {
        int visible = 0;
        foreach (var entry in _buildButtons)
        {
            entry.Value.Visible = !BuildCategory.HasValue || _buildCategories[entry.Key] == BuildCategory.Value;
            if (entry.Value.Visible) visible++;
        }
        GetNode<GridContainer>("BottomArea/TraySlot/TowerBar/Insets/Tray/Scroll/Buttons").Columns = Math.Max(1, (visible + 1) / 2);
        foreach (var entry in _categoryButtons) entry.Value.SetPressedNoSignal(entry.Key == (BuildCategory?.ToString() ?? "All"));
        GetNode<ScrollContainer>("BottomArea/TraySlot/TowerBar/Insets/Tray/Scroll").ScrollHorizontal = 0;
    }

    // =========================================================
    // Stable ID lookup for keyboard/tool integration independent of generated scene-node names.
    public Button GetBuildButton(string towerId) => _buildButtons.GetValueOrDefault(towerId);

    // =========================================================
    // Placement supplies actual instance details and build previews to the independent Inspector.
    public void ShowTowerDetails(string name, string category, string stats, string notes = "", bool newSelection = false)
    {
        bool preview = category.EndsWith("Build Preview", StringComparison.Ordinal) || category == "Placement rejected";
        _buildPreview = preview;
        SetRuntimeDetails(""); SetWorkDetails(null); SetConditionDetails(null); SetUpgradeComparison(null, null); SetUpgradeDetails("", "", false);
        _targetSection.Hide(); _rankSection.Hide(); _defenseMeters.Hide();
        _towerName.Text = name; _towerName.TooltipText = category + "\n" + notes + "\n" + stats;
        _towerCategory.Text = "";
        _towerStats.Text = stats; _towerNotes.Text = notes;
        RefreshDetailsVisibility(); _hasInspectionDetails = true; _inspectorBody.Show(); _inspector.Show();
    }

    // =========================================================
    // Keep full technical descriptions in tooltips; visible gameplay stats never need a foldout.
    private void RefreshDetailsVisibility()
    {
        _towerNotes.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _towerNotes.Visible = _buildPreview && _towerNotes.Text.Length > 0;
        // Detailed descriptions stay on the header tooltip; stats are never hidden behind a foldout.
        _towerStats.Visible = !_upgradeComparison.Visible && _towerStats.Text.Length > 0;
        _runtimeStats.Visible = _runtimeStats.Text.Length > 0;
    }

    // =========================================================
    // Six mutually-exclusive keyboard-focusable choices; the controller validates all user intent.
    private void BuildTargetChoices()
    {
        GridContainer choices = GetNode<GridContainer>("BottomArea/Inspector/Insets/Body/Columns/Side/TargetSection/Choices");
        ButtonGroup group = new();
        foreach (TowerTargeting.Priority priority in new[] { TowerTargeting.Priority.First, TowerTargeting.Priority.Last,
            TowerTargeting.Priority.Closest, TowerTargeting.Priority.Farthest, TowerTargeting.Priority.Strongest, TowerTargeting.Priority.Weakest })
        {
            var selected = priority;
            Button button = UIButtonFactory.Create(priority.ToString(), () => EmitSignal(SignalName.TargetPriorityRequested, (int)selected),
                UIButtonStyle.Compact, 26, Config);
            button.ToggleMode = true; button.ButtonGroup = group;
            // Compact buttons have smaller margins while keeping readable text and a clear selected fill.
            foreach (string state in new[] { "normal", "hover", "pressed", "focus", "disabled" })
                if (button.GetThemeStylebox(state) is StyleBoxFlat box)
                { box.ContentMarginLeft = box.ContentMarginRight = 5; box.ContentMarginTop = box.ContentMarginBottom = 4; }
            button.TooltipText = priority switch
            {
                TowerTargeting.Priority.First => "Shortest valid estimated remaining journey to the Citadel, including required checkpoints.",
                TowerTargeting.Priority.Last => "Longest valid estimated remaining journey to the Citadel; unavailable routes are excluded.",
                TowerTargeting.Priority.Closest => "Nearest eligible enemy within weapon range.",
                TowerTargeting.Priority.Farthest => "Farthest eligible enemy within weapon range.",
                TowerTargeting.Priority.Strongest => "Most currently remaining hull + shield points.",
                _ => "Fewest currently remaining hull + shield points."
            };
            choices.AddChild(button); _targetButtons.Add(priority, button);
        }
    }

    // =========================================================
    // Bind the six selected-instance choices without emitting another user request.
    public void SetTargetPriorityDetails(string priority)
    {
        _targetSection.Visible = !_buildPreview && priority.Length > 0; _targetValue.Text = priority;
        foreach (var pair in _targetButtons) pair.Value.SetPressedNoSignal(pair.Key.ToString() == priority);
    }

    // =========================================================
    // Update inventory/status only; leave numeric stats, rank progress and upgrade previews untouched.
    public void SetRuntimeDetails(string text)
    { _runtimeStats.Text = text; RefreshDetailsVisibility(); }

    // =========================================================
    // Display actual per-instance HP and ammunition through compact, event-driven meters.
    // Preserve the logistical status line; do not repeat numerical HP/ammo in RuntimeStats.
    public void SetDefenseDetails(TowerBase tower)
    {
        if (!GodotObject.IsInstanceValid(tower) || _buildPreview) { _defenseMeters.Hide(); SetRuntimeDetails(""); return; }
        _defenseMeters.Show();
        _towerHealthText.Text = $"HP: {tower.Health:0.#} / {tower.MaxHealth:0.#}";
        _towerHealthBar.MaxValue = Mathf.Max(1, tower.MaxHealth); _towerHealthBar.Value = tower.Health;
        var stock = tower.Ammunition?.Inventory;
        _towerAmmoText.Visible = _towerAmmoBar.Visible = stock != null;
        if (stock != null)
        {
            _towerAmmoText.Text = $"Ammo: {stock.Amount:N0} / {stock.Capacity:N0}";
            _towerAmmoBar.MaxValue = Mathf.Max(1, stock.Capacity); _towerAmmoBar.Value = stock.Amount;
        }
        string status = tower.DescribeRuntime();
        int separator = status.LastIndexOf('\n');
        SetRuntimeDetails(separator >= 0 ? status[(separator + 1)..] : status);
    }

    // =========================================================
    // Selected-only events update this compact card; never resolve stats or poll every tower/frame.
    public void SetConditionDetails(TowerBase tower)
    {
        bool valid = GodotObject.IsInstanceValid(tower) && !_buildPreview;
        HeatStats? heat = valid ? tower.FinalStats?.Heat : null;
        bool disabled = valid && tower.IsDisabled;
        _conditionSection.Visible = heat.HasValue;
        _heatText.Visible = _heatBar.Visible = heat.HasValue;
        _disableReasons.Visible = disabled;
        if (!valid) return;
        if (heat.HasValue)
        {
            _heatText.Text = $"HEAT: {tower.CurrentHeat:0.#} / {heat.Value.Capacity:0.#}"
                + (tower.IsOverheated ? " · OVERHEATED / COOLING" : disabled && tower.CurrentHeat > 0 ? " · COOLING" : "");
            _heatBar.MaxValue = heat.Value.Capacity; _heatBar.Value = tower.CurrentHeat;
            _heatFill.BgColor = tower.IsOverheated ? Config.HealthCriticalColor
                : tower.CurrentHeat >= heat.Value.Capacity * .75f ? Config.HealthWarningColor : Config.AccentColor;
        }
        if (disabled)
        {
            var text = new System.Text.StringBuilder("DISABLED: ");
            bool first = true;
            foreach (TowerDisableReason reason in Enum.GetValues<TowerDisableReason>())
            {
                int count = 0;
                for (int i = 0; i < tower.DisableCount; i++) if (tower.GetDisableReason(i) == reason) count++;
                if (count == 0) continue;
                if (!first) text.Append(" + "); first = false;
                text.Append(reason == TowerDisableReason.NoPower ? "NO POWER" : reason.ToString().ToUpperInvariant());
                if (count > 1) text.Append(" ×").Append(count);
            }
            _disableReasons.Text = text.ToString(); _disableReasons.TooltipText = _disableReasons.Text;
        }
        if (!tower.IsBusy) _workStatus.Text = disabled ? "DISABLED" : "OPERATIONAL";
    }

    // =========================================================
    // Full current/next thermal numbers remain readable in a tooltip without five more permanent table rows.
    private void SetHeatComparisonTooltip()
    {
        static string Describe(HeatStats value) => $"Capacity: {value.Capacity:0.##} · Heat/shot: {value.PerShot:0.###}"
            + $"\nHeat/beam second: {value.PerBeamSecond:0.###} · Cooling/s: {value.CoolingPerSecond:0.###} · Restart: {value.RestartBelowFraction * 100:0.#}%";
        _conditionSection.TooltipText = _comparisonNow?.Heat is HeatStats current ? "CURRENT\n" + Describe(current) : "";
        if (_comparisonNext?.Heat is HeatStats next) _conditionSection.TooltipText += "\nNEXT LEVEL\n" + Describe(next);
    }

    // =========================================================
    // Work-only updates leave current stats, rank, free previews and scroll position untouched.
    public void SetWorkDetails(TowerBase tower)
    {
        if (!GodotObject.IsInstanceValid(tower)) { _workSection.Hide(); return; }
        _towerName.Text = tower.AuthoredConfig.DisplayName + " · Lv " + tower.UpgradeLevel
            + " (" + tower.CurrentRank.Rank + ")";
        _towerCategory.Text = "Footprint: " + (tower.Definition?.Footprint ?? Vector2I.One).X + "×" + (tower.Definition?.Footprint ?? Vector2I.One).Y;
        _workStatus.Text = tower.WorkPhase switch
        {
            TowerWorkPhase.Constructing => "CONSTRUCTING",
            TowerWorkPhase.Upgrading => $"UPGRADING → {tower.PendingUpgradeLevel}",
            _ => tower.IsDisabled ? "DISABLED" : "OPERATIONAL"
        };
        _workBar.Visible = _workTime.Visible = tower.IsBusy;
        _workBar.Value = tower.WorkProgress * 100;
        _workTime.Text = tower.IsBusy ? $"{tower.RemainingWorkSeconds:0.0}s remaining" : "";
        _workSection.Show();
    }

    // =========================================================
    // Event-driven presentation: fractional progress within the current rank, with total XP also visible.
    public void SetRankDetails(TowerBase tower)
    {
        if (!GodotObject.IsInstanceValid(tower) || tower.FinalStats == null || tower.AuthoredConfig == null || tower.AuthoredConfig.Ranks.Count < 2)
        { _rankSection.Hide(); return; }
        TowerRankDefinition current = tower.CurrentRank;
        TowerRankDefinition? next = tower.NextRank;
        double total = tower.RankProgress.TotalXp;
        _rankLabel.Text = $"Combat Rank: {current.Rank}";
        _towerName.Text = tower.AuthoredConfig.DisplayName + " · Lv " + tower.UpgradeLevel + " (" + current.Rank + ")";
        _experienceText.TooltipText = $"Total combat XP: {total:0.##}. Earned rank is independent of paid tower levels.";
        _rankBonuses.Text = $"Damage +{current.DamageBonus * 100:0.##}% · Range +{current.RangeBonus * 100:0.##}% · HP +{current.HpBonus * 100:0.##}%";
        _experienceBar.MinValue = 0;
        _experienceBar.MaxValue = next.HasValue ? next.Value.TotalXp - current.TotalXp : 1;
        _experienceBar.Value = next.HasValue ? total - current.TotalXp : 1;
        _experienceText.Text = next.HasValue
            ? $"{total - current.TotalXp:0.##} / {next.Value.TotalXp - current.TotalXp:0.##} XP → {next.Value.Rank}"
            : "MAX RANK";
        _rankSection.Show();
    }

    // =========================================================
    // Display an authoritative controller preview; buttons carry no payment or stat logic.
    public void SetUpgradeDetails(string preview, string button, bool affordable)
    {
        bool present = !_buildPreview && (preview.Length > 0 || button.Length > 0);
        _upgradeTitle.Visible = present || _upgradeComparison.Visible;
        _upgradeTitle.Text = (_comparisonNext != null ? "STATS · PAID UPGRADE" : "CURRENT STATS")
            + (_comparisonNow != null ? " · " + (_comparisonNow.Weapon?.Type ?? DamageType.Kinetic).ToString().ToUpperInvariant() : "");
        _upgradeDivider.Hide();
        _upgradeButton.TooltipText = "";
        // Busy status already appears in the header/button; retain the full explanation as a tooltip.
        int extraLine = preview.IndexOf('\n');
        _upgradeInfo.Text = _workSection.Visible && _workBar.Visible && extraLine >= 0 ? preview[..extraLine] : preview;
        _upgradeInfo.TooltipText = preview; _upgradeInfo.Visible = present && preview.Length > 0;
        _upgradeButton.Text = button; _upgradeButton.Visible = present && button.Length > 0;
        _upgradeButton.Disabled = !affordable;
    }

    // =========================================================
    // Numeric cells only rebuild when resolved snapshots change; wallet/progress refreshes reuse them.
    public void SetUpgradeComparison(TowerFinalStats current, TowerFinalStats next)
    {
        if (Equals(_comparisonNow, current) && Equals(_comparisonNext, next)) return;
        _comparisonNow = current; _comparisonNext = next; SetHeatComparisonTooltip();
        foreach (Node child in _upgradeComparison.GetChildren())
        { _upgradeComparison.RemoveChild(child); child.QueueFree(); }
        bool hasNext = next != null;
        var rows = TowerUpgradeComparison.Build(current, next ?? current);
        _upgradeComparison.Columns = hasNext ? 4 : 2;
        _upgradeComparison.Visible = !_buildPreview && rows.Count > 0;
        RefreshDetailsVisibility();
        if (rows.Count == 0) return;
        AddCell("STAT", true, true, Config.MutedTextColor);
        AddCell("CURRENT", false, true, Config.MutedTextColor);
        if (hasNext) { AddCell("NEXT", false, true, Config.MutedTextColor); AddCell("GAIN", false, true, Config.MutedTextColor); }
        foreach (TowerUpgradeRow row in rows)
        {
            AddCell(row.Name, true, false, Config.TextColor);
            AddCell(row.Current, false, false, Config.MutedTextColor);
            if (!hasNext) continue;
            AddCell(row.Next, false, false, Config.TextColor);
            AddCell(row.Gain, false, false, row.Tone switch
            {
                TowerUpgradeGain.Better => Config.CreditRewardColor,
                TowerUpgradeGain.Worse => Config.HealthCriticalColor,
                _ => Config.MutedTextColor
            });
        }
    }

    // =========================================================
    // Only stat names absorb spare width; numeric columns stay content-sized and easy to compare.
    private void AddCell(string value, bool left, bool header, Color color)
    {
        Label cell = UIButtonFactory.Label(value, header ? 10 : 12, Config);
        cell.HorizontalAlignment = left ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        cell.SizeFlagsHorizontal = left ? SizeFlags.ExpandFill : SizeFlags.Fill;
        cell.AddThemeColorOverride("font_color", color);
        _upgradeComparison.AddChild(cell);
    }

    // =========================================================
    // Keep a refreshed retry button available while reporting rejected upgrade attempts.
    public void SetUpgradeError(string message) => _upgradeInfo.Text = message + "\n" + _upgradeInfo.Text;

    // =========================================================
    // Retire details while keeping the reserved blank shell visible beside an open build tray.
    public void ClearInspection()
    {
        bool hadDetails = _hasInspectionDetails; _hasInspectionDetails = false;
        _inspectorBody.Hide(); _inspector.Visible = _trayOpen && !IsPlacementPresentation;
        if (hadDetails) EmitSignal(SignalName.InspectionClosed);
    }
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
    // Multi-objective presentation has one horizontal row; legacy single-Citadel nodes/APIs remain intact.
    public void BindCitadels(System.Collections.Generic.IReadOnlyList<CitadelDestination> objectives)
    {
        if (_citadelRow != null) { _citadelRow.Free(); _citadelRow = null; }
        _topBar.GetNode<Control>("Status/Citadel").Visible = objectives.Count <= 1;
        if (objectives.Count <= 1) { LayoutStatus(); return; }
        _citadelRow = new CitadelHudRow { Name = "CitadelRow" };
        _topBar.GetNode("Status").AddChild(_citadelRow); _citadelRow.Configure(Config, objectives);
        _topBar.GetNode<Control>("Status/Citadel").Hide(); LayoutStatus();
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
            _citadelFlashTween?.Kill();
            _core.Modulate = Colors.White;
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
        float inspectorWidth = Mathf.Min(Config.InspectorWidth, Mathf.Max(200, Size.X - Config.EdgeMargin * 2));
        float trayWidth = Mathf.Max(0, Size.X - Config.EdgeMargin * 2 - inspectorWidth - _bottomArea.GetThemeConstant("separation"));
        slot.SizeFlagsHorizontal = SizeFlags.Fill;
        slot.CustomMinimumSize = new Vector2(trayWidth, Config.BottomBarHeight);
        GetNode<Control>("BottomArea/TraySlot/TowerBar/Insets/Tray/Header/Hint").Visible = trayWidth >= 300;
        slot.SizeFlagsVertical = SizeFlags.ShrinkEnd;
        _inspector.CustomMinimumSize = new Vector2(Mathf.Min(Config.InspectorWidth, Mathf.Max(200, Size.X - Config.EdgeMargin * 2)),
            Mathf.Min(Config.InspectorHeight, Mathf.Max(180, Size.Y - Config.TopBarHeight - Config.EdgeMargin * 3 - 40)));
        _inspector.SizeFlagsVertical = SizeFlags.ShrinkEnd;
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
        float centerWidth = Mathf.Min(_citadelRow == null ? Config.CitadelBlockWidth : Config.MultiCitadelBlockWidth, width * 0.46f);
        float sideWidth = Mathf.Max(0f, (width - centerWidth) * 0.5f - margin - Config.StatusGapPixels);
        _credits.Position = new Vector2(margin, 0); _credits.Size = new Vector2(sideWidth, height);
        _wave.Position = new Vector2(width - margin - sideWidth, 0); _wave.Size = new Vector2(sideWidth, height);
        LayoutCreditFeedback();
        Control center = status.GetNode<Control>("Citadel");
        center.Position = new Vector2((width - centerWidth) * 0.5f, 0);
        center.Size = new Vector2(centerWidth, height);
        if (_citadelRow != null) { _citadelRow.Position = center.Position; _citadelRow.Size = center.Size; }
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
        Color background = Config.PanelColor; background.A *= Mathf.Clamp(Config.BottomPanelOpacity, .5f, 1f);
        _towerBar.AddThemeStyleboxOverride("panel", MakePanel(background, Config.AccentColor));
        StyleBoxFlat inspection = MakePanel(background, Config.AccentColor);
        inspection.ContentMarginLeft = inspection.ContentMarginRight = 12;
        inspection.ContentMarginTop = inspection.ContentMarginBottom = 10;
        _inspector.AddThemeStyleboxOverride("panel", inspection);

        _upgradeInfo.AddThemeColorOverride("font_color", Config.MutedTextColor);
        _upgradeTitle.AddThemeColorOverride("font_color", Config.AccentColor);
        _targetValue.AddThemeColorOverride("font_color", Config.TextColor);
        _targetSection.GetNode<Label>("TargetHeading").AddThemeColorOverride("font_color", Config.AccentColor);

        UIButtonFactory.Apply(_upgradeButton, UIButtonStyle.Action, Config);
        UIButtonFactory.Apply(_startWave, UIButtonStyle.Action, Config);
        _healthFill = new StyleBoxFlat { BgColor = Config.HealthHealthyColor };
        _healthBar.AddThemeStyleboxOverride("fill", _healthFill);
        _healthBar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = Config.ButtonColor });

        Label[] primary = { _wave, _credits, _core, _towerName };
        foreach (Label label in primary) label.AddThemeColorOverride("font_color", Config.TextColor);

        _towerHealthBar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = Config.HealthHealthyColor });
        _towerHealthBar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = Config.ButtonColor });
        _towerAmmoBar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = Config.AccentColor });
        _towerAmmoBar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = Config.ButtonColor });
        _towerHealthText.AddThemeColorOverride("font_color", Config.MutedTextColor);
        _towerAmmoText.AddThemeColorOverride("font_color", Config.MutedTextColor);
        _experienceBar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = Config.AccentColor });
        _experienceBar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = Config.ButtonColor });
        _workBar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = Config.AccentColor });
        _workBar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = Config.ButtonColor });
        _heatFill = new StyleBoxFlat { BgColor = Config.AccentColor };
        _heatBar.AddThemeStyleboxOverride("fill", _heatFill);
        _heatBar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = Config.ButtonColor });
        _heatText.AddThemeColorOverride("font_color", Config.MutedTextColor);
        _disableReasons.AddThemeColorOverride("font_color", Config.HealthCriticalColor);
        _rankLabel.AddThemeColorOverride("font_color", Config.AccentColor);
        Label[] muted = { _towerCategory, _towerStats, _towerNotes, _experienceText, _rankBonuses,
            GetNode<Label>("BottomArea/TraySlot/TowerBar/Insets/Tray/Header/Hint") };
        foreach (Label label in muted) label.AddThemeColorOverride("font_color", Config.MutedTextColor);

        Label[] accents = {
            GetNode<Label>("BottomArea/TraySlot/TowerBar/Insets/Tray/Header/Title") };
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

