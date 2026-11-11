// Battlefield-only command router and directory for the existing development tools.
using Godot;

public partial class DeveloperHub : CanvasLayer
{
    private HBoxContainer _panel;
    private Control _visualRoot;
    private ProjectileRenderSettings _projectileSettings;
    private EnemyRegistry _enemies;
    private Node2D _effects;
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
        _enemies = battle.GetNodeOrNull<EnemyRegistry>("World/EnemySystems/Registry");
        _effects = battle.GetNodeOrNull<Node2D>("World/Effects");
        _performance = battle.GetNodeOrNull<PerformanceOverlay>("PerformanceOverlay");
        _alerts = battle.GetNodeOrNull<AlertPreview>("UI/GameplayHUD/AlertPreview");
        _tools = battle.GetNodeOrNull<WorldGrid>("World/Grid")?.Level?.GetNodeOrNull<LevelDebugController>("Debug");
        _session = battle.GetNodeOrNull<LevelSessionController>("LevelSession");
        _catalog = GD.Load<TowerCatalog>("res://TOWERS/DefaultTowerCatalog.tres");
        _theme = GD.Load<UIMasterConfig>("res://UI/DefaultUIConfig.tres") ?? new UIMasterConfig();
        _projectileSettings = battle.GetNodeOrNull<FriendlyProjectileSystem>("World/Projectiles")?.RenderSettings;
        BuildPanel();
        BuildVisualPanel();
        SetProcessInput(true); SetProcessUnhandledKeyInput(true);
    }

    private void BuildPanel()
    {
        _panel = new HBoxContainer { Position = new Vector2(18, 76), MouseFilter = Control.MouseFilterEnum.Ignore };
        _panel.AddThemeConstantOverride("separation", 10);
        AddChild(_panel);
        var list = AddPanel(_panel, 360);
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

    // =========================================================
    // Shared styling for the F1 directory and the independent persistent visual controls.
    private VBoxContainer AddPanel(Node parent, float minWidth)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(minWidth, 0),
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
        };
        panel.AddThemeStyleboxOverride("panel", UIButtonFactory.Box(_theme.PanelColor, _theme.AccentColor));
        parent.AddChild(panel);
        var list = new VBoxContainer();
        list.AddThemeConstantOverride("separation", 6);
        panel.AddChild(list);
        return list;
    }

    // =========================================================
    // Anchors follow viewport resizing without a process loop. Empty screen space stays click-through.
    private void BuildVisualPanel()
    {
        _visualRoot = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_visualRoot);
        _visualRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var list = AddPanel(_visualRoot, 260);
        var panel = (PanelContainer)list.GetParent();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterRight);
        panel.OffsetLeft = -274;
        panel.OffsetRight = -14;
        panel.GrowHorizontal = Control.GrowDirection.Begin;
        panel.GrowVertical = Control.GrowDirection.Both;
        list.AddChild(UIButtonFactory.Label("VISUAL DEBUG", 20, _theme));
        if (_enemies != null)
        {
            AddToggle(list, "Enemy Explosion FX", _enemies.ExplosionFxEnabled, SetExplosionFxEnabled);
            AddToggle(list, "Enemy Health Bars", _enemies.HealthBarsEnabled, _enemies.SetHealthBarsEnabled);
        }
        if (_projectileSettings != null)
            AddToggle(list, "Textured Tracers", _projectileSettings.TexturedTracers, _projectileSettings.SetTexturedTracers);
    }

    private static void AddToggle(VBoxContainer list, string text, bool enabled, System.Action<bool> apply)
    {
        var toggle = new CheckButton { Text = text, ButtonPressed = enabled };
        toggle.Toggled += pressed => apply(pressed);
        list.AddChild(toggle);
    }

    // =========================================================
    // Clear active enemy explosions once when disabled. Future kills skip spawning them.
    private void SetExplosionFxEnabled(bool enabled)
    {
        _enemies.ExplosionFxEnabled = enabled;
        if (enabled || _effects == null) return;
        foreach (Node effect in _effects.GetChildren())
        {
            if (effect.IsInGroup(Enemy.ExplosionFxGroup)) effect.QueueFree();
        }
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
        _visualRoot?.Hide();
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
