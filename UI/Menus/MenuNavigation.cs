// Named destinations and safe scene transitions, shared by Boot/Main/Campaign menus.
using Godot;
using System;

public static class MenuNavigation
{
    public const string BootScene = "res://UI/Menus/Boot/BootScreen.tscn";
    public const string MainScene = "res://UI/Menus/Main/MainMenu.tscn";
    public const string CampaignScene = "res://UI/Menus/Campaign/CampaignMenu.tscn";
    public const string LevelsScene = "res://UI/Menus/Campaign/Levels/LevelSelectionMenu.tscn";
    public const string ResearchScene = "res://UI/Menus/Campaign/Research/ResearchMenu.tscn";
    public const string Level001Scene = "res://Main.tscn";
    public const string Level020Scene = "res://MainLevel020.tscn";

    // =========================================================
    // Unpause before leaving a menu, restoring the previous pause state on a failed transition.
    public static void Open(Node context, string scene)
    {
        SceneTree tree = context.GetTree();
        bool paused = tree.Paused;
        tree.Paused = false;
        Error result = tree.ChangeSceneToFile(scene);
        if (result == Error.Ok) return;
        tree.Paused = paused;
        throw new InvalidOperationException($"Could not open {scene}: {result}");
    }

    // =========================================================
    // Avoid replacing the scene during a screen's own Ready notifications.
    public static void ReturnToBoot(Node context) =>
        Callable.From(() => Open(context, BootScene)).CallDeferred();
}
