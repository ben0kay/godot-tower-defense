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
        CheckHit(new(12, DamageType.Kinetic, shieldPenetration: 0.05f), 100, 60, 94.3f, 59.4f);
        CheckHit(new(12, DamageType.Kinetic, shieldPenetration: 0.05f), 3, 60, 0, 54);
        CheckHit(new(12, DamageType.Kinetic, shieldPenetration: 0.05f), 5.7f, 60, 0, 59.4f);
        CheckHit(new(12, DamageType.Kinetic, shieldPenetration: 0.05f), 0, 60, 0, 48);
        CheckHit(new(12, DamageType.Kinetic, shieldPenetration: 1), 100, 60, 100, 48);
        CheckHit(new(12, DamageType.Kinetic, shieldPenetration: 1), 0, 60, 0, 48);
        CheckHit(new(12, DamageType.Energy, 6, 0.15f), 100, 60, 28, 60);
        CheckHit(new(12, DamageType.Energy, 6, 0.15f), 0, 60, 0, 58.2f);
        CheckHit(new(12, DamageType.Energy, 6, 0.15f), 30, 60, 0, 58.95f);
        CheckHit(new(20, DamageType.Energy, shieldCoefficientOverride: 6), 200, 60, 80, 60);
        CheckHit(new(20, DamageType.Energy, hullCoefficientOverride: 0.15f), 30, 60, 0, 59.25f);
        CheckHit(new(12, DamageType.Kinetic, 0, shieldPenetration: 0.05f), 100, 60, 100, 59.4f);
        CheckHit(new(12, DamageType.Kinetic, 0), 100, 60, 100, 60);
        CheckHit(new(12, DamageType.Kinetic, hullCoefficientOverride: 0, shieldPenetration: 0.05f), 100, 60, 94.3f, 60);
        CheckHit(new(12, DamageType.Energy, hullCoefficientOverride: 0), 0, 60, 0, 60);
        foreach (float invalid in new[] { -1f, float.NaN, float.NegativeInfinity, float.PositiveInfinity })
        {
            Invalid(() => new DamageInfo(12, DamageType.Energy, shieldCoefficientOverride: invalid));
            Invalid(() => new DamageInfo(12, DamageType.Energy, hullCoefficientOverride: invalid));
            Invalid(() => new DamageInfo(12, DamageType.Kinetic, shieldPenetration: invalid));
        }
        Invalid(() => new DamageInfo(12, DamageType.Kinetic, shieldPenetration: 1.01f));
        foreach (float penetration in new[] { 0f, 0.05f, 0.5f, 1f })
            foreach (float shield in new[] { 0f, 1f, 100f, float.MaxValue })
            {
                DamageResult bounded = DamageResolver.Resolve(new(float.MaxValue, DamageType.Energy,
                    float.MaxValue, float.MaxValue, penetration), shield, float.MaxValue);
                Require(float.IsFinite(bounded.Shield) && float.IsFinite(bounded.Health) && bounded.Shield >= 0
                    && bounded.Shield <= shield && bounded.Health >= 0, "Overrides/penetration never overflow or create shield/health");
            }
        Require(DamageResolver.ResolveLegacy(12, 100, 60) == new DamageResult(88, 60), "Legacy hit remains1:1 shield-first");
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
        Enemy shielded = spawns.SpawnWave(scene, new RandomNumberGenerator { Seed = 9 }, spawnWithShield: true);
        Invalid(() => shielded.ApplyDamage(new(12, DamageType.Kinetic, shieldPenetration: float.NaN)));
        Require(shielded.CurrentShield == 100 && shielded.Health == 60, "Invalid hit leaves target state untouched");
        Enemy untouched = spawns.Spawn(scene, true);
        shielded.Movement.SetPhysicsProcess(false); untouched.Movement.SetPhysicsProcess(false);
        untouched.GlobalPosition += new Vector2(2000, 0);
        BattleTower battle = GD.Load<PackedScene>("res://TOWERS/Offensive/BattleTower/BattleTower.tscn").Instantiate<BattleTower>();
        battle.Config = battle.Config.CreateInstanceCopy(); battle.Config.TargetPriority = TowerTargeting.Priority.Closest;
        main.GetNode("World/Towers").AddChild(battle); battle.SetPhysicsProcess(false);
        battle.GlobalPosition = shielded.GlobalPosition + new Vector2(0, 64);
        battle._PhysicsProcess(1);
        Node bullets = main.GetNode("World/Projectiles");
        Require(bullets.GetChildCount() == 1, "Production Battle fires one guided projectile at the shielded target");
        battle.Config.ShieldPenetration = 0.75f; // A shot already in flight retains its original final payload.
        bullets.GetChild<BasicProjectile>(0)._PhysicsProcess(1);
        Require(Mathf.IsEqualApprox(shielded.CurrentShield, 94.3f) && Mathf.IsEqualApprox(shielded.Health, 59.4f)
            && shielded.GetNode<DefenseBar>("DefenseBar").Visible && untouched.Health == 60 && untouched.CurrentShield == 0,
            "Real Battle projectile snapshots5% penetration, hull feedback and target isolation");
        battle.Free();
        int killBefore = wallet.GetBalance(wallet.CreditsResource);
        shielded.ApplyDamage(new(1000, DamageType.Kinetic, shieldPenetration: 1));
        shielded.ApplyDamage(new(1000, DamageType.Kinetic, shieldPenetration: 1)); shielded.Arrive();
        Require(wallet.GetBalance(wallet.CreditsResource) == killBefore + 3, "Penetrating hull death pays once even with shield remaining");
        untouched.Arrive();
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
    // Overrides and bypass use the same state comparison as inherited default fixtures.
    private static void CheckHit(DamageInfo hit, float shield, float health, float expectedShield, float expectedHealth)
    {
        DamageResult result = DamageResolver.Resolve(hit, shield, health);
        Require(Mathf.Abs(result.Shield - expectedShield) < 0.0001f && Mathf.Abs(result.Health - expectedHealth) < 0.0001f,
            $"Defense override/penetration mismatch: {hit} => {result}");
    }
    // =========================================================
    // Malformed values fail before applying a hit.
    private static void Invalid(Action action)
    { try { action(); } catch (ArgumentException) { return; } catch (InvalidOperationException) { return; } throw new InvalidOperationException("Invalid damage accepted."); }
    // =========================================================
    // Fail this checkpoint immediately on a response regression.
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
