// Shared route cache correctness and paired CPU measurements using real navigation/refinement services.
using Godot;
using System;
using System.Diagnostics;
using System.Linq;
using static AmmoLoopFixture;

public partial class SharedRoutePoolChecks : Node
{
    #region Runner
    // =========================================================
    // Private terrain and resources isolate swarm measurements from authored maps, profiles and waves.
    public override async void _Ready()
    {
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                var nav = f.Spawns.Paths.Navigation; var grid = nav.Grid;
                grid.Level.Passable.Clear(); grid.Level.Impassable.Clear(); grid.Level.Interactive.Clear();
                for (int y = 0; y < grid.Config.HeightCells; y++) for (int x = 0; x < grid.Config.WidthCells; x++)
                    grid.Level.Passable.SetCell(new(x, y), 0, new(2, 0));
                grid.RebuildTerrain(); nav.Refresh();
                RawPaths(nav); Refinements(nav); CheckpointChain(f); Swarm(f, 100); Swarm(f, 500); Benchmark(nav);
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: shared raw/refined routes, private arrays, safe jitter connectors, radius/endpoint separation, selective invalidation, settings and bounded storage");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    #endregion

    #region Correctness
    // =========================================================
    // Cache hits preserve native route cost and cannot let a caller mutate another ship's raw path.
    private static void RawPaths(NavigationGrid nav)
    {
        var config = nav.Config; config.EnableSharedRoutePool = true; nav.RoutePool.Clear();
        Vector2 start = nav.Grid.CellToWorldCenter(new(5, 10)); Vector2I goal = new(25, 10);
        var original = nav.FindPath(start, goal, 0, out float a);
        long hits = nav.RoutePool.RawHits;
        var cached = nav.FindPath(start, goal, 0, out float b);
        Require(original.Length > 2 && original.SequenceEqual(cached) && Math.Abs(a - b) < .001 && nav.RoutePool.RawHits == hits + 1, "Equivalent raw request hits with equal cost");
        cached[0] += Vector2.One * 100;
        Require(nav.FindPath(start, goal, 0, out _)[0] == original[0], "Raw arrays are isolated");
        nav.RoutePool.Invalidate(new(80, 70), true); hits = nav.RoutePool.RawHits;
        nav.FindPath(start, goal, 0, out _); Require(nav.RoutePool.RawHits == hits + 1, "Unrelated tower leaves cached raw path");
        Require(nav.Grid.TryOccupy(new(15, 10)), "Fixture blocker accepted"); nav.Refresh();
        var replacement = nav.FindPath(start, goal, 0, out _);
        Require(!replacement.Contains(nav.Grid.CellToWorldCenter(new(15, 10))), "Blocked cached route is not served");
        nav.Grid.ReleaseOccupancy(new(15, 10)); nav.Refresh();
        config.EnableSharedRoutePool = false; hits = nav.RoutePool.RawHits; nav.FindPath(start, goal, 0, out _);
        Require(nav.RoutePool.RawHits == hits, "Master off retains native search"); config.EnableSharedRoutePool = true;
    }
    // =========================================================
    // Finished templates retain endpoint/radius identity; ships own arrays and recomputed connector dependencies.
    private static void Refinements(NavigationGrid nav)
    {
        nav.RoutePool.Clear(); nav.Config.SharedRouteVariants = 8;
        var line = Enumerable.Range(5, 21).Select(x => nav.Grid.CellToWorldCenter(new(x, 10))).ToArray();
        EnemyRoute first = new(), second = new();
        nav.RoutePool.Build(nav, first, line, line[0], 12, 8, true, 1, default);
        long hits = nav.RoutePool.RefinedHits;
        Vector2[] shifted = (Vector2[])line.Clone(); shifted[0] += new Vector2(1, 1);
        nav.RoutePool.Build(nav, second, shifted, shifted[0], 12, 8, true, 9, default);
        Require(nav.RoutePool.RefinedHits == hits + 1 && second.Points[0] == shifted[0] && second.Points[^1] == line[^1], "Same variant reuses tail with actual jittered origin");
        Require(!ReferenceEquals(first.Points, second.Points) && !ReferenceEquals(first.Suffix, second.Suffix), "Per-ship mutable geometry stays private");
        for (int i = 1; i < second.Points.Length; i++) Require(nav.SegmentClear(second.Points[i - 1], second.Points[i], 12), "Shared connectors/links remain safe");
        int entries = nav.RoutePool.RefinedCount;
        nav.RoutePool.Build(nav, new(), line, line[0], 16, 8, true, 1, default);
        Require(nav.RoutePool.RefinedCount == entries + 1, "Different clearance radius cannot alias");
        var endpoint = (Vector2[])line.Clone(); endpoint[^1] += new Vector2(0, 64);
        nav.RoutePool.Build(nav, new(), endpoint, endpoint[0], 12, 8, true, 1, default);
        Require(nav.RoutePool.RefinedCount == entries + 2, "Different endpoint cannot alias");
        entries = nav.RoutePool.RefinedCount; nav.RoutePool.Invalidate(new(80, 70), true);
        Require(nav.RoutePool.RefinedCount == entries, "Unrelated tile preserves variations");
        nav.RoutePool.Invalidate(new(15, 10), true); Require(nav.RoutePool.RefinedCount < entries, "Affected refined route invalidated");
        nav.Config.SharedRouteCacheEntries = 16;
        for (int i = 0; i < 24; i++) nav.RoutePool.Build(nav, new(), line, line[0], 4 + i * .25f, 0, false, 0, default);
        Require(nav.RoutePool.RefinedCount <= 16, "Cache storage is bounded"); nav.Config.SharedRouteCacheEntries = 256;
    }
    #endregion

    #region Checkpoint Obligations
    // =========================================================
    // Shared geometry cannot complete or skip a ship's own required checkpoint chain.
    private static void CheckpointChain(AmmoLoopFixture f)
    {
        var nav = f.Spawns.Paths.Navigation; var grid = nav.Grid;
        bool diversity = nav.Config.EnablePathDiversity; nav.Config.EnablePathDiversity = false; nav.Refresh();
        RouteCheckpoint a = new() { Name = "PoolA", RadiusPixels = 32 }, b = new() { Name = "PoolB", RadiusPixels = 32 };
        grid.Level.AddChild(a); grid.Level.AddChild(b);
        a.GlobalPosition = grid.CellToWorldCenter(new(10, 20)); b.GlobalPosition = grid.CellToWorldCenter(new(15, 20));
        a.NextCheckpoints.Add(new NodePath("../PoolB"));
        var ships = Enumerable.Range(0, 12).Select(_ => f.Target(grid.CellToWorldCenter(new(5, 20)))).ToArray();
        foreach (var ship in ships) { ship.Initialize(f.Spawns.Registry, f.Spawns.Paths, checkpoint: a, requiresCheckpoint: true); Disable(ship); }
        for (int tick = 0; tick < 2000 && ships.Any(e => e.Movement.CompletedCheckpointCount < 2); tick++)
        {
            f.Spawns.Paths._PhysicsProcess(.1);
            foreach (var ship in ships) if (ship.IsAlive)
            {
                ship.Movement._PhysicsProcess(.1);
                Require(!ship.IsAlive || nav.HasClearance(ship.GlobalPosition, ship.Definition.RadiusPixels), "Checkpoint route keeps clearance");
            }
        }
        Require(ships.All(e => e.Movement.CompletedCheckpointCount == 2 && !e.Movement.HeadingToCheckpoint), "Every cached-route ship physically completes both checkpoints");
        foreach (var ship in ships) ship.Free(); a.Free(); b.Free(); nav.Config.EnablePathDiversity = diversity; nav.Refresh();
        GD.Print("PASS: twelve live ships complete two required checkpoints with shared routes");
    }
    #endregion

    #region Live Swarm Safety
    // =========================================================
    // Production path queue and moving ships must retain safe progress across a newly placed tower.
    private static void Swarm(AmmoLoopFixture f, int count)
    {
        var paths = f.Spawns.Paths; var nav = paths.Navigation; var grid = nav.Grid;
        nav.Config.EnableSharedRoutePool = true; nav.Config.EnableCorridorVariation = true;
        nav.Config.CorridorVariationPercent = 100; nav.Config.EnablePathOffsets = true;
        Vector2 start = grid.CellToWorldCenter(new(5, 10));
        var ships = Enumerable.Range(0, count).Select(_ => f.Target(start)).ToArray();
        int ticks = 0;
        while (paths.PendingCount > 0 && ticks++ < 10000) paths._PhysicsProcess(1.0 / 60);
        Require(paths.PendingCount == 0, "Swarm initial requests drain through existing budget");
        Vector2[] line = Enumerable.Range(5, 21).Select(x => grid.CellToWorldCenter(new(x, 10))).ToArray();
        long hits = nav.RoutePool.RefinedHits; long clock = Stopwatch.GetTimestamp();
        foreach (var ship in ships) ship.Movement.AcceptPath(line, nav.Revision);
        double assignments = Stopwatch.GetElapsedTime(clock).TotalMilliseconds;
        Require(nav.RoutePool.RefinedHits > hits, "Actual ships reuse finished templates");
        Require(grid.TryOccupy(new(12, 10)), "Live swarm obstruction placed");
        for (int tick = 0; tick < 600; tick++)
        {
            paths._PhysicsProcess(1.0 / 60);
            Require(paths.LastFrameCalculations <= paths.Config.MaxPathCalculationsPerFrame, "Shared hits retain bounded scheduler count");
            foreach (var ship in ships)
            {
                if (!ship.IsAlive) continue;
                ship.Movement._PhysicsProcess(1.0 / 60);
                Require(!ship.IsAlive || nav.HasClearance(ship.GlobalPosition, ship.Definition.RadiusPixels), "Moving cached-route ships never enter the blocker");
            }
        }
        Require(paths.PendingCount == 0 && ships.All(e => !e.IsAlive || !e.Movement.IsWaitingForPath), "Affected routes settle without permanent waiting");
        GD.Print($"ROUTE SWARM ships={count}; directAssignmentsMs={assignments:0.000}; hits={nav.RoutePool.RefinedHits - hits}; ticks=600; safety=PASS");
        foreach (var ship in ships) ship.Free(); grid.ReleaseOccupancy(new(12, 10));
    }
    #endregion

    #region Paired Benchmark
    // =========================================================
    // Same logical workload and seeds, master off/on; report measured CPU cost without promising desktop FPS.
    private static void Benchmark(NavigationGrid nav)
    {
        var line = Enumerable.Range(5, 41).Select(x => nav.Grid.CellToWorldCenter(new(x, 15))).ToArray();
        nav.Config.CorridorVariationStrength = .65f; nav.Config.CorridorVariationSpacingCells = 3;
        for (int run = 0; run < 3; run++) foreach (bool shared in run % 2 == 0 ? new[] { false, true } : new[] { true, false })
        {
            nav.Config.EnableSharedRoutePool = shared; nav.RoutePool.Clear();
            long hits = nav.RoutePool.RefinedHits, builds = nav.RoutePool.RefinedBuilds;
            long bytes = GC.GetAllocatedBytesForCurrentThread(), clock = Stopwatch.GetTimestamp();
            for (uint i = 0; i < 300; i++) nav.RoutePool.Build(nav, new(), line, line[0], 12, 8, true, i, default);
            double ms = Stopwatch.GetElapsedTime(clock).TotalMilliseconds;
            bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
            GD.Print($"ROUTE POOL run={run + 1}; enabled={shared}; ships=300; ms={ms:0.000}; bytes={bytes}; sharedHits={nav.RoutePool.RefinedHits - hits}; templateBuilds={nav.RoutePool.RefinedBuilds - builds}");
            if (shared) Require(nav.RoutePool.RefinedHits - hits >= 290, "Warm pool actually eliminates repeated refinement");
        }
        nav.Config.EnableSharedRoutePool = true;
    }
    #endregion
}
