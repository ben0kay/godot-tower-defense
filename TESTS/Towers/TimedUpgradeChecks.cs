// C3 transactions and selected-only work UI through the production wallet, manager, weapons and rank pipeline.
using Godot;
using System;
using System.Threading.Tasks;
using static AmmoLoopFixture;

internal static class TimedUpgradeChecks
{
    // =========================================================
    // Keep scene fixtures private and deliberately observe unfinished work rather than using the legacy finish helper.
    internal static async Task Run(Node runner)
    {
        using (AmmoLoopFixture f = new(runner)) await Progression(runner, f);
        Safety(runner); Deliveries(runner); ResultsPause(runner); await Automatic(runner);
        GD.Print("PASS: C3 paid1→2→3 timing, old-tier operation, latest rank/ammo, selected progress/close/reselect, paused/overlapping work and zero/invalid/stale/free guards");
    }

    // =========================================================
    // A paid target remains fixed while the old tier fires; latest rank is resolved only at completion.
    private static async Task Progression(Node runner, AmmoLoopFixture f)
    {
        var manager = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        BattleTower tower = f.Add<BattleTower>("battle", new(2200, 2000));
        tower.Config.SetLevelStats(2, tower.Config.Levels[1].Stats with { Ammunition = new(1200, 1) });
        tower.Ammunition.Inventory.TryConsume(100);
        Enemy enemy = f.Target(tower.GlobalPosition + new Vector2(0, -64));
        f.Placement.Call("SelectTower", tower);
        var body = f.Hud.GetNode("BottomArea/Inspector/Insets/Body");
        Label status = body.GetNode<Label>("WorkSection/Status"), time = body.GetNode<Label>("WorkSection/Time");
        ProgressBar bar = body.GetNode<ProgressBar>("WorkSection/Progress");
        Button button = body.GetNode<Button>("UpgradeButton"); Label preview = body.GetNode<Label>("UpgradeInfo");
        int started = 0, completed = 0, changed = 0;
        tower.WorkStarted += () => { started++; Require(f.Wallet.GetBalance(f.Wallet.CreditsResource) == (started == 1 ? 850 : 600), "WorkStarted publishes after payment"); };
        tower.WorkCompleted += () => completed++; tower.StatsChanged += () => changed++;
        TowerFinalStats old = tower.FinalStats; double xp = tower.RankProgress.TotalXp;
        button.EmitSignal(Button.SignalName.Pressed);
        Require(tower.UpgradeLevel == 1 && tower.FinalStats == old && tower.RankProgress.TotalXp == xp && tower.WorkPhase == TowerWorkPhase.Upgrading
            && tower.IsOperational && tower.IsBusy && tower.CaptureWorkState().PendingUpgradeLevel == 2 && tower.PendingUpgradeLevel == 2 && tower.WorkSeconds == 4 && started == 1 && changed == 0
            && tower.Ammunition.Inventory.Amount == 900 && tower.Ammunition.Inventory.Capacity == 1000, "Purchase charges once but preserves old tier/cached stats/XP/ammo");
        Require(f.Wallet.GetBalance(f.Wallet.CreditsResource) == 850 && button.Disabled && status.Text.Contains("UPGRADING") && status.Text.Contains("Level 2")
            && preview.Text.Contains("Cost already paid") && preview.Text.Contains("Current Level 1"), "Inspector distinguishes paid work from another purchase");
        Require(!TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 1, out _) && f.Wallet.GetBalance(f.Wallet.CreditsResource) == 850, "Repeat purchase while busy cannot spend again");
        manager.Advance(1);
        Require(bar.Value == 25 && time.Text.Contains("3.0s remaining") && tower.FinalStats == old && changed == 0, "Progress updates only work controls/cache remains untouched");
        runner.GetTree().Paused = true; manager.Advance(100); runner.GetTree().Paused = false;
        Require(tower.ElapsedWorkSeconds == 1 && tower.UpgradeLevel == 1, "Upgrade clock freezes during pause");
        for (int i = 0; i < 60; i++) tower._PhysicsProcess(1.0 / 60);
        Require(tower.Ammunition.Inventory.Amount < 900 && tower.UpgradeLevel == 1, "Old tier continues firing and consuming stock during upgrade");
        TowerRankController.AwardExperience(tower, 1000);
        Require(tower.CurrentRank.Rank == TowerRank.Veteran && tower.UpgradeLevel == 1 && tower.FinalStats.DamagePerShot == 31, "Promotion applies to old tier without ending work");
        BattleTower other = f.Add<BattleTower>("battle", new(2500, 2000)); f.Placement.Call("SelectTower", other);
        manager.Advance(1); Require(status.Text == "OPERATIONAL" && !bar.Visible, "Unselected progress cannot overwrite another building");
        f.Placement.Call("SelectTower", tower); Require(bar.Value == 50, "Reselection reads current progress immediately");
        f.Hud.ClearInspection(); double hidden = bar.Value; manager.Advance(1);
        Require(bar.Value == hidden, "Closing Inspector stops hidden work updates");
        f.Placement.Call("SelectTower", tower); Require(bar.Value == 75, "Reopening reads work state without polling");
        manager.Advance(.9); Require(tower.UpgradeLevel == 1 && tower.PendingUpgradeLevel == 2, "No early purchased tier");
        int stock = tower.Ammunition.Inventory.Amount, inventoryChanges = 0;
        tower.Ammunition.Inventory.Changed += () => inventoryChanges++;
        manager.Advance(.1);
        Require(tower.UpgradeLevel == 2 && tower.FinalStats == tower.ResolveLevel(2) && tower.FinalStats.DamagePerShot == 34
            && tower.RankProgress.TotalXp == 1000 && tower.Ammunition.Inventory.Amount == stock && tower.Ammunition.Inventory.Capacity == 1200
            && completed == 1 && changed == 2 && inventoryChanges == 1 && tower.PendingUpgradeLevel == 0, "Completion resolves current rank, updates capacity once and never refills ammunition");
        Require(status.Text == "OPERATIONAL" && !bar.Visible && !button.Disabled && preview.Text.Contains("LEVEL 3") && f.Wallet.GetBalance(f.Wallet.CreditsResource) == 850,
            "Completion updates actual Inspector stats/next preview with no additional payment");
        manager.Advance(99); Require(completed == 1 && changed == 2, "Repeated clock ticks cannot reapply a completed tier");
        Require(!TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 1, out _), "Stale expected level rejects without purchase");
        await runner.ToSignal(runner.GetTree(), SceneTree.SignalName.ProcessFrame);
        // A second job uses its own duration and overlaps a real unfinished Factory.
        TowerFinalStats level2 = tower.FinalStats;
        Require(TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 2, out _) && tower.WorkSeconds == 5 && tower.FinalStats == level2,
            "Second upgrade schedules full target3 without immediate stats");
        AmmunitionFactory factory = f.Purchase<AmmunitionFactory>("ammo_factory", false);
        Require(manager.ActiveCount == 2 && f.Wallet.GetBalance(f.Wallet.CreditsResource) == 400, "Independent build and upgrade overlap after exact150/250/200 payments");
        manager.Advance(5);
        Require(status.Text == "CONSTRUCTING" && bar.Value == 62.5, "Same work section displays partially completed factory construction");
        Require(tower.UpgradeLevel == 3 && tower.CurrentRank.Rank == TowerRank.Veteran && factory.IsBusy && factory.ElapsedWorkSeconds == 5, "Five-second upgrade does not finish eight-second construction");
        manager.Advance(3);
        Require(factory.IsOperational && manager.ActiveCount == 0 && !TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 3, out _), "Maximum tier and all finished work remain safe");
    }

    // =========================================================
    // Invalid, unaffordable, deleted and missing-manager requests cannot partially schedule/deduct; zero time stays atomic.
    private static void Safety(Node runner)
    {
        using AmmoLoopFixture f = new(runner);
        BattleTower tower = f.Add<BattleTower>("battle", new(2200, 2000));
        var manager = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        foreach (double invalid in new[] { -1.0, double.NaN, double.PositiveInfinity })
        {
            tower.NextUpgrade.WorkSeconds = invalid;
            Require(!TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 1, out _) && manager.ActiveCount == 0
                && tower.UpgradeLevel == 1 && f.Wallet.GetBalance(f.Wallet.CreditsResource) == 1000, "Invalid duration does not schedule or pay");
        }
        tower.NextUpgrade.WorkSeconds = 0; int stats = 0;
        tower.StatsChanged += () => { stats++; Require(f.Wallet.GetBalance(f.Wallet.CreditsResource) == 850, "Instant stats publish after payment"); };
        Require(TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 1, out _) && tower.UpgradeLevel == 2 && !tower.IsBusy && stats == 1
            && manager.ActiveCount == 0, "Zero-duration upgrade commits synchronously without a job");
        BattleTower removed = f.Add<BattleTower>("battle", new(2400, 2000)); int completed = 0;
        Require(TowerUpgradeController.TryUpgrade(removed, f.Wallet, f.Placement.Settings, 1, out _), "Removal fixture starts real paid job");
        removed.WorkCompleted += () => completed++; removed.Free(); manager.Advance(100);
        Require(manager.ActiveCount == 0 && completed == 0 && f.Wallet.GetBalance(f.Wallet.CreditsResource) == 700, "Free retires work, no stale completion or refund mechanic");
        BattleTower queued = f.Add<BattleTower>("battle", new(2600, 2000)); queued.QueueFree();
        Require(!TowerUpgradeController.TryUpgrade(queued, f.Wallet, f.Placement.Settings, 1, out _), "Queued-for-deletion purchase rejected");
        manager.Free(); int before = f.Wallet.GetBalance(f.Wallet.CreditsResource);
        Require(!TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 2, out _) && !tower.IsBusy && tower.UpgradeLevel == 2
            && f.Wallet.GetBalance(f.Wallet.CreditsResource) == before, "Missing work manager cannot spend or leave partial work state");
    }

    // =========================================================
    // Existing logistics must keep serving an upgrading operational consumer, without duplicate registration.
    private static void Deliveries(Node runner)
    {
        using AmmoLoopFixture f = new(runner);
        BattleTower tower = f.Add<BattleTower>("battle", new(2200, 2000), c => c.InitialAmmoFraction = 0);
        AmmunitionDepot depot = f.Add<AmmunitionDepot>("ammo_depot", new(2100, 2000)); depot.Inventory.TryAdd(1000);
        Require(TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 1, out _), "Supply fixture begins upgrading");
        f.Dispatch(); Require(f.Logistics.Jobs.Count == 1 && tower.IsBusy, "Upgrading consumer still receives normal drone supply");
        var job = f.Logistics.Jobs[0]; f.Arrive(job); f.Return(job);
        Require(tower.Ammunition.Inventory.Amount == 250 && tower.UpgradeLevel == 1, "Shipment unloads normally while old tier operates");
    }

    // =========================================================
    // Both real launchers finish a positive timed upgrade through Godot physics, without explicit clock advances.
    private static async Task Automatic(Node runner)
    {
        foreach (string scene in new[] { "res://Main.tscn", "res://MainLevel002.tscn" })
        using (AmmoLoopFixture f = new(runner, scene))
        {
            BattleTower tower = f.Add<BattleTower>("battle", new(2200, 2000)); tower.NextUpgrade.WorkSeconds = .05;
            int before = f.Wallet.GetBalance(f.Wallet.CreditsResource);
            Require(TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 1, out _) && tower.UpgradeLevel == 1, "Automatic positive job does not commit synchronously");
            await runner.ToSignal(runner.GetTree().CreateTimer(.2), SceneTreeTimer.SignalName.Timeout);
            Require(tower.UpgradeLevel == 2 && !tower.IsBusy && f.Wallet.GetBalance(f.Wallet.CreditsResource) == before - 150, "Automatic Godot physics completes target once on both launchers");
        }
    }

    // =========================================================
    // Real defeat/result pause must also freeze an upgrade without shutting off its old-tier state prematurely.
    private static void ResultsPause(Node runner)
    {
        using AmmoLoopFixture f = new(runner);
        BattleTower tower = f.Add<BattleTower>("battle", new(2200, 2000));
        Require(TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 1, out _), "Results fixture starts upgrade");
        var clock = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork"); clock.Advance(1);
        CitadelHealth health = f.Main.GetNode<CitadelSession>("CitadelSession").Health;
        health.TakeDamage(health.CurrentHealth); clock.Advance(100);
        Require(runner.GetTree().Paused && tower.UpgradeLevel == 1 && tower.ElapsedWorkSeconds == 1 && tower.WorkPhase == TowerWorkPhase.Upgrading, "Results pause freezes paid upgrade");
        runner.GetTree().Paused = false;
    }
}
