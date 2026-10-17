// Main menu: player identity, campaign gateway and safe return to profile picker.
using Godot;
using System;

public partial class MainMenu : Control
{
    private AcceptDialog _errorDialog;
    #region Inspector
    [ExportGroup("Layout")]
    [Export(PropertyHint.Range, "300,900,10")] public float PanelWidth { get; set; } = 560f;
    [ExportGroup("Development")]
    [Export] public bool ShowProgressionDebugGrant { get; set; } = false;
    #endregion

    #region Lifecycle
    // =========================================================
    // Loading this scene directly without a selected profile returns to Boot.
    public override void _Ready()
    {
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        if (ProfileStore.Selected == null) { MenuNavigation.ReturnToBoot(this); return; }
        VBoxContainer content = MenuLayout.Create(this, "TOWER DEFENSE",
            "COMMAND INTERFACE  /  ACTIVE PROFILE: " + ProfileStore.Selected.Name.ToUpperInvariant(), PanelWidth);
        content.AddChild(new PlayerProgressionPanel { Name = "PlayerProgression", AllowDebugGrant = ShowProgressionDebugGrant });
        Button campaign = UIButtonFactory.Create("CAMPAIGN", () => Navigate(MenuNavigation.CampaignScene),
            UIButtonStyle.Menu, 64f);
        content.AddChild(campaign);
        content.AddChild(UIButtonFactory.Create("CHANGE PROFILE", ChangeProfile, UIButtonStyle.Action));
        content.AddChild(UIButtonFactory.Create("EXIT GAME", () => GetTree().Quit(), UIButtonStyle.Compact, 38f));
        _errorDialog = new AcceptDialog { Title = "Menu error" };
        AddChild(_errorDialog);
        UIButtonFactory.Apply(_errorDialog.GetOkButton(), UIButtonStyle.Action);
        campaign.GrabFocus();
    }
    #endregion

    #region Routing
    // =========================================================
    // Keep scene-navigation errors visible without pretending a campaign was launched.
    private void Navigate(string scene)
    {
        try { MenuNavigation.Open(this, scene); }
        catch (Exception error) { ShowError(error.Message); }
    }

    // =========================================================
    // Changing identity never deletes a profile or touches campaign state.
    private void ChangeProfile()
    {
        PlayerProfile previous = ProfileStore.Selected;
        ProfileStore.ClearSelection();
        try { MenuNavigation.Open(this, MenuNavigation.BootScene); }
        catch (Exception error)
        {
            if (previous != null) ProfileStore.Select(previous.Id);
            ShowError(error.Message);
        }
    }

    // =========================================================
    // Report missing scenes and routing errors without hiding the current screen.
    private void ShowError(string message)
    {
        _errorDialog.DialogText = message;
        _errorDialog.PopupCentered(new Vector2I(460, 180));
    }
    #endregion
}
