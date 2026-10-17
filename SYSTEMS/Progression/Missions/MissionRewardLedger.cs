// Once-only mission transactions in the existing profile file: monotonic tickets avoid an ever-growing GUID ledger.
using System;

public static class MissionRewardLedger
{
    // =========================================================
    // Reserve before gameplay; abandoned missions leave harmless gaps and cannot consume a later run's reward.
    public static MissionRewardTicket Begin(ProgressionStore store, PlayerProgression current, string levelId, MissionXPPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(levelId) || levelId.Length > 64 || policy.VictoryXP < 0 || policy.DefeatXP < 0)
            throw new ArgumentException("Invalid mission reward identity/settings.");
        current = store.Load(current.ProfileId, current.FirstLevelXP, current.LevelStepXP);
        long number = checked(current.LastIssuedMissionNumber + 1);
        store.Save(current with { LastIssuedMissionNumber = number });
        return new(current.ProfileId, number, levelId, policy);
    }

    // =========================================================
    // XP, promotions, RP, receipt and high-water mark commit together; never save an award marker separately.
    public static MissionRewardResult Award(ProgressionStore store, MissionRewardTicket ticket, LevelResultSnapshot snapshot)
    {
        if (ticket == null || snapshot == null || ticket.LevelId != snapshot.LevelName)
            throw new InvalidOperationException("Mission reward does not match this run.");
        PlayerProgression current = store.Load(ticket.ProfileId);
        if (ticket.Number <= 0 || ticket.Number > current.LastIssuedMissionNumber)
            throw new InvalidOperationException("Mission reward ticket was not reserved.");
        if (ticket.Number <= current.LastAwardedMissionNumber)
        {
            if (ticket.Number != current.LastMissionAward?.MissionNumber)
                throw new InvalidOperationException("This older mission has already been retired; no XP awarded.");
            return Result(current, current.LastMissionAward, true);
        }
        var receipt = new MissionAwardReceipt(ticket.Number, ticket.LevelId, current.LifetimePlayerXP,
            current.UnspentResearchPoints, ticket.Policy.Calculate(snapshot));
        PlayerProgression updated = current.AddXP(receipt.Breakdown.TotalXP) with
        { LastAwardedMissionNumber = ticket.Number, LastMissionAward = receipt };
        store.Save(updated);
        return Result(updated, receipt, false);
    }

    // =========================================================
    // Duplicate presentation uses the saved receipt, even after research spending or later manual XP grants.
    private static MissionRewardResult Result(PlayerProgression current, MissionAwardReceipt receipt, bool duplicate)
    {
        PlayerProgression before = current with { LifetimePlayerXP = receipt.XPBefore, UnspentResearchPoints = receipt.ResearchPointsBefore };
        return new(receipt.Breakdown, before, before.AddXP(receipt.Breakdown.TotalXP), duplicate);
    }
}
