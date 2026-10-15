// Simple campaign gateway for two standalone authored battlefields; saves/unlocks remain later.
using Godot;
using System;

public partial class CampaignMenu : Control
{
    private AcceptDialog _errorDialog;
    #region Inspector
    [ExportGroup("Layout")]
    [Export(PropertyHint.Range, "300,900,10")] public float PanelWidth { get; set; } = 560f;
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
            "Commander " + ProfileStore.Selected.Name + "  /  Select a deployment", PanelWidth);
        content.AddChild(UIButtonFactory.Label("LEVEL 001  —  DEFEND THE CITADEL", 17));
        Label details = UIButtonFactory.Label("Initial combat test map with compact routes.", 14);
        details.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(details);
        Button play = UIButtonFactory.Create("PLAY LEVEL 001", () => Navigate(MenuNavigation.Level001Scene),
            UIButtonStyle.Action, 64f);
        content.AddChild(play);
        content.AddChild(UIButtonFactory.Label("LEVEL 002  —  THE FIVE GATES", 17));
        Label mazeDetails = UIButtonFactory.Label("Five western portals. Open staging ground and a sprawling eastern maze with multiple paths.", 14);
        mazeDetails.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(mazeDetails);
        content.AddChild(UIButtonFactory.Create("PLAY LEVEL 002", () => Navigate(MenuNavigation.Level002Scene),
            UIButtonStyle.Action, 64f));
        content.AddChild(UIButtonFactory.Create("BACK TO MAIN MENU",
            () => Navigate(MenuNavigation.MainScene), UIButtonStyle.Compact));
        _errorDialog = new AcceptDialog { Title = "Could not launch" };
        AddChild(_errorDialog);
        UIButtonFactory.Apply(_errorDialog.GetOkButton(), UIButtonStyle.Action);
        play.GrabFocus();
    }
    #endregion

    #region Routing
    // =========================================================
    // Both authored battlefields use identical shared navigation, combat, UI and wallet systems.
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
}
