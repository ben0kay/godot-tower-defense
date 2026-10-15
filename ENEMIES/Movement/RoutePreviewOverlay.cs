// Level-debug-owned F3 panel and cached world polylines; never creates gameplay enemies or modifies their routes.
using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public partial class RoutePreviewOverlay : Node2D
{
    public RoutePreviewJob Job { get; private set; }
    public bool IsOpen => _canvas?.Visible == true;
    public int MaxCalculationsPerFrame { get; set; } = 1;
    public double MaxMillisecondsPerFrame { get; set; } = 1;
    private SpawnManager _spawns;
    private NavigationGrid _nav, _previewNav;
    private EnemyMasterConfig _previewSettings;
    public IReadOnlyList<RoutePreviewJob> PortalJobs => _plans.Select(p => p.Job).ToArray();
    private sealed record Plan(string Portal, Vector2 Start, RoutePreviewJob Job, int ColorIndex);
    private readonly List<Plan> _plans = new();
    private int _planIndex;
    private CheckButton _diversity;
    private SpinBox _strength, _profiles;
    private bool _resetting;
    private CanvasLayer _canvas;
    private OptionButton _portals, _enemies, _mode;
    private SpinBox _count;
    private CheckButton _comparison;
    private RichTextLabel _legend;
    private readonly List<Marker2D> _exits = new();
    private readonly List<EnemyDefinition> _definitions = new();
    private int _inputStamp;


    #region Panel and Input
    // =========================================================
    // Only levels with enabled debug/services construct the panel; map-only scenes remain inert.
    public void Initialize(SpawnManager spawns)
    {
        _spawns = spawns; _nav = spawns.Paths.Navigation; ZIndex = 90;
        _canvas = new CanvasLayer { Name = "PreviewUI", Layer = 20, Visible = false }; AddChild(_canvas);
        PanelContainer panel = new() { Name = "Panel", Position = new(16, 190), Size = new(420, 490) };
        UIMasterConfig theme = GD.Load<UIMasterConfig>("res://UI/DefaultUIConfig.tres");
        panel.AddThemeStyleboxOverride("panel", UIButtonFactory.Box(theme.PanelColor, theme.AccentColor)); _canvas.AddChild(panel);
        ScrollContainer scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        panel.AddChild(scroll);
        VBoxContainer box = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; scroll.AddChild(box);
        box.AddChild(UIButtonFactory.Label("ROUTE PREVIEW · F3", 18));
        _portals = Choice(box, "Portal", "Portal");
        _enemies = Choice(box, "Enemy type", "Enemy");
        HBoxContainer samples = new(); box.AddChild(samples); samples.AddChild(UIButtonFactory.Label("Paths per portal", 14));
        _count = new SpinBox { Name = "Samples", MinValue = 1, MaxValue = 50, Step = 1, Value = 8, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        samples.AddChild(_count);
        _mode = Choice(box, "Display", "Mode"); _mode.AddItem("Final Refined"); _mode.AddItem("Raw A*");
        box.AddChild(UIButtonFactory.Label("PREVIEW ONLY · game settings unchanged", 13));
        _diversity = new CheckButton { Name = "Diversity", Text = "Enable Path Diversity" }; box.AddChild(_diversity);
        _strength = Number(box, "Diversity strength", "DiversityStrength", 0, 3, 0.05);
        _profiles = Number(box, "Weighted profiles", "Profiles", 1, 16, 1);
        box.AddChild(UIButtonFactory.Create("Reset to Game Settings", ResetSettings, UIButtonStyle.Compact, 28));
        _diversity.Toggled += value => { if (_resetting) return; _previewSettings.EnablePathDiversity = value; Clear("Preview diversity changed. Simulate Paths to refresh."); };
        _strength.ValueChanged += value => { if (_resetting) return; _previewSettings.PathDiversityStrength = (float)value; Clear("Preview strength changed. Simulate Paths to refresh."); };
        _profiles.ValueChanged += value => { if (_resetting) return; _previewSettings.RouteProfileCount = (int)value; Clear("Preview profiles changed. Simulate Paths to refresh."); };
        ResetSettings();
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
    // Small numeric controls keep experimentation entirely in the debug-owned settings copy.
    private static SpinBox Number(VBoxContainer box, string caption, string name, double min, double max, double step)
    {
        HBoxContainer row = new(); box.AddChild(row); row.AddChild(UIButtonFactory.Label(caption, 14));
        SpinBox value = new() { Name = name, MinValue = min, MaxValue = max, Step = step, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(value); return value;
    }

    // =========================================================
    // Recopy defaults explicitly; never save, assign or mutate the live EnemyMasterConfig resource.
    public void ResetSettings()
    {
        Clear("Preview values reset to current game settings.");
        _previewSettings?.Dispose(); _previewSettings = (EnemyMasterConfig)_nav.Config.Duplicate(true);
        _resetting = true;
        _diversity.ButtonPressed = _previewSettings.EnablePathDiversity;
        _strength.Value = _previewSettings.PathDiversityStrength;
        _profiles.Value = Mathf.Clamp(_previewSettings.RouteProfileCount, 1, 16);
        _resetting = false;
    }

    // =========================================================
    // Dispose all detached native/resource owners before scene teardown can defer them to GC.
    public override void _ExitTree()
    { Clear(""); _previewSettings?.Dispose(); _previewSettings = null; }

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
        if (_exits.Count > 0) _portals.AddItem("ALL PORTALS");
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
        WorldLevel level = _nav.Grid.Level;
        bool all = _portals.Selected == _exits.Count;
        IEnumerable<int> selected = all ? Enumerable.Range(0, _exits.Count) : new[] { _portals.Selected };
        foreach (int index in selected)
        {
            Marker2D exit = _exits[index];
            if (!GodotObject.IsInstanceValid(exit)) { Clear("Portal removed. Close/reopen F3 to refresh choices."); return; }
            string pathName = _portals.GetItemText(index);
            bool active = level.Settings.SpawnMode != LevelConfig.SpawnSourceMode.MapEdges && level.Settings.ActivePortals.Any(path =>
                path.ToString() == pathName && level.GetNodeOrNull<Node2D>(path)?.GetNodeOrNull<Marker2D>("SpawnExit") == exit);
            if (!active) { Clear("Active portals changed. Close/reopen F3 to refresh choices."); return; }
        }
        _previewNav = NavigationGrid.CreatePreview(_nav, _previewSettings);
        foreach (int index in selected)
        {
            Vector2 start = _exits[index].GlobalPosition;
            EnemyPortal portal = _exits[index].GetParent() as EnemyPortal;
            RoutePreviewJob job = new(_previewNav, _definitions[_enemies.Selected], start, (int)_count.Value, _mode.Selected == 0,
                portal?.ResolveCheckpoint(), portal?.RequiresCheckpoint == true);
            _plans.Add(new(_portals.GetItemText(index), start, job, index));
        }
        _planIndex = 0; Job = _plans[0].Job;
        _inputStamp = InputStamp(); UpdateLegend(); QueueRedraw();
    }

    // =========================================================
    // Only visible debug mode checks its small input signature; no paths regenerate automatically.
    public override void _PhysicsProcess(double delta)
    {
        if (Job == null) return;
        if (_inputStamp != InputStamp()) { Clear("Map/settings/enemy/portal changed. Simulate Paths to refresh."); return; }
        if (Job.Complete && _planIndex == _plans.Count - 1) return;
        RoutePreviewJob previous = Job;
        int before = Job.Results.Count + Job.Failed + (Job.Neutral.Length > 0 ? 1 : 0);
        Job.Step(MaxCalculationsPerFrame, MaxMillisecondsPerFrame);
        if (Job.Complete && ++_planIndex < _plans.Count) Job = _plans[_planIndex].Job;
        else if (Job.Complete) _planIndex = _plans.Count - 1;
        int after = previous.Results.Count + previous.Failed + (previous.Neutral.Length > 0 ? 1 : 0);
        if (before != after || previous.Complete) { UpdateLegend(); QueueRedraw(); }
    }

    // =========================================================
    // Relevant Inspector/settings and source changes invalidate both finished and partial results.
    private int InputStamp()
    {
        HashCode h = new(); h.Add(_nav.Revision); h.Add(RoutePreviewJob.SettingsHash(_nav.Config));
        h.Add(RoutePreviewJob.SettingsHash(_previewSettings));
        h.Add(_nav.Grid.Level.Settings.SpawnMode);
        foreach (NodePath path in _nav.Grid.Level.Settings.ActivePortals) h.Add(path.ToString());
        foreach (Vector2I goal in _nav.Goals) h.Add(goal);
        foreach (Marker2D exit in _exits)
        {
            h.Add(GodotObject.IsInstanceValid(exit) ? exit.GlobalPosition : Vector2.Inf);
            if (GodotObject.IsInstanceValid(exit) && exit.GetParent() is EnemyPortal portal)
            { h.Add(portal.CheckpointPath.ToString()); RouteCheckpoint checkpoint = portal.ResolveCheckpoint();
                if (GodotObject.IsInstanceValid(checkpoint)) h.Add(checkpoint.GeometryStamp); }
        }
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
    {
        Job = null; _plans.Clear(); _planIndex = 0;
        _previewNav?.ReleasePreview(); _previewNav = null;
        if (_legend != null) _legend.Text = message; QueueRedraw();
    }

    // =========================================================
    // Colors match polylines; duplicate samples remain honest and independently labeled.
    private void UpdateLegend()
    {
        if (Job == null) return;
        StringBuilder text = new();
        text.AppendLine($"PREVIEW ONLY: Diversity {_previewSettings.EnablePathDiversity}; Strength {_previewSettings.PathDiversityStrength:0.00}; Profiles {_previewSettings.RouteProfileCount}");
        text.AppendLine("Plans, not traffic predictions. Values do not change game settings.");
        if (_previewSettings.CongestionInfluence > 0) text.AppendLine("Congestion: frozen snapshot from Simulate.");
        foreach (Plan plan in _plans)
        {
            RoutePreviewJob job = plan.Job;
            int unique = job.Results.Count(r => r.DuplicateOf == 0);
            string portalColor = PortalColor(plan.ColorIndex).ToHtml(false);
            text.AppendLine($"[color=#{portalColor}]{plan.Portal} · {job.Results.Count}/{(int)_count.Value} paths · {unique} unique · {job.Failed} failed[/color]");
            if (job.RequiresCheckpoint) text.AppendLine("Required checkpoint → Citadel (two planned stages)");
            if (job.Neutral.Length > 0) text.AppendLine($"{(job.RequiresCheckpoint ? "Neutral staged baseline" : "Neutral shortest")}: {job.NeutralLength:0} px");
            if (job.Results.Count == 0) text.AppendLine(job.Message);
            foreach (RoutePreviewJob.Result r in job.Results)
            {
                string extra = job.NeutralLength > 0 ? $" ({(r.Length / job.NeutralLength - 1) * 100:+0.0;-0.0;0.0}% distance)" : "";
                string duplicate = r.DuplicateOf > 0 ? $" · same as #{r.DuplicateOf}" : "";
                text.AppendLine($"[color=#{SampleColor(plan.ColorIndex, r.Sample).ToHtml(false)}]#{r.Sample} · {(r.Profile < 0 ? "neutral" : "profile " + r.Profile)} · {r.Length:0}px{extra}{duplicate}[/color]");
            }
        }
        text.Append("Portal SpawnExit → Citadel. Spawn spread/timing not simulated.");
        _legend.Text = text.ToString();
    }
    #endregion

    #region Cached Drawing
    // =========================================================
    // One cached draw owner; canvas clipping handles off-screen geometry without segment nodes or per-frame rebuilding.
    public override void _Draw()
    {
        if (!IsOpen || Job == null) return;
        foreach (Plan plan in _plans)
        {
            if (_comparison.ButtonPressed && plan.Job.Neutral.Length > 0) DrawRoute(plan.Start, plan.Job.Neutral, Colors.White, 4);
            foreach (RoutePreviewJob.Result result in plan.Job.Results) DrawRoute(plan.Start, result.Points, SampleColor(plan.ColorIndex, result.Sample), 2);
            RouteCheckpoint checkpoint = plan.Job.Checkpoint;
            if (GodotObject.IsInstanceValid(checkpoint))
            {
                Vector2[] ring = new Vector2[65];
                for (int i = 0; i < ring.Length; i++) ring[i] = ToLocal(checkpoint.GlobalPosition + Vector2.FromAngle(i * Mathf.Tau / 64) * checkpoint.RadiusPixels);
                DrawPolyline(ring, PortalColor(plan.ColorIndex), 2, true);
                DrawString(ThemeDB.FallbackFont, ToLocal(checkpoint.GlobalPosition), "CHECKPOINT", fontSize: 16);
                foreach (var result in plan.Job.Results)
                    if (result.CheckpointEntry.HasValue) DrawCircle(ToLocal(result.CheckpointEntry.Value), 4, PortalColor(plan.ColorIndex));
            }
            DrawCircle(ToLocal(plan.Start), 7, PortalColor(plan.ColorIndex));
            DrawString(ThemeDB.FallbackFont, ToLocal(plan.Start) + new Vector2(10, -10), plan.Portal, fontSize: 16);
        }
        foreach (Vector2I goal in _nav.Goals)
        {
            Vector2 point = ToLocal(_nav.Grid.CellToWorldCenter(goal)); DrawCircle(point, 6, Colors.White);
            DrawString(ThemeDB.FallbackFont, point + new Vector2(10, -10), "CITADEL", fontSize: 16);
        }
    }
    // =========================================================
    // Include the actual unsnapped SpawnExit connector and convert world coordinates into this level-owned canvas.
    private void DrawRoute(Vector2 start, Vector2[] points, Color color, float width)
    {
        Vector2[] local = new Vector2[points.Length + 1]; local[0] = ToLocal(start);
        for (int i = 0; i < points.Length; i++) local[i + 1] = ToLocal(points[i]);
        DrawPolyline(local, color, width, true);
    }
    // =========================================================
    // Stable sample palette independent of any gameplay RNG.
    private static Color SampleColor(int portal, int sample) => Color.FromHsv((portal * 0.618034f + sample * 0.002f) % 1, 0.55f + sample % 4 * 0.1f, 1);
    // =========================================================
    // Each portal owns a stable color family in the world and legend.
    private static Color PortalColor(int portal) => Color.FromHsv((portal * 0.618034f) % 1, 0.65f, 1);
    #endregion
}
