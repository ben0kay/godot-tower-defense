// Persistent profile index and selected local identity, adapted from the isometric 2D game.
// Saves are versioned, validated and written via a temporary file with backup recovery.
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public static class ProfileStore
{
    #region Storage
    private sealed class ProfileIndex
    {
        public int Version { get; set; } = 1;
        public List<PlayerProfile> Profiles { get; set; } = new();
    }
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static List<PlayerProfile> _profiles;
    public static PlayerProfile Selected { get; private set; }
    public static string RecoveryMessage { get; private set; } = "";
    public static IReadOnlyList<PlayerProfile> Profiles { get { Load(); return _profiles.AsReadOnly(); } }
    private static string IndexPath => ProjectSettings.GlobalizePath("user://Profiles/profiles.json");
    #endregion

    #region Loading
    // =========================================================
    // Load once; preserve a damaged index and recover the last valid backup where available.
    public static void Load()
    {
        if (_profiles != null) return;
        if (!File.Exists(IndexPath) && !File.Exists(IndexPath + ".bak"))
        { _profiles = new(); return; }
        try { _profiles = Read(IndexPath); }
        catch (Exception original)
        {
            try
            {
                _profiles = Read(IndexPath + ".bak");
                RecoveryMessage = "Recovered profiles from the previous backup.";
            }
            catch { throw new IOException("Unable to read profiles or their backup. Files were preserved. " + original.Message, original); }
        }
    }

    // =========================================================
    // Validate format, unique GUID IDs and display names before exposing profile data.
    private static List<PlayerProfile> Read(string path)
    {
        if (new FileInfo(path).Length > 1024 * 1024)
            throw new InvalidDataException("Profile index is too large.");
        ProfileIndex index = JsonSerializer.Deserialize<ProfileIndex>(File.ReadAllText(path));
        if (index == null || index.Version != 1 || index.Profiles == null || index.Profiles.Count > 1024)
            throw new InvalidDataException("Invalid profile index.");
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (PlayerProfile profile in index.Profiles)
            if (profile == null || !Guid.TryParseExact(profile.Id, "N", out _) ||
                !seen.Add(profile.Id) || !ValidName(profile.Name))
                throw new InvalidDataException("Invalid profile record.");
        return index.Profiles;
    }
    #endregion

    #region Selection and Creation
    // =========================================================
    // Profile IDs, never user-entered display names, own future campaign file paths.
    public static void Select(string id)
    {
        Load();
        Selected = _profiles.FirstOrDefault(p => p.Id == id)
            ?? throw new ArgumentException("That profile no longer exists.");
    }

    // =========================================================
    // Return to the Boot picker without erasing any saved profiles.
    public static void ClearSelection() => Selected = null;

    // =========================================================
    // Commit to disk before changing active profile, so errors cannot create phantom profiles.
    public static PlayerProfile Create(string name)
    {
        Load();
        name = (name ?? "").Trim();
        if (!ValidName(name)) throw new ArgumentException("Profile names must be 1–32 characters without control characters.");
        if (_profiles.Count >= 1024) throw new InvalidOperationException("Profile limit reached.");
        if (_profiles.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("A profile with that name already exists.");
        PlayerProfile profile = new() { Id = Guid.NewGuid().ToString("N"), Name = name, CreatedUtc = DateTime.UtcNow };
        List<PlayerProfile> updated = new(_profiles) { profile };
        Write(updated);
        _profiles = updated;
        Selected = profile;
        return profile;
    }

    // =========================================================
    // Keep display strings safe for UIs and independent of filesystem paths.
    public static bool ValidName(string name) => !string.IsNullOrWhiteSpace(name) &&
        name.Length <= 32 && !name.Any(char.IsControl);
    #endregion

    #region Persistence
    // =========================================================
    // Write and flush complete JSON, then replace the index and retain an intact backup.
    private static void Write(List<PlayerProfile> profiles)
    {
        string path = IndexPath, tmp = path + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new ProfileIndex { Profiles = profiles }, Json);
        using (FileStream stream = new(tmp, FileMode.Create, System.IO.FileAccess.Write, FileShare.None))
        { stream.Write(bytes); stream.Flush(true); }
        Read(tmp);
        if (File.Exists(path))
        {
            bool valid = false;
            try { Read(path); valid = true; } catch { }
            File.Replace(tmp, path, valid ? path + ".bak" : null);
        }
        else File.Move(tmp, path);
        RecoveryMessage = "";
    }
    #endregion
}
