// Battlefield-only command router and directory for the existing development tools.
using Godot;

public partial class DeveloperHub : CanvasLayer
{
    private PanelContainer _panel;
    private Button _spawnButton, _lookupButton, _routeButton, _profileButton;
    private PerformanceOverlay _performance;
    private AlertPreview _alerts;
    private LevelDebugController _tools;
    private LevelSessionController _session;
    private TowerCatalog _catalog;
    private BuildingLookupPanel _lookup;
    private UIMasterConfig _theme;

    public override void _Ready()
    {
        SetProcessInput(false); SetProcessUnhandledKeyInput(false);
        if (!OS.IsDebugBuild()) return;
        ProcessMode = ProcessModeEnum.Always;
        Layer = 40;
        Node battle = GetParent();
        _performance = battle.GetNodeOrNull<PerformanceOverlay>("PerformanceOverlay");
        _alerts = battle.GetNodeOrNull<AlertPreview>("UI/GameplayHUD/AlertPreview");
        _tools = battle.GetNodeOrNull<WorldGrid>("World/Grid")?.Level?.GetNodeOrNull<LevelDebugController>("Debug");
        _session = battle.GetNodeOrNull<LevelSessionController>("LevelSession");
        _catalog = GD.Load<TowerCatalog>("res://TOWERS/DefaultTowerCatalog.tres");
        _theme = GD.Load<UIMasterConfig>("res://UI/DefaultUIConfig.tres") ?? new UIMasterConfig();
        BuildPanel();
        SetProcessInput(true); SetProcessUnhandledKeyInput(true);
    }

    private void BuildPanel()
    {
        _panel = new PanelContainer { Position = new Vector2(18, 76), CustomMinimumSize = new Vector2(360, 0) };
        _panel.AddThemeStyleboxOverride("panel", UIButtonFactory.Box(_theme.PanelColor, _theme.AccentColor));
        AddChild(_panel);
        var list = new VBoxContainer();
        list.AddThemeConstantOverride("separation", 6);
        _panel.AddChild(list);
        list.AddChild(UIButtonFactory.Label("DEVELOPER TOOLS · F1", 20, _theme));
        _spawnButton = AddButton(list, "1   Enemy Spawner", () => OpenTool(1));
        _lookupButton = AddButton(list, "2   Building Lookup", () => OpenTool(2));
        _routeButton = AddButton(list, "3   Route Preview", () => OpenTool(3));
        list.AddChild(new HSeparator());
        AddButton(list, "Performance Overlay (F2)", ToggleOverlay);
        _profileButton = AddButton(list, "Start Profiling (Shift+F2)", ToggleProfiling);
        AddButton(list, "Save Performance Report (F3)", () => _performance?.DumpProfile());
        AddButton(list, "Notification Samples (F4)", () => _alerts?.CycleSample());
        AddButton(list, "Close · Esc", Dismiss);
        _panel.Hide();
    }

    private Button AddButton(VBoxContainer list, string text, System.Action action)
    {
        var button = UIButtonFactory.Create(text, action, UIButtonStyle.Compact, 34, _theme);
        list.AddChild(button);
        return button;
    }

    // =========================================================
    // Diagnostic shortcuts work even before the hub is opened. Gameplay tools require an unpaused battlefield.
    public override void _Input(InputEvent input)
    {
        if (_panel == null || _session?.State != LevelSessionState.Playing ||
            input is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (input.IsActionPressed(GameInput.BackAction) && (_panel.Visible || ChildOpen))
        {
            Dismiss(); GetViewport().SetInputAsHandled(); return;
        }
        if (key.CtrlPressed || key.AltPressed || key.MetaPressed) return;
        switch (key.Keycode)
        {
            case Key.F1 when !key.ShiftPressed && !GetTree().Paused: ToggleHub(); break;
            case Key.F2 when key.ShiftPressed: ToggleProfiling(); break;
            case Key.F2: ToggleOverlay(); break;
            case Key.F3 when !key.ShiftPressed: _performance?.DumpProfile(); break;
            case Key.F4 when !key.ShiftPressed && !GetTree().Paused: _alerts?.CycleSample(); break;
            default: return;
        }
        GetViewport().SetInputAsHandled();
    }

    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (_panel?.Visible != true || GetTree().Paused || _session?.State != LevelSessionState.Playing ||
            input is not InputEventKey { Pressed: true, Echo: false, ShiftPressed: false, CtrlPressed: false, AltPressed: false, MetaPressed: false } key ||
            GetViewport().GuiGetFocusOwner() is not Control focus || !_panel.IsAncestorOf(focus)) return;
        int tool = key.Keycode switch { Key.Key1 => 1, Key.Key2 => 2, Key.Key3 => 3, _ => 0 };
        if (tool == 0) return;
        OpenTool(tool); GetViewport().SetInputAsHandled();
    }

    private bool ChildOpen => _tools?.SpawnMenu?.IsOpen == true || _tools?.RoutePreview?.IsOpen == true || _lookup?.Visible == true;

    private void ToggleHub()
    {
        if (_panel.Visible) { Dismiss(); return; }
        Dismiss();
        _spawnButton.Disabled = _tools?.Enabled != true || _tools.SpawnMenu == null;
        _routeButton.Disabled = _tools?.Enabled != true || _tools.RoutePreview == null;
        _lookupButton.Disabled = _catalog == null;
        UpdateProfilingCaption(); _panel.Show();
        if (!_spawnButton.Disabled) _spawnButton.GrabFocus();
        else if (!_lookupButton.Disabled) _lookupButton.GrabFocus();
        else _profileButton.GrabFocus();
    }

    private void OpenTool(int tool)
    {
        if (GetTree().Paused || _session?.State != LevelSessionState.Playing) return;
        switch (tool)
        {
            case 1 when !_spawnButton.Disabled: _panel.Hide(); _tools.SpawnMenu.Toggle(); break;
            case 2 when !_lookupButton.Disabled:
                _panel.Hide();
                if (_lookup == null) { _lookup = new BuildingLookupPanel(); AddChild(_lookup); _lookup.Initialize(_catalog, _theme); }
                _lookup.Open(); break;
            case 3 when !_routeButton.Disabled: _panel.Hide(); _tools.RoutePreview.Toggle(); break;
        }
    }

    private void ToggleOverlay() { _performance?.ToggleOverlay(); UpdateProfilingCaption(); }
    private void ToggleProfiling() { _performance?.ToggleProfiling(); UpdateProfilingCaption(); }
    private void UpdateProfilingCaption() =>
        _profileButton.Text = (_performance?.Profiling == true ? "Stop" : "Start") + " Profiling (Shift+F2)";

    internal void StopForOutcome()
    {
        Dismiss();
        _tools?.SpawnMenu?.CancelBatch();
    }

    private void Dismiss()
    {
        _panel?.Hide();
        if (_tools?.SpawnMenu?.IsOpen == true) _tools.SpawnMenu.Close();
        _tools?.RoutePreview?.Close();
        _lookup?.Close();
    }
}
