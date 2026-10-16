// Explicit playable launchers: map existence alone never authorizes next-level navigation.
using Godot;
using System;
using System.Collections.Generic;

public static class PlayableLevelCatalog
{
    private readonly record struct Entry(int Number, string Scene);
    // Add one launcher here when a new playable battlefield is ready; maps alone are not entries.
    private static readonly Entry[] Entries =
    {
        new(1, MenuNavigation.Level001Scene),
        new(2, MenuNavigation.Level002Scene),
    };

    // =========================================================
    // Scene identity handles the Level001 Main.tscn exception; map identity supports future registered launchers.
    public static string Identify(Node battlefield, WorldLevel map)
    {
        foreach (Entry entry in Entries)
            if (entry.Scene == battlefield.SceneFilePath) return $"Level{entry.Number:000}";
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
        foreach (Entry entry in Entries)
            if (entry.Number == number && IsPlayable(entry.Scene)) return entry.Scene;
        return null;
    }

    // =========================================================
    // Validate launcher shape once at navigation time without instantiating worlds or running Ready callbacks.
    public static bool IsPlayable(string path)
    {
        if (string.IsNullOrEmpty(path) || !ResourceLoader.Exists(path, "PackedScene")) return false;
        SceneState state = GD.Load<PackedScene>(path).GetState();
        if (state.GetNodeType(0) != "Node2D") return false;
        HashSet<string> children = new();
        for (int i = 1; i < state.GetNodeCount(); i++)
            if (state.GetNodePath(i).GetNameCount() == 2) children.Add(state.GetNodeName(i).ToString());
        return children.Contains("World") && children.Contains("Waves") && children.Contains("CitadelSession")
            && children.Contains("LevelSession") && children.Contains("LevelResults");
    }
}
