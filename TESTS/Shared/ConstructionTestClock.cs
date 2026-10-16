// Older subsystem checks explicitly finish paid initial builds before testing their unrelated gameplay contracts.
using Godot;

internal static class ConstructionTestClock
{
    // =========================================================
    // Uses the real active-job clock; construction-specific checks never call this convenience helper.
    internal static void FinishInitial(Node battlefield) => FinishWork(battlefield);
    internal static void FinishWork(Node battlefield) => battlefield.GetNode<ConstructionWorkManager>("World/ConstructionWork").Advance(3600);

    // =========================================================
    // Unrelated completed-weapon checks wait through the same paid clock; timing assertions live in TimedUpgradeChecks.
    internal static bool PurchaseAndFinish(TowerBase tower, EconomyManager wallet, TowerMasterConfig rules, int level, out string failure)
    {
        if (!TowerUpgradeController.TryUpgrade(tower, wallet, rules, level, out failure)) return false;
        tower.GetNode<ConstructionWorkManager>("../../ConstructionWork").Advance(tower.RemainingWorkSeconds);
        return true;
    }
}
