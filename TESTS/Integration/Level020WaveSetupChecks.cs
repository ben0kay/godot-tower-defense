// Exercise the serialized wave-source resource and real launcher, catching static-class recipe regressions.
using Godot;
using System;
using static MissionFixture;

public partial class Level020WaveSetupChecks : Node
{
    #region Runner
    // =========================================================
    // Load the actual saved config instead of testing Create() in isolation.
    public override async void _Ready()
    {
        try
        {
            using (MissionFixture f = new(this, MenuNavigation.Level020Scene))
            {
                LevelConfig config = f.Spawns.Paths.Navigation.Grid.Level.Settings;
                Require(config.WaveSource is Level020Waves && config.WaveSource.Build().Length == 7,
                    "Serialized Level020 recipe is an assignable resource containing all seven waves");
                Require(f.Waves.State == WaveManager.WaveState.Ready && f.Waves.TotalWaves == 7,
                    "Real Level020 launcher initializes waves without setup error: " + f.Waves.StatusMessage);
                Require(f.Waves.StartWave() && f.Waves.CurrentWave == 1 && f.Waves.State != WaveManager.WaveState.PausedError,
                    "First authored wave starts through the real scheduler: " + f.Waves.StatusMessage);
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: serialized Level020 wave source, seven-wave setup and real StartWave"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    #endregion
}
