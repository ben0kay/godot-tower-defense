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
        Require(PlayableLevelCatalog.NextScene("Level001") == null && PlayableLevelCatalog.NextScene("Level020") == null,
            "Unfinished Level002 and missing Level021 cannot resolve; Level020 is never skipped to");
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
        // A win cannot skip the unfinished Level002; the preserved Level020 is still directly playable.
        WaveManager waves = retry.GetNode<WaveManager>("Waves"); SpawnManager spawns = retry.GetNode<SpawnManager>("World/EnemySystems/Spawning");
        waves.Configure(new[] { WaveBuilder.Wave(1, WaveBuilder.Batch(1, 0, 0, WaveBuilder.Enemy("basic_mk1"))) }, spawns.Catalog.ToSceneMap());
        Require(waves.StartWave(), "Navigation fixture starts a real wave"); spawns.Registry.Living[0].TakeDamage(999999);
        await owner.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        results = retry.GetNode<LevelResults>("LevelResults"); results.SkipReveal();
        Require(results.PrimaryButton.Disabled, "Level001 cannot advance into the unfinished map or skip to Level020");
        MenuNavigation.Open(owner, MenuNavigation.Level020Scene);
        Node next = await Scene<Node2D>(owner);
        Require(next.SceneFilePath == MenuNavigation.Level020Scene && !tree.Paused && next.GetNode<CitadelSession>("CitadelSession").Health.CurrentHealth > 0,
            "Direct Level020 launch loads its renamed battlefield, never the raw map");
        // Menu action works without needing a further level.
        waves = next.GetNode<WaveManager>("Waves"); spawns = next.GetNode<SpawnManager>("World/EnemySystems/Spawning");
        waves.Configure(new[] { WaveBuilder.Wave(1, WaveBuilder.Batch(1, 0, 0, WaveBuilder.Enemy("basic_mk1"))) }, spawns.Catalog.ToSceneMap());
        Require(waves.StartWave(), "Last available level starts fixture wave"); spawns.Registry.Living[0].TakeDamage(999999);
        await owner.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        results = next.GetNode<LevelResults>("LevelResults"); results.SkipReveal();
        Require(results.PrimaryButton.Disabled && results.PrimaryButton.TooltipText == "No next level available.", "Missing next remains visible/disabled while other exits work");
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
        GD.Print("PASS: R1 actual Retry/Next/Menu navigation, launcher validation, missing-next safety, carry boundaries and binding cleanup");
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
