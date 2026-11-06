// Full-window campaign hub: direct mission grid and fixed command/actions panel.
using Godot;
using System;

public partial class CampaignMenu : Control
{
    private AcceptDialog _errorDialog;

    [ExportGroup("Layout")]
    [Export(PropertyHint.Range, "8,48,1")] public int ScreenMarginPixels { get; set; } = 22;
    [ExportGroup("Development")]
    [Export] public bool ShowProgressionDebugGrant { get; set; }

    // =========================================================
    // Retain profile ownership, Continue checkpoint loading and guarded navigation.
    public override void _Ready()
    {
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        if (ProfileStore.Selected == null) { MenuNavigation.ReturnToBoot(this); return; }

        _errorDialog = new AcceptDialog { Title = "Could not launch" };
        AddChild(_errorDialog);
        UIButtonFactory.Apply(_errorDialog.GetOkButton(), UIButtonStyle.Action);

        BuildCampaignLayout(out Button resume, out Button firstMission);
        if (resume.Disabled) firstMission?.GrabFocus();
        else resume.GrabFocus();
    }

    // =========================================================
    // Route both campaign missions and menu actions through the existing guard.
    private void Navigate(string scene)
    {
        try { MenuNavigation.Open(this, scene); }
        catch (Exception error) { ShowError(error.Message); }
    }

    private void ShowError(string message)
    {
        _errorDialog.DialogText = message;
        _errorDialog.PopupCentered(new Vector2I(460, 180));
    }

    // =========================================================
    // Only the existing selected-profile between-wave checkpoint can be continued.
    private void ContinueCheckpoint() => Callable.From(() =>
    {
        try { CampaignSession.LoadCurrent(this); }
        catch (Exception error) { ShowError(error.Message); }
    }).CallDeferred();
}
