// Actual build-tile signals, wallet, ghost, placement and scoped controls across both battlefields.
using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using static AmmoLoopFixture;

public partial class PlacementHudChecks : Node
{
    // =========================================================
    // Keep the runner alive through mission/GUI process changes without modifying profile persistence.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            ProfileStore.ClearSelection();
            foreach (string path in new[] { MenuNavigation.Level001Scene, MenuNavigation.Level002Scene }) await Run(path);
            await PreviewLayout();
            await SubsystemBattlefield.DrainFreedFixtures(this);
            GD.Print("PlacementHudChecks: PASS both maps hover/focus/cost/affordability, denied signal, hidden placement, RMB/Ctrl, no auto-selection and separate later inspection");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }

    // =========================================================
    // The new basic build view must respect the recent 560px visual resize without changing layout resources.
    private async Task PreviewLayout()
    {
        using AmmoLoopFixture f = new(this);
        f.Hud.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft); f.Hud.SetTowerBarOpen(true, true);
        Control panel = f.Hud.GetNode<Control>("BottomArea/Inspector");
        Require(f.Hud.Config.InspectorWidth == 560 && f.Hud.Config.InspectorHeight == 330, "Latest Inspector defaults retained");
        foreach (Vector2I viewport in new[] { new Vector2I(1920, 1080), new Vector2I(1366, 768), new Vector2I(960, 540) })
        {
            f.Hud.Size = viewport;
            foreach (TowerDefinition definition in f.Placement.Catalog.Definitions)
            {
                if (!definition.Available) continue;
                f.Placement.Call("PreviewBuild", definition.TowerId);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(panel.Size.X <= 560.5f && panel.Size.Y <= 331 && panel.GetGlobalRect().Position.Y >= f.Hud.Config.TopBarHeight &&
                    panel.GetGlobalRect().End.Y <= f.Hud.GetGlobalRect().End.Y + 1, "Basic build preview respects resized Inspector for " + definition.TowerId + " at " + viewport);
            }
        }
        GD.Print("PASS: build previews for all catalog types fit the existing560x330 Inspector at1920x1080,1366x768,960x540");
    }

    // =========================================================
    // The same cell-based production input handler is used by unhandled LMB and these headless checks.
    private async Task Run(string scene)
    {
        using AmmoLoopFixture f = new(this, scene);
        f.Hud.SetTowerBarOpen(true, true);
        var button = f.Hud.GetBuildButton("battle");
        var lower = f.Hud.GetNode<Control>("BottomArea");
        var tray = f.Hud.GetNode<Control>("BottomArea/TraySlot/TowerBar");
        var top = f.Hud.GetNode<Control>("TopBar");
        var ghost = f.Placement.GetNode<Sprite2D>("PlacementGhost");
        var details = f.Hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/Columns/Main/TowerStats");
        var notes = f.Hud.GetNode<Label>("BottomArea/Inspector/Insets/Body/Columns/Main/Notes");
        var grid = f.Main.GetNode<WorldGrid>("World/Grid");
        Vector2I cell = grid.Level.Passable.GetUsedCells().First(c => f.Spawns.PlacementFailure(c).Length == 0);
        int rejected = 0; f.Placement.BuildDenied += (_, _) => rejected++;
        button.EmitSignal(Control.SignalName.MouseEntered);
        Require(f.Hud.IsInspectionVisible && lower.Visible && !f.Placement.IsPlacing && !ghost.Visible &&
            details.Text.Contains("200 Credits") && notes.Visible && notes.Text.Contains("CAN AFFORD"), "Hover shows basic cost/affordability without placement");
        f.Wallet.Initialize(new LevelConfig { StartingCredits = 199 });
        Require(notes.Text.Contains("INSUFFICIENT"), "Wallet events update the visible affordability preview");
        button.EmitSignal(Button.SignalName.Pressed);
        Require(rejected == 1 && !f.Placement.IsPlacing && lower.Visible && tray.Visible && f.Hud.IsInspectionVisible &&
            f.Wallet.GetBalance(f.Wallet.CreditsResource) == 199 && f.Towers.GetChildCount() == 0, "Unaffordable click stays browsing and emits future-audio hook without payment");
        f.Wallet.Initialize(new LevelConfig { StartingCredits = 1000 });
        button.EmitSignal(Control.SignalName.FocusEntered);
        Require(notes.Text.Contains("CAN AFFORD") && !f.Placement.IsPlacing, "Keyboard focus previews without arming");
        button.EmitSignal(Button.SignalName.Pressed);
        f.Placement.Call("RefreshPreview", cell, true);
        Require(f.Placement.IsPlacing && f.Hud.IsPlacementPresentation && f.Hud.IsTowerBarOpen && !lower.Visible &&
            !f.Hud.IsInspectionVisible && ghost.Visible && top.Visible, "Affordable click hides lower UI while keeping logical tray state and top status");
        f.Placement.Call("HandleWorldClick", new Vector2I(-1, -1));
        Require(f.Placement.IsPlacing && !lower.Visible && !f.Hud.IsInspectionVisible && f.Towers.GetChildCount() == 0, "Rejected ground click never reopens Inspector");
        f.Placement._Input(new InputEventMouseButton { ButtonIndex = MouseButton.Right, Pressed = true });
        Require(!f.Placement.IsPlacing && !f.Placement.IsProcessing() && !ghost.Visible && lower.Visible && tray.Visible &&
            !f.Hud.IsInspectionVisible && f.Hud.IsTowerBarOpen, "RMB restores tray and cancels ghost/selection");
        button.EmitSignal(Button.SignalName.Pressed);
        GetNode<GameInput>("/root/GameInput")._UnhandledKeyInput(new InputEventKey { Keycode = Key.Ctrl, Pressed = true });
        Require(!f.Placement.IsPlacing && !f.Hud.IsPlacementPresentation && !f.Hud.IsTowerBarOpen && !tray.Visible &&
            !ghost.Visible && !f.Hud.IsInspectionVisible, "Ctrl cancels and actually closes the menu immediately");
        GetNode<GameInput>("/root/GameInput")._UnhandledKeyInput(new InputEventKey { Keycode = Key.Ctrl, Pressed = true });
        Require(f.Hud.IsTowerBarOpen && !f.Placement.IsPlacing, "Ctrl reopen does not resume a canceled tower");
        button.EmitSignal(Button.SignalName.Pressed);
        f.Placement.Call("HandleWorldClick", cell);
        Require(f.Towers.GetChildCount() == 1 && f.Wallet.GetBalance(f.Wallet.CreditsResource) == 800 && !f.Placement.IsPlacing &&
            lower.Visible && !f.Hud.IsInspectionVisible && !ghost.Visible, "Successful one-shot placement restores HUD but never inspects the placed tower");
        f.Placement.Call("HandleWorldClick", cell);
        Require(!f.Hud.IsInspectionVisible && f.Towers.GetChildCount() == 1, "The placement frame cannot also select or buy twice");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        button.EmitSignal(Control.SignalName.MouseEntered);
        Require(!f.Hud.IsInspectionVisible, "A restored tray under a stationary cursor cannot reopen a build preview");
        f.Placement.Call("HandleWorldClick", cell);
        Require(f.Hud.IsInspectionVisible && f.Towers.GetChildCount() == 1, "A separate later click inspects normally");
        f.Hud.SetTowerBarOpen(false, true);
        Require(f.Hud.IsInspectionVisible, "Closing the tray preserves separately inspected placed towers");
    }
}
