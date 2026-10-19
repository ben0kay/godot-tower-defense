// Real economy tower placement, lifecycle, attribution, Inspector collection and upgrade regression.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using static AmmoLoopFixture;
using static WaveBuilder;

public partial class CreditTowerChecks : Node
{
    #region Runner
    // =========================================================
    // Use actual catalog/purchase/UI and the existing active-wave scheduler with frozen test clocks.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            using (AmmoLoopFixture f = new(this)) await Check(f);
            GetTree().Paused = false;
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: CP2/CP3 paid towers, active-wave timer, attribution/range/overlap, collection/overflow, upgrades and final kill");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
    #endregion

    #region Production and UI
    // =========================================================
    // Paid build sites do not produce; resources remain separate until the Inspector action succeeds.
    private async Task Check(AmmoLoopFixture f)
    {
        WorldGrid grid = f.Spawns.Paths.Navigation.Grid;
        grid.Level.Passable.Clear(); grid.Level.Impassable.Clear(); grid.Level.Interactive.Clear();
        for (int y = 0; y < grid.Config.HeightCells; y++) for (int x = 0; x < grid.Config.WidthCells; x++)
            grid.Level.Passable.SetCell(new(x, y), 0, new(2, 0));
        grid.RebuildTerrain(); f.Spawns.Paths.Navigation.Refresh();
        f.Wallet.Grant(f.Wallet.CreditsResource, 10000);
        Require(f.Hud.GetBuildButton("resource_tower") != null && f.Hud.GetBuildButton("adaptable_resource_generator") != null,
            "Both registered economy types appear in the build tray");
        ResourceTower resource = f.Purchase<ResourceTower>("resource_tower", false);
        AdaptableResourceGenerator arg = f.Purchase<AdaptableResourceGenerator>("adaptable_resource_generator", false);
        resource._PhysicsProcess(50); Require(resource.Buffer.Current == 0, "Incomplete/inactive producer cannot accrue");
        ConstructionTestClock.FinishWork(f.Main);
        resource._PhysicsProcess(5); Require(resource.Buffer.Current == 0, "Between waves production stops");
        WaveManager waves = f.Main.GetNode<WaveManager>("Waves");
        waves.Configure(new[] { Wave(1, Batch(1, 0, 0, Enemy("basic_mk1"))) }, f.Spawns.Catalog.ToSceneMap());
        Require(waves.StartWave(), "Real wave starts"); Enemy final = f.Spawns.Registry.Living.Single(); Disable(final);
        resource._PhysicsProcess(4); Require(resource.Buffer.Current == 0, "Periodic timer waits exact interval");
        resource._PhysicsProcess(1); Require(resource.Buffer.Current == 10, "Five active seconds produces ten stored Credits");
        var handle = resource.ApplyDisable(TowerDisableReason.EMP);
        resource._PhysicsProcess(50); Require(resource.Buffer.Current == 10, "Disabled producer clock freezes"); resource.RemoveDisable(handle);
        GetTree().Paused = true; resource._PhysicsProcess(50); Require(!resource.TryCollect() && resource.Buffer.Current == 10, "Pause blocks production and collection"); GetTree().Paused = false;
        int wallet = f.Wallet.GetBalance(f.Wallet.CreditsResource);
        f.Placement.Call("SelectTower", resource); f.Hud.SetDefenseDetails(resource);
        Button collect = resource.GetNode<GameplayHUD>("../../../UI/GameplayHUD").GetNode<Button>("BottomArea/Inspector/Insets/Body/Columns/Side/DefenseMeters/CollectCredits");
        Require(collect.Visible && !collect.Disabled, "Producer exposes Inspector collection"); collect.EmitSignal(Button.SignalName.Pressed);
        Require(resource.Buffer.Current == 0 && f.Wallet.GetBalance(f.Wallet.CreditsResource) == wallet + 10 && !resource.TryCollect(), "Inspector grants once, then empties only its buffer");
        resource._PhysicsProcess(1000); Require(resource.Buffer.Current == 100, "Storage caps without granting spendable Credits");
        int room = f.Wallet.Config.MaxBalancePerResource - f.Wallet.GetBalance(f.Wallet.CreditsResource);
        f.Wallet.Grant(f.Wallet.CreditsResource, room); Require(!resource.TryCollect() && resource.Buffer.Current == 100, "Full wallet preserves entire buffer");
        f.Wallet.Initialize(f.Spawns.Paths.Navigation.Grid.Level.Settings); f.Wallet.Grant(f.Wallet.CreditsResource, 10000);
        Require(ConstructionTestClock.PurchaseAndFinish(resource, f.Wallet, f.Placement.Settings, 1, out string error), "Paid producer upgrade: " + error);
        Require(resource.UpgradeLevel == 2 && resource.Buffer.Capacity == 150 && resource.Buffer.Current == 100
            && resource.FinalStats.Production.Value.PerCycle == 15 && resource.MaxHealth == 200, "Upgrade preserves stock and applies economy/health snapshot");
        Require(TowerUpgradeComparison.Build(resource.FinalStats, resource.ResolveLevel(3)).Any(r => r.Name == "Credits / cycle")
            && !TowerUpgradeComparison.Build(resource.FinalStats, resource.ResolveLevel(3)).Any(r => r.Name == "Damage"), "Economy preview has production rows, no fake weapon stats");
        resource._PhysicsProcess(5); Require(resource.Buffer.Current == 115, "Upgraded payout uses fresh timer");
        Require(TowerUpgradeController.TryUpgrade(resource, f.Wallet, f.Placement.Settings, 2, out error), "Tier three purchases: " + error);
        resource._PhysicsProcess(50); Require(resource.Buffer.Current == 115, "Paid upgrade work suspends income");
        ConstructionTestClock.FinishWork(f.Main);
        Require(resource.Buffer.Current == 115 && resource.Buffer.Capacity == 200 && resource.FinalStats.Production.Value.PerCycle == 20,
            "Third absolute resource tier retains stored Credits");
        Require(resource.Config.ProductionLevels[0] != GD.Load<ResourceTowerConfig>("res://TOWERS/Economy/ResourceTower/DefaultResourceTowerConfig.tres").ProductionLevels[0],
            "Placed production tier resources are private copies");
        await Attribution(f, arg, final);
    }
    #endregion

    #region Attribution and Safety
    // =========================================================
    // Confirm source-position range rather than victim-position range, missing sources, overlap and terminal kill.
    private async Task Attribution(AmmoLoopFixture f, AdaptableResourceGenerator arg, Enemy final)
    {
        BattleTower killer = f.Add<BattleTower>("battle", arg.GlobalPosition + new Vector2(64, 0));
        AdaptableResourceGenerator second = f.Add<AdaptableResourceGenerator>("adaptable_resource_generator", arg.GlobalPosition + new Vector2(0, 64));
        Enemy victim = f.Spawns.Spawn("basic_mk1", true); Disable(victim);
        victim.GlobalPosition = killer.GlobalPosition + new Vector2(1000, 0);
        double xp = killer.RankProgress.TotalXp;
        double expectedXp = victim.Definition.KillRewardCredits * (killer.ExperienceRules.DamageXpMultiplier + killer.ExperienceRules.KillXpMultiplier);
        int beforeWallet = f.Wallet.GetBalance(f.Wallet.CreditsResource);
        victim.ApplyDamage(new DamageInfo(99999, DamageType.Kinetic, source: killer.DamageSource)); victim.TakeDamage(99999);
        Require(arg.Buffer.Current == 1 && second.Buffer.Current == 1 && f.Wallet.GetBalance(f.Wallet.CreditsResource) == beforeWallet + victim.Definition.KillRewardCredits,
            "Each eligible ARG earns once from the killing tower; victim location is irrelevant and ordinary reward unchanged");
        Require(Math.Abs(killer.RankProgress.TotalXp - xp - expectedXp) < .0001, "Attributed kill retains the exact ordinary tower damage/kill XP budget");
        killer.GlobalPosition = arg.GlobalPosition + new Vector2(1000, 0);
        victim = f.Target(arg.GlobalPosition); victim.ApplyDamage(new DamageInfo(99999, DamageType.Kinetic, source: killer.DamageSource));
        Require(arg.Buffer.Current == 1, "Nearby victim does not qualify a distant killing tower");
        victim = f.Target(arg.GlobalPosition); victim.TakeDamage(99999); Require(arg.Buffer.Current == 1, "Unattributed kill earns no ARG income");
        killer.GlobalPosition = arg.GlobalPosition;
        var handle = arg.ApplyDisable(TowerDisableReason.EMP);
        victim = f.Target(arg.GlobalPosition); victim.ApplyDamage(new DamageInfo(99999, DamageType.Kinetic, source: killer.DamageSource));
        Require(arg.Buffer.Current == 1 && second.Buffer.Current == 2, "Disabled ARG stops independently"); arg.RemoveDisable(handle);
        Require(ConstructionTestClock.PurchaseAndFinish(arg, f.Wallet, f.Placement.Settings, 1, out string error), "ARG paid upgrade: " + error);
        Require(arg.Buffer.Current == 1 && arg.Buffer.Capacity == 150 && arg.FinalStats.Production.Value.PerKill == 2
            && arg.InspectionRange == 288, "ARG upgrade changes yield/range without discarding stored income");
        DamageSource stale = killer.DamageSource; killer.QueueFree();
        victim = f.Target(arg.GlobalPosition); victim.ApplyDamage(new DamageInfo(99999, DamageType.Kinetic, source: stale));
        Require(arg.Buffer.Current == 1, "Queued-for-removal source cannot earn income");
        killer = f.Add<BattleTower>("battle", arg.GlobalPosition);
        CreditBuffer destroyedBuffer = second.Buffer; int destroyedStock = destroyedBuffer.Current;
        second.ApplyDamage(new DamageInfo(99999, DamageType.Kinetic));
        final.ApplyDamage(new DamageInfo(99999, DamageType.Kinetic, source: killer.DamageSource));
        Require(arg.Buffer.Current == 3 && destroyedBuffer.Current == destroyedStock, "Destroyed ARG stops; the final owned wave kill is credited before completion gates close");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(!arg.TryCollect(), "Mission result blocks collection from stale gameplay UI");
    }
    #endregion
}
