// World-positioned UI boundary inspector; constant-size cursor tests, delayed reveal and hovered-only rate sampling.
using Godot;
using System;
using System.Collections.Generic;

public partial class IslandPowerInspection : Node2D
{
    [Export] public IslandPowerInspectionStyle Style { get; set; }
    private PowerGridManager _power;
    private WorldGrid _grid;
    private Node _towerParent;
    private readonly HashSet<TowerBase> _structures = new();
    private readonly HashSet<int> _occupied = new();
    private readonly IslandPowerBoundaryCache _boundary = new();
    private int _candidate, _shown;
    private float _dwell, _leave, _sampleElapsed, _navigationCooldown, _pixelScale = 1;
    private bool _refreshQueued, _exiting;
    private Transform2D _lastCanvas;

    // =========================================================
    // Compose beside World services; UI files own all rendering, no gameplay service creates UI.
    public override void _Ready()
    {
        SetProcess(false); SetProcessInput(false); SetPhysicsProcess(false); ProcessMode = ProcessModeEnum.Pausable; ZIndex = 40;
        Style ??= GD.Load<IslandPowerInspectionStyle>("res://UI/Power/IslandInspection/DefaultIslandPowerInspectionStyle.tres") ?? new();
        _power = GetNodeOrNull<PowerGridManager>("../PowerNetworks"); _grid = _power?.Grid;
        _towerParent = GetNodeOrNull<Node>("../Towers");
        if (_grid == null || _towerParent == null) { SetProcess(false); return; }
        GlobalTransform = _grid.GlobalTransform;
        CreateTooltip();
        _power.AccountingChanged += MarkDirty; _power.NetworksChanged += MarkDirty; _power.DistributionChanged += MarkDirty;
        _towerParent.ChildEnteredTree += TowerEntered; _towerParent.ChildExitingTree += TowerExited;
        foreach (Node node in _towerParent.GetChildren()) TowerEntered(node);
        _lastCanvas = GetGlobalTransformWithCanvas(); MarkDirty();
    }
    private void TowerEntered(Node node)
    {
        if (node is not TowerBase tower || !_structures.Add(tower)) return;
        tower.SpatialChanged += TowerMoved; tower.Destroyed += MarkDirty; tower.ObserveSpatialChanges(true); MarkDirty();
    }
    private void TowerExited(Node node)
    {
        if (node is not TowerBase tower || !_structures.Remove(tower)) return;
        if (GodotObject.IsInstanceValid(tower))
        { tower.SpatialChanged -= TowerMoved; tower.Destroyed -= MarkDirty; tower.ObserveSpatialChanges(false); }
        MarkDirty();
    }
    private void TowerMoved(TowerBase tower) => MarkDirty();
    // Coalesce source, placement and topology events after the current transaction/Ready sequence has finished.
    private void MarkDirty()
    {
        if (_exiting || _refreshQueued || !IsInsideTree()) return;
        _refreshQueued = true; Callable.From(RefreshOccupancy).CallDeferred();
    }
    private void RefreshOccupancy()
    {
        _refreshQueued = false;
        if (_exiting || !GodotObject.IsInstanceValid(_power) || _power.Grid == null) return;
        _power.CopyPowerIslandRegions(_occupied);
        foreach (var tower in _structures)
        {
            if (!GodotObject.IsInstanceValid(tower) || tower.IsDestroyed || tower.IsQueuedForDeletion()) continue;
            if (tower is not PowerGenerator && tower is not PowerDistributionBuilding && tower.AuthoredConfig?.Power == null) continue;
            Vector2 first = _grid.ToLocal(tower.GlobalPosition) - (Vector2)(tower.Footprint - Vector2I.One) * (_grid.Config.CellSize * .5f);
            int region = _power.RegionForFootprint(_grid.WorldToCell(_grid.ToGlobal(first)), tower.Footprint);
            if (region > 0) _occupied.Add(region);
        }
        if (_boundary.Revision != _power.Revision) _boundary.Rebuild(_power);
        if (!_occupied.Contains(_shown)) HideInspection();
        SetProcess(Style.Enabled && _occupied.Count > 0);
        SetProcessInput(Style.Enabled && _occupied.Count > 0);
        QueueRedraw();
    }

    // =========================================================
    // No map/tower scans per frame: nearby cell edges supply the hit target; only an open tooltip samples consumers.
    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_power) || _power.Grid == null) { HideInspection(); SetProcess(false); return; }
        float dt = (float)Math.Min(delta, .1);
        GlobalTransform = _grid.GlobalTransform;
        var canvas = GetGlobalTransformWithCanvas();
        float scale = Mathf.Max(.01f, canvas.Scale.Length() / Mathf.Sqrt(2));
        if (Mathf.Abs(scale - _pixelScale) > .001f) { _pixelScale = scale; QueueRedraw(); }
        bool cameraMoving = canvas.Origin.DistanceSquaredTo(_lastCanvas.Origin) > .01f ||
            canvas.X.DistanceSquaredTo(_lastCanvas.X) > .000001f;
        _lastCanvas = canvas; _navigationCooldown = Mathf.Max(0, _navigationCooldown - dt);
        bool blocked = !Style.Enabled || GetTree().Paused || Input.IsMouseButtonPressed(MouseButton.Middle) ||
            _navigationCooldown > 0 || cameraMoving || GetViewport().GuiGetHoveredControl() != null ||
            !GetViewportRect().HasPoint(GetViewport().GetMousePosition());
        if (blocked) { HideInspection(); return; }

        int hit = HitBoundary(_grid.GetLocalMousePosition());
        if (hit == _shown && hit > 0) _leave = 0;
        else if (_shown > 0 && (_leave += dt) >= Style.LeaveGraceSeconds) HideInspection();
        if (hit != _candidate) { _candidate = hit; _dwell = 0; }
        if (hit > 0 && _shown != hit)
        {
            _dwell += dt;
            if (_dwell >= Style.HoverSeconds) ShowInspection(hit);
        }
        if (_shown > 0)
        {
            _sampleElapsed += dt;
            if (_sampleElapsed >= Math.Max(.1f, Style.SampleSeconds)) SampleIsland(_sampleElapsed);
            PositionTooltip();
        }
    }
    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouseButton button && button.Pressed &&
            button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown or MouseButton.Middle)
        { _navigationCooldown = .2f; HideInspection(); }
    }
    public override void _Notification(int what)
    {
        if (what == NotificationPaused) HideInspection();
    }

    // =========================================================
    // Invisible screen-pixel hit width follows actual conductive edges, including holes; never claims the interior.
    private int HitBoundary(Vector2 point)
    {
        float size = _grid.Config.CellSize, tolerance = Mathf.Clamp(Style.HitWidthPixels, 3, 16) / _pixelScale;
        Vector2I cell = new(Mathf.FloorToInt(point.X / size), Mathf.FloorToInt(point.Y / size));
        int reach = Mathf.Clamp(Mathf.CeilToInt(tolerance / size), 1, 4), best = 0;
        float nearest = tolerance * tolerance;
        for (int y = cell.Y - reach; y <= cell.Y + reach; y++)
        for (int x = cell.X - reach; x <= cell.X + reach; x++)
        {
            Vector2I at = new(x, y); int region = _power.RegionAt(at);
            if (region <= 0 || !_occupied.Contains(region)) continue;
            Vector2 a = new Vector2(x, y) * size;
            for (int edge = 0; edge < 4; edge++)
            {
                Vector2I direction = edge == 0 ? Vector2I.Up : edge == 1 ? Vector2I.Right : edge == 2 ? Vector2I.Down : Vector2I.Left;
                if (_power.RegionAt(at + direction) == region) continue;
                Vector2 from = a + (edge == 1 ? new Vector2(size, 0) : edge == 2 ? new Vector2(size, size) : edge == 3 ? new Vector2(0, size) : Vector2.Zero);
                Vector2 to = from + (edge == 0 ? new Vector2(size, 0) : edge == 1 ? new Vector2(0, size) : edge == 2 ? new Vector2(-size, 0) : new Vector2(0, -size));
                float distance = point.DistanceSquaredTo(Geometry2D.GetClosestPointToSegment(point, from, to));
                if (distance < nearest || distance == nearest && (best == 0 || region < best)) { nearest = distance; best = region; }
            }
        }
        return best;
    }

    // =========================================================
    // World teardown owns every subscription, tooltip and retained reference, including disabled/unfinished towers.
    public override void _ExitTree()
    {
        _exiting = true;
        if (GodotObject.IsInstanceValid(_power))
        { _power.AccountingChanged -= MarkDirty; _power.NetworksChanged -= MarkDirty; _power.DistributionChanged -= MarkDirty; }
        if (GodotObject.IsInstanceValid(_towerParent))
        { _towerParent.ChildEnteredTree -= TowerEntered; _towerParent.ChildExitingTree -= TowerExited; }
        foreach (var tower in _structures) if (GodotObject.IsInstanceValid(tower))
        { tower.SpatialChanged -= TowerMoved; tower.Destroyed -= MarkDirty; tower.ObserveSpatialChanges(false); }
        _structures.Clear(); _warnings.Clear(); _samples.Clear(); _occupied.Clear();
    }
}
