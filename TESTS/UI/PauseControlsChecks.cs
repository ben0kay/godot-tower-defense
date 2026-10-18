// Real menu/battlefield transitions and input delivery; memory-only profile, no campaign save writes.
using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

public partial class PauseControlsChecks : Node
{
    private GameInput _input;
    private int _ticks;

    // =========================================================
    // This runner remains active while the battlefield pauses and replaces its scene.
    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            await Frame(); GetTree().CurrentScene = null;
            _input = GetNode<GameInput>("/root/GameInput");
            Require(InputMap.HasAction(GameInput.FullscreenAction), "F11 named action registered");
            DisplayServer.WindowMode restore = DisplayServer.WindowMode.Windowed;
            Require(GameInput.FullscreenTarget(DisplayServer.WindowMode.Fullscreen, ref restore) == DisplayServer.WindowMode.Windowed,
                "Fullscreen startup returns windowed");
            foreach (DisplayServer.WindowMode mode in new[] { DisplayServer.WindowMode.Windowed, DisplayServer.WindowMode.Maximized })
            {
                Require(GameInput.FullscreenTarget(mode, ref restore) == DisplayServer.WindowMode.Fullscreen, "F11 enters fullscreen");
                Require(GameInput.FullscreenTarget(DisplayServer.WindowMode.ExclusiveFullscreen, ref restore) == mode, "F11 restores prior mode");
            }
            PlayerProfile fixture = new() { Id = Guid.NewGuid().ToString("N"), Name = "Pause fixture" };
            typeof(ProfileStore).GetField("_profiles", BindingFlags.Static | BindingFlags.NonPublic)
                .SetValue(null, new List<PlayerProfile> { fixture });
            ProfileStore.ClearSelection();
            foreach (string path in new[] { MenuNavigation.BootScene, MenuNavigation.MainScene, MenuNavigation.CampaignScene })
            {
                if (path != MenuNavigation.BootScene) ProfileStore.Select(fixture.Id);
                MenuNavigation.Open(this, path); await Scene<Control>();
                KeyPress(Key.F11); Require(GetViewport().IsInputHandled(), "Global F11 routed in " + path);
                KeyPress(Key.Ctrl); KeyPress(Key.Escape);
                Require(_input.BindingCount == 0 && !GetTree().Paused, "No gameplay bindings in " + path);
            }
            foreach (string level in new[] { MenuNavigation.Level001Scene, MenuNavigation.Level020Scene })
            {
                MenuNavigation.Open(this, level); Node2D scene = await Scene<Node2D>();
                GameplayHUD hud = scene.GetNode<GameplayHUD>("UI/GameplayHUD");
                TowerPlacementController placement = scene.GetNode<TowerPlacementController>("TowerPlacement");
                PauseMenu pause = scene.GetNode<PauseMenu>("PauseMenu");
                Require(_input.BindingCount == 4 && !pause.IsOpen, "Exactly one set of level bindings");
                hud.SetTowerBarOpen(false, true); KeyPress(Key.Ctrl);
                Require(hud.IsTowerBarOpen, "Real Ctrl event opens tray exactly once");
                KeyPress(Key.Ctrl, echo: true); KeyPress(Key.Ctrl, pressed: false);
                Require(hud.IsTowerBarOpen, "Repeat and release ignored");
                hud.GetBuildButton("battle").EmitSignal(Button.SignalName.Pressed);
                KeyPress(Key.Escape);
                Require(!placement.IsPlacing && !pause.IsOpen && hud.IsTowerBarOpen, "Escape cancels placement only");
                typeof(CameraController).GetField("_dragging", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(scene.GetNode<CameraController>("CameraRig"), true);
                KeyPress(Key.Escape);
                Require(pause.IsOpen && GetTree().Paused, "Second Escape opens actual pause");
                KeyPress(Key.F11); Require(GetViewport().IsInputHandled() && GetTree().Paused, "F11 works while paused");
                KeyPress(Key.Ctrl); KeyPress(Key.Escape, echo: true);
                Require(pause.IsOpen && hud.IsTowerBarOpen, "Paused Ctrl/repeat cannot change gameplay");
                foreach (string title in new[] { "SAVE", "LOAD", "OPTIONS" })
                    Require(ButtonFor(pause, title + " (NOT YET AVAILABLE)").Disabled, title + " is visibly unavailable");
                Require(ButtonFor(pause, "EXIT GAME") != null, "Exit action exists");
                Timer probe = new() { WaitTime = 10, OneShot = true }; scene.AddChild(probe); probe.Start();
                CameraController camera = scene.GetNode<CameraController>("CameraRig");
                Require(!(bool)typeof(CameraController).GetField("_dragging", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(camera),
                    "Pause clears camera drag state");
                double remaining = probe.TimeLeft;
                for (int i = 0; i < 5; i++) await Frame();
                Require(probe.TimeLeft == remaining, "Battlefield timers really stop while paused");
                _ticks = 0;
                Require(!hud.CanProcess() && !placement.CanProcess() && !probe.CanProcess() && pause.CanProcess() && _input.CanProcess(),
                    "World and HUD suspended; pause/router active");
                await Frame(); KeyPress(Key.Escape);
                Require(!GetTree().Paused && !pause.IsOpen && hud.CanProcess(), "Escape resumes world");
                KeyPress(Key.Escape); ButtonFor(pause, "RESUME").EmitSignal(Button.SignalName.Pressed);
                Require(!GetTree().Paused && !pause.IsOpen, "Actual Resume button works");
                // A higher-priority modal consumes Back without opening the underlying pause screen.
                _input.Bind(this, GameInput.BackAction, () => { _ticks++; return true; }, 200);
                KeyPress(Key.Escape); _input.Unbind(this);
                Require(_ticks == 1 && !pause.IsOpen, "Modal priority dispatches exactly one action");
                KeyPress(Key.Escape); ButtonFor(pause, "QUIT TO MAIN MENU").EmitSignal(Button.SignalName.Pressed);
                await Scene<MainMenu>();
                Require(!GetTree().Paused && _input.BindingCount == 0, "Quit returns to Main and retires bindings");
            }
            ProfileStore.ClearSelection(); MenuNavigation.Open(this, MenuNavigation.Level001Scene);
            Node2D direct = await Scene<Node2D>(); KeyPress(Key.Escape);
            Require(direct.GetNode<PauseMenu>("PauseMenu").IsOpen, "Direct F6-style play requires no profile");
            direct.GetNode<PauseMenu>("PauseMenu").QuitToMainMenu(); await Scene<BootScreen>();
            Require(!GetTree().Paused && _input.BindingCount == 0, "Profileless quit safely redirects to Boot");
            GD.Print("PASS: scoped Ctrl/Escape, placement priority, modal priority, pause/resume, disabled placeholders, both levels, scene cleanup and profileless return");
            await SubsystemBattlefield.DrainFreedFixtures(this); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Paused = false; GetTree().Quit(1); }
    }

    // =========================================================
    // Deliver through Godot's actual input propagation rather than invoking an obsolete HUD listener.
    private void KeyPress(Key key, bool echo = false, bool pressed = true) =>
        GetViewport().PushInput(new InputEventKey { Keycode = key, Pressed = pressed, Echo = echo, CtrlPressed = key == Key.Ctrl && pressed });

    // =========================================================
    // Bound async transition waits also work while the tree is paused.
    private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    private async Task<T> Scene<T>() where T : Node
    {
        for (int i = 0; i < 60; i++)
        { await Frame(); if (GetTree().CurrentScene is T scene && scene.IsNodeReady()) return scene; }
        throw new InvalidOperationException("Scene transition failed: " + typeof(T).Name);
    }

    // =========================================================
    // Locate visible production buttons and fail with a concrete assertion.
    private static Button ButtonFor(Node node, string text)
    {
        if (node is Button button && button.Text == text) return button;
        foreach (Node child in node.GetChildren()) { Button found = ButtonFor(child, text); if (found != null) return found; }
        return null;
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
