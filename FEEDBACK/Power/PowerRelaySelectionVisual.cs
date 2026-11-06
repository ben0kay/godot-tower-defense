// Static selected-only conductive coverage and cached shortest source route; no process, timer or per-frame path work.
using Godot;
using System.Collections.Generic;

public partial class PowerRelaySelectionVisual : Node2D
{
    private readonly List<Vector2I> _cells = new(), _route = new();
    private readonly List<Vector2[]> _tiles = new();
    private readonly List<Vector2> _points = new();
    private int _revision = -1, _tileCount;
    private Transform2D _transform;
    private bool _selected;
    private Label _label;
    private PowerDistributionVisualConfig _style;
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false); ZIndex = 3;
        _style = PowerDistributionVisualConfig.Shared;
        _label = new Label { MouseFilter = Control.MouseFilterEnum.Ignore, Position = new Vector2(40, -70), ZIndex = 1 };
        _label.AddThemeFontSizeOverride("font_size", 13); _label.AddThemeColorOverride("font_color", new Color(.6f, 1, 1));
        _label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, .9f));
        AddChild(_label); Hide();
    }
    public void Present(PowerRelay relay, bool selected)
    {
        _selected = selected; Visible = selected;
        if (!selected) { _revision = -1; _tileCount = 0; _points.Clear(); return; }
        var power = relay.PowerNetworks;
        if (power == null) return;
        int revision = power.DeliveryRevision;
        if (_revision == revision && _transform == GlobalTransform) return;
        _revision = revision; _transform = GlobalTransform;
        bool active = power.CopyRelayPresentation(relay, _cells, _route, out bool truncated);
        _tileCount = _cells.Count;
        while (_tiles.Count < _tileCount) _tiles.Add(new Vector2[4]);
        var grid = power.Grid; int size = grid.Config.CellSize;
        for (int i = 0; i < _tileCount; i++)
        {
            Vector2 corner = (Vector2)_cells[i] * size; var tile = _tiles[i];
            tile[0] = ToLocal(grid.ToGlobal(corner)); tile[1] = ToLocal(grid.ToGlobal(corner + new Vector2(size, 0)));
            tile[2] = ToLocal(grid.ToGlobal(corner + new Vector2(size, size))); tile[3] = ToLocal(grid.ToGlobal(corner + new Vector2(0, size)));
        }
        _points.Clear();
        for (int i = 0; i < _route.Count; i++)
        {
            // Keep turns/endpoints only; a long straight conductive route needs one dashed segment.
            if (i > 0 && i + 1 < _route.Count)
            {
                Vector2I before = _route[i] - _route[i - 1], after = _route[i + 1] - _route[i];
                if ((long)before.X * after.Y == (long)before.Y * after.X && (long)before.X * after.X + (long)before.Y * after.Y > 0) continue;
            }
            _points.Add(ToLocal(grid.CellToWorldCenter(_route[i])));
        }
        _label.Text = active ? (truncated ? "Nearest live source\nIsland supply shared · route preview capped" : "Nearest live source\nIsland supply shared")
            : "Relay inactive — no boosted coverage";
        QueueRedraw();
    }
    public override void _Draw()
    {
        if (!_selected || _style == null) return;
        for (int i = 0; i < _tileCount; i++)
        {
            Vector2[] tile = _tiles[i]; DrawColoredPolygon(tile, _style.CoverageFill);
            for (int j = 0; j < 4; j++) DrawLine(tile[j], tile[(j + 1) % 4], _style.CoverageEdge, 1);
        }
        for (int i = 1; i < _points.Count; i++) DrawDashedLine(_points[i - 1], _points[i], _style.SourceRoute, 1.5f, Mathf.Max(2, _style.DashLength));
    }
}
