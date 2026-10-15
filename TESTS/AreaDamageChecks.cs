// Pure falloff and real registry/lifecycle fixtures for reusable instantaneous area damage.
using Godot;
using System;
using System.Linq;
using System.Diagnostics;

public static class AreaDamageChecks
{
    // =========================================================
    // No weapon class is involved: the shared mechanic receives every balance value as input.
    public static void Run(Node main, SpawnManager spawns)
    {
        DamageInfo source = new(60, DamageType.Explosive, 6, 0.15f, 0.05f);
        foreach (var sample in new[] { (0f, 60f), (40f, 37.5f), (80f, 15f), (81f, 0f) })
        {
            DamageInfo scaled = AreaDamage.AtDistance(source, sample.Item1, 80, 0.25f);
            Near(scaled.Amount, sample.Item2, "Linear center/half/edge/outside base amount");
            Require(scaled.Type == source.Type && scaled.ShieldCoefficientOverride == 6 && scaled.HullCoefficientOverride == 0.15f
                && scaled.ShieldPenetration == 0.05f, "Falloff preserves complete defense payload");
        }
        Near(AreaDamage.AtDistance(source, 80, 80, 0).Amount, 0, "Zero edge has no hidden minimum");
        Near(AreaDamage.AtDistance(source, 80, 80, 1).Amount, 60, "Flat full damage accepts100% edge");
        Near(AreaDamage.AtDistance(source, 40, 80, 0.75f).Amount, 52.5f, "Different weapons can supply different falloffs");
        Near(AreaDamage.AtDistance(source, 0, 0, 0).Amount, 60, "Zero-radius exact center is a full hit");
        Near(AreaDamage.AtDistance(source, 0.001f, 0, 1).Amount, 0, "Zero-radius noncenter is outside");
        Require(float.IsFinite(AreaDamage.AtDistance(new(float.MaxValue, DamageType.Kinetic), float.MaxValue,
            float.MaxValue, 1).Amount), "Extreme finite falloff cannot overflow");
        foreach (float invalid in new[] { -1f, float.NaN, float.NegativeInfinity, float.PositiveInfinity })
        {
            Invalid(() => AreaDamage.AtDistance(source, invalid, 80, 0.25f));
            Invalid(() => AreaDamage.AtDistance(source, 0, invalid, 0.25f));
            Invalid(() => AreaDamage.AtDistance(source, 0, 80, invalid));
        }
        Invalid(() => AreaDamage.AtDistance(source, 0, 80, 1.01f));
        Geometry(main, spawns);
        Defense(spawns);
        Mutation(main, spawns);
        Population(main, spawns);
        main.GetNode<EconomyManager>("Economy").Initialize(spawns.Paths.Navigation.Grid.Level.Settings);
        GD.Print("PASS: instant AoE linear/zero/flat falloff, inclusive centers, defense payloads, mutation-safe one-hit dispatch and500-enemy impact");
    }

    // =========================================================
    // One registry snapshot includes the primary exactly once and survives immediate removal on kill.
    private static void Geometry(Node main, SpawnManager spawns)
    {
        Clear(spawns);
        Vector2 center = new(2000, 2000);
        Enemy primary = Spawn(spawns, center), half = Spawn(spawns, center + new Vector2(40, 0));
        Enemy edge = Spawn(spawns, center + new Vector2(80, 0)), outside = Spawn(spawns, center + new Vector2(80.01f, 0));
        spawns.Registry.Register(half); // Registry uniqueness prevents duplicate blast hits.
        EconomyManager wallet = main.GetNode<EconomyManager>("Economy"); int credits = wallet.GetBalance(wallet.CreditsResource);
        int hits = AreaDamage.ApplyInstant(center, 80, new(60, DamageType.Explosive), 0.25f, spawns.Registry);
        Require(hits == 3 && !primary.IsAlive, "One impact includes exactly center/half/edge recipients");
        Near(half.Health, 13.125f, "Half-radius recipient hit once through Explosive1.25 hull");
        Near(edge.Health, 41.25f, "Exact edge included at weapon-supplied25%");
        Near(outside.Health, 60, "Outside center stays untouched");
        Require(wallet.GetBalance(wallet.CreditsResource) == credits + 3, "Primary death pays only one reward");
        float untouched = outside.Health;
        Invalid(() => AreaDamage.ApplyInstant(center, -1, new(60, DamageType.Kinetic), 0.25f, spawns.Registry));
        Invalid(() => AreaDamage.ApplyInstant(new(float.NaN, 0), 80, new(60, DamageType.Kinetic), 0.25f, spawns.Registry));
        Invalid(() => AreaDamage.ApplyInstant(center, 80, new(60, DamageType.Kinetic), 2, spawns.Registry));
        Near(outside.Health, untouched, "Invalid impact settings never mutate recipients");
        Clear(spawns);
        Enemy exact = Spawn(spawns, center), near = Spawn(spawns, center + new Vector2(0.01f, 0));
        Require(AreaDamage.ApplyInstant(center, 0, new(10, DamageType.Kinetic), 0, spawns.Registry) == 1,
            "Zero-radius blast selects only the exact center");
        Near(exact.Health, 50, "Exact center gets base damage at zero radius"); Near(near.Health, 60, "Near zero is outside zero radius");
        Clear(spawns);
    }

    // =========================================================
    // Each area hit is reduced in base units, then resolved exactly once by the existing defense layer.
    private static void Defense(SpawnManager spawns)
    {
        Vector2 center = new(2000, 2000);
        foreach (var sample in new[] {
            (new DamageInfo(20, DamageType.Explosive), 100f, 95f, 60f),
            (new DamageInfo(20, DamageType.Explosive), 3f, 0f, 50f),
            (new DamageInfo(20, DamageType.Explosive), 0f, 0f, 35f),
            (new DamageInfo(12, DamageType.Energy, 6, 0.15f), 30f, 0f, 58.95f),
            (new DamageInfo(12, DamageType.Kinetic, shieldPenetration: 0.05f), 100f, 100 - 11.4f * DamageResolver.DefaultProfile.KineticShield, 59.4f),
            (new DamageInfo(12, DamageType.Kinetic, 0, 2, 0.5f), 100f, 100f, 48f) })
        {
            Enemy enemy = Spawn(spawns, center, sample.Item2 > 0);
            if (sample.Item2 > 0) enemy.TakeDamage(100 - sample.Item2);
            AreaDamage.ApplyInstant(center, 80, sample.Item1, 0.25f, spawns.Registry);
            Near(enemy.CurrentShield, sample.Item3, "Area shield-only/overflow/overrides/penetration result");
            Near(enemy.Health, sample.Item4, "Area hull coefficient applied once, never a direct plus splash hit");
            Clear(spawns);
        }
    }

    // =========================================================
    // A killed recipient may kill, free, queue or unregister later candidates during the same blast.
    private static void Mutation(Node main, SpawnManager spawns)
    {
        Vector2 center = new(2000, 2000);
        Enemy first = Spawn(spawns, center), killed = Spawn(spawns, center), freed = Spawn(spawns, center);
        Enemy queued = Spawn(spawns, center), unregistered = Spawn(spawns, center);
        first.Removed += _ => { killed.TakeDamage(1000); freed.Free(); queued.QueueFree(); spawns.Registry.Unregister(unregistered); };
        EconomyManager wallet = main.GetNode<EconomyManager>("Economy"); int credits = wallet.GetBalance(wallet.CreditsResource);
        Require(AreaDamage.ApplyInstant(center, 80, new(1000, DamageType.Explosive), 1, spawns.Registry) == 1,
            "Stale snapshot candidates are skipped at application time");
        Near(queued.Health, 60, "Queued-for-deletion recipient is skipped"); Near(unregistered.Health, 60, "Unregistered recipient is skipped");
        Require(wallet.GetBalance(wallet.CreditsResource) == credits + 6, "Callback kill and blast kill each pay once");
        Clear(spawns);
        if (GodotObject.IsInstanceValid(unregistered)) unregistered.Free();
    }

    // =========================================================
    // Measure one bounded impact against500 recipients, with no physics objects or temporary combat nodes.
    private static void Population(Node main, SpawnManager spawns)
    {
        EnemyMasterConfig config = spawns.Paths.Config; int limit = config.MaxActiveEnemies; config.MaxActiveEnemies = Math.Max(limit, 500);
        try
        {
            Vector2 center = new(2000, 2000);
            for (int i = 0; i < 500; i++) Spawn(spawns, center + new Vector2(i % 5, i % 7));
            int nodes = Count(main); Stopwatch timer = Stopwatch.StartNew();
            int count = AreaDamage.ApplyInstant(center, 80, new(0.01f, DamageType.Kinetic), 0.25f, spawns.Registry);
            timer.Stop();
            Require(count == 500 && spawns.Registry.Living.All(e => e.Health < 60 && e.Health > 59), "500 living recipients each get one small hit");
            Require(Count(main) == nodes, "Shared impact calculation allocates no scene nodes");
            GD.Print($"AoE BENCH:500 recipients in one impact {timer.Elapsed.TotalMilliseconds:0.000}ms (headless, not an FPS guarantee)");
        }
        finally { Clear(spawns); config.MaxActiveEnemies = limit; }
    }

    // =========================================================
    // Production spawned enemies use isolated positions and manually stopped movement.
    public static Enemy Spawn(SpawnManager spawns, Vector2 position, bool shield = false)
    {
        PackedScene scene = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn");
        Enemy enemy = shield ? spawns.SpawnWave(scene, new RandomNumberGenerator { Seed = 19 }, spawnWithShield: true) : spawns.Spawn(scene, true);
        Require(enemy != null, "Area fixture enemy spawned"); enemy.Movement.SetPhysicsProcess(false); enemy.GlobalPosition = position; return enemy;
    }

    // =========================================================
    // Cleanup without granting synthetic rewards or altering authored assets.
    public static void Clear(SpawnManager spawns)
    { foreach (Enemy enemy in spawns.Registry.Living.ToArray()) if (GodotObject.IsInstanceValid(enemy)) enemy.Free(); }

    // =========================================================
    // Detect accidental temporary combat objects independently from the helper's returned count.
    private static int Count(Node node) { int count = 1; foreach (Node child in node.GetChildren()) count += Count(child); return count; }
    // =========================================================
    // Consistent finite tolerant comparisons for falloff and float defense arithmetic.
    public static void Near(float actual, float expected, string message) => Require(float.IsFinite(actual) && Mathf.Abs(actual - expected) < 0.001f, message);
    // =========================================================
    // All invalid settings must reject before mutation.
    private static void Invalid(Action action)
    { try { action(); } catch (ArgumentException) { return; } throw new InvalidOperationException("Invalid area settings accepted."); }
    // =========================================================
    // Fail the fixture on any shared-area contract violation.
    public static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
