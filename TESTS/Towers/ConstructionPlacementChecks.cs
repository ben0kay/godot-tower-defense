// C2 production scenes, paid rectangles and endpoint gates; no timer bypass or construction artwork is assumed.
using Godot;
using System;
using System.Linq;
using static AmmoLoopFixture;

internal static class ConstructionPlacementChecks
{
    // =========================================================
    // Real placement claims/charges once and remains selectable without changing navigation on completion.
    internal static void Run(Node runner)
    {
        foreach (string scene in new[] { "res://Main.tscn", "res://MainLevel002.tscn" })
        using (AmmoLoopFixture f = new(runner, scene))
        {
            ConstructionWorkManager clock = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
            var grid = f.Main.GetNode<WorldGrid>("World/Grid");
            int initialCredits = f.Wallet.GetBalance(f.Wallet.CreditsResource);
            AmmunitionFactory factory = f.Purchase<AmmunitionFactory>("ammo_factory", false);
            AmmunitionDepot depot = f.Purchase<AmmunitionDepot>("ammo_depot", false);
            BattleTower tower = f.Purchase<BattleTower>("battle", false);
            int money = f.Wallet.GetBalance(f.Wallet.CreditsResource), revision = grid.NavigationRevision;
            Require(money == initialCredits - 550 && clock.ActiveCount == 3 && factory.WorkSeconds == 8 && depot.WorkSeconds == 5 && tower.WorkSeconds == 3, "Both launchers start exact positive paid jobs");
            foreach (TowerBase building in new TowerBase[] { factory, depot, tower })
            {
                Require(building.IsBusy && !building.IsOperational && !building.IsPhysicsProcessing() && building.UpgradeLevel == 1
                    && building.RankProgress.TotalXp == 0, "Ready cannot activate unfinished buildings");
                for (int y = 0; y < building.Footprint.Y; y++) for (int x = 0; x < building.Footprint.X; x++)
                    Require(f.Placement.TowerAt(building.GridCell + new Vector2I(x, y)) == building && grid.IsOccupied(building.GridCell + new Vector2I(x, y)), "Whole footprint occupied/indexed immediately");
            }
            Require(!TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 1, out _) && f.Wallet.GetBalance(f.Wallet.CreditsResource) == money, "Cannot buy upgrade while constructing");
            Require(f.Hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/WorkSection/Status").Text.Contains("CONSTRUCTING"), "Selected unfinished site shows minimal status");
            f.Tick(100);
            Require(factory.ProducedRounds == 0 && factory.Production.ProgressSeconds == 0 && factory.Inventory.Amount == 0 && f.Logistics.Jobs.Count == 0, "Unfinished factory cannot bank production time");
            factory.Inventory.TryAdd(1000); depot.Inventory.TryAdd(1000); tower.Ammunition.Inventory.TryConsume(1000);
            f.Logistics.RegisterBuilding(factory); f.Logistics.RegisterBuilding(depot); f.Logistics.RegisterConsumer(tower); f.Dispatch();
            Require(f.Logistics.Jobs.Count == 0 && f.Logistics.PendingRequests == 0 && f.Logistics.ReservedSpace(depot) == 0
                && f.Logistics.ReservedSpace(tower) == 0, "Inactive endpoints cannot register/request/reserve even with seeded stock");
            clock.Advance(3); f.Dispatch();
            Require(tower.IsOperational && f.Logistics.PendingRequests == 1 && f.Logistics.Jobs.Count == 0, "Ready consumer waits for unfinished depot");
            clock.Advance(2); f.Dispatch();
            AmmoDeliveryJob delivery = f.Logistics.Jobs.Single();
            Require(delivery.Source == depot && delivery.Destination == tower && !factory.IsOperational, "Depot activates once and automatically serves existing demand");
            f.Arrive(delivery); f.Return(delivery); clock.Advance(3); f.Dispatch();
            AmmoDeliveryJob factoryTrip = f.Logistics.Jobs.Single(j => j.Source == factory);
            Require(factoryTrip.Cargo.Amount == 1000 && depot.Inventory.Capacity == 10000 && factory.ProducedRounds == 0, "Completion does not create ammo; full1000 factory shipments and10000 depot capacity preserved");
            f.Arrive(factoryTrip); f.Return(factoryTrip); factory._PhysicsProcess(2);
            Require(factory.ProducedRounds == 100 && factory.Inventory.Amount == 100 && grid.NavigationRevision == revision
                && f.Wallet.GetBalance(f.Wallet.CreditsResource) == money && clock.ActiveCount == 0, "Completion resumes100/2s production, no payment/nav mutation");
            foreach (TowerBase building in new TowerBase[] { factory, depot, tower }) Disable(building);
        }
        Weapons(runner); FailuresAndFoundations(runner);
        GD.Print("PASS: C2 both launchers, paid multi-cell sites, selection/status, dormant production/dispatch/delivery and all weapon gates");
    }

    // =========================================================
    // Each real offensive type cannot acquire/fire/consume while building; its normal attack works after completion.
    private static void Weapons(Node runner)
    {
        foreach (string id in new[] { "battle", "laser", "blaster_cannon", "shockwave", "sniper", "machine_gun" })
        using (AmmoLoopFixture f = new(runner))
        {
            TowerBase tower = f.Purchase<TowerBase>(id, false);
            Enemy target = f.Target(tower.GlobalPosition + new Vector2(80, 0));
            // This isolated stationary target need not have a valid authored route; focus on capability gating.
            switch (tower.AuthoredConfig)
            {
                case BattleTowerConfig battle: battle.TargetPriority = TowerTargeting.Priority.Closest; break;
                case LaserTowerConfig laser: laser.TargetPriority = TowerTargeting.Priority.Closest; break;
                case BlasterCannonConfig blaster: blaster.TargetPriority = TowerTargeting.Priority.Closest; break;
                case ProjectileTurretConfig turret: turret.TargetPriority = TowerTargeting.Priority.Closest; break;
            }
            int stock = tower.Ammunition.Inventory.Amount; TowerFinalStats stats = tower.FinalStats;
            TickWeapon(tower, 10);
            Require(tower.Ammunition.Inventory.Amount == stock && target.Health == 10000 && f.Main.GetNode("World/Projectiles").GetChildCount() == 0,
                id + " has no attacks or consumption while constructing");
            var clock = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork"); clock.Advance(tower.WorkSeconds);
            Require(tower.IsOperational && tower.FinalStats == stats && tower.UpgradeLevel == 1 && tower.RankProgress.TotalXp == 0, id + " activates without stat/XP/level mutation");
            Disable(tower);
            for (int i = 0; i < 300; i++) TickWeapon(tower, 1.0 / 60);
            Require(tower.Ammunition.Inventory.Amount < stock, id + " resumes normal weapon operation");
        }
    }

    // =========================================================
    // Call each concrete weapon's production physics callback explicitly.
    private static void TickWeapon(TowerBase tower, double delta)
    {
        switch (tower)
        {
            case BattleTower battle: battle._PhysicsProcess(delta); break;
            case LaserTower laser: laser._PhysicsProcess(delta); break;
            case BlasterCannon blaster: blaster._PhysicsProcess(delta); break;
            case ShockwaveTower wave: wave._PhysicsProcess(delta); break;
            case ProjectileTurretBase turret: turret._PhysicsProcess(delta); break;
        }
    }

    // =========================================================
    // Foundation distinction, cancellation and invalid source duration keep wallet/claims/jobs atomic.
    private static void FailuresAndFoundations(Node runner)
    {
        using FootprintFixture f = new(runner);
        ConstructionWorkManager clock = f.World.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        foreach (bool solid in new[] { false, true })
        {
            f.Paint(new(f.Anchor, new(3, 2)), solid); f.Grid.RebuildTerrain(); f.Nav.Refresh();
            int before = f.Grid.NavigationRevision;
            TowerBase tower = f.Build("ammo_factory", f.Anchor); Require(tower != null && tower.IsBusy, "Paid site on homogeneous foundation");
            int built = f.Grid.NavigationRevision; clock.Advance(8);
            Require(f.Grid.NavigationRevision == built && (solid ? built == before : built > before), "Only passable placement changes navigation; completion never does");
            tower.Free(); Require(f.Occupied(f.Anchor, new(3, 2)) == 0 && clock.ActiveCount == 0, "Whole footprint released once");
            f.World.Wallet.Initialize(new LevelConfig { StartingCredits = 1000 });
        }
        TowerBase removed = f.Build("ammo_factory", f.Anchor); int completed = 0; removed.WorkCompleted += () => completed++;
        removed.Free(); clock.Advance(99);
        Require(completed == 0 && clock.ActiveCount == 0 && f.Occupied(f.Anchor, new(3, 2)) == 0, "Free during work retires job and every cell");
        f.World.Wallet.Initialize(new LevelConfig { StartingCredits = 1000 });
        TowerDefinition definition = f.World.Placement.Catalog.Find("ammo_factory");
        double duration = definition.DefaultConfig.ConstructionSeconds;
        try
        {
            definition.DefaultConfig.ConstructionSeconds = double.NaN;
            int balance = f.Balance; TowerBase rejected = f.Build("ammo_factory", f.Anchor);
            Require(rejected == null && f.Balance == balance && clock.ActiveCount == 0 && f.Occupied(f.Anchor, new(3, 2)) == 0, "Invalid duration rolls back claimed footprint and job before payment");
            definition.DefaultConfig.ConstructionSeconds = 0;
            TowerBase instant = f.Build("ammo_factory", f.Anchor);
            Require(instant.IsOperational && clock.ActiveCount == 0, "Explicit zero-duration passive config has no delayed gap"); instant.Free();
        }
        finally { definition.DefaultConfig.ConstructionSeconds = duration; }
    }
}
