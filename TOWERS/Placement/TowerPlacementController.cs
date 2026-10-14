// Build mode for the first Battle Tower: ghost, grid validation and selection.
// WorldGrid owns occupancy; the local dictionary only finds/selects tower instances.
using Godot;
using System.Collections.Generic;
using System;
using System.Text;

public partial class TowerPlacementController : Node2D
{
    #region Inspector
    [ExportGroup("Global Placement")]
    [Export] public TowerMasterConfig Settings { get; set; }

    [ExportGroup("Battle Tower")]
    [Export] public PackedScene BattleTowerScene { get; set; }
    [Export] public BattleTowerConfig BattleConfig { get; set; }
    [Export] public Texture2D BattleArtwork { get; set; }
    #endregion

    #region State
    private EconomyManager _economy;
    private bool _affordable, _previewDirty;
    private WorldGrid _grid;
    private GameplayHUD _hud;
    private SpawnManager _spawns;
    private Node2D _towers;
    private Sprite2D _ghost;
    private readonly Dictionary<Vector2I, BattleTower> _placedTowers = new();
    private Vector2I _hoverCell = new(int.MinValue, int.MinValue);
    private bool _placing, _previewVisible, _previewValid;
    private BattleTower _selected;
    private int _displayedUpgradeLevel;
    private ulong _lastUpgradeFrame = ulong.MaxValue;
    #endregion

    #region Lifecycle
    // =========================================================
    // One placement ghost; the world and HUD continue to own their separate responsibilities.
    public override void _Ready()
    {
        _economy = GetNode<EconomyManager>("../Economy");
        _economy.BalanceChanged += OnBalanceChanged;
        _grid = GetNode<WorldGrid>("../World/Grid");
        _spawns = GetNode<SpawnManager>("../World/EnemySystems/Spawning");
        _towers = GetNode<Node2D>("../World/Towers");
        _hud = GetNode<GameplayHUD>("../UI/GameplayHUD");
        _hud.TowerChoiceRequested += OnTowerChoiceRequested;
        _hud.TowerUpgradeRequested += OnUpgradeRequested;
        BattleConfig.Changed += OnPreviewConfigChanged;
        _ghost = new Sprite2D
        {
            Name = "PlacementGhost", Texture = BattleArtwork, Visible = false, ZIndex = 10,
            Modulate = new Color(0.45f, 1f, 0.73f, 0.66f)
        };
        AddChild(_ghost);
        SetProcess(false);
    }

    // =========================================================
    // Avoid subscriptions retaining scenes that are no longer alive.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_hud))
        { _hud.TowerChoiceRequested -= OnTowerChoiceRequested; _hud.TowerUpgradeRequested -= OnUpgradeRequested; }
        if (GodotObject.IsInstanceValid(_economy)) _economy.BalanceChanged -= OnBalanceChanged;
        if (GodotObject.IsInstanceValid(BattleConfig)) BattleConfig.Changed -= OnPreviewConfigChanged;
        if (GodotObject.IsInstanceValid(_selected)) _selected.StatsChanged -= OnSelectedStatsChanged;
    }
    #endregion

    #region Build Mode
    // =========================================================
    // Battle is the only implemented tower. Other HUD options remain informational.
    private void OnTowerChoiceRequested(string id)
    {
        if (id != "battle") { CancelPlacement(); SelectTower(null); return; }
        SelectTower(null);
        _affordable = _economy.CanAfford(BattleConfig.ConstructionCosts);
        _previewDirty = true;
        _placing = true;
        SetProcess(true);
        _hud.ShowTowerDetails(BattleConfig.DisplayName, "Offensive / Build Preview",
            DescribeTower(), "Left-click a buildable terrain square to build. Right-click / Esc cancels.");
        UpdatePreview();
    }

    // =========================================================
    // Poll cursor position only during placement, stopping immediately on cancellation.
    public override void _Process(double delta) => UpdatePreview();

    // =========================================================
    // One cell-based preview, green on buildable free cells and red on invalid terrain.
    private void UpdatePreview()
    {
        Vector2I cell = _grid.WorldToCell(GetGlobalMousePosition());
        bool visible = _placing && !IsOverUi() && _grid.IsInsideMap(cell);
        bool valid = visible && _affordable && _grid.PlacementFailure(cell).Length == 0;
        if (!_previewDirty && _previewVisible == visible && _previewValid == valid && (!visible || cell == _hoverCell))
            return;

        _previewDirty = false;
        _previewVisible = visible;
        _previewValid = valid;
        _hoverCell = cell;
        _ghost.Visible = visible && Settings.ShowPlacementPreview;
        if (visible)
        {
            if (_selected == null)
                _hud.ShowTowerDetails(BattleConfig.DisplayName, "Offensive / Build Preview", DescribeTower(),
                    valid ? "Left-click to build. Right-click / Esc cancels."
                        : _grid.PlacementFailure(cell).Length > 0 ? _grid.PlacementFailure(cell) : "Insufficient resources.");
            _ghost.GlobalPosition = _grid.CellToWorldCenter(cell);
            _ghost.Modulate = valid ? new Color(0.45f, 1f, 0.73f, 0.66f)
                : new Color(1f, 0.35f, 0.31f, 0.56f);
        }
        QueueRedraw();
    }

    // =========================================================
    // Exit build mode without closing the independent Inspector or tower tray.
    private void CancelPlacement()
    {
        _placing = false;
        _previewVisible = false;
        _previewValid = false;
        if (_ghost != null) _ghost.Hide();
        SetProcess(false);
        QueueRedraw();
    }
    #endregion

    #region World Input
    // =========================================================
    // Unhandled world clicks only; GUI panels consume mouse events independently.
    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventKey key && key.Pressed && !key.Echo
            && key.Keycode == Key.Escape && _placing)
        {
            CancelPlacement();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (input is not InputEventMouseButton mouse || !mouse.Pressed || IsOverUi()) return;
        if (mouse.ButtonIndex == MouseButton.Right && _placing)
        {
            CancelPlacement();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (mouse.ButtonIndex != MouseButton.Left) return;

        Vector2I cell = _grid.WorldToCell(GetGlobalMousePosition());
        if (_placing) TryPlace(cell);
        else if (_placedTowers.TryGetValue(cell, out BattleTower tower)) SelectTower(tower);
        else { SelectTower(null); _hud.ClearInspection(); }
        GetViewport().SetInputAsHandled();
    }

    // =========================================================
    // Spawn a configured firing tower centered on a valid, unoccupied grid cell.
    private void TryPlace(Vector2I cell)
    {
        if (_grid.PlacementFailure(cell).Length != 0) return;
        string failure = _spawns.PlacementFailure(cell);
        if (failure.Length != 0)
        {
            _hud.ShowTowerDetails(BattleConfig.DisplayName, "Placement rejected", DescribeTower(), failure);
            return;
        }
        BattleTower tower = null;
        if (!_economy.TryPurchase(BattleConfig.ConstructionCosts, () => CommitTower(cell, out tower)))
        {
            _hud.ShowTowerDetails(BattleConfig.DisplayName, "Placement rejected", DescribeTower(),
                _economy.CanAfford(BattleConfig.ConstructionCosts) ? "Tower creation failed; no resources charged."
                    : "Insufficient resources.");
            return;
        }
        SelectTower(tower);
        UpdatePreview();
    }

    // =========================================================
    // Roll back occupancy and the scene on creation failure, before the wallet deducts payment.
    private bool CommitTower(Vector2I cell, out BattleTower tower)
    {
        tower = null;
        bool occupied = false;
        try
        {
            if (BattleTowerScene == null) return false;
            Node instance = BattleTowerScene.Instantiate();
            if (instance is not BattleTower created) { instance.Free(); return false; }
            if (created.GetNodeOrNull<Node2D>("TurretPivot") == null
                || created.GetNodeOrNull<Sprite2D>("TurretPivot/Sprite") == null)
            { created.Free(); return false; }
            tower = created;
            tower.Config = BattleConfig.CreateInstanceCopy();
            if (!_grid.TryOccupy(cell)) { tower.Free(); tower = null; return false; }
            occupied = true;
            _towers.AddChild(tower);
            tower.SetPlacement(cell, _grid.CellToWorldCenter(cell));
            if (tower.GetParent() != _towers) throw new InvalidOperationException("Tower did not enter the world.");
            _placedTowers.Add(cell, tower);
            return true;
        }
        catch (Exception exception)
        {
            _placedTowers.Remove(cell);
            if (GodotObject.IsInstanceValid(tower)) tower.Free();
            if (occupied) _grid.ReleaseOccupancy(cell);
            tower = null;
            GD.PushWarning("Tower creation rolled back: " + exception.Message);
            return false;
        }
    }

    // =========================================================
    // Refresh affordability only when wallet state changes, not on every preview frame.
    private void OnBalanceChanged(string resourceId, int amount)
    {
        _affordable = _economy.CanAfford(BattleConfig.ConstructionCosts);
        _previewDirty = true;
        if (_hud.IsInspectionVisible) RefreshUpgradePreview();
    }

    // =========================================================
    // A selected placed tower shares the existing HUD Inspector with build previews.
    private void SelectTower(BattleTower tower)
    {
        if (_selected == tower) return;
        if (GodotObject.IsInstanceValid(_selected))
        { _selected.StatsChanged -= OnSelectedStatsChanged; _selected.SetSelected(false); }
        _selected = tower;
        if (GodotObject.IsInstanceValid(_selected))
        {
            _selected.StatsChanged += OnSelectedStatsChanged;
            _selected.SetSelected(true);
            _hud.ShowTowerDetails(BattleConfig.DisplayName, "Offensive / Placed",
                DescribeTower($"Grid cell: {_selected.GridCell.X}, {_selected.GridCell.Y}"),
                _selected.Config.Description + "\nConstruction paid; automatic firing active.");
        }
        RefreshUpgradePreview();
        QueueRedraw();
    }

    // =========================================================
    // Refresh the selected tower's displayed effective values/range when its inputs change.
    private void OnSelectedStatsChanged()
    {
        if (!GodotObject.IsInstanceValid(_selected) || _selected.FinalStats == null) return;
        if (_hud.IsInspectionVisible) _hud.ShowTowerDetails(_selected.Config.DisplayName, "Offensive / Placed",
            DescribeTower($"Grid cell: {_selected.GridCell.X}, {_selected.GridCell.Y}"),
            _selected.Config.Description + "\nConstruction paid; automatic firing active.");
        if (_hud.IsInspectionVisible) RefreshUpgradePreview();
        QueueRedraw();
    }

    // =========================================================
    // Preview resolution happens on changed inputs/cells, not as a full pipeline every frame.
    private void OnPreviewConfigChanged()
    {
        _previewDirty = true;
        _affordable = _economy.CanAfford(BattleConfig.ConstructionCosts);
        if (_placing && _selected == null) UpdatePreview();
    }

    // =========================================================
    // Preview uses the same pipeline as a real upgrade and cannot mutate/spend.
    private void RefreshUpgradePreview()
    {
        if (!GodotObject.IsInstanceValid(_selected))
        { _hud.SetUpgradeDetails("", "", false); return; }
        _displayedUpgradeLevel = _selected.UpgradeLevel;
        try
        {
            TowerUpgradeController.Validate(_selected.UpgradeTiers);
            TowerUpgradeTier next = _selected.NextUpgrade;
            if (next == null) { _hud.SetUpgradeDetails("Maximum level reached.", "", false); return; }
            TowerFinalStats now = _selected.FinalStats, future = _selected.ResolveLevel(_selected.UpgradeLevel + 1);
            string changes = $"NEXT: LEVEL {next.Level} — {next.DisplayName}\n";
            if (now.DamagePerShot != future.DamagePerShot) changes += $"Damage: {now.DamagePerShot} → {future.DamagePerShot}\n";
            if (now.ShotsPerSecond != future.ShotsPerSecond) changes += $"Rate: {now.ShotsPerSecond:0.##} → {future.ShotsPerSecond:0.##} shots/s\n";
            if (now.RangePixels != future.RangePixels) changes += $"Range: {now.RangePixels:0} → {future.RangePixels:0} px\n";
            if (now.ProjectileSpeed != future.ProjectileSpeed) changes += $"Projectile: {now.ProjectileSpeed:0} → {future.ProjectileSpeed:0} px/s\n";
            if (now.RotationSpeedDegrees != future.RotationSpeedDegrees) changes += $"Turn: {now.RotationSpeedDegrees:0} → {future.RotationSpeedDegrees:0}°/s\n";
            if (now.AimToleranceDegrees != future.AimToleranceDegrees) changes += $"Aim tolerance: {now.AimToleranceDegrees:0} → {future.AimToleranceDegrees:0}°\n";
            if (now.TargetSearchInterval != future.TargetSearchInterval) changes += $"Target search: {now.TargetSearchInterval:0.##} → {future.TargetSearchInterval:0.##}s\n";
            bool enabled = Settings.AllowUpgrades && _economy.CanAfford(next.Costs);
            changes += "Price: " + DescribeResourceCosts(next.Costs);
            if (!Settings.AllowUpgrades) changes += "\nUpgrades disabled.";
            else if (!enabled) changes += "\nInsufficient resources.";
            _hud.SetUpgradeDetails(changes, $"UPGRADE TO LEVEL {next.Level}", enabled);
        }
        catch (Exception exception) { _hud.SetUpgradeDetails("Invalid upgrade data: " + exception.Message, "", false); }
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
    // Show active weapon stats and the one authoritative shared construction recipe.
    private string DescribeTower(string extra = "")
    {
        BattleTowerConfig config = _selected?.Config ?? BattleConfig;
        TowerFinalStats final = _selected?.FinalStats ?? TowerStatPipeline.Resolve(BattleConfig);
        string stats = $"Footprint: 1 x 1 | Barrels: 1\n" +
            (_selected == null ? "" : $"Tower level: {_selected.UpgradeLevel}\n") +
            $"Cost: {DescribeCosts(config)}\n" +
            $"Range: {final.RangePixels:0} px\n" +
            $"Rate: {final.ShotsPerSecond:0.##} shots/s\n" +
            $"Damage: {final.DamagePerShot}\nTargeting: {config.TargetPriority}";
        return extra.Length == 0 ? stats : stats + "\n" + extra;
    }

    // =========================================================
    // The Inspector displays every required resource, including future optional materials.
    private static string DescribeCosts(BattleTowerConfig config)
    {
        return DescribeResourceCosts(config.ConstructionCosts);
    }

    // =========================================================
    // The same display format is used for shared build and upgrade recipes.
    private static string DescribeResourceCosts(Godot.Collections.Array<ResourceCost> costs)
    {
        if (costs == null) return "Invalid cost configuration";
        StringBuilder text = new();
        foreach (ResourceCost cost in costs)
        {
            if (cost?.Resource == null || string.IsNullOrWhiteSpace(cost.Resource.ResourceId) || cost.Amount < 0) return "Invalid cost configuration";
            if (cost.Amount == 0) continue;
            if (text.Length > 0) text.Append(", ");
            text.Append($"{cost.Amount:N0} {cost.Resource.DisplayName}");
        }
        return text.Length == 0 ? "Free" : text.ToString();
    }

    // =========================================================
    // Draw only the active preview cell and selected tower's weapon range.
    public override void _Draw()
    {
        if (GodotObject.IsInstanceValid(_selected) && _selected.FinalStats != null && Settings.ShowSelectionRange)
            DrawArc(ToLocal(_selected.GlobalPosition), _selected.FinalStats.RangePixels, 0f,
                Mathf.Tau, 96, new Color(0.37f, 0.82f, 0.95f, 0.55f), 2f);

        if (!_previewVisible || !Settings.ShowPlacementPreview) return;
        float size = _grid.Config.CellSize;
        Vector2 upperLeft = ToLocal(_grid.CellToWorldCenter(_hoverCell)) - Vector2.One * size * 0.5f;
        Rect2 rect = new(upperLeft, Vector2.One * size);
        Color color = _previewValid ? new Color(0.3f, 0.95f, 0.65f, 0.85f)
            : new Color(1f, 0.35f, 0.30f, 0.9f);
        DrawRect(rect, new Color(color.R, color.G, color.B, 0.12f));
        DrawRect(rect, color, filled: false, width: 2f);
    }
    #endregion
}
