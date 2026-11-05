// Focused PW1/PW4-B checks: terrain islands, cached conductive delivery, real capacitors and dormant lifecycle.
using Godot;
using System;

public partial class PowerNetworkChecks : Node
{
    // =========================================================
    // One small synthetic world uses production grid code and palettes without running combat or campaign fixtures.
    public override void _Ready()
    {
        try
        {
            CheckRegions(); PowerDistanceChecks.Run();
            CheckTerrain();
            GD.Print("PASS: power islands, conductive distance/curve/cache, near/far capacitor refill, isolation and dormant processing");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }

    // =========================================================
    // Check row-wrap, diagonals, footprint bounds and deterministic relabeling without scene-tree work.
    private static void CheckRegions()
    {
        PowerIslandMap map = new();
        bool[] cells = new bool[12]; cells[3] = cells[4] = true;
        map.Rebuild(4, 3, cells);
        Require(map.IslandCount == 2 && map.RegionAt(3, 0) != map.RegionAt(0, 1), "Rows never wrap into neighbours");
        Array.Clear(cells); cells[0] = cells[5] = true; map.Rebuild(4, 3, cells);
        Require(map.IslandCount == 2, "Diagonal-only contacts remain separate");
        cells[1] = true; map.Rebuild(4, 3, cells);
        Require(map.IslandCount == 1 && map.CellCount(1) == 3 && map.ConductingCellCount == 3, "Edge bridge merges islands");
        Require(map.RegionForFootprint(0, 0, 2, 1) == 1 && map.RegionForFootprint(0, 0, 2, 2) == 0,
            "Whole footprint must have conducting terrain, including holes");
        Require(map.RegionAt(-1, 0) == 0 && map.RegionAt(4, 0) == 0 && map.CellCount(999) == 0 &&
            map.RegionForFootprint(int.MaxValue, 0, 2, 1) == 0 && map.RegionForFootprint(0, 0, 0, 1) == 0,
            "Outside/invalid queries return no island without overflowing");
        cells[1] = false; map.Rebuild(4, 3, cells);
        Require(map.IslandCount == 2 && map.RegionAt(0, 0) == 1 && map.RegionAt(1, 1) == 2, "Removing bridge splits and labels in row order");
        map.Rebuild(4, 3, cells); Require(map.RegionAt(1, 1) == 2, "Same topology gives deterministic snapshot IDs");
        Array.Clear(cells); map.Rebuild(4, 3, cells);
        Require(map.IslandCount == 0 && map.ConductingCellCount == 0 && map.RegionAt(0, 0) == 0, "Empty rebuild clears old labels");
    }

    // =========================================================
    // No services beyond Level/Grid/PowerNetworks; transformed coordinates use the normal WorldGrid conversion.
    private void CheckTerrain()
    {
        Node2D world = new() { Position = new(100, -40), Rotation = .17f, Scale = new(1.2f, 1.2f) };
        WorldLevel level = new() { Name = "Level", Settings = new LevelConfig() };
        world.AddChild(level);
        Node2D terrain = new() { Name = "Terrain" }; level.AddChild(terrain);
        TileMapLayer passable = Layer(terrain, "Passable", "PassableTerrain");
        TileMapLayer interactive = Layer(terrain, "Interactive", "InteractiveTerrain");
        TileMapLayer impassable = Layer(terrain, "Impassable", "ImpassableTerrain");
        TileMapLayer decorations = Layer(terrain, "Decorations", "DecorationTerrain");
        Vector2I a = new(3, 3), b = new(5, 3), diagonal = new(6, 4), bridge = new(4, 3), join = new(6, 3);
        foreach (Vector2I cell in new[] { a, b, diagonal }) passable.SetCell(cell, 0, new(2, 0));
        decorations.SetCell(new(10, 8), 0, new(5, 0));
        Node2D objectives = new() { Name = "Objectives" }; level.AddChild(objectives);
        WorldObjectFootprint citadel = new() { Name = "Citadel", Position = new(224, 224), SizeCells = Vector2I.One, BlocksEnemies = true };
        objectives.AddChild(citadel); citadel.AddChild(new CitadelHealth { Name = "Health" });
        Node2D approaches = new() { Name = "ApproachPoints" }; citadel.AddChild(approaches);
        approaches.AddChild(new Marker2D { Position = new(0, 64) });
        WorldGrid grid = new() { Name = "Grid", Config = new WorldMasterConfig { WidthCells = 12, HeightCells = 10 } };
        world.AddChild(grid);
        PowerGridManager power = new() { Name = "PowerNetworks" }; world.AddChild(power);
        try
        {
            AddChild(world);
            Require(power.IslandCount == 3 && power.ConductingCellCount == 3 && power.RebuildCount == 1, "Startup discovers terrain islands");
            Require(grid.WorldToCell(grid.CellToWorldCenter(a)) == a && power.RegionAt(a) != power.RegionAt(b), "Transforms retain correct cell ownership");
            Require(grid.TerrainConductsPower(a) && !grid.CanBuild(a) && grid.TerrainBlocksEnemies(a), "Citadel reservations do not erase land conductivity");
            Require(power.RegionAt(new(10, 8)) == 0 && power.RegionAt(new(2, 2)) == 0, "Decorations/empty space never join grids");
            PowerDistributionChecks.Links(power, grid, world, a, b);
            int rebuilds = power.RebuildCount, navigation = grid.NavigationRevision;
            grid.RebuildTerrain();
            Require(power.RebuildCount == rebuilds && grid.NavigationRevision == navigation, "Unchanged terrain skips topology and navigation rebuilds");
            Require(grid.TryOccupy(b), "Test tower footprint occupies legal floor");
            grid.RebuildTerrain(); grid.ReleaseOccupancy(b);
            Require(power.RebuildCount == rebuilds && power.RegionAt(b) > 0, "Tower occupancy/removal never rebuilds or severs islands");
            int notices = 0; power.NetworksChanged += () => notices++;
            passable.SetCell(bridge, 0, new(2, 0)); grid.RebuildTerrain();
            Require(power.IslandCount == 2 && notices == 1, "Terrain bridge merges, diagonal island remains separate");
            passable.SetCell(join, 0, new(2, 0)); grid.RebuildTerrain();
            Require(power.IslandCount == 1 && power.RegionForFootprint(a, new(3, 1)) > 0, "Second edge joins final island");
            passable.EraseCell(bridge); grid.RebuildTerrain(); Require(power.IslandCount == 2, "Erased bridge splits grids");
            navigation = grid.NavigationRevision;
            interactive.SetCell(join, 1, new(0, 0)); grid.RebuildTerrain();
            Require(power.IslandCount == 3 && power.RegionAt(join) == 0 && grid.NavigationRevision == navigation,
                "Lava cuts conduction over floor without forcing a navigation revision");
            interactive.SetCell(join, 0, new(1, 0)); grid.RebuildTerrain();
            Require(power.IslandCount == 2 && grid.TerrainConductsPower(join) && grid.TerrainBlocksEnemies(join),
                "Solid raised Foundation conducts independently of pathability");
            impassable.SetCell(bridge, 0, new(0, 0)); grid.RebuildTerrain();
            Require(power.IslandCount == 1 && grid.TerrainConductsPower(bridge) && !grid.CanBuild(bridge), "Nonbuildable wall joins the physical island");
            interactive.SetCell(bridge, 1, new(0, 0)); grid.RebuildTerrain();
            Require(power.IslandCount == 1 && grid.TerrainConductsPower(bridge), "Impassable wins over Interactive for conductivity too");
            CheckDelivery(power, grid, world, a, b);
            PowerDistributionChecks.Relay(power, grid, world, a, b);
            PowerAllocationChecks.Run(power, grid, world, a, b);
            CheckAccounting(power, grid, impassable, interactive, bridge, a, b);
            Require(!power.IsProcessing() && !power.IsPhysicsProcessing(), "Topology/accounting manager has no idle/frame callbacks");
        }
        finally { world.Free(); }
    }

    // =========================================================
    // Existing transformed grid plus two actual Battle capacitors; no scene/gameplay simulation or profile I/O.
    private static void CheckDelivery(PowerGridManager power, WorldGrid grid, Node2D world, Vector2I near, Vector2I far)
    {
        power.DeliveryConfig = new PowerDeliveryConfig { FullEfficiencyCells = 0, FalloffCells = 2, MinimumEfficiency = .1 };
        int rebuilds = power.DistanceRebuildCount;
        power.SetProducer("distance:source", near, Vector2I.One, 100);
        power.SetProducer("distance:zero", far, Vector2I.One, 0);
        Require(power.DistanceRebuildCount == rebuilds, "Source registration batches remain lazy");
        Require(power.DeliveryForFootprint(far, Vector2I.One) == new PowerDelivery(2, .5)
            && power.DistanceRebuildCount == rebuilds + 1, "One shared BFS computes conductive distance; zero-output source is not an anchor");
        rebuilds = power.DistanceRebuildCount;
        power.SetProducer("distance:source", near, Vector2I.One, 125);
        power.DeliveryConfig.MinimumEfficiency = .2;
        Require(power.DeliveryForFootprint(far, Vector2I.One).Efficiency == .5 && power.DistanceRebuildCount == rebuilds,
            "Positive rate and curve edits reuse the distance field");
        Node2D towers = new() { Name = "Towers" }; world.AddChild(towers);
        TowerDefinition definition = GD.Load<TowerDefinition>("res://TOWERS/Offensive/BattleTower/BattleTowerDefinition.tres");
        BattleTower a = definition.Scene.Instantiate<BattleTower>(), b = definition.Scene.Instantiate<BattleTower>();
        try
        {
            a.InitializeDefinition(definition); b.InitializeDefinition(definition);
            a.Position = world.ToLocal(grid.CellToWorldCenter(near)); b.Position = world.ToLocal(grid.CellToWorldCenter(far));
            towers.AddChild(a); towers.AddChild(b);
            Require(a.Power.RechargeCeilingPerSecond == 10 && b.Power.RechargeCeilingPerSecond == 5,
                "Consumer mapping respects transformed grid and independently capped recharge");
            power._PhysicsProcess(1);
            Require(Math.Abs(a.Power.Capacitor.Energy - 10) < 1e-6 && Math.Abs(b.Power.Capacitor.Energy - 5) < 1e-6,
                "Actual capacitors credit near/far ceilings without reducing capacity");
            power.RemoveEndpoint("distance:source"); power._PhysicsProcess(1);
            Require(a.Power.RechargeCeilingPerSecond == 0 && b.Power.DeliveryEfficiency == 0
                && a.Power.Capacitor.Energy == 10 && b.Power.Capacitor.Energy == 5,
                "Last source loss stops refill, preserves charge and never applies the efficiency floor without a route");
            power.SetProducer("distance:source", near, Vector2I.One, 100);
            power.SetProducer("distance:other", far, Vector2I.One, 50);
            rebuilds = power.DistanceRebuildCount; power.RefreshConsumerDelivery();
            Require(b.Power.ConductiveDistanceCells == 0 && b.Power.RechargeCeilingPerSecond == 10
                && power.DistanceRebuildCount == rebuilds + 1, "Multiple source updates coalesce; closer source improves ceiling without extra energy");
            b.GlobalPosition = grid.CellToWorldCenter(new(0, 0)); b.ForceUpdateTransform();
            Require(b.Power.RechargeCeilingPerSecond == 0, "Consumer relocation to nonconducting space invalidates stale delivery");
        }
        finally
        {
            a.Free(); b.Free(); towers.Free();
            power.RemoveEndpoint("distance:source"); power.RemoveEndpoint("distance:zero"); power.RemoveEndpoint("distance:other");
            power.DeliveryConfig = null;
        }
    }

    // =========================================================
    // Small explicit registrations exercise replacement, isolation and remapping without a combat simulation.
    private static void CheckAccounting(PowerGridManager power, WorldGrid grid, TileMapLayer impassable,
        TileMapLayer interactive, Vector2I bridge, Vector2I a, Vector2I b)
    {
        power.SetProducer("test:source-a", a, Vector2I.One, 100);
        power.SetProducer("test:source-b", b, Vector2I.One, 50);
        power.SetRequestedDemand("test:demand", b, Vector2I.One, 40);
        Require(power.Totals.Supply == 150 && power.Totals.RequestedDemand == 40 && power.Totals.Margin == 110,
            "Supply/requested demand totals are cached in common capacity units");
        int revision = power.AccountingRevision;
        power.SetProducer("test:source-a", a, Vector2I.One, 100);
        Require(power.AccountingRevision == revision && power.Totals.Producers == 2, "Repeated registration never duplicates or emits a change");
        power.SetProducer("test:source-a", a, Vector2I.One, 125);
        Require(power.Totals.Supply == 175, "Output updates replace the old contribution once");
        impassable.EraseCell(bridge); interactive.EraseCell(bridge); grid.RebuildTerrain();
        Require(power.RegionAt(a) != power.RegionAt(b) && power.TotalsForRegion(power.RegionAt(a)).Supply == 125 &&
            power.TotalsForRegion(power.RegionAt(a)).RequestedDemand == 0 &&
            power.TotalsForRegion(power.RegionAt(b)).Supply == 50 && power.TotalsForRegion(power.RegionAt(b)).RequestedDemand == 40,
            "A terrain split redistributes existing registrations without pooling islands");
        power.SetProducer("test:source-b", new(0, 0), Vector2I.One, 50);
        Require(power.UnassignedProducers == 1 && power.Totals.Supply == 125 && power.TotalsForRegion(0) == default,
            "Unassigned source remains diagnosable and contributes no phantom supply");
        bool rejected = false;
        try { power.SetProducer("test:source-a", a, Vector2I.One, float.NaN); } catch (ArgumentException) { rejected = true; }
        Require(rejected && power.Totals.Supply == 125, "Invalid amounts reject before mutating totals");
        Require(power.DeliveryForFootprint(b, Vector2I.One).Efficiency == 0, "Unassigned source cannot power its former island");
        power.RemoveEndpoint("test:source-b"); power.RemoveEndpoint("test:source-b");
        power.RemoveEndpoint("test:source-a"); power.RemoveEndpoint("test:demand");
        Require(power.Totals == default && power.UnassignedProducers == 0 && power.UnassignedConsumers == 0,
            "Unregistering twice is safe and the last member clears totals exactly");
        CitadelPowerConfig config = new(); int changes = 0; config.Changed += () => changes++;
        config.Output = 125; config.Output = 125;
        Require(changes == 1 && config.Output == 125, "Citadel setting emits only actual valid changes"); config.Dispose();
    }

    // =========================================================
    // Authored production palettes are read-only; test paint exists only in disposable scene instances.
    private static TileMapLayer Layer(Node parent, string name, string palette)
    {
        TileMapLayer layer = new() { Name = name, TileSet = GD.Load<TileSet>("res://WORLD/TERRAIN/TileSets/" + palette + ".tres") };
        parent.AddChild(layer); return layer;
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}

