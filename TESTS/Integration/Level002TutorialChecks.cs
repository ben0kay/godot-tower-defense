// Exercise all four authored tutorial waves on the user's real map through the existing scheduler/results.
using Godot;
using System;
using System.Linq;
using static MissionFixture;

public partial class Level002TutorialChecks : Node
{
    #region Runner
    // =========================================================
    // Disable movement only in this fixture so every authored spawn/reward/clear can be counted exactly.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            using MissionFixture f = new(this, MenuNavigation.Level002Scene);
            Require(f.Spawns.Paths.Navigation.Grid.Level.Settings.WaveSetId == "level002"
                && f.Spawns.Paths.Navigation.Grid.Level.Settings.StartingCredits == 1000,
                "Tutorial launcher uses its own wave set and starting funds");
            WaveDefinition[] data = WaveCatalog.GetWaves("level002");
            Require(data.Length == 4, "Exactly four tutorial waves");
            WaveCatalog.Validate(data, f.Spawns.Catalog.ToSceneMap());
            int[] totals = { 12, 16, 20, 24 };
            for (int wave = 0; wave < 4; wave++)
            {
                Require(f.Waves.StartWave(), "Tutorial wave starts manually");
                int count = 0;
                for (int tick = 0; tick < 200; tick++)
                {
                    f.Waves._PhysicsProcess(2);
                    foreach (Enemy ship in f.Spawns.Registry.Living.ToArray())
                    {
                        Require(ship.CurrentShield == 0 && new[] { "basic_mk1", "scout_mk1", "medium_mk1" }.Contains(ship.Definition.EnemyId),
                            "Only the three introductory unshielded types appear");
                        ship.TakeDamage(999999); count++;
                    }
                    if (f.Waves.PlannedRemaining == 0 && f.Waves.AliveCount == 0) break;
                }
                Require(count == totals[wave], "Real map spawns every scheduled tutorial enemy: wave " + (wave + 1));
                if (wave < 3) Require(f.Session.State == LevelSessionState.Playing, "No victory between waves");
            }
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(f.Session.Snapshot?.Outcome == LevelOutcome.Victory && f.Session.Snapshot.EnemiesKilled == 72,
                "Fourth-wave completion produces one real victory with all 72 kills");
            GD.Print("PASS: Level002 four manual tutorial waves (12/16/20/24), valid portal routes, three unshielded types and real victory");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
    #endregion
}
