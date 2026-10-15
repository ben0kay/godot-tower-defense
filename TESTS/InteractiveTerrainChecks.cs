// Real authoring rules, swept/periodic Lava exposure, canonical damage and cleanup checks.
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

public static class InteractiveTerrainChecks
{
    private readonly record struct Cell(TileMapLayer Layer, Vector2I Position, int Source, Vector2I Atlas, int Alternative);
    // =========================================================
    // Restore every edited cell/config; this suite never writes the designer's saved map.
    public static void Run(Node main, SpawnManager spawns, WorldGrid grid)
    {
        InteractiveTileManager manager = grid.InteractiveTiles;
        Require(manager != null && grid.Level.Interactive != null, "One manager and Interactive palette load");
        LavaTileConfig config = manager.LavaConfig;
        var original = (config.DamagePerTick, config.DamageIntervalSeconds, config.DamageOnEntry, config.DamageType);
        List<Cell> backup = new();
        foreach (TileMapLayer layer in new[] { grid.Level.Passable, grid.Level.Impassable, grid.Level.Interactive })
            for (int y = 6; y <= 16; y++) for (int x = 6; x <= 30; x++)
            {
                Vector2I cell = new(x, y); backup.Add(new(layer, cell, layer.GetCellSourceId(cell), layer.GetCellAtlasCoords(cell), layer.GetCellAlternativeTile(cell)));
                layer.EraseCell(cell);
            }
        try
        {
            Palettes(grid); Rules(grid); Set(config, 5, .5f, true, DamageType.Kinetic);
            foreach (int rate in new[] { 30, 60, 120 }) Stationary(spawns, grid, rate);
            SpawnAndUnload(main, spawns, grid);
            MovingRates(spawns, grid);
            Crossings(spawns, grid); Boundaries(spawns, grid); Edits(spawns, grid);
            Defenses(main, spawns, grid); ActualMovement(spawns, grid); Population(spawns, grid); LethalSpawnWave(main, spawns, grid); Unload(main); InvalidSettings();
        }
        finally
        {
            AreaDamageChecks.Clear(spawns);
            foreach (Cell cell in backup)
                if (cell.Source < 0) cell.Layer.EraseCell(cell.Position);
                else cell.Layer.SetCell(cell.Position, cell.Source, cell.Atlas, cell.Alternative);
            Set(config, original.Item1, original.Item2, original.Item3, original.Item4); grid.RebuildTerrain();
        }
        GD.Print("PASS: Interactive Foundation/Lava palettes/rules, entry+periodic30/60/120Hz, contiguous/swept/gapped/corner/teleport exposure, edits, defenses/rewards/noXP, lethal movement and500 occupants");
    }
    // =========================================================
    // Both scenes expose64px sources; Foundation retains its original artwork and atlas coordinate.
    private static void Palettes(WorldGrid grid)
    {
        WorldLevel template = GD.Load<PackedScene>("res://LEVELS/LevelTemplate.tscn").Instantiate<WorldLevel>();
        try
        {
            foreach (WorldLevel level in new[] { grid.Level, template })
            {
                TileSet set = level.Interactive.TileSet;
                Require(set.TileSize == new Vector2I(64, 64) && set.GetSourceCount() == 2, "Interactive palette exposes two64px sources");
                TileSetAtlasSource foundation = (TileSetAtlasSource)set.GetSource(0), lava = (TileSetAtlasSource)set.GetSource(1);
                Require(foundation.HasTile(new(1, 0)) && foundation.Texture.ResourcePath.EndsWith("prototype_atlas.svg"), "Foundation keeps old art");
                Require(lava.Texture.GetWidth() == 64 && lava.GetTileData(Vector2I.Zero, 0).GetCustomData("interaction").AsString() == "lava", "Lava art/metadata load");
            }
        }
        finally { template.Free(); }
    }
    // =========================================================
    // Overlay rules preserve structural walls, reservations and already-blocked platform purchases.
    private static void Rules(WorldGrid grid)
    {
        for (int y = 6; y <= 16; y++) for (int x = 6; x <= 30; x++) grid.Level.Passable.SetCell(new(x, y), 0, new(2, 0));
        Vector2I cell = new(10, 10); grid.Level.Interactive.SetCell(cell, 0, new(1, 0)); grid.RebuildTerrain();
        Require(grid.CanBuild(cell) && grid.BlocksEnemies(cell), "Foundation stays buildable/blocked");
        int revision = grid.NavigationRevision;
        Require(grid.TryOccupy(cell) && grid.NavigationRevision == revision, "Solid Foundation occupancy does not repath"); grid.ReleaseOccupancy(cell);
        grid.Level.Interactive.SetCell(cell, 1, Vector2I.Zero); grid.RebuildTerrain();
        Require(!grid.CanBuild(cell) && !grid.BlocksEnemies(cell) && !grid.TryOccupy(cell), "Lava overrides buildable floor");
        revision = grid.NavigationRevision; grid.Level.Interactive.EraseCell(cell); grid.RebuildTerrain();
        Require(grid.NavigationRevision == revision, "Hazard-only edit avoids A* revision");
        grid.Level.Impassable.SetCell(cell, 0, Vector2I.Zero); grid.Level.Interactive.SetCell(cell, 1, Vector2I.Zero); grid.RebuildTerrain();
        Require(grid.BlocksEnemies(cell) && !grid.CanBuild(cell) && !grid.InteractiveTiles.IsLava(grid.CellToWorldCenter(cell)), "Lava cannot erase structural blocker");
        grid.Level.Impassable.EraseCell(cell); grid.Level.Interactive.Clear(); grid.RebuildTerrain();
        Require(!grid.CanBuild(grid.WorldToCell(grid.Level.Citadel.GlobalPosition)), "Citadel stays reserved");
        foreach (NodePath path in grid.Level.Settings.ActivePortals)
            Require(!grid.CanBuild(grid.WorldToCell(grid.Level.GetNode<Node2D>(path).GlobalPosition)), "Portal stays reserved");
    }
    // =========================================================
    // At2sec, entry plus four periodic hits are identical across supported physics rates.
    private static void Stationary(SpawnManager spawns, WorldGrid grid, int rate)
    {
        AreaDamageChecks.Clear(spawns); Lava(grid, 10, 12); Enemy enemy = Spawn(spawns, grid, 10); Near(enemy.Health, 55, "Immediate arrival hit");
        for (int tick = 0; tick < rate * 2; tick++) grid.InteractiveTiles._PhysicsProcess(1.0 / rate);
        Near(enemy.Health, 35, "Entry+four ticks at " + rate + "Hz");
        enemy.GlobalPosition = grid.CellToWorldCenter(new(9, 10)); grid.InteractiveTiles._PhysicsProcess(2); Near(enemy.Health, 35, "Exit stops ticks");
        enemy.GlobalPosition = grid.CellToWorldCenter(new(10, 10)); Near(enemy.Health, 30, "Re-entry resets timer and hits");
        AreaDamageChecks.Clear(spawns); grid.InteractiveTiles.LavaConfig.DamageOnEntry = false; enemy = Spawn(spawns, grid, 10);
        Near(enemy.Health, 60, "Disabled entry does not hit"); grid.InteractiveTiles._PhysicsProcess(.49); Near(enemy.Health, 60, "Interval not expired");
        grid.InteractiveTiles._PhysicsProcess(.01); Near(enemy.Health, 55, "First hit after configured interval");
        AreaDamageChecks.Clear(spawns); grid.InteractiveTiles.LavaConfig.DamageOnEntry = true;
    }
    // =========================================================
    // Initialization already on Lava and unload use the same state owner, without requiring movement.
    private static void SpawnAndUnload(Node main, SpawnManager spawns, WorldGrid grid)
    {
        AreaDamageChecks.Clear(spawns); Lava(grid, 10, 10);
        Enemy enemy = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn").Instantiate<Enemy>();
        main.GetNode("World/Enemies").AddChild(enemy); enemy.GlobalPosition = grid.CellToWorldCenter(new(10, 10));
        enemy.Initialize(spawns.Registry, spawns.Paths); enemy.Movement.SetPhysicsProcess(false);
        Near(enemy.Health, 55, "Newly initialized stationary enemy gets immediate Lava entry");
        grid.InteractiveTiles._PhysicsProcess(2); Near(enemy.Health, 35, "Fresh spawn continues stationary ticks");
        AreaDamageChecks.Clear(spawns);
        Set(grid.InteractiveTiles.LavaConfig, 0, .01f, false, DamageType.Kinetic);
        enemy = Spawn(spawns, grid, 10); grid.InteractiveTiles._PhysicsProcess(1000000); Near(enemy.Health, 60, "Zero-damage huge delta fast-forwards without dropped effective damage");
        AreaDamageChecks.Clear(spawns); Set(grid.InteractiveTiles.LavaConfig, 5, .5f, true, DamageType.Kinetic);
    }

    // =========================================================
    // Swept timing is also independent of movement sample rate, not just stationary manager ticks.
    private static void MovingRates(SpawnManager spawns, WorldGrid grid)
    {
        foreach (int rate in new[] { 30, 60, 120 })
        {
            AreaDamageChecks.Clear(spawns); Lava(grid, 10, 12); Enemy enemy = Spawn(spawns, grid, 9);
            Vector2 start = enemy.GlobalPosition, end = grid.CellToWorldCenter(new(13, 10));
            for (int tick = 1; tick <= rate * 2; tick++) Travel(enemy, grid, start.Lerp(end, tick / (float)(rate * 2)), 1.0 / rate);
            Near(enemy.Health, 40, "Moving exposure has one entry+three ticks at " + rate + "Hz");
        }
    }

    // =========================================================
    // Preserve exposure through adjacent cells and count only actual time on Lava within the frame.
    private static void Crossings(SpawnManager spawns, WorldGrid grid)
    {
        AreaDamageChecks.Clear(spawns); Lava(grid, 10, 12); Enemy enemy = Spawn(spawns, grid, 9); Step(enemy, grid, 13, 1);
        Near(enemy.Health, 50, "Three adjacent cells: one entry+one tick for0.75sec"); grid.InteractiveTiles._PhysicsProcess(2); Near(enemy.Health, 50, "No timer after fast exit");
        AreaDamageChecks.Clear(spawns); enemy = Spawn(spawns, grid, 9); Step(enemy, grid, 13, .01); Near(enemy.Health, 55, "Fast crossing cannot tunnel or double-enter");
        AreaDamageChecks.Clear(spawns); grid.Level.Interactive.EraseCell(new(11, 10)); grid.RebuildTerrain();
        enemy = Spawn(spawns, grid, 9); Step(enemy, grid, 13, .01); Near(enemy.Health, 50, "Separate regions produce two entries");
        AreaDamageChecks.Clear(spawns); Lava(grid, 10, 12); enemy = Spawn(spawns, grid, 9);
        Travel(enemy, grid, grid.CellToWorldCenter(new(10, 10)) - new Vector2(31, 0), 1); Near(enemy.Health, 55, "Late entry has no whole-frame credit");
        grid.InteractiveTiles._PhysicsProcess(.45); Near(enemy.Health, 55, "Partial-frame exposure only");
        grid.InteractiveTiles._PhysicsProcess(.05); Near(enemy.Health, 50, "Partial-frame timer continues while stationary");
        AreaDamageChecks.Clear(spawns); enemy = Spawn(spawns, grid, 9); enemy.GlobalPosition = grid.CellToWorldCenter(new(13, 10)); Near(enemy.Health, 60, "Teleport skips intervening Lava");
        enemy.GlobalPosition = grid.CellToWorldCenter(new(11, 10)); Near(enemy.Health, 55, "Teleport onto Lava enters once");
        enemy.Rotation += .5f; Near(enemy.Health, 55, "Rotation cannot re-enter");
    }
    // =========================================================
    // Ignore corner-only contact; accept diagonal interiors, exact endpoints and tiny segments.
    private static void Boundaries(SpawnManager spawns, WorldGrid grid)
    {
        AreaDamageChecks.Clear(spawns); Lava(grid, 10, 10); Enemy enemy = Spawn(spawns, grid, 9);
        Travel(enemy, grid, grid.CellToWorldCenter(new(10, 9)), .01); Near(enemy.Health, 60, "Corner-only touch is not entry");
        enemy.GlobalPosition = grid.CellToWorldCenter(new(9, 9)); Travel(enemy, grid, grid.CellToWorldCenter(new(11, 11)), .01); Near(enemy.Health, 55, "Diagonal interior entry");
        AreaDamageChecks.Clear(spawns); enemy = Spawn(spawns, grid, 9);
        Travel(enemy, grid, grid.CellToWorldCenter(new(10, 10)) - new Vector2(32, 0), .1); Near(enemy.Health, 55, "Exact endpoint belongs to Lava floor cell");
        Travel(enemy, grid, enemy.GlobalPosition + new Vector2(.001f, 0), .1); Near(enemy.Health, 55, "Tiny segment keeps exposure");
    }
    // =========================================================
    // Live edits discover occupants spatially and preserve unchanged timers, without duplicate entry.
    private static void Edits(SpawnManager spawns, WorldGrid grid)
    {
        AreaDamageChecks.Clear(spawns); grid.Level.Interactive.Clear(); grid.RebuildTerrain(); Enemy enemy = Spawn(spawns, grid, 10);
        Lava(grid, 10, 10); Near(enemy.Health, 55, "Painting under stationary enemy enters");
        grid.InteractiveTiles._PhysicsProcess(.25); grid.RebuildTerrain(); Near(enemy.Health, 55, "Same repaint does not enter");
        grid.InteractiveTiles._PhysicsProcess(.25); Near(enemy.Health, 50, "Repaint keeps interval progress");
        grid.Level.Interactive.Clear(); grid.RebuildTerrain(); grid.InteractiveTiles._PhysicsProcess(1); Near(enemy.Health, 50, "Erasing stops damage");
        Lava(grid, 10, 10); Near(enemy.Health, 45, "Painting again enters");
        Set(grid.InteractiveTiles.LavaConfig, 2, .25f, false, DamageType.Kinetic); grid.InteractiveTiles._PhysicsProcess(.75); Near(enemy.Health, 39, "Config edits and all owed large-delta ticks");
        AreaDamageChecks.Clear(spawns); Set(grid.InteractiveTiles.LavaConfig, 5, .5f, true, DamageType.Kinetic);
    }
    // =========================================================
    // Canonical Energy shield/hull math and one environmental kill without tower XP or duplicate Credits.
    private static void Defenses(Node main, SpawnManager spawns, WorldGrid grid)
    {
        AreaDamageChecks.Clear(spawns); Lava(grid, 10, 10); Set(grid.InteractiveTiles.LavaConfig, 5, .5f, true, DamageType.Energy);
        Enemy shield = AreaDamageChecks.Spawn(spawns, grid.CellToWorldCenter(new(10, 10)), true);
        Near(shield.CurrentShield, 90, "Energy2x shield entry"); Near(shield.Health, 60, "Shield protects hull");
        grid.InteractiveTiles._PhysicsProcess(.5); Near(shield.CurrentShield, 80, "Periodic shield hit");
        shield.TakeDamage(75); grid.InteractiveTiles._PhysicsProcess(.5);
        Near(shield.CurrentShield, 0, "Lava breaks shield through canonical resolver"); Near(shield.Health, 57.875f, "Only residual base units damage hull");
        Enemy hull = Spawn(spawns, grid, 10); Near(hull.Health, 55.75f, "Energy hull coefficient is preserved");
        AreaDamageChecks.Clear(spawns); Enemy victim = Spawn(spawns, grid, 9);
        EconomyManager wallet = main.GetNode<EconomyManager>("Economy"); int credits = wallet.GetBalance(wallet.CreditsResource);
        TowerDefinition definition = main.GetNode<TowerPlacementController>("TowerPlacement").Catalog.Find("battle");
        TowerBase witness = definition.Scene.Instantiate<TowerBase>(); witness.InitializeDefinition(definition);
        main.GetNode("World/Towers").AddChild(witness); witness.SetPhysicsProcess(false);
        double xp = main.GetNode("World/Towers").GetChildren().OfType<TowerBase>().Sum(t => t.RankProgress.TotalXp);
        Set(grid.InteractiveTiles.LavaConfig, 1000, .01f, true, DamageType.Energy);
        victim.GlobalPosition = grid.CellToWorldCenter(new(10, 10)); grid.InteractiveTiles._PhysicsProcess(10);
        Require(!victim.IsAlive && !spawns.Registry.Contains(victim) && grid.InteractiveTiles.ActiveCount == 0, "Death removes hazard/spatial membership");
        Require(wallet.GetBalance(wallet.CreditsResource) == credits + victim.Definition.KillRewardCredits, "Lava kill pays once");
        Require(main.GetNode("World/Towers").GetChildren().OfType<TowerBase>().Sum(t => t.RankProgress.TotalXp) == xp, "Environmental damage has no tower XP source"); witness.Free();
        Set(grid.InteractiveTiles.LavaConfig, 5, .5f, true, DamageType.Kinetic);
        Enemy removed = Spawn(spawns, grid, 10); removed.Free(); Require(grid.InteractiveTiles.ActiveCount == 0, "Free cleans exposure");
        Enemy unregistered = Spawn(spawns, grid, 10); spawns.Registry.Unregister(unregistered);
        Require(grid.InteractiveTiles.ActiveCount == 0, "Manual unregistration removes exposure synchronously"); unregistered.Free();
        Enemy arrived = Spawn(spawns, grid, 10); arrived.Arrive(); Require(grid.InteractiveTiles.ActiveCount == 0, "Arrival cleans exposure");
    }
    // =========================================================
    // Production movement must stop at the lethal encounter, before committing its planned endpoint.
    private static void ActualMovement(SpawnManager spawns, WorldGrid grid)
    {
        AreaDamageChecks.Clear(spawns); Lava(grid, 10, 12); Set(grid.InteractiveTiles.LavaConfig, 1000, .5f, true, DamageType.Kinetic);
        Enemy enemy = Spawn(spawns, grid, 9); Vector2 end = grid.CellToWorldCenter(new(14, 10));
        enemy.Movement.AcceptPath(new[] { enemy.GlobalPosition, end }, grid.NavigationRevision); enemy.Movement._PhysicsProcess(10);
        Require(!enemy.IsAlive && enemy.GlobalPosition.X < end.X && grid.InteractiveTiles.ActiveCount == 0, "Lethal swept entry stops actual movement");
        Set(grid.InteractiveTiles.LavaConfig, 5, .5f, true, DamageType.Kinetic);
    }
    // =========================================================
    // One late tick visits only the500 active occupants, with no nodes created per tile or hit.
    private static void Population(SpawnManager spawns, WorldGrid grid)
    {
        AreaDamageChecks.Clear(spawns); Lava(grid, 10, 10); Set(grid.InteractiveTiles.LavaConfig, .01f, .5f, false, DamageType.Kinetic);
        int limit = spawns.Paths.Config.MaxActiveEnemies; spawns.Paths.Config.MaxActiveEnemies = Math.Max(limit, 500);
        try
        {
            for (int i = 0; i < 500; i++) Spawn(spawns, grid, 10);
            Stopwatch watch = Stopwatch.StartNew(); grid.InteractiveTiles._PhysicsProcess(2); watch.Stop();
            Require(grid.InteractiveTiles.ActiveCount == 500 && spawns.Registry.Living.All(e => Math.Abs(e.Health - 59.96) < .001), "500 occupants receive four owed hits each");
            GD.Print($"LAVA BENCH:500 occupants /2000 canonical hits {watch.Elapsed.TotalMilliseconds:0.000}ms;one manager/no tile nodes (headless, not rendered FPS)");
        }
        finally { AreaDamageChecks.Clear(spawns); spawns.Paths.Config.MaxActiveEnemies = limit; Set(grid.InteractiveTiles.LavaConfig, 5, .5f, true, DamageType.Kinetic); }
    }
    // =========================================================
    // A lethal hazard at an authored spawn cannot strand a wave waiting on an already removed ship.
    private static void LethalSpawnWave(Node main, SpawnManager spawns, WorldGrid grid)
    {
        AreaDamageChecks.Clear(spawns);
        string portal = grid.Level.Settings.ActivePortals[0].ToString();
        Vector2I cell = grid.WorldToCell(grid.Level.GetNode<Node2D>(portal).GetNode<Marker2D>("SpawnExit").GlobalPosition);
        TileMapLayer layer = grid.Level.Interactive; int source = layer.GetCellSourceId(cell), alternative = layer.GetCellAlternativeTile(cell);
        Vector2I atlas = layer.GetCellAtlasCoords(cell); float offset = spawns.Paths.Config.PortalSpawnOffsetPixels;
        WaveManager waves = main.GetNode<WaveManager>("Waves");
        try
        {
            Set(grid.InteractiveTiles.LavaConfig, 1000, .5f, true, DamageType.Energy);
            spawns.Paths.Config.PortalSpawnOffsetPixels = 0;
            layer.SetCell(cell, 1, Vector2I.Zero); grid.RebuildTerrain();
            waves.Configure(new[] { WaveBuilder.Wave(1, WaveBuilder.Batch(1, 0, 0, WaveBuilder.Enemy("basic_mk1", portal: portal))) }, spawns.Catalog.ToSceneMap());
            Require(waves.StartWave() && waves.AliveCount == 0 && waves.State == WaveManager.WaveState.AllWavesComplete,
                "Spawn-time Lava death consumes planned ship and completes wave once");
        }
        finally
        {
            if (source < 0) layer.EraseCell(cell); else layer.SetCell(cell, source, atlas, alternative);
            spawns.Paths.Config.PortalSpawnOffsetPixels = offset;
            Set(grid.InteractiveTiles.LavaConfig, 5, .5f, true, DamageType.Kinetic); grid.RebuildTerrain();
            waves.Configure(WaveCatalog.GetWaves(grid.Level.Settings.WaveSetId), spawns.Catalog.ToSceneMap());
        }
    }

    // =========================================================
    // A populated independent world releases occupants/resource subscriptions without touching this world's index.
    private static void Unload(Node main)
    {
        foreach (bool duringHit in new[] { false, true })
        {
            Node2D world = GD.Load<PackedScene>("res://WORLD/World.tscn").Instantiate<Node2D>(); main.AddChild(world);
            WorldGrid grid = world.GetNode<WorldGrid>("Grid"); InteractiveTileManager manager = grid.InteractiveTiles;
            grid.Level.Impassable.EraseCell(new(10, 10)); grid.Level.Interactive.SetCell(new(10, 10), 1, Vector2I.Zero); grid.RebuildTerrain();
            SpawnManager spawns = world.GetNode<SpawnManager>("EnemySystems/Spawning"); Enemy enemy = Spawn(spawns, grid, 10);
            Require(manager.ActiveCount == 1, "Independent world owns its own occupant");
            if (duringHit)
            {
                LavaTileConfig config = manager.LavaConfig; config.DamagePerTick = 1000;
                spawns.Registry.EnemyKilled += _ => world.RemoveChild(manager);
                manager._PhysicsProcess(.5);
                Require(manager.ActiveCount == 0 && !manager.IsInsideTree(), "Removal during a Lava kill stops timer enumeration safely");
                config.DamagePerTick = 5; manager.Free();
            }
            world.Free(); Require(manager.ActiveCount == 0 && !enemy.IsAlive, "World unload clears hazard state and enemy membership");
        }
    }

    // =========================================================
    // Malformed timers/damage fail validation instead of looping forever or dropping ticks.
    private static void InvalidSettings()
    {
        foreach (LavaTileConfig config in new[] { new LavaTileConfig { DamageIntervalSeconds = 0 }, new() { DamageIntervalSeconds = float.NaN }, new() { DamagePerTick = -1 }, new() { DamageType = (DamageType)99 } })
        { bool rejected = false; try { config.Validate(); } catch (InvalidOperationException) { rejected = true; } Require(rejected, "Invalid Lava config rejected"); }
    }
    // =========================================================
    // Repaint the isolated fixture row through the same explicit rebuild route as real terrain edits.
    private static void Lava(WorldGrid grid, int from, int to)
    { for (int x = 6; x <= 30; x++) grid.Level.Interactive.EraseCell(new(x, 10)); for (int x = from; x <= to; x++) grid.Level.Interactive.SetCell(new(x, 10), 1, Vector2I.Zero); grid.RebuildTerrain(); }
    // =========================================================
    // Spawn through production services and use direct-position arrival semantics for this stationary fixture.
    private static Enemy Spawn(SpawnManager spawns, WorldGrid grid, int x) => AreaDamageChecks.Spawn(spawns, grid.CellToWorldCenter(new(x, 10)));
    // =========================================================
    // Manual segment and late manager update mirror production physics ordering.
    private static void Step(Enemy enemy, WorldGrid grid, int x, double seconds) => Travel(enemy, grid, grid.CellToWorldCenter(new(x, 10)), seconds);
    // =========================================================
    // Normal movement bypasses teleport entry logic but retains spatial transform notifications.
    private static void Travel(Enemy enemy, WorldGrid grid, Vector2 end, double seconds)
    { if (grid.InteractiveTiles.Traverse(enemy, enemy.GlobalPosition, end, seconds)) enemy.SetMovementPosition(end); grid.InteractiveTiles._PhysicsProcess(seconds); }
    // =========================================================
    // Inspector/resource updates use the production Changed subscription.
    private static void Set(LavaTileConfig config, float damage, float interval, bool entry, DamageType type)
    { config.DamagePerTick = damage; config.DamageIntervalSeconds = interval; config.DamageOnEntry = entry; config.DamageType = type; }
    // =========================================================
    // Only float arithmetic tolerance is accepted, never an extra/missing hit.
    private static void Near(float actual, float expected, string message) => AreaDamageChecks.Near(actual, expected, message);
    // =========================================================
    // Fail the full integration runner on any terrain behavior regression.
    private static void Require(bool condition, string message) => AreaDamageChecks.Require(condition, message);
}
