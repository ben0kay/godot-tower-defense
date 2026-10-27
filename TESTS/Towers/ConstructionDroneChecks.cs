// CD1/CD2/CD3 production checks: successful paid holds, deterministic multi-Citadel flight, beam, return, safety and bounded updates.
using Godot;
using System;
using System.Diagnostics;
using static AmmoLoopFixture;
public partial class ConstructionDroneChecks : Node
{
    #region Runner and Fixture Helpers
    // =========================================================
    // Explicit central ticks isolate arrival/build time and leave saved maps, catalog resources and profiles untouched.
    public override async void _Ready()
    {
        try
        {
            foreach (string scene in new[] { "res://Main.tscn", "res://MainLevel020.tscn" }) PaidFlow(scene);
            QueuesAndRemoval(); UpgradeFlow(); Fallbacks(); MultiCitadel(); Compatibility(); ResultsFreeze(); Stress();
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: CD1–CD4 paid initial builds and upgrade drone arrival gates, staged art, return, FIFO/cap, pause/fallback, multi-Citadel and cleanup");
            GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    // =========================================================
    // Ordinary subsystem fixtures intentionally disable builders; these dedicated tests explicitly opt into the live feature.
    private static ConstructionDroneManager Enable(AmmoLoopFixture f, int cap = 8)
    {
        SubsystemBattlefield.DetachSession(f.Main);
        // A runtime-only broad foundation accommodates large buildings regardless of future authored maze widths.
        var grid = f.Spawns.Paths.Navigation.Grid;
        for (int y = grid.Config.HeightCells - 12; y < grid.Config.HeightCells - 2; y++)
        for (int x = grid.Config.WidthCells - 16; x < grid.Config.WidthCells - 2; x++)
        {
            Vector2I cell = new(x, y); grid.Level.Passable.EraseCell(cell); grid.Level.Interactive.EraseCell(cell);
            grid.Level.Impassable.SetCell(cell, 1, Vector2I.Zero);
        }
        grid.RebuildTerrain();
        var manager = f.Main.GetNode<ConstructionDroneManager>("World/ConstructionDrones");
        manager.Config = new() { MaxActiveDrones = cap }; return manager;
    }
    // =========================================================
    // Flight releases the specific hold only; work time is advanced separately through its authoritative clock.
    private static void Arrive(ConstructionDroneManager builders, TowerBase tower)
    {
        for (int i = 0; i < 2000 && tower.AwaitingConstructionDrone; i++) builders._PhysicsProcess(.05);
        Require(!tower.AwaitingConstructionDrone && tower.ElapsedWorkSeconds == 0, "Arrival releases without adding work seconds");
    }
    // =========================================================
    // Returning is purely cosmetic and never prevents operational activation.
    private static void Return(ConstructionDroneManager builders)
    {
        for (int i = 0; i < 2000 && (builders.ActiveCount > 0 || builders.PendingCount > 0); i++) builders._PhysicsProcess(.05);
        Require(builders.ActiveCount == 0 && builders.PendingCount == 0 && !builders.IsPhysicsProcessing(), "All return flights retire and manager sleeps");
    }
    #endregion
    #region Paid Flow and Queue
    // =========================================================
    // Real successful placement pays/reserves once before its drone can exist; waiting clocks and artwork stay at zero.
    private void PaidFlow(string scene)
    {
        using AmmoLoopFixture f = new(this, scene); var builders = Enable(f);
        var clock = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        int balance = f.Wallet.GetBalance(f.Wallet.CreditsResource);
        BattleTower tower = f.Purchase<BattleTower>("battle", false);
        int cost = tower.Levels[0].Costs[0].Amount;
        Require(f.Wallet.GetBalance(f.Wallet.CreditsResource) == balance - cost && builders.PendingCount == 1 && builders.ActiveCount == 0,
            "Only funded commit queues drone; payment once");
        Require(tower.AwaitingConstructionDrone && !tower.IsOperational && clock.HeldCount == 1 && !clock.IsPhysicsProcessing(), "Held site sleeps in clock");
        var sites = f.Main.GetNode<ConstructionSiteHologram>("World/Effects/ConstructionSiteHolograms");
        Require(sites.HasSite(tower) && sites.ActiveSites == 1, "CV1 hologram exists while paid site is awaiting drone");
        var status = f.Hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/Heading/WorkSection/Status");
        Require(status.Text == "AWAITING CONSTRUCTION DRONE", "Inspector distinguishes hold");
        var target = f.Target(tower.GlobalPosition + Vector2.Right * 80); int rounds = tower.Ammunition.Inventory.Amount;
        tower._PhysicsProcess(100);
        Require(tower.Ammunition.Inventory.Amount == rounds && target.Health == 10000, "Held tower cannot fire/consume"); target.Free();
        ulong token = tower.ConstructionDroneReservation;
        builders.Call("OnPlaced", tower); Require(builders.PendingCount == 1, "Duplicate notification ignored");
        Require(!tower.ReleaseConstructionDrone(builders, token + 1), "Stale reservation rejected");
        clock.Advance(999); Require(tower.ElapsedWorkSeconds == 0 && clock.LastJobsVisited == 0 && tower.IsBusy, "Even huge clock delta cannot visit/advance held site");
        int revision = f.Spawns.Paths.Navigation.Grid.NavigationRevision; long paths = f.Spawns.Paths.TotalCalculations;
        builders._PhysicsProcess(.01); Require(builders.ActiveCount == 1 && builders.SourceFor(tower) != null, "Nearest home dispatched");
        var drone = builders.DroneFor(tower); Vector2 position = drone.GlobalPosition;
        GetTree().Paused = true; builders._PhysicsProcess(10); clock.Advance(999); GetTree().Paused = false;
        Require(drone.GlobalPosition == position && tower.ElapsedWorkSeconds == 0, "Pause freezes flight and work");
        Arrive(builders, tower);
        var beam = drone.GetNode<ConstructionBeam>("Beam");
        var flashes = builders.GetNode<ConstructionCompletionFlash>("CompletionFlashes");
        Require(drone.BeamVisible && beam.AnimationSeconds > 0 && status.Text == "CONSTRUCTING" &&
            builders.Releases == 1 && clock.HeldCount == 0, "Animated beam deployed before one release");
        int coneBuilds = beam.ConeGeometryRefreshes, scans = beam.ScanRedrawRequests;
        Require(coneBuilds == 1 && scans > 0, "Initial beam builds one reusable cone, independent of scan refreshes");
        for (int i = 0; i < 40; i++) builders._PhysicsProcess(.001);
        Require(beam.ConeGeometryRefreshes == coneBuilds && beam.ScanRedrawRequests - scans <= 2,
            "Hovering builders transform one cached cone while scan redraws remain capped at 24Hz");
        var coneLayer = beam.GetNode<ConstructionBeamCone>("Cone");
        var scanLayer = beam.GetNode<ConstructionBeamScan>("ScanLines");
        Require(coneLayer.Transform == scanLayer.Transform && scanLayer.RedrawRequests == beam.ScanRedrawRequests,
            "Cached cone and animated scans share the identical moving source/target transform");
        Require(coneLayer.Material is ShaderMaterial coneMaterial && coneMaterial.Shader != null &&
            coneMaterial.Shader.ResourcePath.EndsWith("ConstructionBeamGradient.gdshader"),
            "Cached projection uses shared CanvasItem gradient material rather than stacked polygon passes");
        Require(!tower.ReleaseConstructionDrone(builders, token), "Repeated arrival cannot release twice");
        clock.Advance(1); Require(tower.ElapsedWorkSeconds == 1 && !tower.IsOperational, "Ordinary clock starts after arrival");
        clock.Advance(2); Require(tower.IsOperational && !sites.HasSite(tower) && !drone.BeamVisible && builders.ActiveCount == 1 &&
            flashes.ActiveCount == 1 && flashes.IsProcessing(), "Site completion flashes once independently of return");
        Return(builders);
        Require(flashes.ActiveCount == 1, "Returning or pooling the drone does not remove the site flash");
        flashes._Process(.5);
        Require(flashes.ActiveCount == 0 && !flashes.IsProcessing(), "Completion flash expires and renderer sleeps");
        Require(f.Spawns.Paths.TotalCalculations == paths && f.Spawns.Paths.Navigation.Grid.NavigationRevision == revision &&
            f.Wallet.GetBalance(f.Wallet.CreditsResource) == balance - cost, "Flight never changes paths, occupancy or wallet");
        builders.Config.ShowVisuals = false;
        var second = f.Purchase<BattleTower>("battle", false); Arrive(builders, second);
        Require(!builders.DroneFor(second).Visible && builders.Releases == 2 && builders.CreatedVisuals == 1, "Visual OFF retains timing and reuses parked drone");
        clock.Advance(3); Return(builders);
        Require(flashes.ActiveCount == 0, "Visuals-off construction produces no completion flash");
        Require(TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 1, out _) &&
            tower.AwaitingConstructionDrone && clock.HeldCount == 1 && !tower.CanAttack, "Paid upgrade queues a builder even when visuals are off");
        clock.Advance(100); Require(tower.ElapsedWorkSeconds == 0, "Upgrade clock stays held until builder arrival");
        Arrive(builders, tower);
        clock.Advance(tower.RemainingWorkSeconds); Require(tower.UpgradeLevel == 2 && flashes.ActiveCount == 0,
            "Visuals-off upgrade completes from the shared clock without spawning flashes");
        Return(builders);
    }
    // =========================================================
    // Upgrade jobs reuse the flight/beam/return lifecycle and show two staged Battle images without rebuilding a tower.
    private void UpgradeFlow()
    {
        using AmmoLoopFixture f = new(this); var builders = Enable(f);
        var clock = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        BattleTower tower = f.Add<BattleTower>("battle", new Vector2(2200, 2000));
        Sprite2D overlay = tower.GetNode<Sprite2D>(BattleTower.ConstructionSpritePath);
        Sprite2D gun = tower.GetNode<Sprite2D>(BattleTower.GunSpritePath);
        string art = "res://TOWERS/Offensive/BattleTower/Artwork/Upgrades/";
        var status = f.Hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/Heading/WorkSection/Status");
        f.Placement.Call("SelectTower", tower);
        int credits = f.Wallet.GetBalance(f.Wallet.CreditsResource);
        for (int level = 2; level <= 3; level++)
        {
            Require(TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, level - 1, out _)
                && tower.AwaitingConstructionDrone && tower.IsDisabled && tower.PendingUpgradeLevel == level
                && !tower.IsPhysicsProcessing() && builders.PendingCount == 1 && clock.HeldCount == 1,
                "Level " + level + " funded upgrade reserves one sleeping builder hold");
            Require(status.Text.Contains("AWAITING UPGRADE DRONE") && status.Text.Contains(level.ToString()),
                "Inspector distinguishes paused upgrade progress before drone arrival");
            clock.Advance(1000); Require(tower.WorkProgress == 0 && tower.UpgradeLevel == level - 1,
                "Upgrade cannot gain work time before builder beam");
            builders._PhysicsProcess(.01);
            Require(builders.DroneFor(tower) != null && builders.PhaseFor(tower) == ConstructionDronePhase.Flying,
                "Same Citadel flight starts for paid upgrade");
            Arrive(builders, tower);
            Require(builders.DroneFor(tower).BeamVisible && status.Text == "UPGRADING → " + level
                && !tower.CanAttack && clock.HeldCount == 0,
                "Beam release starts upgrade clock but does not enable attacks");
            clock.Advance(level == 2 ? 1.6 : 2.0);
            Require(overlay.Visible && overlay.Texture == GD.Load<Texture2D>(art + "battle_upgrade_l" + level + ".svg"),
                "First upgrade image appears at 35% of work");
            clock.Advance(level == 2 ? 1.4 : 1.5);
            Require(overlay.Texture == GD.Load<Texture2D>(art + "battle_upgrade_l" + level + "_70.svg"),
                "Second upgrade image appears at 70% of work");
            clock.Advance(tower.RemainingWorkSeconds);
            Require(tower.UpgradeLevel == level && tower.CanAttack && !tower.IsDisabled &&
                !overlay.Visible && gun.Texture == GD.Load<Texture2D>(art + "battle_gun_l" + level + ".svg")
                && builders.PhaseFor(tower) == null, "Completed tier activates finished gun; drone returns independently");
            Return(builders);
        }
        Require(credits - f.Wallet.GetBalance(f.Wallet.CreditsResource) == 400,
            "Two upgrade purchases pay 150 and 250 Credits only once");
        builders.Config.ServiceUpgrades = false;
        // The optional toggle preserves the old immediate upgrade clock with no drone allocation.
        var noDrone = f.Add<BattleTower>("battle", new Vector2(2400, 2200));
        Require(TowerUpgradeController.TryUpgrade(noDrone, f.Wallet, f.Placement.Settings, 1, out _) &&
            !noDrone.AwaitingConstructionDrone && builders.PendingCount == 0 && clock.HeldCount == 0,
            "Opting out restores ordinary timed upgrades");
        clock.Advance(noDrone.RemainingWorkSeconds);
        Require(noDrone.UpgradeLevel == 2, "Disabled upgrade drone option cannot strand work");
    }

    // =========================================================
    // FIFO holds remain reserved while outbound/building/returning slots are busy; cancellation promptly frees capacity.
    private void QueuesAndRemoval()
    {
        using AmmoLoopFixture f = new(this); var builders = Enable(f, 1); f.Wallet.Grant(f.Wallet.CreditsResource, 10000);
        var clock = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        var a = f.Purchase<BattleTower>("battle", false); var b = f.Purchase<AmmunitionDepot>("ammo_depot", false);
        var c = f.Purchase<AmmunitionFactory>("ammo_factory", false);
        f.Tick(100); Require(c.ProducedRounds == 0 && c.Production.ProgressSeconds == 0, "Queued factory cannot accumulate income/production");
        builders._PhysicsProcess(.01); Require(builders.DroneFor(a) != null && builders.DroneFor(b) == null && builders.PendingCount == 2, "FIFO cap queues independently sized sites");
        f.Hud.GetBuildButton("battle").EmitSignal(Button.SignalName.Pressed); f.Placement.Call("TryPlace", a.GridCell);
        Require(builders.PendingCount == 2 && builders.ActiveCount == 1, "Rejected placement cannot dispatch");
        var definition = f.Placement.Catalog.Find("battle"); var originalScene = definition.Scene;
        int paid = f.Wallet.GetBalance(f.Wallet.CreditsResource), pending = builders.PendingCount;
        try
        {
            definition.Scene = null;
            f.Placement.Call("TryPlace", new Vector2I(f.Spawns.Paths.Navigation.Grid.Config.WidthCells - 3, f.Spawns.Paths.Navigation.Grid.Config.HeightCells - 3));
            Require(f.Wallet.GetBalance(f.Wallet.CreditsResource) == paid && builders.PendingCount == pending, "Failed commit neither pays nor dispatches");
        }
        finally { definition.Scene = originalScene; }
        a.Free(); builders._PhysicsProcess(.01);
        Require(builders.DroneFor(b) != null && builders.ActiveCount == 1 && builders.PendingCount == 1, "Removal cancels flight and frees FIFO slot");
        Arrive(builders, b); Require(builders.DroneFor(b).BeamVisible && b.Footprint == new Vector2I(2, 2), "2x2 uses shared beam");
        b.ApplyDamage(new DamageInfo(100000, DamageType.Kinetic)); builders._PhysicsProcess(.01);
        Require(builders.DroneFor(c) != null && c.Footprint == new Vector2I(3, 2), "Beaming tower death cancels and starts factory");
        builders.Config.MaxActiveDrones = 0; // Sanitizes to one and must not lose the existing job.
        Arrive(builders, c); clock.Advance(c.WorkSeconds); Return(builders);
        Require(c.IsOperational && builders.CreatedVisuals == 1, "Factory complete; cap edits never lose work");
        var d = f.Purchase<BattleTower>("battle", false);
        builders.Config.Enabled = false; builders._PhysicsProcess(.01);
        Require(!d.AwaitingConstructionDrone && d.ElapsedWorkSeconds == 0, "Feature OFF releases queued site, not work time");
        clock.Advance(d.WorkSeconds); Require(d.IsOperational, "Disabled feature preserves original construction");
    }
    #endregion
    #region Fallback and Compatibility
    // =========================================================
    // Missing art/home is never a permanent paid-site deadlock in ordinary mode; strict mode retries without unattended work.
    private void Fallbacks()
    {
        using AmmoLoopFixture f = new(this); var builders = Enable(f); f.Wallet.Grant(f.Wallet.CreditsResource, 10000);
        var clock = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        var validScene = builders.DroneScene; builders.DroneScene = null;
        var first = f.Purchase<BattleTower>("battle", false); builders._PhysicsProcess(.01);
        Require(!first.AwaitingConstructionDrone && builders.CreatedVisuals == 0, "Missing scene fallback releases"); clock.Advance(3);
        builders.Config.RequireDroneToBuild = true;
        var strict = f.Purchase<BattleTower>("battle", false); builders._PhysicsProcess(1); clock.Advance(100);
        Require(strict.AwaitingConstructionDrone && strict.ElapsedWorkSeconds == 0, "Strict missing-scene remains held");
        using (var invalidScene = new PackedScene())
        {
            var wrong = new Node2D(); invalidScene.Pack(wrong); wrong.Free(); builders.DroneScene = invalidScene;
            builders._PhysicsProcess(1); Require(strict.AwaitingConstructionDrone && builders.CreatedVisuals == 0, "Wrong scene type safely retries in strict mode");
        }
        builders.DroneScene = validScene; Arrive(builders, strict); clock.Advance(3); Return(builders);
        var outbound = f.Purchase<BattleTower>("battle", false); builders.Config.RequireDroneToBuild = false; builders._PhysicsProcess(.01);
        var source = builders.SourceFor(outbound); Require(source != null, "Outbound home captured"); source.Health.TakeDamage(int.MaxValue);
        builders._PhysicsProcess(.01); Require(!outbound.AwaitingConstructionDrone, "Source destruction en route uses fallback"); clock.Advance(3);
        var noHome = f.Purchase<BattleTower>("battle", false); builders._PhysicsProcess(1);
        Require(!noHome.AwaitingConstructionDrone && builders.ActiveCount == 0, "No available home fallback");
        clock.Advance(3); Require(noHome.IsOperational, "No-home paid site not stranded");
    }
    // =========================================================
    // Actual two-Citadel registration, stable tie, custom marker, unavailable source and return rerouting.
    private void MultiCitadel()
    {
        using MultiCitadelChecks.Fixture f = new(this);
        var builders = f.Main.GetNode<ConstructionDroneManager>("World/ConstructionDrones"); builders.Config = new();
        var first = f.Session.Objectives[0]; var second = f.Session.Objectives[1];
        var markerA = new Marker2D { Name = "DroneLaunch", Position = new(64, 64) }; first.Root.AddChild(markerA);
        var markerB = new Marker2D { Name = "DroneLaunch", Position = new(64, 64) }; second.Root.AddChild(markerB);
        Require(builders.NearestHome(markerA.GlobalPosition) == first && builders.NearestHome(markerB.GlobalPosition) == second, "Nearest available home per target");
        Vector2 midpoint = (markerA.GlobalPosition + markerB.GlobalPosition) * .5f;
        Require(builders.NearestHome(midpoint) == first, "Stable authored number breaks tie");
        // Runtime placement on fixture's passable corridors is deliberately narrow; use a held production tower at a fixed site for source lifecycle checks.
        var clock = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        var tower = GD.Load<PackedScene>("res://TOWERS/Offensive/BattleTower/BattleTower.tscn").Instantiate<BattleTower>();
        tower.StartConstruction(clock, builders); f.Main.GetNode("World/Towers").AddChild(tower); tower.GlobalPosition = markerB.GlobalPosition + Vector2.Down * 48; Disable(tower);
        builders.Call("OnPlaced", tower); builders._PhysicsProcess(.01);
        Require(builders.SourceFor(tower) == second && builders.DroneFor(tower).GlobalPosition == markerB.GlobalPosition, "Authored launch marker used");
        Arrive(builders, tower); second.Health.TakeDamage(int.MaxValue); clock.Advance(3);
        Require(tower.IsOperational && builders.ActiveCount == 1, "Source lost during beam cannot block normal completion"); Return(builders);
        Require(builders.NearestHome(markerB.GlobalPosition) == first, "Destroyed home excluded; return uses survivor");
        first.Root.QueueFree(); Require(builders.NearestHome(midpoint) == null, "Queued removal excluded immediately");
    }
    // =========================================================
    // Zero duration and direct scenes stay instant/operational; removing the manager releases live orphan holds safely.
    private void Compatibility()
    {
        using AmmoLoopFixture f = new(this); var builders = Enable(f);
        var clock = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        var zero = new PassiveTestTower { Config = new TowerConfig { ConstructionSeconds = 0 } };
        zero.StartConstruction(clock, builders); f.Main.GetNode("World/Towers").AddChild(zero);
        Require(zero.IsOperational && !zero.AwaitingConstructionDrone && clock.ActiveCount == 0, "Zero-second has no hold/job");
        var direct = f.Add<BattleTower>("battle", Vector2.Zero); Require(direct.IsOperational && builders.PendingCount == 0, "Direct scene opt-out");
        var tower = f.Purchase<BattleTower>("battle", false);
        var independent = ConstructionChecks.Site(f.Main.GetNode("World/Towers"), clock, 1);
        clock.Advance(.5);
        Require(independent.ElapsedWorkSeconds == .5 && tower.ElapsedWorkSeconds == 0, "Held site cannot suspend unrelated active work");
        independent.Free(); builders.Free();
        Require(!tower.AwaitingConstructionDrone && tower.IsBusy && !tower.IsOperational, "Service removal releases hold without activating site");
        clock.Advance(3); Require(tower.IsOperational, "Orphan work remains completable");
    }
    // =========================================================
    // Actual mission defeat pauses both services; unfinished held sites cannot secretly progress behind Results.
    private void ResultsFreeze()
    {
        using MissionFixture f = new(this);
        var builders = f.Main.GetNode<ConstructionDroneManager>("World/ConstructionDrones");
        var clock = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        var tower = new PassiveTestTower { Config = new TowerConfig { ConstructionSeconds = 10 } };
        tower.StartConstruction(clock, builders); f.Main.GetNode("World/Towers").AddChild(tower); tower.GlobalPosition = new(2000, 2000);
        builders.Call("OnPlaced", tower); builders._PhysicsProcess(.01);
        f.Health.TakeDamage(int.MaxValue); builders._PhysicsProcess(1000); clock.Advance(1000);
        Require(f.Session.State == LevelSessionState.Defeat && GetTree().Paused && tower.ElapsedWorkSeconds == 0,
            "Results freezes held flight and work");
    }
    #endregion
    #region Performance
    // =========================================================
    // Twenty simultaneous real paid builds use bounded central motion/pool, with no ground navigation or tick allocations.
    private void Stress()
    {
        using AmmoLoopFixture f = new(this); var builders = Enable(f, 20); f.Wallet.Grant(f.Wallet.CreditsResource, 10000);
        builders.Config.DispatchesPerTick = 16;
        for (int i = 0; i < 20; i++) f.Purchase<BattleTower>("battle", false);
        builders._PhysicsProcess(.01); builders._PhysicsProcess(.01);
        Require(builders.ActiveCount == 20 && builders.CreatedVisuals == 20, "Bounded twenty concurrent builds");
        long paths = f.Spawns.Paths.TotalCalculations;
        builders._PhysicsProcess(.001); long start = GC.GetAllocatedBytesForCurrentThread(); var timer = Stopwatch.StartNew();
        for (int i = 0; i < 120; i++) builders._PhysicsProcess(.001);
        timer.Stop(); long bytes = GC.GetAllocatedBytesForCurrentThread() - start;
        GD.Print($"BUILDERS 20 active/120 central ticks: {timer.Elapsed.TotalMilliseconds:0.###}ms total; managedBytes={bytes}; visualNodes={builders.CreatedVisuals}; no rendered GPU timing");
        Require(bytes < 4096 && f.Spawns.Paths.TotalCalculations == paths, "No per-frame managed allocation or A* work");
        for (int i = 0; i < 3000 && builders.Releases < 20; i++) builders._PhysicsProcess(.05);
        Require(builders.Releases == 20, "All twenty beams arrive independently without clock progress");
        builders._PhysicsProcess(.001); start = GC.GetAllocatedBytesForCurrentThread(); timer.Restart();
        for (int i = 0; i < 600; i++) builders._PhysicsProcess(1d / 60);
        timer.Stop(); bytes = GC.GetAllocatedBytesForCurrentThread() - start;
        GD.Print($"BUILDERS 20 beams/600 central ticks: {timer.Elapsed.TotalMilliseconds:0.###}ms total; managedBytes={bytes}; no rendered GPU timing");
        Require(bytes < 4096, "Beaming has no per-frame managed allocation");
        builders.Config.MaxActiveDrones = 1;
        var queued = f.Purchase<BattleTower>("battle", false);
        Require(queued.AwaitingConstructionDrone && builders.ActiveCount == 20, "Reduced cap preserves in-flight assignments");
        builders.Config.Enabled = false; builders._PhysicsProcess(.01);
        Require(builders.PendingCount == 0 && builders.ActiveCount == 0 && !queued.AwaitingConstructionDrone, "Disable drains all holds");
    }
    #endregion
}
