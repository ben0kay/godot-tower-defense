// Catalog-driven tower build mode: ghost, grid validation and selection.
// WorldGrid owns occupancy; the local dictionary only finds/selects tower instances.
using Godot;
using System.Collections.Generic;
using System;

public partial class TowerPlacementController : Node2D
{
    public event Action<TowerBase> TowerPlaced;
    [Signal] public delegate void BuildDeniedEventHandler(string towerId, string reason);
    private bool _browsingBuild;
    private ulong _ignorePreviewFrame = ulong.MaxValue, _placedInputFrame = ulong.MaxValue;

    #region Inspector
    [ExportGroup("Global Placement")]
    [Export] public TowerMasterConfig Settings { get; set; }

    [ExportGroup("Buildable Types")]
    [Export] public TowerCatalog Catalog { get; set; }
    #endregion

    #region State
    private TowerDefinition _buildDefinition;
    private EconomyManager _economy;
    private bool _affordable, _previewDirty;
    private string _previewFailure;
    private string _buildPriceFailure;
    private WorldGrid _grid;
    private GameplayHUD _hud;
    private SpawnManager _spawns;
    private Node2D _towers;
    private ConstructionWorkManager _construction;
    private ConstructionDroneManager _builders;
    private SalvageLogistics _salvage;
    private TowerPlacementGhost _ghost;
    private readonly Dictionary<Vector2I, TowerBase> _placedTowers = new();
    private Vector2I _hoverCell = new(int.MinValue, int.MinValue);
    private GameInput _input;
    public bool IsPlacing => _placing;
    public string PreviewFailure => _previewFailure ?? "";
    private bool _placing, _previewVisible, _previewValid, _rightShiftHeld;
    private readonly Dictionary<TowerBase, Action> _placementExits = new();
    private TowerBase _selected;
    private TowerSelectionOverlay _selectionOverlay;
    private TowerRangeOverlay _rangeOverlay;
    private int _displayedUpgradeLevel;
    private ulong _lastUpgradeFrame = ulong.MaxValue;
    #endregion

    #region Lifecycle
    // =========================================================
    // One placement ghost; the world and HUD continue to own their separate responsibilities.
    public override void _Ready()
    {
        SetProcess(false);
        SetProcessUnhandledInput(false);
        if (Catalog == null)
        {
            Catalog = GD.Load<TowerCatalog>("res://TOWERS/DefaultTowerCatalog.tres");
            if (Catalog == null)
                throw new InvalidOperationException("Tower placement could not load res://TOWERS/DefaultTowerCatalog.tres. Check the resource/import errors above.");
            GD.PushWarning("TowerPlacement had no Catalog resource; loaded DefaultTowerCatalog automatically.");
        }
        _economy = GetNode<EconomyManager>("../Economy");
        _grid = GetNode<WorldGrid>("../World/Grid");
        _spawns = GetNode<SpawnManager>("../World/EnemySystems/Spawning");
        _towers = GetNode<Node2D>("../World/Towers");
        _construction = GetNode<ConstructionWorkManager>("../World/ConstructionWork");
        _builders = GetNodeOrNull<ConstructionDroneManager>("../World/ConstructionDrones"); _builders?.Bind(this);
        _hud = GetNode<GameplayHUD>("../UI/GameplayHUD");
        _salvage = GetNodeOrNull<SalvageLogistics>("../World/Salvage");
        if (_salvage != null) { _salvage.StockChanged += OnMetalStockChanged; OnMetalStockChanged(); }
        _selectionOverlay = new TowerSelectionOverlay { Name = "SelectedTowerOverlay" };
        AddChild(_selectionOverlay);
        _selectionOverlay.Configure(_grid, _hud);
        _rangeOverlay = new TowerRangeOverlay { Name = "SelectedTowerRange", ZIndex = 9 };
        AddChild(_rangeOverlay); _rangeOverlay.Configure(_grid, Settings);
        BindPowerLinkActions();
        _hud.TowerSellRequested += OnSellRequested; _hud.TowerSellConfirmed += OnSellConfirmed;
        Catalog.Rebuild();
        _hud.BindCatalog(Catalog);
        foreach (TowerDefinition definition in Catalog.Definitions)
            if (definition.Available) { _buildDefinition = definition; break; }
        if (_buildDefinition?.DefaultConfig != null) _buildDefinition.DefaultConfig.Changed += OnPreviewConfigChanged;
        _ghost = new TowerPlacementGhost
        {
            Name = "PlacementGhost", Visible = false, ZIndex = 10,
            Modulate = new Color(0.45f, 1f, 0.73f, 0.66f)
        };
        AddChild(_ghost);
        _ghost.LoadFinishedLevelOne(_buildDefinition);
        _economy.BalanceChanged += OnBalanceChanged;
        _hud.TowerChoiceRequested += OnTowerChoiceRequested; _hud.TowerPreviewRequested += OnTowerPreviewRequested;
        _hud.TowerBarOpened += OnBuildTrayOpened; _hud.TowerBarClosed += CancelPlacement; _hud.BuildCategoryChanged += CancelPlacement; _hud.InspectionClosed += OnInspectionClosed;
        _hud.TowerUpgradeRequested += OnUpgradeRequested; _hud.TargetPriorityRequested += OnTargetPriorityRequested;
        SetProcessUnhandledInput(true);
        _input = GetNode<GameInput>("/root/GameInput");
        _input.Bind(this, GameInput.BackAction, () =>
        { if (_hud.CancelFighterPatrolEdit() || CancelPowerLink()) return true; if (!_placing && !_browsingBuild) return false; CancelPlacement(); return true; }, 10);
    }

    // =========================================================
    // Clear a held modifier when the window loses focus before key-up arrives.
    public override void _Notification(int what)
    { if (what == NotificationWMWindowFocusOut || what == NotificationApplicationFocusOut) _rightShiftHeld = false;
      if (what == NotificationPaused) InvalidateSale(); }

    // =========================================================
    // Avoid subscriptions retaining scenes that are no longer alive.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_salvage)) _salvage.StockChanged -= OnMetalStockChanged;
        InvalidateSale();
        if (GodotObject.IsInstanceValid(_hud)) { _hud.TowerSellRequested -= OnSellRequested; _hud.TowerSellConfirmed -= OnSellConfirmed; }
        UnbindPowerLinkActions();
        ObserveWork(null);
        foreach (var pair in _placementExits) if (GodotObject.IsInstanceValid(pair.Key)) pair.Key.TreeExiting -= pair.Value;
        _placementExits.Clear();
        if (GodotObject.IsInstanceValid(_input)) _input.Unbind(this);
        if (GodotObject.IsInstanceValid(_hud))
        { _hud.TowerBarOpened -= OnBuildTrayOpened; _hud.TowerBarClosed -= CancelPlacement; _hud.BuildCategoryChanged -= CancelPlacement; _hud.InspectionClosed -= OnInspectionClosed; _hud.TowerChoiceRequested -= OnTowerChoiceRequested; _hud.TowerPreviewRequested -= OnTowerPreviewRequested; _hud.TowerUpgradeRequested -= OnUpgradeRequested; _hud.TargetPriorityRequested -= OnTargetPriorityRequested; }
        if (GodotObject.IsInstanceValid(_economy)) _economy.BalanceChanged -= OnBalanceChanged;
        if (GodotObject.IsInstanceValid(_buildDefinition?.DefaultConfig)) _buildDefinition.DefaultConfig.Changed -= OnPreviewConfigChanged;
        if (GodotObject.IsInstanceValid(_selected))
        { _selected.StatsChanged -= OnSelectedStatsChanged; _selected.TargetPriorityChanged -= OnSelectedTargetPriorityChanged; _selected.ExperienceChanged -= OnSelectedExperienceChanged; _selected.RuntimeChanged -= OnSelectedRuntimeChanged; _selected.HeatChanged -= OnSelectedConditionsChanged; _selected.DisableStateChanged -= OnSelectedConditionsChanged; }
    }
    #endregion

    #region Build Mode
    // =========================================================
    // Ctrl starts a fresh blank browser and detaches old instance events before the shell becomes visible.
    private void OnBuildTrayOpened() { CancelPlacement(); SelectTower(null); }

    // =========================================================
    // Browsing is mouse-hover/keyboard-focus only; it never arms a ghost or spends resources.
    private void OnTowerPreviewRequested(string id)
    {
        if (_placing || GetTree().Paused || !_hud.IsTowerBarOpen || Engine.GetProcessFrames() == _ignorePreviewFrame) return;
        PreviewBuild(id);
    }

    // =========================================================
    // One shared definition/affordability view for hover, focus and rejected clicks.
    private bool PreviewBuild(string id)
    {
        TowerDefinition choice = Catalog.Find(id);
        if (choice == null || !choice.Available) return false;
        if (GodotObject.IsInstanceValid(_buildDefinition?.DefaultConfig)) _buildDefinition.DefaultConfig.Changed -= OnPreviewConfigChanged;
        _buildDefinition = choice; choice.DefaultConfig.Changed += OnPreviewConfigChanged;
        _ghost.LoadFinishedLevelOne(choice); SelectTower(null); _browsingBuild = true;
        RefreshBuildAffordability();
        RefreshBuildDetails(); return true;
    }

    // =========================================================
    // Basic definition/cost/affordability belongs in the existing right-hand Inspector while browsing.
    private void RefreshBuildDetails()
    {
        if (!_browsingBuild || _placing || _buildDefinition == null) return;
        TowerConfig config = _buildDefinition.DefaultConfig;
        TowerFinalStats final = ResolveBuildStats(config);
        string info = _buildDefinition.Description + "\n\n" +
            $"Cost: {TowerPresentation.DescribeResourceCosts(config.ConstructionCosts)}\n" +
            $"Build time: {config.BuildSeconds:0.#} s\nFootprint: {_buildDefinition.Footprint.X} x {_buildDefinition.Footprint.Y}";
        if (final != null)
        {
            info += final.Beam.HasValue ? $"\nDamage: {final.Beam.Value.DamagePerSecond:0.##} DPS" :
                final.Shockwave.HasValue ? $"\nDamage: {final.Shockwave.Value.DamagePerWave:0.##} per wave" : $"\nDamage: {final.DamagePerShot}";
            info += $"\nRange: {final.RangePixels:0.#} px";
            if (final.ShotsPerSecond > 0) info += $"\nRate: {final.ShotsPerSecond:0.##} shots/s";
        }
        _hud.ShowTowerDetails(_buildDefinition.DisplayName, _buildDefinition.Category + " / Build Preview", info,
            _affordable ? "CAN AFFORD — Click to place." : BuildPriceFailure());
    }

    // =========================================================
    // An affordable click arms one placement and clears the lower UI; unaffordable clicks retain the preview.
    private void OnTowerChoiceRequested(string id)
    {
        if (GetTree().Paused || _placing) return;
        if (!PreviewBuild(id)) { CancelPlacement(); SelectTower(null); _hud.ClearInspection(); return; }
        if (!_affordable)
        { EmitSignal(SignalName.BuildDenied, id, BuildPriceFailure()); GetViewport().SetInputAsHandled(); return; }
        _browsingBuild = false; _placing = true; _previewDirty = true;
        _hud.ClearInspection(); _hud.SetPlacementPresentation(true);
        SetProcess(true); UpdatePreview(); GetViewport().SetInputAsHandled();
    }

    // =========================================================
    // Poll cursor position only during placement, stopping immediately on cancellation.
    public override void _Process(double delta) => UpdatePreview();

    // =========================================================
    // Cursor preview performs only cheap full-footprint terrain/occupancy checks; route safety is click-only.
    private void UpdatePreview()
    {
        Vector2I cell = _grid.WorldToCell(GetGlobalMousePosition());
        RefreshPreview(cell, _placing && !IsOverUi() && _grid.IsInsideMap(cell));
    }

    // =========================================================
    // Cell-based presentation shares full-rectangle validation and can be verified without a desktop mouse.
    private void RefreshPreview(Vector2I cell, bool visible)
    {
        string failure = _grid.PlacementFailure(cell, _buildDefinition.Footprint);
        if (failure.Length == 0 && !_affordable) failure = _buildPriceFailure ?? "Insufficient resources.";
        bool valid = visible && _affordable && failure.Length == 0;
        if (!_previewDirty && _previewVisible == visible && _previewValid == valid && _previewFailure == failure && (!visible || cell == _hoverCell))
            return;

        _previewDirty = false; _previewFailure = failure;
        _previewVisible = visible;
        _previewValid = valid;
        _hoverCell = cell;
        _ghost.Visible = visible && Settings.ShowPlacementPreview;
        if (visible)
        {
            _ghost.GlobalPosition = _grid.FootprintCenter(cell, _buildDefinition.Footprint);
            // Keep the finished scene's native proportions, including multi-cell scales and long barrels.
            _ghost.Modulate = valid ? new Color(0.45f, 1f, 0.73f, 0.66f)
                : new Color(1f, 0.35f, 0.31f, 0.56f);
        }
        QueueRedraw();
    }

    // =========================================================
    // Clear build previews without closing the tray or a placed-tower Inspector.
    private void CancelPlacement()
    {
        CancelPowerLink();
        bool clearPreview = _placing || _browsingBuild;
        if (clearPreview || _hud.IsPlacementPresentation) _ignorePreviewFrame = Engine.GetProcessFrames();
        _placing = false; _browsingBuild = false;
        if (clearPreview && _selected == null) _hud.ClearInspection();
        _hud.SetPlacementPresentation(false);
        _previewVisible = false;
        _previewValid = false;
        if (_ghost != null) _ghost.Hide();
        SetProcess(false);
        QueueRedraw();
    }
    #endregion

    #region World Input
    // =========================================================
    // Track physical Right Shift only; Left Shift must never enable repeat placement.
    // RMB still cancels over the build tray or Inspector.
    public override void _Input(InputEvent input)
    {
        if (input is InputEventKey key && !key.Echo && key.Location == KeyLocation.Right &&
            (key.PhysicalKeycode == Key.Shift || key.Keycode == Key.Shift))
        { _rightShiftHeld = key.Pressed; return; }
        if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right } && (_hud.CancelFighterPatrolEdit() || CancelPowerLink()))
        { GetViewport().SetInputAsHandled(); return; }
        if ((!_placing && !_browsingBuild) || input is not InputEventMouseButton mouse || !mouse.Pressed
            || mouse.ButtonIndex != MouseButton.Right) return;
        CancelPlacement();
        GetViewport().SetInputAsHandled();
    }

    // =========================================================
    // Unhandled world clicks only; GUI panels consume mouse events independently.
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventMouseMotion)
        {
            _hud.UpdateFighterPatrolPreview(!_placing && !IsOverUi() ? GetGlobalMousePosition() : null);
            return;
        }
        if (input is not InputEventMouseButton mouse || !mouse.Pressed || IsOverUi()) return;
        if (mouse.ButtonIndex != MouseButton.Left) return;

        Vector2 point = GetGlobalMousePosition();
        if (_placing || !_hud.HandleFighterPatrolClick(point)) HandleWorldClick(_grid.WorldToCell(point));
        GetViewport().SetInputAsHandled();
    }

    // =========================================================
    // One placement press cannot also inspect; a later independent click uses the normal selection path.
    private void HandleWorldClick(Vector2I cell)
    {
        if (TryPowerLinkClick(cell)) return;
        if (Engine.GetProcessFrames() == _placedInputFrame) return;
        if (_placing) TryPlace(cell);
        else if (TryCollectCitadelAt(cell)) return;
        else if (_placedTowers.TryGetValue(cell, out TowerBase tower)) SelectTower(tower);
        else { SelectTower(null); _browsingBuild = false; _hud.ClearInspection(); }
    }

    // =========================================================
    // A physical Citadel footprint is one click target for its own Credits, never another objective's.
    // Return true even when empty/full wallet so the click cannot also select/clear a tower.
    public bool TryCollectCitadelAt(Vector2I cell)
    {
        if (_placing) return false;
        CitadelSession session = GetNode<CitadelSession>("../CitadelSession");
        foreach (CitadelDestination destination in session.Objectives)
        {
            if (!destination.Available || destination.Root is not WorldObjectFootprint footprint) continue;
            Vector2I origin = _grid.WorldToCell(footprint.GlobalPosition);
            if (!new Rect2I(origin, footprint.SizeCells).HasPoint(cell)) continue;
            session.TryCollectCredits(destination);
            return true;
        }
        return false;
    }

    // =========================================================
    // Validate the entire proposed building before the single authoritative purchase transaction.
    private void TryPlace(Vector2I cell)
    {
        if (_buildDefinition == null || !_buildDefinition.Available) return;
        string priceFailure = BuildPriceFailure();
        if (priceFailure.Length != 0) { ShowPlacementFailure(priceFailure); return; }
        string failure = _spawns.PlacementFailure(cell, _buildDefinition.Footprint);
        if (failure.Length != 0)
        {
            ShowPlacementFailure(failure);
            return;
        }
        TowerBase tower = null;
        var wallet = WalletRecipe(out int metal);
        if (!_economy.TryPurchase(wallet, () => CommitTower(cell, metal, out tower), paid => tower.RecordCreditsPaid(paid)))
        {
            ShowPlacementFailure(BuildPriceFailure().Length == 0
                ? "Tower creation failed; no resources charged." : BuildPriceFailure());
            return;
        }
        if (metal > 0) _salvage.PublishReservation(tower);
        TowerPlaced?.Invoke(tower);
        _placedInputFrame = Engine.GetProcessFrames();
        if (_rightShiftHeld)
        {
            // One committed tower per click; preserve the current catalog type and ghost.
            RefreshBuildAffordability();
            _previewDirty = true; UpdatePreview();
            return;
        }
        CancelPlacement(); SelectTower(null); _hud.ClearInspection();
    }

    // =========================================================
    // Failed repeated builds must preserve the selected tower's rank/runtime and paid upgrade preview.
    private void ShowPlacementFailure(string failure)
    {
        if (GodotObject.IsInstanceValid(_selected)) _hud.SetUpgradeError(failure);
        else if (!_placing) _hud.ShowTowerDetails(_buildDefinition.DisplayName, "Placement rejected", DescribeTower(), failure);
        EmitSignal(SignalName.BuildDenied, _buildDefinition.TowerId, failure);
    }

    // =========================================================
    // Roll back occupancy and the scene on creation failure, before the wallet deducts payment.
    private bool CommitTower(Vector2I cell, int metal, out TowerBase tower)
    {
        tower = null;
        bool occupied = false;
        try
        {
            if (_buildDefinition.Scene == null) return false;
            Node instance = _buildDefinition.Scene.Instantiate();
            if (instance is not TowerBase created) { instance.Free(); return false; }
            tower = created; // Track ownership before any preflight can throw.
            if (!created.AcceptsConfig(_buildDefinition.DefaultConfig) || !created.ValidateScene())
            { created.Free(); tower = null; return false; }
            tower.InitializeDefinition(_buildDefinition, Settings,
                GetNode<CitadelSession>("../CitadelSession").Research.ForTower(_buildDefinition.TowerId));
            TowerUpgradeController.Validate(tower.Levels); TowerConfig.ValidateWorkSeconds(tower.AuthoredConfig.BuildSeconds);
            if (tower.Levels.Count > 0) tower.ResolveLevel(1); // Fail invalid inputs before occupancy/Ready/payment.
            if (!_grid.TryOccupy(cell, _buildDefinition.Footprint)) { tower.Free(); tower = null; return false; }
            occupied = true;
            Vector2 worldCenter = _grid.FootprintCenter(cell, _buildDefinition.Footprint);
            if (metal > 0 && !_salvage.ReserveConstruction(tower, metal, worldCenter))
                throw new InvalidOperationException("Material reservation failed.");
            tower.StartConstruction(_construction, GodotObject.IsInstanceValid(_builders) && _builders.CanGate ? _builders : null);
            tower.Position = _towers.ToLocal(_grid.FootprintCenter(cell, _buildDefinition.Footprint));
            _towers.AddChild(tower);
            tower.SetPlacement(cell, _grid.FootprintCenter(cell, _buildDefinition.Footprint));
            if (tower.GetParent() != _towers) throw new InvalidOperationException("Tower did not enter the world.");
            Vector2I footprint = _buildDefinition.Footprint;
            for (int y = 0; y < footprint.Y; y++) for (int x = 0; x < footprint.X; x++)
                _placedTowers.Add(cell + new Vector2I(x, y), tower);
            TowerBase placed = tower;
            Action exit = () => RemovePlacement(placed, cell, footprint);
            _placementExits.Add(placed, exit); placed.TreeExiting += exit;
            return true;
        }
        catch (Exception exception)
        {
            if (GodotObject.IsInstanceValid(tower))
            {
                if (metal > 0) _salvage.RollbackConstruction(tower);
                tower.CancelConstruction();
            }
            if (tower != null) RemovePlacement(tower, cell, _buildDefinition.Footprint);
            if (GodotObject.IsInstanceValid(tower)) tower.Free();
            if (occupied) _grid.ReleaseOccupancy(cell, _buildDefinition.Footprint);
            tower = null;
            GD.PushWarning("Tower creation rolled back: " + exception.Message);
            return false;
        }
    }

    // =========================================================
    // Indexed any-cell selection and exact claim release also cover external removal and failed creation.
    public TowerBase TowerAt(Vector2I cell) => _placedTowers.GetValueOrDefault(cell);
    private void RemovePlacement(TowerBase tower, Vector2I anchor, Vector2I footprint)
    {
        for (int y = 0; y < footprint.Y; y++) for (int x = 0; x < footprint.X; x++)
        {
            Vector2I cell = anchor + new Vector2I(x, y);
            if (_placedTowers.TryGetValue(cell, out TowerBase owner) && owner == tower) _placedTowers.Remove(cell);
        }
        if (_placementExits.Remove(tower, out Action exit) && GodotObject.IsInstanceValid(tower)) tower.TreeExiting -= exit;
        if (_selected == tower) { SelectTower(null); _hud.ClearInspection(); }
        if (GodotObject.IsInstanceValid(_grid)) _grid.ReleaseOccupancy(anchor, footprint);
        _previewDirty = true;
    }

    private Godot.Collections.Array<ResourceCost> WalletRecipe(out int metal)
    {
        var recipe = _buildDefinition.DefaultConfig.ConstructionCosts;
        metal = 0;
        Godot.Collections.Array<ResourceCost> wallet = null;
        foreach (ResourceCost cost in recipe)
        {
            if (!SalvageLogistics.IsMetal(cost)) continue;
            metal = (int)Math.Min(int.MaxValue, (long)metal + Math.Max(0, cost.Amount));
            wallet ??= new Godot.Collections.Array<ResourceCost>();
        }
        if (wallet == null) return recipe;
        foreach (ResourceCost cost in recipe) if (!SalvageLogistics.IsMetal(cost)) wallet.Add(cost);
        return wallet;
    }

    private string BuildPriceFailure()
    {
        var wallet = WalletRecipe(out int metal);
        if (metal > 0)
        {
            string shortage = _salvage?.PlacementFailure(metal) ?? "Material logistics unavailable.";
            if (shortage.Length != 0) return shortage;
        }
        return _economy.CanAfford(wallet) ? "" : "Insufficient resources.";
    }

    private void RefreshBuildAffordability()
    {
        _buildPriceFailure = _buildDefinition == null ? "No building selected." : BuildPriceFailure();
        _affordable = _buildPriceFailure.Length == 0;
    }

    private void OnMetalStockChanged()
    {
        _hud.SetStoredMetal(_salvage.StoredMetal);
        if (_buildDefinition != null) OnBalanceChanged(SalvageLogistics.MetalId, 0);
    }

    // =========================================================
    // Refresh affordability only when wallet state changes, not on every preview frame.
    private void OnBalanceChanged(string resourceId, int amount)
    {
        RefreshBuildAffordability();
        _previewDirty = true;
        if (_browsingBuild) RefreshBuildDetails();
        else if (_hud.IsInspectionVisible) RefreshUpgradePreview();
    }

    // =========================================================
    // A selected placed tower shares the existing HUD Inspector with build previews.
    private void SelectTower(TowerBase tower)
    {
        if (tower != _selected) { InvalidateSale(); CancelPowerLink(); }
        if (_selected == tower && (tower == null || _hud.IsInspectionVisible)) return;
        if (GodotObject.IsInstanceValid(_selected))
        { _selected.StatsChanged -= OnSelectedStatsChanged; _selected.TargetPriorityChanged -= OnSelectedTargetPriorityChanged; _selected.ExperienceChanged -= OnSelectedExperienceChanged; _selected.RuntimeChanged -= OnSelectedRuntimeChanged; _selected.HeatChanged -= OnSelectedConditionsChanged; _selected.DisableStateChanged -= OnSelectedConditionsChanged; _selected.SetSelected(false); }
        _selected = tower; ObserveWork(tower);
        if (tower != null) _browsingBuild = false;
        if (GodotObject.IsInstanceValid(_selected))
        {
            _selected.StatsChanged += OnSelectedStatsChanged; _selected.TargetPriorityChanged += OnSelectedTargetPriorityChanged;
            _selected.ExperienceChanged += OnSelectedExperienceChanged; _selected.RuntimeChanged += OnSelectedRuntimeChanged; _selected.HeatChanged += OnSelectedConditionsChanged; _selected.DisableStateChanged += OnSelectedConditionsChanged;
            _selected.SetSelected(true);
            _hud.ShowTowerDetails(_selected.AuthoredConfig.DisplayName, (_selected.Definition?.Category.ToString() ?? "Tower") + " / Placed",
                DescribeTower($"Grid cell: {_selected.GridCell.X}, {_selected.GridCell.Y}"),
                _selected.AuthoredConfig.Description + "\nConstruction paid.", newSelection: true);
            _hud.SetTargetPriorityDetails(_selected.AuthoredConfig.InspectorTargetPriorityLabel);
            _hud.SetRankDetails(_selected); _hud.SetDefenseDetails(_selected); _hud.SetWorkDetails(_selected); _hud.SetConditionDetails(_selected);
        }
        RefreshUpgradePreview();
        if (GodotObject.IsInstanceValid(_selected)) _selectionOverlay?.ShowTower(_selected);
        else _selectionOverlay?.Clear();
        _rangeOverlay?.Present(_selected);
        QueueRedraw();
    }

    // =========================================================
    // Refresh the selected tower's displayed effective values/range when its inputs change.
    private void OnSelectedTargetPriorityChanged()
    { if (_hud.IsInspectionVisible && GodotObject.IsInstanceValid(_selected)) _hud.SetTargetPriorityDetails(_selected.TargetPriority?.ToString() ?? ""); }
    // =========================================================
    // A visible placed tower is the only action target; previews, pause and passive types reject safely.
    private void OnTargetPriorityRequested(int priority)
    {
        if (!_hud.IsInspectionVisible || _placing || GetTree().Paused || !GodotObject.IsInstanceValid(_selected)) return;
        _selected.SetTargetPriority((TowerTargeting.Priority)priority); OnSelectedTargetPriorityChanged();
    }
    // =========================================================
    // Re-resolve only real stat input changes, never a targeting-policy selection.
    private void OnSelectedStatsChanged()
    {
        if (!GodotObject.IsInstanceValid(_selected)) return;
        if (_hud.IsInspectionVisible) _hud.ShowTowerDetails(_selected.AuthoredConfig.DisplayName, (_selected.Definition?.Category.ToString() ?? "Tower") + " / Placed",
            DescribeTower($"Grid cell: {_selected.GridCell.X}, {_selected.GridCell.Y}"),
            _selected.AuthoredConfig.Description + "\nConstruction paid.");
        if (_hud.IsInspectionVisible) { _hud.SetTargetPriorityDetails(_selected.AuthoredConfig.InspectorTargetPriorityLabel); _hud.SetRankDetails(_selected); _hud.SetDefenseDetails(_selected); _hud.SetWorkDetails(_selected); _hud.SetConditionDetails(_selected); RefreshUpgradePreview(); }
        QueueRedraw();
    }

    private TowerBase _workObserved;
    // =========================================================
    // Only the selected visible building publishes work UI; close, selection changes and unload detach all hooks.
    private void ObserveWork(TowerBase tower)
    {
        if (_workObserved == tower) return;
        if (GodotObject.IsInstanceValid(_workObserved))
        {
            _workObserved.WorkStarted -= OnSelectedWorkStarted;
            _workObserved.WorkProgressChanged -= OnSelectedWorkProgress;
            _workObserved.WorkCompleted -= OnSelectedWorkCompleted;
        }
        _workObserved = tower;
        if (GodotObject.IsInstanceValid(tower))
        {
            tower.WorkStarted += OnSelectedWorkStarted;
            tower.WorkProgressChanged += OnSelectedWorkProgress;
            tower.WorkCompleted += OnSelectedWorkCompleted;
        }
    }
    // =========================================================
    // Retain existing selection semantics but stop hidden progress notifications until reselected.
    private void OnInspectionClosed()
    {
        InvalidateSale();
        if (_selected is PowerDistributionBuilding distribution) distribution.SetSelected(false);
        CancelPowerLink();
        _selectionOverlay?.Clear();
        _rangeOverlay?.Clear();
        _browsingBuild = false;
        ObserveWork(null);
        if (GodotObject.IsInstanceValid(_selected))
        { _selected.HeatChanged -= OnSelectedConditionsChanged; _selected.DisableStateChanged -= OnSelectedConditionsChanged; }
    }
    // =========================================================
    // Start/completion refresh purchase availability once; intermediate ticks never resolve combat stats.
    private void OnSelectedWorkStarted()
    { _selectionOverlay?.RefreshState(); OnSelectedWorkProgress(); if (_hud.IsInspectionVisible) RefreshUpgradePreview(); }
    private void OnSelectedWorkProgress()
    { if (_hud.IsInspectionVisible && GodotObject.IsInstanceValid(_selected)) { _hud.SetWorkDetails(_selected); _hud.SetConditionDetails(_selected); } }
    private void OnSelectedWorkCompleted()
    {
        _selectionOverlay?.RefreshState();
        OnSelectedWorkProgress(); OnSelectedRuntimeChanged();
        if (_hud.IsInspectionVisible) RefreshUpgradePreview();
    }

    private bool _conditionRefreshQueued;
    // =========================================================
    // One pending callback reads the current selection, so switching/closing cannot publish a stale tower card.
    private void OnSelectedConditionsChanged()
    {
        _selectionOverlay?.RefreshState();
        if (_conditionRefreshQueued || !_hud.IsInspectionVisible) return;
        _conditionRefreshQueued = true; Callable.From(() =>
        {
            _conditionRefreshQueued = false;
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || !GodotObject.IsInstanceValid(_hud)) return;
            if (GodotObject.IsInstanceValid(_selected) && !_selected.IsQueuedForDeletion() && _hud.IsInspectionVisible)
                _hud.SetConditionDetails(_selected);
        }).CallDeferred();
    }

    // =========================================================
    // Coalesce fast beam/inventory changes into one selected-only label update, never pipeline work.
    private bool _runtimeRefreshQueued;
    private void OnSelectedRuntimeChanged()
    {
        if (_runtimeRefreshQueued || !_hud.IsInspectionVisible) return;
        _runtimeRefreshQueued = true; Callable.From(() =>
        {
            _runtimeRefreshQueued = false;
            if (GodotObject.IsInstanceValid(_selected) && _hud.IsInspectionVisible) { _hud.SetDefenseDetails(_selected); RefreshSelling(); }
        }).CallDeferred();
    }

    // =========================================================
    // XP ticks update only the rank section; they do not resolve stats or erase the paid upgrade preview.
    private bool _rankRefreshQueued;
    // =========================================================
    // Schedule only for a visible selected tower; one pending callback handles all awards in this frame.
    private void OnSelectedExperienceChanged()
    {
        if (_rankRefreshQueued || !_hud.IsInspectionVisible || !GodotObject.IsInstanceValid(_selected)) return;
        _rankRefreshQueued = true; CallDeferred(MethodName.RefreshSelectedExperience);
    }

    // =========================================================
    // Coalesce an AoE's many XP awards into one display refresh; there is no per-frame progression poll.
    private void RefreshSelectedExperience()
    {
        _rankRefreshQueued = false;
        if (_hud.IsInspectionVisible && GodotObject.IsInstanceValid(_selected)) _hud.SetRankDetails(_selected);
    }

    // =========================================================
    // Preview resolution happens on changed inputs/cells, not as a full pipeline every frame.
    private void OnPreviewConfigChanged()
    {
        _previewDirty = true;
        RefreshBuildAffordability();
        if (_browsingBuild) RefreshBuildDetails();
        else if (_placing && _selected == null) UpdatePreview();
    }

    // =========================================================
    // Preview uses the same pipeline as a real upgrade and cannot mutate/spend.
    private void RefreshUpgradePreview()
    {
        RefreshSelling();
        if (!GodotObject.IsInstanceValid(_selected))
        { _hud.SetUpgradeComparison(null, null); _hud.SetUpgradeDetails("", "", false); return; }
        _displayedUpgradeLevel = _selected.UpgradeLevel;
        try
        {
            if (_selected.Levels == null || _selected.Levels.Count == 0)
            { _hud.SetUpgradeComparison(null, null); _hud.SetUpgradeDetails("", "", false); return; }
            TowerUpgradeController.Validate(_selected.Levels);
            bool upgrading = _selected.WorkPhase == TowerWorkPhase.Upgrading;
            TowerLevel next = upgrading ? _selected.Levels[_selected.PendingUpgradeLevel - 1] : _selected.NextUpgrade;
            if (next == null)
            {
                _hud.SetUpgradeComparison(_selected.FinalStats, null);
                _hud.SetUpgradeDetails("MAX LEVEL — all purchased tiers complete.", "", false);
                return;
            }
            TowerFinalStats now = _selected.FinalStats, future = _selected.ResolveLevel(_selected.UpgradeLevel + 1);
            _hud.SetUpgradeComparison(now, future);
            string changes = upgrading ? $"LEVEL {next.Level} · Cost already paid." : $"LEVEL {next.Level} · "
                + TowerPresentation.DescribeResourceCosts(next.Costs) + $" · {next.WorkSeconds:0.##}s";
            bool enabled = !_selected.IsBusy && Settings.AllowUpgrades && _economy.CanAfford(next.Costs);
            if (_selected.IsBusy) changes += upgrading ? "\nUpgrade in progress." : "\nAvailable after construction.";
            else if (!Settings.AllowUpgrades) changes += "\nUpgrades disabled.";
            else if (!enabled) changes += "\nInsufficient resources.";
            _hud.SetUpgradeDetails(changes, upgrading ? "UPGRADING…" : $"UPGRADE LEVEL {next.Level}",
                enabled, upgrading ? "" : UpgradeButtonPrice(next));
        }
        catch (Exception exception)
        { _hud.SetUpgradeComparison(null, null); _hud.SetUpgradeDetails("Invalid upgrade data: " + exception.Message, "", false); }
    }

    // =========================================================
    // Keep the button short: show Credits, while its tooltip lists every resource and work time.
    private static string UpgradeButtonPrice(TowerLevel level)
    {
        if (level?.Costs == null) return "";
        foreach (ResourceCost cost in level.Costs)
            if (cost?.Resource?.ResourceId == "credits")
                return $"{cost.Amount:N0} CREDITS";
        return TowerPresentation.DescribeResourceCosts(level.Costs).ToUpperInvariant();
    }

    // =========================================================
    // Reject stale selection/level requests; world occupancy and aim/cooldown are not rebuilt.
    private void OnUpgradeRequested()
    {
        if (!_hud.IsInspectionVisible || !GodotObject.IsInstanceValid(_selected)) return;
        ulong frame = Engine.GetProcessFrames();
        if (_lastUpgradeFrame == frame) return;
        _lastUpgradeFrame = frame;
        if (!TowerUpgradeController.TryUpgrade(_selected, _economy, Settings, _displayedUpgradeLevel, out string failure))
        {
            RefreshUpgradePreview();
            _hud.SetUpgradeError(failure);
        }
    }

    // =========================================================
    // Detect actual GUI hover instead of blocking the full-screen transparent HUD root.
    private bool IsOverUi()
    {
        Control hovered = GetViewport().GuiGetHoveredControl();
        return hovered != null && hovered.MouseFilter != Control.MouseFilterEnum.Ignore;
    }
    #endregion

    #region Feedback
    // =========================================================
    // Build previews use the same frozen deployment bonuses as paid placement; passive configs stay capability-free.
    private TowerFinalStats ResolveBuildStats(TowerConfig config) => config.Levels.Count == 0 ? config.ResolveStats(1) :
        TowerStatPipeline.Resolve(config, 1, research: GetNode<CitadelSession>("../CitadelSession").Research.ForTower(_buildDefinition.TowerId));

    // =========================================================
    // Show active weapon stats and the one authoritative shared construction recipe.
    private string DescribeTower(string extra = "")
    {
        TowerConfig config = _selected?.AuthoredConfig ?? _buildDefinition.DefaultConfig;
        TowerFinalStats final = _selected == null ? ResolveBuildStats(config) : _selected.FinalStats;
        Vector2I footprint = _selected?.Footprint ?? _buildDefinition.Footprint;
        string stats = $"Footprint: {footprint.X} x {footprint.Y}\n" +
            (_selected == null ? "" : $"Tower level: {_selected.UpgradeLevel}\n") +
            $"Cost: {TowerPresentation.DescribeResourceCosts(config.ConstructionCosts)}\n" + config.DescribeStats(final) + (final?.Ammunition is AmmunitionStats ammo ? "\n" + ammo.Describe() : "");
        return extra.Length == 0 ? stats : stats + "\n" + extra;
    }

    // =========================================================
    // Draw the full preview rectangle and optional selected weapon range; grid transforms stay authoritative.
    public override void _Draw()
    {
        if (!_previewVisible || !Settings.ShowPlacementPreview) return;
        float size = _grid.Config.CellSize;
        DrawSetTransformMatrix(GlobalTransform.AffineInverse() * _grid.GlobalTransform);
        Rect2 rect = new((Vector2)_hoverCell * size, (Vector2)_buildDefinition.Footprint * size);
        Color color = _previewValid ? new Color(0.3f, 0.95f, 0.65f, 0.85f)
            : new Color(1f, 0.35f, 0.30f, 0.9f);
        DrawRect(rect, new Color(color.R, color.G, color.B, 0.12f));
        DrawRect(rect, color, filled: false, width: 2f);
        DrawSetTransformMatrix(Transform2D.Identity);
    }
    #endregion
}

