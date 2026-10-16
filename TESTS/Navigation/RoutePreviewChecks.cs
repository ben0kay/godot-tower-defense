// End-to-end F3 controls, real navigation previews and gameplay isolation on both campaign levels.
using Godot;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Diagnostics;

public partial class RoutePreviewChecks : Node
{
    // =========================================================
    // Standalone headless runner exercises the actual debug panel callbacks and deterministic planners.
    public override async void _Ready()
    {
        try
        {
            EnemyMasterConfig sixteen = new() { RouteProfileCount = 16 };
            PathRequestManager assignment = new() { Config = sixteen };
            try { for (int i = 0; i < 32; i++) Require(assignment.AssignProfile() == i % 16, "Production assignment cycles all sixteen profile IDs"); }
            finally { assignment.Free(); sixteen.Dispose(); }
            foreach (string scene in new[] { "Main.tscn", "MainLevel002.tscn" }) await CheckLevel(scene);
            GD.Print("PASS: F3 preview UI, deterministic raw/refined paths, budgets, neutral comparison, duplicate reporting, input cancellation and gameplay isolation on both levels");
            await SubsystemBattlefield.DrainFreedFixtures(this); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    // =========================================================
    // Real level services retain their live state while preview samples use independent assignments.
    private async Task CheckLevel(string scene)
    {
        Node2D main = GD.Load<PackedScene>("res://" + scene).Instantiate<Node2D>(); SubsystemBattlefield.DetachSession(main); AddChild(main);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Disable(main);
        WorldGrid grid = main.GetNode<WorldGrid>("World/Grid");
        SpawnManager spawns = main.GetNode<SpawnManager>("World/EnemySystems/Spawning");
        PathRequestManager paths = spawns.Paths; NavigationGrid nav = paths.Navigation;
        LevelDebugController debug = grid.Level.GetNode<LevelDebugController>("Debug");
        RoutePreviewOverlay overlay = debug.RoutePreview;
        Require(overlay != null && !overlay.IsOpen, "Overlay starts hidden");
        var key = new InputEventKey { Keycode = Key.F3, Pressed = true };
        debug._UnhandledKeyInput(key); Require(overlay.IsOpen, "F3 opens panel");
        Node panel = overlay.GetNode("PreviewUI/Panel");
        Button simulate = Find<Button>(panel, "Simulate"), clear = Find<Button>(panel, "Clear");
        OptionButton portal = Find<OptionButton>(panel, "Portal"), enemies = Find<OptionButton>(panel, "Enemy"), mode = Find<OptionButton>(panel, "Mode");
        SpinBox count = Find<SpinBox>(panel, "Samples");
        Require(portal.ItemCount > 0 && enemies.ItemCount == spawns.Catalog.Scenes.Count, "Registered portals and all types are available");
        int nextProfile = Field<int>(paths, "_nextProfile"), living = spawns.Registry.Living.Count;
        uint ordinal = Field<uint>(paths, "_corridorSpawnOrdinal"); long calculations = paths.TotalCalculations;
        int pending = paths.PendingCount; Vector2[] last = null;
        Stopwatch timer = Stopwatch.StartNew();
        foreach (int display in new[] { 1, 0 })
        {
            mode.Select(display); count.Value = 12;
            simulate.EmitSignal(Button.SignalName.Pressed); Run(overlay);
            Require(overlay.Job.Results.Count + overlay.Job.Failed == 12, "All requested samples accounted for");
            Require(overlay.Job.Neutral.Length > 0 && overlay.Job.Results.Count > 0, "Neutral and sample plans exist");
            last = overlay.Job.Results[0].Points;
            if (display == 1 && nav.Config.RouteProfileCount < 12)
                Require(overlay.Job.Results.Any(r => r.DuplicateOf > 0), "Raw profile repetition is reported honestly");
            simulate.EmitSignal(Button.SignalName.Pressed); Run(overlay);
            Require(last.SequenceEqual(overlay.Job.Results[0].Points), "Same settings give stable repeated samples");
        }
        SpinBox strength = Find<SpinBox>(panel, "DiversityStrength"), profiles = Find<SpinBox>(panel, "Profiles");
        CheckButton diversity = Find<CheckButton>(panel, "Diversity");
        EnemyMasterConfig liveConfig = nav.Config; int liveHash = RoutePreviewJob.SettingsHash(liveConfig), livePools = nav.TotalSharedProfileCount;
        Require(count.MaxValue == 50 && profiles.MaxValue == 16 && portal.GetItemText(portal.ItemCount - 1) == "ALL PORTALS", "Expanded Inspector/UI limits and ALL option");
        profiles.Value = 16; strength.Value = 2.5; diversity.ButtonPressed = true;
        portal.Select(portal.ItemCount - 1); mode.Select(1); count.Value = 50;
        Stopwatch allClock = Stopwatch.StartNew(); simulate.EmitSignal(Button.SignalName.Pressed);
        NavigationGrid privatePool = Field<NavigationGrid>(overlay, "_previewNav");
        Require(!ReferenceEquals(privatePool, nav) && !ReferenceEquals(privatePool.Config, liveConfig) && privatePool.SharedProfileCount == 16,
            "Sixteen actual preview grids are privately owned, plus neutral");
        Run(overlay);
        RoutePreviewJob[] all = overlay.PortalJobs.ToArray();
        Require(all.Length == portal.ItemCount - 1 && all.All(j => j.Complete && j.Results.Count + j.Failed == 50), "ALL completes 50 samples for each registered portal");
        Require(all.SelectMany(j => j.Results).Any(r => r.Profile == 15), "Sixteenth weighted profile participates in preview selection");
        Require(ReferenceEquals(nav.Config, liveConfig) && RoutePreviewJob.SettingsHash(liveConfig) == liveHash && nav.TotalSharedProfileCount == livePools,
            "Preview overrides never mutate live config or live A* pool");
        GD.Print($"PREVIEW ALL {scene}: {all.Length} portals x 50 samples, {all.Sum(j => j.TotalCalculations)} queries, {allClock.Elapsed.TotalMilliseconds:0.00}ms total bounded work including private grid setup");
        diversity.ButtonPressed = false; Require(overlay.Job == null, "Preview setting edit cancels all cached portals");
        count.Value = 2; simulate.EmitSignal(Button.SignalName.Pressed); Run(overlay);
        Require(overlay.PortalJobs.All(j => j.Results.All(r => r.Profile == -1)), "Preview diversity OFF uses neutral paths for every portal");
        overlay.ResetSettings();
        Require((int)profiles.Value == Mathf.Clamp(liveConfig.RouteProfileCount, 1, 16) && Mathf.IsEqualApprox((float)strength.Value, liveConfig.PathDiversityStrength)
            && diversity.ButtonPressed == liveConfig.EnablePathDiversity && overlay.Job == null, "Reset restores game values without writing them");
        portal.Select(0); count.Value = 12;
        Require(nextProfile == Field<int>(paths, "_nextProfile") && ordinal == Field<uint>(paths, "_corridorSpawnOrdinal")
            && living == spawns.Registry.Living.Count && calculations == paths.TotalCalculations && pending == paths.PendingCount,
            "Preview changes no live assignments, registry, queues or gameplay calculation count");
        simulate.EmitSignal(Button.SignalName.Pressed); overlay._PhysicsProcess(0);
        count.Value = 2; Require(overlay.Job == null, "Input changes cancel partially completed output");
        simulate.EmitSignal(Button.SignalName.Pressed); Run(overlay); Require(overlay.Job.Results.Count + overlay.Job.Failed == 2, "New input replaces old run");
        clear.EmitSignal(Button.SignalName.Pressed); Require(overlay.Job == null, "Clear removes results");
        simulate.EmitSignal(Button.SignalName.Pressed);
        debug._UnhandledKeyInput(new InputEventKey { Keycode = Key.F3, Pressed = true, Echo = true });
        Require(overlay.IsOpen, "Key repeat ignored");
        debug._UnhandledKeyInput(key); Require(!overlay.IsOpen && overlay.Job == null && !overlay.IsPhysicsProcessing(), "F3 closes, hides and cancels work");
        debug._UnhandledKeyInput(key); debug.Enabled = false;
        Require(!overlay.IsOpen && !overlay.IsPhysicsProcessing(), "Disabling level Debug closes overlay without polling");
        debug.Enabled = true;
        EnemyDefinition fixture = new() { RadiusPixels = 12, AllowPathDiversity = false, AllowPathOffsets = false, AllowCorridorVariation = false };
        try
        {
            Vector2 start = grid.Level.GetNode<Node2D>(grid.Level.Settings.ActivePortals[0]).GetNode<Marker2D>("SpawnExit").GlobalPosition;
            RoutePreviewJob neutral = new(nav, fixture, start, 3, true); Complete(neutral);
            Vector2[] neutralConnected = start.DistanceSquaredTo(neutral.Neutral[0]) > 0.0001f
                ? new[] { start }.Concat(neutral.Neutral).ToArray() : neutral.Neutral;
            Require(neutral.Results.All(r => r.Profile == NavigationGrid.NeutralProfile && r.Points.SequenceEqual(neutralConnected)),
                "All-false type uses neutral geometry with the production unsnapped-start connector");
            for (int flags = 0; flags < 8; flags++)
            {
                fixture.AllowPathDiversity = (flags & 1) != 0; fixture.AllowPathOffsets = (flags & 2) != 0;
                fixture.AllowCorridorVariation = (flags & 4) != 0;
                foreach (bool refined in new[] { false, true })
                {
                    RoutePreviewJob matrix = new(nav, fixture, start, 2, refined); Complete(matrix);
                    Require(matrix.Results.Count + matrix.Failed == 2, "Every permission/mode combination completes");
                    foreach (RoutePreviewJob.Result result in matrix.Results)
                        Require(result.Profile == (!fixture.AllowPathDiversity || !nav.Config.EnablePathDiversity ? -1 : result.Sample - 1), "Preview profile respects type/global diversity");
                }
            }
            fixture.AllowPathDiversity = fixture.AllowPathOffsets = fixture.AllowCorridorVariation = false;
            RoutePreviewJob unsnapped = new(nav, fixture, start + new Vector2(2, 1), 1, true); Complete(unsnapped);
            Require(unsnapped.Results.Count == 1 && unsnapped.Results[0].Points[0] == start + new Vector2(2, 1), "Refined geometry preserves actual unsnapped SpawnExit");
            RoutePreviewJob blocked = new(nav, fixture, grid.CellToWorldCenter(new(-1, -1)), 1, false);
            Require(blocked.Complete && blocked.Results.Count == 0, "Invalid source draws no unsafe path");
            RoutePreviewJob stale = new(nav, fixture, start, 12, true); stale.Step();
            bool previous = nav.Config.EnablePathOffsets;
            try { nav.Config.EnablePathOffsets = !previous; stale.Step(); Require(stale.Complete && stale.Results.Count == 0, "Settings changes discard partial results"); }
            finally { nav.Config.EnablePathOffsets = previous; }
            RoutePreviewJob revision = new(nav, fixture, start, 12, false);
            Vector2I obstruction = grid.WorldToCell(neutral.Neutral[neutral.Neutral.Length / 2]);
            Require(grid.TryOccupy(obstruction), "Runtime tower obstruction fixture");
            try { revision.Step(); Require(revision.Complete && revision.Results.Count == 0, "Navigation edit discards old job"); }
            finally { grid.ReleaseOccupancy(obstruction); }
            Vector2I[] goals = nav.Goals.ToArray(); nav.Goals.Clear();
            try { RoutePreviewJob none = new(nav, fixture, start, 1, false); Require(none.Complete && none.Results.Count == 0, "No goals produces diagnostic only"); }
            finally { nav.Goals.AddRange(goals); }
            var population = new System.Collections.Generic.List<Enemy>();
            try
            {
                for (int i = spawns.Registry.Living.Count; i < 500; i++)
                { Enemy enemy = spawns.Spawn("basic_mk1", true); Require(enemy != null, "500-ship debug fixture spawn"); enemy.Movement.SetPhysicsProcess(false); population.Add(enemy); }
                int queued = paths.PendingCount; long calls = paths.TotalCalculations; Stopwatch perf = Stopwatch.StartNew();
                RoutePreviewJob busy = new(nav, fixture, start, 12, true); Complete(busy);
                Require(paths.PendingCount == queued && paths.TotalCalculations == calls && spawns.Registry.Living.Count == 500, "Preview with 500 ships leaves live requests untouched");
                if (!overlay.IsOpen) overlay.Toggle();
                portal.Select(portal.ItemCount - 1); count.Value = 50; profiles.Value = 16; strength.Value = 1.5; diversity.ButtonPressed = true;
                perf.Restart(); simulate.EmitSignal(Button.SignalName.Pressed); Run(overlay);
                Require(overlay.PortalJobs.All(j => j.Results.Count + j.Failed == 50) && paths.PendingCount == queued && paths.TotalCalculations == calls,
                    "ALL/50/16 with 500 enemies preserves every live request");
                GD.Print($"PREVIEW ALL500 {scene}: {overlay.PortalJobs.Count} portals x 50 samples, {perf.Elapsed.TotalMilliseconds:0.00}ms bounded work; live queue unchanged");
                overlay.Toggle();

            }
            finally { foreach (Enemy enemy in population) enemy.Free(); }
        }
        finally { fixture.Dispose(); }
        GD.Print($"PASS: {scene} preview controls/isolation; {timer.Elapsed.TotalMilliseconds:0.00}ms multi-run fixture (not FPS)");
        main.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    // =========================================================
    // Bound each debug tick independently and ensure every A* query fits the selected count cap.
    private static void Run(RoutePreviewOverlay overlay)
    {
        for (int i = 0; overlay.Job != null && !overlay.Job.Complete && i < 10000; i++)
        { overlay._PhysicsProcess(1.0 / 60); Require(overlay.Job == null || overlay.Job.LastCalculations <= 1, "Debug A* count cap"); }
        Require(overlay.Job != null && overlay.Job.Complete, "Preview completes within bounded work");
    }
    // =========================================================
    // Exercise model-only fixtures without involving world physics ticks or live path queues.
    private static void Complete(RoutePreviewJob job)
    { for (int i = 0; !job.Complete && i < 1000; i++) job.Step(); Require(job.Complete, "Model completion"); }
    // =========================================================
    // Locate real controls by stable names without relying on layout implementation details.
    private static T Find<T>(Node root, string name) where T : Node
    {
        if (root is T value && root.Name == name) return value;
        foreach (Node child in root.GetChildren()) { T found = Find<T>(child, name); if (found != null) return found; }
        return null;
    }
    // =========================================================
    // Test-only access verifies private production assignment streams are untouched.
    private static T Field<T>(object value, string name) => (T)value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
    // =========================================================
    // Freeze production nodes so the fixture can compare before/after state exactly.
    private static void Disable(Node root)
    { root.SetProcess(false); root.SetPhysicsProcess(false); foreach (Node child in root.GetChildren()) Disable(child); }
    // =========================================================
    // Fail the runner immediately on a UI, isolation or navigation regression.
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
