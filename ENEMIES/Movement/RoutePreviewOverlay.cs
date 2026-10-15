// Level-debug-owned F3 panel and cached world polylines; never creates gameplay enemies or modifies their routes.
using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public partial class RoutePreviewOverlay : Node2D
{
    public RoutePreviewJob Job { get; private set; }
    public bool IsOpen => _canvas?.Visible == true;
    public int MaxCalculationsPerFrame { get; set; } = 1;
    public double MaxMillisecondsPerFrame { get; set; } = 1;
    private SpawnManager _spawns;
    private NavigationGrid _nav;
    private CanvasLayer _canvas;
    private OptionButton _portals, _enemies, _mode;
    private SpinBox _count;
    private CheckButton _comparison;
    private RichTextLabel _legend;
    private readonly List<Marker2D> _exits = new();
    private readonly List<EnemyDefinition> _definitions = new();
    private int _inputStamp;
    private Vector2 _start;

    #region Panel and Input
    // =========================================================
    // Only levels with enabled debug/services construct the panel; map-only scenes remain inert.
    public void Initialize(SpawnManager spawns)
    {
        _spawns = spawns; _nav = spawns.Paths.Navigation; ZIndex = 90;
        _canvas = new CanvasLayer { Name = "PreviewUI", Layer = 20, Visible = false }; AddChild(_canvas);
        PanelContainer panel = new() { Name = "Panel", Position = new(16, 190), Size = new(390, 480) };
        UIMasterConfig theme = GD.Load<UIMasterConfig>("res://UI/DefaultUIConfig.tres");
        panel.AddThemeStyleboxOverride("panel", UIButtonFactory.Box(theme.PanelColor, theme.AccentColor)); _canvas.AddChild(panel);
        VBoxContainer box = new(); panel.AddChild(box);
        box.AddChild(UIButtonFactory.Label("ROUTE PREVIEW · F3", 18));
        _portals = Choice(box, "Portal", "Portal");
        _enemies = Choice(box, "Enemy type", "Enemy");
        HBoxContainer samples = new(); box.AddChild(samples); samples.AddChild(UIButtonFactory.Label("Path samples", 14));
        _count = new SpinBox { Name = "Samples", MinValue = 1, MaxValue = 12, Step = 1, Value = 8, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        samples.AddChild(_count);
        _mode = Choice(box, "Display", "Mode"); _mode.AddItem("Final Refined"); _mode.AddItem("Raw A*");
        _comparison = new CheckButton { Name = "Comparison", Text = "Show neutral shortest route", ButtonPressed = true };
        box.AddChild(_comparison); _comparison.Toggled += _ => { QueueRedraw(); UpdateLegend(); };
        HBoxContainer actions = new(); box.AddChild(actions);
        Button simulate = UIButtonFactory.Create("Simulate Paths", Simulate, UIButtonStyle.Action, 32); simulate.Name = "Simulate"; actions.AddChild(simulate);
        Button clear = UIButtonFactory.Create("Clear Paths", () => Clear("Paths cleared."), UIButtonStyle.Compact, 32); clear.Name = "Clear"; actions.AddChild(clear);
        _legend = new RichTextLabel { Name = "Legend", BbcodeEnabled = true, CustomMinimumSize = new(360, 155),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill, ScrollActive = true, FitContent = false };
        _legend.AddThemeFontSizeOverride("normal_font_size", 13); box.AddChild(_legend);
        _portals.ItemSelected += _ => Clear("Portal changed. Simulate Paths to refresh.");
        _enemies.ItemSelected += _ => Clear("Enemy changed. Simulate Paths to refresh.");
        _mode.ItemSelected += _ => Clear("Display changed. Simulate Paths to refresh.");
        _count.ValueChanged += _ => Clear("Sample count changed. Simulate Paths to refresh.");
        RefreshChoices(); Clear("Choose inputs and click Simulate Paths. No enemies spawn.");
        SetPhysicsProcess(false); Hide();
    }

    // =========================================================
    // Shared compact labeled selector, styled consistently with the existing HUD palette.
    private static OptionButton Choice(VBoxContainer box, string caption, string name)
    {
        HBoxContainer row = new(); box.AddChild(row); row.AddChild(UIButtonFactory.Label(caption, 14));
        OptionButton choice = new() { Name = name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(choice); return choice;
    }

    // =========================================================
    // F3 hides both panel and paths; reopen starts clean and reflects current registered sources.
    public void Toggle()
    {
        bool open = !IsOpen; _canvas.Visible = open; Visible = open; SetPhysicsProcess(open);
        Clear("Choose inputs and click Simulate Paths. No enemies spawn.");
        if (open) RefreshChoices();
    }

    // =========================================================
    // Definitions are read from packed scene state without instantiating even a temporary enemy.
    private void RefreshChoices()
    {
        _portals.Clear(); _exits.Clear();
        WorldLevel level = _nav.Grid.Level;
        if (level.Settings.SpawnMode != LevelConfig.SpawnSourceMode.MapEdges)
            foreach (NodePath path in level.Settings.ActivePortals)
            {
                Marker2D exit = level.GetNodeOrNull<Node2D>(path)?.GetNodeOrNull<Marker2D>("SpawnExit");
                if (exit == null) continue;
                _portals.AddItem(path.ToString()); _exits.Add(exit);
            }
        _enemies.Clear(); _definitions.Clear();
        foreach (PackedScene scene in _spawns.Catalog.Scenes)
        {
            SceneState state = scene.GetState();
            for (int i = 0; i < state.GetNodePropertyCount(0); i++)
                if (state.GetNodePropertyName(0, i) == "Definition" && state.GetNodePropertyValue(0, i).AsGodotObject() is EnemyDefinition definition)
                { _definitions.Add(definition); _enemies.AddItem(definition.DisplayName); break; }
        }
    }
    #endregion

    #region Planning and Diagnostics
    // =========================================================
    // A new job replaces all previous output, so pressing Simulate during calculation is safe.
    public void Simulate()
    {
        Clear("Calculating route plans…");
        if (_portals.Selected < 0 || _enemies.Selected < 0 || _exits.Count == 0 || _definitions.Count == 0)
        { Clear("No registered active portal/enemy. Map-edge-only levels have no portal preview."); return; }
        Marker2D exit = _exits[_portals.Selected];
        if (!GodotObject.IsInstanceValid(exit)) { Clear("Portal removed. Close/reopen F3 to refresh choices."); return; }
        WorldLevel level = _nav.Grid.Level;
        string selectedPath = _portals.GetItemText(_portals.Selected);
        bool active = false;
        if (level.Settings.SpawnMode != LevelConfig.SpawnSourceMode.MapEdges)
            foreach (NodePath path in level.Settings.ActivePortals)
                if (path.ToString() == selectedPath && level.GetNodeOrNull<Node2D>(path)?.GetNodeOrNull<Marker2D>("SpawnExit") == exit) active = true;
        if (!active) { Clear("Active portals changed. Close/reopen F3 to refresh choices."); return; }
        _start = exit.GlobalPosition;
        Job = new(_nav, _definitions[_enemies.Selected], _start, (int)_count.Value, _mode.Selected == 0);
        _inputStamp = InputStamp(); UpdateLegend(); QueueRedraw();
    }

    // =========================================================
    // Only visible debug mode checks its small input signature; no paths regenerate automatically.
    public override void _PhysicsProcess(double delta)
    {
        if (Job == null) return;
        if (_inputStamp != InputStamp()) { Clear("Map/settings/enemy/portal changed. Simulate Paths to refresh."); return; }
        if (Job.Complete) return;
        Job.Step(MaxCalculationsPerFrame, MaxMillisecondsPerFrame); UpdateLegend(); QueueRedraw();
    }

    // =========================================================
    // Relevant Inspector/settings and source changes invalidate both finished and partial results.
    private int InputStamp()
    {
        HashCode h = new(); h.Add(_nav.Revision); h.Add(RoutePreviewJob.SettingsHash(_nav.Config));
        h.Add(_nav.Grid.Level.Settings.SpawnMode);
        foreach (NodePath path in _nav.Grid.Level.Settings.ActivePortals) h.Add(path.ToString());
        foreach (Vector2I goal in _nav.Goals) h.Add(goal);
        if (_portals.Selected >= 0 && _portals.Selected < _exits.Count)
        { Marker2D exit = _exits[_portals.Selected]; h.Add(GodotObject.IsInstanceValid(exit) ? exit.GlobalPosition : Vector2.Inf); }
        if (_enemies.Selected >= 0 && _enemies.Selected < _definitions.Count)
        {
            EnemyDefinition d = _definitions[_enemies.Selected]; h.Add(d.RadiusPixels); h.Add(d.AllowPathDiversity);
            h.Add(d.AllowPathOffsets); h.Add(d.AllowCorridorVariation);
        }
        return h.ToHashCode();
    }

    // =========================================================
    // Clearing cancels pending work immediately; the next run cannot publish old results.
    public void Clear(string message)
    { Job = null; if (_legend != null) _legend.Text = message; QueueRedraw(); }

    // =========================================================
    // Colors match polylines; duplicate samples remain honest and independently labeled.
    private void UpdateLegend()
    {
        if (Job == null) return;
        StringBuilder text = new(); text.AppendLine(Job.Message);
        if (_nav.Config.CongestionInfluence > 0) text.AppendLine("Congestion weights: calculation-time snapshot.");
        if (Job.Neutral.Length > 0) text.AppendLine($"[color=#ffffff]Neutral shortest: {Job.NeutralLength:0} px[/color]");
        else text.AppendLine("Neutral shortest: unavailable / calculating.");
        foreach (RoutePreviewJob.Result r in Job.Results)
        {
            string extra = Job.NeutralLength > 0 ? $" ({(r.Length / Job.NeutralLength - 1) * 100:+0.0;-0.0;0.0}% distance)" : "";
            string duplicate = r.DuplicateOf > 0 ? $" · same as #{r.DuplicateOf}" : "";
            text.AppendLine($"[color=#{SampleColor(r.Sample).ToHtml(false)}]#{r.Sample} · {(r.Profile < 0 ? "neutral" : "profile " + r.Profile)} · {r.Length:0}px{extra}{duplicate}[/color]");
        }
        text.Append("Portal center SpawnExit → Citadel; spawn spread and traffic timing are not simulated.");
        _legend.Text = text.ToString();
    }
    #endregion

    #region Cached Drawing
    // =========================================================
    // One cached draw owner; canvas clipping handles off-screen geometry without segment nodes or per-frame rebuilding.
    public override void _Draw()
    {
        if (!IsOpen || Job == null) return;
        if (_comparison.ButtonPressed && Job.Neutral.Length > 0) DrawRoute(Job.Neutral, Colors.White, 4);
        foreach (RoutePreviewJob.Result result in Job.Results) DrawRoute(result.Points, SampleColor(result.Sample), 2);
        DrawCircle(ToLocal(_start), 7, Colors.White);
        DrawString(ThemeDB.FallbackFont, ToLocal(_start) + new Vector2(10, -10), "PORTAL", fontSize: 16);
        foreach (Vector2I goal in _nav.Goals)
        {
            Vector2 point = ToLocal(_nav.Grid.CellToWorldCenter(goal)); DrawCircle(point, 6, Colors.White);
            DrawString(ThemeDB.FallbackFont, point + new Vector2(10, -10), "CITADEL", fontSize: 16);
        }
    }
    // =========================================================
    // Include the actual unsnapped SpawnExit connector and convert world coordinates into this level-owned canvas.
    private void DrawRoute(Vector2[] points, Color color, float width)
    {
        Vector2[] local = new Vector2[points.Length + 1]; local[0] = ToLocal(_start);
        for (int i = 0; i < points.Length; i++) local[i + 1] = ToLocal(points[i]);
        DrawPolyline(local, color, width, true);
    }
    // =========================================================
    // Stable sample palette independent of any gameplay RNG.
    private static Color SampleColor(int sample) => Color.FromHsv((sample * 0.618034f) % 1, 0.65f, 1);
    #endregion
}
