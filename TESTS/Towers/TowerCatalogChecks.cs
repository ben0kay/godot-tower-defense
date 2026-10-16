// Real multi-type placement and catalog validation using a weapon-free test-only fixture.
using Godot;
using System;

public static class TowerCatalogChecks
{
    // =========================================================
    // Prove registration and generic placement without adding production placeholder towers.
    public static void Run(Node main)
    {
        TowerPlacementController placement = main.GetNode<TowerPlacementController>("TowerPlacement");
        WorldGrid grid = main.GetNode<WorldGrid>("World/Grid");
        SpawnManager spawns = main.GetNode<SpawnManager>("World/EnemySystems/Spawning");
        EconomyManager wallet = main.GetNode<EconomyManager>("Economy");
        GameplayHUD hud = main.GetNode<GameplayHUD>("UI/GameplayHUD");
        Node2D towers = main.GetNode<Node2D>("World/Towers");
        TowerDefinition battle = placement.Catalog.Find("battle");
        TowerDefinition passive = new() { TowerId = "test-passive", Category = TowerDefinition.TowerCategory.Utility,
            Scene = GD.Load<PackedScene>("res://TESTS/Towers/PassiveTestTower.tscn"),
            DefaultConfig = new TowerConfig { DisplayName = "Passive fixture", Description = "No weapon loop.",
                ConstructionCosts = new() { new ResourceCost { Resource = wallet.CreditsResource, Amount = 75 } } } };
        TowerCatalog catalog = new() { Definitions = new() { battle, passive } };
        catalog.Rebuild();
        Require(catalog.Find("test-passive") == passive && catalog.Find("missing") == null, "Catalog ID lookup is authoritative");
        Invalid(new TowerCatalog { Definitions = new() { battle, battle } });
        foreach (TowerDefinition invalid in new[] {
            new TowerDefinition { TowerId = "" }, new TowerDefinition { TowerId = "bad", Footprint = new Vector2I(2, 2) },
            new TowerDefinition { TowerId = "bad", Scene = battle.Scene, DefaultConfig = passive.DefaultConfig },
            new TowerDefinition { TowerId = "bad", Scene = passive.Scene, DefaultConfig = null } })
            Invalid(new TowerCatalog { Definitions = new() { invalid } });
        PackedScene wrongScene = new(); Node2D wrongRoot = new(); wrongScene.Pack(wrongRoot); wrongRoot.Free();
        Invalid(new TowerCatalog { Definitions = new() { new TowerDefinition { TowerId = "wrong-root",
            Scene = wrongScene, DefaultConfig = passive.DefaultConfig } } });
        TowerConfig invalidPrice = passive.DefaultConfig.CreateInstanceCopy(); invalidPrice.ConstructionCosts[0].Amount = -1;
        Invalid(new TowerCatalog { Definitions = new() { new TowerDefinition { TowerId = "negative-price",
            Scene = passive.Scene, DefaultConfig = invalidPrice } } });
        Invalid(new TowerCatalog { Definitions = new() { null } });
        TowerCatalog original = placement.Catalog;
        placement.Catalog = catalog;
        hud.BindCatalog(catalog);
        wallet.Initialize(new LevelConfig { StartingCredits = 1000 });
        hud.GetBuildButton(passive.TowerId).EmitSignal(Button.SignalName.Pressed);
        Vector2I first = Cell(grid, spawns);
        placement.Call("TryPlace", first);
        TowerBase p = towers.GetChild<TowerBase>(towers.GetChildCount() - 1);
        Require(p is PassiveTestTower && p.TowerTypeId == passive.TowerId && p.GridCell == first && p.Footprint == Vector2I.One,
            "Generic placement uses registered scene/type/anchor");
        Require(p.FinalStats == null && !p.InspectionRange.HasValue && !p.IsPhysicsProcessing() && p.NextUpgrade == null,
            "Passive tower needs no artificial weapon stats, range or ticks");
        Require(wallet.GetBalance(wallet.CreditsResource) == 925 && p.AuthoredConfig != passive.DefaultConfig &&
            p.AuthoredConfig.ConstructionCosts[0] != passive.DefaultConfig.ConstructionCosts[0], "Passive recipe paid with private config/cost data");
        Require(hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/TowerName").Text == "Passive fixture" &&
            !hud.GetNode<Button>("BottomArea/Inspector/Insets/Body/UpgradeButton").Visible, "Actual passive selection has safe no-upgrades Inspector");
        placement.Call("TryPlace", first);
        Require(wallet.GetBalance(wallet.CreditsResource) == 925, "Repeat passive placement never charges");
        hud.EmitSignal(GameplayHUD.SignalName.TowerChoiceRequested, "battle");
        Vector2I second = Cell(grid, spawns);
        placement.Call("TryPlace", second);
        BattleTower b = towers.GetChild<BattleTower>(towers.GetChildCount() - 1); b.SetPhysicsProcess(false);
        Require(wallet.GetBalance(wallet.CreditsResource) == 725 && b.FinalStats.DamagePerShot == 12, "Battle still pays 200 and resolves baseline");
        Require(TowerUpgradeController.TryUpgrade(b, wallet, placement.Settings, 1, out _) &&
            b.UpgradeLevel == 2 && p.UpgradeLevel == 1 && b.FinalStats.DamagePerShot == 16 &&
            wallet.GetBalance(wallet.CreditsResource) == 575, "Mixed types retain independent progression and paid Battle upgrades");
        placement.Call("SelectTower", p);
        Require(hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/TowerName").Text == passive.DisplayName,
            "Selection reads actual tower rather than last build config");
        passive.Available = false;
        hud.EmitSignal(GameplayHUD.SignalName.TowerChoiceRequested, passive.TowerId);
        Vector2I unused = Cell(grid, spawns);
        placement.Call("TryPlace", unused);
        // Unavailable choice cancels build mode; direct placement must also reject the unavailable type.
        Require(wallet.GetBalance(wallet.CreditsResource) == 575 && !grid.IsOccupied(unused), "Unavailable registration cannot be built");
        passive.Available = true;
        placement.Call("SelectTower", default(Variant));
        p.Free(); b.Free(); grid.ReleaseOccupancy(first); grid.ReleaseOccupancy(second);
        // Fixture teardown removes only its own private selection-index entries.
        var index = (System.Collections.Generic.Dictionary<Vector2I, TowerBase>)typeof(TowerPlacementController)
            .GetField("_placedTowers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(placement);
        index.Remove(first); index.Remove(second);
        placement.Catalog = original;
        hud.BindCatalog(original);
        hud.EmitSignal(GameplayHUD.SignalName.TowerChoiceRequested, "battle");
        placement.Call("CancelPlacement"); hud.ClearInspection(); wallet.Initialize(grid.Level.Settings);
        GD.Print("PASS: catalog validation, mixed tower placement/selection, passive capabilities and independent paid progression");
    }

    // =========================================================
    // Find a live route-safe terrain cell through production validation.
    private static Vector2I Cell(WorldGrid grid, SpawnManager spawns)
    {
        foreach (Vector2I cell in grid.Level.Passable.GetUsedCells())
            if (grid.PlacementFailure(cell).Length == 0 && spawns.PlacementFailure(cell).Length == 0) return cell;
        throw new InvalidOperationException("No route-safe test cell.");
    }
    // =========================================================
    // Invalid entries must fail before world state/payment can be touched.
    private static void Invalid(TowerCatalog catalog)
    {
        try { catalog.Rebuild(); } catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("Malformed catalog was accepted.");
    }
    // =========================================================
    // Fail immediately on registry or generic placement regression.
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
