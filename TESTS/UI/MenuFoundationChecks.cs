// Non-destructive menu foundation checks: reusable buttons, scene resources and profile validation.
using Godot;
using System;

public static class MenuFoundationChecks
{
    #region Integration
    // =========================================================
    // Run from the existing Main-scene test; never write into the user's profile directory.
    public static void Run(Node main)
    {
        Require(MenuNavigation.Level001Scene == "res://Main.tscn", "Campaign keeps the original Main.tscn");
        Require(MenuNavigation.Level020Scene == "res://MainLevel020.tscn" && ResourceLoader.Exists(MenuNavigation.Level020Scene),
            "Campaign exposes the playable Level020 launch scene");
        Level020Checks.Run();
        foreach (string scenePath in new[] {
            MenuNavigation.BootScene, MenuNavigation.MainScene, MenuNavigation.CampaignScene, MenuNavigation.LevelsScene })
        {
            PackedScene packed = GD.Load<PackedScene>(scenePath);
            Require(packed != null, "Menu scene loads: " + scenePath);
            Control root = packed.Instantiate<Control>();
            Require(root != null, "Menu scene has a Control root: " + scenePath);
            root.Free();
        }

        Require(ProfileStore.ValidName("Commander") && !ProfileStore.ValidName("") &&
            !ProfileStore.ValidName(new string('x', 33)) && !ProfileStore.ValidName("bad\nname"),
            "Profile names reject blanks, control characters and oversized input");

        GameplayHUD hud = main.GetNode<GameplayHUD>("UI/GameplayHUD");
        Button wave = hud.GetNode<Button>("TopBar/Status/StartWave");
        Button tower = hud.GetBuildButton("battle");
        Button close = hud.GetNode<Button>("BottomArea/Inspector/Insets/Body/Heading/CloseButton");
        Require(wave.GetThemeFontSize("font_size") >= 10 &&
                tower.GetThemeFontSize("font_size") == 13 &&
                close.GetThemeFontSize("font_size") == 12,
                "Existing wave, tower and close buttons use the shared factory's variants");
        Require(wave.GetThemeStylebox("normal") is StyleBoxFlat &&
                tower.GetThemeStylebox("hover") is StyleBoxFlat &&
                close.GetThemeStylebox("focus") is StyleBoxFlat,
                "Shared button visual states exist on authored HUD buttons");

        int clicks = 0;
        Button generated = UIButtonFactory.Create("Test", () => clicks++);
        generated.EmitSignal(Button.SignalName.Pressed);
        Require(clicks == 1, "Factory-created button invokes its owner callback exactly once");
        generated.Free();
        GD.Print("PASS: menu scene resources, button factory and profile name validation");
    }
    #endregion

    // =========================================================
    // Assert without deleting or changing the user's real profile files.
    private static void Require(bool ok, string message)
    { if (!ok) throw new InvalidOperationException(message); }
}
