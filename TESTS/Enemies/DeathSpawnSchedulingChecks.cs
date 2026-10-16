// Physics-rate timing, automatic release clock, entry-hazard ownership, XP attribution and 500-active stress.
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using static MissionFixture;
using static WaveBuilder;

internal static class DeathSpawnSchedulingChecks
{
    // =========================================================
    // Manual clocks verify quantization; one real automatic run verifies the production physics callback wiring.
    internal static async Task Run(Node owner)
    {
        foreach (int hz in new[] { 30, 60, 120 })
        {
            using DeathSpawnFixture f = new(owner); Enemy parent = f.Parent(); parent.TakeDamage(99999);
            List<double> times = new(); double time = 0; int previous = 0;
            while (f.Queue.PendingJobs > 0 && time < 2)
            {
                time += 1.0 / hz; f.Tick(1.0 / hz);
                if (f.Queue.SuccessfulChildren != previous) { times.Add(time); previous = f.Queue.SuccessfulChildren; }
            }
            Require(times.Count == 6 && times.Zip(times.Skip(1), (a, b) => b - a).All(gap => gap >= .11999 && gap <= .12 + 1.0 / hz + .00001), "Release interval at " + hz + " Hz");
        }
        using (DeathSpawnFixture f = new(owner))
        {
            Enemy parent = f.Parent(); f.Config(parent).ChildCount = 2;
            f.Queue.ProcessMode = Node.ProcessModeEnum.Inherit; parent.TakeDamage(99999);
            await owner.ToSignal(owner.GetTree().CreateTimer(.35), SceneTreeTimer.SignalName.Timeout);
            Require(f.Queue.SuccessfulChildren == 2 && f.Queue.PendingJobs == 0, "Real automatic physics releases without manual ticks");
        }
        Attribution(owner); EntryHazard(owner); UnownedDuringWave(owner); Stress(owner);
        GD.Print("PASS: release clocks30/60/120Hz and automatic physics, actual tower XP, entry-hazard pre-removal ownership, debug isolation and500-active bounded stress");
    }
    // =========================================================
    // Releasing children grants no tower XP; each actual attributed child hit/kill uses the normal finite budget.
    private static void Attribution(Node owner)
    {
        using DeathSpawnFixture f = new(owner);
        TowerDefinition definition = f.Main.GetNode<TowerPlacementController>("TowerPlacement").Catalog.Find("battle");
        BattleTower tower = definition.Scene.Instantiate<BattleTower>(); tower.InitializeDefinition(definition); f.Main.GetNode("World/Towers").AddChild(tower); AmmoLoopFixture.Disable(tower);
        Enemy parent = f.Parent(); parent.ApplyDamage(new DamageInfo(99999, DamageType.Kinetic, 1, 1, source: tower.DamageSource));
        double xp = tower.RankProgress.TotalXp; Require(xp > 0, "Actual parent damage/kill credits tower XP");
        f.FinishReleases(); Require(tower.RankProgress.TotalXp == xp, "No XP for releasing enemies");
        f.Spawns.Registry.Living[0].ApplyDamage(new DamageInfo(99999, DamageType.Kinetic, 1, 1, source: tower.DamageSource));
        Require(Math.Abs(tower.RankProgress.TotalXp - xp - 36) < .001, "Child own3-Credit reward produces normal6 damageXP +30 killXP");
    }
    // =========================================================
    // A carrier killed during Initialize must already be owned and reserve its pending children before removal.
    private static void EntryHazard(Node owner)
    {
        using DeathSpawnFixture f = new(owner); LavaTileConfig config = f.Grid.InteractiveTiles.LavaConfig;
        var saved = (config.DamagePerTick, config.DamageOnEntry); float offset = f.Spawns.Paths.Config.PortalSpawnOffsetPixels;
        try
        {
            config.DamagePerTick = 10000; config.DamageOnEntry = true; f.Spawns.Paths.Config.PortalSpawnOffsetPixels = 0;
            string portal = f.Grid.Level.Settings.ActivePortals[0].ToString();
            Vector2I cell = f.Grid.WorldToCell(f.Grid.Level.GetNode<Node2D>(portal).GetNode<Marker2D>("SpawnExit").GlobalPosition);
            f.Grid.Level.Interactive.SetCell(cell, 1, Vector2I.Zero); f.Grid.RebuildTerrain();
            f.Waves.Configure(new[] { Wave(1, Batch(1, 0, 0, Enemy("transporter_mk1", portal: portal))) }, f.Spawns.Catalog.ToSceneMap());
            Require(f.Waves.StartWave() && f.Waves.PendingDeathSpawns == 6 && f.Waves.AliveCount == 0
                && f.Waves.State == WaveManager.WaveState.WaitingForClear, "Spawn-entry carrier death cannot drop reservations or complete the wave");
            f.FinishReleases(); Require(f.Waves.PendingDeathSpawns == 0, "Spawn-entry offspring deaths/skips retire reservations without deadlock");
        }
        finally { config.DamagePerTick = saved.Item1; config.DamageOnEntry = saved.Item2; f.Spawns.Paths.Config.PortalSpawnOffsetPixels = offset; }
    }
    // =========================================================
    // Debug children never join or prolong an unrelated authored wave.
    private static void UnownedDuringWave(Node owner)
    {
        using DeathSpawnFixture f = new(owner); Enemy owned = f.Parent(true, "basic_mk1"); Enemy debug = f.Parent(); debug.TakeDamage(99999);
        Require(f.Waves.PendingDeathSpawns == 0 && f.Waves.AliveCount == 1, "Debug job has no wave reservation");
        f.FinishReleases(); owned.TakeDamage(99999);
        Require(f.Waves.State == WaveManager.WaveState.AllWavesComplete && f.Spawns.Registry.Living.Count == 6, "Unowned offspring cannot hold a wave open");
    }
    // =========================================================
    // Measure queue-only creation time, keeping existing500 enemies stationary; A* remains separately budgeted.
    private static void Stress(Node owner)
    {
        using DeathSpawnFixture f = new(owner);
        for (int i = 0; i < 500; i++) f.Parent(false, "basic_mk1");
        for (int i = 0; i < 25; i++) f.Parent().TakeDamage(99999);
        var rng = (RandomNumberGenerator)typeof(SpawnManager).GetField("_spawnRandom", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f.Spawns);
        ulong state = rng.State; int frames = 0;
        while (f.Queue.PendingJobs > 0 && frames++ < 300)
        { f.Tick(1.0 / 60); Require(f.Queue.LastFrameAttempts <= 2, "Global budget under simultaneous carrier deaths"); }
        Require(f.Queue.SuccessfulChildren == 150 && f.Queue.SkippedChildren == 0 && rng.State == state, "Independent scatter spawns150 children near500 existing enemies without portal RNG consumption");
        f.Spawns.Paths._PhysicsProcess(1.0 / 60);
        Require(f.Spawns.Paths.LastFrameCalculations <= f.Spawns.Paths.Config.MaxPathCalculationsPerFrame, "Child paths retain A* count budget");
        GD.Print($"DEATH RELEASE BENCH:500 existing,25 simultaneous carriers,150 children; max2 attempts/frame; {frames} ticks; peak {f.Queue.PeakReleaseMilliseconds:0.000}ms queue CPU (not rendered FPS)");
    }
}
