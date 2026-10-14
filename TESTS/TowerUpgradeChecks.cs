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
            Require(!ReferenceEquals(first.UpgradeTiers[0], shared.UpgradeTiers[0]), "Tier Resources are isolated per instance");
            TowerFinalStats initial = first.FinalStats;
            TowerFinalStats preview = first.ResolveLevel(2);
            Require(preview.DamagePerShot == 16 && Mathf.IsEqualApprox(preview.ShotsPerSecond, 1.2f)
                && preview.RangePixels == 288 && first.FinalStats == initial && first.UpgradeLevel == 1,
                "Next-tier preview applies operations without changing live stats or progression");
            wallet.Initialize(new LevelConfig { StartingCredits = 149 });
            Require(!TowerUpgradeController.TryUpgrade(first, wallet, placement.Settings, 1, out _) &&
                first.UpgradeLevel == 1 && wallet.GetBalance(wallet.CreditsResource) == 149, "Unaffordable upgrade never changes level or funds");
            wallet.Initialize(new LevelConfig { StartingCredits = 400 });
            placement.Settings.AllowUpgrades = false;
            Require(!TowerUpgradeController.TryUpgrade(first, wallet, placement.Settings, 1, out _) &&
                wallet.GetBalance(wallet.CreditsResource) == 400, "Global upgrade rule prevents payment");
            placement.Settings.AllowUpgrades = true;
            placement.Call("SelectTower", first);
            hud.SetTowerBarOpen(true, true);
            await battle.ToSignal(battle.GetTree(), SceneTree.SignalName.ProcessFrame);
            await battle.ToSignal(battle.GetTree(), SceneTree.SignalName.ProcessFrame);
            Control panel = hud.GetNode<Control>("BottomArea/Inspector");
            Control tray = hud.GetNode<Control>("BottomArea/TraySlot/TowerBar");
            Require(panel.GetGlobalRect().End.Y <= hud.GetGlobalRect().End.Y + 1 &&
                Mathf.IsEqualApprox(tray.Size.Y, hud.Config.BottomBarHeight), "Expanded Inspector stays onscreen while tray retains configured height");
            hud.SetTowerBarOpen(false, true);
            Require(panel.Visible, "Closing build tray leaves upgrade Inspector open");
            Button button = hud.GetNode<Button>("BottomArea/Inspector/Insets/Body/UpgradeButton");
            Label info = hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/UpgradeInfo");
            Require(button.Visible && !button.Disabled && info.Text.Contains("150 Credits") &&
                info.Text.Contains("12 → 16"), "Inspector shows next tier, effective changes and affordable price");
            hud.EmitSignal(GameplayHUD.SignalName.TowerUpgradeRequested);
            Require(first.UpgradeLevel == 2 && first.FinalStats == preview &&
                wallet.GetBalance(wallet.CreditsResource) == 250, "Actual HUD upgrade action pays once and matches preview");
            Require(second.UpgradeLevel == 1 && second.FinalStats == initial && shared.DamagePerShot == 12,
                "Upgrade leaves other towers and authored base unchanged");
            Require(!TowerUpgradeController.TryUpgrade(first, wallet, placement.Settings, 1, out _) &&
                wallet.GetBalance(wallet.CreditsResource) == 250, "Stale request cannot buy an already purchased tier");
            hud.EmitSignal(GameplayHUD.SignalName.TowerUpgradeRequested);
            Require(first.UpgradeLevel == 2 && wallet.GetBalance(wallet.CreditsResource) == 250,
                "Repeated HUD requests in one frame cannot buy multiple tiers");
            await battle.ToSignal(battle.GetTree(), SceneTree.SignalName.ProcessFrame);
            TowerFinalStats levelThree = first.ResolveLevel(3);
            hud.EmitSignal(GameplayHUD.SignalName.TowerUpgradeRequested);
            Require(first.UpgradeLevel == 3 && first.FinalStats == levelThree &&
                first.FinalStats.DamagePerShot == 22 && Mathf.IsEqualApprox(first.FinalStats.ShotsPerSecond, 1.5f) &&
                wallet.GetBalance(wallet.CreditsResource) == 0, "Second purchase accumulates tier improvements, not old final values");
            Require(!button.Visible && info.Text.Contains("Maximum level"), "Maximum tier removes upgrade action");
            Require(!TowerUpgradeController.TryUpgrade(first, wallet, placement.Settings, 3, out _) &&
                wallet.GetBalance(wallet.CreditsResource) == 0, "Maximum tier cannot spend again");
            first.RebuildStats(); Require(first.FinalStats == levelThree, "Repeated rebuild does not compound bonuses");

            ResourceDefinition alloy = new() { ResourceId = "alloy", DisplayName = "Alloy" };
            second.NextUpgrade.Costs.Add(new ResourceCost { Resource = alloy, Amount = 2 });
            wallet.Initialize(new LevelConfig { StartingCredits = 200 }); wallet.Grant(alloy, 1);
            Require(!TowerUpgradeController.TryUpgrade(second, wallet, placement.Settings, 1, out _) &&
                wallet.GetBalance(wallet.CreditsResource) == 200 && wallet.GetBalance(alloy) == 1, "Missing secondary resource causes no partial payment");
            wallet.Grant(alloy, 1);
            Require(TowerUpgradeController.TryUpgrade(second, wallet, placement.Settings, 1, out _) &&
                wallet.GetBalance(wallet.CreditsResource) == 50 && wallet.GetBalance(alloy) == 0, "Multi-resource upgrades pay atomically");
            Require(shared.UpgradeTiers[0].Costs.Count == 1, "Runtime recipe edits do not mutate saved tier defaults");
            Require(grid.NavigationRevision == revision, "Upgrading never changes occupancy/navigation revision");

            BattleTowerConfig invalid = shared.CreateInstanceCopy();
            invalid.UpgradeTiers[0].Level = 3;
            try { TowerStatPipeline.Resolve(invalid, 2); throw new InvalidOperationException("Invalid tier chain accepted."); }
            catch (ArgumentException) { }
            BattleTower malformed = scene.Instantiate<BattleTower>();
            malformed.Config = invalid;
            towers.AddChild(malformed); malformed.SetPhysicsProcess(false);
            try
            {
                int before = wallet.GetBalance(wallet.CreditsResource);
                Require(malformed.FinalStats.DamagePerShot == 12 &&
                    !TowerUpgradeController.TryUpgrade(malformed, wallet, placement.Settings, 1, out _) &&
                    malformed.UpgradeLevel == 1 && wallet.GetBalance(wallet.CreditsResource) == before,
                    "Invalid upgrade profile rejects payment while baseline tower still works");
            }
            finally { malformed.Free(); }
            invalid = shared.CreateInstanceCopy();
            invalid.UpgradeTiers[0].Modifiers.Add(new TowerUpgradeModifier { Stat = TowerStatId.Range,
                Operation = TowerStatOperation.Multiplier, Amount = float.NaN });
            try { TowerStatPipeline.Resolve(invalid, 2); throw new InvalidOperationException("Invalid modifier accepted."); }
            catch (ArgumentException) { }
            var context = new TowerStatContext(new TowerStatBlock(100, 1, 10, 600, 180, 12, .15f),
                System.Collections.Immutable.ImmutableArray.Create(
                    new TowerStatModifier(TowerStatId.Range, TowerStatOperation.Flat, 10, "one"),
                    new TowerStatModifier(TowerStatId.Range, TowerStatOperation.AdditivePercent, .2f, "one"),
                    new TowerStatModifier(TowerStatId.Range, TowerStatOperation.AdditivePercent, .2f, "two"),
                    new TowerStatModifier(TowerStatId.Range, TowerStatOperation.Multiplier, .5f, "three")));
            Require(Mathf.IsEqualApprox(TowerStatPipeline.Resolve(context).RangePixels, 77),
                "Stage arithmetic combines flat, additive percentages and explicit multipliers");
            GD.Print("PASS: paid tower tiers, real HUD action, preview parity, atomic costs, isolation and modifier arithmetic");
        }
        finally
        {
            placement.Call("SelectTower", Variant.From((GodotObject)null));
            first.Free(); second.Free(); placement.Settings.AllowUpgrades = allow;
            wallet.Initialize(grid.Level.Settings); hud.ClearInspection();
        }
    }

    // =========================================================
    // Fail the integration run on any tier/payment/preview regression.
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
