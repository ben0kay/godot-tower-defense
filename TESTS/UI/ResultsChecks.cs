// Standalone R1 production-world outcome, reveal and navigation checks; no profile/save writes.
using Godot;
using System;
using System.IO;
using System.Reflection;

public partial class ResultsChecks : Node
{
    // =========================================================
    // Always-processing runner remains alive while each production battlefield freezes at mission end.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        string directory = Path.Combine(Path.GetTempPath(), "td-results-" + Guid.NewGuid().ToString("N"));
        FieldInfo profiles = typeof(ProfileStore).GetField("_profiles", BindingFlags.Static | BindingFlags.NonPublic);
        FieldInfo store = typeof(ProgressionService).GetField("_store", BindingFlags.Static | BindingFlags.NonPublic);
        object oldProfiles = profiles.GetValue(null), oldStore = store.GetValue(null); PlayerProfile selected = ProfileStore.Selected;
        try
        {
            ProfileStore.ClearSelection(); store.SetValue(null, new ProgressionStore(directory));
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await MissionOutcomeChecks.Run(this);
            await SubsystemBattlefield.DrainFreedFixtures(this);
            await ResultsNavigationChecks.Run(this);
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: Mission Debrief R1 outcomes, UI and playable navigation"); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
        finally
        {
            ProfileStore.ClearSelection(); profiles.SetValue(null, oldProfiles); store.SetValue(null, oldStore);
            if (selected != null) ProfileStore.Select(selected.Id);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
