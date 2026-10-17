// Isolated research graph/storage and real battlefield telemetry checks; no user progression writes.
using Godot;
using System;
using System.IO;
using System.Linq;
using System.Collections.Immutable;
using System.Threading.Tasks;
using static AmmoLoopFixture;

public partial class ResearchFoundationChecks : Node
{
    // =========================================================
    // Test pure graph/storage first, then real placement/upgrade/construction/result paths on both maps.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            ProfileStore.ClearSelection();
            PureResearch(); GraphGeometry();
            foreach (string scene in new[] { MenuNavigation.Level001Scene, MenuNavigation.Level002Scene })
                await Telemetry(scene);
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("ResearchFoundationChecks: PASS catalog, purchase, v1 migration, persistence, graph and both-map telemetry");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }

    // =========================================================
    // Only temporary profile files are touched; purchases and XP remain independent of campaign saves.
    private static void PureResearch()
    {
        var nodes = ResearchService.Nodes; Require(nodes.Count == 3, "Only three real nodes");
        ResearchNode core = nodes["research_core"], upgrade = nodes["citadel_master_1"];
        string root = Path.Combine(Path.GetTempPath(), "td-research-" + Guid.NewGuid().ToString("N"));
        try
        {
            string id = Guid.NewGuid().ToString("N"); var store = new ProgressionStore(root);
            PlayerProgression fresh = store.Load(id);
            Require(ResearchRules.State(fresh, core) == ResearchState.Researched &&
                ResearchRules.State(fresh, upgrade) == ResearchState.InsufficientPoints, "Core free, purchase gated by RP");
            Expect(() => ResearchRules.Purchase(fresh, upgrade), "insufficient RP");
            PlayerProgression earned = fresh.AddXP(600), bought = ResearchRules.Purchase(earned, upgrade);
            Require(bought.LifetimePlayerXP == 600 && bought.Level == 2 && bought.UnspentResearchPoints == 0 &&
                bought.PurchasedResearchIds.SequenceEqual(new[] { upgrade.Id }), "Atomic RP payment and ID ownership");
            Expect(() => ResearchRules.Purchase(bought, upgrade), "repeat purchase");
            Expect(() => ResearchRules.Purchase(earned, core), "core purchase");
            ResearchNode locked = upgrade with { Id = "later", Prerequisites = ImmutableArray.Create("missing") };
            Require(ResearchRules.State(earned, locked) == ResearchState.Locked, "Prerequisite blocks purchase");
            store.Save(bought); PlayerProgression loaded = new ProgressionStore(root).Load(id);
            Require(loaded.PurchasedResearchIds.SequenceEqual(bought.PurchasedResearchIds) && loaded.UnspentResearchPoints == 0, "Purchase survives reload");
            Require(ResearchRules.CitadelMaximum(1000, loaded, nodes) == 1100 &&
                ResearchRules.CitadelMaximum(1000, loaded, nodes) == 1100, "Repeated resolution never compounds");
            Require(store.Load(Guid.NewGuid().ToString("N")).PurchasedResearchIds.Length == 0, "Independent profile");
            string oldId = Guid.NewGuid().ToString("N"), path = Path.Combine(root, oldId, "progression.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "{\"Version\":1,\"ProfileId\":\"" + oldId + "\",\"LifetimePlayerXP\":600,\"UnspentResearchPoints\":1,\"FirstLevelXP\":500,\"LevelStepXP\":250}");
            var migrated = store.Load(oldId);
            Require(migrated.Version == 2 && migrated.Level == 2 && migrated.UnspentResearchPoints == 1 &&
                migrated.PurchasedResearchIds.Length == 0 && File.ReadAllText(path).Contains("\"Version\":1"), "v1 migration preserves data without write-on-load");
            store.Save(ResearchRules.Purchase(migrated, upgrade));
            Require(store.Load(oldId).PurchasedResearchIds.Contains(upgrade.Id), "Migrated purchase persists");
            File.WriteAllText(path, "broken");
            Require(store.Load(oldId).UnspentResearchPoints == 1, "Recover old-schema backup");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        ResearchCatalog baseCatalog = GD.Load<ResearchCatalog>("res://SYSTEMS/Progression/Research/DefaultResearchCatalog.tres");
        ResearchCatalog Copy() => (ResearchCatalog)baseCatalog.Duplicate(true);
        var duplicate = Copy(); duplicate.Definitions.Add(duplicate.Definitions[1]); Expect(() => duplicate.Build(), "duplicate ID");
        var coordinate = Copy(); coordinate.Definitions[1].AxialPosition = Vector2I.Zero; Expect(() => coordinate.Build(), "coordinate collision");
        var missing = Copy(); missing.Definitions[1].PrerequisiteIds = new[] { "absent" }; Expect(() => missing.Build(), "missing dependency");
        var cycle = Copy(); cycle.Definitions[1].PrerequisiteIds = new[] { "citadel_master_1" }; Expect(() => cycle.Build(), "cycle");
        var cost = Copy(); cost.Definitions[1].CostRP = 0; Expect(() => cost.Build(), "nonpositive paid cost");
        Require(ResearchService.ResolveCitadelMaximum(1234) == 1234, "Profileless authored HP unchanged");
    }

    // =========================================================
    // Every expansion direction has equal spacing; selection uses exactly the transformed painted center.
    private void GraphGeometry()
    {
        var graph = new ResearchGraph { Nodes = ResearchService.Nodes, Size = new Vector2(600, 400) };
        AddChild(graph); graph.ResetView();
        foreach (Vector2I direction in ResearchGraph.Directions)
            Require(Math.Abs(ResearchGraph.AxialPosition(direction).Length() - 100 * Math.Sqrt(3)) < .01, "Six equal directions");
        Vector2 center = ResearchGraph.AxialPosition(new Vector2I(1, 0));
        Require(graph.HitNode(graph.GraphToScreen(center)) == "citadel_master_1", "Initial hit geometry");
        graph.ZoomAt(new Vector2(200, 100), 1.6f);
        Require(graph.HitNode(graph.GraphToScreen(center)) == "citadel_master_1", "Zoom/pan hit parity");
        graph._GuiInput(new InputEventKey { Keycode = Key.Right, Pressed = true });
        Require(graph.SelectedId == "citadel_master_1", "Keyboard selection");
        graph.Free();
    }

    // =========================================================
    // Production payment signals count real buildings exactly once, including timed jobs and frozen results.
    private async Task Telemetry(string scene)
    {
        using AmmoLoopFixture f = new(this, scene);
        var session = f.Main.GetNode<LevelSessionController>("LevelSession");
        var stats = session.RunStats; var work = f.Main.GetNode<ConstructionWorkManager>("World/ConstructionWork");
        var tower = f.Purchase<BattleTower>("battle", false);
        Require(stats.TowersPlaced == 1 && stats.TowersConstructed == 0, "Paid placement vs unfinished construction");
        f.Placement.Call("TryPlace", tower.GridCell);
        Require(stats.TowersPlaced == 1, "Occupied/failed placement excluded");
        Require(!TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 1, out _) && stats.UpgradesPurchased == 0, "Failed upgrade excluded");
        work.Advance(3); Require(stats.TowersConstructed == 1, "First operational transition counted");
        Require(TowerUpgradeController.TryUpgrade(tower, f.Wallet, f.Placement.Settings, 1, out _), "Real upgrade purchase");
        Require(stats.UpgradesPurchased == 1 && stats.TowersConstructed == 1, "Upgrade purchase does not count as new construction");
        work.Advance(4); Require(stats.TowersConstructed == 1 && stats.UpgradesPurchased == 1, "Upgrade completion never double-counts");
        stats._PhysicsProcess(5); GetTree().Paused = true; stats._PhysicsProcess(20); GetTree().Paused = false;
        Require(stats.MissionSeconds == 5, "Unpaused game clock");
        var pending = f.Purchase<BattleTower>("battle", false);
        Require(stats.TowersPlaced == 2 && stats.TowersConstructed == 1, "Second unfinished site");
        Expect(() => ResearchService.Purchase(f.Main, "citadel_master_1"), "Battlefield cannot purchase research");
        f.Main.GetNode<CitadelSession>("CitadelSession").Health.TakeDamage(int.MaxValue);
        stats._PhysicsProcess(100); Require(stats.Frozen && stats.MissionSeconds == 5, "Defeat freezes clock immediately");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(session.Snapshot.TowersPlaced == 2 && session.Snapshot.TowersConstructed == 1 &&
            session.Snapshot.UpgradesPurchased == 1 && session.Snapshot.MissionSeconds == 5, "Immutable real result counters");
        GetTree().Paused = false; work.Advance(100);
        Require(stats.TowersConstructed == 1 && session.Snapshot.TowersConstructed == 1, "Deferred cleanup cannot change frozen stats");
    }

    // =========================================================
    // Rejected actions must be explicit rather than a partial save or silent payment.
    private static void Expect(Action action, string name)
    { try { action(); } catch { return; } throw new Exception("Expected rejection: " + name); }
}
