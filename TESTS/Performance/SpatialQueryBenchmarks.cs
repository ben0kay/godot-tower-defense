// Repeatable same-machine combat-query benchmark; original scans are retained as test-only baselines.
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class SpatialQueryBenchmarks : Node
{
    private readonly record struct Sample(double Total, double Maintain, double Target, double Blast, double Wave, long Bytes, long Candidates, int ActiveWaves);
    // =========================================================
    // Stop production ticks/render feedback and run paired fixtures, without saved profile or balance changes.
    public override async void _Ready()
    {
        try
        {
            Node main = GD.Load<PackedScene>("res://Main.tscn").Instantiate(); AddChild(main);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Stop(main);
            SpawnManager spawns = main.GetNode<SpawnManager>("World/EnemySystems/Spawning");
            spawns.Paths.Config.MaxActiveEnemies = 1000;
            GD.Load<FeedbackMasterConfig>("res://FEEDBACK/DefaultFeedbackConfig.tres").DeathExplosionsEnabled = false;
            GD.Print("SPATIAL BENCH: paired old/new;128px cells;120 measured ticks after60 warmup;all towers acquire each tick (burst workload);8 impacts/tick;8 independent fronts;movement included;zero-amount damage isolates delivery;headless NOT rendered FPS");
            foreach (bool dense in new[] { false, true }) foreach (int count in new[] { 500, 1000 }) foreach (int towers in new[] { 50, 100, 200 })
            {
                Sample[] before = Measure(main, spawns, count, towers, dense, false);
                Sample[] after = Measure(main, spawns, count, towers, dense, true);
                Print(count, towers, dense, "scan", before); Print(count, towers, dense, "index", after);
                GD.Print($"SPATIAL SPEEDUP {count}/{towers}/{(dense ? "dense" : "sparse")}: {before.Average(s => s.Total) / after.Average(s => s.Total):0.00}x net including maintenance");
            }
            foreach (int count in new[] { 500, 1000 }) { Overflow(spawns, count, false); Overflow(spawns, count, true); }
            AreaDamageChecks.Clear(spawns); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    // =========================================================
    // Same positions, paths, movement, centers, damage inputs and wave lifetime for each side.
    private Sample[] Measure(Node main, SpawnManager spawns, int count, int towers, bool dense, bool indexed)
    {
        AreaDamageChecks.Clear(spawns); Random random = new(900);
        Enemy[] enemies = new Enemy[count]; Vector2 origin = new(2000, 2000);
        for (int i = 0; i < count; i++)
        {
            int span = dense ? 64 : 2048;
            enemies[i] = AreaDamageChecks.Spawn(spawns, origin + new Vector2(random.Next(-span, span), random.Next(-span, span)));
            enemies[i].SetNotifyLocalTransform(indexed); enemies[i].SetNotifyTransform(indexed);
            enemies[i].Movement.AcceptPath(new[] { enemies[i].GlobalPosition, enemies[i].GlobalPosition + new Vector2(0, 500) }, spawns.Paths.Navigation.Grid.NavigationRevision);
        }
        Vector2[] centers = Enumerable.Range(0, towers).Select(_ => origin + (dense ? Vector2.Zero : new Vector2(random.Next(-1800, 1800), random.Next(-1800, 1800)))).ToArray();
        List<Node> waves = new(); Sample[] samples = new Sample[120];
        for (int frame = 0; frame < 180; frame++)
        {
            long allocations = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
            if (frame % 50 == 0)
            {
                foreach (Node wave in waves) wave.Free(); waves.Clear();
                for (int i = 0; i < 8; i++)
                {
                    Node wave;
                    if (indexed) { var w = new ShockwaveEmitter(); w.Initialize(spawns.Registry, centers[i], new(0, DamageType.Energy), 200, 250, .25f); wave = w; }
                    else { var w = new BruteForceShockwave(); w.Initialize(spawns.Registry, centers[i], new(0, DamageType.Energy), 200, 250, .25f); wave = w; }
                    main.GetNode("World/Projectiles").AddChild(wave); wave.SetPhysicsProcess(false); waves.Add(wave);
                }
            }
            foreach (Enemy e in enemies) e.GlobalPosition += new Vector2(.75f, .25f);
            long moved = Stopwatch.GetTimestamp(), candidates = 0;
            for (int i = 0; i < towers; i++)
            {
                var priority = i % 2 == 0 ? TowerTargeting.Priority.Closest : TowerTargeting.Priority.First;
                if (indexed) { TowerTargeting.Find(spawns.Registry, centers[i], 256, priority); candidates += spawns.Registry.LastQueryCandidates; }
                else { SpatialQueryChecks.BruteTarget(spawns.Registry, centers[i], 256, priority); candidates += count; }
            }
            long targeted = Stopwatch.GetTimestamp();
            for (int i = 0; i < 8; i++)
            {
                if (indexed) { AreaDamage.ApplyInstant(centers[i], 120, new(0, DamageType.Explosive), .25f, spawns.Registry); candidates += spawns.Registry.LastQueryCandidates; }
                else { BruteBlast(centers[i], spawns.Registry); candidates += count; }
            }
            long blasted = Stopwatch.GetTimestamp();
            int activeWaves = 0;
            foreach (Node wave in waves)
                if (indexed)
                {
                    bool active = !((ShockwaveEmitter)wave).IsComplete;
                    ((ShockwaveEmitter)wave)._PhysicsProcess(1.0 / 60);
                    if (active) { candidates += spawns.Registry.LastQueryCandidates; activeWaves++; }
                }
                else
                {
                    bool active = !((BruteForceShockwave)wave).IsComplete;
                    ((BruteForceShockwave)wave)._PhysicsProcess(1.0 / 60);
                    if (active) { candidates += count; activeWaves++; }
                }
            long end = Stopwatch.GetTimestamp();
            if (frame >= 60) samples[frame - 60] = new(Ms(end - start), Ms(moved - start), Ms(targeted - moved), Ms(blasted - targeted), Ms(end - blasted), GC.GetAllocatedBytesForCurrentThread() - allocations, candidates, activeWaves);
        }
        foreach (Node wave in waves) wave.Free();
        return samples;
    }

    // =========================================================
    // Deliberately pathological motion forces every mover into the conservative overflow set.
    // This isolates the correctness/maintenance trade-off rather than disguising it as a normal speed.
    private static void Overflow(SpawnManager spawns, int count, bool indexed)
    {
        AreaDamageChecks.Clear(spawns); Enemy[] enemies = new Enemy[count]; Vector2 origin = new(2000, 2000);
        for (int i = 0; i < count; i++)
        {
            enemies[i] = AreaDamageChecks.Spawn(spawns, origin + new Vector2(1000000, i % 20));
            enemies[i].SetNotifyLocalTransform(indexed); enemies[i].SetNotifyTransform(indexed);
        }
        ulong[] cursors = indexed ? Enumerable.Range(0, 8).Select(_ => spawns.Registry.BeginWaveQuery()).ToArray() : Array.Empty<ulong>();
        List<EnemySpatialIndex.Sweep> results = new(); double[] times = new double[80]; long candidates = 0, hits = 0;
        for (int tick = 0; tick < 100; tick++)
        {
            long start = Stopwatch.GetTimestamp(); float previous = tick % 2 == 0 ? 1000000 : -1000000;
            for (int i = 0; i < count; i++) enemies[i].GlobalPosition = origin + new Vector2(-previous, i % 20);
            for (int wave = 0; wave < 8; wave++)
                if (indexed)
                {
                    spawns.Registry.GetWaveCandidates(cursors[wave], origin, 50, 100, results);
                    if (tick >= 20) candidates += spawns.Registry.LastQueryCandidates;
                    foreach (var result in results)
                        if (ShockwaveEmitter.Crossing(result.Start - origin, result.End - origin, 50, 100, out _)) hits++;
                }
                else
                {
                    if (tick >= 20) candidates += count;
                    for (int i = 0; i < count; i++)
                        if (BruteForceShockwave.Crossing(new(previous, i % 20), enemies[i].GlobalPosition - origin, 50, 100, out _)) hits++;
                }
            if (tick >= 20) times[tick - 20] = Ms(Stopwatch.GetTimestamp() - start);
        }
        foreach (ulong cursor in cursors) spawns.Registry.EndWaveQuery(cursor);
        AreaDamageChecks.Require(hits == (long)count * 8 * 100, "Overflow preserves every swept crossing");
        Array.Sort(times);
        GD.Print($"SPATIAL OVERFLOW {count}/{(indexed ? "index" : "scan")}: avg={times.Average():0.000}ms p95={times[76]:0.000}ms max={times[^1]:0.000}ms candidates/tick={candidates / 80} activeWaves=8; all enemies teleport2,000,000px/tick; movement+solver included");
    }

    // =========================================================
    // Exact original instant-AoE scan/snapshot, including validity checks and double-distance falloff.
    private static void BruteBlast(Vector2 impact, EnemyRegistry registry)
    {
        const float radius = 120; List<(Enemy Enemy, DamageInfo Hit)> recipients = new();
        foreach (Enemy enemy in registry.Living)
        {
            if (!Eligible(enemy, registry)) continue;
            double squared = EnemySpatialIndex.DistanceSquared(enemy.GlobalPosition, impact);
            if (!double.IsFinite(squared) || squared > radius * radius) continue;
            recipients.Add((enemy, AreaDamage.AtDistance(new(0, DamageType.Explosive), (float)Math.Sqrt(squared), radius, .25f)));
        }
        foreach (var recipient in recipients) if (Eligible(recipient.Enemy, registry)) recipient.Enemy.ApplyDamage(recipient.Hit);
    }
    // =========================================================
    // Same native/liveness checks used by the pre-pass instant blast.
    private static bool Eligible(Enemy enemy, EnemyRegistry registry) => GodotObject.IsInstanceValid(registry)
        && GodotObject.IsInstanceValid(enemy) && enemy.IsAlive && enemy.IsInsideTree() && !enemy.IsQueuedForDeletion() && registry.Contains(enemy);
    // =========================================================
    // Print separate maintenance/query/delivery timing, total distribution, candidate work and managed allocation.
    private static void Print(int count, int towers, bool dense, string mode, Sample[] samples)
    {
        double[] sorted = samples.Select(s => s.Total).OrderBy(x => x).ToArray();
        GD.Print($"SPATIAL RESULT {count}/{towers}/{(dense ? "dense" : "sparse")}/{mode}: avg={samples.Average(s => s.Total):0.000}ms p95={sorted[(int)(sorted.Length * .95)]:0.000}ms max={sorted[^1]:0.000}ms maintain={samples.Average(s => s.Maintain):0.000}ms target={samples.Average(s => s.Target):0.000}ms blast={samples.Average(s => s.Blast):0.000}ms wave={samples.Average(s => s.Wave):0.000}ms candidates={samples.Average(s => s.Candidates):0} bytes/tick={samples.Average(s => s.Bytes):0} activeWaves={samples.Average(s => s.ActiveWaves):0.00}");
    }
    // =========================================================
    // Avoid production physics/navigation/beam work contaminating these isolated combat-query batches.
    private static void Stop(Node node) { node.SetProcess(false); node.SetPhysicsProcess(false); foreach (Node child in node.GetChildren()) Stop(child); }
    // =========================================================
    // High-resolution stopwatch conversion without allocating Stopwatch objects inside the measured loop.
    private static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
}
