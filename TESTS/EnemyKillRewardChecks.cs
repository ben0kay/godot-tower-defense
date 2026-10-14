// Production-scene tests for reusable per-enemy credit rewards, not wave-completion bonuses.
using Godot;
using System;
using static WaveBuilder;

public static class EnemyKillRewardChecks
{
    // =========================================================
    // Kill rewards pay once and update the existing wallet/HUD; arrivals pay nothing.
    public static void Run(Node battle, SpawnManager spawns)
    {
        EconomyManager wallet = battle.GetNode<EconomyManager>("Economy");
        GameplayHUD hud = battle.GetNode<GameplayHUD>("UI/GameplayHUD");
        WaveManager waves = battle.GetNode<WaveManager>("Waves");
        Label label = hud.GetNode<Label>("TopBar/Status/Credits");
        PackedScene scene = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn");

        wallet.Initialize(new LevelConfig { StartingCredits = 100 });
        int updates = 0;
        void Changed(string id, int amount) { if (id == wallet.CreditsResource.ResourceId) updates++; }
        wallet.BalanceChanged += Changed;
        try
        {
            Enemy killed = spawns.Spawn(scene, true);
            Require(killed != null && killed.Definition.KillRewardCredits == 3,
                "Saved Enemy001 definition grants 3 kill credits");
            killed.Movement.SetPhysicsProcess(false); killed.TakeDamage(10000);
            Require(wallet.GetBalance(wallet.CreditsResource) == 103 && label.Text == "CREDITS 103" &&
                updates == 1, "A kill pays 3 Credits and updates the HUD exactly once");
            killed.TakeDamage(10000); killed.Arrive();
            Require(wallet.GetBalance(wallet.CreditsResource) == 103 && updates == 1,
                "Dead ships cannot pay a second time");

            Enemy arrived = spawns.Spawn(scene, true);
            Require(arrived != null, "Spawn arrival test enemy");
            arrived.Movement.SetPhysicsProcess(false); arrived.Arrive();
            Require(wallet.GetBalance(wallet.CreditsResource) == 103 && updates == 1,
                "Enemy arrival never pays a kill reward");

            Enemy zeroReward = spawns.Spawn(scene, true);
            Require(zeroReward != null, "Spawn configurable zero-reward enemy");
            zeroReward.Movement.SetPhysicsProcess(false);
            zeroReward.Definition = (EnemyDefinition)zeroReward.Definition.Duplicate();
            zeroReward.Definition.KillRewardCredits = 0;
            zeroReward.TakeDamage(10000);
            Require(wallet.GetBalance(wallet.CreditsResource) == 103 && updates == 1,
                "A zero-credit override pays nothing and leaves the shared type resource unchanged");

            waves.Configure(new[] { Wave(1, Batch(1, 0, 0, Enemy("enemy001"))) },
                WaveCatalog.LoadScenes());
            Require(waves.StartWave() && waves.AliveCount == 1, "Spawn a wave-owned enemy");
            Enemy waveEnemy = spawns.Registry.Living[0];
            waveEnemy.Movement.SetPhysicsProcess(false); waveEnemy.TakeDamage(10000);
            Require(wallet.GetBalance(wallet.CreditsResource) == 106 && updates == 2 &&
                waves.State == WaveManager.WaveState.AllWavesComplete,
                "Wave kills pay the same reward and resolve normally");

            GD.Print("PASS: Enemy001 kill rewards, HUD events, arrival/duplicate exclusions and wave payout");
        }
        finally
        {
            wallet.BalanceChanged -= Changed;
            waves.Configure(WaveCatalog.GetWaves(spawns.Paths.Navigation.Grid.Level.Settings.WaveSetId),
                WaveCatalog.LoadScenes());
            waves.SetPhysicsProcess(false);
            wallet.Initialize(spawns.Paths.Navigation.Grid.Level.Settings);
        }
    }

    // =========================================================
    // Fail the integration run when a kill-credit rule is broken.
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
