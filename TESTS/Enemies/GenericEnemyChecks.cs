// Scene-authoring regressions and bounded before/after instantiation benchmarks; no gameplay runtime helpers.
using Godot;
using System;
using System.Diagnostics;
using System.Linq;
using static MissionFixture;

public partial class GenericEnemyChecks : Node
{
    // =========================================================
    // Exercise inheritance without touching committed scenes, then existing production subsystems.
    public override async void _Ready()
    {
        try
        {
            Scenes();
            using (MissionFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                EnemyDefinition definition = f.Spawns.Catalog.DefinitionFor("basic_mk1"); float shield = definition.MaxShield;
                try
                {
                    definition.MaxShield = 100;
                    f.Spawns.Paths.Config = (EnemyMasterConfig)f.Spawns.Paths.Config.Duplicate();
                    f.Spawns.Paths.Config.PortalSpawnOffsetPixels = 0;
                    foreach (PackedScene scene in f.Spawns.Catalog.Scenes)
                    {
                        Enemy ship = f.Spawns.Spawn(scene, true); Require(ship != null, "Every inherited type spawns");
                        AmmoLoopFixture.Disable(ship);
                        for (int i = 0; f.Spawns.Paths.PendingCount > 0 && i < 200; i++) f.Spawns.Paths._PhysicsProcess(1.0 / 60);
                        Vector2 start = ship.GlobalPosition; ship.Movement._PhysicsProcess(1.0 / 60);
                        Require(ship.GlobalPosition != start && float.IsFinite(ship.RemainingPathDistance), "Every inherited type follows its real route"); ship.Arrive();
                    }
                    f.Health.Initialize(f.Health.MaxHealth);
                    EnemyCatalogChecks.Run(f.Main, f.Spawns);
                    EnemyShieldChecks.Run(f.Main, f.Spawns);
                    ShieldEmitterChecks.Run(f.Main, f.Spawns);
                    WaveChecks.Run(f.Main, f.Spawns.Paths.Navigation.Grid, f.Spawns);
                    f.Spawns.Paths.Config.PortalSpawnOffsetPixels = 8;
                    PortalSpawnChecks.Run(f.Spawns.Paths.Navigation.Grid, f.Spawns);
                    CitadelHealthChecks.Run(f.Main, f.Spawns);
                    AreaDamageChecks.Run(f.Main, f.Spawns);
                    await LaserTowerChecks.Run(f.Main, f.Spawns);
                    await BlasterCannonChecks.Run(f.Main, f.Spawns);
                    await ShockwaveChecks.Run(f.Main, f.Spawns);
                }
                finally { definition.MaxShield = shield; }
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: GE1–GE3 inherited scenes, propagation, catalog, shields/abilities, waves/portals, Citadel and area damage"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }

    // =========================================================
    // Changing the actual parent text in a user:// fixture must propagate through every real child override.
    private static void Scenes()
    {
        string parent = FileAccess.GetFileAsString("res://ENEMIES/Shared/GenericEnemy.tscn");
        Write("user://ge-parent.tscn", parent.Replace("[node name=\"Movement\" type=\"Node\" parent=\".\"]", "[node name=\"Movement\" type=\"Node\" parent=\".\"]\nprocess_priority = 7")
            .Replace("[node name=\"DefenseBar\" parent=\".\" instance=ExtResource(\"3_bar\")]", "[node name=\"DefenseBar\" parent=\".\" instance=ExtResource(\"3_bar\")]\nposition = Vector2(13, -37)"));
        foreach (PackedScene scene in EnemyCatalog.Default.Scenes)
        {
            string source = FileAccess.GetFileAsString(scene.ResourcePath);
            Require(source.Contains("instance=ExtResource(\"1_base\")") && !source.Contains("name=\"Movement\"") && !source.Contains("name=\"DefenseBar\""), "Shared nodes have no redundant overrides");
            Write("user://ge-child.tscn", source.Replace("res://ENEMIES/Shared/GenericEnemy.tscn", "user://ge-parent.tscn"));
            using PackedScene edited = ResourceLoader.Load<PackedScene>("user://ge-child.tscn", cacheMode: ResourceLoader.CacheMode.Ignore);
            Enemy normal = scene.Instantiate<Enemy>(), changed = edited.Instantiate<Enemy>();
            try
            {
                Require(normal.GetNode<EnemyMovement>("Movement") != null && normal.GetNode<DefenseBar>("DefenseBar").Position == Vector2.Zero, "Exact common paths/default transform");
                Require(changed.GetNode<DefenseBar>("DefenseBar").Position == new Vector2(13, -37) && changed.GetNode<EnemyMovement>("Movement").ProcessPriority == 7, "Parent bar and movement edits propagate");
                Require(changed.Definition == normal.Definition && changed.GetNode<Sprite2D>("Sprite").Texture == normal.GetNode<Sprite2D>("Sprite").Texture, "Independent definition/art retained");
                bool ability = normal.Definition.EnemyId is "transporter_mk1" or "shield_emitter_mk1";
                Require(normal.GetChildCount() == (ability ? 4 : 3) && (normal.GetNodeOrNull<Node>("Abilities") != null) == ability, "Only actual optional abilities add nodes");
                Require(!ability || changed.GetNode("Abilities").GetChild(0).GetType() == normal.GetNode("Abilities").GetChild(0).GetType(), "Optional components survive inheritance");
                normal.GetNode<DefenseBar>("DefenseBar").SetHealth(48, 60);
                Require(normal.GetNode<DefenseBar>("DefenseBar").HealthFraction == .8f, "Shared defense bar still draws correct fraction");
            }
            finally { normal.Free(); changed.Free(); }
            string baseline = "user://ge-baseline-" + scene.ResourcePath.GetFile() ;
            if (FileAccess.FileExists(baseline)) Benchmark(ResourceLoader.Load<PackedScene>(baseline), scene);
            else GD.Print("GE BENCH skipped: supply pre-migration scene at " + baseline);
        }
        DirAccess.RemoveAbsolute("user://ge-child.tscn"); DirAccess.RemoveAbsolute("user://ge-parent.tscn");
    }

    // =========================================================
    // Keep 100/500 instances concurrently alive; compare identical node counts and warm creation cost.
    private static void Benchmark(PackedScene before, PackedScene after)
    {
        foreach (int count in new[] { 100, 500 })
        {
            double[][] samples = { new double[5], new double[5] }; int[] nodes = new int[2];
            for (int round = 0; round < 5; round++)
            for (int pass = 0; pass < 2; pass++)
            {
                PackedScene scene = pass == 0 ? before : after; Node[] roots = new Node[count];
                Stopwatch watch = Stopwatch.StartNew();
                for (int i = 0; i < count; i++) { roots[i] = scene.Instantiate(); if (round == 0) nodes[pass] += Count(roots[i]); }
                watch.Stop(); samples[pass][round] = watch.Elapsed.TotalMilliseconds;
                foreach (Node root in roots) root.Free();
            }
            foreach (double[] sample in samples) Array.Sort(sample);
            Require(nodes[0] == nodes[1], "Inheritance adds no runtime nodes");
            GD.Print($"GE BENCH {after.ResourcePath.GetFile()} {count}: baseline median={samples[0][2]:F3}ms inherited median={samples[1][2]:F3}ms nodes={nodes[1]}");
        }
    }
    // =========================================================
    // Count full subtrees, including optional components and shared bar internals.
    private static int Count(Node node) => 1 + node.GetChildren().Sum(Count);
    // =========================================================
    // Isolated user-data fixtures never save changes to project assets.
    private static void Write(string path, string text) { using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write); file.StoreString(text); }
}
