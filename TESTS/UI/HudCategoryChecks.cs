// Real build categories, placement cancellation, retained inspection and bounded transparent HUD layout.
using Godot;
using System;
using System.Linq;
using static AmmoLoopFixture;

public partial class HudCategoryChecks : Node
{
    // =========================================================
    // Drive production controls and await container layout; no maps, balances or shared configs are saved.
    public override async void _Ready()
    {
        try
        {
            using (AmmoLoopFixture f = new(this))
            {
                GameplayHUD hud = f.Hud; var catalog = f.Placement.Catalog;
                hud.SetTowerBarOpen(true, true);
                Require(hud.BuildCategory == null && hud.GetBuildCategoryButton("All").ButtonPressed
                    && hud.GetBuildCategoryButton("Offensive") != null && hud.GetBuildCategoryButton("Economy") != null
                    && hud.GetBuildCategoryButton("Support") == null && hud.GetBuildCategoryButton("Utility") == null, "All default and only populated categories appear");
                hud.GetBuildButton("battle").EmitSignal(Button.SignalName.Pressed);
                Require(f.Placement.IsPlacing, "Build choice begins placement");
                hud.GetBuildCategoryButton("Economy").EmitSignal(Button.SignalName.Pressed);
                Require(!f.Placement.IsPlacing && hud.IsTowerBarOpen && !hud.GetBuildButton("battle").Visible
                    && hud.GetBuildButton("ammo_factory").Visible && hud.GetBuildButton("ammo_depot").Visible,
                    "Category filters existing buttons and cancels pending ghost without closing tray");
                hud.SetTowerBarOpen(false, true); hud.SetTowerBarOpen(true, true);
                Require(hud.BuildCategory == TowerDefinition.TowerCategory.Economy && hud.GetBuildCategoryButton("Economy").ButtonPressed, "Category remembered across Ctrl closing/reopening");
                BattleTower tower = f.Purchase<BattleTower>("battle");
                hud.SelectBuildCategory(TowerDefinition.TowerCategory.Offensive);
                Require(hud.IsInspectionVisible && hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/TowerName").Text == tower.AuthoredConfig.DisplayName,
                    "Filtering does not clear placed-tower Inspector");
                hud.BindCatalog(catalog); Require(hud.BuildCategory == TowerDefinition.TowerCategory.Offensive, "Existing populated category survives rebind");
                TowerCatalog economy = new() { Definitions = new() { catalog.Find("ammo_factory"), catalog.Find("ammo_depot") } };
                economy.Rebuild(); hud.BindCatalog(economy);
                Require(hud.BuildCategory == null && hud.GetBuildCategoryButton("Offensive") == null && hud.GetBuildCategoryButton("Economy").Text.Contains("2"), "Missing selected category safely falls back to All");
                TowerDefinition source = catalog.Find("ammo_factory");
                TowerCatalog support = new() { Definitions = new() { new TowerDefinition { TowerId = "future-support", Scene = source.Scene,
                    DefaultConfig = source.DefaultConfig, Icon = source.Icon, Category = TowerDefinition.TowerCategory.Support } } };
                support.Rebuild(); hud.BindCatalog(support);
                Require(hud.GetBuildCategoryButton("Support") != null && hud.GetBuildButton("future-support") != null, "New types/categories appear automatically from metadata");
                TowerCatalog empty = new(); empty.Rebuild(); hud.BindCatalog(empty);
                Require(hud.GetBuildCategoryButton("All") != null && hud.GetBuildButton("battle") == null, "Empty catalog remains safe"); hud.BindCatalog(catalog);
                hud.ShowTowerDetails("LONG INSPECTION", "Test", string.Join("\n", Enumerable.Range(1, 80).Select(i => "Stat " + i)), "Readable notes");
                hud.SetUpgradeDetails("Real preview remains reachable", "UPGRADE", true);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                PanelContainer inspector = hud.GetNode<PanelContainer>("BottomArea/Inspector"); ScrollContainer scroll = inspector.GetNode<ScrollContainer>("Insets");
                Control tray = hud.GetNode<Control>("BottomArea/TraySlot/TowerBar");
                Require(Mathf.IsEqualApprox(tray.Size.Y, hud.Config.BottomBarHeight) && inspector.GetGlobalRect().End.Y <= hud.GetGlobalRect().End.Y,
                    "Short Inspector stays onscreen while category tray retains its configured height");
                Require(inspector.Size.Y <= hud.Config.InspectorHeight + 1 && inspector.Size.X >= hud.Config.InspectorWidth - 1
                    && scroll.GetVScrollBar().MaxValue > scroll.Size.Y, "Inspector stays wider/shorter and scrolls long stats");
                scroll.EnsureControlVisible(inspector.GetNode<Button>("Insets/Body/UpgradeButton")); Require(scroll.ScrollVertical > 0, "Upgrade remains reachable inside bounded panel");
                hud.ShowTowerDetails("NEW SELECTION", "Test", "Short stats", "");
                Require(scroll.ScrollVertical == 0, "A new selection starts at its heading instead of retaining the previous scroll offset");
                foreach (string path in new[] { "BottomArea/Inspector", "BottomArea/TraySlot/TowerBar" })
                {
                    var panel = hud.GetNode<PanelContainer>(path); var style = (StyleBoxFlat)panel.GetThemeStylebox("panel");
                    Require(Math.Abs(style.BgColor.A - .9f) < .001 && panel.Modulate.A == 1 && panel.MouseFilter == Control.MouseFilterEnum.Stop,
                        "Only panel background is translucent; text stays opaque and panel consumes world clicks");
                }
                await TowerBuildBarChecks.Run(f.Main);
            }
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: category tabs/counts/filter/rebind/remembering, placement vs Inspector independence, compact scrolling Inspector and subtle backgrounds;40-entry tray regression"); GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
