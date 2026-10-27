// Shared recoil regression: Battle pilot plus six projectile assemblies, funded origins, construction and cleanup.
using Godot;
using System;
using System.Linq;
using static AmmoLoopFixture;

public partial class TowerRecoilChecks : Node
{
    #region Runner
    // =========================================================
    // Keep fixtures private; no profile writes, source-map edits or authored balance changes.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            Motion();
            using (AmmoLoopFixture f = new(this))
            {
                OpenTerrain(f); Firing(f); Rollout(f); ConstructionAndUpgrades(f); OtherTowers(f);
            }
            GetTree().Paused = false;
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: shared recoil bounds/recovery, seven projectile assemblies, funded muzzle snapshots, fixed bases, construction, upgrades and lifecycle");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
    // =========================================================
    // A paintable isolated terrain avoids depending on free cells in user-authored maps.
    private static void OpenTerrain(AmmoLoopFixture f)
    {
        SubsystemBattlefield.DetachSession(f.Main);
        var grid = f.Spawns.Paths.Navigation.Grid;
        grid.Level.Passable.Clear(); grid.Level.Impassable.Clear(); grid.Level.Interactive.Clear();
        for (int y = 0; y < grid.Config.HeightCells; y++) for (int x = 0; x < grid.Config.WidthCells; x++)
            grid.Level.Passable.SetCell(new(x, y), 0, new(2, 0));
        grid.RebuildTerrain(); f.Spawns.Paths.Navigation.Refresh();
        f.Wallet.Grant(f.Wallet.CreditsResource, 10000);
    }
    #endregion

    #region Controller Contract
    // =========================================================
    // Local motion stays bounded under repeated shots, ignores invalid deltas and restores exact nonzero baselines.
    private void Motion()
    {
        Node2D parent = new() { Position = new(70, 90), Rotation = 1.1f };
        Node2D target = new() { Position = new(3, 5) };
        TowerRecoilController recoil = new();
        AddChild(parent); parent.AddChild(target); parent.AddChild(recoil);
        using TowerRecoilStyle style = new() { Enabled = true };
        recoil.Bind(target, style);
        Require(!recoil.IsProcessing(), "Bound controller sleeps at rest");
        var transform = parent.Transform; Vector2 rest = target.Position;
        recoil.Kick(); Require(target.Position == rest && recoil.Impulses == 1, "Kick is deferred until after muzzle snapshots");
        recoil.Advance(style.KickSeconds);
        Near(recoil.OffsetPixels, 2, "Default peak"); Require(parent.Transform == transform && target.Position == rest + Vector2.Down * 2, "Only target local +Y moves");
        GetTree().Paused = true; recoil.Advance(1); GetTree().Paused = false;
        Near(recoil.OffsetPixels, 2, "Pause holds pose");
        recoil.Advance(double.NaN); Near(recoil.OffsetPixels, 2, "Invalid delta inert");
        int count = parent.GetChildCount();
        for (int i = 0; i < 1000; i++) { recoil.Kick(); recoil.Advance(.01); Require(recoil.OffsetPixels <= 4, "Repeated impulses bounded"); }
        Require(parent.GetChildCount() == count, "Repeated shots create no nodes");
        recoil.Advance(10); Require(!recoil.Active && !recoil.IsProcessing() && target.Position == rest, "Large delta restores exact pose and sleeps");
        recoil.Kick(); recoil.Advance(.02); style.Enabled = false;
        Require(target.Position == rest && !recoil.Active, "Disabling live style restores immediately");
        style.Enabled = true; recoil.Kick(); recoil.Advance(.02);
        Node2D replacement = new() { Position = new(-4, 8) }; parent.AddChild(replacement);
        recoil.Bind(replacement, style); Require(target.Position == rest, "Replacement restores previous target");
        recoil.Kick(); recoil.Advance(.02); recoil.Unbind();
        Require(replacement.Position == new Vector2(-4, 8) && !recoil.IsProcessing(), "Unbind releases target/style state");
        style.KickDistancePixels = float.NaN; style.MaximumOffsetPixels = float.PositiveInfinity; style.KickSeconds = -10; style.ReturnSeconds = double.NaN;
        var safe = style.Capture(); Require(float.IsFinite(safe.Distance) && safe.Maximum <= 8 && safe.KickSeconds > 0 && double.IsFinite(safe.ReturnSeconds), "Malformed Inspector values sanitized");
        RemoveChild(parent); parent.Free();
    }
    #endregion

    #region Funded Battle Firing
    // =========================================================
    // The immutable gameplay snapshot, aiming/root and emitted flash/bullet stay independent of later recoil motion.
    private static void Firing(AmmoLoopFixture f)
    {
        BattleTower tower = f.Add<BattleTower>("battle", new(2000, 2064));
        tower.Config.Recoil = (TowerRecoilStyle)tower.Config.Recoil.Duplicate();
        var recoil = tower.GetNode<TowerRecoilController>("Recoil");
        var root = tower.GetNode<Node2D>("TurretPivot/RecoilRoot");
        var basis = tower.GetNode<Sprite2D>("BaseSprite");
        var muzzle = tower.GetNode<Marker2D>(BattleTower.MuzzlePath);
        var flash = tower.GetNode<WeaponVisualRenderer>("TurretPivot/WeaponVisuals");
        var pivot = tower.GetNode<Node2D>("TurretPivot");
        Enemy enemy = f.Target(new(2000, 2000));
        TowerFinalStats stats = tower.FinalStats; var placement = tower.Transform; var baseTransform = basis.Transform;
        Vector2 launch = muzzle.GlobalPosition;
        tower._PhysicsProcess(.25);
        BasicProjectile shot = f.Main.GetNode("World/Projectiles").GetChildren().OfType<BasicProjectile>().Single();
        Require(recoil.Impulses == 1 && tower.Ammunition.ConsumedRounds == 1 && flash.TotalFlashes == 1
            && shot.GlobalPosition.IsEqualApprox(launch), "One funded shot produces one kick, flash and ammo charge");
        Vector2 flashPosition = flash.ToGlobal(flash.LastFlashPosition); var aiming = pivot.Transform;
        recoil.Advance(.045);
        Require(root.Position.Y > 0 && muzzle.GlobalPosition != launch && shot.GlobalPosition.IsEqualApprox(launch)
            && flash.ToGlobal(flash.LastFlashPosition).IsEqualApprox(flashPosition), "Moving barrel does not drag an already emitted bullet or flash");
        Require(basis.Transform == baseTransform && pivot.Transform == aiming && tower.Transform == placement && ReferenceEquals(stats, tower.FinalStats),
            "Base/aim/root/gameplay snapshots unchanged by recoil");
        long shots = recoil.Impulses;
        tower._PhysicsProcess(.001); Require(recoil.Impulses == shots, "Cooldown is not a recoil event");
        f.ClearProjectiles();
        var disabled = tower.ApplyDisable(TowerDisableReason.EMP);
        Require(root.Position == Vector2.Zero && !recoil.Active, "Disable restores pose immediately");
        tower._PhysicsProcess(1); Require(recoil.Impulses == shots, "Disabled tower never kicks");
        tower.RemoveDisable(disabled);
        tower.GetTree().Paused = true; tower._PhysicsProcess(1); tower.GetTree().Paused = false;
        Require(recoil.Impulses == shots, "Pause never starts a kick");
        tower.Ammunition.Inventory.TryConsume(tower.Ammunition.Inventory.Amount);
        tower._PhysicsProcess(1); Require(recoil.Impulses == shots, "Unfunded shot never kicks");
        tower.SetSelected(true);
        Require(basis.Modulate == tower.GetNode<Sprite2D>(BattleTower.GunSpritePath).Modulate
            && basis.Modulate == tower.GetNode<Sprite2D>(BattleTower.ConstructionSpritePath).Modulate, "Selection colors every presentation layer");
        tower.SetSelected(false);
        recoil.Kick(); recoil.Advance(.02); tower.ApplyDamage(new DamageInfo(99999, DamageType.Kinetic));
        Require(root.Position == Vector2.Zero && !recoil.Active, "Destruction restores visual before removal");
        f.Towers.RemoveChild(tower); tower.Free(); enemy.Free();
    }
    #endregion

    #region Construction and Upgrade Artwork
    // =========================================================
    // Actual paid work uses original stage textures, then switches to split defaults without changing prices/tier mechanics.
    private static void ConstructionAndUpgrades(AmmoLoopFixture f)
    {
        BattleTower tower = f.Purchase<BattleTower>("battle", false);
        var overlay = tower.GetNode<Sprite2D>(BattleTower.ConstructionSpritePath);
        var basis = tower.GetNode<Sprite2D>("BaseSprite");
        var moving = tower.GetNode<Node2D>("TurretPivot/RecoilRoot");
        var recoil = tower.GetNode<TowerRecoilController>("Recoil");
        Require(overlay.Visible && !basis.Visible && !moving.Visible && !tower.IsOperational, "Initial stage overlay hides finished layers");
        f.Target(tower.GlobalPosition + new Vector2(0, -64));
        tower._PhysicsProcess(1); Require(recoil.Impulses == 0, "Initial work cannot fire/recoil");
        ConstructionTestClock.FinishWork(f.Main);
        Require(!overlay.Visible && basis.Visible && moving.Visible && tower.IsOperational, "Completed artwork switches to fixed base and gun");
        tower._PhysicsProcess(1); Require(recoil.Impulses == 1, "Completed tower fires with recoil");
        f.ClearProjectiles();
        Require(TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 1, out _), "Paid level2 upgrade starts");
        var old = tower.FinalStats; long before = recoil.Impulses;
        tower._PhysicsProcess(1); Require(tower.IsBusy && tower.IsDisabled && tower.UpgradeLevel == 1
            && tower.FinalStats == old && recoil.Impulses == before && !recoil.Active,
            "Paid upgrade suspends firing/recoil while the original stats remain cached");
        f.ClearProjectiles(); ConstructionTestClock.FinishWork(f.Main);
        Require(tower.UpgradeLevel == 2 && moving.Visible && basis.Visible && !overlay.Visible, "Level2 retains split finished layers");
        Require(ConstructionTestClock.PurchaseAndFinish(tower, f.Wallet, f.Placement.Settings, 2, out _), "Paid level3 completes");
        tower.ReceiveExperience(100000); var promoted = tower.FinalStats;
        tower._PhysicsProcess(1); Require(recoil.Active && tower.FinalStats == promoted && tower.UpgradeLevel == 3, "Purchased tier and earned rank remain independent of visual kick");
        recoil.Cancel(); f.ClearProjectiles();
        var definition = f.Placement.Catalog.Find("battle");
        BattleTower researched = definition.Scene.Instantiate<BattleTower>();
        researched.InitializeDefinition(definition, research: new TowerResearchBonuses(.05));
        f.Towers.AddChild(researched); Disable(researched);
        var researchStats = researched.FinalStats;
        researched.GetNode<TowerRecoilController>("Recoil").Kick(); researched.GetNode<TowerRecoilController>("Recoil").Advance(.04);
        Require(researched.FinalStats == researchStats, "Captured research snapshot is not changed by animation");
        f.Towers.RemoveChild(researched); researched.Free();
        f.Towers.RemoveChild(tower); tower.Free();
    }
    // =========================================================
    // The six additional projectile assemblies opt in; beam/radial/passive structures remain unaffected.
    private static void Rollout(AmmoLoopFixture f)
    {
        foreach (string id in new[] { "machine_gun", "sniper", "blaster_cannon", "rocket", "cryo", "stasis" })
        {
            TowerBase tower = f.Add<TowerBase>(id, new(2000, 2064));
            Enemy enemy = f.Target(new(2000, 2000));
            var recoil = tower.GetNode<TowerRecoilController>("Recoil");
            var moving = tower.GetNode<Node2D>("TurretPivot/RecoilRoot");
            var basis = tower.GetNode<Sprite2D>("BaseSprite");
            var pivot = tower.GetNode<Node2D>("TurretPivot");
            var muzzle = tower.GetNode<Marker2D>("TurretPivot/RecoilRoot/Muzzles/MuzzleA");
            var renderer = tower.GetNode<WeaponVisualRenderer>("TurretPivot/WeaponVisuals");
            var placement = tower.Transform; var fixedBase = basis.Transform; var stats = tower.FinalStats;
            Require(tower.ValidateScene() && !recoil.IsProcessing(), id + " validates and sleeps at rest");
            Vector2 launch = muzzle.GlobalPosition;
            tower._PhysicsProcess(1);
            Node2D shot = f.Main.GetNode("World/Projectiles").GetChildren().OfType<Node2D>().Single();
            Vector2 flash = renderer.ToGlobal(renderer.LastFlashPosition); var aim = pivot.Transform;
            Require(recoil.Impulses == 1 && tower.Ammunition.ConsumedRounds == 1 && renderer.TotalFlashes == 1
                && shot.GlobalPosition.IsEqualApprox(launch), id + " funded shot uses the attached barrel tip");
            recoil.Advance(recoil.Style.KickSeconds);
            Require(moving.Position.Y > 0 && muzzle.GlobalPosition != launch
                && shot.GlobalPosition.IsEqualApprox(launch) && renderer.ToGlobal(renderer.LastFlashPosition).IsEqualApprox(flash)
                && tower.Transform == placement && basis.Transform == fixedBase && pivot.Transform == aim
                && ReferenceEquals(stats, tower.FinalStats), id + " only gun/muzzle move; launched effects and combat remain independent");
            tower._PhysicsProcess(.001); Require(recoil.Impulses == 1, id + " cooldown does not kick");
            recoil.Style = (TowerRecoilStyle)recoil.Style.Duplicate();
            Require(!recoil.Active && moving.Position == Vector2.Zero && ReferenceEquals(stats, tower.FinalStats),
                id + " replacing the scene preset resets only presentation");
            var handle = tower.ApplyDisable(TowerDisableReason.EMP);
            Require(!recoil.Active && moving.Position == Vector2.Zero, id + " generic disable restores immediately");
            tower.RemoveDisable(handle);
            tower.Ammunition.Inventory.TryConsume(tower.Ammunition.Inventory.Amount);
            tower._PhysicsProcess(10); Require(recoil.Impulses == 1, id + " empty ammo does not kick");
            tower.SetSelected(true);
            Require(basis.Modulate == tower.GetNode<Sprite2D>("TurretPivot/RecoilRoot/Sprite").Modulate, id + " selection colors both layers");
            recoil.Kick(); recoil.Advance(.01);
            tower.ApplyDamage(new DamageInfo(99999, DamageType.Kinetic));
            Require(!recoil.Active && moving.Position == Vector2.Zero, id + " destruction cancels");
            tower.Free(); enemy.Free(); f.ClearProjectiles();
        }
    }
    // =========================================================
    // Scene-only adoption must remain explicit, including unplaced catalog instances.
    private static void OtherTowers(AmmoLoopFixture f)
    {
        foreach (TowerDefinition definition in f.Placement.Catalog.Definitions)
        {
            Node tower = definition.Scene.Instantiate();
            bool expected = tower is BattleTower or ProjectileTurretBase or RocketTower or BlasterCannon;
            Require(HasRecoil(tower) == expected, "Recoil matches projectile assembly adoption: " + definition.TowerId);
            tower.Free();
        }
    }
    // =========================================================
    // C# type checks work for non-global script classes too; no dependence on native FindChildren class names.
    private static bool HasRecoil(Node node)
    {
        if (node is TowerRecoilController) return true;
        foreach (Node child in node.GetChildren()) if (HasRecoil(child)) return true;
        return false;
    }
    // =========================================================
    // Presentation comparisons use a small float tolerance, never require a renderer or exact elapsed wall time.
    private static void Near(float a, float b, string message) => Require(Math.Abs(a - b) < .002f, message);
    #endregion
}
