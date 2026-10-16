// Required area stages, actual portal ownership, swept entry, blocked retries, preview parity and navigation budgets.
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

public partial class CheckpointChecks : Node
{
    // =========================================================
    // Isolated editor-painted fixtures protect authored maps, definitions and normal no-checkpoint gameplay.
    public override async void _Ready()
    {
        Node2D main = null; EnemyMasterConfig config = null; LevelConfig settings = null;
        try
        {
            main = GD.Load<PackedScene>("res://Main.tscn").Instantiate<Node2D>(); AddChild(main);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Disable(main);
            WorldGrid grid = main.GetNode<WorldGrid>("World/Grid"); SpawnManager spawns = main.GetNode<SpawnManager>("World/EnemySystems/Spawning");
            PathRequestManager paths = spawns.Paths; NavigationGrid nav = paths.Navigation;
            config = (EnemyMasterConfig)paths.Config.Duplicate(true); paths.Config = config; nav.Config = config;
            settings = (LevelConfig)grid.Level.Settings.Duplicate(true); grid.Level.Settings = settings;
            config.PortalSpawnOffsetPixels = 0; config.EnablePathDiversity = true; config.CongestionInfluence = 0;
            config.EnableCorridorVariation = true; config.CorridorVariationPercent = 100; config.EnablePathOffsets = true;
            config.MaxActiveEnemies = 1000; config.MaxPathCalculationsPerFrame = 8; config.MaxPathfindingMillisecondsPerFrame = 2;
            grid.Level.Passable.Clear(); grid.Level.Impassable.Clear(); grid.Level.Interactive?.Clear();
            for (int y = 0; y < grid.Config.HeightCells; y++)
                for (int x = 0; x < grid.Config.WidthCells; x++) grid.Level.Impassable.SetCell(new(x, y), 0, new(0, 0));
            for (int y = 8; y <= 18; y++) for (int x = 4; x <= 46; x++)
            { grid.Level.Impassable.EraseCell(new(x, y)); grid.Level.Passable.SetCell(new(x, y), 0, new(2, 0)); }
            Vector2 start = grid.CellToWorldCenter(new(6, 12)); Vector2I goal = new(42, 12);
            EnemyPortal portal = grid.Level.GetNode<EnemyPortal>("Portals/EnemyPortal"); portal.GlobalPosition = start;
            EnemyPortal second = grid.Level.GetNode<EnemyPortal>("Portals/EnemyPortal2"); second.GlobalPosition = grid.CellToWorldCenter(new(6, 16));
            grid.Level.Citadel.GlobalPosition = grid.CellToWorldCenter(goal + new Vector2I(2, -1));
            foreach (Node child in grid.Level.Citadel.GetNode("ApproachPoints").GetChildren()) ((Node2D)child).GlobalPosition = grid.CellToWorldCenter(goal);
            settings.SpawnMode = LevelConfig.SpawnSourceMode.Portals; settings.ActivePortals = new() { new NodePath("Portals/EnemyPortal") }; grid.RebuildTerrain();
            RouteCheckpoint checkpoint = GD.Load<PackedScene>("res://WORLD/ROUTING/RouteCheckpoint/RouteCheckpoint.tscn").Instantiate<RouteCheckpoint>();
            checkpoint.Name = "TestCheckpoint"; grid.Level.AddChild(checkpoint); checkpoint.GlobalPosition = grid.CellToWorldCenter(new(22, 14)); checkpoint.RadiusPixels = 96;
            portal.CheckpointPath = portal.GetPathTo(checkpoint);
            Require(checkpoint.GetChildren().Count == 0 && !checkpoint.IsPhysicsProcessing(), "Checkpoint has no physics nodes/processing");
            int reserved = grid.NavigationRevision;
            checkpoint.GlobalPosition += new Vector2(3, 1);
            Require(grid.NavigationRevision == reserved, "Free marker does not reserve or block map cells");
            var mixed = new List<Enemy>(); var definitions = new List<EnemyDefinition>();
            foreach (bool master in new[] { false, true })
                for (int flags = 0; flags < 8; flags++)
                {
                    config.EnablePathDiversity = master; config.EnablePathOffsets = master; config.EnableCorridorVariation = master;
                    string id = flags % 3 == 0 ? "medium_mk1" : flags % 3 == 1 ? "basic_mk1" : "scout_mk1";
                    Enemy ship = spawns.Catalog.Find(id).Instantiate<Enemy>();
                    EnemyDefinition definition = (EnemyDefinition)ship.Definition.Duplicate(true); definitions.Add(definition);
                    definition.AllowPathDiversity = (flags & 1) != 0; definition.AllowPathOffsets = (flags & 2) != 0;
                    definition.AllowCorridorVariation = (flags & 4) != 0; definition.CitadelDamage = 0;
                    ship.Definition = definition; main.GetNode("World/Enemies").AddChild(ship); ship.GlobalPosition = start;
                    ship.Initialize(spawns.Registry, paths, checkpoint: checkpoint, requiresCheckpoint: true); ship.Movement.SetPhysicsProcess(false);
                    Drain(paths); EnemyMovement movement = ship.Movement;
                    Require(!movement.CheckpointCompleted && checkpoint.Contains(movement.Waypoints[^1]), "Planning targets circle but does not complete physical stage");
                    uint seed = movement.CorridorSeed; int profile = movement.RouteProfile; float offset = movement.LateralOffsetPixels;
                    for (int i = 0; !movement.CheckpointCompleted && i < 600; i++) { Tick(paths, ship, 0.1); Require(nav.HasClearance(ship.GlobalPosition, definition.RadiusPixels), "Radius/corner safety approaching checkpoint"); }
                    Require(movement.CheckpointCompleted && checkpoint.Contains(ship.GlobalPosition) && ship.IsAlive, $"Physical entry advances without Citadel arrival master={master} flags={flags} completed={movement.CheckpointCompleted} alive={ship.IsAlive} position={ship.GlobalPosition} distance={ship.GlobalPosition.DistanceTo(checkpoint.GlobalPosition)} radius={checkpoint.RadiusPixels}");
                    Drain(paths); Require(movement.Waypoints.Length > 0 && grid.WorldToCell(movement.Waypoints[^1]) == goal, "Second stage targets actual Citadel");
                    Require(seed == movement.CorridorSeed && profile == movement.RouteProfile && offset == movement.LateralOffsetPixels, "Stage transition preserves all lifetime preferences");
                    for (int i = 0; ship.IsAlive && i < 600; i++) Tick(paths, ship, 0.1);
                    Require(!ship.IsAlive && movement.CheckpointCompleted, "Citadel arrival only after checkpoint, once"); mixed.Add(ship);
                }
            foreach (Enemy ship in mixed) if (GodotObject.IsInstanceValid(ship)) ship.Free();
            foreach (EnemyDefinition definition in definitions) definition.Dispose();
            config.EnablePathDiversity = config.EnablePathOffsets = config.EnableCorridorVariation = false;
            // Debug and wave paths resolve the actual selected portal before initial movement requests.
            Enemy debugShip = spawns.Spawn("basic_mk1", true); debugShip.Movement.SetPhysicsProcess(false);
            Require(debugShip.Movement.Checkpoint == checkpoint && debugShip.Movement.HeadingToCheckpoint, "Real debug spawn uses linked portal");
            Drain(paths);
            debugShip.Definition = (EnemyDefinition)debugShip.Definition.Duplicate(true); EnemyDefinition fastDefinition = debugShip.Definition; fastDefinition.MoveSpeed = 10000;
            debugShip.Movement._PhysicsProcess(1);
            Require(debugShip.IsAlive && debugShip.Movement.CheckpointCompleted && checkpoint.Contains(debugShip.GlobalPosition), "Fast swept entry stops at circle rather than skipping to Citadel");
            debugShip.Free(); fastDefinition.Dispose();
            Vector2? rateEntry = null;
            foreach (int hz in new[] { 30, 60, 120 })
            {
                Enemy ship = spawns.Catalog.Find("basic_mk1").Instantiate<Enemy>();
                EnemyDefinition definition = (EnemyDefinition)ship.Definition.Duplicate(true); definition.MoveSpeed = 5000; definition.CitadelDamage = 0;
                ship.Definition = definition; main.GetNode("World/Enemies").AddChild(ship); ship.GlobalPosition = start;
                ship.Initialize(spawns.Registry, paths, checkpoint: checkpoint, requiresCheckpoint: true); ship.Movement.SetPhysicsProcess(false); Drain(paths);
                for (int i = 0; !ship.Movement.CheckpointCompleted && i < 1000; i++) Tick(paths, ship, 1.0 / hz);
                Require(ship.Movement.CheckpointCompleted && checkpoint.Contains(ship.GlobalPosition) && ship.IsAlive, "Swept checkpoint entry at every physics rate");
                if (rateEntry.HasValue) Require(rateEntry.Value.DistanceTo(ship.GlobalPosition) < 0.1f, "30/60/120Hz preserve physical entry geometry");
                rateEntry = ship.GlobalPosition; ship.Free(); definition.Dispose();
            }
            RouteCheckpoint temporary = new() { RadiusPixels = 50 }; grid.Level.AddChild(temporary); temporary.GlobalPosition = start;
            portal.CheckpointPath = portal.GetPathTo(temporary);
            Enemy inside = spawns.Spawn("basic_mk1", true); inside.Movement.SetPhysicsProcess(false); Drain(paths);
            Require(inside.Movement.CheckpointCompleted && grid.WorldToCell(inside.Movement.Waypoints[^1]) == goal, "Spawn already physically inside starts Citadel stage"); inside.Free();
            temporary.GlobalPosition = checkpoint.GlobalPosition;
            Enemy removed = spawns.Spawn("basic_mk1", true); removed.Movement.SetPhysicsProcess(false); Drain(paths); temporary.Free();
            Tick(paths, removed, 0.1); Drain(paths);
            Require(removed.IsAlive && removed.Movement.HeadingToCheckpoint && !removed.Movement.CheckpointCompleted, "Removed checkpoint does not bypass requirement or crash"); removed.Free();
            portal.CheckpointPath = portal.GetPathTo(checkpoint);
            RouteCheckpoint other = new() { Name = "OtherCheckpoint", RadiusPixels = 80 }; grid.Level.AddChild(other); other.GlobalPosition = grid.CellToWorldCenter(new(30, 16));
            second.CheckpointPath = second.GetPathTo(other); settings.ActivePortals.Add(new NodePath("Portals/EnemyPortal2")); grid.RebuildTerrain();
            Enemy wave = spawns.SpawnWave("scout_mk1", new RandomNumberGenerator { Seed = 42 }, "Portals/EnemyPortal2"); wave.Movement.SetPhysicsProcess(false);
            Require(wave.Movement.Checkpoint == other, "Wave uses actual required portal, independent of type/catalog order"); wave.Free();
            // Invalid links remain hard requirements.
            portal.CheckpointPath = new NodePath("../../MissingCheckpoint"); settings.ActivePortals = new() { new NodePath("Portals/EnemyPortal") }; grid.RebuildTerrain();
            Enemy invalid = spawns.Spawn("basic_mk1", true); invalid.Movement.SetPhysicsProcess(false); Drain(paths); invalid.Movement._PhysicsProcess(1);
            Require(invalid.IsAlive && invalid.Movement.HeadingToCheckpoint && invalid.Movement.Waypoints.Length == 0, "Missing required link waits without fallback"); invalid.Free();
            portal.CheckpointPath = portal.GetPathTo(checkpoint);
            Enemy waiting = spawns.Spawn("basic_mk1", true); waiting.Movement.SetPhysicsProcess(false); Drain(paths);
            // Tower edits invalidate pending checkpoint routes; an inaccessible area waits and retries.
            for (int y = 8; y <= 18; y++) Require(grid.TryOccupy(new(18, y)), "Temporary wall fixture");
            for (int i = 0; i < 30; i++) Tick(paths, waiting, 0.1); Drain(paths);
            Require(waiting.IsAlive && !waiting.Movement.CheckpointCompleted && waiting.GlobalPosition.X < grid.CellToWorldCenter(new(18, 12)).X, "Blocked stage never bypasses checkpoint");
            for (int y = 8; y <= 18; y++) grid.ReleaseOccupancy(new(18, y));
            for (int i = 0; !waiting.Movement.CheckpointCompleted && i < 600; i++) Tick(paths, waiting, 0.1);
            Require(waiting.Movement.CheckpointCompleted, "Reopened checkpoint recovers through bounded retries"); waiting.Free();
            // Preview plans both actual legs and exposes the physical entry point.
            EnemyDefinition reference = GD.Load<EnemyDefinition>("res://ENEMIES/Types/Enemy001/Enemy001.tres");
            RoutePreviewJob preview = new(nav, reference, start, 3, true, checkpoint, true);
            for (int i = 0; !preview.Complete && i < 10000; i++) { preview.Step(); Require(preview.LastCalculations <= 1, "Preview stage queries retain separate budget"); }
            Require(preview.Complete && preview.Results.Count == 3 && preview.Results.All(r => r.CheckpointEntry.HasValue && checkpoint.Contains(r.CheckpointEntry.Value)), "Preview must visibly pass through circle before Citadel");
            RoutePreviewJob missing = new(nav, reference, start, 1, true, requiresCheckpoint: true);
            Require(missing.Complete && missing.Results.Count == 0 && missing.Message.Contains("checkpoint"), "Preview reports missing required links");
            RouteCheckpoint solid = new() { RadiusPixels = 8 }; grid.Level.AddChild(solid); solid.GlobalPosition = grid.CellToWorldCenter(new(2, 2));
            RoutePreviewJob invalidArea = new(nav, reference, start, 1, true, solid, true);
            Require(invalidArea.Complete && invalidArea.Results.Count == 0 && solid.Targets(nav, reference.RadiusPixels).Length == 0,
                "A checkpoint inside solid terrain has no unsafe targets or fallback"); solid.Free();
            int builds = checkpoint.TargetBuildCount; checkpoint.Targets(nav, reference.RadiusPixels); checkpoint.Targets(nav, reference.RadiusPixels);
            Require(checkpoint.TargetBuildCount <= builds + 1 && checkpoint.Targets(nav, reference.RadiusPixels).Length <= 33, "Bounded targets are shared/cached, not a per-tile A* search");
            // F3 obtains each portal's actual checkpoint and invalidates drawings on a marker edit.
            settings.ActivePortals.Add(new NodePath("Portals/EnemyPortal2")); grid.RebuildTerrain();
            RoutePreviewOverlay overlay = grid.Level.GetNode<LevelDebugController>("Debug").RoutePreview; overlay.Toggle();
            OptionButton choices = Find<OptionButton>(overlay.GetNode("PreviewUI/Panel"), "Portal");
            SpinBox samples = Find<SpinBox>(overlay.GetNode("PreviewUI/Panel"), "Samples");
            choices.Select(choices.ItemCount - 1); samples.Value = 1; overlay.Simulate();
            for (int i = 0; overlay.Job != null && !overlay.Job.Complete && i < 10000; i++) overlay._PhysicsProcess(1.0 / 60);
            Require(overlay.PortalJobs.Count == 2 && overlay.PortalJobs.All(j => j.RequiresCheckpoint && j.Results.Count == 1 && j.Checkpoint.Contains(j.Results[0].CheckpointEntry.Value)),
                "ALL F3 plans each real portal checkpoint separately");
            Vector2 oldPosition = checkpoint.GlobalPosition; checkpoint.GlobalPosition += new Vector2(1, 0); overlay._PhysicsProcess(0);
            Require(overlay.Job == null, "Moving a checkpoint invalidates cached F3 output"); checkpoint.GlobalPosition = oldPosition; overlay.Toggle();
            settings.ActivePortals = new() { new NodePath("Portals/EnemyPortal") }; grid.RebuildTerrain();
            // No assigned checkpoint exactly preserves direct routing.
            portal.CheckpointPath = new NodePath(""); Enemy plain = spawns.Spawn("basic_mk1", true); plain.Movement.SetPhysicsProcess(false); Drain(paths);
            Require(!plain.Movement.RequiresCheckpoint && grid.WorldToCell(plain.Movement.Waypoints[^1]) == goal, "Unassigned portal retains existing route"); plain.Free();
            // 500 active ships share one cached target array and the unchanged bounded scheduler.
            portal.CheckpointPath = portal.GetPathTo(checkpoint);
            int targetBuilds = checkpoint.TargetBuildCount; Stopwatch perf = Stopwatch.StartNew();
            var crowd = new List<Enemy>();
            for (int i = 0; i < 500; i++)
            { Enemy ship = spawns.Spawn("basic_mk1", true); Require(ship != null, "Checkpoint 500 spawn"); ship.Movement.SetPhysicsProcess(false); crowd.Add(ship); }
            double spawnMs = perf.Elapsed.TotalMilliseconds; perf.Restart(); long queries = paths.TotalCalculations; Drain(paths);
            double routeMs = perf.Elapsed.TotalMilliseconds;
            Require(checkpoint.TargetBuildCount <= targetBuilds + 1 && crowd.All(e => e.Movement.HeadingToCheckpoint && e.Movement.Waypoints.Length > 0), "500 ships share cached region targets");
            GD.Print($"CHECKPOINT BENCH 500: spawn {spawnMs:0.00}ms; bounded routing total {routeMs:0.00}ms / {paths.TotalCalculations - queries} queries; no per-enemy grids");
            foreach (Enemy ship in crowd) ship.Free();
            // Both thin and wider corridors preserve actual ship-radius clearance on staged routes.
            foreach (int width in new[] { 1, 3 })
            {
                for (int y = 8; y <= 18; y++) for (int x = 4; x <= 46; x++)
                {
                    bool open = Math.Abs(y - 12) <= width / 2;
                    if (open) { grid.Level.Impassable.EraseCell(new(x, y)); grid.Level.Passable.SetCell(new(x, y), 0, new(2, 0)); }
                    else { grid.Level.Passable.EraseCell(new(x, y)); grid.Level.Impassable.SetCell(new(x, y), 0, new(0, 0)); }
                }
                checkpoint.GlobalPosition = grid.CellToWorldCenter(new(22, 12)); checkpoint.RadiusPixels = 70; grid.RebuildTerrain();
                foreach (string id in new[] { "scout_mk1", "medium_mk1" })
                {
                    Enemy ship = spawns.Spawn(id, true); ship.Movement.SetPhysicsProcess(false); Drain(paths);
                    for (int i = 0; !ship.Movement.CheckpointCompleted && i < 1000; i++)
                    { Tick(paths, ship, 0.1); Require(nav.HasClearance(ship.GlobalPosition, ship.Definition.RadiusPixels), "Thin corridor physical clearance"); }
                    Require(ship.Movement.CheckpointCompleted, "Narrow/wide checkpoint entry"); ship.Free();
                }
            }
            other.Free();
            second.CheckpointPath = new NodePath("");
            CheckpointChainChecks.Run(main, grid, spawns, checkpoint);
            GD.Print("PASS: required checkpoint matrix16, exact portal attribution, physical/swept entry, persistent preferences, blocked retry, no-link parity, preview stages and bounded target cache");
            main.QueueFree(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); main = null;
            config.Dispose(); config = null; settings.Dispose(); settings = null; GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    // Serve all pending searches with unchanged production hard/soft budgets.
    private static void Drain(PathRequestManager paths)
    {
        for (int i = 0; paths.PendingCount > 0 && i < 10000; i++)
        { paths._PhysicsProcess(1.0 / 60); Require(paths.LastFrameCalculations <= paths.Config.MaxPathCalculationsPerFrame, "Gameplay A* count cap"); }
        Require(paths.PendingCount == 0, "Bounded checkpoint requests drain");
    }
    // =========================================================
    // Real movement and queued work, without advancing unrelated scene systems.
    private static void Tick(PathRequestManager paths, Enemy ship, double delta)
    { paths._PhysicsProcess(delta); if (ship.IsAlive) ship.Movement._PhysicsProcess(delta); }
    // =========================================================
    // Inspect actual F3 widgets without making the test depend on panel layout containers.
    private static T Find<T>(Node node, string name) where T : Node
    {
        if (node is T match && node.Name == name) return match;
        foreach (Node child in node.GetChildren()) { T result = Find<T>(child, name); if (result != null) return result; }
        return null;
    }
    // =========================================================
    // Freeze world-owned systems so fixture checks are reproducible.
    private static void Disable(Node node)
    { node.SetProcess(false); node.SetPhysicsProcess(false); foreach (Node child in node.GetChildren()) Disable(child); }
    // =========================================================
    // Stop the runner on an unsafe route or incorrect checkpoint lifecycle.
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
