// Level-owned pause overlay: shared menu styling, actual SceneTree pause and safe menu transitions.
using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
    public bool IsOpen => _screen?.Visible == true;
    private Control _screen;
    private Button _resume, _save, _load;
    private Label _saveReason;
    private GameInput _input;
    private Control _previousFocus;
    private AcceptDialog _error;
    private bool _resultsLocked;

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
        _save = UIButtonFactory.Create("SAVE", SaveCheckpoint, UIButtonStyle.Action); content.AddChild(_save);
        _saveReason = UIButtonFactory.Label("", 14); _saveReason.AutowrapMode = TextServer.AutowrapMode.WordSmart; content.AddChild(_saveReason);
        _load = UIButtonFactory.Create("LOAD", LoadCheckpoint, UIButtonStyle.Action); content.AddChild(_load);
        foreach (string title in new[] { "OPTIONS" })
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
        if (_resultsLocked) return false;
        if (_error.Visible) { _error.Hide(); return true; }
        if (IsOpen) Resume(); else Open();
        return true;
    }

    // =========================================================
    // Capture keyboard focus so resuming returns to the existing HUD without changing build state.
    public void Open()
    {
        if (_resultsLocked || IsOpen || GetTree().Paused) return;
        _previousFocus = GetViewport().GuiGetFocusOwner();
        RefreshSaveControls(); _screen.Show(); GetTree().Paused = true; _resume.GrabFocus();
    }

    // =========================================================
    // Resume has no save, wave or placement side effects.
    public void Resume()
    {
        if (_resultsLocked || !IsOpen) return;
        _screen.Hide(); GetTree().Paused = false;
        if (GodotObject.IsInstanceValid(_previousFocus) && _previousFocus.IsVisibleInTree()) _previousFocus.GrabFocus();
        else _resume.ReleaseFocus();
        _previousFocus = null;
    }

    // =========================================================
    // Shared navigation unpauses before transition and restores pause if scene loading fails.
    public void QuitToMainMenu()
    {
        if (_resultsLocked) return;
        try { MenuNavigation.Open(this, MenuNavigation.MainScene); }
        catch (Exception error) { _error.DialogText = error.Message; _error.PopupCentered(new Vector2I(460, 180)); }
    }

    // =========================================================
    // Transfer pause ownership to the final debrief without briefly resuming the battlefield.
    public void LockForResults()
    {
        _resultsLocked = true; _screen.Hide(); _error.Hide(); _previousFocus = null;
    }

    // =========================================================
    // External scene changes cannot strand the next screen paused or retain old bindings.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_input)) _input.Unbind(this);
        if (IsOpen) GetTree().Paused = false;
    }

    // =========================================================
    // One eligibility refresh per pause-open/write; paused gameplay cannot change beneath these buttons.
    private void RefreshSaveControls()
    {
        string reason = (GetParent() as BattlefieldLauncher)?.Saves?.SaveUnavailableReason() ?? "Battlefield saving is unavailable.";
        _save.Disabled = reason.Length > 0; _save.TooltipText = reason;
        _saveReason.Text = reason; _saveReason.Visible = reason.Length > 0;
        try
        {
            string unavailable = CampaignSession.LoadUnavailableReason(CampaignSession.ReadCurrent());
            _load.Disabled = unavailable.Length > 0; _load.TooltipText = _load.Disabled ? unavailable : "Load this profile's saved battlefield. " + CampaignSession.RecoveryMessage;
        }
        catch (Exception error) { _load.Disabled = true; _load.TooltipText = error.Message; }
    }
    // =========================================================
    // Recheck ownership/combat inside the command; disabled controls are not the security or consistency boundary.
    private void SaveCheckpoint()
    {
        try
        {
            ((BattlefieldLauncher)GetParent()).Saves.Save();
            _error.Title = "Battlefield saved"; _error.DialogText = "Saved for " + ProfileStore.Selected.Name + ".";
            _error.PopupCentered(new Vector2I(460, 180)); RefreshSaveControls();
        }
        catch (Exception error) { ShowSaveError(error); }
    }
    // =========================================================
    // Scene replacement is deferred outside the pressed signal; load failures retain the paused battlefield.
    private void LoadCheckpoint() => Callable.From(() =>
    {
        try { CampaignSession.LoadCurrent(this); }
        catch (Exception error) { ShowSaveError(error); }
    }).CallDeferred();
    // =========================================================
    // Never present a failed write or restore as success.
    private void ShowSaveError(Exception error)
    { _error.Title = "Save / Load error"; _error.DialogText = error.Message; _error.PopupCentered(new Vector2I(460, 180)); }
}
