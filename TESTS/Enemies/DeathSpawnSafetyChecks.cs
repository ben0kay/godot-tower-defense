// Kill-only gates, bounded placement/caps, independent jobs/RNG, recursive children and cancellation.
using Godot;
using System;
using System.Linq;
using System.Reflection;
using static MissionFixture;

internal static class DeathSpawnSafetyChecks
{
    // =========================================================
    // Each small production fixture isolates world services and tuning without changing saved resources.
    internal static void Run(Node owner)
    {
        using (DeathSpawnFixture f = new(owner))
        {
            Enemy arrived = f.Parent(); arrived.Arrive(); Require(f.Queue.PendingJobs == 0, "Arrival does not trigger");
            Enemy removed = f.Parent(); removed.Free(); Require(f.Queue.PendingJobs == 0, "External free does not trigger");
            Enemy shield = f.Parent(); shield.Definition = (EnemyDefinition)shield.Definition.Duplicate(true); shield.Definition.MaxShield = 100;
            shield.RestoreShield(100); shield.TakeDamage(10); Require(f.Queue.PendingJobs == 0, "Shield-only damage does not trigger");
            var random = (RandomNumberGenerator)typeof(WaveManager).GetField("_random", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f.Waves);
            ulong waveState = random.State;
            f.Config(shield).SpawnIntervalSeconds = 0; shield.TakeDamage(99999);
            Enemy second = f.Parent(); f.Config(second).SpawnIntervalSeconds = 0; second.TakeDamage(99999);
            int before = f.Queue.SuccessfulChildren;
            f.Tick(100); Require(f.Queue.LastFrameAttempts <= 2 && f.Queue.SuccessfulChildren - before == 2, "Shared zero-interval/same-frame kill budget");
            f.FinishReleases(); Require(f.Queue.SuccessfulChildren == 12 && random.State == waveState && f.Waves.AliveCount == 0 && f.Waves.PendingDeathSpawns == 0,
                "Debug children unowned; scatter independent of wave RNG");
        }
        using (DeathSpawnFixture f = new(owner))
        {
            Enemy recursive = f.Parent(); f.Config(recursive).ChildEnemyId = "transporter_mk1"; f.Config(recursive).ChildCount = 2;
            recursive.TakeDamage(99999); f.FinishReleases();
            foreach (Enemy child in f.Spawns.Registry.Living.ToArray()) child.TakeDamage(99999);
            Require(f.Queue.PendingJobs == 0 && f.Queue.SuccessfulChildren == 2, "Generation-one carrier children cannot create grandchildren");
        }
        using (DeathSpawnFixture f = new(owner))
        {
            Enemy carrier = f.Parent(true); f.Config(carrier).ChildEnemyId = "scout_mk1"; f.Config(carrier).SpawnWithShield = true;
            carrier.TakeDamage(99999); f.Tick(.01);
            Enemy child = f.Spawns.Registry.Living.Single(); Require(child.CurrentShield == child.Definition.MaxShield && child.CurrentShield > 0, "Child own capacity/shield policy");
            f.Waves.Configure(Array.Empty<WaveDefinition>(), f.Spawns.Catalog.ToSceneMap());
            Require(f.Queue.PendingJobs == 0 && f.Waves.PendingDeathSpawns == 0 && f.Waves.AliveCount == 0, "Reconfigure retires owned jobs/reservations immediately");
        }
        using (DeathSpawnFixture f = new(owner))
        {
            Enemy carrier = f.Parent(true); carrier.TakeDamage(99999);
            f.Spawns.Paths.Config.MaxActiveEnemies = 1; Enemy blocker = f.Parent(false, "basic_mk1");
            f.Tick(.01); Require(f.Queue.SuccessfulChildren == 0 && f.Waves.PendingDeathSpawns == 5, "Cap skips child and retires reservation");
            blocker.Free(); f.Tick(.12); Require(f.Queue.SuccessfulChildren == 1, "Later release can use a newly freed active slot");
            f.FinishReleases(); Require(f.Queue.SkippedChildren == 5 && f.Queue.SuccessfulChildren == 1 && f.Waves.AliveCount == 1, "Cap cannot freeze release completion");
        }
        using (DeathSpawnFixture f = new(owner))
        {
            Enemy carrier = f.Parent(true); carrier.TakeDamage(99999);
            bool reset = false;
            f.Waves.Changed += () =>
            {
                if (!reset && f.Waves.PendingDeathSpawns == 5)
                { reset = true; f.Waves.Configure(Array.Empty<WaveDefinition>(), f.Spawns.Catalog.ToSceneMap()); }
            };
            f.Tick(.01);
            Require(reset && f.Queue.PendingChildren == 0 && f.Queue.PendingJobs == 0 && f.Waves.PendingDeathSpawns == 0,
                "Reconfiguration inside a reservation event cannot corrupt the advancing queue");
        }
        BlockedAndNarrow(owner); Cancellation(owner);
        GD.Print("PASS: DeathSpawn kill gates, rewards-safe creation, overlapping budget, shield policy, recursion limit, cap/blocked skips and wave/unload cancellation");
    }
    // =========================================================
    // No clearance fallback may teleport to portals or force children into new towers/walls/outside bounds.
    private static void BlockedAndNarrow(Node owner)
    {
        using DeathSpawnFixture f = new(owner);
        Enemy parent = f.Parent(true); parent.TakeDamage(99999);
        for (int y = 17; y <= 23; y++) for (int x = 17; x <= 23; x++)
        { f.Grid.Level.Passable.EraseCell(new(x, y)); f.Grid.Level.Impassable.SetCell(new(x, y), 0, Vector2I.Zero); }
        f.Grid.RebuildTerrain(); f.FinishReleases();
        Require(f.Queue.SuccessfulChildren == 0 && f.Queue.SkippedChildren == 6 && f.Waves.PendingDeathSpawns == 0
            && f.Waves.State == WaveManager.WaveState.AllWavesComplete, "All rejected positions clear reservations without an infinite retry");
    }
    // =========================================================
    // World jobs do not retain parent nodes; unloading and wave reset cannot leak work into another mission.
    private static void Cancellation(Node owner)
    {
        using (DeathSpawnFixture f = new(owner))
        {
            Enemy invalid = f.Parent(); f.Config(invalid).ChildEnemyId = "unknown-child"; invalid.TakeDamage(99999);
            Require(f.Queue.PendingJobs == 0, "Unknown ID is explicit, with no fallback");
            Enemy limit = f.Parent(); f.Config(limit).ChildCount = 65; limit.TakeDamage(99999); Require(f.Queue.PendingJobs == 0, "Invalid count rejected");
        }
        DeathSpawnFixture unloading = new(owner); Enemy parent = unloading.Parent(true); parent.TakeDamage(99999);
        DeathReleaseQueue queue = unloading.Queue; unloading.Dispose(); Require(!GodotObject.IsInstanceValid(queue), "World unload deletes scheduler/jobs");
        using DeathSpawnFixture next = new(owner); Require(next.Queue.PendingJobs == 0 && next.Spawns.Registry.Living.Count == 0, "No release leaks into next level");
    }
}
