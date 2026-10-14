// Safe-prefix movement and forward handoff checks with deliberately delayed replacement queries.
using Godot;
using System;
using System.Linq;

public static class SmoothRepathChecks
{
    // =========================================================
    // Verify behavior through actual world services, without saving the in-memory fixture.
    public static void Run(WorldGrid grid, PathRequestManager paths, SpawnManager spawns)
    {
        foreach (Enemy enemy in spawns.Registry.Living.ToArray()) enemy.TakeDamage(10000);
        grid.Level.Impassable.Clear(); grid.Level.Passable.Clear();
        for (int y = 0; y < grid.Config.HeightCells; y++)
            for (int x = 0; x < grid.Config.WidthCells; x++) grid.Level.Passable.SetCell(new(x, y), 0, new(2, 0));
        grid.Level.GetNode<Node2D>("Portals/EnemyPortal").GlobalPosition = grid.CellToWorldCenter(new(5, 20));
        foreach (Node marker in grid.Level.Citadel.GetNode("ApproachPoints").GetChildren())
            ((Node2D)marker).GlobalPosition = grid.CellToWorldCenter(new(25, 20));
        grid.RebuildTerrain(); paths.Navigation.Refresh();
        bool offsets = paths.Config.EnablePathOffsets; paths.Config.EnablePathOffsets = false;
        Enemy ship = spawns.Spawn(GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn"), true);
        ship.Movement.SetPhysicsProcess(false); Drain(paths);
        Vector2[] old = Enumerable.Range(5, 21).Select(x => grid.CellToWorldCenter(new(x, 20))).ToArray();
        ship.Movement.AcceptPath(old, grid.NavigationRevision);
        ship.Movement._PhysicsProcess(0.2);
        Vector2 before = ship.GlobalPosition;
        grid.TryOccupy(new(12, 20));
        ship.Movement.ValidateRoute();
        Require(ship.Movement.IsWaitingForPath, "Affected ship did not queue a replacement");
        for (int i = 0; i < 30; i++) ship.Movement._PhysicsProcess(1.0 / 60);
        Require(ship.GlobalPosition.X > before.X + 40, "Queued replacement froze a safe old prefix");
        // Hold the request indefinitely: the ship must stop on a safe waypoint before the wall.
        for (int i = 0; i < 400; i++)
        {
            ship.Movement._PhysicsProcess(1.0 / 60);
            Require(paths.Navigation.HasClearance(ship.GlobalPosition, ship.Definition.RadiusPixels), "Safe-prefix movement entered the tower");
        }
        Require(grid.WorldToCell(ship.GlobalPosition).X < 12, "Ship crossed a blocked pending segment");
        Vector2 stopped = ship.GlobalPosition; ship.Movement._PhysicsProcess(0.2);
        Require(ship.GlobalPosition == stopped, "Waiting ship did not stop before obstruction");
        // An explicit detour at the wall must turn via a safe elbow, not retreat to the cell center.
        ship.Movement.AcceptPath(new[] { grid.CellToWorldCenter(new(11, 20)), grid.CellToWorldCenter(new(11, 19)),
            grid.CellToWorldCenter(new(12, 19)), grid.CellToWorldCenter(new(13, 19)), grid.CellToWorldCenter(new(14, 20)) }, grid.NavigationRevision);
        ship.Movement._PhysicsProcess(0.1);
        Require(ship.GlobalPosition.X >= stopped.X - 0.01f && ship.GlobalPosition.Y < stopped.Y,
            "Wall detour handoff unnecessarily retreated to the cell center");
        GD.Print("PASS: near-wall handoff turns through a safe elbow without cell-center retreat");
        Drain(paths);
        Require(!ship.Movement.IsWaitingForPath, "Replacement did not complete");
        ship.Movement._PhysicsProcess(0.1);
        Require(paths.Navigation.HasClearance(ship.GlobalPosition, ship.Definition.RadiusPixels), "Replacement handoff crossed a blocker");
        GD.Print("PASS: replacement queue keeps safe-prefix movement and stops only before the obstruction");
        grid.ReleaseOccupancy(new(12, 20));
        // Return to a straight route and hand off from beyond the current cell's center.
        ship.GlobalPosition = grid.CellToWorldCenter(new(5, 20)); ship.Movement.AcceptPath(old, grid.NavigationRevision);
        ship.Movement._PhysicsProcess(0.2); before = ship.GlobalPosition;
        paths.Config.EnablePathOffsets = true;
        Vector2[] centerStarted = paths.Navigation.FindPath(before, new(25, 20), ship.Movement.RouteProfile, out _);
        Require(centerStarted[0].X < before.X, "Fixture did not start behind the moving ship");
        ship.Movement.AcceptPath(centerStarted, grid.NavigationRevision);
        ship.Movement._PhysicsProcess(0.1);
        Require(ship.GlobalPosition.X > before.X, "New path caused unnecessary cell-center backtracking");
        // Simulate a result calculated several cells ago while the ship continued moving.
        ship.Movement._PhysicsProcess(2.0); before = ship.GlobalPosition;
        ship.Movement.AcceptPath(centerStarted, grid.NavigationRevision);
        ship.Movement._PhysicsProcess(0.1);
        Require(ship.GlobalPosition.X > before.X && paths.Navigation.HasClearance(ship.GlobalPosition, ship.Definition.RadiusPixels),
            "Delayed snapshot did not reconnect safely at current progress");
        GD.Print("PASS: forward handoff with offsets and delayed snapshots avoids the cell-center U-turn");
        ship.TakeDamage(10000); Drain(paths); paths.Config.EnablePathOffsets = offsets;
    }

    // =========================================================
    // Both validation and path requests remain inside their production budgets.
    private static void Drain(PathRequestManager paths)
    {
        int frames = 0;
        while ((paths.PendingCount > 0 || paths.PendingValidationCount > 0) && frames++ < 10000) paths._PhysicsProcess(1.0 / 60);
        Require(frames < 10000, "Delayed request starved");
    }

    // =========================================================
    // Fail the integration scene on a movement or obstacle-safety regression.
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
