// Profile-owned campaign hub: progression plus clear Levels and Research destinations.
using Godot;
using System;

public partial class CampaignMenu : Control
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
    // Keep campaign entry profile-owned while leaving direct Main.tscn testing possible.
    public override void _Ready()
    {
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        if (ProfileStore.Selected == null) { MenuNavigation.ReturnToBoot(this); return; }
        VBoxContainer content = MenuLayout.Create(this, "CAMPAIGN",
            "Commander " + ProfileStore.Selected.Name + "  /  Campaign command", PanelWidth);
        content.AddChild(new PlayerProgressionPanel { Name = "PlayerProgression", AllowDebugGrant = ShowProgressionDebugGrant });
        Button levels = UIButtonFactory.Create("LEVELS", () => Navigate(MenuNavigation.LevelsScene), UIButtonStyle.Menu, 64f);
        content.AddChild(levels);
        AddDescription(content, "Choose a mission and deploy your defenses.");
        content.AddChild(UIButtonFactory.Create("RESEARCH", () => Navigate(MenuNavigation.ResearchScene), UIButtonStyle.Menu, 64f));
        AddDescription(content, "Spend Research Points on permanent upgrades between missions.");
        content.AddChild(UIButtonFactory.Create("BACK TO MAIN MENU",
            () => Navigate(MenuNavigation.MainScene), UIButtonStyle.Compact));
        _errorDialog = new AcceptDialog { Title = "Could not launch" };
        AddChild(_errorDialog);
        UIButtonFactory.Apply(_errorDialog.GetOkButton(), UIButtonStyle.Action);
        Button resume = UIButtonFactory.Create("CONTINUE", ContinueCheckpoint, UIButtonStyle.Menu, 64f);
        content.AddChild(resume); content.MoveChild(resume, 1);
        try
        {
            var saved = CampaignSession.ReadCurrent();
            string unavailable = CampaignSession.LoadUnavailableReason(saved);
            resume.Disabled = unavailable.Length > 0;
            resume.TooltipText = resume.Disabled ? unavailable : "Resume " + saved.Checkpoint.LevelId + ". " + CampaignSession.RecoveryMessage;
        }
        catch (Exception error) { resume.Disabled = true; resume.TooltipText = error.Message; }
        if (resume.Disabled) levels.GrabFocus(); else resume.GrabFocus();
    }
    #endregion

    // =========================================================
    // Keep supporting copy consistent with the existing menu typography and responsive wrapping.
    private static void AddDescription(VBoxContainer content, string text)
    {
        Label label = UIButtonFactory.Label(text, 14); label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(label);
    }

    #region Routing
    // =========================================================
    // Both hub destinations use the same guarded scene navigation as the rest of the menus.
    private void Navigate(string scene)
    {
        try { MenuNavigation.Open(this, scene); }
        catch (Exception error)
        {
            _errorDialog.DialogText = error.Message;
            _errorDialog.PopupCentered(new Vector2I(460, 180));
        }
    }
    #endregion

    // =========================================================
    // Use the same profile-owned loader as Pause; the menu retains all navigation if preparation fails.
    private void ContinueCheckpoint() => Callable.From(() =>
    {
        try { CampaignSession.LoadCurrent(this); }
        catch (Exception error)
        { _errorDialog.DialogText = error.Message; _errorDialog.PopupCentered(new Vector2I(460, 180)); }
    }).CallDeferred();
}
