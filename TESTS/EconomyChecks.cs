// Production wallet and placement transaction checks, including failure rollback.
using Godot;
using System;

public static class EconomyChecks
{
    // =========================================================
    // Validate session initialization, atomic recipes, HUD signals and actual tower purchases.
    public static void Run(Node battle, WorldGrid grid)
    {
        EconomyManager wallet = battle.GetNode<EconomyManager>("Economy");
        ResourceDefinition credits = wallet.CreditsResource;
        GameplayHUD hud = battle.GetNode<GameplayHUD>("UI/GameplayHUD");
        Label label = hud.GetNode<Label>("TopBar/Status/Credits");
        Require(wallet.GetBalance(credits) == grid.Level.Settings.StartingCredits, "Live level seeds its wallet");
        Require(label.Text == $"Credits: {wallet.GetBalance(credits):N0}", "HUD starts with the actual credit balance");
        wallet.Initialize(new LevelConfig { StartingCredits = 700 });
        Require(wallet.GetBalance(credits) == 700, "First level uses its starting amount");
        ResourceDefinition alloy = new() { ResourceId = "alloy", DisplayName = "Alloy" };
        Require(wallet.Grant(alloy, 10), "Future materials use the same wallet");
        wallet.Initialize(new LevelConfig { StartingCredits = 400 });
        Require(wallet.GetBalance(credits) == 400 && wallet.GetBalance(alloy) == 0, "New level resets credits and secondary resources");

        var duplicate = Costs((credits, 100), (credits, 150));
        Require(wallet.TrySpend(duplicate) && wallet.GetBalance(credits) == 150, "Duplicate IDs charge the combined amount once");
        Require(!wallet.TrySpend(Costs((credits, 100), (alloy, 1))) && wallet.GetBalance(credits) == 150,
            "Insufficient secondary material causes no partial deduction");
        Require(!wallet.TrySpend(Costs((credits, -1))) && !wallet.TrySpend(Costs((null, 0))),
            "Negative and null-definition entries are rejected");
        Require(!wallet.TrySpend(Costs((credits, int.MaxValue), (credits, 1))), "Duplicate cost overflow is rejected");
        Require(wallet.TrySpend(Costs((credits, 0))) && !wallet.Grant(credits, -1), "Zero costs are safe and negative grants are rejected");
        Require(!wallet.Grant(credits, int.MaxValue) && wallet.GetBalance(credits) == 150, "Overflowing grants change nothing");
        Require(wallet.Grant(alloy, 3), "Material grant works");
        bool allCommittedAtEvent = false;
        void Changed(string id, int amount)
        {
            if (id == "credits") allCommittedAtEvent = wallet.GetBalance(credits) == 100 && wallet.GetBalance(alloy) == 1;
        }
        wallet.BalanceChanged += Changed;
        Require(wallet.TrySpend(Costs((credits, 50), (alloy, 2))) && allCommittedAtEvent, "Events observe the fully committed multi-resource payment");
        wallet.BalanceChanged -= Changed;
        Require(hud.TargetCredits == 100 && label.Text.StartsWith("Credits: "), "Credit target updates without polling while the visual number counts");
        Require(!wallet.TryPurchase(Costs((credits, 50)), () => false) && wallet.GetBalance(credits) == 100,
            "Failed build callback leaves funds untouched");
        try { wallet.TryPurchase(Costs((credits, 50)), () => throw new InvalidOperationException("Test creation failure")); }
        catch (InvalidOperationException) { }
        Require(wallet.GetBalance(credits) == 100 && wallet.CanAfford(Costs((credits, 100))), "Thrown callback leaves balance and transaction guard intact");
        Require(wallet.TryPurchase(Costs((credits, 50)), () =>
            !wallet.TrySpend(Costs((credits, 1))) && !wallet.Grant(credits, 1)), "Reentrant wallet mutation is blocked during build commit");

        TowerPlacementController placement = battle.GetNode<TowerPlacementController>("TowerPlacement");
        TowerDefinition definition = placement.Catalog.Find("battle");
        Node2D towers = battle.GetNode<Node2D>("World/Towers");
        Vector2I cell = FindBuildCell(grid, battle.GetNode<SpawnManager>("World/EnemySystems/Spawning"));
        wallet.Initialize(new LevelConfig { StartingCredits = 400 });
        Require(definition.DefaultConfig.ConstructionCosts.Count == 1 &&
            definition.DefaultConfig.ConstructionCosts[0].Amount == 200, "Saved Battle Tower recipe is 200 Credits");
        PackedScene original = definition.Scene;
        PackedScene broken = new(); Node2D wrongRoot = new(); broken.Pack(wrongRoot); wrongRoot.Free();
        definition.Scene = broken;
        placement.Call("TryPlace", cell);
        Require(wallet.GetBalance(credits) == 400 && !grid.IsOccupied(cell) && towers.GetChildCount() == 0,
            "Invalid tower scene creates no tower, occupancy or charge");
        definition.Scene = original;
        placement.Call("TryPlace", cell);
        Require(wallet.GetBalance(credits) == 200 && grid.IsOccupied(cell) && towers.GetChildCount() == 1,
            "Actual placement charges exactly 200 and commits occupancy");
        BattleTower tower = towers.GetChild<BattleTower>(0); tower.SetPhysicsProcess(false);
        Require(tower.Config != definition.DefaultConfig && tower.Config.ConstructionCosts[0] != definition.DefaultConfig.ConstructionCosts[0],
            "Placed tower has isolated nested cost/config resources");
        placement.Call("TryPlace", cell);
        Require(wallet.GetBalance(credits) == 200 && towers.GetChildCount() == 1, "Repeat click on occupied cell never charges twice");
        Vector2I other = FindBuildCell(grid, battle.GetNode<SpawnManager>("World/EnemySystems/Spawning"));
        wallet.Initialize(new LevelConfig { StartingCredits = 199 });
        placement.Call("TryPlace", other);
        Require(wallet.GetBalance(credits) == 199 && !grid.IsOccupied(other) && towers.GetChildCount() == 1,
            "Unaffordable build creates nothing and keeps its funds");
        tower.Free(); grid.ReleaseOccupancy(cell);
        wallet.Initialize(new LevelConfig { StartingCredits = 400 });
        // Simulate a stale placement index: the dictionary insertion fails after occupancy succeeds.
        placement.Call("TryPlace", cell);
        Require(wallet.GetBalance(credits) == 400 && !grid.IsOccupied(cell) && towers.GetChildCount() == 0,
            "Failure after occupancy rolls back world state and keeps the entire payment");
        TowerConfig savedConfig = definition.DefaultConfig;
        definition.DefaultConfig = (BattleTowerConfig)savedConfig.Duplicate(true);
        definition.DefaultConfig.ConstructionCosts.Add(new ResourceCost { Resource = alloy, Amount = 2 });
        wallet.Grant(alloy, 1);
        placement.Call("TryPlace", other);
        Require(wallet.GetBalance(credits) == 400 && wallet.GetBalance(alloy) == 1 && !grid.IsOccupied(other),
            "Actual multi-resource build rejects a missing material without partial payment");
        wallet.Grant(alloy, 1);
        placement.Call("TryPlace", other);
        Require(wallet.GetBalance(credits) == 200 && wallet.GetBalance(alloy) == 0 && grid.IsOccupied(other),
            "Actual multi-resource construction commits all costs together");
        towers.GetChild<BattleTower>(0).Free(); grid.ReleaseOccupancy(other);
        definition.DefaultConfig = savedConfig;
        wallet.Initialize(grid.Level.Settings);
        GD.Print("PASS: economy initialization, atomic transactions, HUD updates and paid placement checks");
    }

    // =========================================================
    // Use a real authored build tile that retains all required enemy routes.
    private static Vector2I FindBuildCell(WorldGrid grid, SpawnManager spawns)
    {
        foreach (Vector2I cell in grid.Level.Passable.GetUsedCells())
            if (grid.PlacementFailure(cell).Length == 0 && spawns.PlacementFailure(cell).Length == 0) return cell;
        throw new InvalidOperationException("No route-safe build cell in test level.");
    }

    // =========================================================
    // Construct readable recipes using the same typed Resource as saved tower defaults.
    private static Godot.Collections.Array<ResourceCost> Costs(params (ResourceDefinition resource, int amount)[] entries)
    {
        var result = new Godot.Collections.Array<ResourceCost>();
        foreach (var entry in entries) result.Add(new ResourceCost { Resource = entry.resource, Amount = entry.amount });
        return result;
    }

    // =========================================================
    // Stop the integration run immediately if an economy invariant fails.
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
