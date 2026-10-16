// Standalone production-menu navigation check; injected memory-only profile avoids user save writes.
using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

public partial class MenuTowerFlowChecks : Node
{
    // =========================================================
    // Persist this runner across real scene changes and use actual menu/HUD button callbacks.
    public override async void _Ready()
    {
        try
        {
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
            ButtonFor(campaign, "PLAY LEVEL 001").EmitSignal(Button.SignalName.Pressed);
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
            Require(placed && wallet.GetBalance(wallet.CreditsResource) == 800, "Menu-launched Battle placement pays 200");
            hud.GetNode<Button>("BottomArea/Inspector/Insets/Body/UpgradeButton").EmitSignal(Button.SignalName.Pressed);
            BattleTower tower = battlefield.GetNode("World/Towers").GetChild<BattleTower>(0);
            Require(tower.UpgradeLevel == 2 && tower.FinalStats.DamagePerShot == 16 && wallet.GetBalance(wallet.CreditsResource) == 650,
                "Menu-launched paid Upgrade action uses the same pipeline and wallet");
            hud.SetTowerBarOpen(false, true);
            Require(hud.IsInspectionVisible, "Closing the tray preserves placed-tower inspection");
            GD.Print("PASS: Boot → Main → Campaign → Level001 real button flow, catalog build and paid Battle upgrade (no save writes)");
            GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
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
