// Explicit, dormant campaign storage: one current slot per profile, strict ownership and recoverable atomic writes.
// This foundation does not serialize a battlefield or enable Continue/Save/Load. No Godot dependency or polling.
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class CampaignStore
{
    #region Format and paths
    public const int MaximumFileBytes = 1024 * 1024;
    private readonly string _profilesRoot;
    private readonly object _gate = new();
    private static readonly JsonSerializerOptions Json = new()
    { WriteIndented = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    public string RecoveryMessage { get; private set; } = "";

    // =========================================================
    // Production passes GlobalizePath("user://Profiles"); tests pass an isolated temporary directory.
    public CampaignStore(string profilesRoot)
    {
        if (string.IsNullOrWhiteSpace(profilesRoot)) throw new ArgumentException("A profiles directory is required.");
        _profilesRoot = Path.GetFullPath(profilesRoot);
    }

    // =========================================================
    // Canonical lowercase GUIDs prevent traversal and case-dependent duplicate profile directories.
    internal static void ValidateId(string id)
    {
        if (!Guid.TryParseExact(id, "N", out Guid parsed) || parsed == Guid.Empty || parsed.ToString("N") != id)
            throw new ArgumentException("Invalid profile or campaign ID.");
    }

    // =========================================================
    // A fixed filename gives one unambiguous current slot; campaign identity remains inside the document.
    private string PathFor(string profileId)
    {
        ValidateId(profileId);
        return Path.Combine(_profilesRoot, profileId, "Campaigns", "current.json");
    }
    #endregion

    #region Reading and validation
    // =========================================================
    // Absence is distinct from corruption. Recovery is read-only and never erases either original file.
    public CampaignData Load(string profileId)
    {
        lock (_gate)
        {
            string path = PathFor(profileId);
            RecoveryMessage = "";
            if (!File.Exists(path) && !File.Exists(path + ".bak")) return null;
            try { return Read(path, profileId); }
            catch (Exception original) when (IsReadFailure(original))
            {
                try
                {
                    CampaignData recovered = Read(path + ".bak", profileId);
                    RecoveryMessage = "Campaign recovered from the previous backup. Original files were preserved.";
                    return recovered;
                }
                catch (Exception backup) when (IsReadFailure(backup))
                { throw new IOException("Cannot read campaign or backup. Files were preserved.", new AggregateException(original, backup)); }
            }
        }
    }

    // =========================================================
    // Catch format/storage failures only; unrelated programming failures must remain visible.
    private static bool IsReadFailure(Exception error) => error is IOException or UnauthorizedAccessException or JsonException or ArgumentException;

    // =========================================================
    // Validate caller ownership and metadata, never trust an embedded ID to choose a directory.
    private static void Validate(CampaignData value, string profileId)
    {
        if (value == null) throw new InvalidDataException("Missing campaign record.");
        ValidateId(value.ProfileId); ValidateId(value.CampaignId);
        if (value.Version != 1 || value.ProfileId != profileId || value.CreatedUtc.Kind != DateTimeKind.Utc ||
            value.SavedUtc.Kind != DateTimeKind.Utc || value.CreatedUtc == default || value.SavedUtc < value.CreatedUtc)
            throw new InvalidDataException("Unsupported, incomplete or incorrectly owned campaign record.");
    }

    // =========================================================
    // Read at most the byte limit, including files that grow while open; required JSON fields cannot default silently.
    private static CampaignData Read(string path, string profileId)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > MaximumFileBytes) throw new InvalidDataException("Campaign file is too large.");
        byte[] bytes = new byte[(int)stream.Length];
        stream.ReadExactly(bytes);
        if (stream.ReadByte() != -1) throw new InvalidDataException("Campaign file changed while reading.");
        using JsonDocument document = JsonDocument.Parse(bytes);
        // Duplicate properties are ambiguous even when the JSON serializer otherwise accepts them.
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Expected campaign object.");
        var names = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty property in document.RootElement.EnumerateObject())
            if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate campaign property.");
        CampaignData value = JsonSerializer.Deserialize<CampaignData>(bytes, Json);
        Validate(value, profileId);
        return value;
    }
    #endregion

    #region Writing
    // =========================================================
    // Validate/read back and flush before atomic publication. Damaged files require explicit future recovery UX.
    public void Save(string profileId, CampaignData value)
    {
        lock (_gate)
        {
            string path = PathFor(profileId);
            Validate(value, profileId);
            // Do not turn an accidental New/Save into silent campaign replacement or corruption repair.
            foreach (string existing in new[] { path, path + ".bak" })
            {
                if (!File.Exists(existing)) continue;
                CampaignData previous = Read(existing, profileId);
                if (previous.CampaignId != value.CampaignId || previous.CreatedUtc != value.CreatedUtc || value.SavedUtc < previous.SavedUtc)
                    throw new InvalidOperationException("This slot belongs to a different campaign or newer save. Files were preserved.");
            }
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, Json);
            if (bytes.Length > MaximumFileBytes) throw new InvalidDataException("Campaign file is too large.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (FileStream stream = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(bytes); stream.Flush(true); }
                Read(temporary, profileId);
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                RecoveryMessage = "";
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
    #endregion
}
