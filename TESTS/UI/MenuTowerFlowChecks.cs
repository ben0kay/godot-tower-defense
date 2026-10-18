// Standalone production-menu navigation check; injected memory-only profile avoids user save writes.
using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;

public partial class MenuTowerFlowChecks : Node
{
    // =========================================================
    // Persist this runner across real scene changes and use actual menu/HUD button callbacks.
    public override async void _Ready()
    {
        string temporary = Path.Combine(Path.GetTempPath(), "td-menu-flow-" + Guid.NewGuid().ToString("N"));
        FieldInfo profiles = typeof(ProfileStore).GetField("_profiles", BindingFlags.Static | BindingFlags.NonPublic);
        FieldInfo store = typeof(ProgressionService).GetField("_store", BindingFlags.Static | BindingFlags.NonPublic);
        object previousProfiles = profiles.GetValue(null), previousStore = store.GetValue(null);
        PlayerProfile previousSelection = ProfileStore.Selected;
        try
        {
            store.SetValue(null, new ProgressionStore(temporary));
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GetTree().CurrentScene = null;
            PlayerProfile fixture = new() { Id = Guid.NewGuid().ToString("N"), Name = "Tower flow fixture" };
            typeof(ProfileStore).GetField("_profiles", BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, new List<PlayerProfile> { fixture });
            ProfileStore.ClearSelection();
            MenuNavigation.Open(this, MenuNavigation.BootScene);
            BootScreen boot = await Scene<BootScreen>();
            ButtonFor(boot, fixture.Name).EmitSignal(Button.SignalName.Pressed);
            MainMenu menu = await Scene<MainMenu>();
            Require(ProfileStore.Selected == fixture, "Boot's real profile action selects its memory-only fixture");
            ButtonFor(menu, "CAMPAIGN").EmitSignal(Button.SignalName.Pressed);
            CampaignMenu campaign = await Scene<CampaignMenu>();
            Require(ButtonFor(campaign, "PLAY LEVEL 001") == null && ButtonFor(campaign, "RESEARCH") != null, "Hub separates Levels from Research");
            await Geometry(campaign);
            ButtonFor(campaign, "LEVELS").EmitSignal(Button.SignalName.Pressed);
            LevelSelectionMenu levels = await Scene<LevelSelectionMenu>();
            Require(ButtonFor(levels, "PLAY LEVEL 020") != null && ProfileStore.Selected == fixture, "Levels offers both existing missions for the same profile");
            await Geometry(levels);
            ButtonFor(levels, "BACK TO CAMPAIGN").EmitSignal(Button.SignalName.Pressed);
            campaign = await Scene<CampaignMenu>();
            ButtonFor(campaign, "LEVELS").EmitSignal(Button.SignalName.Pressed);
            levels = await Scene<LevelSelectionMenu>();
            ButtonFor(levels, "PLAY LEVEL 001").EmitSignal(Button.SignalName.Pressed);
            Node2D battlefield = await Scene<Node2D>();
            GameplayHUD hud = battlefield.GetNode<GameplayHUD>("UI/GameplayHUD");
            TowerPlacementController placement = battlefield.GetNode<TowerPlacementController>("TowerPlacement");
            WorldGrid grid = battlefield.GetNode<WorldGrid>("World/Grid");
            SpawnManager spawns = battlefield.GetNode<SpawnManager>("World/EnemySystems/Spawning");
            EconomyManager wallet = battlefield.GetNode<EconomyManager>("Economy");
            Require(hud.GetBuildButton("battle") != null && hud.GetBuildButton("blaster_cannon") != null && hud.GetBuildButton("shockwave") != null && wallet.GetBalance(wallet.CreditsResource) == 1000,
                "Campaign launch initializes default catalog and level wallet");
            hud.GetBuildButton("battle").EmitSignal(Button.SignalName.Pressed);
            PanelContainer tray = hud.GetNode<PanelContainer>("BottomArea/TraySlot/TowerBar");
            Sprite2D ghost = placement.GetNode<Sprite2D>("PlacementGhost");
            hud.SetTowerBarOpen(true, true);
            ghost.Show(); // Headless cursor position is outside the map; exercise preview cleanup explicitly.
            placement._Input(new InputEventMouseButton { ButtonIndex = MouseButton.Right, Pressed = true });
            Require(tray.Visible && !ghost.Visible && !placement.IsProcessing() && !hud.IsInspectionVisible,
                "RMB clears placement and build details while leaving the tray open");
            hud.GetBuildButton("battle").EmitSignal(Button.SignalName.Pressed);
            ghost.Show();
            GetNode<GameInput>("/root/GameInput")._UnhandledKeyInput(new InputEventKey { Keycode = Key.Ctrl, Pressed = true });
            Require(!ghost.Visible && !placement.IsProcessing() && !hud.IsInspectionVisible,
                "Ctrl immediately cancels placement before the close animation ends");
            await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
            Require(!tray.Visible, "Ctrl closes the build tray");
            GetNode<GameInput>("/root/GameInput")._UnhandledKeyInput(new InputEventKey { Keycode = Key.Ctrl, Pressed = true });
            Require(tray.Visible && !ghost.Visible && !placement.IsProcessing(), "Reopening the tray does not resume placement");
            hud.GetBuildButton("battle").EmitSignal(Button.SignalName.Pressed);
            bool placed = false;
            foreach (Vector2I cell in grid.Level.Passable.GetUsedCells())
                if (grid.PlacementFailure(cell).Length == 0 && spawns.PlacementFailure(cell).Length == 0)
                { placement.Call("TryPlace", cell); placed = true; break; }
            ConstructionTestClock.FinishInitial(battlefield);
            Require(placed && wallet.GetBalance(wallet.CreditsResource) == 800, "Menu-launched Battle placement pays 200");
            BattleTower tower = battlefield.GetNode("World/Towers").GetChild<BattleTower>(0);
            Require(!hud.IsInspectionVisible && !placement.IsPlacing, "Successful placement does not inspect the new tower");
            placement.Call("SelectTower", tower); // A later inspection action precedes upgrades.
            hud.GetNode<Button>("BottomArea/Inspector/Insets/Body/Columns/Main/UpgradeButton").EmitSignal(Button.SignalName.Pressed); ConstructionTestClock.FinishWork(battlefield);
            Require(tower.UpgradeLevel == 2 && tower.FinalStats.DamagePerShot == 33 && wallet.GetBalance(wallet.CreditsResource) == 650,
                "Menu-launched paid Upgrade action uses the same pipeline and wallet");
            hud.SetTowerBarOpen(false, true);
            Require(hud.IsInspectionVisible, "Closing the tray preserves placed-tower inspection");
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: Boot → Main → Campaign → Levels/back → Level001, both mission choices, viewport geometry, catalog build and paid Battle upgrade (temporary saves only)");
            GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
        finally
        {
            ProfileStore.ClearSelection(); profiles.SetValue(null, previousProfiles); store.SetValue(null, previousStore);
            if (previousSelection != null) ProfileStore.Select(previousSelection.Id);
            if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
        }
    }

    // =========================================================
    // Verify the actual hub/mission panels fit common viewports without changing shared menu layout.
    private async Task Geometry(Control screen)
    {
        var panel = screen.GetChild(1).GetChild(0).GetChild<PanelContainer>(0);
        screen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        foreach (Vector2 size in new[] { new Vector2(1920,1080), new Vector2(1366,768), new Vector2(960,540) })
        {
            screen.Size = size;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Rect2 rect = panel.GetGlobalRect(), bounds = screen.GetGlobalRect();
            Require(rect.Position.X >= bounds.Position.X - 1 && rect.Position.Y >= bounds.Position.Y - 1 &&
                rect.End.X <= bounds.End.X + 1 && rect.End.Y <= bounds.End.Y + 1, "Campaign panel fits " + size);
        }
    }

    // =========================================================
    // Bound scene-transition waits so missing/redirected destinations fail rather than hang.
    private async Task<T> Scene<T>() where T : Node
    {
        for (int i = 0; i < 30; i++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (GetTree().CurrentScene is T scene && scene.IsNodeReady()) return scene;
        }
        throw new InvalidOperationException("Menu transition did not reach " + typeof(T).Name);
    }

    // =========================================================
    // Find the actual generated menu button by its visible label.
    private static Button ButtonFor(Node root, string text)
    {
        if (root is Button button && button.Text == text) return button;
        foreach (Node child in root.GetChildren()) { Button found = ButtonFor(child, text); if (found != null) return found; }
        return null;
    }
    // =========================================================
    // Fail on startup-to-battlefield regression.
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
}
