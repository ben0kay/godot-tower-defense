// F2-only batch spawner: catalog choices and bounded portal delivery, isolated from authored wave ownership.
using Godot;
using System.Collections.Generic;

public partial class EnemySpawnMenu : CanvasLayer
{
    #region State
    [Export(PropertyHint.Range, "1,64,1")] public int MaxSpawnsPerTick { get; set; } = 16;
    public bool IsOpen => _panel?.Visible == true;
    public bool IsSpawning => Remaining > 0;
    public int Remaining { get; private set; }
    public int Spawned { get; private set; }
    public string Status => _status?.Text ?? "";
    private SpawnManager _spawns;
    private readonly RandomNumberGenerator _random = new();
    private readonly List<PackedScene> _scenes = new();
    private PanelContainer _panel;
    private OptionButton _enemy;
    private SpinBox _amount, _gap;
    private Label _status;
    private Button _start;
    private PackedScene _batch;
    private double _wait, _interval;
    #endregion

    #region Menu
    // =========================================================
    // Build once per live level; hidden and idle tools do no physics work.
    public void Initialize(SpawnManager spawns)
    {
        _spawns = spawns; _random.Randomize(); Layer = 30;
        Control root = new() { Name = "Root", MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(root); root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        CenterContainer center = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.AddChild(center); center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _panel = new() { Name = "Panel", CustomMinimumSize = new(440, 0), MouseFilter = Control.MouseFilterEnum.Stop };
        center.AddChild(_panel);
        UIMasterConfig theme = GD.Load<UIMasterConfig>("res://UI/DefaultUIConfig.tres");
        _panel.AddThemeStyleboxOverride("panel", UIButtonFactory.Box(theme.PanelColor, theme.AccentColor));
        VBoxContainer box = new(); _panel.AddChild(box);
        box.AddChild(UIButtonFactory.Label("ENEMY SPAWNER · F2", 20));
        box.AddChild(UIButtonFactory.Label("Random valid portal per ship", 14));
        _enemy = new() { Name = "Enemy", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; box.AddChild(_enemy);
        foreach (var pair in spawns.Catalog.ToSceneMap())
        { _scenes.Add(pair.Value); _enemy.AddItem(spawns.Catalog.DefinitionFor(pair.Key).DisplayName); }
        _amount = Number(box, "Amount", "Amount", 1, 10000, 1, 10);
        _gap = Number(box, "Spawn gap (seconds)", "Gap", 0, 60, .01, .1);
        _start = UIButtonFactory.Create("Spawn batch", StartSelected, UIButtonStyle.Compact, 36); box.AddChild(_start);
        box.AddChild(UIButtonFactory.Create("Cancel remaining spawns", CancelBatch, UIButtonStyle.Compact, 32));
        box.AddChild(UIButtonFactory.Create("Close · F2 / Escape", Close, UIButtonStyle.Compact, 32));
        _status = UIButtonFactory.Label("Choose an enemy and batch settings.", 14); _status.AutowrapMode = TextServer.AutowrapMode.WordSmart; box.AddChild(_status);
        Close(); SetPhysicsProcess(false);
    }
    // =========================================================
    // Labeled inputs retain values when reopening and use seconds directly.
    private static SpinBox Number(VBoxContainer box, string text, string name, double min, double max, double step, double initial)
    {
        HBoxContainer row = new(); box.AddChild(row); row.AddChild(UIButtonFactory.Label(text, 14));
        SpinBox input = new() { Name = name, MinValue = min, MaxValue = max, Step = step, Value = initial, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(input); return input;
    }
    // =========================================================
    // Opening the menu does not pause simulation or create ships.
    public void Toggle() { if (IsOpen) Close(); else { _panel.Show(); _enemy.GrabFocus(); } }
    // =========================================================
    // Closing leaves an accepted batch running; cancellation is explicit.
    public void Close() => _panel?.Hide();
    // =========================================================
    // Capture controls once so editing inputs cannot mutate an in-flight batch.
    private void StartSelected() => StartBatch(_enemy.Selected, (int)_amount.Value, _gap.Value);
    #endregion

    #region Delivery
    // =========================================================
    // One batch at a time; reject invalid inputs instead of replacing or silently truncating existing work.
    public bool StartBatch(int enemyIndex, int amount, double gap)
    {
        if (IsSpawning || enemyIndex < 0 || enemyIndex >= _scenes.Count || amount < 1 || amount > 10000
            || !double.IsFinite(gap) || gap < 0 || gap > 60) return false;
        _batch = _scenes[enemyIndex]; Remaining = amount; Spawned = 0; _interval = gap; _wait = 0;
        _start.Disabled = true; SetPhysicsProcess(true); UpdateStatus(); return true;
    }
    // =========================================================
    // Zero-gap batches are spread over bounded ticks; positive gaps never burst to catch up after a hitch.
    public override void _PhysicsProcess(double delta)
    {
        if (!IsSpawning || GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        _wait -= delta; if (_wait > 0) return;
        int budget = _interval == 0 ? Mathf.Clamp(MaxSpawnsPerTick, 1, 64) : 1;
        for (int i = 0; i < budget && Remaining > 0; i++)
        {
            if (!GodotObject.IsInstanceValid(_spawns) || _spawns.IsQueuedForDeletion()) { CancelBatch(); return; }
            if (_spawns.SpawnRandomPortal(_batch, _random) == null)
            {
                string reason = _spawns.LastMessage; CancelBatch();
                _status.Text = $"Stopped after {Spawned}: {reason}"; return;
            }
            Remaining--; Spawned++;
        }
        _wait = _interval; UpdateStatus();
        if (Remaining == 0) { _batch = null; _start.Disabled = false; SetPhysicsProcess(false); }
    }
    // =========================================================
    // Cancellation removes only pending requests; existing enemies retain ordinary gameplay lifecycle.
    public void CancelBatch()
    { Remaining = 0; _batch = null; SetPhysicsProcess(false); if (_start != null) _start.Disabled = false; UpdateStatus(); }
    // =========================================================
    // Status changes only on delivery/cancellation, never in a separate UI frame loop.
    private void UpdateStatus() { if (_status != null) _status.Text = $"Spawned {Spawned} · remaining {Remaining}"; }
    #endregion
}
