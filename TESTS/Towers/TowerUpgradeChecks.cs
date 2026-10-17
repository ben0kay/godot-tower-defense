// Paid tier progression, preview parity, instance isolation and actual Inspector actions.
using Godot;
using System;
using System.Linq;

public static class TowerUpgradeChecks
{
    // =========================================================
    // Run on production scenes/resources without mutating saved tower definitions.
    public static async System.Threading.Tasks.Task Run(Node battle)
    {
        EconomyManager wallet = battle.GetNode<EconomyManager>("Economy");
        TowerPlacementController placement = battle.GetNode<TowerPlacementController>("TowerPlacement");
        GameplayHUD hud = battle.GetNode<GameplayHUD>("UI/GameplayHUD");
        PackedScene scene = GD.Load<PackedScene>("res://TOWERS/Offensive/BattleTower/BattleTower.tscn");
        BattleTower first = scene.Instantiate<BattleTower>(), second = scene.Instantiate<BattleTower>();
        BattleTowerConfig shared = first.Config;
        first.Config = shared.CreateInstanceCopy(); second.Config = shared.CreateInstanceCopy();
        Node towers = battle.GetNode("World/Towers");
        towers.AddChild(first); towers.AddChild(second); first.SetPhysicsProcess(false); second.SetPhysicsProcess(false);
        var grid = battle.GetNode<WorldGrid>("World/Grid");
        int revision = grid.NavigationRevision;
        bool allow = placement.Settings.AllowUpgrades;
        try
        {
            Require(first.UpgradeLevel == 1 && first.NextUpgrade.Level == 2, "Placed towers begin at level 1");
            Require(!ReferenceEquals(first.Levels[0], shared.Levels[0]), "Tier Resources are isolated per instance");
            TowerFinalStats initial = first.FinalStats;
            Label stats = hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/TowerStats");
            for (int level = 1; level <= 3; level++)
                Require(first.ResolveLevel(level).ShieldPenetration == 0.05f, "Purchased tiers never increase base penetration");
            TowerFinalStats preview = first.ResolveLevel(2);
            VerifyTypedComparison();
            Require(preview.DamagePerShot == 33 && Mathf.IsEqualApprox(preview.ShotsPerSecond, 1.47f)
                && preview.RangePixels == 264 && first.FinalStats == initial && first.UpgradeLevel == 1,
                "Next-tier preview selects a full snapshot without changing live stats or progression");
            wallet.Initialize(new LevelConfig { StartingCredits = 149 });
            Require(!ConstructionTestClock.PurchaseAndFinish(first, wallet, placement.Settings, 1, out _) &&
                first.UpgradeLevel == 1 && wallet.GetBalance(wallet.CreditsResource) == 149, "Unaffordable upgrade never changes level or funds");
            wallet.Initialize(new LevelConfig { StartingCredits = 400 });
            placement.Settings.AllowUpgrades = false;
            Require(!ConstructionTestClock.PurchaseAndFinish(first, wallet, placement.Settings, 1, out _) &&
                wallet.GetBalance(wallet.CreditsResource) == 400, "Global upgrade rule prevents payment");
            placement.Settings.AllowUpgrades = true;
            placement.Call("SelectTower", first);
            Require(stats.Text.Contains("Shield Penetration: 5%"), "Placed level1 Inspector shows penetration");
            hud.SetTowerBarOpen(true, true);
            await battle.ToSignal(battle.GetTree(), SceneTree.SignalName.ProcessFrame);
            await battle.ToSignal(battle.GetTree(), SceneTree.SignalName.ProcessFrame);
            Control panel = hud.GetNode<Control>("BottomArea/Inspector");
            Control tray = hud.GetNode<Control>("BottomArea/TraySlot/TowerBar");
            Require(panel.GetGlobalRect().End.Y <= hud.GetGlobalRect().End.Y + 1 &&
                Mathf.IsEqualApprox(tray.Size.Y, hud.Config.BottomBarHeight), "Expanded Inspector stays onscreen while tray retains configured height");
            hud.SetTowerBarOpen(false, true);
            Require(panel.Visible && hud.Config.InspectorWidth == 540 && hud.Config.InspectorHeight == 325,
                "Closing build tray leaves the 540×325 Inspector open");
            Button button = hud.GetNode<Button>("BottomArea/Inspector/Insets/Body/UpgradeButton");
            Label info = hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/UpgradeInfo");
            Require(button.Visible && !button.Disabled && info.Text.Contains("150 Credits") &&
                UpgradeTableChecks.HasRow(hud, "Damage", "30", "33", "(+3)"), "Inspector shows next tier, typed gain and affordable price");
            hud.EmitSignal(GameplayHUD.SignalName.TowerUpgradeRequested); ConstructionTestClock.FinishWork(battle);
            Require(first.UpgradeLevel == 2 && first.FinalStats == preview &&
                wallet.GetBalance(wallet.CreditsResource) == 250, "Actual HUD upgrade action pays once and matches preview");
            Require(stats.Text.Contains("Shield Penetration: 5%") && first.FinalStats.ShieldPenetration == 0.05f,
                "Level2 Inspector retains baseline penetration");
            Require(second.UpgradeLevel == 1 && second.FinalStats == initial && shared.DamagePerShot == 30,
                "Upgrade leaves other towers and authored base unchanged");
            Require(!ConstructionTestClock.PurchaseAndFinish(first, wallet, placement.Settings, 1, out _) &&
                wallet.GetBalance(wallet.CreditsResource) == 250, "Stale request cannot buy an already purchased tier");
            hud.EmitSignal(GameplayHUD.SignalName.TowerUpgradeRequested); ConstructionTestClock.FinishWork(battle);
            Require(first.UpgradeLevel == 2 && wallet.GetBalance(wallet.CreditsResource) == 250,
                "Repeated HUD requests in one frame cannot buy multiple tiers");
            await battle.ToSignal(battle.GetTree(), SceneTree.SignalName.ProcessFrame);
            TowerFinalStats levelThree = first.ResolveLevel(3);
            hud.EmitSignal(GameplayHUD.SignalName.TowerUpgradeRequested); ConstructionTestClock.FinishWork(battle);
            Require(first.UpgradeLevel == 3 && first.FinalStats == levelThree &&
                first.FinalStats.DamagePerShot == 36 && Mathf.IsEqualApprox(first.FinalStats.ShotsPerSecond, 1.54f) &&
                wallet.GetBalance(wallet.CreditsResource) == 0, "Second purchase selects the complete level3 snapshot");
            Require(stats.Text.Contains("Shield Penetration: 5%") && first.FinalStats.ShieldPenetration == 0.05f,
                "Level3 Inspector retains baseline penetration");
            Require(!button.Visible && info.Text.Contains("MAX LEVEL") && !hud.GetNode<Control>("BottomArea/Inspector/Insets/Body/UpgradeComparison").Visible,
                "Maximum tier removes upgrade action and hypothetical comparison");
            Require(!ConstructionTestClock.PurchaseAndFinish(first, wallet, placement.Settings, 3, out _) &&
                wallet.GetBalance(wallet.CreditsResource) == 0, "Maximum tier cannot spend again");
            first.RebuildStats(); Require(first.FinalStats == levelThree, "Repeated rebuild does not compound bonuses");

            ResourceDefinition alloy = new() { ResourceId = "alloy", DisplayName = "Alloy" };
            second.NextUpgrade.Costs.Add(new ResourceCost { Resource = alloy, Amount = 2 });
            wallet.Initialize(new LevelConfig { StartingCredits = 200 }); wallet.Grant(alloy, 1);
            Require(!ConstructionTestClock.PurchaseAndFinish(second, wallet, placement.Settings, 1, out _) &&
                wallet.GetBalance(wallet.CreditsResource) == 200 && wallet.GetBalance(alloy) == 1, "Missing secondary resource causes no partial payment");
            wallet.Grant(alloy, 1);
            Require(ConstructionTestClock.PurchaseAndFinish(second, wallet, placement.Settings, 1, out _) &&
                wallet.GetBalance(wallet.CreditsResource) == 50 && wallet.GetBalance(alloy) == 0, "Multi-resource upgrades pay atomically");
            Require(shared.Levels[1].Costs.Count == 1, "Runtime recipe edits do not mutate saved tier defaults");
            Require(grid.NavigationRevision == revision, "Upgrading never changes occupancy/navigation revision");

            BattleTowerConfig invalid = shared.CreateInstanceCopy();
            invalid.Levels[1].Level = 3;
            try { TowerStatPipeline.Resolve(invalid, 2); throw new InvalidOperationException("Invalid tier chain accepted."); }
            catch (ArgumentException) { }
            BattleTower malformed = scene.Instantiate<BattleTower>();
            malformed.Config = invalid;
            towers.AddChild(malformed); malformed.SetPhysicsProcess(false);
            try
            {
                int before = wallet.GetBalance(wallet.CreditsResource);
                Require(malformed.FinalStats.DamagePerShot == 30 &&
                    !ConstructionTestClock.PurchaseAndFinish(malformed, wallet, placement.Settings, 1, out _) &&
                    malformed.UpgradeLevel == 1 && wallet.GetBalance(wallet.CreditsResource) == before,
                    "Invalid upgrade profile rejects payment while baseline tower still works");
            }
            finally { malformed.Free(); }
            BattleTowerConfig independent = shared.CreateInstanceCopy();
            TowerFinalStats thirdBefore = independent.ResolveStats(3);
            independent.SetLevelStats(2, independent.Levels[1].Stats with {
                DamagePerShot = 999, RangePixels = 900, ShotsPerSecond = 8,
                ProjectileSpeed = 2000, ShieldPenetration = 0.8f });
            Require(independent.ResolveStats(2).DamagePerShot == 999 && independent.ResolveStats(3) == thirdBefore,
                "Changing every level2 combat value leaves level3 entirely independent");
            var context = new TowerStatContext(new TowerStatBlock(100, 1, 10, 600, 180, 12, .15f),
                new TowerStatBlock(350, 2, 28, 800, 240, 8, .1f, ShieldPenetration: .2f));
            TowerStatTrace trace = new();
            Require(TowerStatPipeline.Resolve(context, trace).RangePixels == 350 &&
                trace.Steps[0].Stats.RangePixels == 100 && trace.Steps[1].Stats.RangePixels == 350,
                "Stage02 substitutes the complete selected row before the remaining six stages");
            GD.Print("PASS: paid tower tiers, real HUD action, preview parity, atomic costs, isolation and explicit level independence");
        }
        finally
        {
            placement.Call("SelectTower", Variant.From((GodotObject)null));
            first.Free(); second.Free(); placement.Settings.AllowUpgrades = allow;
            wallet.Initialize(grid.Level.Settings); hud.ClearInspection();
        }
    }

    // =========================================================
    // Typed comparison examples test exact display precision and positive/negative improvement.
    private static void VerifyTypedComparison()
    {
        TowerFinalStats a = new(220, .8f, 18, 800, 100, 3, .15f,
            Weapon: new WeaponDamageStats(DamageType.Kinetic, false, 1, false, 1),
            Ammunition: new AmmunitionStats(1000, 1));
        TowerFinalStats b = a with { DamagePerShot = 21, RangePixels = 228, ShotsPerSecond = .92f,
            Ammunition = new AmmunitionStats(1200, 1) };
        var rows = TowerUpgradeComparison.Build(a, b);
        Require(rows.Any(x => x.Name == "Damage" && x.Current == "18" && x.Next == "21" && x.Gain == "(+3)" && x.Tone == TowerUpgradeGain.Better)
            && rows.Any(x => x.Name == "Range (px)" && x.Gain == "(+8)")
            && rows.Any(x => x.Name == "Ammo Capacity" && x.Gain == "(+200)"),
            "Typed projectile comparisons show real numbers and gains");
        TowerFinalStats laserA = a with { Beam = new BeamStats(25), Weapon = new WeaponDamageStats(DamageType.Energy, false, 1, false, 1) };
        var laserRows = TowerUpgradeComparison.Build(laserA, laserA with { Beam = new BeamStats(28) });
        Require(laserRows.Any(x => x.Name == "DPS" && x.Gain == "(+3)") && !laserRows.Any(x => x.Name == "Projectile (px/s)"),
            "Beam comparison omits fictional projectile statistics");
        TowerFinalStats waveA = a with { Shockwave = new ShockwaveStats(10, 150, 2.4f, .5f) };
        var waveRows = TowerUpgradeComparison.Build(waveA, waveA with { Shockwave = new ShockwaveStats(12, 150, 2f, .5f) });
        Require(waveRows.Any(x => x.Name == "Wave Interval (s)" && x.Gain == "(−0.4)" && x.Tone == TowerUpgradeGain.Better),
            "Shorter wave interval is a green improvement despite a negative delta");
        Require(TowerUpgradeComparison.Build(null, b).Count == 0, "Missing snapshots don't generate made-up rows");
    }

    // =========================================================
    // Fail the integration run on any tier/payment/preview regression.
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
