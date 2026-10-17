// Standalone R1 production-world outcome, reveal and navigation checks; no profile/save writes.
using Godot;
using System;

public partial class ResultsChecks : Node
{
    // =========================================================
    // Always-processing runner remains alive while each production battlefield freezes at mission end.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await MissionOutcomeChecks.Run(this);
            await SubsystemBattlefield.DrainFreedFixtures(this);
            await ResultsNavigationChecks.Run(this);
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: Mission Debrief R1 outcomes, UI and playable navigation"); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
}
