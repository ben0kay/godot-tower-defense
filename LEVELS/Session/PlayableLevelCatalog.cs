// Explicit playable launchers: map existence alone never authorizes next-level navigation.
using Godot;
using System;

public static class PlayableLevelCatalog
{
    public static MissionDefinition[] Entries => MissionCatalog.Default.Entries();

    // =========================================================
    // Scene identity handles the Level001 Main.tscn exception; map identity supports future registered launchers.
    public static string Identify(Node battlefield, WorldLevel map)
    {
        if (battlefield is BattlefieldLauncher launcher && launcher.Mission != null) return launcher.Mission.LevelId;
        foreach (MissionDefinition entry in Entries)
            if (entry.LaunchPath == battlefield.SceneFilePath) return entry.LevelId;
        string name = map.SceneFilePath.GetFile().GetBaseName();
        return TryNumber(name, out _) ? name : "UNKNOWN SECTOR";
    }

    // =========================================================
    // Only exactly three ASCII digits are accepted, including carry boundaries such as099->100.
    public static string FollowingName(string level)
    {
        if (!TryNumber(level, out int number) || number == 999) return null;
        return $"Level{number + 1:000}";
    }
    private static bool TryNumber(string level, out int number)
    {
        number = 0;
        if (level == null || level.Length != 8 || !level.StartsWith("Level", StringComparison.Ordinal)) return false;
        for (int i = 5; i < 8; i++) { if (level[i] < '0' || level[i] > '9') return false; number = number * 10 + level[i] - '0'; }
        return number > 0;
    }

    // =========================================================
    // Resolve only the immediately following registered, existing gameplay scene; never skip a missing level.
    public static string NextScene(string level)
    {
        string next = FollowingName(level);
        if (!TryNumber(next, out int number)) return null;
        foreach (MissionDefinition entry in Entries)
            if (entry.Number == number && IsPlayable(entry.LaunchPath)) return entry.LaunchPath;
        return null;
    }

    // =========================================================
    // Validate inherited launcher shape off-tree without running gameplay Ready callbacks.
    public static bool IsPlayable(string path)
    {
        if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path)) return false;
        if (path.EndsWith(".tres", StringComparison.Ordinal))
        {
            try { var mission = GD.Load<MissionDefinition>(path); if (mission == null || !mission.Enabled) return false; mission.Validate(); return true; }
            catch { return false; }
        }
        if (!ResourceLoader.Exists(path, "PackedScene")) return false;
        Node root = GD.Load<PackedScene>(path).Instantiate();
        try
        {
            return root is Node2D && root.GetNodeOrNull<Node>("World") != null && root.GetNodeOrNull<Node>("Waves") != null
                && root.GetNodeOrNull<Node>("CitadelSession") != null && root.GetNodeOrNull<Node>("LevelSession") != null
                && root.GetNodeOrNull<Node>("LevelResults") != null;
        }
        finally { root.Free(); }
    }
}
