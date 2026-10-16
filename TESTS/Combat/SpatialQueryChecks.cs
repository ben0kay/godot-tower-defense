// Spatial circle/target/motion parity against the pre-pass scans; deterministic adversarial fixtures.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static class SpatialQueryChecks
{
    // =========================================================
    // Cover normal and extreme queries, stable priorities, transform updates and independent moving fronts.
    public static void Run(Node main, SpawnManager spawns)
    {
        var config = spawns.Paths.Config; int limit = config.MaxActiveEnemies; config.MaxActiveEnemies = Math.Max(1000, limit);
        try
        {
            foreach (int count in new[] { 0, 1, 500, 1000 }) Circles(spawns, count);
            Targets(spawns); BlastParity(main, spawns);
            foreach (int rate in new[] { 30, 60, 120 }) WaveParity(main, spawns, rate);
            WorldIsolation(main, spawns);
        }
        finally { AreaDamageChecks.Clear(spawns); config.MaxActiveEnemies = limit; }
        GD.Print("PASS: spatial circles0/1/500/1000, negative/edge/huge/invalid queries, movement/cleanup, stable First/Closest, isolated worlds and swept oracle30/60/120Hz");
    }

    // =========================================================
    // Exact double-distance circle oracle includes every center at its inclusive boundary.
    private static void Circles(SpawnManager spawns, int count)
    {
        AreaDamageChecks.Clear(spawns); Random random = new(431);
        for (int i = 0; i < count; i++) AreaDamageChecks.Spawn(spawns,
            i < 5 ? new Vector2(-128 + i * 64, 0) : new Vector2(random.Next(-4096, 4096), random.Next(-4096, 4096)));
        List<Enemy> result = new();
        foreach (Vector2 center in new[] { Vector2.Zero, new Vector2(-128, 0), new Vector2(4000, -4000), new Vector2(float.MaxValue, 0) })
            foreach (float radius in new[] { 0f, 1, 64, 128, 256, 800, 12000, float.MaxValue })
            {
                spawns.Registry.GetEnemiesInRadius(center, radius, result);
                Enemy[] expected = spawns.Registry.Living.Where(e => EnemySpatialIndex.DistanceSquared(e.GlobalPosition, center) <= (double)radius * radius).ToArray();
                Require(result.Count == expected.Length && result.Distinct().Count() == result.Count && result.All(expected.Contains), $"Circle equals brute-force oracle count={count} center={center} radius={radius} actual={result.Count} expected={expected.Length}");
            }
        Invalid(() => spawns.Registry.GetEnemiesInRadius(Vector2.Zero, -1, result));
        Invalid(() => spawns.Registry.GetEnemiesInRadius(Vector2.Zero, float.NaN, result));
        Invalid(() => spawns.Registry.GetEnemiesInRadius(Vector2.Zero, float.PositiveInfinity, result));
        Invalid(() => spawns.Registry.GetEnemiesInRadius(new(float.NaN, 0), 1, result));
        if (count == 0) return;
        Enemy moving = spawns.Registry.Living[0]; spawns.Registry.Register(moving);
        moving.GlobalPosition = new(-1024, -1024);
        spawns.Registry.GetEnemiesInRadius(moving.GlobalPosition, 0, result);
        Require(result.Contains(moving) && result.Count(e => e == moving) == 1, "Teleport and duplicate registration update immediately");
        moving.GlobalPosition = new(float.NaN, 0);
        spawns.Registry.GetEnemiesInRadius(Vector2.Zero, 12000, result); Require(!result.Contains(moving), "Nonfinite positions are unqueryable");
        moving.GlobalPosition = Vector2.Zero;
        spawns.Registry.GetEnemiesInRadius(Vector2.Zero, 0, result); Require(result.Contains(moving), "Finite reposition restores membership");
        moving.Free(); spawns.Registry.GetEnemiesInRadius(Vector2.Zero, 12000, result);
        Require(result.Count == count - 1, "External free removes spatial membership immediately");
    }

    // =========================================================
    // Preserve strict-score behavior, infinite-route ineligibility and earlier-registration ties.
    private static void Targets(SpawnManager spawns)
    {
        AreaDamageChecks.Clear(spawns);
        Enemy first = AreaDamageChecks.Spawn(spawns, new(640, 640)), second = AreaDamageChecks.Spawn(spawns, new(640, 640));
        foreach (Enemy e in spawns.Registry.Living)
            e.Movement.AcceptPath(new[] { e.GlobalPosition, e.GlobalPosition + new Vector2(0, 100) }, spawns.Paths.Navigation.Grid.NavigationRevision);
        foreach (var priority in new[] { TowerTargeting.Priority.First, TowerTargeting.Priority.Closest })
        {
            Require(TowerTargeting.Find(spawns.Registry, new Vector2(640, 640), 100, priority) == BruteTarget(spawns.Registry, new Vector2(640, 640), 100, priority), "Equal scores preserve living order");
            first.GlobalPosition = new(1140, 640); first.GlobalPosition = new(640, 640);
            Require(TowerTargeting.Find(spawns.Registry, new Vector2(640, 640), 100, priority) == first, "Bucket swap/reinsert cannot change tie winner");
        }
        Random random = new(31);
        for (int i = 0; i < 200; i++) AreaDamageChecks.Spawn(spawns, new(random.Next(-2000, 2000), random.Next(-2000, 2000)));
        foreach (var priority in new[] { TowerTargeting.Priority.First, TowerTargeting.Priority.Closest })
            for (int i = 0; i < 100; i++)
            {
                Vector2 center = new(random.Next(-1500, 1500), random.Next(-1500, 1500)); float range = random.Next(0, 2000);
                Require(TowerTargeting.Find(spawns.Registry, center, range, priority) == BruteTarget(spawns.Registry, center, range, priority), "Random target selection matches old scan including stale paths");
            }
        first.Arrive(); Require(TowerTargeting.Find(spawns.Registry, new Vector2(640, 640), 100, TowerTargeting.Priority.Closest) == second, "Removed tie winner is never returned");
    }

    // =========================================================
    // Original production targeting retained only as a correctness/performance oracle.
    internal static Enemy BruteTarget(EnemyRegistry registry, Vector2 position, float range, TowerTargeting.Priority priority)
    {
        Enemy best = null; float score = float.PositiveInfinity;
        foreach (Enemy enemy in registry.Living)
        {
            if (!enemy.IsAlive) continue;
            float distance = position.DistanceSquaredTo(enemy.GlobalPosition);
            if (distance > range * range) continue;
            float candidate = priority == TowerTargeting.Priority.First ? enemy.RemainingPathDistance : distance;
            if (candidate < score) { score = candidate; best = enemy; }
        }
        return best;
    }

    // =========================================================
    // Sparse shield/hull blast outputs match a brute-force snapshot with overrides and penetration intact.
    private static void BlastParity(Node main, SpawnManager spawns)
    {
        foreach (DamageInfo hit in new[] { new DamageInfo(3, DamageType.Explosive), new DamageInfo(7, DamageType.Energy, 2, .8f, .3f) })
        {
            AreaDamageChecks.Clear(spawns); Random random = new(89); var expected = new Dictionary<Enemy, DamageResult>();
            Vector2 center = new(2000, 2000);
            for (int i = 0; i < 500; i++)
            {
                Enemy enemy = AreaDamageChecks.Spawn(spawns, center + new Vector2(random.Next(-1500, 1500), random.Next(-1500, 1500)), i % 2 == 0);
                double squared = EnemySpatialIndex.DistanceSquared(enemy.GlobalPosition, center);
                DamageInfo scaled = squared <= 800 * 800 ? AreaDamage.AtDistance(hit, (float)Math.Sqrt(squared), 800, .25f) : new(0, hit.Type);
                expected[enemy] = DamageResolver.Resolve(scaled, enemy.CurrentShield, enemy.Health);
            }
            AreaDamage.ApplyInstant(center, 800, hit, .25f, spawns.Registry);
            foreach (var pair in expected)
            {
                AreaDamageChecks.Near(pair.Key.Health, pair.Value.Health, "Sparse blast hull parity");
                AreaDamageChecks.Near(pair.Key.CurrentShield, pair.Value.Shield, "Sparse blast shield parity");
            }
        }
        AreaDamageChecks.Clear(spawns); Vector2 origin = new(2000, 2000);
        Enemy first = AreaDamageChecks.Spawn(spawns, origin), survivor = AreaDamageChecks.Spawn(spawns, origin + Vector2.Right);
        Enemy remote = AreaDamageChecks.Spawn(spawns, origin + new Vector2(500, 0)); first.TakeDamage(55);
        first.Removed += _ =>
        {
            AreaDamage.ApplyInstant(remote.GlobalPosition, 0, new(10, DamageType.Energy, hullCoefficientOverride: 1), 1, spawns.Registry);
            survivor.GlobalPosition += new Vector2(1000, 0);
        };
        AreaDamage.ApplyInstant(origin, 80, new(10, DamageType.Energy, hullCoefficientOverride: 1), 1, spawns.Registry);
        AreaDamageChecks.Near(remote.Health, 50, "Nested blast uses independent buffers");
        AreaDamageChecks.Near(survivor.Health, 50, "Outer recipient/falloff snapshot survives nested damage and movement");
    }

    // =========================================================
    // Run the old emitter and optimized emitter separately against identical moving/spawning fixtures.
    private static void WaveParity(Node main, SpawnManager spawns, int rate)
    {
        float[] baseline = WaveScenario(main, spawns, rate, false), indexed = WaveScenario(main, spawns, rate, true);
        Require(baseline.Length == indexed.Length, "Wave fixture has identical membership");
        for (int i = 0; i < baseline.Length; i++) AreaDamageChecks.Near(indexed[i], baseline[i], "Swept health/falloff matches original at " + rate + "Hz");
    }

    // =========================================================
    // Fast net chords, late spawns, multiple position writes, cell boundaries and end-of-life overshoot.
    private static float[] WaveScenario(Node main, SpawnManager spawns, int rate, bool indexed)
    {
        AreaDamageChecks.Clear(spawns); var enemies = new List<Enemy>(); Vector2 origin = new(-128, -128);
        for (int i = 0; i < 90; i++) enemies.Add(AreaDamageChecks.Spawn(spawns, origin + new Vector2(i * 11 - 450, i % 3 * 37)));
        Node wave;
        if (indexed) { var w = new ShockwaveEmitter(); w.Initialize(spawns.Registry, origin, new(.4f, DamageType.Energy), 400, 250, .25f); wave = w; }
        else { var w = new BruteForceShockwave(); w.Initialize(spawns.Registry, origin, new(.4f, DamageType.Energy), 400, 250, .25f); wave = w; }
        main.GetNode("World/Projectiles").AddChild(wave); wave.SetPhysicsProcess(false);
        ulong other = spawns.Registry.BeginWaveQuery(); // A slower reader must not discard the fast reader's samples.
        var scratch = new List<EnemySpatialIndex.Sweep>();
        for (int tick = 0; tick < rate; tick++)
        {
            for (int i = 0; i < enemies.Count; i++)
                if (i % 3 == 0) enemies[i].GlobalPosition = origin + new Vector2((tick % 2 == 0 ? 1 : -1) * (i % 6 == 0 ? 1000000 : 700 + i), i % 7);
                else if (i % 3 == 1)
                { enemies[i].GlobalPosition += new Vector2(1000, 300); enemies[i].GlobalPosition -= new Vector2(1008, 300); }
            if (tick == rate / 2) enemies.Add(AreaDamageChecks.Spawn(spawns, origin + new Vector2(30, 0)));
            if (indexed) ((ShockwaveEmitter)wave)._PhysicsProcess(1.0 / rate); else ((BruteForceShockwave)wave)._PhysicsProcess(1.0 / rate);
            if (tick % 4 == 0) spawns.Registry.GetWaveCandidates(other, origin, 0, 100, scratch);
        }
        if (indexed) ((ShockwaveEmitter)wave)._PhysicsProcess(3); else ((BruteForceShockwave)wave)._PhysicsProcess(3);
        spawns.Registry.EndWaveQuery(other); wave.Free();
        return enemies.Select(e => e.Health).ToArray();
    }

    // =========================================================
    // Two worlds never share membership, buffers or history, and teardown releases the registry list.
    private static void WorldIsolation(Node main, SpawnManager spawns)
    {
        EnemyRegistry isolated = new(); main.AddChild(isolated); List<Enemy> results = new();
        isolated.GetEnemiesInRadius(Vector2.Zero, 10000, results); Require(results.Count == 0, "Other world is empty");
        Enemy independent = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn").Instantiate<Enemy>();
        main.AddChild(independent); independent.Initialize(isolated, spawns.Paths); independent.Movement.SetPhysicsProcess(false);
        independent.GlobalPosition = Vector2.Zero;
        isolated.GetEnemiesInRadius(Vector2.Zero, 0, results);
        Require(results.Count == 1 && results[0] == independent && !spawns.Registry.Contains(independent), "Populated worlds have isolated membership/indexes");
        main.RemoveChild(isolated);
        Require(isolated.Living.Count == 0 && !isolated.Contains(independent), "World unload clears membership and history");
        independent.Free(); isolated.Free();
        AreaDamageChecks.Clear(spawns); Enemy e = AreaDamageChecks.Spawn(spawns, Vector2.Zero);
        spawns.Registry.GetEnemiesInRadius(Vector2.Zero, 0, results); Require(results.Count == 1 && results[0] == e, "Other world teardown cannot corrupt this world");
    }
    // =========================================================
    // Invalid query policy is explicit rather than relying on NaN comparisons.
    private static void Invalid(Action action)
    { try { action(); } catch (ArgumentException) { return; } throw new InvalidOperationException("Invalid spatial query accepted"); }
    // =========================================================
    // Stop the integration runner on any parity or membership failure.
    private static void Require(bool condition, string message) => AreaDamageChecks.Require(condition, message);
}
