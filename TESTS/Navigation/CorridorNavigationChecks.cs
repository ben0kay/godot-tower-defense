// Focused N4 geometry, live repathing and 100/500-ship CPU benchmarks; headless timings are not rendered FPS.
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

public partial class CorridorNavigationChecks : Node
{
    private readonly record struct Fixture(Node2D Main, WorldGrid Grid, SpawnManager Spawns, PathRequestManager Paths,
        EnemyMasterConfig Config, Vector2 Start, Vector2I Goal);
    // =========================================================
    // Standalone fixture supplements the existing integrated regression runner.
    public override async void _Ready()
    { try { await Run(this); await SubsystemBattlefield.DrainFreedFixtures(this); GetTree().Quit(); } catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); } }

    // =========================================================
    // Isolated runtime maps/settings protect original gameplay fixtures and authored resources.
    public static async System.Threading.Tasks.Task Run(Node owner)
    {
        Fixture correctness = await Create(owner, false, 3);
        try { MovementPermissionChecks.Run(correctness.Main, correctness.Spawns, correctness.Start, correctness.Goal); Geometry(correctness); }
        finally { await Release(owner, correctness); }
        foreach (bool winding in new[] { false, true })
            foreach (int population in new[] { 100, 500 })
                foreach (int percent in new[] { 0, 50, 100 })
                {
                    Fixture fixture = await Create(owner, winding, 3);
                    try { Benchmark(fixture, population, percent, winding); }
                    finally { await Release(owner, fixture); }
                }
        GD.Print("PASS: N4 straight/winding geometry, parity/stability, safe dynamic tower repathing and 0/50/100% 100/500-ship benchmarks");
    }
    // =========================================================
    // Release fixture-owned resource copies on the main thread after deferred scene teardown.
    // Repeated stress worlds must not leave script-backed copies for native finalizer-thread disposal.
    private static async System.Threading.Tasks.Task Release(Node owner, Fixture fixture)
    {
        LevelConfig settings = fixture.Grid.Level.Settings;
        fixture.Main.QueueFree();
        await owner.ToSignal(owner.GetTree(), SceneTree.SignalName.ProcessFrame);
        fixture.Config.Dispose(); settings.Dispose();
    }

    // =========================================================
    // Paint an isolated bounded three-cell band or right-angle passage, keeping a solid Citadel beyond the goal.
    private static async System.Threading.Tasks.Task<Fixture> Create(Node owner, bool winding, int width)
    {
        Node2D main = GD.Load<PackedScene>("res://Main.tscn").Instantiate<Node2D>(); SubsystemBattlefield.DetachSession(main); owner.AddChild(main);
        await owner.ToSignal(owner.GetTree(), SceneTree.SignalName.ProcessFrame);
        await owner.ToSignal(owner.GetTree(), SceneTree.SignalName.ProcessFrame);
        Disable(main);
        WorldGrid grid = main.GetNode<WorldGrid>("World/Grid"); SpawnManager spawns = main.GetNode<SpawnManager>("World/EnemySystems/Spawning");
        PathRequestManager paths = spawns.Paths;
        EnemyMasterConfig config = (EnemyMasterConfig)paths.Config.Duplicate(true); paths.Config = config; paths.Navigation.Config = config;
        grid.Level.Settings = (LevelConfig)grid.Level.Settings.Duplicate(true);
        config.EnablePathDiversity = false; config.EnablePathOffsets = false; config.CongestionInfluence = 0;
        config.PortalSpawnOffsetPixels = 0; config.MaxActiveEnemies = 1000; config.MaxPathCalculationsPerFrame = 8;
        config.MaxPathfindingMillisecondsPerFrame = 2; config.MaxRouteValidationsPerFrame = 16; config.MaxRouteValidationMillisecondsPerFrame = 0.5f;
        config.EnableCorridorVariation = true; config.CorridorVariationPercent = 100; config.CorridorVariationStrength = 0.65f;
        config.CorridorVariationSpacingCells = 3; config.RoutePreferenceSeed = 12345;
        grid.Level.Passable.Clear(); grid.Level.Impassable.Clear(); grid.Level.Interactive?.Clear();
        for (int y = 0; y < grid.Config.HeightCells; y++)
            for (int x = 0; x < grid.Config.WidthCells; x++) grid.Level.Impassable.SetCell(new(x, y), 0, new(0, 0));
        int half = width / 2;
        if (!winding) Carve(grid, 4, 46, 12 - half, 12 + half);
        else { Carve(grid, 4, 22, 12 - half, 12 + half); Carve(grid, 20 - half, 20 + half, 12 - half, 24 + half); Carve(grid, 20 - half, 46, 24 - half, 24 + half); }
        Vector2 start = grid.CellToWorldCenter(new(6, 12)); Vector2I goal = new(44, winding ? 24 : 12);
        grid.Level.GetNode<Node2D>("Portals/EnemyPortal").GlobalPosition = start;
        grid.Level.Citadel.GlobalPosition = grid.CellToWorldCenter(goal + new Vector2I(2, -1));
        foreach (Node child in grid.Level.Citadel.GetNode("ApproachPoints").GetChildren()) ((Node2D)child).GlobalPosition = grid.CellToWorldCenter(goal);
        grid.Level.Settings.SpawnMode = LevelConfig.SpawnSourceMode.Portals;
        grid.Level.Settings.ActivePortals = new() { new NodePath("Portals/EnemyPortal") };
        grid.RebuildTerrain();
        return new(main, grid, spawns, paths, config, start, goal);
    }
    // =========================================================
    // Toggle parity, distinct continuous preferences, endpoint/distance/corner safety and stable repaths.
    private static void Geometry(Fixture f)
    {
        Vector2[] logical = f.Paths.Navigation.FindPath(f.Start, f.Goal);
        Require(logical.Length > 20, "Straight fixture has enough corridor for sustained variation");
        Enemy ship = f.Spawns.Spawn("basic_mk1", true); ship.Movement.SetPhysicsProcess(false);
        uint seed = ship.Movement.CorridorSeed;
        f.Config.EnablePathOffsets = true; f.Config.EnableCorridorVariation = false;
        ship.Movement.AcceptPath(logical, f.Grid.NavigationRevision);
        EnemyRoute legacy = new(); legacy.Build(logical, f.Start, f.Paths.Navigation, 12, ship.Movement.LateralOffsetPixels);
        Require(ship.Movement.Waypoints.SequenceEqual(legacy.Points) && ship.Movement.CorridorSamplingChecks == 0,
            "Disabled master preserves exact existing offset builder with no N4 samples");
        f.Config.EnablePathOffsets = false; f.Config.EnableCorridorVariation = true;
        ship.Movement.AcceptPath(logical, f.Grid.NavigationRevision);
        Vector2[] first = (Vector2[])ship.Movement.Waypoints.Clone();
        ship.Movement.AcceptPath(logical, f.Grid.NavigationRevision);
        Require(ship.Movement.UsesCorridorVariation && ship.Movement.CorridorSeed == seed && first.SequenceEqual(ship.Movement.Waypoints),
            "Forced repath retains per-ship selection/seed and coherent preferences");
        f.Config.CorridorVariationPercent = 0;
        Enemy unselected = f.Spawns.Spawn("basic_mk1", true); unselected.Movement.SetPhysicsProcess(false);
        unselected.Movement.AcceptPath(logical, f.Grid.NavigationRevision);
        Require(!unselected.Movement.UsesCorridorVariation && unselected.Movement.CorridorSamplingChecks == 0 && unselected.Movement.Waypoints.SequenceEqual(logical),
            "Live zero-percent spawn keeps exact ordinary route");
        f.Config.CorridorVariationPercent = 100; unselected.Movement.AcceptPath(logical, f.Grid.NavigationRevision);
        Require(!unselected.Movement.UsesCorridorVariation && unselected.Movement.CorridorSamplingChecks == 0, "Percentage edits never reroll an existing ship");
        unselected.Free();
        int upper = 0, lower = 0, center = 0; float max = 0;
        for (uint i = 0; i < 96; i++)
        {
            EnemyRoute route = new(); route.BuildCorridor(logical, f.Start, f.Paths.Navigation, 12, 0, CorridorPreference.Seed(i, 12345), f.Config);
            Validate(route.Points, f.Start, f.Paths.Navigation, 12);
            Require(route.Points[0] == logical[0] && route.Points[^1] == logical[^1], "N4 preserves origin/goal");
            float deviation = route.Points[logical.Length / 2].Y - logical[logical.Length / 2].Y;
            if (deviation < -12) upper++; else if (deviation > 12) lower++; else center++;
            max = Mathf.Max(max, Mathf.Abs(deviation));
            Require(route.Suffix[0] <= Length(logical) * 1.35f + 0.01f, "N4 bounded detour length");
            Require(Mathf.Abs(route.Suffix[0] - Length(route.Points)) < 0.01f, "Distance cache reflects actual traveled geometry");
        }
        Require(upper > 5 && lower > 5 && center > 5 && max > 25, "Same-source ships populate upper/lower/central corridor space beyond eight pixels");
        float expected = Length(first);
        Require(Mathf.Abs(ship.RemainingPathDistance - expected) < 0.01f, "First target distance uses varied travel geometry");
        // Regression: sampled clearance used to miss this small grazing interval after a new block.
        f.Grid.TryOccupy(new(12, 12));
        Vector2 corner = f.Grid.CellToWorldCenter(new(12, 12)) - Vector2.One * f.Grid.Config.CellSize * 0.5f;
        Vector2 grazing = corner + new Vector2(-7.37354f, -9.4731f), exit = f.Grid.CellToWorldCenter(new(12, 11));
        Require(f.Paths.Navigation.HasClearance(grazing, 12) && !CorridorClearance.ConnectionClear(f.Paths.Navigation, grazing, exit, 12),
            "N4 analytic swept disc rejects a between-sample grazing collision");
        ship.GlobalPosition = grazing;
        typeof(EnemyMovement).GetField("_travelDirection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .SetValue(ship.Movement, new Vector2(0.8679793f, -0.49660036f));
        ship.Movement.AcceptPath(f.Paths.Navigation.FindPath(grazing, f.Goal), f.Grid.NavigationRevision);
        for (int i = 0; i < 5; i++) ship.Movement._PhysicsProcess(1.0 / 60);
        Require(!ship.Movement.IsWaitingForPath && ship.GlobalPosition.DistanceTo(grazing) > 1 && f.Paths.Navigation.HasClearance(ship.GlobalPosition, 12),
            "Grazing-corner handoff escapes safely without an infinite repath loop");
        f.Grid.ReleaseOccupancy(new(12, 12));
        // Single-cell corridor safely retains the original tiny-offset branch.
        Carve(f.Grid, 4, 46, 11, 13, erase: true); Carve(f.Grid, 4, 46, 12, 12); f.Grid.RebuildTerrain();
        EnemyRoute narrow = new(), oldNarrow = new();
        narrow.BuildCorridor(logical, f.Start, f.Paths.Navigation, 12, 5, seed, f.Config);
        oldNarrow.Build(logical, f.Start, f.Paths.Navigation, 12, 5);
        Require(!narrow.CorridorVariationApplied && narrow.Points.SequenceEqual(oldNarrow.Points), "One-cell passage falls back exactly to normal safe offsets");
        // Open area, larger radius and tiny-radius corner rules all use real clearance.
        Carve(f.Grid, 4, 46, 8, 16); f.Grid.RebuildTerrain();
        foreach (float radius in new[] { 0.1f, 8f, 24f })
        { EnemyRoute route = new(); route.BuildCorridor(logical, f.Start, f.Paths.Navigation, radius, 0, seed, f.Config); Validate(route.Points, f.Start, f.Paths.Navigation, radius); }
        ship.Free();
        GD.Print($"PASS: N4 exact master-off parity, stable forced repaths, corridor upper/lower/center {upper}/{lower}/{center}, peak {max:0.0}px, narrow/open/radius safety");
    }
    // =========================================================
    // Time actual budgeted assignments, isolated refinement and normal moving frames for live ships.
    private static void Benchmark(Fixture f, int population, int percent, bool winding)
    {
        f.Config.CorridorVariationPercent = percent;
        List<Enemy> ships = new(population);
        for (int i = 0; i < population; i++)
        { Enemy enemy = f.Spawns.Spawn("basic_mk1", true); Require(enemy != null, "N4 benchmark spawn failed"); enemy.Movement.SetPhysicsProcess(false); ships.Add(enemy); }
        int selected = ships.Count(e => e.Movement.UsesCorridorVariation);
        Require(percent != 0 || selected == 0, "Zero percent stays ordinary"); Require(percent != 100 || selected == population, "100 percent selects every initialized ship");
        if (percent == 50) Require(selected > population * 0.35 && selected < population * 0.65, "Live 50 percent distribution is plausible");
        List<double> assignments = new();
        for (int i = 0; f.Paths.PendingCount > 0 && i < population * 4; i++)
        {
            f.Paths._PhysicsProcess(1.0 / 60); assignments.Add(f.Paths.LastPathfindingMilliseconds);
            Require(f.Paths.LastFrameCalculations <= f.Config.MaxPathCalculationsPerFrame, "N4 retains strict A* count budget");
        }
        Require(f.Paths.PendingCount == 0 && ships.All(e => e.Movement.Waypoints.Length > 0), "Every live ship receives a valid route");
        Require(ships.Where(e => !e.Movement.UsesCorridorVariation).All(e => e.Movement.CorridorSamplingChecks == 0), "Unselected ships do zero N4 geometry work");
        foreach (Enemy enemy in ships) Validate(enemy.Movement.Waypoints, enemy.GlobalPosition, f.Paths.Navigation, enemy.Definition.RadiusPixels);
        Vector2[] logical = f.Paths.Navigation.FindPath(f.Start, f.Goal);
        double buildTotal = 0, buildMax = 0;
        foreach (Enemy enemy in ships)
        {
            long started = Stopwatch.GetTimestamp(); enemy.Movement.AcceptPath(logical, f.Grid.NavigationRevision);
            double ms = Stopwatch.GetElapsedTime(started).TotalMilliseconds; buildTotal += ms; buildMax = Math.Max(buildMax, ms);
        }
        long calculations = f.Paths.TotalCalculations;
        List<double> moving = new();
        for (int frame = 0; frame < 60; frame++)
        {
            long started = Stopwatch.GetTimestamp(); foreach (Enemy enemy in ships) enemy.Movement._PhysicsProcess(1.0 / 60);
            moving.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
        Require(f.Paths.TotalCalculations == calculations && ships.All(e => e.IsAlive), "Ordinary movement does not calculate A* or stop prematurely");
        // A real new solid intersects future physical dependencies; selected ships must stop/hand off safely.
        Vector2I blocked = new(12, 12); f.Grid.TryOccupy(blocked);
        for (int frame = 0; frame < population + 240; frame++)
        {
            f.Paths._PhysicsProcess(1.0 / 60);
            Require(f.Paths.LastFrameCalculations <= f.Config.MaxPathCalculationsPerFrame, "Dynamic N4 repaths retain budget");
            foreach (Enemy enemy in ships)
            {
                enemy.Movement._PhysicsProcess(1.0 / 60);
                Require(f.Paths.Navigation.HasClearance(enemy.GlobalPosition, enemy.Definition.RadiusPixels), "N4 movement never enters a newly blocked cell");
            }
        }
        if (f.Paths.PendingCount != 0 || ships.Any(e => e.Movement.IsWaitingForPath))
        {
            GD.Print($"N4 pending={f.Paths.PendingCount} waiting={ships.Count(e => e.Movement.IsWaitingForPath)} calcs={f.Paths.TotalCalculations - calculations}");
            foreach (Enemy stuck in ships.Where(e => e.Movement.IsWaitingForPath).Take(3))
                GD.Print($"stuck pos={stuck.GlobalPosition} next={stuck.Movement.NextWaypoint} waypoints={stuck.Movement.Waypoints.Length} seed={stuck.Movement.CorridorSeed} selected={stuck.Movement.UsesCorridorVariation}");
        }
        Require(f.Paths.PendingCount == 0 && ships.All(e => !e.Movement.IsWaitingForPath && e.Movement.Waypoints.Length > 0), "Dynamic block repaths settle without permanent waiting");
        foreach (Enemy enemy in ships) Validate(enemy.Movement.Waypoints.Skip(enemy.Movement.NextWaypoint).ToArray(), enemy.GlobalPosition, f.Paths.Navigation, enemy.Definition.RadiusPixels);
        Require(ships.All(e => e.GlobalPosition.X > f.Grid.CellToWorldCenter(blocked).X), "Ships safely pass the new obstruction");
        f.Grid.ReleaseOccupancy(blocked);
        assignments.Sort(); moving.Sort();
        GD.Print($"N4 BENCH {(winding ? "winding" : "straight")} n={population} pct={percent} selected={selected}: build mean/max {buildTotal / population:0.000}/{buildMax:0.000}ms; path-frame p95/max {P95(assignments):0.00}/{assignments[^1]:0.00}ms; movement-frame p95/max {P95(moving):0.00}/{moving[^1]:0.00}ms");
    }
    // =========================================================
    // Check every cached segment against both radius clearance and strict grid-corner traversal.
    private static void Validate(Vector2[] points, Vector2 start, NavigationGrid nav, float radius)
    { Vector2 from = start; foreach (Vector2 point in points) { Require(nav.HasClearance(point, radius) && nav.ConnectionClear(from, point, radius), "N4 path geometry is unsafe"); from = point; } }
    // =========================================================
    // Physical length is independent from weighted A* costs.
    private static float Length(Vector2[] points) { float length = 0; for (int i = 1; i < points.Length; i++) length += points[i - 1].DistanceTo(points[i]); return length; }
    // =========================================================
    // Sorted nearest-rank timing summary.
    private static double P95(List<double> values) => values[Math.Max(0, (int)Math.Ceiling(values.Count * 0.95) - 1)];
    // =========================================================
    // Fixture terrain edits are setup-only and never saved to authored maps.
    private static void Carve(WorldGrid grid, int left, int right, int top, int bottom, bool erase = false)
    {
        for (int y = top; y <= bottom; y++) for (int x = left; x <= right; x++)
        {
            Vector2I cell = new(x, y);
            if (erase) { grid.Level.Passable.EraseCell(cell); grid.Level.Impassable.SetCell(cell, 0, new(0, 0)); }
            else { grid.Level.Impassable.EraseCell(cell); grid.Level.Passable.SetCell(cell, 0, new(2, 0)); }
        }
    }
    // =========================================================
    // Deterministic manual physics excludes render/timer scheduling from CPU measurements.
    private static void Disable(Node node) { node.SetProcess(false); node.SetPhysicsProcess(false); foreach (Node child in node.GetChildren()) Disable(child); }
    // =========================================================
    // Fail correctness/budget checks while reporting actual performance rather than asserting hardware FPS.
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
