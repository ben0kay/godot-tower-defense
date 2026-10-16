// Batched wall/foundation updates, combined-route safety and radius-aware clearance without per-cell floods.
using Godot;
using System;
using System.Diagnostics;
using System.Linq;
using static AmmoLoopFixture;

internal static class FootprintNavigationChecks
{
    // =========================================================
    // Real wall placement/removal causes zero navigation work; passable occupancy stages all six cells first.
    internal static void Run(Node owner)
    {
        using (FootprintFixture f = new(owner))
        {
            Vector2I size = new(3, 2); f.Paint(new(f.Anchor, size), true); f.Grid.RebuildTerrain(); f.Nav.Refresh();
            int revision = f.Grid.NavigationRevision, updates = f.Nav.IncrementalCellUpdates, rebuilds = f.Nav.FullRebuildCount,
                floods = f.Nav.PlacementFloodCount, pending = f.World.Spawns.Paths.PendingValidationCount;
            long searches = f.World.Spawns.Paths.TotalCalculations;
            TowerBase building = f.Build("ammo_factory", f.Anchor);
            Require(building != null && f.Occupied(f.Anchor, size) == 6, "Factory builds on six buildable wall cells");
            building.Free(); f.Nav.Refresh();
            Require(f.Grid.NavigationRevision == revision && f.Nav.IncrementalCellUpdates == updates && f.Nav.FullRebuildCount == rebuilds
                && f.Nav.PlacementFloodCount == floods && f.World.Spawns.Paths.PendingValidationCount == pending
                && f.World.Spawns.Paths.TotalCalculations == searches, "All-impassable build/removal performs zero revision/A*/flood/repath work");
            f.Paint(new(f.Anchor, size), false); f.Grid.RebuildTerrain(); f.Nav.Refresh();
            revision = f.Grid.NavigationRevision; updates = f.Nav.IncrementalCellUpdates; rebuilds = f.Nav.FullRebuildCount;
            int events = 0;
            f.Grid.NavigationBatchChanged += (rect, blocked) =>
            { events++; Require(rect == new Rect2I(f.Anchor, size) && f.Occupied(f.Anchor, size) == (blocked ? 6 : 0), "Notification observes complete atomic rectangle"); };
            Require(f.Grid.TryOccupy(f.Anchor, size), "Whole passable claim"); f.Nav.Refresh();
            Require(events == 1 && f.Grid.NavigationRevision == revision + 1 && f.Nav.IncrementalCellUpdates == updates + 6
                && f.Nav.FullRebuildCount == rebuilds, "Single batch/revision, six incremental edits, no full rebuild");
            for (int y = 0; y < 2; y++) for (int x = 0; x < 3; x++)
                Require(f.Nav.FindPath(f.Grid.CellToWorldCenter(f.Anchor + new Vector2I(x, y)), f.Nav.Goals[0]).Length == 0, "All six A* points are blocked");
            f.Grid.ReleaseOccupancy(f.Anchor, size); Require(events == 2 && f.Nav.IncrementalCellUpdates == updates + 12, "Batched exact removal");
            Enemy enemy = f.World.Target(f.Grid.CellToWorldCenter(f.Anchor) - new Vector2(f.Grid.Config.CellSize * .5f + 5, 0));
            Require(f.World.Spawns.PlacementFailure(f.Anchor, size).Contains("too close"), "Nonzero enemy radius clips rectangle edge");
            f.NoBuild("ammo_factory", f.Anchor, size); enemy.Free();
        }
        CombinedRoute(owner); Population(owner);
        GD.Print("PASS: whole-rectangle route sealing/clearance, all-wall zero-work, passable batched incremental changes and 500-enemy placement benchmark");
    }

    // =========================================================
    // Each tile alone leaves the adjacent lane open, but the whole rectangle must fail before payment.
    private static void CombinedRoute(Node owner)
    {
        using FootprintFixture f = new(owner);
        f.Grid.Level.Settings = (LevelConfig)f.Grid.Level.Settings.Duplicate(true);
        LevelConfig settings = f.Grid.Level.Settings;
        f.Grid.Level.Passable.Clear(); f.Grid.Level.Impassable.Clear(); f.Grid.Level.Interactive.Clear();
        f.Paint(new(Vector2I.Zero, new(f.Grid.Config.WidthCells, f.Grid.Config.HeightCells)), true);
        f.Paint(new(new(4, 12), new(43, 2)), false);
        EnemyPortal portal = f.Grid.Level.GetNode<EnemyPortal>("Portals/EnemyPortal");
        portal.GlobalPosition = f.Grid.CellToWorldCenter(new(6, 12)); portal.GetNode<Marker2D>("SpawnExit").GlobalPosition = portal.GlobalPosition;
        Vector2I goal = new(42, 12); f.Grid.Level.Citadel.GlobalPosition = f.Grid.CellToWorldCenter(goal + new Vector2I(2, -1));
        foreach (Node child in f.Grid.Level.Citadel.GetNode("ApproachPoints").GetChildren()) ((Node2D)child).GlobalPosition = f.Grid.CellToWorldCenter(goal);
        settings.SpawnMode = LevelConfig.SpawnSourceMode.Portals; settings.ActivePortals = new() { new NodePath("Portals/EnemyPortal") };
        f.Grid.RebuildTerrain(); f.Nav.Refresh(); Vector2I anchor = new(20, 12), size = new(3, 2);
        for (int y = 0; y < size.Y; y++) for (int x = 0; x < size.X; x++)
            Require(f.World.Spawns.PlacementFailure(anchor + new Vector2I(x, y)).Length == 0, "Individual cell leaves other lane available");
        int floods = f.Nav.PlacementFloodCount;
        Require(f.World.Spawns.PlacementFailure(anchor, size).Contains("seal") && f.Nav.PlacementFloodCount == floods + 1, "One combined flood rejects collective route seal");
        f.NoBuild("ammo_factory", anchor, size);
        settings.ActivePortals.Clear(); f.Grid.RebuildTerrain(); f.Nav.Refresh();
        Enemy trapped = f.World.Target(f.Grid.CellToWorldCenter(new(10, 12)));
        Require(f.World.Spawns.PlacementFailure(anchor, size).Contains("trap"), "Protect a live enemy even with no active portal sources");
        f.NoBuild("ammo_factory", anchor, size); trapped.Free();
    }

    // =========================================================
    // Click-time only: nearby-circle clearance plus one reusable flood, not six enemy/flood passes.
    private static void Population(Node owner)
    {
        using FootprintFixture f = new(owner);
        Vector2 position = f.Grid.CellToWorldCenter(f.Anchor + new Vector2I(8, 8));
        for (int i = 0; i < 500; i++) f.World.Target(position);
        // Warm the source cache before measuring candidate floods.
        f.World.Spawns.PlacementFailure(f.Anchor, new(3, 2)); int before = f.Nav.PlacementFloodCount;
        Stopwatch watch = Stopwatch.StartNew();
        for (int i = 0; i < 20; i++) Require(f.World.Spawns.PlacementFailure(f.Anchor, new(3, 2)).Length == 0, "500-enemy safe placement");
        watch.Stop(); Require(f.Nav.PlacementFloodCount - before == 20, "One candidate flood per click, independent of footprint area/population");
        PathRequestManager paths = f.World.Spawns.Paths;
        Vector2[] route = Enumerable.Range(10, 11).Reverse().Select(x => f.Grid.CellToWorldCenter(new(x, 20))).ToArray();
        foreach (Enemy enemy in f.World.Spawns.Registry.Living) enemy.Movement.AcceptPath(route, f.Grid.NavigationRevision);
        Require(paths.PendingValidationCount == 0, "No candidate preview schedules route work");
        Vector2I obstacle = new(12, 20);
        Require(f.Grid.TryOccupy(obstacle, new(3, 2)) && paths.PendingValidationCount == 500, "Six blocked cells coalesce into one indexed validation per affected enemy");
        paths._PhysicsProcess(1.0 / 60);
        Require(paths.LastFrameValidations <= paths.Config.MaxRouteValidationsPerFrame && paths.LastFrameCalculations <= paths.Config.MaxPathCalculationsPerFrame,
            "Batched obstruction preserves strict validation/A* budgets");
        int queued = paths.PendingValidationCount; f.Grid.ReleaseOccupancy(obstacle, new(3, 2));
        Require(paths.PendingValidationCount == queued, "Removal does not queue extra route validations");
        GD.Print($"FOOTPRINT BENCH: 500 stationary enemies, 3x2 proposal,20 validations: {watch.Elapsed.TotalMilliseconds / 20:0.000}ms mean; one flood/click; headless CPU, not rendered FPS");
    }
}
