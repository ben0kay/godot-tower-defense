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
            ShieldVisual visual = shielded.GetNodeOrNull<ShieldVisual>("ShieldVisual");
            Require(bar.Visible && bar.HullVisible && bar.ShieldVisible
                && Mathf.IsEqualApprox(bar.HealthFraction, 1f) && Mathf.IsEqualApprox(bar.ShieldFraction, 1f)
                && !bar.IsProcessing() && !bar.IsPhysicsProcessing()
                && !ordinary.GetNode<DefenseBar>("DefenseBar").Visible,
                "Full-shield enemies display stacked full hull and shield meters; unshielded enemies stay uncluttered");
            shielded.Rotation = 0.3f; shielded.UpdateDefenseBarTransform();
            Require(Mathf.IsEqualApprox(bar.Rotation, -0.3f),
                "Shield and hull meters share counter-rotation from the existing movement callback");
            shielded.Rotation = 0f; shielded.UpdateDefenseBarTransform();
            Require(visual != null && visual.HasShieldVisual && visual.Visible &&
                !visual.IsProcessing() && !visual.IsPhysicsProcessing() &&
                shielded.GetNode<Sprite2D>("Sprite").Texture != null &&
                visual.GetNode<Sprite2D>("Aura").Scale.X > 0f &&
                ordinary.GetNodeOrNull<ShieldVisual>("ShieldVisual") == null,
                "Only shielded enemies lazily instance one baked, scaled, idle shield visual");

            shielded.TakeDamage(40f);
            Require(shielded.CurrentShield == 60f && shielded.Health == 60f && bar.Visible
                && bar.HullVisible && bar.ShieldVisible
                && Mathf.IsEqualApprox(bar.HealthFraction, 1f) && Mathf.IsEqualApprox(bar.ShieldFraction, 0.6f)
                && visual.HasShieldVisual && visual.GetNode<Sprite2D>("Aura").Visible &&
                visual.GetNode<Sprite2D>("Aura").Modulate.A > visual.Style.IdleOpacity,
                "Shield-only damage flashes the same aura and preserves hull/defense bar");
            shielded.TakeDamage(60f);
            Require(shielded.CurrentShield == 0f && shielded.Health == 60f && shielded.IsAlive &&
                !bar.ShieldVisible && !bar.Visible && Mathf.IsZeroApprox(bar.ShieldFraction)
                && !visual.HasShieldVisual && visual.Visible,
                "Exact shield break leaves hull intact and starts a brief event-only break flash");
            shielded.TakeDamage(12f);
            Require(shielded.Health == 48f && bar.Visible && bar.HullVisible && !bar.ShieldVisible &&
                Mathf.IsEqualApprox(bar.HealthFraction, 0.8f),
                "A subsequent hit damages health and updates the existing health bar");

            overflow.TakeDamage(125f);
            Require(overflow.CurrentShield == 0f && overflow.Health == 35f && overflow.IsAlive,
                "Damage exceeding the shield carries exactly the excess into hull health");
            ordinary.TakeDamage(12f);
            Require(ordinary.CurrentShield == 0f && ordinary.Health == 48f &&
                ordinary.GetNodeOrNull<ShieldVisual>("ShieldVisual") == null,
                "Unshielded wave entries retain original damage and allocate no shield art");

            Require(shielded.RestoreShield(15f) == 15f &&
                bar.Visible && bar.HullVisible && bar.ShieldVisible
                && Mathf.IsEqualApprox(bar.ShieldFraction, 0.15f)
                && visual.HasShieldVisual && visual.Visible &&
                shielded.GetNodeOrNull<ShieldVisual>("ShieldVisual") == visual,
                "Existing support restoration reactivates the same visual without new nodes or regeneration");
            shielded.TakeDamage(15f);
            Require(!visual.HasShieldVisual && !bar.ShieldVisible && bar.HullVisible
                && bar.Visible && shielded.Health == 48f,
                "Re-depleting a restored shield plays break feedback without changing hull");

            Enemy debug = spawns.Spawn(scene, true);
            Require(debug != null && debug.Definition.MaxShield == 100f &&
                debug.CurrentShield == 0f && debug.Health == 60f,
                "F2-style default spawning never grants shields even with shield capacity");
            debug.Movement.SetPhysicsProcess(false);
            debug.TakeDamage(12f);
            Require(debug.Health == 48f && shielded.CurrentShield == 0f
                && !debug.GetNode<DefenseBar>("DefenseBar").ShieldVisible &&
                authored.MaxShield == 100f, "Per-instance shield state never modifies the shared definition");
            GD.Print("PASS: opt-in shields, stacked hull/shield bars, event-only redraw, depletion/restoration, overflow and debug defaults");
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
