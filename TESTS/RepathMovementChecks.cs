// Selective invalidation, fairness, offset safety and audit regression fixtures.
using Godot;
using System;
using System.Linq;

public static class RepathMovementChecks
{
    // =========================================================
    // Use an open in-memory arena to isolate behavior from the designer's current level layout.
    public static void Run(WorldGrid grid, PathRequestManager paths, SpawnManager spawns)
    {
        foreach (Enemy enemy in spawns.Registry.Living.ToArray()) enemy.TakeDamage(10000);
        grid.Level.Walls.Clear(); grid.Level.Ground.Clear();
        for (int y = 0; y < grid.Config.HeightCells; y++)
            for (int x = 0; x < grid.Config.WidthCells; x++) grid.Level.Ground.SetCell(new(x, y), 0, new(2, 0));
        Vector2I start = new(5, 10), goal = new(25, 10);
        grid.Level.GetNode<Node2D>("Portals/EnemyPortal").GlobalPosition = grid.CellToWorldCenter(start);
        foreach (Node node in grid.Level.Citadel.GetNode("ApproachPoints").GetChildren())
            ((Node2D)node).GlobalPosition = grid.CellToWorldCenter(goal);
        grid.RebuildTerrain(); paths.Navigation.Refresh();
        EnemyMasterConfig config = paths.Config;
        int count = config.MaxPathCalculationsPerFrame, checks = config.MaxRouteValidationsPerFrame;
        float time = config.MaxPathfindingMillisecondsPerFrame, audit = config.RouteAuditIntervalSeconds;
        bool offsets = config.EnablePathOffsets;
        config.EnablePathOffsets = false; config.MaxPathCalculationsPerFrame = 1;
        config.MaxRouteValidationsPerFrame = 2; config.MaxPathfindingMillisecondsPerFrame = 0;
        PackedScene scene = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn");
        Enemy[] affected = new Enemy[500];
        Vector2[] line = Enumerable.Range(5, 21).Select(x => grid.CellToWorldCenter(new(x, 10))).ToArray();
        for (int i = 0; i < affected.Length; i++)
        {
            affected[i] = spawns.Spawn(scene, true); affected[i].Movement.SetPhysicsProcess(false);
        }
        Drain(paths);
        foreach (Enemy ship in affected) ship.Movement.AcceptPath(line, grid.NavigationRevision);
        Enemy unaffected = spawns.Spawn(scene, true); unaffected.Movement.SetPhysicsProcess(false); Drain(paths);
        unaffected.GlobalPosition = grid.CellToWorldCenter(new(5, 14));
        unaffected.Movement.AcceptPath(Enumerable.Range(5, 21).Select(x => grid.CellToWorldCenter(new(x, 14))).ToArray(), grid.NavigationRevision);
        Vector2 before = unaffected.GlobalPosition; int rebuilds = paths.Navigation.FullRebuildCount;
        int updates = paths.Navigation.IncrementalCellUpdates;
        Require(grid.TryOccupy(new(12, 10)), "Fixture tower occupancy failed"); paths.Navigation.Refresh();
        Require(paths.Navigation.FullRebuildCount == rebuilds && paths.Navigation.IncrementalCellUpdates == updates + 1,
            "Tower did not use exactly one incremental cell update");
        unaffected.Movement._PhysicsProcess(1.0 / 60);
        Require(unaffected.GlobalPosition != before && !unaffected.Movement.IsWaitingForPath, "Unaffected route stopped");
        paths._PhysicsProcess(1.0 / 60);
        Require(paths.LastFrameValidations <= 2 && paths.LastFrameCalculations <= 1 && paths.PendingValidationCount > 0,
            "Affected checks/repaths did not spread across frames");
        int frames = 0;
        while ((paths.PendingValidationCount > 0 || paths.PendingCount > 0) && frames++ < 2000)
        {
            foreach (Enemy ship in affected)
            {
                ship.Movement._PhysicsProcess(1.0 / 60);
                Require(paths.Navigation.HasClearance(ship.GlobalPosition, ship.Definition.RadiusPixels), "Ship entered newly blocked terrain");
            }
            paths._PhysicsProcess(1.0 / 60);
            Require(paths.LastFrameCalculations <= 1 && paths.LastFrameValidations <= 2, "Configured count budget exceeded");
        }
        Require(paths.PendingCount == 0 && frames > 1, "Repath queue did not complete fairly");
        foreach (Enemy ship in affected)
            Require(!ship.Movement.IsWaitingForPath && !ship.Movement.Waypoints.Any(p => grid.WorldToCell(p) == new Vector2I(12, 10)), "Replacement route used the tower");
        GD.Print("PASS: one-cell updates, uninterrupted unaffected ship, staggered affected checks/repaths and new-block clearance");
        long calculations = paths.TotalCalculations;
        grid.ReleaseOccupancy(new(12, 10)); Drain(paths);
        Require(paths.TotalCalculations == calculations, "Removal needlessly repathed valid routes");
        config.RouteAuditIntervalSeconds = 0.1f;
        int auditChecks = 0;
        for (int tick = 0; tick < 240; tick++)
        {
            unaffected.Movement._PhysicsProcess(1.0 / 60); paths._PhysicsProcess(1.0 / 60);
            auditChecks += paths.LastFrameValidations;
        }
        Drain(paths);
        Require(auditChecks > 0, "Periodic audit never ran");
        Require(paths.TotalCalculations == calculations, "Valid periodic audits calculated A*");
        GD.Print("PASS: blocker removal and staggered valid audits do not calculate A*");
        foreach (Enemy enemy in spawns.Registry.Living.ToArray()) enemy.TakeDamage(10000); Drain(paths);
        config.EnablePathOffsets = true; config.RouteAuditIntervalSeconds = audit;
        Enemy[] varied = new Enemy[24];
        for (int i = 0; i < varied.Length; i++)
        { varied[i] = spawns.Spawn(scene, true); varied[i].Movement.SetPhysicsProcess(false); }
        Drain(paths);
        foreach (Enemy ship in varied) ship.Movement.AcceptPath(line, grid.NavigationRevision);
        Require(varied.Select(s => Math.Round(s.Movement.Waypoints[5].Y, 2)).Distinct().Count() > 4, "Offsets lack per-ship diversity");
        foreach (Enemy ship in varied)
        {
            for (int i = 0; i < line.Length; i++) Require(ship.Movement.Waypoints[i].DistanceTo(line[i]) <= 8.01f, "Offset exceeds eight pixels");
            for (int i = 1; i < line.Length; i++) Require(paths.Navigation.SegmentClear(ship.Movement.Waypoints[i - 1], ship.Movement.Waypoints[i], ship.Definition.RadiusPixels), "Offset segment is unsafe");
        }
        // A nearly full-cell-radius ship in a one-cell corridor must taper its lateral offset.
        for (int x = 5; x <= 25; x++)
        { grid.Level.Walls.SetCell(new(x, 9), 0, new(0, 0)); grid.Level.Walls.SetCell(new(x, 11), 0, new(0, 0)); }
        grid.RebuildTerrain(); paths.Navigation.Refresh();
        Enemy narrow = varied.OrderByDescending(s => Math.Abs(s.Movement.LateralOffsetPixels)).First();
        narrow.Definition = (EnemyDefinition)narrow.Definition.Duplicate(); narrow.Definition.RadiusPixels = 30f;
        narrow.Movement.AcceptPath(line, grid.NavigationRevision);
        Require(Math.Abs(narrow.Movement.Waypoints[5].Y - line[5].Y) < 2.01f, "Narrow-corridor offset did not taper");
        Require(paths.Navigation.SegmentClear(narrow.Movement.Waypoints[4], narrow.Movement.Waypoints[5], 30f), "Tapered offset lacks clearance");
        GD.Print("PASS: stable per-ship offsets stay within eight pixels and taper safely in a narrow corridor");
        foreach (Enemy enemy in spawns.Registry.Living.ToArray()) enemy.TakeDamage(10000); Drain(paths);
        config.MaxPathCalculationsPerFrame = 64; config.MaxPathfindingMillisecondsPerFrame = 0.000001f;
        Enemy regular = spawns.Spawn(scene, true); regular.Movement.SetPhysicsProcess(false);
        Enemy urgent = spawns.Spawn(scene, true); urgent.Movement.SetPhysicsProcess(false);
        Enemy later = spawns.Spawn(scene, true); later.Movement.SetPhysicsProcess(false);
        paths.RequestPath(urgent.Movement, 0f);
        Require(paths.PendingCount == 3, "Duplicate request was queued twice");
        for (int tick = 0; tick < 2; tick++)
        {
            paths._PhysicsProcess(1.0 / 60);
            Require(paths.LastFrameCalculations == 1, "Soft time budget did not yield after one calculation");
        }
        Require(!urgent.Movement.IsWaitingForPath, "Immediate-obstacle request was not prioritized");
        Drain(paths);
        Require(!regular.Movement.IsWaitingForPath && !later.Movement.IsWaitingForPath, "Normal requests starved behind urgent work");
        foreach (Enemy enemy in spawns.Registry.Living.ToArray()) enemy.TakeDamage(10000); Drain(paths);
        GD.Print("PASS: soft time budget, urgent priority, duplicate-request coalescing and normal-request fairness");
        Enemy corner = spawns.Spawn(scene, true); corner.Movement.SetPhysicsProcess(false); Drain(paths);
        corner.Definition = (EnemyDefinition)corner.Definition.Duplicate(); corner.Definition.RadiusPixels = 1;
        corner.GlobalPosition = grid.CellToWorldCenter(new(5, 20));
        corner.Movement.AcceptPath(new[] { corner.GlobalPosition, grid.CellToWorldCenter(new(6, 21)), grid.CellToWorldCenter(new(7, 22)) }, grid.NavigationRevision);
        Vector2 cornerBefore = corner.GlobalPosition;
        grid.TryOccupy(new(6, 20)); corner.Movement._PhysicsProcess(1.0 / 60);
        Require(corner.GlobalPosition == cornerBefore && corner.Movement.IsWaitingForPath, "Tiny offset ship crossed an invalid diagonal corner");
        corner.TakeDamage(10000); Drain(paths); grid.ReleaseOccupancy(new(6, 20));
        GD.Print("PASS: immediate logical corner protection also covers tiny-radius offset ships");
        config.MaxPathCalculationsPerFrame = count; config.MaxRouteValidationsPerFrame = checks;
        config.MaxPathfindingMillisecondsPerFrame = time; config.RouteAuditIntervalSeconds = audit; config.EnablePathOffsets = offsets;
    }

    // =========================================================
    // Drain both bounded work queues, including invalidation audits before A* exists.
    private static void Drain(PathRequestManager paths)
    {
        int frames = 0;
        while ((paths.PendingCount > 0 || paths.PendingValidationCount > 0) && frames++ < 10000) paths._PhysicsProcess(1.0 / 60);
        Require(frames < 10000, "Queue starved");
    }

    // =========================================================
    // Fail the headless integration scene on a safety or scheduling regression.
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
