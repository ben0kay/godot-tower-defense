// Level-owned pause overlay: shared menu styling, actual SceneTree pause and safe menu transitions.
using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
    public bool IsOpen => _screen?.Visible == true;
    private Control _screen;
    private Button _resume;
    private GameInput _input;
    private Control _previousFocus;
    private AcceptDialog _error;

    // =========================================================
    // Always-process UI remains interactive while the entire battlefield is paused.
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; Layer = 100;
        _screen = new Control { Name = "Screen", MouseFilter = Control.MouseFilterEnum.Stop };
        AddChild(_screen); _screen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        VBoxContainer content = MenuLayout.Create(_screen, "PAUSED", "BATTLEFIELD ON HOLD", 560f);
        _screen.GetChild<ColorRect>(0).Color = new Color(0.02f, 0.05f, 0.09f, 0.88f);
        _resume = UIButtonFactory.Create("RESUME", Resume); content.AddChild(_resume);
        foreach (string title in new[] { "SAVE", "LOAD", "OPTIONS" })
        {
            Button pending = UIButtonFactory.Create(title + " (NOT YET AVAILABLE)", null, UIButtonStyle.Action);
            pending.Disabled = true; pending.TooltipText = "Not implemented yet."; content.AddChild(pending);
        }
        content.AddChild(UIButtonFactory.Create("QUIT TO MAIN MENU", QuitToMainMenu, UIButtonStyle.Action));
        content.AddChild(UIButtonFactory.Create("EXIT GAME", () => GetTree().Quit(), UIButtonStyle.Danger));
        _error = new AcceptDialog { Title = "Navigation error" }; AddChild(_error);
        UIButtonFactory.Apply(_error.GetOkButton(), UIButtonStyle.Action);
        _screen.Hide();
        _input = GetNode<GameInput>("/root/GameInput");
        _input.Bind(this, GameInput.BackAction, HandleEscape);
        _input.Bind(this, GameInput.BuildAction, () => IsOpen, 100);
    }

    // =========================================================
    // Placement has higher priority; a single Escape either cancels placement or toggles pause.
    private bool HandleEscape()
    {
        if (_error.Visible) { _error.Hide(); return true; }
        if (IsOpen) Resume(); else Open();
        return true;
    }

    // =========================================================
    // Capture keyboard focus so resuming returns to the existing HUD without changing build state.
    public void Open()
    {
        if (IsOpen || GetTree().Paused) return;
        _previousFocus = GetViewport().GuiGetFocusOwner();
        _screen.Show(); GetTree().Paused = true; _resume.GrabFocus();
    }

    // =========================================================
    // Resume has no save, wave or placement side effects.
    public void Resume()
    {
        if (!IsOpen) return;
        _screen.Hide(); GetTree().Paused = false;
        if (GodotObject.IsInstanceValid(_previousFocus) && _previousFocus.IsVisibleInTree()) _previousFocus.GrabFocus();
        else _resume.ReleaseFocus();
        _previousFocus = null;
    }

    // =========================================================
    // Shared navigation unpauses before transition and restores pause if scene loading fails.
    public void QuitToMainMenu()
    {
        try { MenuNavigation.Open(this, MenuNavigation.MainScene); }
        catch (Exception error) { _error.DialogText = error.Message; _error.PopupCentered(new Vector2I(460, 180)); }
    }

    // =========================================================
    // External scene changes cannot strand the next screen paused or retain old bindings.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_input)) _input.Unbind(this);
        if (IsOpen) GetTree().Paused = false;
    }
}
