// Older subsystem checks explicitly finish paid initial builds before testing their unrelated gameplay contracts.
using Godot;

internal static class ConstructionTestClock
{
    // =========================================================
    // Uses the real active-job clock; construction-specific checks never call this convenience helper.
    internal static void FinishInitial(Node battlefield) => battlefield.GetNode<ConstructionWorkManager>("World/ConstructionWork").Advance(3600);
}
