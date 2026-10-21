// Outcome races, official wave-only counters, frozen snapshots and results/pause input priority.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using static MissionFixture;

internal static class MissionOutcomeChecks
{
    // =========================================================
    // Exercise genuine kills/arrivals and the existing notification order on both playable maps.
    internal static async Task Run(Node parent)
    {
        SceneTree tree = parent.GetTree();
        foreach (string path in new[] { MenuNavigation.Level001Scene, MenuNavigation.Level020Scene })
        {
            using MissionFixture f = new(parent, path);
            Require(f.Session.State == LevelSessionState.Playing && !f.Results.IsOpen && f.Session.Snapshot == null, "No startup result");
            f.Hud.SetTowerBarOpen(true, true); f.Hud.GetBuildButton("battle").EmitSignal(Button.SignalName.Pressed);
            f.Main.GetNode<Sprite2D>("TowerPlacement/PlacementGhost").Show();
            Enemy debug = f.Spawns.Spawn("basic_mk1", true); debug.TakeDamage(999999);
            Enemy debugArrival = f.Spawns.Spawn("basic_mk1", true); debugArrival.Arrive();
            Enemy last = f.Start(); last.TakeDamage(999999);
            await parent.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            LevelResultSnapshot snapshot = f.Session.Snapshot;
            Require(snapshot?.Outcome == LevelOutcome.Victory && f.Session.State == LevelSessionState.Victory && tree.Paused,
                "Final actual wave kill yields frozen victory on " + path);
            Require(snapshot.EnemiesKilled == 1 && snapshot.EnemiesBreached == 0 && snapshot.WavesCompleted == 1 && snapshot.TotalWaves == 1,
                "Official counters exclude F2/unowned kills and breaches");
            Require(snapshot.CitadelHealth == f.Health.CurrentHealth && snapshot.CitadelHealth > 0 && snapshot.CitadelMaxHealth == f.Health.MaxHealth,
                "Snapshot reads actual Citadel integrity");
            Require(snapshot.TowersPlaced == 0 && snapshot.TowersConstructed == 0 && snapshot.UpgradesPurchased == 0 && snapshot.MissionSeconds == 0 && snapshot.CreditsEarned == null && snapshot.CreditsSpent == null,
                "Tracked stats report actual zero; Credits remain explicitly untracked");
            Require(!f.Main.GetNode<TowerPlacementController>("TowerPlacement").IsPlacing && !f.Main.GetNode<Sprite2D>("TowerPlacement/PlacementGhost").Visible,
                "Outcome clears active placement/ghost");
            Require(!f.Results.ActionsAvailable && f.Results.DetailsButton.Disabled && f.Results.PrimaryButton.Disabled,
                "Actions locked during ordered reveal");
            Require(!f.Results.XPRevealCompleted && f.Results.GetNode<ResultsVectorBackdrop>("Screen/VectorBackdrop").MouseFilter == Control.MouseFilterEnum.Ignore,
                "R2 vector decorations ignore input while XP reveal is still pending");
            f.Results.PrimaryButton.EmitSignal(Button.SignalName.Pressed); Require(tree.Paused, "Hidden primary cannot navigate early");
            f.Pause.Open(); f.Pause.Resume(); Require(tree.Paused && !f.Pause.IsOpen, "Pause cannot resume or cover final results");
            parent.GetViewport().PushInput(new InputEventKey { Keycode = Key.Ctrl, Pressed = true });
            Require(!f.Hud.IsTowerBarOpen, "Results block build input");
            parent.GetViewport().PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = true });
            Require(f.Results.ActionsAvailable && tree.Paused && !f.Pause.IsOpen, "Escape skips reveal only, never resumes");
            parent.GetViewport().PushInput(new InputEventKey { Keycode = Key.F11, Pressed = true });
            Require(parent.GetViewport().IsInputHandled(), "Global F11 remains responsive in results");
            Require(f.Results.XPRevealCompleted && !f.Results.GetNode<ResultsVectorBackdrop>("Screen/VectorBackdrop").IsDefeat,
                "R2 victory theme and instant Skip reveal");
            Require(f.Results.PrimaryButton.Text == "NEXT LEVEL" && f.Results.PrimaryButton.Disabled == (path == MenuNavigation.Level020Scene),
                "Victory primary reflects actual next playable launcher");
            Require(f.Results.DetailsButton.Disabled && f.Results.DetailsButton.TooltipText.Contains("coming soon"), "Details remains disabled after Skip");
            Require(f.Results.FindChildren("*", "Label", true, false).OfType<Label>().Count(label => label.Text == "—") == 2
                && f.Results.FindChildren("*", "ProgressBar", true, false).OfType<ProgressBar>().Single().Value == 0,
                "Two untracked Credits statistics show dashes and Player XP remains an empty placeholder");
            f.Health.TakeDamage(f.Health.CurrentHealth); last.TakeDamage(999999); last.Arrive();
            await parent.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Require(ReferenceEquals(snapshot, f.Session.Snapshot) && f.Session.State == LevelSessionState.Victory,
                "Later events cannot overwrite or display a second result");
        }
        using (MissionFixture f = new(parent))
        {
            f.Health.Initialize(1); Enemy last = f.Start(); last.Arrive();
            Require(f.Session.State == LevelSessionState.Defeat && tree.Paused, "Lethal final breach latches/freeze defeat synchronously");
            await parent.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Require(f.Session.Snapshot.Outcome == LevelOutcome.Defeat && f.Session.Snapshot.EnemiesBreached == 1
                && f.Session.Snapshot.WavesCompleted == 1 && f.Waves.State == WaveManager.WaveState.AllWavesComplete,
                "Final lethal arrival includes breach/completion, and defeat beats victory");
            f.Results.SkipReveal(); Require(f.Results.PrimaryButton.Text == "RETRY MISSION" && !f.Results.PrimaryButton.Disabled, "Defeat offers Retry");
            Require(f.Results.GetNode<ResultsVectorBackdrop>("Screen/VectorBackdrop").IsDefeat &&
                ((Label)f.Results.FindChild("DebriefStatus", true, false)).Text.Contains("CRITICAL FAILURE"),
                "R2 defeat uses a red warning vector presentation");
        }
        using (MissionFixture f = new(parent))
        {
            f.Start().TakeDamage(999999); f.Health.TakeDamage(int.MaxValue);
            await parent.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Require(f.Session.State == LevelSessionState.Defeat && f.Session.Snapshot.Outcome == LevelOutcome.Defeat,
                "Destruction in the same frame also defeats an already queued victory");
        }
        using (MissionFixture f = new(parent))
        {
            f.Health.Initialize(1); Enemy first = f.Start(2); f.Pause.Open(); first.Arrive();
            Require(tree.Paused && !f.Pause.IsOpen && f.Session.State == LevelSessionState.Defeat, "Defeat safely takes ownership from open Pause");
            await parent.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Require(f.Session.Snapshot.WavesCompleted == 0 && f.Waves.PlannedRemaining == 1, "Mid-wave Citadel destruction ends immediately");
            await parent.ToSignal(tree.CreateTimer(1.4), SceneTreeTimer.SignalName.Timeout);
            Require(f.Results.ActionsAvailable && f.Results.PrimaryButton.IsVisibleInTree(), "Natural stat reveal continues while battlefield is paused");
        }
        using (MissionFixture f = new(parent))
        {
            Enemy first = f.Start(2); first.TakeDamage(999999);
            await parent.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Require(f.Session.State == LevelSessionState.Playing && !f.Results.IsOpen && f.Waves.PlannedRemaining == 1,
                "Zero currently alive cannot win while later authored spawns remain");
        }
        using (MissionFixture f = new(parent))
        {
            f.Health.TakeDamage(int.MaxValue); f.Main.Free();
            await parent.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Require(!tree.Paused, "Unload before deferred defeat snapshot clears pause and cancels presentation");
        }
        using (MissionFixture f = new(parent))
        {
            f.Waves.Configure(Array.Empty<WaveDefinition>(), f.Spawns.Catalog.ToSceneMap());
            await parent.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Require(f.Session.State == LevelSessionState.Playing && !f.Results.IsOpen, "Disabled/empty wave set cannot auto-win");
        }
        await SpawnEntryHazard(parent);
        GD.Print("PASS: R1 victory/defeat priority, lethal final/mid-wave arrivals, wave-only stats, snapshot latch, placement cancellation, pause takeover, F11 and paused natural/skipped reveals");
    }
    // =========================================================
    // Entry hazards can kill before WaveManager receives the returned ship; provenance must already be recorded.
    private static async Task SpawnEntryHazard(Node parent)
    {
        using MissionFixture f = new(parent);
        WorldGrid grid = f.Spawns.Paths.Navigation.Grid; LavaTileConfig lava = grid.InteractiveTiles.LavaConfig;
        var original = (lava.DamagePerTick, lava.DamageIntervalSeconds, lava.DamageOnEntry, lava.DamageType);
        float offset = f.Spawns.Paths.Config.PortalSpawnOffsetPixels;
        string portal = grid.Level.Settings.ActivePortals[0].ToString();
        Vector2I cell = grid.WorldToCell(grid.Level.GetNode<Node2D>(portal).GetNode<Marker2D>("SpawnExit").GlobalPosition);
        int source = grid.Level.Interactive.GetCellSourceId(cell), alt = grid.Level.Interactive.GetCellAlternativeTile(cell);
        Vector2I atlas = grid.Level.Interactive.GetCellAtlasCoords(cell);
        try
        {
            lava.DamagePerTick = 10000; lava.DamageIntervalSeconds = .5f; lava.DamageOnEntry = true; lava.DamageType = DamageType.Energy;
            f.Spawns.Paths.Config.PortalSpawnOffsetPixels = 0;
            grid.Level.Interactive.SetCell(cell, 1, Vector2I.Zero); grid.RebuildTerrain();
            f.Waves.Configure(new[] { WaveBuilder.Wave(1, WaveBuilder.Batch(1, 0, 0, WaveBuilder.Enemy("basic_mk1", portal: portal))) }, f.Spawns.Catalog.ToSceneMap());
            Require(f.Waves.StartWave(), "Hazard fixture starts");
            await parent.ToSignal(parent.GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(f.Session.Snapshot?.Outcome == LevelOutcome.Victory && f.Session.Snapshot.EnemiesKilled == 1,
                "Wave-owned spawn-time Lava kill is included in official mission totals");
        }
        finally
        {
            if (source < 0) grid.Level.Interactive.EraseCell(cell); else grid.Level.Interactive.SetCell(cell, source, atlas, alt);
            lava.DamagePerTick = original.Item1; lava.DamageIntervalSeconds = original.Item2; lava.DamageOnEntry = original.Item3; lava.DamageType = original.Item4;
            f.Spawns.Paths.Config.PortalSpawnOffsetPixels = offset; grid.RebuildTerrain();
        }
    }

}
