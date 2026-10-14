// Integration fixtures for wave timing, ownership, random sources and bounded retries.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static WaveBuilder;

public static class WaveChecks
{
    // =========================================================
    // Use production Main services and authored portals; synthetic mixes only test pure quota data.
    public static void Run(Node battle, WorldGrid grid, SpawnManager spawns)
    {
        WaveManager manager = battle.GetNode<WaveManager>("Waves");
        GameplayHUD hud = battle.GetNode<GameplayHUD>("UI/GameplayHUD");
        Button button = hud.GetNode<Button>("TopBar/Status/StartWave");
        Require(manager.State == WaveManager.WaveState.Ready && spawns.Registry.Living.Count == 0 && !button.Disabled,
            "Load prepares waves without spawning; manual start is enabled");
        var scenes = WaveCatalog.LoadScenes();
        WaveBatch mixed = Batch(20, 0.2, 0, Enemy("a", 80), Enemy("b", 20));
        RandomNumberGenerator rng = new() { Seed = 42 };
        var order = WaveCatalog.Compose(mixed, rng); rng.Seed = 42;
        Require(order.Count(e => e.EnemyId == "a") == 16 && order.Count(e => e.EnemyId == "b") == 4 &&
            order.SequenceEqual(WaveCatalog.Compose(mixed, rng)), "Exact quotas are 16/4 and shuffle is reproducible");
        var tied = WaveCatalog.Compose(Batch(5, 0, 0, Enemy("a", 1), Enemy("b", 1)), rng);
        Require(tied.Count(e => e.EnemyId == "a") == 3, "Largest remainder uses stable entry-order ties");

        manager.Configure(new[] { Wave(1, Batch(1, 0, 0, Enemy("missing"))) }, scenes);
        Require(manager.State == WaveManager.WaveState.PausedError && !manager.StartWave(), "Unknown IDs fail before spawning");
        manager.Configure(new[] { Wave(1, Batch(1, -1, 0, Enemy("enemy001"))) }, scenes);
        Require(manager.State == WaveManager.WaveState.PausedError, "Invalid timings reject setup");
        manager.Configure(new[] { Wave(1, Batch(1, 0, 0, Enemy("enemy001"))), Wave(1, Batch(1, 0, 0, Enemy("enemy001"))) }, scenes);
        Require(manager.State == WaveManager.WaveState.PausedError, "Duplicate wave numbers reject setup");

        manager.Configure(new[]
        {
            Wave(1, Batch(10, 0.1, 2, Enemy("enemy001")), Batch(5, 0.3, 5, Enemy("enemy001")),
                Batch(20, 0.2, 99, Enemy("enemy001"))),
            Wave(2, Batch(1, 0, 0, Enemy("enemy001")))
        }, scenes);
        double time = 0; List<double> spawnTimes = new(); int previous = 0, completions = 0;
        void Changed()
        {
            if (manager.AliveCount > previous) spawnTimes.Add(time);
            previous = manager.AliveCount;
            foreach (Enemy ship in spawns.Registry.Living) ship.Movement.SetPhysicsProcess(false);
        }
        void Completed(int wave) => completions++;
        manager.Changed += Changed; manager.WaveCompleted += Completed;
        hud.EmitSignal(GameplayHUD.SignalName.WaveStartRequested);
        Require(manager.AliveCount == 1 && !manager.StartWave() && button.Disabled, "First spawn is immediate; repeated start is rejected");
        for (int i = 0; manager.PlannedRemaining > 0 && i < 300; i++) { time += 0.1; manager._PhysicsProcess(0.1); }
        Require(spawnTimes.Count == 35 && Math.Abs(spawnTimes[9] - 0.9) < 0.01 &&
            Math.Abs(spawnTimes[10] - 2.9) < 0.01 && Math.Abs(spawnTimes[14] - 4.1) < 0.01 &&
            Math.Abs(spawnTimes[15] - 9.1) < 0.01, "Batch intervals and 2s/5s post-delays apply after successful final spawns");
        Require(manager.State == WaveManager.WaveState.WaitingForClear && manager.AliveCount == 35 &&
            manager.EnemiesRemaining == 35 && completions == 0, "Earlier batches overlap; final post-delay is ignored and living ships keep wave open");
        Enemy debug = spawns.Spawn(scenes["enemy001"], true); debug.Movement.SetPhysicsProcess(false);
        Enemy last = spawns.Registry.Living.First(s => s != debug);
        foreach (Enemy ship in spawns.Registry.Living.ToArray()) if (ship != debug && ship != last) ship.TakeDamage(10000);
        Require(manager.AliveCount == 1 && completions == 0, "Last owned ship keeps the wave open");
        last.Arrive(); last.Arrive(); last.TakeDamage(10000);
        Require(manager.State == WaveManager.WaveState.Completed && !button.Disabled && completions == 1 &&
            debug.IsAlive, "Arrival resolves once; F2 ships do not delay wave completion");
        Require(manager.StartWave() && !manager.StartWave(), "Next wave requires a new manual start");
        spawns.Registry.Living.First(s => s != debug).TakeDamage(10000);
        Require(manager.State == WaveManager.WaveState.AllWavesComplete && completions == 2 && !manager.StartWave() &&
            button.Disabled, "Finite wave set completes once without restarting");
        manager.Changed -= Changed; manager.WaveCompleted -= Completed; debug.TakeDamage(10000);

        manager.Configure(new[] { Wave(1, Batch(200, 0, 0, Enemy("enemy001"))) }, scenes);
        manager.StartWave();
        while (manager.PlannedRemaining > 0)
        {
            int before = manager.PlannedRemaining;
            manager._PhysicsProcess(100);
            Require(before - manager.PlannedRemaining <= spawns.Paths.Config.MaxWaveSpawnAttemptsPerFrame,
                "Lag/zero intervals cannot exceed the per-frame spawn limit");
            foreach (Enemy ship in spawns.Registry.Living) ship.Movement.SetPhysicsProcess(false);
        }
        Vector2[] sources = grid.Level.Settings.ActivePortals.Select(path =>
            grid.Level.GetNode<Node2D>(path).GetNode<Marker2D>("SpawnExit").GlobalPosition).ToArray();
        Enemy[] ships = spawns.Registry.Living.ToArray();
        int fromFirst = ships.Count(s => s.GlobalPosition.IsEqualApprox(sources[0]));
        Require(sources.Length == 2 && fromFirst >= 65 && fromFirst <= 135 &&
            ships.All(s => sources.Any(p => s.GlobalPosition.IsEqualApprox(p))), "Random eligible portal traffic is approximately balanced");
        Require(ships.Zip(ships.Skip(1)).Any(pair => pair.First.GlobalPosition.IsEqualApprox(pair.Second.GlobalPosition)),
            "Wave sources are random rather than forced alternation");
        foreach (Enemy ship in ships) ship.TakeDamage(10000);
        Enemy d1 = spawns.Spawn(scenes["enemy001"], true), d2 = spawns.Spawn(scenes["enemy001"], true);
        Require(d1 != null && d2 != null && !d1.GlobalPosition.IsEqualApprox(d2.GlobalPosition), "F2 round-robin stays unchanged");
        d1.TakeDamage(10000); d2.TakeDamage(10000);

        manager.Configure(new[] { Wave(1, Batch(1, 0, 0, Enemy("enemy001", portal: "Portals/NotRegistered"))) }, scenes);
        manager.StartWave();
        Require(manager.State == WaveManager.WaveState.SpawnBlocked && manager.PlannedRemaining == 1 &&
            manager.AliveCount == 0, "Unavailable required portal blocks without fallback or dropping a ship");
        manager.Configure(new[] { Wave(1, Batch(1, 0, 0, Enemy("enemy001", portal: "Portals/EnemyPortal2"))) }, scenes);
        manager.StartWave();
        Require(manager.AliveCount == 1 && spawns.Registry.Living[0].GlobalPosition.IsEqualApprox(sources[1]),
            "Named permitted portal uses only its own exit");
        Enemy cancelled = spawns.Registry.Living[0];
        manager.Configure(Array.Empty<WaveDefinition>(), scenes);
        Require(manager.State == WaveManager.WaveState.Disabled && manager.AliveCount == 0 && !button.Visible,
            "Reset releases wave ownership and disables an empty wave set");
        cancelled.TakeDamage(10000);
        Require(manager.State == WaveManager.WaveState.Disabled, "Old ship resolution cannot revive a reset scheduler");

        Vector2I blockedCell = grid.WorldToCell(sources[1]);
        int oldSource = grid.Level.Impassable.GetCellSourceId(blockedCell);
        Vector2I oldAtlas = grid.Level.Impassable.GetCellAtlasCoords(blockedCell);
        int oldAlternative = grid.Level.Impassable.GetCellAlternativeTile(blockedCell);
        grid.Level.Impassable.SetCell(blockedCell, 0, new(0, 0)); grid.RebuildTerrain();
        for (int i = 0; i < 20; i++)
        {
            Enemy safe = spawns.SpawnWave(scenes["enemy001"], rng);
            Require(safe != null && safe.GlobalPosition.IsEqualApprox(sources[0]),
                "Random portal selection excludes a blocked exit");
            safe.TakeDamage(10000);
        }
        Require(spawns.SpawnWave(scenes["enemy001"], rng, "Portals/EnemyPortal2") == null,
            "Blocked named portal never falls back to a different source");
        if (oldSource < 0) grid.Level.Impassable.EraseCell(blockedCell);
        else grid.Level.Impassable.SetCell(blockedCell, oldSource, oldAtlas, oldAlternative);
        grid.RebuildTerrain();

        int max = spawns.Paths.Config.MaxActiveEnemies;
        spawns.Paths.Config.MaxActiveEnemies = 0;
        manager.Configure(new[] { Wave(1, Batch(2, 0, 0, Enemy("enemy001"))) }, scenes);
        manager.StartWave();
        Require(manager.State == WaveManager.WaveState.SpawnBlocked && manager.PlannedRemaining == 2 && manager.EnemiesRemaining == 2,
            "Enemy cap never loses scheduled ships or completes a blocked wave");
        spawns.Paths.Config.MaxActiveEnemies = max;
        manager._PhysicsProcess(0.1);
        Require(manager.AliveCount == 0, "Failed attempts wait for retry backoff");
        manager._PhysicsProcess(0.5);
        Require(manager.PlannedRemaining == 0 && manager.AliveCount == 2, "Retry resumes the retained batch entry");
        foreach (Enemy ship in spawns.Registry.Living.ToArray()) ship.TakeDamage(10000);

        LevelConfig.SpawnSourceMode mode = grid.Level.Settings.SpawnMode;
        LevelConfig.MapEdge edges = grid.Level.Settings.AllowedEdges;
        grid.Level.Settings.SpawnMode = LevelConfig.SpawnSourceMode.MapEdges;
        grid.Level.Settings.AllowedEdges = LevelConfig.MapEdge.Left; grid.RebuildTerrain();
        manager.Configure(new[] { Wave(1, Batch(1, 0, 0, Enemy("enemy001"))) }, scenes); manager.StartWave();
        Require(manager.AliveCount == 1 && grid.WorldToCell(spawns.Registry.Living[0].GlobalPosition).X == 0,
            "Wave spawning respects map-edge-only levels");
        spawns.Registry.Living[0].TakeDamage(10000);
        grid.Level.Settings.SpawnMode = mode; grid.Level.Settings.AllowedEdges = edges; grid.RebuildTerrain();
        while (spawns.Paths.PendingCount > 0) spawns.Paths._PhysicsProcess(1.0 / 60);
        manager.Configure(WaveCatalog.GetWaves(grid.Level.Settings.WaveSetId), scenes);
        manager.SetPhysicsProcess(false);
        GD.Print("PASS: wave timing, manual progression, exact quotas, owned completion, random portals, map edges and bounded retry checks");
    }

    // =========================================================
    // Stop the host integration run on any scheduler or source-selection regression.
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
