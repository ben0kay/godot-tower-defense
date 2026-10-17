// Selected-profile mission boundary; permanent awards remain outside Results controls and enemy/tower XP.
using System;

public static partial class ProgressionService
{
    // =========================================================
    // The caller captures the deployment owner once; a profile change must never redirect a running mission.
    public static MissionRewardTicket BeginMission(string profileId, string levelId, MissionXPPolicy policy)
    {
        RequireMissionOwner(profileId);
        return MissionRewardLedger.Begin(Store, GetCurrent(), levelId, policy);
    }

    // =========================================================
    // Notify menus only after disk commit; duplicate requests never repeat grants or notifications.
    public static MissionRewardResult AwardMission(MissionRewardTicket ticket, LevelResultSnapshot snapshot)
    {
        if (ticket == null) throw new ArgumentNullException(nameof(ticket));
        RequireMissionOwner(ticket.ProfileId);
        MissionRewardResult result = MissionRewardLedger.Award(Store, ticket, snapshot);
        if (!result.AlreadySaved) Notify(ticket.ProfileId);
        return result;
    }

    // =========================================================
    // Profileless development runs do not mint saved progression under a placeholder identity.
    private static void RequireMissionOwner(string profileId)
    {
        if (string.IsNullOrEmpty(profileId) || ProfileStore.Selected?.Id != profileId)
            throw new InvalidOperationException("The deployment profile is not selected. Restore that profile before retrying XP.");
    }
}
