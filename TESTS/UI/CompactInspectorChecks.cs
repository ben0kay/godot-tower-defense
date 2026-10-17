// Focused Inspector layout checks without unrelated combat/journey fixture lifetimes.
using Godot;
using System;

public partial class CompactInspectorChecks : Node
{
    // =========================================================
    // Reuse actual tower selection, paid work and all viewport geometry assertions.
    public override async void _Ready()
    {
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                SubsystemBattlefield.DetachSession(f.Main);
                await InspectorTargetingChecks.Layout(f);
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: 560px Inspector, content-sized comparison columns, vector divider grid, all tower families/max/busy states at three viewport sizes");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }
}
