// Verify preserved Five Gates content, fresh template copy and explicit playable identities after renumbering.
using Godot;
using System;
using static MissionFixture;

public partial class LevelReorganizationChecks : Node
{
    #region Runner
    // =========================================================
    // Inspect actual resources and launch the renamed map through the shared battlefield fixture.
    public override void _Ready()
    {
        try
        {
            WorldLevel fresh = GD.Load<PackedScene>("res://LEVELS/Level002/Level002.tscn").Instantiate<WorldLevel>();
            WorldLevel template = GD.Load<PackedScene>("res://LEVELS/LevelTemplate.tscn").Instantiate<WorldLevel>();
            try
            {
                Require(fresh.Name == "Level002" && fresh.Settings.ResourcePath == "res://LEVELS/Level002/Level002Config.tres"
                    && fresh.Settings != template.Settings, "New Level002 has independent identity and settings");
                Require(fresh.Passable.GetUsedCells().Count == 0 && fresh.Impassable.GetUsedCells().Count == 0
                    && fresh.Interactive.GetUsedCells().Count == 0 && fresh.Decorations.GetUsedCells().Count == 0,
                    "Fresh map contains no Five Gates terrain");
                Require(fresh.GetNode("Portals").GetChildCount() == template.GetNode("Portals").GetChildCount()
                    && fresh.GetNodeOrNull<Node>("Objectives/Citadel") != null && string.IsNullOrEmpty(fresh.Settings.WaveSetId),
                    "Template objects retained without borrowing old waves");
            }
            finally { fresh.Free(); template.Free(); }
            Require(PlayableLevelCatalog.NextScene("Level001") == null && PlayableLevelCatalog.NextScene("Level020") == null
                && !PlayableLevelCatalog.IsPlayable("res://LEVELS/Level002/Level002.tscn"),
                "Unfinished map is not playable and progression never skips to Level020");
            using (MissionFixture fixture = new(this, MenuNavigation.Level020Scene))
            {
                WorldLevel map = fixture.Spawns.Paths.Navigation.Grid.Level;
                Require(map.SceneFilePath == "res://LEVELS/Level020/Level020.tscn"
                    && PlayableLevelCatalog.Identify(fixture.Main, map) == "Level020"
                    && map.Settings.WaveSetId == "level020" && map.Settings.ActivePortals.Count == 5
                    && WaveCatalog.GetWaves("level020").Length == 4, "Renamed launcher loads and identifies Level020");
                Require(fixture.Spawns.Spawn("basic_mk1", true) != null, "Preserved Five Gates still spawns through a valid portal");
            }
            GD.Print("PASS: Level020 preserved terrain/config/waves/launcher, fresh independent Level002 and gap-safe progression");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    #endregion
}
