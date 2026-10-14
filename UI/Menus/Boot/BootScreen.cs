// Boot menu: create or select a locally persisted player profile before main-menu access.
using Godot;
using System;

public partial class BootScreen : Control
{
    #region Inspector
    [ExportGroup("Layout")]
    [Export(PropertyHint.Range, "300,900,10")] public float PanelWidth { get; set; } = 560f;
    #endregion

    #region UI
    private VBoxContainer _profileList;
    private Label _status, _nameError;
    private Button _newButton;
    private AcceptDialog _createDialog;
    private LineEdit _nameEntry;
    #endregion

    #region Lifecycle
    // =========================================================
    // Startup requires an explicit profile choice; no gameplay world is loaded here.
    public override void _Ready()
    {
        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        BuildUi();
        RefreshProfiles();
    }
    #endregion

    #region Layout
    // =========================================================
    // Create a scrollable list using the same factory as every later menu.
    private void BuildUi()
    {
        VBoxContainer content = MenuLayout.Create(this, "SELECT PROFILE", "Choose a local commander profile.", PanelWidth);
        ScrollContainer scroll = new()
        {
            CustomMinimumSize = new Vector2(0, 210),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        content.AddChild(scroll);
        _profileList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _profileList.AddThemeConstantOverride("separation", 7);
        scroll.AddChild(_profileList);

        _newButton = UIButtonFactory.Create("NEW PROFILE", ShowCreate);
        content.AddChild(_newButton);
        content.AddChild(UIButtonFactory.Create("EXIT", () => GetTree().Quit(), UIButtonStyle.Compact, 38));
        _status = UIButtonFactory.Label("", 13);
        _status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        content.AddChild(_status);

        _createDialog = new AcceptDialog { Title = "Create Profile", DialogHideOnOk = false, Exclusive = true };
        AddChild(_createDialog);
        VBoxContainer form = new();
        form.AddThemeConstantOverride("separation", 8);
        _createDialog.AddChild(form);
        form.AddChild(UIButtonFactory.Label("Commander name", 16));
        _nameEntry = new LineEdit { MaxLength = 32, PlaceholderText = "Name (1–32 characters)" };
        form.AddChild(_nameEntry);
        _nameError = UIButtonFactory.Label("", 13);
        _nameError.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        form.AddChild(_nameError);
        _createDialog.GetOkButton().Text = "CREATE";
        UIButtonFactory.Apply(_createDialog.GetOkButton(), UIButtonStyle.Action);
        UIButtonFactory.Apply(_createDialog.AddCancelButton("CANCEL"), UIButtonStyle.Compact);
        _createDialog.Confirmed += CreateProfile;
        _nameEntry.TextSubmitted += _ => CreateProfile();
    }
    #endregion

    #region Profile Actions
    // =========================================================
    // Display saved identities without hardcoded slots; errors and recovery stay visible.
    private void RefreshProfiles()
    {
        foreach (Node child in _profileList.GetChildren()) { _profileList.RemoveChild(child); child.QueueFree(); }
        try
        {
            foreach (PlayerProfile profile in ProfileStore.Profiles)
            {
                string id = profile.Id;
                _profileList.AddChild(UIButtonFactory.Create(profile.Name, () => Choose(id)));
            }
            if (ProfileStore.Profiles.Count == 0)
                _profileList.AddChild(UIButtonFactory.Label("No profiles yet. Create one to begin.", 14));
            _newButton.Disabled = false;
            _status.Text = ProfileStore.RecoveryMessage;
            _newButton.GrabFocus();
        }
        catch (Exception error)
        {
            _status.Text = error.Message;
            _newButton.Disabled = true;
        }
    }

    // =========================================================
    // Only the selected local identity is carried into subsequent menu scenes.
    private void Choose(string id)
    {
        try { ProfileStore.Select(id); MenuNavigation.Open(this, MenuNavigation.MainScene); }
        catch (Exception error) { _status.Text = error.Message; }
    }

    // =========================================================
    // A modal prevents accidental profile creation from an empty name.
    private void ShowCreate()
    {
        _nameEntry.Text = ""; _nameError.Text = "";
        _createDialog.PopupCentered(new Vector2I(460, 210));
        _nameEntry.GrabFocus();
    }

    // =========================================================
    // Persist first; then navigate. A failed write keeps the form available for retry.
    private void CreateProfile()
    {
        try
        {
            ProfileStore.Create(_nameEntry.Text);
            _createDialog.Hide();
            MenuNavigation.Open(this, MenuNavigation.MainScene);
        }
        catch (Exception error) { _nameError.Text = error.Message; }
    }
    #endregion
}
