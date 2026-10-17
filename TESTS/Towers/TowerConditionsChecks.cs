// HD1/HD2 actual-world coverage: generic disable sources, all weapons, optional heat and lifecycle clocks.
using Godot;
using System;
using System.Diagnostics;
using static AmmoLoopFixture;

public partial class TowerConditionsChecks : Node
{
    // =========================================================
    // Existing scenes/configs/services; explicit clocks isolate pause and exact boundaries from renderer timing.
    public override async void _Ready()
    {
        try
        {
            await Run(this);
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: HD1/HD2 independent timed/conditional disabling, seven weapons, heat thresholds/cooling, beam30/60/120, pause, upgrades/ranks, lifecycle and bounded work");
            GetTree().Quit();
        }
        catch (Exception e) { GD.PushError(e.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
    // =========================================================
    // The full integration runner can execute this same focused production-world group.
    internal static async System.Threading.Tasks.Task Run(Node owner)
    {
        using (AmmoLoopFixture f = new(owner))
        {
            SubsystemBattlefield.DetachSession(f.Main);
            Sources(f); Weapons(f); BeamRates(f); CoolingRates(f); Progression(f); await Automatic(owner, f); Bench(f);
        }
    }
    // =========================================================
    // Handles isolate applications/towers; same nonzero source refreshes, separate sources overlap safely.
    private static void Sources(AmmoLoopFixture f)
    {
        TowerBase a = f.Add<TowerBase>("battle", new(2000,2064)), b = f.Add<TowerBase>("battle", new(2400,2064));
        Require(a.GetNodeOrNull("CombatConditions") == null && a.CanAttack, "Heatless unaffected towers have no extra clock");
        int changes = 0; a.DisableStateChanged += () => changes++;
        var first = a.ApplyDisable(TowerDisableReason.EMP, 2, 10);
        var second = a.ApplyDisable(TowerDisableReason.EMP, 3, 11);
        Require(a.IsDisabled && a.IsOperational && !a.CanAttack && a.DisableCount == 2 && changes == 2, "Construction meaning independent of generic disabling");
        Require(!b.RemoveDisable(first), "Foreign handles cannot clear another tower");
        Require(a.ApplyDisable(TowerDisableReason.EMP, 4, 10) == first && a.DisableCount == 2, "Same source refreshes instead of duplicating");
        a.AdvanceConditions(3); Require(a.IsDisabled && a.DisableCount == 1 && !a.RemoveDisable(second), "Only elapsed application expires");
        f.Main.GetTree().Paused = true; a.AdvanceConditions(100); Require(a.IsDisabled, "Paused timers freeze"); f.Main.GetTree().Paused = false;
        a.AdvanceConditions(1); Require(!a.IsDisabled && a.CanAttack, "Final timed application releases gate exactly at boundary");
        first = a.ApplyDisable(TowerDisableReason.NoPower, sourceId: 20);
        second = a.ApplyDisable(TowerDisableReason.EMP, sourceId: 21);
        a.RemoveDisablesFromSource(20); Require(a.IsDisabled && a.GetDisableReason(0) == TowerDisableReason.EMP, "Sustained source cleanup is independent");
        Require(a.RemoveDisable(second) && !a.RemoveDisable(second) && !a.RemoveDisable(first), "Expired/removal handles are idempotent");
        foreach (double bad in new[] { 0d, -1, double.NaN, double.PositiveInfinity })
        { bool rejected = false; try { a.ApplyDisable(TowerDisableReason.EMP, bad); } catch (ArgumentOutOfRangeException) { rejected = true; } Require(rejected, "Invalid duration rejected"); }
        first = a.ApplyDisable(TowerDisableReason.Other); a.AdvanceConditions(double.NaN); a.AdvanceConditions(-1);
        Require(a.IsDisabled && !a.GetNode<TowerConditions>("CombatConditions").IsPhysicsProcessing(), "Indefinite-only state needs no ticking");
        a.RemoveDisable(first); a.Free(); b.Free();
    }
    // =========================================================
    // Optional heat is configured through the selected complete row, not modifiers or shared resource mutations.
    private static TowerBase Heated(AmmoLoopFixture f, string id, HeatStats stats) =>
        f.Add<TowerBase>(id, new(2000,2128), config => config.SetLevelStats(1, config.Levels[0].Stats with { Heat = stats }));
    // =========================================================
    // Commit threshold attack once, block subsequent launches/beam/aim, retain launched attacks and other causes.
    private static void Weapons(AmmoLoopFixture f)
    {
        foreach (string id in new[] { "battle", "machine_gun", "sniper", "cryo", "blaster_cannon", "shockwave", "laser" })
        {
            TowerBase tower = Heated(f, id, new(1, 1, 2, 1, .35f)); tower.SetTargetPriority(TowerTargeting.Priority.Closest);
            Enemy target = f.Target(new(2000,2000));
            var emp = tower.ApplyDisable(TowerDisableReason.EMP, sourceId: 42);
            int ammo = tower.Ammunition.Inventory.Amount; float hp = target.Health;
            tower.Call("_PhysicsProcess", .5);
            Require(tower.CurrentHeat == 0 && tower.Ammunition.Inventory.Amount == ammo && target.Health == hp, "Disabled weapon does not attack/charge " + id);
            tower.RemoveDisable(emp); tower.Call("_PhysicsProcess", .5);
            Require(tower.IsOverheated && tower.IsDisabled && Math.Abs(tower.CurrentHeat - 1) < .001f, "Committed attack trips heat on " + id);
            if (id == "battle")
            {
                var shot = f.Main.GetNode("World/Projectiles").GetChild<BasicProjectile>(0); Disable(shot); shot._PhysicsProcess(1);
                Require(target.Health < hp, "Already launched bullet hits while source remains disabled");
            }
            int stock = tower.Ammunition.Inventory.Amount;
            TowerFinalStats cache = tower.FinalStats; tower.Call("_PhysicsProcess", 10);
            Require(tower.Ammunition.Inventory.Amount == stock && ReferenceEquals(cache, tower.FinalStats), "Overheat prevents spending/rebuild " + id);
            if (id == "laser") Require(!tower.GetNode<BeamRenderer>("BeamRenderer").IsActive, "Threshold beam switches off immediately");
            if (id == "sniper") Require(!tower.GetNode<WeaponVisualRenderer>("TurretPivot/WeaponVisuals").AimVisible, "Disabled aiming clears immediately");
            emp = tower.ApplyDisable(TowerDisableReason.EMP, sourceId: 43);
            tower.AdvanceConditions(.7); Require(!tower.IsOverheated && tower.IsDisabled && tower.DisableCount == 1, "Cooling removes only overheat under continuing EMP " + id);
            tower.RemoveDisable(emp); Require(tower.CanAttack, "All causes must clear " + id);
            f.Main.GetTree().Paused = true; float heat = tower.CurrentHeat; tower.AdvanceConditions(1);
            Require(tower.CurrentHeat == heat && !tower.CanAttack, "Pause freezes heat/gate " + id); f.Main.GetTree().Paused = false;
            tower.AdvanceConditions(100); Require(tower.CurrentHeat == 0 && !tower.GetNode<TowerConditions>("CombatConditions").IsPhysicsProcessing(), "Cold clock sleeps " + id);
            tower.Free(); target.Free(); f.ClearProjectiles();
        }
        // Two-barrel committed volley is paid entirely and charges both bullets before subsequent shutdown.
        var gun = (MachineGunTower)Heated(f, "machine_gun", new(1, .6f, 0, 0, .35f));
        gun.Config.FiringMode = BarrelFireMode.Simultaneous; gun.SetTargetPriority(TowerTargeting.Priority.Closest);
        Enemy enemy = f.Target(new(2000,2000)); gun._PhysicsProcess(.5);
        Require(gun.IsOverheated && gun.Ammunition.ConsumedRounds == 2 && f.Main.GetNode("World/Projectiles").GetChildCount() == 2, "Volley threshold commits all funded bullets once");
        gun.Free(); enemy.Free(); f.ClearProjectiles();
        TowerBase empty = Heated(f, "battle", new(10, 1, 0, 1, .35f)); enemy = f.Target(new(2000,2000)); empty.SetTargetPriority(TowerTargeting.Priority.Closest);
        empty.Ammunition.Inventory.TryConsume(empty.Ammunition.Inventory.Amount); empty.Call("_PhysicsProcess", 1);
        Require(empty.CurrentHeat == 0, "No heat from unfunded launch"); empty.Free(); enemy.Free();
    }
    // =========================================================
    // Fund only the beam time before capacity; no extra ammo/damage after the exact thermal boundary.
    private static void BeamRates(AmmoLoopFixture f)
    {
        foreach (int rate in new[] { 30, 60, 120 })
        {
            var laser = (LaserTower)Heated(f, "laser", new(1, 0, 2, 0, .35f)); laser.SetTargetPriority(TowerTargeting.Priority.Closest);
            Enemy enemy = f.Target(new(2000,2000));
            enemy.Definition.MaxHealth = 60; enemy.Initialize(f.Spawns.Registry, f.Spawns.Paths); Disable(enemy);
            float before = enemy.Health;
            for (int i = 0; i < rate; i++) laser._PhysicsProcess(1d / rate);
            Require(laser.IsOverheated && Math.Abs(laser.CurrentHeat - 1) < .001f && Math.Abs((before - enemy.Health) - .9f) < .02f,
                "Half-second damaging beam reaches capacity consistently " + rate + $" heat={laser.CurrentHeat}, overheated={laser.IsOverheated}, damage={before - enemy.Health}");
            laser.Free(); enemy.Free();
        }
    }
    // =========================================================
    // Hysteresis and elapsed cooling have matching results at ordinary physics frequencies.
    private static void CoolingRates(AmmoLoopFixture f)
    {
        foreach (int rate in new[] { 30, 60, 120 })
        {
            TowerBase tower = Heated(f, "battle", new(1, 1, 0, .5f, .35f)); tower.SetTargetPriority(TowerTargeting.Priority.Closest);
            Enemy enemy = f.Target(new(2000,2000)); tower.Call("_PhysicsProcess", 1);
            for (int i = 0; i < rate; i++) tower.AdvanceConditions(1d / rate);
            Require(tower.IsOverheated && Math.Abs(tower.CurrentHeat - .5f) < .0001f, "Hysteresis remains active above restart threshold " + rate);
            for (int i = 0; i < rate; i++) tower.AdvanceConditions(1d / rate);
            Require(!tower.IsDisabled && tower.CurrentHeat < .0001f, "Cooling reaches neutral at all rates " + rate);
            tower.Free(); enemy.Free(); f.ClearProjectiles();
        }
        var laser = (LaserTower)Heated(f, "laser", new(100, 0, 2, 0, .35f)); laser.SetTargetPriority(TowerTargeting.Priority.Closest);
        Enemy target = f.Target(new(2000,2000)); laser._PhysicsProcess(.25);
        Require(laser.GetNode<BeamRenderer>("BeamRenderer").IsActive, "Beam active before external EMP");
        laser.ApplyDisable(TowerDisableReason.EMP, 3);
        Require(!laser.GetNode<BeamRenderer>("BeamRenderer").IsActive, "External disable clears live beam synchronously");
        laser.Free(); target.Free();
        laser = (LaserTower)Heated(f, "laser", new(100, 0, 2, 0, .35f)); laser.SetTargetPriority(TowerTargeting.Priority.Closest);
        laser.Config.OverrideHullCoefficient = true; laser.Config.HullCoefficient = 0;
        target = f.Target(new(2000,2000)); laser._PhysicsProcess(.25);
        Require(laser.CurrentHeat == 0, "Zero-effective-damage beam time generates no heat");
        laser.Free(); target.Free();
    }
    // =========================================================
    // Upgrades retain heat and XP; independent selected rows/capacity changes reconcile only their own latch.
    private static void Progression(AmmoLoopFixture f)
    {
        TowerBase tower = Heated(f, "battle", new(10, 5, 0, 0, .35f)); tower.SetTargetPriority(TowerTargeting.Priority.Closest);
        Enemy enemy = f.Target(new(2000,2000)); tower.Call("_PhysicsProcess", 1); float heat = tower.CurrentHeat;
        TowerRankController.AwardExperience(tower, 1000); Require(tower.CurrentHeat == heat, "Promotion retains absolute heat");
        TowerStatBlock level2 = tower.AuthoredConfig.Levels[1].Stats;
        tower.AuthoredConfig.SetLevelStats(2, level2 with { Heat = new(20, 6, 0, 1, .25f) });
        Require(tower.ResolveLevel(2).Heat.Value.Capacity == 20 && tower.FinalStats.Heat.Value.Capacity == 10, "Independent rows/previews retain heat capability");
        f.Wallet.Initialize(new LevelConfig { StartingCredits = 1000 });
        Require(TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 1, out string error), error);
        ConstructionTestClock.FinishWork(f.Main);
        Require(tower.UpgradeLevel == 2 && tower.CurrentHeat == heat && tower.RankProgress.TotalXp == 1000, "Purchased tier keeps heat/rank/XP");
        var emp = tower.ApplyDisable(TowerDisableReason.EMP);
        tower.AuthoredConfig.SetLevelStats(2, level2 with { Heat = new(2, 1, 0, 0, .35f) });
        Require(tower.CurrentHeat == 2 && tower.IsOverheated && tower.DisableCount == 2, "Smaller capacity clamps/trips without clearing EMP");
        tower.AuthoredConfig.SetLevelStats(2, level2 with { Heat = null }); Require(tower.CurrentHeat == 0 && !tower.IsOverheated && tower.IsDisabled, "Removing heat capability preserves EMP");
        tower.RemoveDisable(emp); tower.Free(); enemy.Free(); f.ClearProjectiles();
        Require(HeatStats.Sanitize(new(float.NaN, 1, 1, 1, .35f)) == null && HeatStats.Sanitize(new(0, 1, 1, 1, .35f)) == null, "Invalid/zero capacity neutral");
        var clean = HeatStats.Sanitize(new(10, -1, float.NaN, float.PositiveInfinity, 2)).Value;
        Require(clean.PerShot == 0 && clean.PerBeamSecond == 0 && clean.CoolingPerSecond == 0 && clean.RestartBelowFraction == .99f, "Invalid rates/threshold sanitized");
    }
    // =========================================================
    // Actual Godot clocks expire while weapon physics is stopped; pulse lifetime does not retain source nodes.
    private static async System.Threading.Tasks.Task Automatic(Node owner, AmmoLoopFixture f)
    {
        TowerBase tower = f.Add<TowerBase>("battle", new(2000,2128)); tower.ApplyDisable(TowerDisableReason.EMP, .04);
        for (int i = 0; i < 30 && tower.IsDisabled; i++) await owner.ToSignal(owner.GetTree(), SceneTree.SignalName.PhysicsFrame);
        Require(!tower.IsDisabled && !tower.GetNode<TowerConditions>("CombatConditions").IsPhysicsProcessing(), "Real lazy timer runs independently of weapon physics");
        tower.ApplyDisable(TowerDisableReason.EMP, 3); tower.DisableStateChanged += () => { if (!tower.IsDisabled) tower.QueueFree(); };
        tower.AdvanceConditions(3); Require(tower.IsQueuedForDeletion(), "Expiry callbacks may remove owner safely");
        await owner.ToSignal(owner.GetTree(), SceneTree.SignalName.ProcessFrame);
    }
    // =========================================================
    // Warmed heat/timer ticks allocate no managed bytes and never resolve FinalStats.
    private static void Bench(AmmoLoopFixture f)
    {
        TowerBase tower = Heated(f, "battle", new(100, 1, 0, 0, .35f)); tower.ApplyDisable(TowerDisableReason.EMP, 100000);
        for (int i = 0; i < 100; i++) tower.AdvanceConditions(.01);
        TowerFinalStats cached = tower.FinalStats; long before = GC.GetAllocatedBytesForCurrentThread(); Stopwatch sw = Stopwatch.StartNew();
        // Stopwatch allocation is intentionally outside the measured delta.
        before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100000; i++) tower.AdvanceConditions(.001);
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before; sw.Stop();
        Require(bytes == 0 && ReferenceEquals(cached, tower.FinalStats), "Clock ticks allocate nothing and preserve stat cache");
        for (ulong source = 1; source <= 500; source++) tower.ApplyDisable(TowerDisableReason.EMP, 100000, source);
        Require(tower.DisableCount == 501, "500 independent attacker sources coexist without scene searches");
        tower.AdvanceConditions(.001); before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) tower.AdvanceConditions(.001);
        Require(GC.GetAllocatedBytesForCurrentThread() == before, "500-source expiry ticks allocate no managed memory");
        GD.Print($"HD BENCH: 100000 single-active-tower timer ticks {sw.Elapsed.TotalMilliseconds:0.###}ms, {bytes} managed bytes; not FPS"); tower.Free();
    }
}
