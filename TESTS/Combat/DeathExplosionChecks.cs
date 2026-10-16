// Focused scene-level death puff tests: auto scaling and killed-only effects.
using Godot;
using System;

public static class DeathExplosionChecks
{
    // =========================================================
    // Test one-shot effects without running GPU emitters during 500-ship navigation checks.
    public static void Run(Node battlefield, SpawnManager spawns, FeedbackMasterConfig feedback)
    {
        Node2D layer = battlefield.GetNode<Node2D>("World/Effects");
        PackedScene enemyScene = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn");
        bool oldEnabled = feedback.DeathExplosionsEnabled;
        feedback.DeathExplosionsEnabled = true;
        try
        {
            Enemy first = spawns.Spawn(enemyScene, true);
            Require(first != null, "Can spawn Enemy001 for death visuals");
            first.Movement.SetPhysicsProcess(false);
            float originalSize = DeathExplosion.MeasureVisualSize(first.GetNode<Sprite2D>("Sprite"));
            Require(Mathf.IsEqualApprox(originalSize, 32f), "Default Enemy001 sprite measures 32 world pixels");
            first.TakeDamage(100000);
            Require(layer.GetChildCount() == 1 && layer.GetChild<DeathExplosion>(0).AppliedScale == 1f,
                "A kill creates one 1x puff independently of the freed enemy");
            first.TakeDamage(100000); first.Arrive();
            Require(layer.GetChildCount() == 1, "Repeated damage or arrival after kill cannot duplicate explosion");

            Enemy large = spawns.Spawn(enemyScene, true);
            Require(large != null, "Can spawn second larger enemy");
            large.Movement.SetPhysicsProcess(false);
            large.GetNode<Sprite2D>("Sprite").Scale = Vector2.One * 2f;
            float enlargedSize = DeathExplosion.MeasureVisualSize(large.GetNode<Sprite2D>("Sprite"));
            Require(Mathf.IsEqualApprox(enlargedSize, originalSize * 2f), "Sprite scaling changes measured visual size");
            large.TakeDamage(100000);
            Require(layer.GetChildCount() == 2 &&
                Mathf.IsEqualApprox(layer.GetChild<DeathExplosion>(1).AppliedScale, 2f),
                "Automatically doubled sprite size doubles explosion scale");

            Enemy boss = spawns.Spawn(enemyScene, true);
            Require(boss != null, "Can spawn an enemy with an override");
            boss.Movement.SetPhysicsProcess(false);
            boss.Definition = (EnemyDefinition)boss.Definition.Duplicate();
            boss.Definition.DeathExplosionMultiplier = 1.5f;
            boss.TakeDamage(100000);
            Require(layer.GetChildCount() == 3 &&
                Mathf.IsEqualApprox(layer.GetChild<DeathExplosion>(2).AppliedScale, 1.5f),
                "Per-type override scales puff without changing visual size");

            Enemy arrived = spawns.Spawn(enemyScene, true);
            Require(arrived != null, "Can spawn a non-kill arrival");
            arrived.Movement.SetPhysicsProcess(false); arrived.Arrive();
            Require(layer.GetChildCount() == 3, "Arrivals do not create explosions");
            Require(DeathExplosion.ActiveCount == 3, "Effects track active instance count without leaks");
            GD.Print("PASS: automatic death puff scale, multiplier, kill-only spawning and independence");
        }
        finally
        {
            foreach (Node effect in layer.GetChildren()) effect.Free();
            feedback.DeathExplosionsEnabled = oldEnabled;
            Require(DeathExplosion.ActiveCount == 0, "Effect cleanup releases concurrency slots");
        }
    }

    // =========================================================
    // Report a focused, reproducible failure when a death effect rule regresses.
    private static void Require(bool ok, string message)
    { if (!ok) throw new InvalidOperationException(message); }
}
