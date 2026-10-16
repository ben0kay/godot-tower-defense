// Shipment reservation, scarce-stock fairness, interrupted flight and conservation regressions.
using Godot;
using System.Linq;
using static AmmoLoopFixture;

internal static class DeliverySafetyChecks
{
    // =========================================================
    // Two factories must reserve only actual remaining depot space, never race to overfill it.
    internal static void Run(Node parent)
    {
        using (AmmoLoopFixture f = new(parent))
        {
            AmmunitionFactory a = f.Add<AmmunitionFactory>("ammo_factory", new(1800, 2000));
            AmmunitionFactory b = f.Add<AmmunitionFactory>("ammo_factory", new(2200, 2000));
            AmmunitionDepot depot = f.Add<AmmunitionDepot>("ammo_depot", new(2000, 2000), c => ((AmmunitionDepotConfig)c).StorageCapacity = 2000);
            a._PhysicsProcess(20); b._PhysicsProcess(20); f.Dispatch();
            Require(f.Logistics.Jobs.Count == 2 && f.Logistics.ReservedSpace(depot) == 2000 && depot.Inventory.Amount == 0
                && f.Logistics.Jobs.All(j => j.Cargo.Amount == 1000), "Two full factory shipments reserve exactly 2000 incoming rounds");
            foreach (AmmoDeliveryJob job in f.Logistics.Jobs.ToArray()) { f.Arrive(job); f.Return(job); }
            a._PhysicsProcess(20); b._PhysicsProcess(20); f.Dispatch();
            Require(depot.Inventory.Amount == 2000 && f.Logistics.Jobs.Count == 0 && f.AccountedRounds() == 4000,
                "Full depot prevents further factory trips without stock duplication/loss");
        }
        // 900 buffered rounds must NOT launch early; insufficient destination space must NOT cause partial flights.
        using (AmmoLoopFixture f = new(parent))
        {
            AmmunitionFactory factory = f.Add<AmmunitionFactory>("ammo_factory", new(1800, 2000));
            AmmunitionDepot depot = f.Add<AmmunitionDepot>("ammo_depot", new(2000, 2000), c => ((AmmunitionDepotConfig)c).StorageCapacity = 1500);
            Require(factory.Config.ShipmentAmount == 1000 && factory.Config.DronePayload == 1000
                && factory.Inventory.Capacity == 1000 && depot.Inventory.Capacity == 1500,
                "Configured shipment fits the factory buffer and drone");
            factory._PhysicsProcess(18); f.Dispatch();
            Require(factory.Inventory.Amount == 900 && f.Logistics.Jobs.Count == 0, "Factory holds stock under 1000; no early trip");
            Require(depot.Inventory.TryAdd(600), "Preload depot to leave only 900 free");
            factory._PhysicsProcess(2); f.Dispatch();
            Require(factory.Inventory.Amount == 1000 && f.Logistics.Jobs.Count == 0,
                "Factory holds complete 1000-round cargo when depot has fewer than 1000 unreserved slots");
            AmmunitionDepot second = f.Add<AmmunitionDepot>("ammo_depot", new(2200, 2000));
            Require(second.Inventory.Capacity == 10000, "Unmodified depot defaults to 10000 storage");
            f.Dispatch();
            Require(f.Logistics.Jobs.Count == 1 && f.Logistics.Jobs[0].Destination == second
                && f.Logistics.Jobs[0].Cargo.Amount == 1000 && f.Logistics.ReservedSpace(second) == 1000,
                "Nearest depot without a full 1000 slots is skipped in favour of one with sufficient capacity");
        }
        using (AmmoLoopFixture f = new(parent))
        {
            AmmunitionDepot a = f.Add<AmmunitionDepot>("ammo_depot", new(1900, 2000), c => ((AmmunitionDepotConfig)c).DroneSlots = 1);
            AmmunitionDepot b = f.Add<AmmunitionDepot>("ammo_depot", new(2100, 2000), c => ((AmmunitionDepotConfig)c).DroneSlots = 1);
            a.Inventory.TryAdd(25); b.Inventory.TryAdd(25);
            for (int i = 0; i < 3; i++) EmptyTower(f, new(2000 + 20 * i, 2100));
            f.Dispatch(); f.Dispatch();
            Require(f.Logistics.Jobs.Count == 2 && f.Logistics.Jobs.Select(j => j.Destination).Distinct().Count() == 2 && a.Inventory.Amount + b.Inventory.Amount == 0,
                "Two depots serve distinct requests without overspending scarce stock");
            foreach (AmmoDeliveryJob job in f.Logistics.Jobs.ToArray()) { f.Arrive(job); f.Return(job); }
            Require(f.AccountedRounds() == 50 && f.Logistics.PendingRequests == 1, "Unfulfilled third request waits and conserved stock reaches first two towers");
            a.Inventory.TryAdd(20); f.Dispatch(); Require(f.Logistics.Jobs.Count == 1, "Waiting request retries when stock arrives");
        }
        foreach (string interruption in new[] { "destination", "full-home", "source", "drone", "capacity" })
        {
            using AmmoLoopFixture f = new(parent);
            AmmunitionDepot depot = f.Add<AmmunitionDepot>("ammo_depot", new(1900, 2000), c => ((AmmunitionDepotConfig)c).StorageCapacity = 100);
            depot.Inventory.TryAdd(100); BattleTower tower = EmptyTower(f, new(2100, 2000)); f.Dispatch();
            AmmoDeliveryJob job = f.Logistics.Jobs.Single();
            if (interruption is "destination" or "full-home")
            {
                tower.Free(); Require(f.Logistics.ReservedSpace(tower) == 0 && job.Drone.Returning, "Removed destination releases incoming reservation");
                if (interruption == "full-home") depot.Inventory.TryAdd(100);
                f.Return(job);
                Require(f.Logistics.Jobs.Count == 0 && f.Logistics.BusySlots(depot) == 0 && depot.Inventory.Amount == 100,
                    "Returned cargo/home slot reconcile after missing destination");
                Require(f.Logistics.LostRounds == (interruption == "full-home" ? 100 : 0), "Full-home cargo loss is explicit and counted");
            }
            else if (interruption == "source")
            {
                depot.Free(); Require(f.Logistics.Jobs.Count == 0 && f.Logistics.LostRounds == 100 && tower.Ammunition.Inventory.Amount == 0,
                    "Source destruction loses cargo once and cancels its flight");
            }
            else if (interruption == "drone")
            {
                job.Drone.Free(); Require(f.Logistics.Jobs.Count == 0 && f.Logistics.LostRounds == 100 && f.Logistics.BusySlots(depot) == 0,
                    "External drone destruction loses cargo once and releases its slot");
            }
            else
            {
                tower.Config.SetLevelStats(1, tower.Config.Levels[0].Stats with { Ammunition = new(10, 1) });
                f.Arrive(job); f.Return(job);
                Require(tower.Ammunition.Inventory.Amount == 10 && depot.Inventory.Amount == 90 && f.Logistics.LostRounds == 0,
                    "Capacity changes accept only current space and return all surplus");
            }
        }
        // Dispose with active outgoing cargo: no stale event subscriptions, jobs or callbacks survive world unload.
        using (AmmoLoopFixture f = new(parent))
        {
            AmmunitionDepot depot = f.Add<AmmunitionDepot>("ammo_depot", new(1900, 2000)); depot.Inventory.TryAdd(100);
            EmptyTower(f, new(2100, 2000)); f.Dispatch(); Require(f.Logistics.Jobs.Count == 1, "Unload fixture contains active cargo");
        }
        using (AmmoLoopFixture f = new(parent))
        {
            int balance = f.Wallet.GetBalance(f.Wallet.CreditsResource);
            f.Wallet.TryPurchase(new Godot.Collections.Array<ResourceCost> { new ResourceCost { Resource = f.Wallet.CreditsResource, Amount = balance } }, () => true);
            WorldGrid grid = f.Main.GetNode<WorldGrid>("World/Grid");
            Vector2I cell = grid.Level.Passable.GetUsedCells().First(c => grid.PlacementFailure(c).Length == 0 && f.Spawns.PlacementFailure(c).Length == 0);
            f.Hud.GetBuildButton("ammo_factory").EmitSignal(Button.SignalName.Pressed); f.Placement.Call("TryPlace", cell);
            Require(f.Towers.GetChildCount() == 0 && f.Wallet.GetBalance(f.Wallet.CreditsResource) == 0 && grid.PlacementFailure(cell).Length == 0,
                "Unaffordable building creates no stock, structure or occupancy");
        }
        GD.Print("PASS: multiple factories/depots, scarce-stock requests, capacity reservations, full storage/home, endpoint/drone destruction, surplus return and active-flight unload");
    }

    // =========================================================
    // Empty creation is an explicit per-instance setting, not a later free refill.
    private static BattleTower EmptyTower(AmmoLoopFixture f, Vector2 position) => f.Add<BattleTower>("battle", position, c =>
    { c.InitialAmmoFraction = 0; c.SetLevelStats(1, c.Levels[0].Stats with { Ammunition = new(100, 1) }); });
}
