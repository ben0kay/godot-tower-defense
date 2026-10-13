// Build mode for the first Battle Tower: ghost, grid validation and selection.
// WorldGrid owns occupancy; the local dictionary only finds/selects tower instances.
using Godot;
using System.Collections.Generic;

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
    private WorldGrid _grid;
    private GameplayHUD _hud;
    private Node2D _towers;
    private Sprite2D _ghost;
    private readonly Dictionary<Vector2I, BattleTower> _placedTowers = new();
    private Vector2I _hoverCell = new(int.MinValue, int.MinValue);
    private bool _placing, _previewVisible, _previewValid;
    private BattleTower _selected;
    #endregion

    #region Lifecycle
    // =========================================================
    // One placement ghost; the world and HUD continue to own their separate responsibilities.
    public override void _Ready()
    {
        _grid = GetNode<WorldGrid>("../World/Grid");
        _towers = GetNode<Node2D>("../World/Towers");
        _hud = GetNode<GameplayHUD>("../UI/GameplayHUD");
        _hud.TowerChoiceRequested += OnTowerChoiceRequested;
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
        if (GodotObject.IsInstanceValid(_hud)) _hud.TowerChoiceRequested -= OnTowerChoiceRequested;
    }
    #endregion

    #region Build Mode
    // =========================================================
    // Battle is the only implemented tower. Other HUD options remain informational.
    private void OnTowerChoiceRequested(string id)
    {
        if (id != "battle") { CancelPlacement(); return; }
        SelectTower(null);
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
        bool valid = visible && _grid.PlacementFailure(cell).Length == 0;
        if (_previewVisible == visible && _previewValid == valid && (!visible || cell == _hoverCell))
            return;

        _previewVisible = visible;
        _previewValid = valid;
        _hoverCell = cell;
        _ghost.Visible = visible && Settings.ShowPlacementPreview;
        if (visible)
        {
            if (_selected == null)
                _hud.ShowTowerDetails(BattleConfig.DisplayName, "Offensive / Build Preview", DescribeTower(),
                    valid ? "Left-click to build. Right-click / Esc cancels."
                        : _grid.PlacementFailure(cell));
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
    // Spawn an idle tower centered on a valid, unoccupied grid cell.
    private void TryPlace(Vector2I cell)
    {
        if (_grid.PlacementFailure(cell).Length != 0) return;
        BattleTower tower = BattleTowerScene.Instantiate<BattleTower>();
        if (!_grid.TryOccupy(cell)) { tower.Free(); return; }
        _towers.AddChild(tower);
        tower.SetPlacement(cell, _grid.CellToWorldCenter(cell));
        _placedTowers.Add(cell, tower);
        SelectTower(tower);
        UpdatePreview();
    }

    // =========================================================
    // A selected placed tower shares the existing HUD Inspector with build previews.
    private void SelectTower(BattleTower tower)
    {
        if (_selected == tower) return;
        if (GodotObject.IsInstanceValid(_selected)) _selected.SetSelected(false);
        _selected = tower;
        if (GodotObject.IsInstanceValid(_selected))
        {
            _selected.SetSelected(true);
            _hud.ShowTowerDetails(BattleConfig.DisplayName, "Offensive / Placed",
                DescribeTower($"Grid cell: {_selected.GridCell.X}, {_selected.GridCell.Y}"),
                BattleConfig.Description + "\nFiring and credits are not active yet.");
        }
        QueueRedraw();
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
    // Clearly label stats as planned until shots and the economy exist.
    private string DescribeTower(string extra = "")
    {
        string stats = $"Footprint: 1 x 1 | Barrels: 1\n" +
            $"Cost (planned): {BattleConfig.BuildCost}\n" +
            $"Range (planned): {BattleConfig.RangePixels:0} px\n" +
            $"Rate (planned): {BattleConfig.ShotsPerSecond:0.##} shots/s\n" +
            $"Damage (planned): {BattleConfig.DamagePerShot}";
        return extra.Length == 0 ? stats : stats + "\n" + extra;
    }

    // =========================================================
    // Draw only the active preview cell and selected tower's proposed range.
    public override void _Draw()
    {
        if (GodotObject.IsInstanceValid(_selected) && Settings.ShowSelectionRange)
            DrawArc(ToLocal(_selected.GlobalPosition), BattleConfig.RangePixels, 0f,
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
