// Versioned campaign identity and optional typed between-wave checkpoint.
using System;
using System.Text.Json.Serialization;

public sealed record CampaignData
{
    [JsonRequired] public int Version { get; init; } = 2;
    [JsonRequired] public string ProfileId { get; init; } = "";
    [JsonRequired] public string CampaignId { get; init; } = "";
    [JsonRequired] public DateTime CreatedUtc { get; init; }
    [JsonRequired] public DateTime SavedUtc { get; init; }

    public LevelSaveData Checkpoint { get; init; }

    // =========================================================
    // Prepare metadata without writing files, selecting a campaign or reserving mission rewards.
    public static CampaignData Create(string profileId)
    {
        CampaignStore.ValidateId(profileId);
        DateTime now = DateTime.UtcNow;
        return new() { ProfileId = profileId, CampaignId = Guid.NewGuid().ToString("N"), CreatedUtc = now, SavedUtc = now };
    }
}
