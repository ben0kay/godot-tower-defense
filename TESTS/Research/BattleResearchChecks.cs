// Full Battle damage research flow with isolated profiles, real purchases, ranks/tiers, previews and projectile damage.
using Godot;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AmmoLoopFixture;

public partial class BattleResearchChecks : Node
{
    // =========================================================
    // Use temporary progression files and memory-only profile identities; restore shared test state afterwards.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        string directory = Path.Combine(Path.GetTempPath(), "td-battle-research-" + Guid.NewGuid().ToString("N"));
        FieldInfo profiles = typeof(ProfileStore).GetField("_profiles", BindingFlags.Static | BindingFlags.NonPublic);
        FieldInfo store = typeof(ProgressionService).GetField("_store", BindingFlags.Static | BindingFlags.NonPublic);
        object oldProfiles = profiles.GetValue(null), oldStore = store.GetValue(null);
        PlayerProfile selected = ProfileStore.Selected;
        try
        {
            await Frame(); GetTree().CurrentScene = null;
            var a = new PlayerProfile { Id = Guid.NewGuid().ToString("N"), Name = "Battle research A" };
            var b = new PlayerProfile { Id = Guid.NewGuid().ToString("N"), Name = "Battle research B" };
            profiles.SetValue(null, new List<PlayerProfile> { a, b }); store.SetValue(null, new ProgressionStore(directory));
            ProfileStore.Select(a.Id); ProgressionService.GrantXP(600);
            ResearchDeployment before = ResearchService.CaptureDeployment();
            MenuNavigation.Open(this, MenuNavigation.ResearchScene); ResearchMenu room = await Scene<ResearchMenu>();
            ResearchGraph graph = room.FindChildren("*", "Control", true, false).OfType<ResearchGraph>().Single();
            graph.Select("battle_weapons_1");
            Button buy = (Button)room.FindChild("PurchaseResearch", true, false);
            Require(!buy.Disabled && room.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text.Contains("+5% tower damage")), "Battle node is purchasable with an exact effect description");
            buy.EmitSignal(Button.SignalName.Pressed);
            Require(buy.Disabled && buy.Text == "RESEARCHED" && ProgressionService.GetCurrent().UnspentResearchPoints == 0 &&
                ProgressionService.GetCurrent().PurchasedResearchIds.Contains("battle_weapons_1"), "Real research button saves payment and ownership");
            buy.EmitSignal(Button.SignalName.Pressed); Require(ProgressionService.GetCurrent().UnspentResearchPoints == 0, "Repeat purchase cannot charge");
            store.SetValue(null, new ProgressionStore(directory));
            var deployment = ResearchService.CaptureDeployment();
            Require(deployment.ForTower("battle").DamagePercent == 5 && before.ForTower("battle").DamagePercent == 0 &&
                deployment.ForTower("laser").DamagePercent == 0 && deployment.CitadelMaximum(1000) == 1000, "Frozen, type-scoped snapshot survives disk reload without unrelated bonuses");
            foreach (string path in new[] { MenuNavigation.Level001Scene, MenuNavigation.Level002Scene }) await Battlefield(path);
            ProfileStore.Select(b.Id);
            using (AmmoLoopFixture f = new(this)) Require(f.Purchase<BattleTower>("battle").FinalStats.DamagePerShot == 30, "Other profile remains neutral");
            ProfileStore.ClearSelection();
            using (AmmoLoopFixture f = new(this)) Require(f.Purchase<BattleTower>("battle").FinalStats.DamagePerShot == 30, "Profileless F6 remains neutral");
            ProfileStore.Select(a.Id); MenuNavigation.Open(this, MenuNavigation.ResearchScene); room = await Scene<ResearchMenu>();
            graph = room.FindChildren("*", "Control", true, false).OfType<ResearchGraph>().Single(); graph.Select("battle_weapons_1");
            Require(((Button)room.FindChild("PurchaseResearch", true, false)).Text == "RESEARCHED", "Purchase remains visible after reopen");
            var invalid = (ResearchCatalog)GD.Load<ResearchCatalog>("res://SYSTEMS/Progression/Research/DefaultResearchCatalog.tres").Duplicate(true);
            invalid.Definitions.Last().TargetTowerId = "";
            bool rejected = false; try { invalid.Build(); } catch { rejected = true; }
            Require(rejected, "Tower effects require an authored stable target ID");
            GD.Print("BattleResearchChecks: PASS actual purchase/reload, all ranks/tiers, both maps, Inspector, live projectile, profile isolation and neutral F6");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
        finally
        {
            if (GetTree().CurrentScene is Node scene && scene != this) scene.Free(); GetTree().CurrentScene = null;
            ProfileStore.ClearSelection(); profiles.SetValue(null, oldProfiles); store.SetValue(null, oldStore);
            if (selected != null) ProfileStore.Select(selected.Id);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    // =========================================================
    // Real placed towers and previews share Stage04; upgrades/ranks/config refresh never compound the bonus.
    private async Task Battlefield(string scene)
    {
        using AmmoLoopFixture f = new(this, scene);
        f.Hud.GetBuildButton("battle").EmitSignal(Button.SignalName.Pressed);
        Require(f.Hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/Columns/Main/TowerStats").Text.Contains("Damage: 32"), "Build preview includes research before paying");
        var tower = f.Purchase<BattleTower>("battle");
        Require(tower.FinalStats.DamagePerShot == 32 && tower.AuthoredConfig.Levels[0].Stats.DamagePerShot == 30,
            "30 damage becomes32 after integer rounding without authored mutation");
        Require(UpgradeTableChecks.HasRow(f.Hud, "Damage", "32", "35", "(+3)"), "Actual Inspector current/next/gain includes research");
        foreach (TowerRankDefinition rank in tower.AuthoredConfig.Ranks)
            for (int level = 1; level <= 3; level++)
            {
                TowerFinalStats neutral = TowerStatPipeline.Resolve(tower.AuthoredConfig, level, rank: rank);
                TowerFinalStats enhanced = TowerStatPipeline.Resolve(tower.AuthoredConfig, level, rank: rank, research: new(5));
                int expected = (int)Math.Round(neutral.DamagePerShot * 1.05);
                Require(enhanced.DamagePerShot == expected && enhanced == neutral with { DamagePerShot = expected }, "All tiers/ranks modify damage only");
            }
        TowerFinalStats preview = tower.ResolveLevel(2);
        Require(ConstructionTestClock.PurchaseAndFinish(tower, f.Wallet, f.Placement.Settings, 1, out _) && tower.FinalStats == preview && tower.FinalStats.DamagePerShot == 35, "Paid tier matches research preview");
        TowerRankController.AwardExperience(tower, 1000);
        Require(tower.FinalStats.DamagePerShot == (int)Math.Round(TowerStatPipeline.Resolve(tower.AuthoredConfig, 2, rank: tower.CurrentRank).DamagePerShot * 1.05), "Real rank promotion retains research");
        int amount = tower.FinalStats.DamagePerShot;
        tower.Config.SetLevelStats(2, tower.Config.Levels[1].Stats);
        Require(tower.FinalStats.DamagePerShot == amount && tower.ResolveLevel(2).DamagePerShot == amount, "Rebuilds do not multiply research again");
        tower.SetTargetPriority(TowerTargeting.Priority.Closest);
        Enemy enemy = f.Target(tower.GlobalPosition + new Vector2(0, -80));
        int stock = tower.Ammunition.Inventory.Amount; float health = enemy.Health;
        for (int i = 0; i < 180 && f.Main.GetNode("World/Projectiles").GetChildCount() == 0; i++) tower._PhysicsProcess(1.0 / 60);
        BasicProjectile bullet = f.Main.GetNode("World/Projectiles").GetChildren().OfType<BasicProjectile>().First();
        bullet.SetPhysicsProcess(false); bullet._PhysicsProcess(1);
        Require(enemy.Health == health - amount && tower.Ammunition.Inventory.Amount == stock - 1, "Actual projectile carries research damage without changing ammo cost");
        enemy.Free(); // Stationary damage target is not a navigation fixture for subsequent placements.
        var laser = f.Purchase<LaserTower>("laser");
        Require(laser.FinalStats == TowerStatPipeline.Resolve(laser.Config, 1, rank: laser.CurrentRank), "Unrelated tower is unchanged");
        var captured = f.Main.GetNode<CitadelSession>("CitadelSession").Research;
        ProfileStore.ClearSelection();
        Require(tower.ResolveLevel(2).DamagePerShot == amount && captured.ForTower("battle").DamagePercent == 5, "Active mission/preview uses captured data rather than current selection");
        ProfileStore.Select(captured.ProfileId);
        await Frame();
    }

    // =========================================================
    // Settle deferred scene navigation before querying the actual Research Room.
    private async Task<T> Scene<T>() where T : Node
    {
        for (int i = 0; i < 30; i++) { await Frame(); if (GetTree().CurrentScene is T scene && scene.IsNodeReady()) return scene; }
        throw new Exception("Expected scene " + typeof(T).Name);
    }
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
}
