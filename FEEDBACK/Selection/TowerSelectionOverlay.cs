// One selected-tower world outline and one HUD overlay; no effects for unselected towers.
using Godot;

public partial class TowerSelectionOverlay : Node2D
{
    private TowerSelectionStyle _style;
    private TowerSelectionScreen _screen;
    private TowerBase _tower;
    private WorldGrid _grid;
    private float _elapsed, _redrawTimer;
    private bool _disabled;
    private string _displayedName;

    // =========================================================
    // The HUD screen is a non-interactive first child, behind the existing Inspector and build tray.
    public void Configure(WorldGrid grid, Control hud)
    {
        _grid = grid;
        _style = GD.Load<TowerSelectionStyle>("res://FEEDBACK/Selection/DefaultTowerSelectionStyle.tres") ?? new TowerSelectionStyle();
        _screen = new TowerSelectionScreen { Name = "SelectedTowerScreen" };
        hud.AddChild(_screen);
        hud.MoveChild(_screen, 0);
        _screen.Configure(_style, hud.GetNode<Control>("BottomArea"));
        ZIndex = 9; Hide(); SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Called only when selected placed tower changes; build previews never use this visual.
    public void ShowTower(TowerBase tower)
    {
        if (!GodotObject.IsInstanceValid(tower) || tower.IsQueuedForDeletion() || tower.IsDestroyed)
        { Clear(); return; }
        bool changed = _tower != tower;
        _tower = tower;
        if (changed) { _elapsed = 0f; _redrawTimer = 0f; _displayedName = null; }
        Visible = _style.HologramEnabled;
        SetProcess(Visible);
        RefreshState();
    }

    // =========================================================
    // Existing selected-only disable and construction notifications update colour immediately.
    public void RefreshState()
    {
        if (!GodotObject.IsInstanceValid(_tower) || _tower.IsQueuedForDeletion() || _tower.IsDestroyed)
        { Clear(); return; }
        bool disabled = _tower.IsDisabled;
        string name = _tower.AuthoredConfig.DisplayName;
        if (_displayedName == name && _disabled == disabled) return;
        _disabled = disabled; _displayedName = name;
        Color color = _disabled ? _style.DisabledColor : _style.NormalColor;
        if (GodotObject.IsInstanceValid(_screen)) _screen.Present(name, _disabled, color);
        QueueRedraw();
    }

    // =========================================================
    // Hide on deselect/Inspector close/unload; don't track every tower or poll any gameplay state.
    public void Clear()
    {
        _tower = null; _displayedName = null; _elapsed = _redrawTimer = 0f;
        Hide(); SetProcess(false);
        if (GodotObject.IsInstanceValid(_screen)) _screen.Clear();
    }

    // =========================================================
    // Only one selected hologram pulses, capped at a modest presentation redraw frequency.
    public override void _Process(double delta)
    {
        if (!GodotObject.IsInstanceValid(_tower) || _tower.IsQueuedForDeletion()) { Clear(); return; }
        _elapsed += (float)delta;
        _redrawTimer += (float)delta;
        if (_redrawTimer < 1f / Mathf.Clamp(_style.RedrawHertz, 8, 30)) return;
        _redrawTimer = 0f;
        QueueRedraw();
    }

    // =========================================================
    // Draw the exact grid footprint (including 2x2/3x2 buildings), with soft fill and bright pulsing corners.
    public override void _Draw()
    {
        if (_tower == null || _grid == null || _style == null || !_style.HologramEnabled) return;
        float cell = _grid.Config.CellSize;
        Vector2 footprint = (Vector2)_tower.Footprint * cell;
        float inset = Mathf.Clamp(_style.FootprintInsetPixels, 0f, Mathf.Min(footprint.X, footprint.Y) * 0.24f);
        Rect2 bounds = new((Vector2)_tower.GridCell * cell + Vector2.One * inset,
            footprint - Vector2.One * (2f * inset));
        float cycle = Mathf.Tau * _elapsed / Mathf.Max(0.5f, _style.PulseSeconds);
        float pulse = 0.8f + 0.2f * Mathf.Sin(cycle);
        Color baseColor = _disabled ? _style.DisabledColor : _style.NormalColor;
        Color border = WithAlpha(baseColor, _style.BorderOpacity * pulse);
        Color fill = WithAlpha(baseColor, _style.FootprintFillOpacity * pulse);
        Color corners = WithAlpha(baseColor, Mathf.Min(1f, _style.BorderOpacity * (pulse + 0.2f)));
        float x1 = bounds.Position.X, y1 = bounds.Position.Y;
        float x2 = bounds.End.X, y2 = bounds.End.Y;
        float length = Mathf.Min(Mathf.Max(4f, _style.CornerLengthPixels), Mathf.Min(bounds.Size.X, bounds.Size.Y) * 0.35f);

        DrawSetTransformMatrix(GlobalTransform.AffineInverse() * _grid.GlobalTransform);
        DrawRect(bounds, fill);
        DrawRect(bounds.Grow(1.8f), WithAlpha(baseColor, 0.11f * pulse), filled: false, width: 3.6f);
        DrawRect(bounds, border, filled: false, width: 1.4f);
        DrawLine(new Vector2(x1, y1), new Vector2(x1 + length, y1), corners, 2.4f);
        DrawLine(new Vector2(x1, y1), new Vector2(x1, y1 + length), corners, 2.4f);
        DrawLine(new Vector2(x2, y1), new Vector2(x2 - length, y1), corners, 2.4f);
        DrawLine(new Vector2(x2, y1), new Vector2(x2, y1 + length), corners, 2.4f);
        DrawLine(new Vector2(x1, y2), new Vector2(x1 + length, y2), corners, 2.4f);
        DrawLine(new Vector2(x1, y2), new Vector2(x1, y2 - length), corners, 2.4f);
        DrawLine(new Vector2(x2, y2), new Vector2(x2 - length, y2), corners, 2.4f);
        DrawLine(new Vector2(x2, y2), new Vector2(x2, y2 - length), corners, 2.4f);
        DrawSetTransformMatrix(Transform2D.Identity);
    }

    // =========================================================
    // This UI child lives under the HUD, not under the world presenter.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_screen)) _screen.QueueFree();
        _screen = null;
    }

    private static Color WithAlpha(Color c, float alpha) => new(c.R, c.G, c.B, Mathf.Clamp(alpha, 0f, 1f));
}
