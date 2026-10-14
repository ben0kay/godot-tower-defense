// Catalog-driven HUD coverage with many authored data variants and unavailable entries.
using Godot;
using System;
using System.Threading.Tasks;

public static class TowerBuildBarChecks
{
    // =========================================================
    // Generate a large tray without instantiating towers, exercise action IDs and keyboard focus.
    public static async Task Run(Node main)
    {
        GameplayHUD hud = main.GetNode<GameplayHUD>("UI/GameplayHUD");
        TowerPlacementController placement = main.GetNode<TowerPlacementController>("TowerPlacement");
        TowerDefinition battle = placement.Catalog.Find("battle");
        Require(hud.GetBuildButton("battle") != null && hud.GetBuildButton("laser") == null,
            "Default build bar contains only playable Battle Tower");
        TowerCatalog many = new();
        for (int i = 0; i < 40; i++)
        {
            BattleTowerConfig config = ((BattleTowerConfig)battle.DefaultConfig).CreateInstanceCopy();
            config.DisplayName = "Test variant " + i;
            config.ConstructionCosts[0].Amount = 201 + i;
            many.Definitions.Add(new TowerDefinition { TowerId = "variant-" + i, Scene = battle.Scene,
                DefaultConfig = config, Icon = battle.Icon, Category = TowerDefinition.TowerCategory.Offensive,
                Available = i != 5 });
        }
        many.Rebuild();
        int active = main.GetNode("World/Towers").GetChildCount();
        placement.Catalog = many;
        hud.BindCatalog(many);
        HBoxContainer entries = hud.GetNode<HBoxContainer>("BottomArea/TraySlot/TowerBar/Insets/Tray/Scroll/Buttons");
        Require(entries.GetChildCount() == 39 && hud.GetBuildButton("variant-5") == null &&
            main.GetNode("World/Towers").GetChildCount() == active, "Arbitrary registered types render without inactive placeholders or live tower instances");
        Button last = hud.GetBuildButton("variant-39");
        Require(last.Text.Contains("240 Credits") && last.Icon == battle.Icon && last.FocusMode == Control.FocusModeEnum.All,
            "Generated button reads canonical recipe/art and retains keyboard focus");
        string requested = null;
        void Chosen(string id) => requested = id;
        hud.TowerChoiceRequested += Chosen;
        last.EmitSignal(Button.SignalName.Pressed);
        hud.TowerChoiceRequested -= Chosen;
        Require(requested == "variant-39" && hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/TowerName").Text == "Test variant 39",
            "Generated action delivers the type ID to generic placement and its own preview");
        hud.SetTowerBarOpen(true, true);
        await hud.ToSignal(hud.GetTree(), SceneTree.SignalName.ProcessFrame);
        await hud.ToSignal(hud.GetTree(), SceneTree.SignalName.ProcessFrame);
        ScrollContainer scroll = entries.GetParent<ScrollContainer>();
        Require(scroll.GetHScrollBar().MaxValue > scroll.Size.X && scroll.GetGlobalRect().End.X <= hud.GetGlobalRect().End.X,
            "Large catalog scrolls inside viewport without widening the HUD");
        scroll.EnsureControlVisible(last);
        Require(scroll.ScrollHorizontal > 0, "Last registered entry is reachable via scrolling");
        hud.SetTowerBarOpen(false, true);
        Require(hud.IsInspectionVisible, "Closing large tray preserves independent Inspector");
        placement.Call("CancelPlacement");
        TowerCatalog empty = new(); empty.Rebuild(); hud.BindCatalog(empty);
        Require(hud.GetBuildButton("variant-39") == null && entries.GetChildCount() == 1,
            "Rebinding clears stale controls and empty catalog has safe message");
        placement.Catalog = GD.Load<TowerCatalog>("res://TOWERS/DefaultTowerCatalog.tres");
        hud.BindCatalog(placement.Catalog);
        hud.GetBuildButton("battle").EmitSignal(Button.SignalName.Pressed);
        placement.Call("CancelPlacement"); hud.ClearInspection();
        GD.Print("PASS: dynamic 40-type tray, availability, metadata/prices, actions, focus, scrolling and independent panels");
    }
    // =========================================================
    // Fail on dynamic build UI integration regression.
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
