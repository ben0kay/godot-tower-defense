// Separate process verifies the actual Exit Game button without terminating the main results regression runner.
using Godot;
using System;
using static MissionFixture;

public partial class ResultsExitChecks : Node
{
    // =========================================================
    // The real production button must terminate this process with exit0; a missing handler times out as failure.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            using MissionFixture f = new(this);
            if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--victory") >= 0) f.Start().TakeDamage(999999);
            else f.Health.TakeDamage(int.MaxValue);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); f.Results.SkipReveal();
            Button exit = ButtonFor(f.Results, "EXIT GAME"); Require(!exit.Disabled, "Exit available after reveal");
            exit.EmitSignal(Button.SignalName.Pressed); Require(!GetTree().Paused, "Exit releases pause");
            GD.Print("PASS: actual Mission Debrief Exit Game requested; missing quit would fail timeout");
            await ToSignal(GetTree().CreateTimer(2), SceneTreeTimer.SignalName.Timeout);
            throw new InvalidOperationException("Results Exit Game did not quit.");
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
