// Production menu/portal integration checks: bounded batches, pause, cancellation and independent wave ownership.
using Godot;
using System;
using System.Linq;
using static AmmoLoopFixture;

public partial class EnemySpawnMenuChecks : Node
{
    #region Runner
    // =========================================================
    // Run against real map services, with explicit ticks to avoid renderer-dependent timing.
    public override async void _Ready()
    {
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                LevelDebugController debug = f.Main.GetNode<LevelDebugController>("World/Level/Debug");
                EnemySpawnMenu menu = debug.SpawnMenu;
                Require(menu != null && !menu.IsOpen && !menu.IsSpawning, "Menu initializes closed and idle");
                int before = f.Spawns.Registry.Living.Count;
                debug._UnhandledKeyInput(new InputEventKey { Keycode = Key.F2, Pressed = true });
                Require(menu.IsOpen && f.Spawns.Registry.Living.Count == before, "F2 opens without spawning");
                debug._UnhandledKeyInput(new InputEventKey { Keycode = Key.F2, Pressed = true, Echo = true });
                Require(menu.IsOpen, "F2 ignores repeat");
                debug._Input(new InputEventKey { Keycode = Key.Escape, Pressed = true });
                Require(!menu.IsOpen, "Escape closes");
                int waveCreated = 0; f.Spawns.WaveEnemyCreated += _ => waveCreated++;
                Require(!menu.StartBatch(-1, 2, 0) && !menu.StartBatch(0, 0, 0) && !menu.StartBatch(0, 1, double.NaN), "Reject invalid inputs");
                menu.MaxSpawnsPerTick = 2;
                Require(menu.StartBatch(0, 5, 0) && !menu.StartBatch(1, 5, 0), "One captured batch");
                menu._PhysicsProcess(.016);
                Require(menu.Spawned == 2 && menu.Remaining == 3, "Zero gap bounded per tick");
                menu.Close(); menu._PhysicsProcess(.016); menu._PhysicsProcess(.016);
                Require(menu.Spawned == 5 && !menu.IsSpawning && !menu.IsPhysicsProcessing(), "Exact count completes while closed and sleeps");
                Require(waveCreated == 0, "Debug ships do not claim authored wave ownership");
                foreach (Enemy e in f.Spawns.Registry.Living.ToArray()) e.Free();
                Require(menu.StartBatch(0, 3, .2), "Timed batch starts");
                menu._PhysicsProcess(.01); Require(menu.Spawned == 1, "First timed delivery");
                menu._PhysicsProcess(.1); Require(menu.Spawned == 1, "Gap delays next ship");
                GetTree().Paused = true; menu._PhysicsProcess(10); GetTree().Paused = false;
                Require(menu.Spawned == 1, "Pause freezes batch");
                menu._PhysicsProcess(.11); Require(menu.Spawned == 2, "Gap expires");
                menu.CancelBatch(); menu._PhysicsProcess(10);
                Require(menu.Spawned == 2 && !menu.IsSpawning, "Cancel keeps existing ships, stops pending");
                RandomPortals(f);
                menu.StartBatch(0, 10, 0); debug.Enabled = false;
                Require(!menu.IsSpawning && !menu.IsOpen, "Disabled level tool cancels batch");
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: F2 menu, bounded exact batches, gaps, pause, close, cancel, validation and wave isolation"); GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    // Registered portals are sampled independently per ship; absent portals never fall back to map edges.
    private static void RandomPortals(AmmoLoopFixture f)
    {
        foreach (Enemy e in f.Spawns.Registry.Living.ToArray()) e.Free();
        var nav = f.Spawns.Paths.Navigation; var grid = nav.Grid; var level = grid.Level;
        level.Passable.Clear(); level.Impassable.Clear(); level.Interactive.Clear();
        for (int y = 0; y < grid.Config.HeightCells; y++) for (int x = 0; x < grid.Config.WidthCells; x++)
            level.Passable.SetCell(new(x, y), 0, new(2, 0));
        Vector2 a = grid.CellToWorldCenter(new(5, 10)), b = grid.CellToWorldCenter(new(25, 10));
        foreach (var pair in new[] { ("TestPortalA", a), ("TestPortalB", b) })
        {
            Node2D portal = new() { Name = pair.Item1 }; portal.AddChild(new Marker2D { Name = "SpawnExit" });
            level.AddChild(portal); portal.GlobalPosition = pair.Item2;
        }
        level.Settings.SpawnMode = LevelConfig.SpawnSourceMode.Portals;
        level.Settings.ActivePortals = new() { new NodePath("TestPortalA"), new NodePath("TestPortalB") };
        grid.RebuildTerrain(); nav.Refresh();
        RandomNumberGenerator random = new() { Seed = 123 }; int first = 0, second = 0;
        for (int i = 0; i < 40; i++)
        {
            Enemy ship = f.Spawns.SpawnRandomPortal(f.Spawns.Catalog.Scenes[0], random);
            Require(ship != null, "Random portal delivery accepted");
            if (ship.GlobalPosition.DistanceTo(a) < 40) first++;
            else if (ship.GlobalPosition.DistanceTo(b) < 40) second++;
            else throw new InvalidOperationException("Ship did not use a registered portal");
            ship.Free();
        }
        Require(first > 0 && second > 0, "Both portals used within one enemy batch");
        level.Settings.SpawnMode = LevelConfig.SpawnSourceMode.MapEdges; grid.RebuildTerrain();
        Require(f.Spawns.SpawnRandomPortal(f.Spawns.Catalog.Scenes[0], random) == null, "Portal-only delivery never falls back to edges");
    }
    // =========================================================
    // Fail fast with the behavior under test.
    private static void Require(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
    #endregion
}
