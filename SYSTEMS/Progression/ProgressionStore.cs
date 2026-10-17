// Validated per-GUID progression persistence with flushed atomic writes and backup recovery.
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class ProgressionStore
{
    private readonly string _root;
    private static readonly JsonSerializerOptions Json = new()
    { WriteIndented = true, IgnoreReadOnlyProperties = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    public string RecoveryMessage { get; private set; } = "";

    // =========================================================
    // Accept a directory explicitly so isolated tests never touch real player files.
    public ProgressionStore(string root) => _root = root;

    // =========================================================
    // Never use display names or arbitrary caller strings as filesystem paths.
    private string PathFor(string id)
    {
        if (!Guid.TryParseExact(id, "N", out _)) throw new ArgumentException("Invalid profile ID.");
        return Path.Combine(_root, id, "progression.json");
    }

    // =========================================================
    // Missing data starts fresh; damaged existing data must recover or report an error.
    public PlayerProgression Load(string id, long first = 500, long step = 250)
    {
        string path = PathFor(id);
        RecoveryMessage = "";
        if (!File.Exists(path) && !File.Exists(path + ".bak"))
        {
            PlayerProgression fresh = new() { ProfileId = id, FirstLevelXP = first, LevelStepXP = step };
            Validate(fresh, id);
            return fresh;
        }
        try { return Read(path, id); }
        catch (Exception original)
        {
            try
            {
                PlayerProgression recovered = Read(path + ".bak", id);
                RecoveryMessage = "Progression recovered from the previous backup.";
                return recovered;
            }
            catch { throw new IOException("Cannot read progression or backup. Files preserved. " + original.Message, original); }
        }
    }

    // =========================================================
    // Validate source fields and derived arithmetic before displaying or replacing data.
    private static void Validate(PlayerProgression value, string id)
    {
        if (value == null || value.Version != 1 || value.ProfileId != id ||
            value.LifetimePlayerXP < 0 || value.UnspentResearchPoints < 0 ||
            value.FirstLevelXP <= 0 || value.LevelStepXP < 0)
            throw new InvalidDataException("Invalid progression record.");
        if (value.UnspentResearchPoints > value.Level - 1L)
            throw new InvalidDataException("Research Points exceed earned promotions.");
        _ = value.RequiredXP;
    }

    // =========================================================
    // Bound file size and reject missing required source properties rather than resetting XP.
    private static PlayerProgression Read(string path, string id)
    {
        if (new FileInfo(path).Length > 65536) throw new InvalidDataException("Progression file too large.");
        string text = File.ReadAllText(path);
        using JsonDocument document = JsonDocument.Parse(text);
        foreach (string name in new[] { "Version", "ProfileId", "LifetimePlayerXP",
            "UnspentResearchPoints", "FirstLevelXP", "LevelStepXP" })
            if (!document.RootElement.TryGetProperty(name, out _))
                throw new InvalidDataException("Incomplete progression record.");
        PlayerProgression value = JsonSerializer.Deserialize<PlayerProgression>(text, Json);
        Validate(value, id);
        return value;
    }

    // =========================================================
    // Publish only a validated complete record; preserve an intact previous backup.
    public void Save(PlayerProgression value)
    {
        string path = PathFor(value.ProfileId), temporary = path + ".tmp";
        Validate(value, value.ProfileId);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, Json);
        using (FileStream stream = new(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        { stream.Write(bytes); stream.Flush(true); }
        Read(temporary, value.ProfileId);
        if (File.Exists(path))
        {
            bool valid = false;
            try { Read(path, value.ProfileId); valid = true; } catch { }
            File.Replace(temporary, path, valid ? path + ".bak" : null);
        }
        else File.Move(temporary, path);
    }
}
