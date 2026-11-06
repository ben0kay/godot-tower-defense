// Small isolated SPAWN0/1 fixture: real scheduler/source service, sparse wave numbers and open boundary terrain.
using Godot;
using System;
using System.Linq;
using static WaveBuilder;
using static MissionFixture;

public partial class PortalActivationChecks : Node
{
    // =========================================================
    // Manual owner runner; no authored level/map/resource edits and no prolonged simulation.
    public override void _Ready()
    {
        try
        {
            using DeathSpawnFixture f = new(this, MenuNavigation.Level001Scene);
            var grid = f.Grid; var spawns = f.Spawns; var waves = f.Waves;
            LevelConfig original = grid.Level.Settings;
            grid.Level.Settings = (LevelConfig)original.Duplicate(true);
            try { CheckActivation(f); CheckEdges(f); }
            finally { grid.Level.Settings = original; }
            GD.Print("PASS: SPAWN0/1 activation, source exclusion, exact wave numbers, restore, reservation and edge baseline");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }

    private static void CheckActivation(DeathSpawnFixture f)
    {
        var grid = f.Grid; var spawns = f.Spawns; var waves = f.Waves;
        string firstPath = grid.Level.Settings.ActivePortals[0].ToString(), futurePath = grid.Level.Settings.ActivePortals[1].ToString();
        var first = grid.Level.GetNode<EnemyPortal>(firstPath);
        var future = grid.Level.GetNode<EnemyPortal>(futurePath);
        future.AppearAtWave = 6; spawns.Paths.Config.PortalSpawnOffsetPixels = 0;
        var data = new[] { Wave(2, Batch(1, 0, 0, Enemy("basic_mk1", portal: firstPath))),
            Wave(6, Batch(1, 0, 0, Enemy("basic_mk1", portal: futurePath))), Wave(9, Batch(1, 0, 0, Enemy("basic_mk1"))) };
        var scenes = spawns.Catalog.ToSceneMap(); waves.Configure(data, scenes);
        Require(waves.State == WaveManager.WaveState.Ready && first.IsSpawnActive && !future.IsSpawnActive
            && !future.GetNode<Node2D>("Visuals").Visible, "Future portal hidden; START portal preserved");
        var rng = new RandomNumberGenerator { Seed = 12 };
        Require(spawns.SpawnWave("basic_mk1", rng, futurePath) == null, "Required dormant portal refuses spawn without fallback");
        for (int i = 0; i < 8; i++)
        {
            Enemy debug = spawns.SpawnRandomPortal(scenes["basic_mk1"], rng);
            Require(debug != null && debug.GlobalPosition == first.GlobalPosition, "F2/random sources exclude dormant portal");
            debug.TakeDamage(99999);
        }
        Require(grid.PlacementFailure(grid.WorldToCell(future.GlobalPosition)).Contains("Reserved"), "Invisible footprint stays reserved");
        // Leave only one way out of the future source; even before activation it must not be sealed.
        Vector2I anchor = grid.WorldToCell(future.GlobalPosition);
        for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
            if ((x != 0 || y != 0) && (x != 1 || y != 0)) grid.Level.Impassable.SetCell(anchor + new Vector2I(x, y), 0, Vector2I.Zero);
        grid.RebuildTerrain();
        Require(spawns.PlacementFailure(anchor + Vector2I.Right).Length > 0, "Placement protects the future portal route before reveal");
        int revision = grid.NavigationRevision; long comparisons = spawns.CitadelAssignments.SelectionQueries;
        Require(waves.StartWave(), "First authored Wave 2 starts");
        spawns.Registry.Living.Single().TakeDamage(99999);
        ulong state = waves.SavedRandomState;
        waves.RestoreCheckpoint(1, state);
        Require(!future.IsSpawnActive, "Restore after Wave 2 leaves next Wave 6 portal dormant");
        bool revealedBeforeEvent = false;
        void Started(WaveDefinition wave) { if (wave.Number == 6) revealedBeforeEvent = future.IsSpawnActive; }
        waves.WaveStarted += Started;
        Require(waves.StartWave(), "Accepted Wave 6 starts"); waves.WaveStarted -= Started;
        Require(revealedBeforeEvent && future.IsSpawnActive && future.GetNode<Node2D>("Visuals").Visible
            && spawns.Registry.Living.Single().GlobalPosition == future.GlobalPosition, "Activation precedes observers and first required ship");
        Require(grid.NavigationRevision == revision && spawns.CitadelAssignments.SelectionQueries == comparisons,
            "Pure activation never changes navigation or assigned destination");
        spawns.Registry.Living.Single().TakeDamage(99999);
        waves.RestoreCheckpoint(2, waves.SavedRandomState);
        Require(future.IsSpawnActive, "Restore after completed Wave 6 preserves activation using authored Number");
        int eligibility = spawns.PortalEligibilityRevision;
        spawns.ActivatePortalsForWave(6);
        Require(spawns.PortalEligibilityRevision == eligibility, "Activation is idempotent");
        waves.Configure(new[] { Wave(2, Batch(1, 0, 0, Enemy("basic_mk1", portal: futurePath))), data[1] }, scenes);
        Require(waves.State == WaveManager.WaveState.PausedError && waves.StatusMessage.Contains("appears at Wave 6"),
            "Required portal before appearance fails setup instead of retrying forever");
        future.AppearAtWave = 0;
    }

    // =========================================================
    // Single-Citadel all-edges baseline; one blocked corner is never sampled and Both still prefers portals.
    private static void CheckEdges(DeathSpawnFixture f)
    {
        var settings = f.Grid.Level.Settings;
        settings.SpawnMode = LevelConfig.SpawnSourceMode.MapEdges;
        settings.AllowedEdges = LevelConfig.MapEdge.Top | LevelConfig.MapEdge.Bottom | LevelConfig.MapEdge.Left | LevelConfig.MapEdge.Right;
        f.Grid.Level.Impassable.SetCell(Vector2I.Zero, 0, Vector2I.Zero); f.Grid.RebuildTerrain();
        var seen = LevelConfig.MapEdge.None; var rng = new RandomNumberGenerator { Seed = 87 };
        for (int i = 0; i < 64; i++)
        {
            Enemy ship = f.Spawns.SpawnWave("basic_mk1", rng);
            Require(ship != null, "Reachable map boundary spawns");
            Vector2I cell = f.Grid.WorldToCell(ship.GlobalPosition);
            Require(cell != Vector2I.Zero && !f.Grid.BlocksEnemies(cell), "Blocked edge cell excluded");
            if (cell.X == 0) seen |= LevelConfig.MapEdge.Left;
            if (cell.Y == 0) seen |= LevelConfig.MapEdge.Top;
            if (cell.X == f.Grid.Config.WidthCells - 1) seen |= LevelConfig.MapEdge.Right;
            if (cell.Y == f.Grid.Config.HeightCells - 1) seen |= LevelConfig.MapEdge.Bottom;
            ship.TakeDamage(99999);
        }
        Require(seen == settings.AllowedEdges, "All four valid sides participate");
        settings.SpawnMode = LevelConfig.SpawnSourceMode.Both; f.Grid.RebuildTerrain();
        Enemy preferred = f.Spawns.SpawnWave("basic_mk1", rng);
        Require(preferred != null && settings.ActivePortals.Any(p => preferred.GlobalPosition == f.Grid.Level.GetNode<Node2D>(p).GlobalPosition),
            "Both retains portal preference"); preferred.TakeDamage(99999);
    }
}
