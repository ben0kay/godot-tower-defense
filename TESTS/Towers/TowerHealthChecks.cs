// Real catalog/placement regression for shared tower hull, DefenseBar reuse and destructive lifecycle cleanup.
using Godot;
using System;
using static AmmoLoopFixture;

public partial class TowerHealthChecks : Node
{
    // =========================================================
    // Test both weapon/passive ownership and real paid construction without introducing enemy attacks.
    public override async void _Ready()
    {
        try
        {
            using AmmoLoopFixture f = new(this);
            SubsystemBattlefield.DetachSession(f.Main);
            foreach (float invalid in new[] { 0f, -5f, float.NaN, float.PositiveInfinity })
            {
                var source = new TowerStatBlock(0, 0, 0, 1, 0, 0, .1f, MaxHealth: invalid);
                TowerStatTrace trace = new();
                Require(TowerStatPipeline.Resolve(new TowerStatContext(source), trace).MaxHealth == 1 && trace.Steps.Count == 8, "Eight-stage finite HP minimum");
            }
            var hpOnly = new TowerRankDefinition(TowerRank.Veteran, 1000, 0, 0, .25f);
            Require(TowerStatPipeline.Resolve(new TowerStatContext(new(0, 0, 0, 1, 0, 0, .1f), Rank: hpOnly)).MaxHealth == 125, "HP-only rank uses Stage03");
            foreach (string id in new[] { "battle", "laser", "blaster_cannon", "shockwave", "sniper", "machine_gun", "cryo", "ammo_factory", "ammo_depot" })
            {
                TowerBase tower = f.Add<TowerBase>(id, new(2000, 2100));
                Require(tower.Health == 100 && tower.MaxHealth == 100, id + " starts full");
                DefenseBar bar = tower.GetNode<DefenseBar>("DefenseBar");
                Require(!bar.Visible && !bar.IsProcessing() && !bar.IsPhysicsProcessing(), "Shared undamaged bar has no tick");
                TowerFinalStats cache = tower.FinalStats;
                tower.ApplyDamage(new(25, DamageType.Kinetic, hullCoefficientOverride: 1));
                Require(tower.Health == 75 && bar.Visible && bar.HealthFraction == .75f && !bar.ShieldVisible, "Shared damaged hull-only bar");
                Require(ReferenceEquals(cache, tower.FinalStats), "Damage does not invalidate weapon cache");
                Require(bar.Position.Y < -tower.Footprint.Y * 32 && bar.Rotation == 0, "Bar clears footprint above fixed root");
                GetTree().Paused = true; tower.ApplyDamage(new(10, DamageType.Energy)); tower.RestoreHealth(10); GetTree().Paused = false;
                Require(tower.Health == 75, "Damage and repair respect pause");
                tower.ApplyDamage(new(10, DamageType.Energy, hullCoefficientOverride: 2));
                Require(tower.Health == 55, "Typed shared hull multiplier applied once");
                tower.RestoreHealth(1000); Require(tower.Health == 100 && !bar.Visible, "Explicit repair caps and hides bar");
                tower.ApplyDamage(new(float.MaxValue, DamageType.Explosive, hullCoefficientOverride: 1));
                Require(tower.IsDestroyed && !bar.Visible && !tower.CanOperate, "All catalog types stop operation on destruction");
                await Frames();
            }
            BattleTower battle = f.Add<BattleTower>("battle", new(2000,2100));
            battle.ApplyDamage(new(25, DamageType.Kinetic, hullCoefficientOverride: 1));
            TowerRankController.AwardExperience(battle, 1000);
            float ranked = 100 * (1 + battle.CurrentRank.HpBonus);
            Require(Near(battle.MaxHealth, ranked) && Near(battle.Health, ranked - 25), "Rank HP preserves missing hull");
            var old = battle.Config.Levels[0].Stats;
            battle.Config.SetLevelStats(1, old with { MaxHealth = 200 });
            Require(Near(battle.MaxHealth, 200 * (1 + battle.CurrentRank.HpBonus)) && Near(battle.Health, battle.MaxHealth - 25), "Config rebuild does not refill damage");
            battle.Config.SetLevelStats(2, battle.Config.Levels[1].Stats with { MaxHealth = 300 });
            TowerFinalStats preview = battle.ResolveLevel(2);
            Require(TowerUpgradeComparison.Build(battle.FinalStats, preview)[0].Name == "Max HP", "Upgrade exposes HP increase");
            f.Wallet.Initialize(new LevelConfig { StartingCredits = 10000 });
            Require(ConstructionTestClock.PurchaseAndFinish(battle, f.Wallet, f.Placement.Settings, 1, out string error), error);
            Require(battle.MaxHealth == preview.MaxHealth && Near(battle.Health, preview.MaxHealth - 25), "Paid tier matches preview without healing damage");
            f.Placement.Call("SelectTower", battle); await Frames();
            Label runtime = f.Hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/Columns/Side/RuntimeStats");
            Require(runtime.Text.Contains("HP:"), "Selected Inspector includes HP");
            battle.ApplyDamage(new(5, DamageType.Kinetic, hullCoefficientOverride: 1)); await Frames();
            Require(runtime.Text.Contains($"HP: {battle.Health:0.#} / {battle.MaxHealth:0.#}"), "Damage refreshes selected Inspector");
            battle.Free();

            foreach (var (id, finish) in new[] { ("battle", false), ("battle", true), ("ammo_factory", false), ("ammo_depot", true) })
            {
                TowerBase placed = f.Purchase<TowerBase>(id, finish);
                Vector2I cell = placed.GridCell, footprint = placed.Footprint; WorldGrid grid = f.Main.GetNode<WorldGrid>("World/Grid");
                int balance = f.Wallet.GetBalance(f.Wallet.CreditsResource), deaths = 0;
                placed.Destroyed += () => { deaths++; Require(placed.IsDestroyed && !placed.CanAttack, "Death latched before callback"); };
                placed.ApplyDamage(new(float.MaxValue, DamageType.Explosive, hullCoefficientOverride: 1));
                placed.ApplyDamage(new(1, DamageType.Kinetic)); placed.RestoreHealth(100);
                Require(deaths == 1 && placed.Health == 0, "Death exactly once, cannot revive");
                await Frames();
                for (int y = 0; y < footprint.Y; y++) for (int x = 0; x < footprint.X; x++)
                {
                    Vector2I claimed = cell + new Vector2I(x, y);
                    Require(f.Placement.TowerAt(claimed) == null && !grid.IsOccupied(claimed), "Destruction releases whole footprint and navigation claim");
                }
                Require(!f.Hud.IsInspectionVisible && f.Wallet.GetBalance(f.Wallet.CreditsResource) == balance, "Clears Inspector, no destruction refund");
                ConstructionTestClock.FinishWork(f.Main);
            }
            GD.Print("TOWER HEALTH CHECKS PASSED"); GetTree().Quit();
        }
        catch (Exception exception) { GetTree().Paused = false; GD.PushError(exception.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    // Real deferred HUD notifications and QueueFree cleanup settle on the engine event loop.
    private async System.Threading.Tasks.Task Frames()
    { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private static bool Near(float a, float b) => Math.Abs(a - b) < .001f;
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
