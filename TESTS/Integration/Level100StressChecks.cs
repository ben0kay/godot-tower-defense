// Source/scene regression for Level100 large-wave authoring; does not require painted spawn routes.
using Godot;
using System;
using System.Linq;
using static MissionFixture;

public partial class Level100StressChecks : Node
{
    #region Runner
    // =========================================================
    // Verify the real saved resources, map template and deterministic exact enemy quotas.
    public override void _Ready()
    {
        try
        {
            MissionDefinition mission = GD.Load<MissionDefinition>("res://LEVELS/Level100/Level100Mission.tres");
            LevelConfig config = GD.Load<LevelConfig>("res://LEVELS/Level100/Level100Config.tres");
            Require(mission != null && mission.Number == 100 && mission.Enabled && mission.LegacyLauncher == "",
                "Level100 is an enabled data-driven stress mission with no special launcher");
            Require(mission.MapScenePath == "res://LEVELS/Level100/Level100.tscn", "Mission references its own map");
            Require(MissionCatalog.Default.Entries().Any(m => m.Number == 100),
                "Level100 appears in the standard mission catalog");
            Require(config != null && config.StartingCredits == 100000 && config.CitadelMaxHealth == 10000
                    && config.SpawnMode == LevelConfig.SpawnSourceMode.Portals && config.ActivePortals.Count == 5,
                "Stress mission starting values and five authorized portals");
            Require(config.WaveSource is Level100Waves && config.WaveSetId == "level100",
                "WaveSource is an Inspector-assignable Level100Waves resource");
            WaveDefinition[] waves = config.WaveSource.Build();
            Require(waves.Length == 5, "Exactly five stress waves");
            WaveCatalog.Validate(waves, EnemyCatalog.Default.ToSceneMap());
            int[] totals = { 100, 200, 300, 400, 500 };
            RandomNumberGenerator random = new(); random.Seed = 12345;
            int all = 0;
            for (int i = 0; i < waves.Length; i++)
            {
                WaveDefinition wave = waves[i];
                Require(wave.Number == i + 1, "Contiguous wave numbering");
                Require(wave.Batches.Length == 8, "Four mixed swarms alternating with four gunners");
                int count = 0, gunner = 0;
                for (int b = 0; b < wave.Batches.Length; b++)
                {
                    WaveBatch batch = wave.Batches[b];
                    WaveEnemyEntry[] group = WaveCatalog.Compose(batch, random);
                    count += group.Length;
                    int gunnersInGroup = group.Count(e => e.EnemyId == "gunner_drone_mk1");
                    if (b % 2 == 1) Require(batch.Count == 5 && gunnersInGroup == 5,
                        "Each spaced minigun group contains precisely five armed Gunner Drones");
                    else Require(gunnersInGroup == 0, "Mixed swarms contain no hidden Gunner Drones");
                    gunner += gunnersInGroup;
                }
                Require(count == totals[i] && gunner == 20, "Wave " + wave.Number + " has exact enemy and gunner totals");
                all += count;
            }
            Require(all == 1500, "Five waves schedule 1,500 enemies in all");
            WorldLevel map = GD.Load<PackedScene>(mission.MapScenePath).Instantiate<WorldLevel>();
            try
            {
                Require(map.Settings == config || (map.Settings.WaveSetId == "level100" && map.Settings.StartingCredits == 100000),
                    "Map uses the saved Level100 config");
                Require(map.Passable.GetUsedCells().Count == 0 && map.Impassable.GetUsedCells().Count == 0
                    && map.Interactive.GetUsedCells().Count == 0 && map.Decorations.GetUsedCells().Count == 0,
                    "Template terrain stays entirely blank for the user's painting");
                foreach (NodePath path in config.ActivePortals)
                    Require(map.GetNodeOrNull<Node2D>(path) != null, "Configured portal exists in map: " + path);
                Require(map.GetNodeOrNull<Node2D>("Objectives/Citadel") != null, "Citadel remains editable");
            }
            finally { map.Free(); }
            GD.Print("PASS: Level100 blank terrain, campaign registry, five portals, 100k credits, 1500 scheduled enemies, 20 gunners/wave.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    #endregion
}
