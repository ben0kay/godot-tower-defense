// Selected fighter orders and the click-through world preview for a pending A/B edit.
using Godot;

public partial class FighterStationPanel
{
    private Button _autoPilot, _autoPatrol, _editRoute;
    private Label _patrolStatus;
    private string _patrolError = "";
    private FighterDrone _editing;
    private Vector2? _draftA, _draftB;
    private FighterPatrolRoute _draftRoute, _shownRoute;
    private Node2D _patrolPreview;
    private WorldGrid _patrolGrid;

    private void BuildPatrolControls()
    {
        _patrolStatus = UIButtonFactory.Label("", 10, ThemeConfig);
        _patrolStatus.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        AddChild(_patrolStatus);
        var modes = new HBoxContainer();
        AddChild(modes);
        _autoPilot = UIButtonFactory.Create("AUTO-PILOT", ChooseAutoPilot, UIButtonStyle.Compact, 24, ThemeConfig);
        _autoPatrol = UIButtonFactory.Create("PATROL", ChoosePatrol, UIButtonStyle.Compact, 24, ThemeConfig);
        _editRoute = UIButtonFactory.Create("EDIT A/B", BeginPatrolEdit, UIButtonStyle.Compact, 24, ThemeConfig);
        modes.AddChild(_autoPilot); modes.AddChild(_autoPatrol);
        AddChild(_editRoute);
    }

    // =========================================================
    // Existing accepted endpoints are retained when switching mode or cancelling an edit.
    private FighterDrone SelectedFighter => _station?.Fighters?.At(_slot);
    private void ChooseAutoPilot()
    {
        CancelPatrolEdit();
        if (SelectedFighter is { } fighter) _station.Fighters.SetPatrolMode(fighter, false);
    }

    private void ChoosePatrol()
    {
        if (SelectedFighter is not { } fighter) return;
        if (fighter.PatrolRoute == null) { BeginPatrolEdit(); return; }
        CancelPatrolEdit();
        _station.Fighters.SetPatrolMode(fighter, true);
    }

    private void BeginPatrolEdit()
    {
        if (_station?.CanCommand != true || _patrolGrid == null || SelectedFighter is not { } fighter) return;
        _editing = fighter;
        _draftA = _draftB = null; _draftRoute = null; _patrolError = "";
        RefreshPatrolControls(fighter);
        _patrolPreview?.QueueRedraw();
    }

    internal bool CancelPatrolEdit()
    {
        if (_editing == null) return false;
        _editing = null;
        _draftA = _draftB = null; _draftRoute = null; _patrolError = "";
        RefreshPatrolControls(SelectedFighter);
        _patrolPreview?.QueueRedraw();
        return true;
    }

    // =========================================================
    // The placement controller forwards only unhandled world clicks. Commit A and B atomically.
    internal bool HandlePatrolClick(Vector2 point)
    {
        if (_editing == null || _station?.CanCommand != true) return false;
        if (!GodotObject.IsInstanceValid(_editing) || _editing != SelectedFighter) { CancelPatrolEdit(); return false; }
        if (!_patrolGrid.IsInsideMap(_patrolGrid.WorldToCell(point)))
        { _patrolStatus.Text = _patrolError = "Choose a point inside the map."; return true; }
        if (!_draftA.HasValue)
        {
            _draftA = point; _patrolError = "";
            RefreshPatrolControls(_editing);
            _patrolPreview.QueueRedraw();
            return true;
        }
        var route = _station.Fighters.PreviewPatrol(_editing, _draftA.Value, point);
        if (route == null)
        { _patrolStatus.Text = _patrolError = "Move B farther away or give the loop more room."; return true; }
        FighterDrone fighter = _editing;
        CancelPatrolEdit();
        _station.Fighters.SetPatrolMode(fighter, true, route);
        return true;
    }

    internal void UpdatePatrolPreview(Vector2? point)
    {
        if (_editing == null || !_draftA.HasValue || _draftB == point) return;
        _draftB = point;
        _draftRoute = point.HasValue && GodotObject.IsInstanceValid(_editing)
            ? _station.Fighters.PreviewPatrol(_editing, _draftA.Value, point.Value) : null;
        _patrolPreview?.QueueRedraw();
    }

    // =========================================================
    // The preview only exists for the inspected station and redraws when its route changes.
    private void AttachPatrolPreview()
    {
        _patrolGrid = _station.Fighters?.PatrolGrid;
        if (_patrolGrid == null) return;
        _patrolPreview = new Node2D { Name = "FighterPatrolPreview", ZIndex = 12,
            Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded } };
        _patrolPreview.Draw += DrawPatrolPreview;
        _patrolGrid.AddChild(_patrolPreview);
    }

    private void RefreshPatrolControls(FighterDrone fighter)
    {
        if (_autoPilot == null) return;
        bool editing = _editing != null;
        bool unavailable = fighter == null || _patrolGrid == null || _station?.CanCommand != true;
        _autoPilot.Disabled = unavailable || (!editing && !fighter.IsPatrolling);
        _autoPatrol.Disabled = unavailable || (!editing && fighter.IsPatrolling);
        _editRoute.Disabled = unavailable;
        FighterPatrolRoute route = fighter?.PatrolRoute;
        string order = fighter?.IsPatrolling == true ? "AUTO-PATROL" : "AUTO-PILOT";
        string endpoints = route == null || _patrolGrid == null ? "" : $" · A {_patrolGrid.WorldToCell(route.A)} / B {_patrolGrid.WorldToCell(route.B)}";
        _patrolStatus.Text = editing ? (_patrolError.Length > 0 ? _patrolError : _draftA.HasValue ? "Click B · Esc/RMB cancels" : "Click A · Esc/RMB cancels")
            : fighter == null ? "" : order + endpoints;
        if (_shownRoute == route) return;
        _shownRoute = route;
        _patrolPreview?.QueueRedraw();
    }

    private void DrawPatrolPreview()
    {
        FighterPatrolRoute route = _editing != null ? _draftRoute : _shownRoute;
        Color cyan = new(.25f, .91f, 1f, .72f);
        if (route != null)
        {
            _patrolPreview.DrawSetTransformMatrix(_patrolPreview.GlobalTransform.AffineInverse());
            _patrolPreview.DrawPolyline(route.Points, cyan, 2, true);
            _patrolPreview.DrawSetTransformMatrix(Transform2D.Identity);
        }
        if (route != null || _draftA.HasValue)
            DrawPatrolPoint(_draftA ?? route.A, "A", cyan);
        if (route != null || _draftB.HasValue)
            DrawPatrolPoint(_draftB ?? route.B, "B", route == null ? new Color("#e58b75") : cyan);
    }

    private void DrawPatrolPoint(Vector2 point, string label, Color color)
    {
        Vector2 local = _patrolPreview.ToLocal(point);
        _patrolPreview.DrawCircle(local, 6, color, true, -1, true);
        _patrolPreview.DrawString(ThemeDB.FallbackFont, local + new Vector2(9, -6), label, fontSize: 13, modulate: color);
    }

    private void DropPatrolPreview()
    {
        _editing = null; _draftA = _draftB = null; _draftRoute = _shownRoute = null;
        if (GodotObject.IsInstanceValid(_patrolPreview))
        {
            _patrolPreview.Draw -= DrawPatrolPreview;
            _patrolPreview.Hide();
            _patrolPreview.QueueFree();
        }
        _patrolPreview = null; _patrolGrid = null;
    }
}
