// Pure state-machine checks: production, independent depots and conservative source/cargo/destination transfers.
using System;

public static class AmmoFoundationChecks
{
    // =========================================================
    // Runs inside the established headless EnemyIntegrationChecks suite without spawning any nodes.
    public static void Run()
    {
        AmmoInventory tower = new(1000, 1000);
        Require(!tower.TryConsume(1001) && tower.Amount == 1000, "Cannot overspend weapon ammo");
        Require(tower.TryConsume(900) && tower.Amount == 100, "Weapon ammo is per-instance finite");
        AmmunitionMasterConfig config = new();
        Require(config.NeedsResupply(tower.Amount, tower.Capacity)
            && !config.NeedsResupply(101, 1000) && !config.NeedsResupply(0, 0),
            "Global 10% threshold includes 100/1000, excludes 101/1000 and disabled ammo");
        Require(!tower.TrySetCapacity(99) && tower.Capacity == 1000,
            "Capacity cannot shrink below stock or delete rounds");
        Require(tower.TrySetCapacity(1200) && tower.Amount == 100, "Capacity changes preserve stock");

        AmmoInventory src = new(200, 150), cargo = new(100), dst = new(300);
        int changes = 0;
        src.Changed += () => changes++;
        Require(!src.TryTransferTo(dst, -1) && !src.TryTransferTo(src, 1)
            && !src.TryTransferTo(cargo, 101) && src.Amount == 150 && cargo.Amount == 0,
            "Bad or oversize transfers leave both inventories unchanged");
        Require(src.TryTransferTo(cargo, 100) && cargo.TryTransferTo(dst, 100)
            && src.Amount == 50 && cargo.Amount == 0 && dst.Amount == 100 && changes == 1,
            "Two-leg cargo move conserves stock and publishes source event once");

        AmmoProduction factory = new(100, 2, 250);
        AmmoStorage depot = new(300);
        Require(factory.Advance(1) == 0 && factory.Buffer.Amount == 0, "Production timer is not instant");
        Require(factory.Advance(4) == 200 && factory.Buffer.Amount == 200 && factory.IsPaused,
            "Multiple completed batches fill finite buffer then pause");
        Require(factory.Advance(20) == 0 && factory.Buffer.Amount == 200, "Full-buffer production cannot mint ammunition");
        AmmoInventory outbound = new(100);
        Require(factory.Buffer.TryTransferTo(outbound, 100) && !factory.IsPaused
            && depot.TryReceiveFrom(outbound, 100)
            && depot.CurrentStock == 100 && outbound.Amount == 0 && factory.Buffer.Amount == 100,
            "Factory buffer -> cargo -> depot preserves stock");
        Require(factory.Advance(1.9) == 0 && factory.Advance(0.1) == 100 && factory.IsPaused,
            "Factory resumes only after a full fresh interval when space frees");
        Require(!depot.TryLoadInto(new AmmoInventory(10), 20) && depot.CurrentStock == 100,
            "Depot rejects cargo larger than its free drone payload");
        Require(depot.TryLoadInto(outbound, 75) && outbound.Amount == 75 && depot.CurrentStock == 25,
            "Depot loads actual stock for outbound tower flight");
        // Reuse a nearly full target to prove no partial mutation on overflow.
        AmmoInventory nearlyFullTower = new(1000, 990);
        Require(!outbound.TryTransferTo(nearlyFullTower, 75) && nearlyFullTower.Amount == 990,
            "Destination capacity prevents overfill without stock loss");
        Require(outbound.TryTransferTo(tower, 75) && tower.Amount == 175 && outbound.Amount == 0,
            "Tower replenishment occurs only when simulated drone unloads");
        Require(!factory.Buffer.TryTransferTo(null, 10) && !factory.Buffer.TryConsume(0),
            "Null destinations and zero consumption are rejected");
        RequireThrows(() => new AmmoProduction(0, 2, 100), "Invalid batch sizes rejected");
        RequireThrows(() => factory.Advance(-1), "Negative time rejected");
    }

    // =========================================================
    private static void Require(bool ok, string reason)
    {
        if (!ok) throw new InvalidOperationException("Ammunition foundation: " + reason);
    }

    // =========================================================
    private static void RequireThrows(Action action, string reason)
    {
        try { action(); }
        catch (ArgumentOutOfRangeException) { return; }
        throw new InvalidOperationException("Ammunition foundation: " + reason);
    }
}
