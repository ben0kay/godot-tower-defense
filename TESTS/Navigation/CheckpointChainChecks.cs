// Chain/branch semantics, authoring validation, deterministic previews and shared above-tile design drawing.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

public static class CheckpointChainChecks
{
    // =========================================================
    // Runs in the existing isolated checkpoint arena; authored maps and definitions remain untouched.
    public static void Run(Node2D main, WorldGrid grid, SpawnManager spawns, RouteCheckpoint a)
    {
        PathRequestManager paths = spawns.Paths; NavigationGrid nav = paths.Navigation;
        EnemyPortal portal = grid.Level.GetNode<EnemyPortal>("Portals/EnemyPortal"); Vector2 start = portal.GetNode<Marker2D>("SpawnExit").GlobalPosition;
        a.Name = "Checkpoint2A"; a.GlobalPosition = grid.CellToWorldCenter(new(22, 12)); a.RadiusPixels = 30; portal.CheckpointPath = portal.GetPathTo(a);
        RouteCheckpoint b = Add("Checkpoint2B", 28, 11), c = Add("Checkpoint2C", 28, 13),
            d = Add("Checkpoint2D", 37, 12), e = Add("Checkpoint2E", 33, 13), f = Add("Checkpoint2F", 33, 12);
        EnemyDefinition definition = (EnemyDefinition)GD.Load<EnemyDefinition>("res://ENEMIES/Types/Enemy001/Enemy001.tres").Duplicate(true);
        definition.MoveSpeed = 1500; definition.CitadelDamage = 0;
        try
        {
            Link(a, b); Link(b, d);
            Enemy chain = Spawn(); uint seed = chain.Movement.CorridorSeed; int profile = chain.Movement.RouteProfile; float offset = chain.Movement.LateralOffsetPixels;
            string trace = Travel(chain);
            Require(trace == "Checkpoint2A,Checkpoint2B,Checkpoint2D" && chain.Movement.CompletedCheckpointCount == 3, "Hard chain visits all areas in order");
            Require(chain.Movement.CorridorSeed == seed && chain.Movement.RouteProfile == profile && chain.Movement.LateralOffsetPixels == offset, "Every stage preserves movement preferences"); chain.Free();
            Link(a, b, c); Link(c, e, f); Link(e, d); Link(f, d);
            CheckpointNetwork graph = CheckpointNetwork.Capture(a);
            Require(graph.Error.Length == 0 && graph.HasBranches && graph.Nodes.Count == 6, "Branches and shared merges form a valid graph");
            int[] split = new int[4];
            for (uint i = 0; i < 10000; i++)
            { int selected = CheckpointNetwork.Choice(i, 0, 4); split[selected]++; Require(selected == CheckpointNetwork.Choice(i, 0, 4), "Branch stream is reproducible"); }
            Require(split.All(n => n > 2300 && n < 2700), "Equal branch selection has no strong deterministic bias");
            var expectedWave = WaveCatalog.Compose(WaveBuilder.Batch(40, 0, 0, WaveBuilder.Enemy("basic_mk1", weight: 50), WaveBuilder.Enemy("scout_mk1", weight: 50)), new RandomNumberGenerator { Seed = 17 });
            for (uint i = 0; i < 100; i++) CheckpointNetwork.Choice(i, 4, 3);
            var actualWave = WaveCatalog.Compose(WaveBuilder.Batch(40, 0, 0, WaveBuilder.Enemy("basic_mk1", weight: 50), WaveBuilder.Enemy("scout_mk1", weight: 50)), new RandomNumberGenerator { Seed = 17 });
            Require(expectedWave.SequenceEqual(actualWave), "Branch sampling does not consume wave RNG");
            HashSet<string> seen = new();
            for (int i = 0; i < 32; i++)
            {
                Enemy ship = Spawn(); seen.Add(Travel(ship));
                Require(ship.Movement.CheckpointCompleted && ship.IsAlive, "Chain completion precedes Citadel arrival"); ship.Free();
            }
            Require(seen.Count == 3 && seen.All(t => t.StartsWith("Checkpoint2A,") && t.EndsWith("Checkpoint2D")), "Nested random branches reach each terminal/merge route");
            Enemy stable = Spawn();
            for (int i = 0; stable.Movement.CompletedCheckpointCount == 0 && i < 1000; i++) Tick(stable);
            RouteCheckpoint selectedNext = stable.Movement.Checkpoint;
            for (int i = 0; i < 8; i++) { paths.RequestPath(stable.Movement); Drain(); Require(stable.Movement.Checkpoint == selectedNext, "Repaths do not reroll selected branch"); }
            // Snapshot links stay valid for an existing enemy even if the author edits future-spawn links.
            RouteCheckpoint[] oldLinks = selectedNext.NextCheckpoints.Select(p => selectedNext.GetNode<RouteCheckpoint>(p)).ToArray();
            Link(selectedNext); TickUntil(stable, 2);
            Require(stable.Movement.HeadingToCheckpoint, "Captured outgoing link survives live authoring edits");
            Link(selectedNext, oldLinks); stable.Free();
            Link(a, a); Require(CheckpointNetwork.Capture(a).Error.Contains("cycle"), "Self cycles are rejected");
            Enemy cycle = Spawn(); Drain(); Tick(cycle);
            Require(cycle.IsAlive && cycle.Movement.HeadingToCheckpoint && cycle.Movement.RouteStatus.Contains("cycle"), "Invalid chain waits instead of looping or bypassing"); cycle.Free();
            Link(a, b); Link(d, a); Require(CheckpointNetwork.Capture(a).Error.Contains("cycle"), "Multi-node cycles rejected"); Link(d);
            a.NextCheckpoints = new() { new NodePath("../MissingCheckpoint") }; Require(CheckpointNetwork.Capture(a).Error.Contains("Broken"), "Broken outgoing links rejected");
            Link(a, b, b); Require(CheckpointNetwork.Capture(a).Error.Contains("Duplicate"), "Duplicate links cannot secretly weight a branch"); Link(a, b, c);
            // Same snapshot/routing helpers drive F3, with per-sample branch traces and hard count budgets.
            RoutePreviewJob preview = new(nav, definition, start, 24, true, a, true); Finish(preview);
            Require(preview.Results.Count == 24 && preview.Results.All(r => r.CheckpointNames[0] == "Checkpoint2A" && r.CheckpointNames[^1] == "Checkpoint2D"
                && r.CheckpointEntries.Length == r.CheckpointNames.Length), "Preview includes every required checkpoint and entry");
            RoutePreviewJob repeat = new(nav, definition, start, 24, true, a, true); Finish(repeat);
            Require(preview.Results.Zip(repeat.Results).All(pair => pair.First.Points.SequenceEqual(pair.Second.Points) && pair.First.CheckpointNames.SequenceEqual(pair.Second.CheckpointNames)), "Repeated branch previews reproduce complete chains");
            Require(preview.Results.Select(r => string.Join(",", r.CheckpointNames)).Distinct().Count() == 3, "F3 samples all nested branches honestly");
            RoutePreviewJob stale = new(nav, definition, start, 2, true, a, true); stale.Step();
            Vector2 position = e.GlobalPosition; e.GlobalPosition += new Vector2(1, 0); stale.Step();
            Require(stale.Complete && stale.Results.Count == 0, "Downstream marker edits invalidate pending preview"); e.GlobalPosition = position;
            // Shared design overlay draws only metadata, never changes solidity or runs while hidden.
            CheckpointDesignOverlay overlay = grid.Level.GetNode<CheckpointDesignOverlay>("Debug/CheckpointOverlay");
            int revision = nav.Revision; overlay.SetRuntimeVisible(true); overlay.RefreshNow();
            Require(overlay.ZIndex == 4090 && !overlay.ZAsRelative && overlay.CircleCount >= 6 && overlay.ConnectionCount >= 8, "Above-tile owner sees circles and every authored arrow");
            int refreshes = overlay.RefreshCount; overlay.RefreshNow(); Require(overlay.RefreshCount == refreshes, "Unchanged design drawing stays cached");
            e.GlobalPosition += new Vector2(1, 0); overlay.RefreshNow(); Require(overlay.RefreshCount > refreshes && nav.Revision == revision, "Moving a marker redraws without navigation changes"); e.GlobalPosition = position;
            overlay.SetRuntimeVisible(false); Require(!overlay.IsProcessing(), "Design overlay has no hidden runtime work");
            foreach (string scene in new[] { "Level001/Level001", "Level020/Level020", "LevelTemplate" })
            {
                Node root = GD.Load<PackedScene>($"res://LEVELS/{scene}.tscn").Instantiate();
                try { var owner = root.GetNode<CheckpointDesignOverlay>("Debug/CheckpointOverlay"); Require(owner.ZIndex == 4090 && !owner.ZAsRelative, "All authored/template scenes draw above tiles"); }
                finally { root.Free(); }
            }
            Stopwatch clock = Stopwatch.StartNew();
            for (int i = 0; i < 500; i++) Require(CheckpointNetwork.Capture(a).Error.Length == 0, "500 graph snapshots remain valid");
            GD.Print($"CHAIN BENCH: 500 six-node validation snapshots {clock.Elapsed.TotalMilliseconds:0.00}ms; branch split {string.Join('/', split)} /10000");
            GD.Print("PASS: checkpoint chains, nested branches/merges, stable repaths, snapshot edits, cycle/broken/duplicate validation, RNG isolation, F3 chain parity and above-tile editor metadata");
        }
        finally { a.NextCheckpoints.Clear(); foreach (RouteCheckpoint node in new[] { b, c, d, e, f }) node.Free(); definition.Dispose(); }

        RouteCheckpoint Add(string name, int x, int y)
        { RouteCheckpoint node = new() { Name = name, RadiusPixels = 20 }; grid.Level.AddChild(node); node.GlobalPosition = grid.CellToWorldCenter(new(x, y)); return node; }
        void Link(RouteCheckpoint from, params RouteCheckpoint[] to)
        { from.NextCheckpoints = new(); foreach (RouteCheckpoint target in to) from.NextCheckpoints.Add(from.GetPathTo(target)); }
        Enemy Spawn()
        {
            Enemy ship = spawns.Catalog.Find("basic_mk1").Instantiate<Enemy>(); ship.Definition = definition;
            main.GetNode("World/Enemies").AddChild(ship); ship.GlobalPosition = start;
            ship.Initialize(spawns.Registry, paths, checkpoint: a, requiresCheckpoint: true); ship.Movement.SetPhysicsProcess(false); Drain(); return ship;
        }
        void Drain()
        { for (int i = 0; paths.PendingCount > 0 && i < 10000; i++) { paths._PhysicsProcess(1.0 / 60); Require(paths.LastFrameCalculations <= paths.Config.MaxPathCalculationsPerFrame, "Chain queries retain gameplay A* budget"); } Require(paths.PendingCount == 0, "Chain requests drain"); }
        void Tick(Enemy ship) { paths._PhysicsProcess(0.05); if (ship.IsAlive) ship.Movement._PhysicsProcess(0.05); }
        void TickUntil(Enemy ship, int completed) { for (int i = 0; ship.Movement.CompletedCheckpointCount < completed && i < 2000; i++) Tick(ship); }
        string Travel(Enemy ship)
        {
            List<string> trace = new();
            for (int i = 0; !ship.Movement.CheckpointCompleted && ship.IsAlive && i < 2000; i++)
            {
                RouteCheckpoint current = ship.Movement.Checkpoint; int completed = ship.Movement.CompletedCheckpointCount; Tick(ship);
                if (ship.Movement.CompletedCheckpointCount > completed)
                { Require(current.Contains(ship.GlobalPosition), "Only physical entry advances each link"); trace.Add(current.Name.ToString()); }
            }
            Require(ship.Movement.CheckpointCompleted, "All required links complete"); return string.Join(",", trace);
        }
    }
    // =========================================================
    // Finish deterministic debug samples without advancing world gameplay or exceeding per-step query counts.
    private static void Finish(RoutePreviewJob job)
    { for (int i = 0; !job.Complete && i < 100000; i++) { job.Step(); Require(job.LastCalculations <= 1, "Preview chain query cap"); } Require(job.Complete, "Preview chains complete"); }
    // =========================================================
    // Fail the owning checkpoint runner on any branch, safety or authoring contract regression.
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
