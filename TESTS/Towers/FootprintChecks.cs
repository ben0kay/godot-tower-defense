// Rectangular placement and navigation regressions through real paid building scenes.
using Godot;
using System;

public partial class FootprintChecks : Node
{
    // =========================================================
    // Dedicated worlds keep authored maps/configs and ordinary mission outcomes untouched.
    public override async void _Ready()
    {
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            FootprintPlacementChecks.Run(this); FootprintNavigationChecks.Run(this);
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: rectangular footprints F1-F5, atomic occupancy/payment/rollback, any-cell selection and navigation distinction");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
}
