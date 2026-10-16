// Integration coverage for per-level Citadel durability and actual arriving enemies.
using Godot;
using System;

public static class CitadelHealthChecks
{
    #region Arrival and Health Checks
    // =========================================================
    // Tests in the live Main scene without modifying saved level assets.
    public static void Run(Node battle, SpawnManager spawns)
    {
        CitadelHealth health = battle.GetNode<CitadelSession>("CitadelSession").Health;
        GameplayHUD hud = battle.GetNode<GameplayHUD>("UI/GameplayHUD");
        Label label = hud.GetNode<Label>("TopBar/Status/Citadel/Health");
        ProgressBar bar = hud.GetNode<ProgressBar>("TopBar/Status/Citadel/Bar");
        PackedScene scene = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn");
        int max = spawns.Paths.Navigation.Grid.Level.Settings.CitadelMaxHealth;
        int changed = 0, destroyed = 0;
        void OnChanged(int current, int maximum) => changed++;
        void OnDestroyed() => destroyed++;
        health.HealthChanged += OnChanged;
        health.Destroyed += OnDestroyed;
        try
        {
            Require(health.MaxHealth == max && max > 0, "Citadel loads max HP from this level's config");
            health.Initialize(10);
            Require(health.CurrentHealth == 10 && label.Text == "CITADEL 10 / 10" && bar.Value == 100,
                "Reset initializes full health and publishes HUD update");

            Enemy arrival = spawns.Spawn(scene, true);
            Require(arrival != null && arrival.Definition.CitadelDamage == 1, "Enemy001 has 1 arrival damage");
            arrival.Movement.SetPhysicsProcess(false); arrival.Arrive();
            Require(health.CurrentHealth == 9 && label.Text == "CITADEL 9 / 10" && bar.Value <= 100 && bar.Value >= 90,
                "An actual arrival applies damage, updates health text immediately and begins the bar transition");
            hud.SetCitadelHealth(9, 10, false);
        Require(bar.Value == 90, "Citadel bar has the correct destination value when the transition is snapped");
        int afterArrival = changed;
            arrival.Arrive(); arrival.TakeDamage(9999);
            Require(health.CurrentHealth == 9 && changed == afterArrival,
                "Repeated arrival or post-arrival hits never subtract additional HP");

            Enemy killed = spawns.Spawn(scene, true);
            Require(killed != null, "Spawn a killed enemy for arrival-only test");
            killed.Movement.SetPhysicsProcess(false); killed.TakeDamage(9999);
            Require(health.CurrentHealth == 9, "Killed enemies do not damage the Citadel");

            Enemy heavy = spawns.Spawn(scene, true);
            Require(heavy != null, "Spawn an enemy with a large per-instance arrival damage");
            heavy.Movement.SetPhysicsProcess(false);
            heavy.Definition = (EnemyDefinition)heavy.Definition.Duplicate();
            heavy.Definition.CitadelDamage = 20;
            heavy.Arrive();
            Require(health.CurrentHealth == 0 && destroyed == 1 && label.Text == "CITADEL 0 / 10",
                "Heavy arrival damage clamps at zero and emits one destruction event");
            health.TakeDamage(1000); health.TakeDamage(-10);
            Require(health.CurrentHealth == 0 && destroyed == 1, "Further damage never retriggers destruction");

            health.Initialize(7);
            Require(health.CurrentHealth == 7 && health.MaxHealth == 7 && destroyed == 1,
                "A fresh level reset restores live HP without a destruction event");
            health.TakeDamage(7);
            Require(health.IsDestroyed && destroyed == 2, "A new session may be destroyed once again");
            GD.Print("PASS: level-specific Citadel HP, arrival damage, HUD, idempotency and reset");
        }
        finally
        {
            health.HealthChanged -= OnChanged;
            health.Destroyed -= OnDestroyed;
            health.Initialize(max);
            Require(health.CurrentHealth == max && bar.Value == 100,
                "Citadel test restores level health and HUD before later tests");
        }
    }
    #endregion

    // =========================================================
    // Fail integration checks immediately on a health regression.
    private static void Require(bool ok, string message)
    { if (!ok) throw new InvalidOperationException(message); }
}
