// Wave-configured enemy shields: instance isolation, shield-first damage and unchanged debug spawns.
using Godot;
using System;
using System.Linq;
using static WaveBuilder;

public static class EnemyShieldChecks
{
    #region Shield and Wave Integration
    // =========================================================
    // Uses the real WaveManager and SpawnManager; changes only in-memory wave state.
    public static void Run(Node battle, SpawnManager spawns)
    {
        WaveManager waves = battle.GetNode<WaveManager>("Waves");
        PackedScene scene = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn");
        var scenes = WaveCatalog.LoadScenes();
        string waveSet = spawns.Paths.Navigation.Grid.Level.Settings.WaveSetId;
        Require(spawns.Registry.Living.Count == 0, "Shield fixture begins with no live enemies");
        try
        {
            EnemyDefinition authored = GD.Load<EnemyDefinition>("res://ENEMIES/Types/Enemy001/Enemy001.tres");
            Require(authored.MaxShield == 100f && new EnemyDefinition().MaxShield == 0f,
                "Enemy001 has opt-in shield capacity while generic enemy types default to none");

            WaveBatch batch = Batch(6, 0, 0,
                Enemy("basic_mk1", weight: 50),
                Enemy("basic_mk1", weight: 50, shield: true));
            RandomNumberGenerator random = new() { Seed = 123 };
            WaveEnemyEntry[] entries = WaveCatalog.Compose(batch, random);
            Require(entries.Count(e => e.SpawnWithShield) == 3 &&
                entries.Count(e => !e.SpawnWithShield) == 3,
                "Weighted wave entries with one enemy ID retain separate shield flags");

            waves.Configure(new[] { Wave(1, batch) }, scenes);
            Require(waves.StartWave(), "Opt-in shield wave starts");
            for (int i = 0; waves.PlannedRemaining > 0 && i < 12; i++)
            {
                foreach (Enemy enemy in spawns.Registry.Living) enemy.Movement.SetPhysicsProcess(false);
                waves._PhysicsProcess(0);
            }
            Enemy[] ships = spawns.Registry.Living.ToArray();
            foreach (Enemy enemy in ships) enemy.Movement.SetPhysicsProcess(false);
            Require(ships.Length == 6 && waves.PlannedRemaining == 0 &&
                ships.Count(e => e.CurrentShield == 100f) == 3 &&
                ships.Count(e => e.CurrentShield == 0f) == 3,
                "WaveManager propagates optional full shields to individual spawned enemies");

            Enemy shielded = ships.First(e => e.CurrentShield == 100f);
            Enemy overflow = ships.Last(e => e.CurrentShield == 100f);
            Enemy ordinary = ships.First(e => e.CurrentShield == 0f);
            DefenseBar bar = shielded.GetNode<DefenseBar>("DefenseBar");

            shielded.TakeDamage(40f);
            Require(shielded.CurrentShield == 60f && shielded.Health == 60f && !bar.Visible,
                "Shield-only damage preserves hull HP and the hull-only defense bar");
            shielded.TakeDamage(60f);
            Require(shielded.CurrentShield == 0f && shielded.Health == 60f && shielded.IsAlive,
                "An exact shield-breaking hit does not damage health or kill the enemy");
            shielded.TakeDamage(12f);
            Require(shielded.Health == 48f && bar.Visible &&
                Mathf.IsEqualApprox(bar.HealthFraction, 0.8f),
                "A subsequent hit damages health and updates the existing health bar");

            overflow.TakeDamage(125f);
            Require(overflow.CurrentShield == 0f && overflow.Health == 35f && overflow.IsAlive,
                "Damage exceeding the shield carries exactly the excess into hull health");
            ordinary.TakeDamage(12f);
            Require(ordinary.CurrentShield == 0f && ordinary.Health == 48f,
                "Unshielded wave entries retain original damage behavior");

            Enemy debug = spawns.Spawn(scene, true);
            Require(debug != null && debug.Definition.MaxShield == 100f &&
                debug.CurrentShield == 0f && debug.Health == 60f,
                "F2-style default spawning never grants shields even with shield capacity");
            debug.Movement.SetPhysicsProcess(false);
            debug.TakeDamage(12f);
            Require(debug.Health == 48f && shielded.CurrentShield == 0f &&
                authored.MaxShield == 100f, "Per-instance shield state never modifies the shared definition");
            GD.Print("PASS: wave-only shield opt-in, mixed entries, shield overflow and debug defaults");
        }
        finally
        {
            foreach (Enemy enemy in spawns.Registry.Living.ToArray())
                if (enemy.IsAlive) enemy.TakeDamage(100000f);
            waves.Configure(WaveCatalog.GetWaves(waveSet), scenes);
            waves.SetPhysicsProcess(false);
        }
    }
    #endregion

    // =========================================================
    // Fail immediately if shield integration changes any existing spawn semantics.
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
