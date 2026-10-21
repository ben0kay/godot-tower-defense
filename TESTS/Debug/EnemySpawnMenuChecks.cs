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
                Preview(f, menu);
                HoverPreview(f, menu);
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
                Require(menu.StartBatch(0, 5, 0), "First captured batch");
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
                IndependentBatches(f, menu);
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
    // Real selector changes render authored hull/gun geometry without ever registering a gameplay enemy.
    private static void Preview(AmmoLoopFixture f, EnemySpawnMenu menu)
    {
        OptionButton choice = (OptionButton)menu.FindChild("Enemy", true, false);
        int before = f.Spawns.Registry.Living.Count;
        for (int index = 0; index < f.Spawns.Catalog.Scenes.Count; index++)
        {
            choice.Select(index); choice.EmitSignal(OptionButton.SignalName.ItemSelected, index);
            Require(menu.SpritePreview.SpriteCount >= 1 && menu.SpritePreview.ArtworkBounds.Size.X > 0, "Catalog artwork preview populated");
            Require(!menu.SpritePreview.IsProcessing() && !menu.SpritePreview.IsPhysicsProcessing(), "Static artwork preview does not tick");
            Node source = f.Spawns.Catalog.Scenes[index].Instantiate();
            try
            {
                Sprite2D hull = source.GetNode<Sprite2D>("Sprite");
                Sprite2D copy = menu.SpritePreview.GetNode<Node2D>("Artwork").GetChildren().OfType<Sprite2D>().First();
                Require(copy.Texture == hull.Texture && copy.Scale.IsEqualApprox(hull.Scale) && copy.GetScript().AsGodotObject() == null,
                    "Native hull texture/scale copied without scripts");
                if (source is Enemy enemy && enemy.Definition.EnemyId == "gunner_drone_mk1")
                    Require(menu.SpritePreview.SpriteCount == 2, "Separate gun artwork included");
            }
            finally { source.Free(); }
        }
        Require(f.Spawns.Registry.Living.Count == before, "Preview never creates a live enemy");
        Node2D large = new() { Scale = Vector2.One * 2 };
        large.AddChild(new Sprite2D { Name = "Sprite", Texture = new PlaceholderTexture2D { Size = new(500, 300) } });
        PackedScene packed = new(); large.GetNode("Sprite").Owner = large; packed.Pack(large);
        menu.SpritePreview.ShowScene(packed); large.Free();
        Require(menu.SpritePreview.ArtworkBounds.Size.IsEqualApprox(new Vector2(1000, 600))
            && menu.SpritePreview.CustomMinimumSize.X >= 1000, "Oversized artwork keeps authored scale instead of shrinking");
        choice.Select(0); choice.EmitSignal(OptionButton.SignalName.ItemSelected, 0);
    }
    // =========================================================
    // The popup highlight previews other enemy art without committing a new spawn type or creating gameplay ships.
    private static void HoverPreview(AmmoLoopFixture f, EnemySpawnMenu menu)
    {
        OptionButton choice = (OptionButton)menu.FindChild("Enemy", true, false);
        PopupMenu popup = choice.GetPopup();
        int selected = choice.Selected, next = (selected + 1) % choice.ItemCount;
        int live = f.Spawns.Registry.Living.Count;
        menu.Toggle();
        menu.Call("OnEnemyPopupOpened"); // Emulate the popup's about-to-open signal independently of window-system rendering.
        Require(menu.IsProcessing(), "Open enemy popup enables lightweight hover polling");
        popup.SetFocusedItem(next); menu._Process(0);
        Require(menu.PreviewEnemyIndex == next && choice.Selected == selected && f.Spawns.Registry.Living.Count == live,
            "Hover highlights a different sprite without changing selected enemy or spawning a ship");
        popup.SetFocusedItem(selected); menu._Process(0);
        Require(menu.PreviewEnemyIndex == selected, "Returning hover to the selected item updates the preview");
        popup.SetFocusedItem(next); menu._Process(0);
        menu.Call("OnEnemyPopupClosed");
        Require(menu.PreviewEnemyIndex == selected && !menu.IsProcessing() && choice.Selected == selected,
            "Dismissing the list restores the committed selection and stops polling");
        menu.Close();
        Require(!menu.IsProcessing() && !menu.IsOpen, "Closed F2 menu never polls the popup");
    }
    // =========================================================
    // Concurrent batches retain different intervals/types/flags and share a fair global spawn budget.
    private static void IndependentBatches(AmmoLoopFixture f, EnemySpawnMenu menu)
    {
        foreach (Enemy e in f.Spawns.Registry.Living.ToArray()) e.Free();
        menu.MaxSpawnsPerTick = 2;
        Require(menu.StartBatch(0, 3, .5, true, false) && menu.StartBatch(1, 3, .1, false, true), "Concurrent batches accepted");
        menu._PhysicsProcess(.016);
        Require(menu.ActiveBatchCount == 2 && menu.Spawned == 2 && menu.Remaining == 4, "Global budget shared by both ready batches");
        Enemy shield = f.Spawns.Registry.Living.Single(e => e.Definition.EnemyId == "basic_mk1");
        Enemy stealth = f.Spawns.Registry.Living.Single(e => e.Definition.EnemyId == "scout_mk1");
        Require(shield.CurrentShield > 0 && !shield.HasStealth, "First batch captures shield only");
        Require(stealth.HasStealth && stealth.CurrentShield == 0, "Second batch captures stealth only");
        menu._PhysicsProcess(.11);
        Require(menu.Spawned == 3 && f.Spawns.Registry.Living.Count(e => e.Definition.EnemyId == "basic_mk1") == 1, "Faster batch proceeds while slower waits");
        menu._PhysicsProcess(.11);
        Require(menu.ActiveBatchCount == 1 && menu.Spawned == 4, "Fast batch completes independently");
        menu._PhysicsProcess(.3);
        Require(menu.Spawned == 5 && menu.ActiveBatchCount == 1, "Slow batch preserves its timer");
        menu.CancelBatch();
        foreach (Enemy e in f.Spawns.Registry.Living.ToArray()) e.Free();
        ((SpinBox)menu.FindChild("Amount", true, false)).Value = 1;
        ((SpinBox)menu.FindChild("Gap", true, false)).Value = 0;
        ((CheckBox)menu.FindChild("Shielded", true, false)).ButtonPressed = true;
        ((CheckBox)menu.FindChild("Stealthed", true, false)).ButtonPressed = true;
        menu.FindChildren("*", "Button", true, false).OfType<Button>().Single(b => b.Text == "Spawn batch").EmitSignal(Button.SignalName.Pressed);
        menu._PhysicsProcess(.016);
        Enemy both = f.Spawns.Registry.Living.Single(); Require(both.HasStealth && both.CurrentShield > 0, "Shield and stealth combine"); both.Free();
        menu.MaxSpawnsPerTick = 1;
        menu.StartBatch(0, 4, 0); menu.StartBatch(1, 4, 0);
        menu._PhysicsProcess(.016); menu._PhysicsProcess(.016);
        Require(menu.Spawned == 2 && f.Spawns.Registry.Living.Select(e => e.Definition.EnemyId).Distinct().Count() == 2, "One-per-tick budget rotates fairly");
        menu.FindChildren("*", "Button", true, false).OfType<Button>()
            .First(b => b.Text == "Cancel" && !b.GetParent().IsQueuedForDeletion()).EmitSignal(Button.SignalName.Pressed);
        Require(menu.ActiveBatchCount == 1 && menu.Remaining == 3, "Per-row cancel leaves other batch intact");
        menu.CancelBatch();
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
