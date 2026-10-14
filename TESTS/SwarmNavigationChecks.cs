// Deterministic alternative-corridor, weight isolation and 500-ship budget/clearance checks.
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

public static class SwarmNavigationChecks
{
    // =========================================================
    // Called by the integration scene; all terrain changes are in-memory test fixtures.
    public static void Run(WorldGrid grid, PathRequestManager paths, SpawnManager spawns)
    {
        EnemyMasterConfig config = paths.Config;
        bool enabled = config.EnablePathDiversity;
        float strength = config.PathDiversityStrength, congestion = config.CongestionInfluence;
        int profiles = config.RouteProfileCount;
        foreach (Enemy enemy in spawns.Registry.Living.ToArray()) enemy.TakeDamage(10000);
        grid.Level.Ground.Clear(); grid.Level.Walls.Clear();
        for (int y = 0; y < grid.Config.HeightCells; y++)
            for (int x = 0; x < grid.Config.WidthCells; x++) grid.Level.Walls.SetCell(new(x, y), 0, new(0, 0));
        for (int y = 2; y <= 28; y++)
            for (int x = 2; x <= 38; x++)
            {
                if (x == 20 && !(y >= 7 && y <= 9) && !(y >= 21 && y <= 23)) continue;
                grid.Level.Walls.EraseCell(new(x, y)); grid.Level.Ground.SetCell(new(x, y), 0, new(2, 0));
            }
        Vector2 start = grid.CellToWorldCenter(new(5, 15)); Vector2I goal = new(35, 15);
        grid.Level.GetNode<Node2D>("Portals/EnemyPortal").GlobalPosition = grid.CellToWorldCenter(new(4, 15));
        foreach (Node child in grid.Level.Citadel.GetNode("ApproachPoints").GetChildren())
            ((Node2D)child).GlobalPosition = grid.CellToWorldCenter(goal);
        grid.Level.Settings.SpawnMode = LevelConfig.SpawnSourceMode.Portals;
        grid.RebuildTerrain(); config.EnablePathDiversity = true; config.CongestionInfluence = 0;
        NavigationGrid nav = paths.Navigation;
        HashSet<int> corridors = new();
        for (int id = 0; id < config.RouteProfileCount; id++)
        {
            Vector2[] route = nav.FindPath(start, goal, id, out float cost);
            Require(route.Length > 0 && float.IsFinite(cost), "Weighted route missing");
            foreach (Vector2 point in route) Require(nav.HasClearance(point, 12), "Weighted point lacks clearance");
            for (int i = 1; i < route.Length; i++) Require(nav.SegmentClear(route[i - 1], route[i], 12), "Weighted path cuts a corner");
            corridors.Add(grid.WorldToCell(route.First(p => grid.WorldToCell(p).X == 20)).Y < 15 ? 0 : 1);
            Vector2[] repeat = nav.FindPath(start, goal, id, out _);
            Require(route.SequenceEqual(repeat), "Profile is not deterministic");
        }
        Require(corridors.Count == 2, "Default profiles did not distribute across both alternative corridors");
        GD.Print("PASS: default profiles use both corridors, deterministically, with valid clearance");
        config.EnablePathDiversity = false; config.CongestionInfluence = 1;
        Vector2[] baseline = nav.FindPath(start, goal, 0, out _);
        Require(baseline.SequenceEqual(nav.FindPath(start, goal, 3, out _)) && nav.SharedProfileCount == 1,
            "Disabling diversity did not restore the ordinary shared grid");
        config.EnablePathDiversity = true; config.PathDiversityStrength = 0; config.CongestionInfluence = 0;
        Require(baseline.SequenceEqual(nav.FindPath(start, goal, 2, out _)), "Zero-strength weights changed ordinary shortest paths");
        GD.Print("PASS: disabling diversity and zero strength restore ordinary shortest paths");
        PackedScene scene = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn");
        Enemy crowd = spawns.Spawn(scene, true); crowd.Movement.SetPhysicsProcess(false);
        crowd.GlobalPosition = grid.CellToWorldCenter(new(20, 8));
        config.CongestionInfluence = 2; nav.UpdateCongestion(10);
        Vector2[] congested = nav.FindPath(start, goal, 0, out float congestedCost);
        Require(congested.Any(p => grid.WorldToCell(p).X == 20 && grid.WorldToCell(p).Y > 15), "Congestion did not favor the empty corridor");
        config.CongestionInfluence = 0;
        Require(baseline.SequenceEqual(nav.FindPath(start, goal, 0, out _)), "Congestion weights leaked after disabling");
        crowd.TakeDamage(10000); config.CongestionInfluence = 2; nav.UpdateCongestion(10);
        Require(baseline.SequenceEqual(nav.FindPath(start, goal, 0, out _)), "Old crowd snapshot left stale costs");
        GD.Print("PASS: congestion prefers the empty corridor and clears without weight leakage");
        config.PathDiversityStrength = strength; config.CongestionInfluence = 0; nav.Refresh();
        while (paths.PendingCount > 0) paths._PhysicsProcess(1.0 / 60);
        Stopwatch clock = Stopwatch.StartNew();
        Enemy[] ships = new Enemy[500];
        for (int i = 0; i < ships.Length; i++)
        { ships[i] = spawns.Spawn(scene, true); Require(ships[i] != null, "500-ship spawn failed"); ships[i].Movement.SetPhysicsProcess(false); }
        double spawnMs = clock.Elapsed.TotalMilliseconds;
        List<double> frameMs = new();
        while (paths.PendingCount > 0)
        {
            clock.Restart(); paths._PhysicsProcess(1.0 / 60); frameMs.Add(clock.Elapsed.TotalMilliseconds);
            Require(paths.LastFrameCalculations <= config.MaxPathCalculationsPerFrame, "A* budget exceeded");
        }
        Require(ships.Select(s => s.Movement.RouteProfile).Distinct().Count() == profiles, "Profiles not assigned across spawned batch");
        double movementMax = 0;
        for (int tick = 0; tick < 120; tick++)
        {
            clock.Restart();
            foreach (Enemy ship in ships)
            {
                ship.Movement._PhysicsProcess(1.0 / 60);
                Require(nav.HasClearance(ship.GlobalPosition, 12), "Swarm crossed a blocker");
            }
            movementMax = Math.Max(movementMax, clock.Elapsed.TotalMilliseconds);
        }
        grid.TryOccupy(new(10, 15));
        Vector2 before = ships[0].GlobalPosition; ships[0].Movement._PhysicsProcess(1.0 / 60);
        Require(ships[0].GlobalPosition == before && float.IsPositiveInfinity(ships[0].RemainingPathDistance), "Tower revision did not stop stale weighted route");
        while (paths.PendingCount > 0) paths._PhysicsProcess(1.0 / 60);
        Require(float.IsFinite(ships[0].RemainingPathDistance), "Weighted tower repath failed");
        grid.ReleaseOccupancy(new(10, 15));
        foreach (Enemy ship in ships) ship.TakeDamage(10000);
        config.EnablePathDiversity = enabled; config.PathDiversityStrength = strength; config.CongestionInfluence = congestion;
        frameMs.Sort();
        GD.Print($"PASS: 500 assigned ships, budgeted weighted routing, safe movement and tower repath; spawn batch {spawnMs:0.0}ms; path-frame p95 {frameMs[(int)((frameMs.Count - 1) * 0.95)]:0.00}ms / max {frameMs.Max():0.00}ms; movement+clearance max {movementMax:0.00}ms (headless, not a rendered FPS guarantee)");
    }

    // =========================================================
    // Fail the host integration scene on behavioral regression.
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
