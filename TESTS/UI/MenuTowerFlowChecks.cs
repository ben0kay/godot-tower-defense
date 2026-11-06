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
            Require(AudioManager.Instance?.GetNodeOrNull<VoiceAssistant>("VoiceAssistant")?.CurrentCueId ==
                "voice_profile_welcome", "Selecting a valid profile starts the persistent welcome voice cue");
            MainMenu menu = await Scene<MainMenu>();
            Require(ProfileStore.Selected == fixture, "Boot's real profile action selects its memory-only fixture");
            ButtonFor(menu, "CAMPAIGN").EmitSignal(Button.SignalName.Pressed);
            CampaignMenu campaign = await Scene<CampaignMenu>();
            Require(MissionCard(campaign, 1) != null && MissionCard(campaign, 2) != null
                && MissionCard(campaign, 3) != null && MissionCard(campaign, 20) != null
                && ButtonFor(campaign, "RESEARCH") != null && ButtonFor(campaign, "CONTINUE") != null,
                "Full-width hub exposes the ordered mission cards and preserves Research/Continue");
            Require(ProfileStore.Selected == fixture, "Campaign roster retains selected profile");
            await Geometry(campaign);
            MissionCard(campaign, 1).EmitSignal(Button.SignalName.Pressed);
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
            hud.GetNode<Button>("BottomArea/Inspector/Insets/Body/Columns/Main/Actions/UpgradeButton").EmitSignal(Button.SignalName.Pressed); ConstructionTestClock.FinishWork(battlefield);
            Require(tower.UpgradeLevel == 2 && tower.FinalStats.DamagePerShot == 33 && wallet.GetBalance(wallet.CreditsResource) == 650,
                "Menu-launched paid Upgrade action uses the same pipeline and wallet");
            hud.SetTowerBarOpen(false, true);
            Require(hud.IsInspectionVisible, "Closing the tray preserves placed-tower inspection");
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PASS: Boot → Main → full-width Campaign grid → Level001, mission choices, viewport geometry, catalog build and paid Battle upgrade (temporary saves only)");
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
    // =========================================================
    // Check the actual 2:1 campaign split and scrolling/anchoring at common viewport sizes.
    private async Task Geometry(CampaignMenu screen)
    {
        PanelContainer left = screen.FindChild("MissionPanel", true, false) as PanelContainer;
        PanelContainer right = screen.FindChild("CommandPanel", true, false) as PanelContainer;
        GridContainer grid = screen.FindChild("MissionGrid", true, false) as GridContainer;
        ScrollContainer scroll = screen.FindChild("MissionScroll", true, false) as ScrollContainer;
        Button back = ButtonFor(screen, "BACK TO MAIN MENU");
        Require(left != null && right != null && grid?.Columns == 3 && scroll != null && back != null,
            "Campaign has three-column scroll grid and fixed right controls");
        screen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        foreach (Vector2 size in new[] { new Vector2(1920, 1080), new Vector2(1366, 768), new Vector2(960, 540) })
        {
            screen.Size = size;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Rect2 bounds = screen.GetGlobalRect(), l = left.GetGlobalRect(), r = right.GetGlobalRect();
            Require(l.Position.X >= bounds.Position.X - 1 && l.Position.Y >= bounds.Position.Y - 1
                && r.End.X <= bounds.End.X + 1 && r.End.Y <= bounds.End.Y + 1,
                "Both full-screen campaign panels fit " + size);
            Require(l.Size.X > r.Size.X * 1.35f && r.Position.X > l.End.X - 1
                && back.GetGlobalRect().End.Y <= bounds.End.Y + 1,
                "Scrollable mission panel takes most width and fixed Back stays visible at " + size);
        }
    }

    // =========================================================
    // New campaign cards launch directly using the existing mission metadata.
    private static Button MissionCard(Node root, int level) =>
        root.FindChild($"MissionLevel{level:000}", true, false) as Button;

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
