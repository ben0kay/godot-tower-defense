// Global named shortcuts and scoped, priority-ordered screen bindings; no gameplay logic lives here.
using Godot;
using System;
using System.Collections.Generic;

public partial class GameInput : Node
{
    public const string FullscreenAction = "game_fullscreen", BuildAction = "game_build", BackAction = "game_back";
    private readonly List<Binding> _bindings = new();
    private DisplayServer.WindowMode _restoreMode = DisplayServer.WindowMode.Windowed;
    public int BindingCount => _bindings.Count;
    private sealed record Binding(Node Owner, string Action, Func<bool> Callback, int Priority);

    // =========================================================
    // One Autoload remains responsive while paused; named actions preserve existing Input Map bindings.
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        EnsureAction(FullscreenAction, Key.F11); EnsureAction(BuildAction, Key.Ctrl); EnsureAction(BackAction, Key.Escape);
    }

    // =========================================================
    // Add defaults only when the project has not supplied a binding for this action.
    private static void EnsureAction(string action, Key key)
    {
        if (InputMap.HasAction(action)) return;
        InputMap.AddAction(action); InputMap.ActionAddEvent(action, new InputEventKey { Keycode = key });
    }

    // =========================================================
    // Owners register once on entry and unregister on exit; higher priorities override lower screens.
    public void Bind(Node owner, string action, Func<bool> callback, int priority = 0)
    {
        _bindings.Add(new Binding(owner, action, callback, priority));
        _bindings.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    // =========================================================
    // Scene-owned callbacks must never retain unloaded HUDs or modal windows.
    public void Unbind(Node owner) => _bindings.RemoveAll(binding => binding.Owner == owner);

    // =========================================================
    // Global fullscreen is available even when focused UI handles other keyboard events.
    public override void _Input(InputEvent input)
    {
        if (!Pressed(input) || !input.IsActionPressed(FullscreenAction)) return;
        ToggleFullscreen(); GetViewport().SetInputAsHandled();
    }

    // =========================================================
    // GUI gets first refusal; dispatch at most one active scoped action, including during pause.
    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (!Pressed(input)) return;
        foreach (Binding binding in _bindings)
        {
            if (!GodotObject.IsInstanceValid(binding.Owner) || !binding.Owner.IsInsideTree() || !binding.Owner.CanProcess()) continue;
            if (!input.IsActionPressed(binding.Action) || !binding.Callback()) continue;
            GetViewport().SetInputAsHandled(); return;
        }
    }

    // =========================================================
    // Release and OS repeat never retrigger toggles.
    private static bool Pressed(InputEvent input) => input is InputEventKey key && key.Pressed && !key.Echo;

    // =========================================================
    // Restore maximized/windowed state; fullscreen startup has a sensible windowed fallback.
    public void ToggleFullscreen()
    {
        DisplayServer.WindowSetMode(FullscreenTarget(DisplayServer.WindowGetMode(), ref _restoreMode));
    }

    // =========================================================
    // Separate the tiny restoration rule so headless checks do not depend on a desktop window manager.
    internal static DisplayServer.WindowMode FullscreenTarget(DisplayServer.WindowMode current, ref DisplayServer.WindowMode restore)
    {
        if (current is DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen) return restore;
        restore = current == DisplayServer.WindowMode.Maximized ? current : DisplayServer.WindowMode.Windowed;
        return DisplayServer.WindowMode.Fullscreen;
    }
}
