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
            foreach (string path in new[] { MenuNavigation.Level001Scene, MenuNavigation.Level020Scene }) await Run(path);
            await PreviewLayout();
            await StableBuildTray();
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
        Require(f.Hud.Config.InspectorWidth == 560 && f.Hud.Config.InspectorHeight == 252, "Latest Inspector defaults retained");
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
        GD.Print("PASS: build previews for all catalog types fit the content-sized, bottom-aligned Inspector at1920x1080,1366x768,960x540");
    }

    #region Stable Build Tray Regression
    // =========================================================
    // Reserve Inspector space before hover, keep a blank shell and prove two rows/overflow with real catalog data.
    private async Task StableBuildTray()
    {
        using AmmoLoopFixture f = new(this);
        GameplayHUD hud = f.Hud;
        hud.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        Control panel = hud.GetNode<Control>("BottomArea/Inspector"), body = panel.GetNode<Control>("Insets/Body");
        Control tray = hud.GetNode<Control>("BottomArea/TraySlot/TowerBar");
        GridContainer entries = tray.GetNode<GridContainer>("Insets/Tray/Scroll/Buttons");
        ScrollContainer scroll = entries.GetParent<ScrollContainer>();
        foreach (Vector2 size in new[] { new Vector2(1920,1080), new Vector2(1366,768), new Vector2(960,540) })
        {
            GetTree().Root.Size = (Vector2I)size; GetTree().Root.ContentScaleSize = (Vector2I)size;
            hud.SetTowerBarOpen(false, true); hud.Size = size; hud.SetTowerBarOpen(true, true);
            await SettleTray();
            Require(panel.Visible && !body.Visible && !hud.IsInspectionVisible, "Ctrl opening shows blank reserved shell");
            Require(Math.Abs(tray.Size.Y - hud.Config.BottomBarHeight) < .01f, "Two rows fit configured tray height");
            Require(hud.Config.BottomBarHeight == 224 && hud.Config.InspectorHeight == 252,
                "Compact tray/Inspector defaults are enabled");
            Require(Math.Abs(tray.GetGlobalRect().End.Y - hud.GetGlobalRect().End.Y) < 1.1f
                && Math.Abs(panel.GetGlobalRect().End.Y - hud.GetGlobalRect().End.Y) < 1.1f,
                "Both bottom panels touch the viewport bottom at " + size);
            Require(panel.Size.Y <= 253f && tray.Size.Y <= 225f,
                "Blank Inspector shell and tray have no old oversized vertical reservation");
            Rect2 before = tray.GetGlobalRect(); Button rocket = hud.GetBuildButton("rocket");
            Require(rocket != null, "Actual Rocket build entry exists");
            hud._Input(new InputEventMouseMotion { Relative = Vector2.One });
            rocket.EmitSignal(Control.SignalName.MouseEntered); await SettleTray();
            Require(body.Visible && tray.GetGlobalRect() == before && before.End.X <= panel.GetGlobalRect().Position.X,
                "Hover cannot move tray or cover Rocket at " + size);
            Require(entries.Columns == (f.Placement.Catalog.Definitions.Count(d => d.Available) + 1) / 2 &&
                entries.Size.Y <= 121 && scroll.GetVScrollBar().Visible == false
                && scroll.Size.Y >= entries.Size.Y + (scroll.GetHScrollBar().Visible ? scroll.GetHScrollBar().Size.Y : 0), "Compact two-row buttons and horizontal scrollbar fit vertically");
            scroll.EnsureControlVisible(rocket); await SettleTray();
            Vector2 cursor = rocket.GetGlobalRect().GetCenter();
            GetViewport().PushInput(new InputEventMouseMotion { Position = cursor, GlobalPosition = cursor, Relative = Vector2.One });
            await SettleTray();
            Require(GetViewport().GuiGetHoveredControl() == rocket, "Real pointer hits Rocket rather than Inspector: " + GetViewport().GuiGetHoveredControl()?.GetPath() + " cursor=" + cursor + " tile=" + rocket.GetGlobalRect() + " viewport=" + GetViewport().GetVisibleRect());
            GetViewport().PushInput(new InputEventMouseButton { Position = cursor, GlobalPosition = cursor, ButtonIndex = MouseButton.Left, Pressed = true });
            GetViewport().PushInput(new InputEventMouseButton { Position = cursor, GlobalPosition = cursor, ButtonIndex = MouseButton.Left, Pressed = false });
            Require(f.Placement.IsPlacing && hud.IsPlacementPresentation, "Rocket can be chosen beside Inspector");
            f.Placement.Call("CancelPlacement"); await SettleTray();
            Require(panel.Visible && !body.Visible && !hud.IsInspectionVisible, "Cancel restores blank shell without stale details");
            Require(tray.GetGlobalRect() == before, "Placement/cancel never expands the tray");
        }
        TowerDefinition source = f.Placement.Catalog.Find("battle");
        TowerCatalog many = new();
        for (int n = 0; n < 40; n++)
            many.Definitions.Add(new TowerDefinition { TowerId = "tray-layout-" + n, Scene = source.Scene,
                DefaultConfig = source.DefaultConfig, Icon = source.Icon, Category = source.Category });
        many.Rebuild(); hud.BindCatalog(many); await SettleTray();
        Require(entries.Columns == 20 && entries.Size.Y <= 121 && scroll.GetHScrollBar().MaxValue > scroll.Size.X,
            "Forty entries remain two rows and overflow horizontally");
        scroll.EnsureControlVisible(hud.GetBuildButton("tray-layout-39")); await SettleTray();
        Require(scroll.ScrollHorizontal > 0 && tray.GetGlobalRect().End.X <= panel.GetGlobalRect().Position.X, "Final tile scrolls into reserved tray");
        hud.BindCatalog(f.Placement.Catalog); hud.SelectBuildCategory(TowerDefinition.TowerCategory.Economy); await SettleTray();
        Require(entries.Columns == 1, "Filtering recomputes two-row columns without empty gaps");
        GD.Print("PASS: blank Ctrl shell, stable non-overlapping Rocket tray, two-row categories and forty-tile horizontal scrolling at three sizes");
    }
    // =========================================================
    // Native containers settle before geometry assertions; there is no wall-clock delay.
    private async Task SettleTray()
    { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    #endregion

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
        f.Hud._Input(new InputEventMouseMotion { Relative = Vector2.One });
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
        // =========================================================
        // Only physical Right Shift permits consecutive paid placements; releasing returns to one-shot.
        f.Hud.SetTowerBarOpen(true, true);
        button.EmitSignal(Button.SignalName.Pressed);
        f.Placement._Input(new InputEventKey { PhysicalKeycode = Key.Shift, Location = KeyLocation.Right, Pressed = true });
        for (int i = 0; i < 2; i++)
        {
            Vector2I next = grid.Level.Passable.GetUsedCells().First(c => f.Spawns.PlacementFailure(c).Length == 0);
            int before = f.Towers.GetChildCount(), credits = f.Wallet.GetBalance(f.Wallet.CreditsResource);
            f.Placement.Call("HandleWorldClick", next);
            Require(f.Towers.GetChildCount() == before + 1 && f.Wallet.GetBalance(f.Wallet.CreditsResource) == credits - 200
                && f.Placement.IsPlacing && f.Hud.IsPlacementPresentation && !lower.Visible && !f.Hud.IsInspectionVisible,
                "Held Right Shift keeps the same tower ghost armed after one paid placement");
            Vector2I another = grid.Level.Passable.GetUsedCells().First(c => f.Spawns.PlacementFailure(c).Length == 0);
            f.Placement.Call("HandleWorldClick", another);
            Require(f.Towers.GetChildCount() == before + 1, "Same-frame second click cannot buy twice");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        f.Placement._Input(new InputEventKey { PhysicalKeycode = Key.Shift, Location = KeyLocation.Right, Pressed = false });
        Vector2I finalCell = grid.Level.Passable.GetUsedCells().First(c => f.Spawns.PlacementFailure(c).Length == 0);
        f.Placement.Call("HandleWorldClick", finalCell);
        Require(!f.Placement.IsPlacing && lower.Visible && !f.Hud.IsInspectionVisible &&
            f.Wallet.GetBalance(f.Wallet.CreditsResource) == 200, "Releasing Right Shift makes the next purchase one-shot");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        f.Wallet.Initialize(new LevelConfig { StartingCredits = 400 });
        button.EmitSignal(Button.SignalName.Pressed);
        f.Placement._Input(new InputEventKey { PhysicalKeycode = Key.Shift, Location = KeyLocation.Left, Pressed = true });
        Vector2I leftCell = grid.Level.Passable.GetUsedCells().First(c => f.Spawns.PlacementFailure(c).Length == 0);
        f.Placement.Call("HandleWorldClick", leftCell);
        Require(!f.Placement.IsPlacing && f.Wallet.GetBalance(f.Wallet.CreditsResource) == 200,
            "Left Shift does not enable repeat placement");
        f.Placement._Input(new InputEventKey { PhysicalKeycode = Key.Shift, Location = KeyLocation.Left, Pressed = false });
    }
}
