// Campaign-owned mission selection; launch existing battlefields without inventing unlock/save state.
using Godot;
using System;

public partial class LevelSelectionMenu : Control
{
    private AcceptDialog _errorDialog;
    #region Inspector
    [ExportGroup("Layout")]
    [Export(PropertyHint.Range, "300,900,10")] public float PanelWidth { get; set; } = 660f;
    #endregion

    #region Lifecycle
    // =========================================================
    // Keep campaign entry profile-owned while leaving direct Main.tscn testing possible.
    public override void _Ready()
    {
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        if (ProfileStore.Selected == null) { MenuNavigation.ReturnToBoot(this); return; }
        VBoxContainer content = MenuLayout.Create(this, "LEVELS",
            "Commander " + ProfileStore.Selected.Name + "  /  Choose a mission", PanelWidth);
        ScrollContainer scroll = new() { CustomMinimumSize = new Vector2(0, 280),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        content.AddChild(scroll);
        VBoxContainer missions = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        missions.AddThemeConstantOverride("separation", 12); scroll.AddChild(missions);
        Button play = null;
        foreach (MissionDefinition mission in PlayableLevelCatalog.Entries)
        {
            missions.AddChild(UIButtonFactory.Label($"LEVEL {mission.Number:000}  —  {mission.Title}", 17));
            Label details = UIButtonFactory.Label(mission.Description, 14);
            details.AutowrapMode = TextServer.AutowrapMode.WordSmart; missions.AddChild(details);
            Button button = UIButtonFactory.Create($"PLAY LEVEL {mission.Number:000}", () => Navigate(mission.LaunchPath), UIButtonStyle.Action, 64f);
            missions.AddChild(button); play ??= button;
        }
        content.AddChild(UIButtonFactory.Create("BACK TO CAMPAIGN",
            () => Navigate(MenuNavigation.CampaignScene), UIButtonStyle.Compact));
        _errorDialog = new AcceptDialog { Title = "Could not launch" };
        AddChild(_errorDialog);
        UIButtonFactory.Apply(_errorDialog.GetOkButton(), UIButtonStyle.Action);
        play?.GrabFocus();
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
