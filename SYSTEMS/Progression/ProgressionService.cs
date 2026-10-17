// Selected-profile progression authority; no gameplay node, autoload or frame polling.
using Godot;
using System;

public static partial class ProgressionService
{
    private static ProgressionStore _store;
    private static ProgressionStore Store => _store ??= new(
        ProjectSettings.GlobalizePath("user://Profiles"));
    public static event Action<string> Changed;
    public static string RecoveryMessage => Store.RecoveryMessage;

    // =========================================================
    // Resolve identity at each call so switching profiles cannot reuse another player's state.
    public static PlayerProgression GetCurrent()
    {
        string id = ProfileStore.Selected?.Id ??
            throw new InvalidOperationException("Select a profile before using player progression.");
        ProgressionMasterConfig config = GD.Load<ProgressionMasterConfig>(
            "res://SYSTEMS/Progression/DefaultProgressionConfig.tres") ??
            throw new InvalidOperationException("Missing progression config.");
        return Store.Load(id, config.FirstLevelXP, config.LevelStepXP);
    }

    // =========================================================
    // Save first; errors propagate and never announce a successful fictitious XP grant.
    // This low-level development/manual grant is not idempotent; real missions use AwardMission.
    public static PlayerProgression GrantXP(long amount)
    {
        PlayerProgression current = GetCurrent();
        PlayerProgression updated = current.AddXP(amount);
        if (amount == 0) return current;
        Store.Save(updated);
        Notify(updated.ProfileId);
        return updated;
    }

    // =========================================================
    // ResearchService validates the active room and eligibility before this internal atomic commit.
    internal static void CommitResearch(PlayerProgression updated)
    {
        if (ProfileStore.Selected?.Id != updated.ProfileId) throw new InvalidOperationException("Profile changed.");
        Store.Save(updated); Notify(updated.ProfileId);
    }

    // =========================================================
    // Presentation failures never invalidate an already committed progression change.
    private static void Notify(string id)
    {
        if (Changed != null)
            foreach (Action<string> listener in Changed.GetInvocationList())
                try { listener(id); }
                catch (Exception error) { GD.PushError(error.ToString()); }
    }
}
