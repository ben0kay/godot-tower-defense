// Self-contained, debug-build performance readout; reads existing counters without changing gameplay.
// Cached services, allocation-free frame sampling and throttled text updates; F4 remains available while paused.
using Godot;

public partial class PerformanceOverlay : CanvasLayer
{
    #region Inspector and State
    [ExportGroup("Visibility")]
    [Export] public bool DebugBuildsOnly { get; set; } = true;
    [Export] public bool ShowOnStart { get; set; } = true;
    [Export] public Key ToggleKey { get; set; } = Key.F4;
    [ExportGroup("Display")]
    [Export(PropertyHint.Range, "0.1,2,0.05")] public double RefreshSeconds { get; set; } = .25;
    [Export(PropertyHint.Range, "1,10,0.5")] public double PeakWindowSeconds { get; set; } = 2;
    [Export] public float EdgeMargin { get; set; } = 12;
    [ExportGroup("Pressure Thresholds")]
    [Export] public float FrameWarningMs { get; set; } = 22;
    [Export] public float FrameCriticalMs { get; set; } = 33;
    [Export] public int QueueWarning { get; set; } = 32;
    [Export] public int QueueCritical { get; set; } = 128;
    private PanelContainer _panel;
    private readonly Label[] _rows = new Label[21];
    private PathRequestManager _paths;
    private EnemyRegistry _enemies;
    private Node _projectiles;
    private Control _topBar, _bottomArea;
    private Node _towers;
    private EnemyBulletSystem _hostile;
    private SubsystemProfiler _profiler;
    private bool _profileRequested;
    [ExportGroup("Subsystem Profiling (debug only)")]
    [Export] public bool ProfileOnStart { get; set; } = false;
    [Export(PropertyHint.Range, "30,1200,30")] public int ProfileWindowTicks { get; set; } = 300;
    [Export(PropertyHint.Range, "1,60,1")] public int ProfileEveryTicks { get; set; } = 1;
    [Export] public string BuildLabel { get; set; } = "PF2 development build; record commit SHA when comparing";
    public SubsystemProfiler Profiler => _profiler;
    private double _refresh, _peakAge, _frameTotal, _framePeak, _pathPeak, _astarPeak, _validationPeak;
    private int _frames, _queryPeak;
    private ulong _lastFrameUsec;
    private bool _allowed, _headless;
    private static readonly Color Normal = new("#9ce7c6"), Warning = new("#ffd06b"), Critical = new("#ff7979");
    #endregion

    #region Lifecycle and Input
    // =========================================================
    // One instance belongs to each battlefield; release exports allocate no UI by default.
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; Layer = 20; ProcessPhysicsPriority = int.MaxValue;
        _allowed = !DebugBuildsOnly || OS.IsDebugBuild();
        SetProcess(false); SetPhysicsProcess(false); SetProcessInput(_allowed);
        if (!_allowed) { Hide(); return; }
        _headless = DisplayServer.GetName() == "headless";
        BuildPanel();
        Callable.From(ConnectServices).CallDeferred();
    }

    // =========================================================
    // Resolve known battlefield paths once, after sibling world/HUD initialization; missing services show N/A.
    private void ConnectServices()
    {
        if (!IsInsideTree()) return;
        Node battle = GetParent();
        _paths = battle.GetNodeOrNull<PathRequestManager>("World/EnemySystems/Paths");
        _enemies = battle.GetNodeOrNull<EnemyRegistry>("World/EnemySystems/Registry");
        _projectiles = battle.GetNodeOrNull<Node>("World/Projectiles");
        _topBar = battle.GetNodeOrNull<Control>("UI/GameplayHUD/TopBar");
        _bottomArea = battle.GetNodeOrNull<Control>("UI/GameplayHUD/BottomArea");
        _towers = battle.GetNodeOrNull<Node>("World/Towers");
        _hostile = battle.GetNodeOrNull<EnemyBulletSystem>("World/EnemyBullets");
        _profiler = new SubsystemProfiler { Name = "SubsystemProfiler" }; AddChild(_profiler);
        _profiler.Initialize(battle.GetNodeOrNull<Node>("World"), ProfileWindowTicks);
        _profiler.SampleEveryTicks = ProfileEveryTicks; _profileRequested = ProfileOnStart;
        SetOverlayVisible(ShowOnStart);
    }

    // =========================================================
    // F4 is independent of focused controls and pause/results; repeat events cannot flicker visibility.
    public override void _Input(InputEvent input)
    {
        if (!_allowed || input is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.Keycode == ToggleKey && key.ShiftPressed) SetProfiling(!(_profiler?.Running ?? false));
        else if (key.Keycode == ToggleKey) SetOverlayVisible(!Visible);
        else if (key.Keycode == Key.F5) DumpProfile();
        else return;
        GetViewport().SetInputAsHandled();
    }

    // =========================================================
    // Hidden panels stop all sampling and formatting; reopening starts a fresh observation window.
    public void SetOverlayVisible(bool visible)
    {
        visible &= _allowed;
        Visible = visible; SetProcess(visible); SetPhysicsProcess(visible);
        _lastFrameUsec = 0; _refresh = _peakAge = _frameTotal = 0; _frames = 0;
        ResetPeaks();
        if (!visible) _profiler?.Stop();
        else if (_profileRequested) _profiler?.Start();
        if (visible && _panel != null) RefreshDisplay();
    }
    #endregion

    #region Profiling Controls
    // =========================================================
    // Explicit profiling is independent of ordinary F4 visibility; hidden captures always stop.
    public void SetProfiling(bool enabled)
    {
        if (!_allowed || _profiler == null) return;
        if (enabled && !Visible) SetOverlayVisible(true);
        _profileRequested = enabled; _profiler.SampleEveryTicks = ProfileEveryTicks;
        if (enabled) _profiler.Start(); else _profiler.Stop();
        RefreshDisplay();
    }
    // =========================================================
    // Capture only on demand; report workload and hardware alongside CPU observations, never infer GPU utilization.
    public void DumpProfile()
    {
        if (_profiler == null) return;
        var camera = GetParent().GetNodeOrNull<CameraController>("CameraRig");
        string context = $"build={BuildLabel}; battlefield={GetParent().SceneFilePath}; level={_paths?.Navigation.Grid.Level.SceneFilePath}; enemies={_enemies?.Living.Count ?? 0}; towers={_towers?.GetChildCount() ?? 0}; projectileNodes={_projectiles?.GetChildCount() ?? 0}; hostileBullets={_hostile?.ActiveCount ?? 0}; zoom={camera?.ZoomLevel}; viewport={GetViewport().GetVisibleRect().Size}; maxFps={Engine.MaxFps}; fps={Engine.GetFramesPerSecond()}; processMs={Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000:0.000}; physicsMs={Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000:0.000}; headless={_headless}; CPU={OS.GetProcessorName()}; threads={OS.GetProcessorCount()}; OS={OS.GetName()}; pathQueue={_paths?.PendingCount}; sharedRawHits={_paths?.Navigation.RoutePool.RawHits}; sharedRefinedHits={_paths?.Navigation.RoutePool.RefinedHits}; sharedTemplateBuilds={_paths?.Navigation.RoutePool.RefinedBuilds}; segmentSafetyHits={_paths?.Navigation.SegmentSafety.Hits}; segmentSafetyBuilds={_paths?.Navigation.SegmentSafety.Builds}; segmentSafetyEvictions={_paths?.Navigation.SegmentSafety.Evictions}; segmentSafetyEntries={_paths?.Navigation.SegmentSafety.Count}; GPU timing=unmeasured";
        string report = _profiler.Report(context); GD.Print(report);
        DirAccess.MakeDirRecursiveAbsolute("user://Debug");
        string path = $"user://Debug/pf2-{System.DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.txt";
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null) GD.PushWarning("PF2 report could not be saved: " + FileAccess.GetOpenError());
        else { file.StoreString(report); GD.Print("PF2 report saved: " + ProjectSettings.GlobalizePath(path)); }
    }
    // =========================================================
    // Clear the active capture before the scene-owned UI disappears.
    public override void _ExitTree() => _profiler?.Stop();

    #endregion

    #region Sampling
    // =========================================================
    // Real elapsed frame spacing is independent of time scale; no strings/collections are made per frame.
    public override void _Process(double delta)
    {
        ulong now = Time.GetTicksUsec();
        if (_lastFrameUsec == 0) { _lastFrameUsec = now; return; }
        double seconds = (now - _lastFrameUsec) / 1000000.0; _lastFrameUsec = now;
        _peakAge += seconds;
        if (_peakAge >= Mathf.Max(1, PeakWindowSeconds)) { ResetPeaks(); _peakAge = 0; }
        double ms = seconds * 1000;
        _frameTotal += ms; _frames++; _framePeak = System.Math.Max(_framePeak, ms);
        _refresh += seconds;
        if (_refresh < Mathf.Max(.1, RefreshSeconds)) return;
        RefreshDisplay(); _refresh = _frameTotal = 0; _frames = 0;
    }

    // =========================================================
    // Read after the world path manager's physics tick so brief query/validation spikes are retained.
    public override void _PhysicsProcess(double delta)
    {
        _profiler?.EndTick();
        if (GetTree().Paused || !GodotObject.IsInstanceValid(_paths)) return;
        _queryPeak = System.Math.Max(_queryPeak, _paths.LastFrameCalculations);
        _pathPeak = System.Math.Max(_pathPeak, _paths.LastPathfindingMilliseconds);
        _astarPeak = System.Math.Max(_astarPeak, _paths.LastFrameAStarMilliseconds);
        _validationPeak = System.Math.Max(_validationPeak, _paths.LastValidationMilliseconds);
    }

    // =========================================================
    // Fixed observation windows avoid permanent session peaks dominating the live readout.
    private void ResetPeaks()
    { _framePeak = _pathPeak = _astarPeak = _validationPeak = 0; _queryPeak = 0; }

    // =========================================================
    // Navigation totals include A*; those numbers must never be added together as separate costs.
    private void RefreshDisplay()
    {
        bool paths = GodotObject.IsInstanceValid(_paths), paused = GetTree().Paused;
        double average = _frames > 0 ? _frameTotal / _frames : 0;
        Write(0, "PERFORMANCE · F4" + (paused ? " · PAUSED" : ""), new Color("#d9efff"));
        Write(1, $"FPS {Engine.GetFramesPerSecond()} · frame {average:0.0}ms · peak {_framePeak:0.0}", Pressure(average, FrameWarningMs, FrameCriticalMs));
        Write(2, $"Engine process {Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000:0.00}ms · physics {Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000:0.00}ms", Normal);
        string enemies = GodotObject.IsInstanceValid(_enemies) ? _enemies.Living.Count.ToString() : "N/A";
        string projectiles = GodotObject.IsInstanceValid(_projectiles) ? _projectiles.GetChildCount().ToString() : "N/A";
        Write(3, $"Enemies {enemies} · projectile nodes {projectiles} · towers {_towers?.GetChildCount() ?? 0} · hostile bullets {_hostile?.ActiveCount ?? 0}", Normal);
        double budget = paths ? _paths.Config?.MaxPathfindingMillisecondsPerFrame ?? 0 : 0;
        Write(4, paths ? $"Path peak {_pathPeak:0.00}ms · A* {_astarPeak:0.00}ms · queries {_queryPeak}" : "Navigation N/A", budget > 0 ? Pressure(_pathPeak, budget, budget * 2) : Normal);
        int pending = paths ? _paths.PendingCount : 0;
        Write(5, paths ? $"Path queue {pending} · audit queue {_paths.PendingValidationCount} · audit peak {_validationPeak:0.00}ms" : "Route validation N/A", Pressure(pending, QueueWarning, QueueCritical));
        Write(6, _headless ? "Rendering N/A (headless)" : $"Draw calls {Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame):0} · render objects {Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame):0}", Normal);
        Write(7, "Shift+F4: profile " + (_profiler?.Running == true ? "ON" : "OFF") + " · F5: save report", new Color("#9aafbd"));
        for (int i = 8; i < _rows.Length; i++) _rows[i].Visible = _profileRequested;
        if (_profileRequested && _profiler != null)
        {
            for (int i = 0; i < SubsystemProfiler.CategoryCount; i++)
            {
                var r = _profiler.Read((ProfileCategory)i);
                Write(8 + i, $"{SubsystemProfiler.Names[i]}  {r.Mean:0.00} / {r.P95:0.00} / {r.Peak:0.00}ms · {r.Calls} calls", Normal);
            }
            Write(19, $"Mean / p95 / peak per sampled tick · n={_profiler.Samples} · stride={ProfileEveryTicks}", Normal);
            Write(20, $"Nested rows overlap · observer {_profiler.BookkeepingMeanMs:0.000}ms/tick (partial)", Warning);
        }
        LayoutPanel();
    }
    #endregion

    #region Presentation
    // =========================================================
    // All Controls ignore mouse input so the panel cannot block build/selection clicks.
    private void BuildPanel()
    {
        _panel = new PanelContainer { Name = "Panel", MouseFilter = Control.MouseFilterEnum.Ignore };
        StyleBoxFlat style = new() { BgColor = new Color(.025f, .055f, .085f, .92f), BorderColor = new Color("#44647c"),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 7, ContentMarginBottom = 7 };
        _panel.AddThemeStyleboxOverride("panel", style); AddChild(_panel);
        VBoxContainer lines = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        lines.AddThemeConstantOverride("separation", 3); _panel.AddChild(lines);
        for (int i = 0; i < _rows.Length; i++)
        {
            _rows[i] = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
            _rows[i].AddThemeFontSizeOverride("font_size", 12); lines.AddChild(_rows[i]);
        }
    }

    // =========================================================
    // Stack below existing top-left diagnostics; fit remaining space above the build HUD on small windows.
    private void LayoutPanel()
    {
        float margin = Mathf.Max(4, EdgeMargin), top = margin;
        if (GodotObject.IsInstanceValid(_topBar) && _topBar.IsVisibleInTree()) top = Mathf.Max(top, _topBar.GetGlobalRect().End.Y + margin);
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        float bottom = viewport.Y - margin;
        if (GodotObject.IsInstanceValid(_bottomArea) && _bottomArea.IsVisibleInTree() && _bottomArea.Size.Y > 0)
            bottom = Mathf.Min(bottom, _bottomArea.GetGlobalRect().Position.Y - margin);
        Vector2 natural = _panel.GetCombinedMinimumSize();
        float scale = Mathf.Min(1, Mathf.Min((viewport.X - margin * 2) / Mathf.Max(1, natural.X), (bottom - top) / Mathf.Max(1, natural.Y)));
        // Extremely short windows have no usable slot; suppress drawing until space returns, keep F4 active.
        _panel.Visible = scale > .25f;
        _panel.Scale = Vector2.One * Mathf.Max(.25f, scale); _panel.Position = new(margin, top); _panel.Size = natural;
    }

    // =========================================================
    // Reuse labels; only the throttled refresh allocates formatted text.
    private void Write(int row, string text, Color color)
    { _rows[row].Text = text; _rows[row].AddThemeColorOverride("font_color", color); }

    // =========================================================
    // Thresholds are indicators, not automatic diagnoses or hardware-independent FPS promises.
    private static Color Pressure(double value, double warning, double critical)
        => value >= critical ? Critical : value >= warning ? Warning : Normal;
    #endregion
}
