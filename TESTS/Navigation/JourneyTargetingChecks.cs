// Checkpoint-tail targeting uses captured branch choices and shared budgeted geometry, not tower-local A*.
using Godot;
using System;
using System.Linq;
using static AmmoLoopFixture;

public static class JourneyTargetingChecks
{
    // =========================================================
    // Controlled runtime terrain plus two real portal spawns protects the designer's saved battlefield.
    public static async System.Threading.Tasks.Task Run(Node owner)
    {
        using AmmoLoopFixture f = new(owner);
        SubsystemBattlefield.DetachSession(f.Main);
        WorldGrid grid = f.Main.GetNode<WorldGrid>("World/Grid"); PathRequestManager paths = f.Spawns.Paths;
        paths.Config = (EnemyMasterConfig)paths.Config.Duplicate(true); paths.Navigation.Config = paths.Config;
        paths.Config.EnablePathDiversity = paths.Config.EnablePathOffsets = paths.Config.EnableCorridorVariation = false;
        paths.Config.PortalSpawnOffsetPixels = 0; paths.Config.MaxPathCalculationsPerFrame = 2;
        paths.Config.CongestionInfluence = 0; paths.Config.MaxPathfindingMillisecondsPerFrame = 1;
        grid.Level.Settings = (LevelConfig)grid.Level.Settings.Duplicate(true);
        grid.Level.Passable.Clear(); grid.Level.Impassable.Clear(); grid.Level.Interactive.Clear();
        for (int y = 0; y < grid.Config.HeightCells; y++) for (int x = 0; x < grid.Config.WidthCells; x++) grid.Level.Impassable.SetCell(new(x,y), 0, Vector2I.Zero);
        for (int y = 6; y <= 14; y++) for (int x = 4; x <= 32; x++)
        { grid.Level.Impassable.EraseCell(new(x,y)); grid.Level.Passable.SetCell(new(x,y), 0, new(2,0)); }
        foreach (Node point in grid.Level.Citadel.GetNode("ApproachPoints").GetChildren())
            ((Node2D)point).GlobalPosition = grid.CellToWorldCenter(new(30,10));
        RouteCheckpoint a = Point("TestA", new(8,10)), c = Point("TestC", new(18,8)), d = Point("TestD", new(18,12));
        a.NextCheckpoints.Add(a.GetPathTo(c)); a.NextCheckpoints.Add(a.GetPathTo(d));
        EnemyPortal portalA = Portal("TestPortalA", new(7,10)), portalB = Portal("TestPortalB", new(27,10));
        portalA.CheckpointPath = portalA.GetPathTo(a);
        grid.Level.Settings.ActivePortals = new() { grid.Level.GetPathTo(portalA), grid.Level.GetPathTo(portalB) };
        grid.Level.Settings.SpawnMode = LevelConfig.SpawnSourceMode.Portals; grid.RebuildTerrain(); paths.Navigation.Refresh();
        RandomNumberGenerator random = new() { Seed = 771 };
        Enemy checkpointShip = f.Spawns.SpawnWave("basic_mk1", random, grid.Level.GetPathTo(portalA).ToString());
        Enemy directShip = f.Spawns.SpawnWave("basic_mk1", random, grid.Level.GetPathTo(portalB).ToString());
        Require(checkpointShip != null && directShip != null, "Two actual registered portals spawn successfully");
        Disable(checkpointShip); Disable(directShip); ulong rng = random.State;
        Drain(paths);
        Require(checkpointShip.RemainingPathDistance < directShip.RemainingPathDistance
            && checkpointShip.RemainingJourneyDistance > directShip.RemainingJourneyDistance,
            "Short checkpoint leg does not masquerade as a short Citadel journey");
        Vector2 origin = grid.CellToWorldCenter(new(20,10)); long calculations = paths.TotalCalculations;
        Require(TowerTargeting.Find(f.Spawns.Registry, origin, 4096, TowerTargeting.Priority.First) == directShip
            && TowerTargeting.Find(f.Spawns.Registry, origin, 4096, TowerTargeting.Priority.Last) == checkpointShip,
            "First/Last compare the full estimated journey across portals");
        ShieldPreference(f, checkpointShip, directShip, origin);
        for (int i = 0; i < 1000; i++) TowerTargeting.Find(f.Spawns.Registry, origin, 4096, TowerTargeting.Priority.Last);
        Require(calculations == paths.TotalCalculations && random.State == rng, "Target scoring does no A* and consumes no wave RNG");
        float before = checkpointShip.RemainingJourneyDistance;
        EnemyRouteHandoff snapshot = checkpointShip.Movement.CaptureRouteHandoff();
        RouteCheckpoint chosen = snapshot.Network.Next(a, checkpointShip.Movement.CorridorSeed, 0, out _);
        // Live link edits cannot reroll the network captured by an existing enemy.
        a.NextCheckpoints.Clear();
        for (int i = 0; i < 120 && checkpointShip.Movement.Checkpoint == a; i++)
        { checkpointShip.Movement._PhysicsProcess(1.0 / 60); paths._PhysicsProcess(1.0 / 60); }
        Require(checkpointShip.Movement.Checkpoint == chosen && checkpointShip.IsAlive, "Actual checkpoint transition follows the same captured branch");
        Drain(paths);
        chosen.GlobalPosition += new Vector2(0, 64); chosen.ForceUpdateTransform();
        Require(!float.IsFinite(checkpointShip.RemainingJourneyDistance), "Moving checkpoint invalidates old estimate immediately");
        checkpointShip.Movement._PhysicsProcess(.01); Drain(paths);
        Require(float.IsFinite(checkpointShip.RemainingJourneyDistance), "Moved geometry can receive a refreshed safe estimate");
        // Block all remaining routes to Citadel: stale estimates cannot win Last while real paths repath safely.
        for (int y = 6; y <= 14; y++) Require(grid.TryOccupy(new(22,y)), "Barrier tile: " + grid.PlacementFailure(new(22,y)));
        paths._PhysicsProcess(1.0 / 60);
        Require(!float.IsFinite(checkpointShip.RemainingJourneyDistance), "Navigation revision invalidates cached tails");
        Drain(paths);
        Require(!float.IsFinite(checkpointShip.RemainingJourneyDistance), "Unreachable downstream route remains unavailable");
        Require(TowerTargeting.Find(f.Spawns.Registry, origin, 4096, TowerTargeting.Priority.Last) == directShip, "Infinity is never Last");
        for (int y = 6; y <= 14; y++) grid.ReleaseOccupancy(new(22,y));
        Drain(paths);
        Require(float.IsFinite(checkpointShip.RemainingJourneyDistance), "Opening downstream route refreshes tail without rerolling branch");
        checkpointShip.Free(); directShip.Free();
        // A spawn burst shares one tail per profile/radius/edge, rather than one downstream search per enemy.
        Enemy[] burst = new Enemy[500];
        for (int i = 0; i < burst.Length; i++)
        {
            burst[i] = f.Target(grid.CellToWorldCenter(new(7,10)));
            burst[i].Initialize(f.Spawns.Registry, paths, checkpoint: a, requiresCheckpoint: true); Disable(burst[i]);
        }
        Drain(paths);
        Require(paths.Journeys.CachedEdges < 20 && burst.All(e => float.IsFinite(e.RemainingJourneyDistance)), "500 enemies share bounded checkpoint-tail cache");
        for (int i = 0; i < burst.Length; i++) burst[i].TakeDamage(i);
        foreach (TowerTargeting.Priority mode in Enum.GetValues<TowerTargeting.Priority>())
        {
            Func<Enemy, float> score = mode switch
            {
                TowerTargeting.Priority.First or TowerTargeting.Priority.Last => e => e.RemainingJourneyDistance,
                TowerTargeting.Priority.Strongest or TowerTargeting.Priority.Weakest => e => e.Health + e.CurrentShield,
                _ => e => e.GlobalPosition.DistanceSquaredTo(origin)
            };
            bool maximum = mode is TowerTargeting.Priority.Last or TowerTargeting.Priority.Farthest or TowerTargeting.Priority.Strongest;
            Enemy expected = maximum ? burst.OrderByDescending(score).First() : burst.OrderBy(score).First();
            Require(TowerTargeting.Find(f.Spawns.Registry, origin, 4096, mode) == expected, "500-enemy brute-force ranking parity for " + mode);
        }
        GD.Print($"IT4 BENCH: 500 enemies; shared tail cache={paths.Journeys.CachedEdges}, total tail queries={paths.TotalJourneyCalculations}, per-frame combined cap={paths.Config.MaxPathCalculationsPerFrame}");
        foreach (Enemy enemy in burst) enemy.Free();
        RouteCheckpoint brokenRoot = Point("BrokenRoot", new(8,8)), removedLeaf = Point("RemovedLeaf", new(12,8));
        brokenRoot.NextCheckpoints.Add(brokenRoot.GetPathTo(removedLeaf));
        CheckpointNetwork captured = CheckpointNetwork.Capture(brokenRoot); removedLeaf.Free();
        var invalid = paths.Journeys.Attach(captured, brokenRoot, 0, 0, NavigationGrid.NeutralProfile, 12);
        Drain(paths); Require(!float.IsFinite(invalid.Distance), "Removed captured destination cannot become a shortened valid journey");
        grid.TryOccupy(new(25,6)); Drain(paths);
        Require(!float.IsFinite(invalid.Distance), "Invalid captured plan stays invalid through cache revisions");
        paths.Journeys.Detach(invalid); grid.ReleaseOccupancy(new(25,6)); brokenRoot.Free();
        await owner.ToSignal(owner.GetTree(), SceneTree.SignalName.ProcessFrame);
        GD.Print("PASS: checkpoint-aware First/Last, captured branches, two portals, geometry/revision invalidation, blocked tails, RNG and 500-enemy sharing");

        RouteCheckpoint Point(string name, Vector2I cell)
        { var point = new RouteCheckpoint { Name = name, RadiusPixels = 16 }; grid.Level.AddChild(point); point.GlobalPosition = grid.CellToWorldCenter(cell); return point; }
        EnemyPortal Portal(string name, Vector2I cell)
        { var portal = new EnemyPortal { Name = name }; portal.AddChild(new Marker2D { Name = "SpawnExit" }); grid.Level.AddChild(portal); portal.GlobalPosition = grid.CellToWorldCenter(cell); return portal; }
    }

    #region Shield Preference
    // =========================================================
    // Use actual finite routes from two portals; preference cannot bypass eligibility or change mode scoring.
    private static void ShieldPreference(AmmoLoopFixture f, Enemy checkpoint, Enemy direct, Vector2 origin)
    {
        float maximum = checkpoint.Definition.MaxShield;
        checkpoint.Definition.MaxShield = Math.Max(maximum, 20); checkpoint.RestoreShield(20);
        foreach (TowerTargeting.Priority mode in Enum.GetValues<TowerTargeting.Priority>())
            Require(TowerTargeting.Find(f.Spawns.Registry, origin, 4096, mode, preference: TowerTargeting.Preference.Shielded) == checkpoint,
                "Only shielded eligible ship wins every mode: " + mode);
        direct.RestoreShield(20);
        Require(TowerTargeting.Find(f.Spawns.Registry, origin, 4096, TowerTargeting.Priority.First, preference: TowerTargeting.Preference.Shielded) == direct &&
            TowerTargeting.Find(f.Spawns.Registry, origin, 4096, TowerTargeting.Priority.Last, preference: TowerTargeting.Preference.Shielded) == checkpoint,
            "First/Last shielded still compare full journeys");
        checkpoint.TakeDamage(20); direct.TakeDamage(20);
        foreach (TowerTargeting.Priority mode in Enum.GetValues<TowerTargeting.Priority>())
            Require(TowerTargeting.Find(f.Spawns.Registry, origin, 4096, mode, preference: TowerTargeting.Preference.Shielded) ==
                TowerTargeting.Find(f.Spawns.Registry, origin, 4096, mode), "Shield depletion falls back to ordinary " + mode);
        checkpoint.Definition.MaxShield = maximum;
    }
    #endregion

    // =========================================================
    // Optional queries use the same configured combined count cap, yielding after every native call.
    private static void Drain(PathRequestManager paths)
    {
        // A terrain edit invalidates lazily on the next manager tick, before queues are inspected.
        paths._PhysicsProcess(1.0 / 60);
        Require(paths.LastFrameCalculations <= paths.Config.MaxPathCalculationsPerFrame, "Initial combined query budget");
        int ticks = 0;
        while ((paths.PendingCount > 0 || paths.Journeys.PendingEdges > 0) && ticks++ < 30000)
        { paths._PhysicsProcess(1.0 / 60); Require(paths.LastFrameCalculations <= paths.Config.MaxPathCalculationsPerFrame, "Combined movement/estimate A* budget"); }
        Require(paths.PendingCount == 0 && paths.Journeys.PendingEdges == 0, "Bounded query queues eventually drain");
    }
}
