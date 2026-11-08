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
    public const string Level002Scene = "res://MainLevel002.tscn";
    public const string Level020Scene = "res://MainLevel020.tscn";
    public const string LevelLoadingScene = "res://UI/Loading/LevelLoadingScreen.tscn";

    // =========================================================
    // Unpause before leaving a menu, restoring the previous pause state on a failed transition.
    public static void Open(Node context, string scene)
    {
        SceneTree tree = context.GetTree();
        bool paused = tree.Paused;
        tree.Paused = false;
        Error result;
        try
        {
            if (scene.EndsWith(".tres", StringComparison.Ordinal))
            {
                MissionDefinition mission = GD.Load<MissionDefinition>(scene);
                if (mission == null || !mission.Enabled) throw new InvalidOperationException("Mission is unavailable.");
                mission.Validate();
                BattlefieldLauncher instance = GD.Load<PackedScene>("res://LEVELS/Runtime/Battlefield.tscn").Instantiate<BattlefieldLauncher>();
                instance.Mission = mission;
                result = tree.ChangeSceneToNode(instance);
                if (result != Error.Ok) instance.Free();
            }
            else result = tree.ChangeSceneToFile(scene);
        }
        catch { tree.Paused = paused; throw; }
        if (result == Error.Ok) return;
        tree.Paused = paused;
        throw new InvalidOperationException($"Could not open {scene}: {result}");
    }

    // =========================================================
    // Campaign PLAY buttons enter a brief threaded mission-loading screen.
    // Open() retains its synchronous semantics for direct F6, save restore and fixtures.
    public static void OpenLevel(Node context, string missionPath)
    {
        if (!missionPath.EndsWith(".tres", StringComparison.Ordinal))
        {
            Open(context, missionPath);
            return;
        }

        SceneTree tree = context.GetTree();
        bool paused = tree.Paused;
        LevelLoadingScreen screen = null;
        try
        {
            PackedScene packed = GD.Load<PackedScene>(LevelLoadingScene);
            if (packed == null) throw new InvalidOperationException("Mission loading screen is missing.");
            screen = packed.Instantiate<LevelLoadingScreen>();
            screen.MissionPath = missionPath;
            string previous = tree.CurrentScene?.SceneFilePath;
            screen.ReturnScenePath = string.IsNullOrEmpty(previous) ? LevelsScene : previous;
            tree.Paused = false;
            Error result = tree.ChangeSceneToNode(screen);
            if (result != Error.Ok)
                throw new InvalidOperationException($"Could not show mission loader: {result}");
        }
        catch
        {
            if (GodotObject.IsInstanceValid(screen) && !screen.IsInsideTree()) screen.Free();
            tree.Paused = paused;
            throw;
        }
    }

    // =========================================================
    // Menu Escape uses the existing scoped GameInput action; popup dialogs retain Escape.
    public static void BindBack(Node menu, Action back)
    {
        if (back == null) throw new ArgumentNullException(nameof(back));
        menu.GetNodeOrNull<GameInput>("/root/GameInput")?.Bind(menu, GameInput.BackAction, () =>
        {
            foreach (Node child in menu.GetChildren())
                if (child is Window dialog && dialog.Visible) return false;
            UIButtonFactory.PlayBack(menu);
            back();
            return true;
        }, priority: 20);
    }

    // =========================================================
    // Remove only this scene's binding; no stale handlers after transition.
    public static void UnbindBack(Node menu) =>
        menu.GetNodeOrNull<GameInput>("/root/GameInput")?.Unbind(menu);

    // =========================================================
    // Avoid replacing the scene during a screen's own Ready notifications.
    public static void ReturnToBoot(Node context) =>
        Callable.From(() => Open(context, BootScene)).CallDeferred();
}
