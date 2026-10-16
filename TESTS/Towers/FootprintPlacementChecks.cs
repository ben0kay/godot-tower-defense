// Generic dimensions, structural foundations, real economy-building selection and transaction rollback.
using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using static AmmoLoopFixture;

internal static class FootprintPlacementChecks
{
    // =========================================================
    // Definition and grid claims are generic, including future taller rectangles and invalid/overflow inputs.
    internal static void Run(Node owner)
    {
        using FootprintFixture f = new(owner);
        TowerDefinition definition = (TowerDefinition)f.World.Placement.Catalog.Find("ammo_factory").Duplicate();
        foreach (Vector2I size in new[] { Vector2I.One, new(2, 2), new(3, 2), new(2, 3) })
        {
            definition.Footprint = size; definition.Validate();
            Require(f.Grid.TryOccupy(f.Anchor, size) && f.Occupied(f.Anchor, size) == size.X * size.Y, "Complete rectangular claim " + size);
            if (size != Vector2I.One)
            { f.Grid.ReleaseOccupancy(f.Anchor); Require(f.Occupied(f.Anchor, size) == size.X * size.Y, "Partial release cannot split a building"); }
            f.Grid.ReleaseOccupancy(f.Anchor, size); Require(f.Occupied(f.Anchor, size) == 0, "Complete release " + size);
        }
        foreach (Vector2I size in new[] { new Vector2I(0, 2), new(-1, 1), new(1, 0), new(1, -2), new(int.MaxValue, int.MaxValue) })
        {
            definition.Footprint = size; bool rejected = false;
            try { definition.Validate(); } catch (InvalidOperationException) { rejected = true; }
            Require(rejected && !f.Grid.TryOccupy(f.Anchor, size), "Invalid dimensions rejected before iteration " + size);
        }
        definition.Dispose();
        Vector2I factory = new(3, 2);
        f.NoBuild("ammo_factory", new(f.Grid.Config.WidthCells - 2, 12), factory);
        f.NoBuild("ammo_factory", new(12, f.Grid.Config.HeightCells - 1), factory);
        f.NoBuild("ammo_factory", new(int.MaxValue, 0), factory);
        // Non-anchor unbuildable/reserved/occupied cells invalidate the entire transaction.
        Vector2I edge = f.Anchor + new Vector2I(2, 1);
        f.Grid.Level.Passable.EraseCell(edge); f.Grid.RebuildTerrain(); f.NoBuild("ammo_factory", f.Anchor, factory);
        f.Paint(new(f.Anchor, factory), false); f.Grid.RebuildTerrain();
        Require(f.Grid.TryOccupy(edge), "Neighbour claim"); f.NoBuild("ammo_factory", f.Anchor, factory);
        f.Grid.ReleaseOccupancy(f.Anchor, factory); Require(f.Grid.IsOccupied(edge), "Wrong rectangle cannot release neighbour"); f.Grid.ReleaseOccupancy(edge);
        WorldObjectFootprint reserved = new() { SizeCells = Vector2I.One }; f.Grid.Level.AddChild(reserved);
        reserved.GlobalPosition = f.Grid.CellToWorldCenter(edge); f.Grid.RebuildTerrain(); f.NoBuild("ammo_factory", f.Anchor, factory);
        reserved.Free(); f.Grid.RebuildTerrain();
        foreach (Vector2I size in new[] { new Vector2I(3, 2), new(2, 2), new(2, 3) }) foreach (bool majority in new[] { false, true })
        {
            f.Paint(new(f.Anchor, size), majority); f.Paint(new(f.Anchor + size - Vector2I.One, Vector2I.One), !majority); f.Grid.RebuildTerrain();
            int floods = f.Nav.PlacementFloodCount;
            Require(f.Grid.PlacementFailure(f.Anchor, size).Contains("uniform") && f.World.Spawns.PlacementFailure(f.Anchor, size).Contains("uniform"), "Mixed terrain structural reason");
            Require(!f.Grid.TryOccupy(f.Anchor, size) && f.Nav.PlacementFloodCount == floods, "Mixed terrain never reaches a flood or occupancy");
            if (size == factory) f.NoBuild("ammo_factory", f.Anchor, size);
        }
        f.Paint(new(f.Anchor, new(10, 6)), false, true); f.Grid.RebuildTerrain();
        Preview(f); ActualBuildings(f); Failures(f);
        GD.Print("PASS: footprint dimensions, bounds/reservations/overlap/mixed categories, real 6/4-cell centering/selection/Inspector/payment and rollback");
    }

    // =========================================================
    // Ghost/rectangle validity and dimensions use cheap grid rules, with no candidate flood per preview frame.
    private static void Preview(FootprintFixture f)
    {
        f.World.Hud.GetBuildButton("ammo_factory").EmitSignal(Button.SignalName.Pressed);
        int floods = f.Nav.PlacementFloodCount;
        for (int i = 0; i < 100; i++) f.World.Placement.Call("RefreshPreview", f.Anchor, true);
        Sprite2D ghost = f.World.Placement.GetNode<Sprite2D>("PlacementGhost");
        Require(f.Nav.PlacementFloodCount == floods && ghost.Visible && ghost.Modulate.G > ghost.Modulate.R
            && ghost.GlobalPosition.IsEqualApprox(f.Grid.FootprintCenter(f.Anchor, new(3, 2)))
            && (ghost.Scale * ghost.Texture.GetSize()).IsEqualApprox(new Vector2(192, 128)), "Preview spans/centers 3x2, green and flood-free");
        f.Paint(new(f.Anchor + new Vector2I(2, 1), Vector2I.One), true); f.Grid.RebuildTerrain();
        f.World.Placement.Call("RefreshPreview", f.Anchor, true);
        Require(ghost.Modulate.R > ghost.Modulate.G && f.World.Hud.FindChildren("*", "Label", true, false)
            .Any(label => ((Label)label).Text.Contains("uniform")), "Mixed foundation has red preview and structural failure message");
        f.Paint(new(f.Anchor, new(3, 2)), false, true); f.Grid.RebuildTerrain();
        f.World.Placement.Call("CancelPlacement"); Require(!ghost.Visible, "Cancellation clears full ghost");
    }

    // =========================================================
    // Every cell maps to one instance; deletion retires all cells but never an adjacent building.
    private static void ActualBuildings(FootprintFixture f)
    {
        Vector2I depotAnchor = f.Anchor + new Vector2I(5, 0);
        int credits = f.Balance;
        TowerBase factory = f.Build("ammo_factory", f.Anchor), depot = f.Build("ammo_depot", depotAnchor);
        Require(factory is AmmunitionFactory && depot is AmmunitionDepot && f.Balance == credits - 350, "One construction price per building");
        foreach (TowerBase tower in new[] { factory, depot })
        {
            Require(f.Occupied(tower.GridCell, tower.Footprint) == tower.Footprint.X * tower.Footprint.Y
                && tower.GlobalPosition.IsEqualApprox(f.Grid.FootprintCenter(tower.GridCell, tower.Footprint)), "Complete occupancy and full center");
            for (int y = 0; y < tower.Footprint.Y; y++) for (int x = 0; x < tower.Footprint.X; x++)
            {
                TowerBase selected = f.World.Placement.TowerAt(tower.GridCell + new Vector2I(x, y));
                Require(selected == tower, "Any occupied cell selects the same instance"); f.World.Placement.Call("SelectTower", selected);
                string description = f.World.Placement.Call("DescribeTower", "").AsString();
                Require(description.Contains($"Footprint: {tower.Footprint.X} x {tower.Footprint.Y}"), "Placed Inspector dimensions");
            }
        }
        Require(((AmmunitionDepot)depot).Inventory.Capacity == 10000 && ((AmmunitionFactory)factory).Config.ShipmentAmount == 1000, "Ammunition balance unchanged");
        f.NoBuild("ammo_factory", f.Anchor + Vector2I.One, new(3, 2));
        factory.Free(); Require(f.Occupied(f.Anchor, new(3, 2)) == 0 && f.World.Placement.TowerAt(f.Anchor + new Vector2I(2, 1)) == null
            && f.Occupied(depotAnchor, new(2, 2)) == 4, "Removal clears exact mapping/claim, preserves neighbour"); depot.Free();
        f.World.Hud.GetBuildButton("ammo_factory").EmitSignal(Button.SignalName.Pressed);
        Require(f.World.Placement.Call("DescribeTower", "").AsString().Contains("Footprint: 3 x 2"), "Build preview dimensions");
        Vector2 offset = f.Grid.Position; f.Grid.Position += new Vector2(37, 19);
        Require(f.Grid.FootprintCenter(f.Anchor, new(3, 2)).IsEqualApprox(f.Grid.ToGlobal(((Vector2)f.Anchor + new Vector2(1.5f, 1)) * f.Grid.Config.CellSize)), "Center respects grid transform"); f.Grid.Position = offset;
    }

    // =========================================================
    // A failure after occupancy/indexing starts must roll back cells/node and never charge the wallet.
    private static void Failures(FootprintFixture f)
    {
        TowerDefinition live = f.World.Placement.Catalog.Find("ammo_factory"); PackedScene original = live.Scene;
        PackedScene malformed = new(); Node2D wrong = new(); malformed.Pack(wrong); wrong.Free();
        live.Scene = malformed; f.NoBuild("ammo_factory", f.Anchor, new(3, 2)); live.Scene = original; malformed.Dispose();
        var index = (Dictionary<Vector2I, TowerBase>)typeof(TowerPlacementController).GetField("_placedTowers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(f.World.Placement);
        PassiveTestTower sentinel = new(); Vector2I edge = f.Anchor + new Vector2I(2, 1); index.Add(edge, sentinel);
        int jobs = f.World.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork").ActiveCount;
        int before = f.Balance, nodes = f.World.Towers.GetChildCount(); f.Build("ammo_factory", f.Anchor);
        Require(f.Balance == before && f.World.Towers.GetChildCount() == nodes && f.Occupied(f.Anchor, new(3, 2)) == 0
            && f.World.Placement.TowerAt(f.Anchor) == null && f.World.Placement.TowerAt(edge) == sentinel
            && f.World.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork").ActiveCount == jobs, "Late failure cleans only its own index entries/cells/node, with no charge");
        index.Remove(edge); sentinel.Free();
        f.World.Wallet.TrySpend(new() { new ResourceCost { Resource = f.World.Wallet.CreditsResource, Amount = f.Balance } });
        f.NoBuild("ammo_factory", f.Anchor, new(3, 2));
    }
}
