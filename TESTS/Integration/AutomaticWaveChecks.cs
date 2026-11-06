// Focused owner-run checks for pause-aware automatic progression, early start, checkpoint timing and live victory hold.
using Godot;
using System;
using System.Linq;
using static WaveBuilder;
using static MissionFixture;

public partial class AutomaticWaveChecks : Node
{
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        PlayerProfile selected = ProfileStore.Selected;
        ProfileStore.ClearSelection(); // This fixture must never issue/award a real profile reward.
        try
        {
            Countdown();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            using (MissionFixture f = new(this))
            {
                var data = new[] { Wave(1, Batch(1, 0, 0, Enemy("basic_mk1"))) };
                f.Waves.Configure(data, f.Spawns.Catalog.ToSceneMap());
                f.Spawns.RestorePortalActivation(data, 0);
                f.Session.VictoryResultsDelaySeconds = 4;
                f.Waves.StartWave(); f.Spawns.Registry.Living.Single().TakeDamage(999999);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(f.Session.IsAwaitingResults && !GetTree().Paused && !f.Results.IsOpen &&
                    f.Session.Snapshot?.Outcome == LevelOutcome.Victory && f.Session.RunStats.Frozen,
                    "Victory locks its snapshot but keeps the map live before Results");
                Require(!f.Waves.CanStart && !f.Waves.AutoStartPending, "Outcome retires all future starts");
                var snapshot = f.Session.Snapshot;
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(ReferenceEquals(snapshot, f.Session.Snapshot) && !GetTree().Paused,
                    "Visual hold cannot replace the final snapshot or freeze the map early");
                Timer timer = f.Session.GetChildren().OfType<Timer>().Single();
                timer.Stop(); timer.EmitSignal(Timer.SignalName.Timeout);
                Require(GetTree().Paused && f.Results.IsOpen && !f.Session.IsAwaitingResults,
                    "The map pauses when Results actually opens");
            }
            GD.Print("PASS: automatic wave defaults/overrides, pause, early start, checkpoint countdown, outcome cancellation and live results delay");
            GetTree().Quit();
        }
        catch (Exception error) { GetTree().Paused = false; GD.PushError(error.ToString()); GetTree().Quit(1); }
        finally { if (selected != null) ProfileStore.Select(selected.Id); }
    }

    // =========================================================
    // Advance the real scheduler directly; no long simulated playthrough or wall-clock countdown is required.
    private void Countdown()
    {
        using MissionFixture f = new(this);
        var waves = f.Waves;
        var data = new[] { Wave(1, Batch(1, 0, 0, Enemy("basic_mk1"))),
            Wave(2, Batch(1, 0, 0, Enemy("basic_mk1"))) with { NextWaveDelaySeconds = 5 },
            Wave(3, Batch(1, 0, 0, Enemy("basic_mk1"))) };
        waves.Configure(data, f.Spawns.Catalog.ToSceneMap()); f.Spawns.RestorePortalActivation(data, 0);
        int starts = 0; waves.WaveStarted += _ => starts++;
        waves._PhysicsProcess(100);
        Require(waves.CurrentWave == 0 && !waves.AutoStartPending && starts == 0, "First wave remains manual indefinitely");
        waves.StartWave(); f.Spawns.Registry.Living.Single().TakeDamage(999999);
        Require(waves.AutoStartSecondsRemaining == 30 && waves.CanStart &&
            f.Hud.GetNode<Label>("TopBar/Status/AutoStart").Text.Contains("00:30"),
            "Default 30-second countdown appears beside an enabled early-start button");
        waves._PhysicsProcess(10);
        GetTree().Paused = true; waves._PhysicsProcess(10); GetTree().Paused = false;
        Require(waves.AutoStartSecondsRemaining == 20, "Pause does not consume countdown time");
        waves.RestoreCheckpoint(1, waves.SavedRandomState, waves.SavedAutoStartSeconds);
        waves._PhysicsProcess(1);
        Require(waves.AutoStartSecondsRemaining == 19, "Checkpoint resumes its exact saved countdown");
        Require(waves.StartWave() && starts == 2 && !waves.AutoStartPending, "Manual early start cancels automatic countdown");
        waves._PhysicsProcess(100);
        Require(starts == 2 && waves.CurrentWave == 2, "A living owned enemy prevents starting the next countdown");
        f.Spawns.Registry.Living.Single().TakeDamage(999999);
        Require(waves.AutoStartSecondsRemaining == 5, "Per-wave custom delay overrides the level default");
        waves._PhysicsProcess(4); Require(starts == 2, "Custom countdown cannot start early");
        waves._PhysicsProcess(1);
        Require(starts == 3 && waves.CurrentWave == 3 && !waves.AutoStartPending, "Expiry accepts exactly one next wave");
        f.Spawns.Registry.Living.Single().TakeDamage(999999);
        waves._PhysicsProcess(100);
        Require(starts == 3 && waves.State == WaveManager.WaveState.AllWavesComplete && !waves.AutoStartPending &&
            !f.Hud.GetNode<Label>("TopBar/Status/AutoStart").Visible, "Final wave never arms another countdown");

        var zero = new[] { Wave(4, Batch(1, 0, 0, Enemy("basic_mk1"))) with { NextWaveDelaySeconds = 0 },
            Wave(9, Batch(1, 0, 0, Enemy("basic_mk1"))) };
        waves.Configure(zero, f.Spawns.Catalog.ToSceneMap()); f.Spawns.RestorePortalActivation(zero, 0);
        waves.StartWave(); f.Spawns.Registry.Living.Single().TakeDamage(999999);
        Require(waves.CurrentWave == 4 && waves.AutoStartSecondsRemaining == 0, "Zero delay does not recursively start inside a death callback");
        waves._PhysicsProcess(0);
        Require(waves.CurrentWave == 9, "Zero delay advances sparse authored wave numbers on the next tick");
        f.Spawns.Registry.Living.Single().TakeDamage(999999);
        waves.RestoreCheckpoint(1, waves.SavedRandomState, 10);
        waves.StopForOutcome(); waves._PhysicsProcess(100);
        Require(!waves.AutoStartPending && !waves.CanStart && !waves.StartWave(), "Outcome cancels and rejects automatic/manual starts");

        var authored = new WaveResource { Number = 1, NextWaveDelaySeconds = 7.5 };
        var batch = new WaveBatchResource { Count = 1 };
        batch.Enemies.Add(new WaveEnemyResource { EnemyId = "basic_mk1" }); authored.Batches.Add(batch);
        var source = new WaveSetResource(); source.Waves.Add(authored);
        Require(authored.Copy().NextWaveDelaySeconds == 7.5 && source.Build()[0].NextWaveDelaySeconds == 7.5,
            "Wave Editor duplication and resource conversion retain custom delay");
    }
}
