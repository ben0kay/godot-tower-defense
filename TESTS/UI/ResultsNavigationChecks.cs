// Real Retry/Next/Menu transitions, strict three-digit IDs and explicit playable-launcher validation.
using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using static MissionFixture;

internal static class ResultsNavigationChecks
{
    // =========================================================
    // Keep the runner outside CurrentScene so production buttons can replace actual battlefield scenes.
    internal static async Task Run(Node owner)
    {
        foreach (var pair in new[] { ("Level001", "Level002"), ("Level002", "Level003"), ("Level006", "Level007"),
            ("Level009", "Level010"), ("Level099", "Level100") })
            Require(PlayableLevelCatalog.FollowingName(pair.Item1) == pair.Item2, "Zero-padded next ID " + pair.Item1);
        foreach (string invalid in new[] { "Level000", "Level999", "level001", "Level1", "Level1000", "Level/001", "Level-01" })
            Require(PlayableLevelCatalog.FollowingName(invalid) == null, "Reject malformed/out-of-range ID " + invalid);
        Require(PlayableLevelCatalog.NextScene("Level001") == MenuNavigation.Level002Scene
            && PlayableLevelCatalog.NextScene("Level002") == "res://LEVELS/Level003/Level003Mission.tres"
            && PlayableLevelCatalog.NextScene("Level003") == null
            && PlayableLevelCatalog.NextScene("Level020") == null,
            "Levels 001→002→003 are sequential; unfinished 004 and 021 cannot be skipped");
        Require(PlayableLevelCatalog.IsPlayable(MenuNavigation.Level001Scene) && PlayableLevelCatalog.IsPlayable(MenuNavigation.Level020Scene)
            && !PlayableLevelCatalog.IsPlayable("res://LEVELS/Level020/Level020.tscn"), "Map scenes cannot masquerade as battlefields");
        SceneTree tree = owner.GetTree(); tree.CurrentScene = null;
        PlayerProfile profile = new() { Id = Guid.NewGuid().ToString("N"), Name = "Results fixture" };
        typeof(ProfileStore).GetField("_profiles", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, new List<PlayerProfile> { profile });
        ProfileStore.Select(profile.Id);
        MenuNavigation.Open(owner, MenuNavigation.Level001Scene); Node main = await Scene<Node2D>(owner);
        LevelSessionController session = main.GetNode<LevelSessionController>("LevelSession");
        main.GetNode<CitadelSession>("CitadelSession").Health.TakeDamage(int.MaxValue);
        await owner.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        LevelResults results = main.GetNode<LevelResults>("LevelResults"); results.SkipReveal();
        FieldInfo pathField = typeof(LevelSessionController).GetField("_scenePath", BindingFlags.Instance | BindingFlags.NonPublic);
        pathField.SetValue(session, "res://TESTS/MissingBattlefield.tscn");
        results.PrimaryButton.EmitSignal(Button.SignalName.Pressed);
        Require(tree.Paused && results.DismissError(), "Missing retry scene shows error and preserves results pause");
        pathField.SetValue(session, MenuNavigation.Level001Scene);
        results.PrimaryButton.EmitSignal(Button.SignalName.Pressed);
        Node retry = await Scene<Node2D>(owner);
        Require(retry != main && retry.SceneFilePath == MenuNavigation.Level001Scene && !tree.Paused
            && retry.GetNode<LevelSessionController>("LevelSession").State == LevelSessionState.Playing,
            "Retry replaces the actual current launcher, unpaused and fresh");
        // A win follows enabled consecutive missions; Level020 remains a separate direct destination.
        WaveManager waves = retry.GetNode<WaveManager>("Waves"); SpawnManager spawns = retry.GetNode<SpawnManager>("World/EnemySystems/Spawning");
        waves.Configure(new[] { WaveBuilder.Wave(1, WaveBuilder.Batch(1, 0, 0, WaveBuilder.Enemy("basic_mk1"))) }, spawns.Catalog.ToSceneMap());
        Require(waves.StartWave(), "Navigation fixture starts a real wave"); spawns.Registry.Living[0].TakeDamage(999999);
        await owner.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        results = retry.GetNode<LevelResults>("LevelResults"); results.SkipReveal();
        Require(!results.PrimaryButton.Disabled, "Level001 advances to the playable tutorial Level002");
        results.PrimaryButton.EmitSignal(Button.SignalName.Pressed);
        Node next = await Scene<Node2D>(owner);
        Require(next.SceneFilePath == MenuNavigation.Level002Scene && !tree.Paused && next.GetNode<CitadelSession>("CitadelSession").Health.CurrentHealth > 0,
            "Next loads the tutorial Level002 battlefield, never the raw map");
        // The tutorial can now advance to the editable Level003 mission after victory.
        waves = next.GetNode<WaveManager>("Waves"); spawns = next.GetNode<SpawnManager>("World/EnemySystems/Spawning");
        waves.Configure(new[] { WaveBuilder.Wave(1, WaveBuilder.Batch(1, 0, 0, WaveBuilder.Enemy("basic_mk1"))) }, spawns.Catalog.ToSceneMap());
        Require(waves.StartWave(), "Level002 starts fixture wave"); spawns.Registry.Living[0].TakeDamage(999999);
        await owner.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        results = next.GetNode<LevelResults>("LevelResults"); results.SkipReveal();
        Require(!results.PrimaryButton.Disabled, "Level002 victory offers Next Level003");
        results.PrimaryButton.EmitSignal(Button.SignalName.Pressed);
        BattlefieldLauncher third = await Scene<BattlefieldLauncher>(owner);
        WorldLevel blank = third.GetNode<WorldGrid>("World/Grid").Level;
        Require(third.Mission.Number == 3 && blank.SceneFilePath == "res://LEVELS/Level003/Level003.tscn"
            && blank.Settings.WaveSetId == "level003" && third.GetNode<WaveManager>("Waves").TotalWaves == 4,
            "Next opens four-wave Level003 in the shared battlefield");
        Require(blank.Passable.GetUsedCells().Count == 0 && blank.Impassable.GetUsedCells().Count == 0
            && blank.Interactive.GetUsedCells().Count == 0 && blank.Decorations.GetUsedCells().Count == 0,
            "Level003 remains completely unpainted for the editor");
        Require(PlayableLevelCatalog.NextScene("Level003") == null, "Missing Level004 never skips to020");
        third.GetNode<CitadelSession>("CitadelSession").Health.TakeDamage(int.MaxValue);
        await owner.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        results = third.GetNode<LevelResults>("LevelResults"); results.SkipReveal();
        ButtonFor(results, "RETURN TO CAMPAIGN").EmitSignal(Button.SignalName.Pressed);
        CampaignMenu campaign = await Scene<CampaignMenu>(owner);
        Require(campaign.FindChildren("*", "Label", true, false).OfType<Label>().Any(label => label.Text.Contains("PLAYER LEVEL 2")), "Campaign displays progression earned from actual mission wins");
        Require(!tree.Paused && owner.GetNode<GameInput>("/root/GameInput").BindingCount == 0, "Menu exits unpaused with no stale gameplay/results bindings");
        ProfileStore.ClearSelection(); MenuNavigation.Open(owner, MenuNavigation.Level020Scene);
        Node direct = await Scene<Node2D>(owner); direct.GetNode<CitadelSession>("CitadelSession").Health.TakeDamage(int.MaxValue);
        await owner.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        results = direct.GetNode<LevelResults>("LevelResults"); results.SkipReveal(); results.PrimaryButton.EmitSignal(Button.SignalName.Pressed);
        direct = await Scene<Node2D>(owner);
        Require(direct.SceneFilePath == MenuNavigation.Level020Scene && !tree.Paused, "Level020 Retry also reloads its current playable launcher");
        direct.GetNode<CitadelSession>("CitadelSession").Health.TakeDamage(int.MaxValue);
        await owner.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        results = direct.GetNode<LevelResults>("LevelResults"); results.SkipReveal(); ButtonFor(results, "RETURN TO CAMPAIGN").EmitSignal(Button.SignalName.Pressed);
        await Scene<BootScreen>(owner); Require(!tree.Paused, "Direct F6/profileless menu exit safely redirects to Boot");
        GD.Print("PASS: R1 Retry/Next/Menu including blank Level003, strict no-skip progression, launcher validation and binding cleanup");
    }

    // =========================================================
    // Bound transition waits also work while the departing scene is paused.
    private static async Task<T> Scene<T>(Node owner) where T : Node
    {
        for (int i = 0; i < 90; i++)
        { await owner.ToSignal(owner.GetTree(), SceneTree.SignalName.ProcessFrame); if (owner.GetTree().CurrentScene is T scene && scene.IsNodeReady()) return scene; }
        throw new InvalidOperationException("Results navigation failed: " + typeof(T).Name);
    }
}
