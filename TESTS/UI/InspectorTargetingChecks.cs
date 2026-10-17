// Real non-scrolling Inspector geometry, six per-instance policies and cached spatial ranking regressions.
using Godot;
using System;
using System.Linq;
using static AmmoLoopFixture;

public partial class InspectorTargetingChecks : Node
{
    // =========================================================
    // Drive production controls and wait for actual Godot layout; no mocked HUD or damage systems.
    public override async void _Ready()
    {
        try
        {
            await Run(this);
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: IT1–IT4 compact Inspector, six priorities, instance isolation, journey estimates, spatial parity and bounded work");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }

    // =========================================================
    // Shared entry point lets the full integration suite run these same production fixtures.
    public static async System.Threading.Tasks.Task Run(Node owner)
    {
        using (AmmoLoopFixture f = new(owner))
        {
            SubsystemBattlefield.DetachSession(f.Main);
            Ranking(f); Policies(f); await Layout(f);
            SpatialQueryChecks.Run(f.Main, f.Spawns);
            await TowerRankChecks.Run(f.Main, f.Spawns);
        }
        await JourneyTargetingChecks.Run(owner);
    }

    // =========================================================
    // Durability is remaining hull plus shield, not authored maximum, speed, reward or attempted damage.
    private static void Ranking(AmmoLoopFixture f)
    {
        var registry = f.Spawns.Registry;
        Enemy near = f.Target(new(2000, 2000)), far = f.Target(new(2100, 2000));
        near.Definition.MaxShield = 500; far.Definition.MaxHealth = 100;
        far.Initialize(registry, f.Spawns.Paths); Disable(far);
        near.ApplyDamage(new(9900, DamageType.Kinetic));
        Require(TowerTargeting.Find(registry, new(2000, 2000), 208, TowerTargeting.Priority.Closest) == near, "Closest physical distance");
        Require(TowerTargeting.Find(registry, new(2000, 2000), 208, TowerTargeting.Priority.Farthest) == far, "Farthest physical distance");
        // Equal 100 hull: earlier registered enemy wins both extrema.
        Require(TowerTargeting.Find(registry, new(2000, 2000), 208, TowerTargeting.Priority.Strongest) == near, "Stable equal-durability order");
        far.ApplyDamage(new(30, DamageType.Kinetic));
        Require(TowerTargeting.Find(registry, new(2000, 2000), 208, TowerTargeting.Priority.Weakest) == far, "Damaged hull affects Weakest");
        near.RestoreShield(200);
        Require(TowerTargeting.Find(registry, new(2000, 2000), 208, TowerTargeting.Priority.Strongest) == near, "Restored shield affects Strongest");
        near.GlobalPosition = new(5000, 5000);
        foreach (TowerTargeting.Priority mode in new[] { TowerTargeting.Priority.Closest, TowerTargeting.Priority.Farthest, TowerTargeting.Priority.Strongest, TowerTargeting.Priority.Weakest })
            Require(TowerTargeting.Find(registry, new(2000, 2000), 208, mode) == far, "Out-of-range enemy excluded for " + mode);
        Require(TowerTargeting.Find(registry, new(2000, 2000), 208, TowerTargeting.Priority.First) == null
            && TowerTargeting.Find(registry, new(2000, 2000), 208, TowerTargeting.Priority.Last) == null, "Unrouted infinity wins neither First nor Last");
        near.Free(); far.Free();
        foreach (TowerTargeting.Priority mode in Enum.GetValues<TowerTargeting.Priority>())
            Require(TowerTargeting.Find(registry, Vector2.Zero, 10000, mode) == null, "Empty registry is safe for " + mode);
        Require((int)TowerTargeting.Priority.First == 0 && (int)TowerTargeting.Priority.Closest == 1, "Original serialized enum values retained");
    }

    // =========================================================
    // Real button intents change only one private policy, never spend Credits or rebuild FinalStats.
    private static void Policies(AmmoLoopFixture f)
    {
        foreach (string id in new[] { "battle", "laser", "blaster_cannon", "sniper", "machine_gun", "cryo" })
        {
            TowerBase a = f.Add<TowerBase>(id, new(2000, 2064)), b = f.Add<TowerBase>(id, new(2500, 2064));
            f.Placement.Call("SelectTower", a);
            TowerFinalStats cached = a.FinalStats;
            var otherPolicy = b.TargetPriority;
            var authoredPolicy = ((ITowerTargetingPolicy)f.Placement.Catalog.Find(id).DefaultConfig).TargetPriority;
            int credits = f.Wallet.GetBalance(f.Wallet.CreditsResource), ammo = a.Ammunition.Inventory.Amount;
            double xp = a.RankProgress.TotalXp;
            foreach (var priority in Enum.GetValues<TowerTargeting.Priority>())
            {
                f.Hud.GetTargetPriorityButton(priority).EmitSignal(Button.SignalName.Pressed);
                Require(a.TargetPriority == priority && b.TargetPriority == otherPolicy
                    && f.Placement.Catalog.Find(id).DefaultConfig is ITowerTargetingPolicy policy && policy.TargetPriority == authoredPolicy,
                    "Choice is private and supported by " + id);
                Require(ReferenceEquals(a.FinalStats, cached) && a.Ammunition.Inventory.Amount == ammo && a.RankProgress.TotalXp == xp
                    && f.Wallet.GetBalance(f.Wallet.CreditsResource) == credits, "Target choice has no progression/economy/cache side effects");
            }
            f.Placement.Call("SelectTower", b); f.Placement.Call("SelectTower", a);
            Require(f.Hud.GetTargetPriorityButton(TowerTargeting.Priority.Weakest).ButtonPressed, "Reselect retains selected style");
            TowerRankController.AwardExperience(a, 1000);
            Require(a.TargetPriority == TowerTargeting.Priority.Weakest, "Rank promotion preserves policy");
            f.Main.GetTree().Paused = true;
            Require(!a.SetTargetPriority(TowerTargeting.Priority.First), "Pause rejects policy mutations"); f.Main.GetTree().Paused = false;
            if (id == "battle")
            {
                f.Wallet.Initialize(new LevelConfig { StartingCredits = 1000 });
                double retained = a.RankProgress.TotalXp;
                Require(ConstructionTestClock.PurchaseAndFinish(a, f.Wallet, f.Placement.Settings, 1, out string error), error);
                Require(a.TargetPriority == TowerTargeting.Priority.Weakest && a.RankProgress.TotalXp == retained,
                    "Paid level preserves targeting and earned XP");
                credits = f.Wallet.GetBalance(f.Wallet.CreditsResource);
            }
            // Policy change must override the stagger delay on next physics acquisition.
            Enemy target = f.Target(new(2000, 2000));
            a.SetTargetPriority(TowerTargeting.Priority.Closest);
            a.Call("_PhysicsProcess", .25);
            Require(a.Ammunition.Inventory.Amount < ammo, "Next regular weapon tick promptly acquires for " + id);
            target.Free(); a.Free(); b.Free(); f.ClearProjectiles();
        }
        foreach (string id in new[] { "shockwave", "ammo_factory", "ammo_depot" })
        {
            TowerBase passive = f.Add<TowerBase>(id, new(2400, 2000)); f.Placement.Call("SelectTower", passive);
            Require(passive.TargetPriority == null && !passive.SetTargetPriority(TowerTargeting.Priority.Last)
                && !f.Hud.GetTargetPriorityButton(TowerTargeting.Priority.First).IsVisibleInTree(), "No false targeting controls for " + id);
            passive.Free();
        }
    }

    // =========================================================
    // All current tower families fit without scroll/foldouts at supported desktop sizes and busy/max states.
    private static async System.Threading.Tasks.Task Layout(AmmoLoopFixture f)
    {
        GameplayHUD hud = f.Hud; hud.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft); Control panel = hud.GetNode<Control>("BottomArea/Inspector");
        foreach (Vector2I viewport in new[] { new Vector2I(1920,1080), new Vector2I(1366,768), new Vector2I(960,540) })
        {
            hud.Size = viewport; float maximumHeight = 0;
            foreach (string id in new[] { "battle", "laser", "blaster_cannon", "shockwave", "sniper", "machine_gun", "cryo", "ammo_factory", "ammo_depot" })
            {
                TowerBase tower = f.Add<TowerBase>(id, new(2000, 2000)); f.Placement.Call("SelectTower", tower);
                hud.SetTowerBarOpen(true, true);
                await hud.ToSignal(hud.GetTree(), SceneTree.SignalName.ProcessFrame);
                await hud.ToSignal(hud.GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(hud.Size == (Vector2)viewport, "Test viewport remains the requested size");
                Require(panel.GetNode("Insets") is MarginContainer, "Inspector is not a scrolling surface");
                CheckVisible(panel, panel.GetGlobalRect()); maximumHeight = Math.Max(maximumHeight, panel.Size.Y);
                Require(panel.GetGlobalRect().Position.Y >= hud.Config.TopBarHeight
                    && panel.GetGlobalRect().End.X <= hud.GetGlobalRect().End.X + 1
                    && panel.GetGlobalRect().End.Y <= hud.GetGlobalRect().End.Y + 1, "Actual panel fits viewport " + viewport + " for " + id);
                if (tower.FinalStats != null)
                {
                    Require(hud.GetNode<Control>("BottomArea/Inspector/Insets/Body/Columns/Main/UpgradeComparison").IsVisibleInTree(), "Current/next numeric stats immediately visible");
                    // MAX LEVEL uses current-only columns; no invented future tier or folded stats.
                    hud.SetUpgradeComparison(tower.FinalStats, null); hud.SetUpgradeDetails("MAX LEVEL", "", false);
                    Require(hud.GetNode<GridContainer>("BottomArea/Inspector/Insets/Body/Columns/Main/UpgradeComparison").Columns == 2, "Max tier is current-only");
                }
                Require(hud.GetNodeOrNull<Control>("BottomArea/Inspector/Insets/Body/Columns/Main/DetailsToggle") == null, "No additional-details foldout");
                tower.Free();
            }
            // Busy headers and earned MAX RANK must fit too, including the larger Economy footprint.
            foreach (string id in new[] { "battle", "cryo", "ammo_factory", "ammo_depot" })
            {
                TowerDefinition definition = f.Placement.Catalog.Find(id);
                TowerBase site = definition.Scene.Instantiate<TowerBase>(); site.InitializeDefinition(definition);
                ConstructionWorkManager manager = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
                site.StartConstruction(manager); f.Towers.AddChild(site); Disable(site); f.Placement.Call("SelectTower", site);
                await hud.ToSignal(hud.GetTree(), SceneTree.SignalName.ProcessFrame);
                await hud.ToSignal(hud.GetTree(), SceneTree.SignalName.ProcessFrame);
                CheckVisible(panel, panel.GetGlobalRect()); maximumHeight = Math.Max(maximumHeight, panel.Size.Y);
                manager.Advance(site.RemainingWorkSeconds);
                if (site.NextUpgrade != null)
                {
                    f.Wallet.Initialize(new LevelConfig { StartingCredits = 1000 });
                    Require(TowerUpgradeController.TryUpgrade(site, f.Wallet, f.Placement.Settings, 1, out string error), error);
                    TowerRankController.AwardExperience(site, 15000);
                    await hud.ToSignal(hud.GetTree(), SceneTree.SignalName.ProcessFrame);
                    await hud.ToSignal(hud.GetTree(), SceneTree.SignalName.ProcessFrame);
                    CheckVisible(panel, panel.GetGlobalRect()); maximumHeight = Math.Max(maximumHeight, panel.Size.Y);
                    Require(hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/Columns/Side/RankSection/ExperienceText").Text == "MAX RANK",
                        "Busy upgrade keeps the separate MAX RANK card");
                }
                site.Free();
            }
            GD.Print($"IT HUD GEOMETRY {viewport}: width={panel.Size.X:0}, max height={maximumHeight:0}; all visible leaves contained");
        }
    }

    // =========================================================
    // Check every visible leaf control instead of merely checking the panel's outer bounding box.
    private static void CheckVisible(Node node, Rect2 bounds)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Control control && control.IsVisibleInTree())
            {
                Rect2 rect = control.GetGlobalRect();
                Require(rect.Position.X >= bounds.Position.X - 1 && rect.Position.Y >= bounds.Position.Y - 1
                    && rect.End.X <= bounds.End.X + 1 && rect.End.Y <= bounds.End.Y + 1,
                    "Visible content inside panel: " + control.GetPath() + " " + rect + " bounds " + bounds);
            }
            CheckVisible(child, bounds);
        }
    }
}
