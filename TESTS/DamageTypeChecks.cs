// Deterministic typed hit math plus real enemy shield/hull lifecycle integration.
using Godot;
using System;
public static class DamageTypeChecks
{
    // =========================================================
    // Verify base-unit spillover, invalid input safety and production target-only lifecycle.
    public static void Run(Node main, SpawnManager spawns)
    {
        DamageResponseProfile profile = DamageResolver.DefaultProfile;
        Check(DamageType.Energy, 20, 30, 60, 0, 55.75f);
        Check(DamageType.Energy, 20, 100, 60, 60, 60);
        Check(DamageType.Energy, 15, 30, 60, 0, 60);
        Check(DamageType.Kinetic, 12, 0, 60, 0, 48);
        Check(DamageType.Kinetic, 12, 100, 60, 94, 60);
        Check(DamageType.Explosive, 20, 100, 60, 95, 60);
        Check(DamageType.Explosive, 20, 0, 60, 0, 35);
        Check(DamageType.Energy, 20, 0, 60, 0, 43);
        Check(DamageType.Energy, 0, 30, 60, 30, 60);
        DamageResponseProfile zero = (DamageResponseProfile)profile.Duplicate(); zero.EnergyShield = 0;
        Require(DamageResolver.Resolve(new(100, DamageType.Energy), 30, 60, zero) == new DamageResult(30, 60), "Zero shield coefficient blocks, never bypasses");
        foreach (float amount in new[] { -1f, float.NaN, float.PositiveInfinity })
            Invalid(() => new DamageInfo(amount, DamageType.Energy));
        Invalid(() => new DamageInfo(1, (DamageType)99));
        zero.EnergyHull = float.NaN; Invalid(zero.Validate);
        zero.EnergyHull = -1; Invalid(zero.Validate);
        Invalid(() => DamageResolver.Resolve(new(1, DamageType.Energy), float.NaN, 60));
        DamageResult huge = DamageResolver.Resolve(new(float.MaxValue, DamageType.Energy), 1, float.MaxValue);
        Require(float.IsFinite(huge.Health) && float.IsFinite(huge.Shield) && huge.Health >= 0 && huge.Shield >= 0, "Extreme finite damage never overflows state");
        PackedScene scene = GD.Load<PackedScene>("res://ENEMIES/Types/Enemy001/Enemy001.tscn");
        Enemy target = spawns.SpawnWave(scene, new RandomNumberGenerator { Seed = 3 }, spawnWithShield: true);
        Enemy other = spawns.Spawn(scene, true);
        target.Movement.SetPhysicsProcess(false); other.Movement.SetPhysicsProcess(false);
        target.ApplyDamage(new(20, DamageType.Energy));
        Require(target.CurrentShield == 60 && target.Health == 60 && !target.GetNode<DefenseBar>("DefenseBar").Visible && other.Health == 60 && other.CurrentShield == 0,
            "Typed shield-only hit changes only target and leaves hull bar alone");
        target.TakeDamage(30); target.ApplyDamage(new(20, DamageType.Energy));
        Require(target.CurrentShield == 0 && target.Health == 55.75f && target.GetNode<DefenseBar>("DefenseBar").Visible, "Legacy1:1 and typed fractional spillover share one hull lifecycle");
        EconomyManager wallet = main.GetNode<EconomyManager>("Economy");
        int before = wallet.GetBalance(wallet.CreditsResource);
        target.ApplyDamage(new(10000, DamageType.Energy)); target.ApplyDamage(new(10000, DamageType.Energy)); target.Arrive();
        Require(wallet.GetBalance(wallet.CreditsResource) == before + 3, "Repeated typed hits pay a kill once");
        other.Arrive(); other.ApplyDamage(new(10000, DamageType.Energy));
        Require(wallet.GetBalance(wallet.CreditsResource) == before + 3, "Arrived enemy cannot pay a typed kill");
        wallet.Initialize(spawns.Paths.Navigation.Grid.Level.Settings);
        GD.Print("PASS: typed shield/hull coefficients, base spillover, invalid inputs, target isolation and idempotent lifecycle");
    }
    // =========================================================
    // Compare pure response results at ordinary float precision.
    private static void Check(DamageType type, float amount, float shield, float health, float expectedShield, float expectedHealth)
    {
        DamageResult result = DamageResolver.Resolve(new(amount, type), shield, health);
        Require(Mathf.IsEqualApprox(result.Shield, expectedShield) && Mathf.IsEqualApprox(result.Health, expectedHealth), "Typed response mismatch: " + type);
    }
    // =========================================================
    // Malformed values fail before applying a hit.
    private static void Invalid(Action action)
    { try { action(); } catch (ArgumentException) { return; } catch (InvalidOperationException) { return; } throw new InvalidOperationException("Invalid damage accepted."); }
    // =========================================================
    // Fail this checkpoint immediately on a response regression.
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
