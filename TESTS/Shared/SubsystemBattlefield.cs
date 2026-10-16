// Subsystem fixtures intentionally mutate waves/HP; remove mission orchestration/presentation before entering the tree.
using Godot;
using System;
using System.Threading.Tasks;

internal static class SubsystemBattlefield
{
    // =========================================================
    // Preserve production services/assertions while reserving real end-of-level behavior for ResultsChecks.
    internal static void DetachSession(Node battlefield)
    { battlefield.GetNodeOrNull("LevelSession")?.Free(); battlefield.GetNodeOrNull("LevelResults")?.Free(); }
    // =========================================================
    // Settle freed world wrappers before the headless native engine tears down its resource database.
    internal static async Task DrainFreedFixtures(Node runner)
    {
        GC.Collect(); GC.WaitForPendingFinalizers();
        await runner.ToSignal(runner.GetTree(), SceneTree.SignalName.ProcessFrame);
    }
}
