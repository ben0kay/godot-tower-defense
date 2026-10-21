// F2-only batch spawner: catalog choices and bounded portal delivery, isolated from authored wave ownership.
using Godot;
using System.Collections.Generic;

public partial class EnemySpawnMenu : CanvasLayer
{
    #region State
    [Export(PropertyHint.Range, "1,64,1")] public int MaxSpawnsPerTick { get; set; } = 16;
    public bool IsOpen => _panel?.Visible == true;
    public bool IsSpawning => _batches.Count > 0;
    public int ActiveBatchCount => _batches.Count;
    public int Remaining { get { int total = 0; foreach (Batch batch in _batches) total += batch.Remaining; return total; } }
    public int Spawned { get; private set; }
    public string Status => _status?.Text ?? "";
    private SpawnManager _spawns;
    private readonly RandomNumberGenerator _random = new();
    private readonly List<PackedScene> _scenes = new();
    private PanelContainer _panel;
    private OptionButton _enemy;
    private SpinBox _amount, _gap;
    private Label _status;
    private CheckBox _shielded, _stealthed;
    private VBoxContainer _batchRows;
    private readonly List<Batch> _batches = new();
    private int _cursor, _nextId;
    private string _lastFailure = "";
    private sealed class Batch
    {
        public int Id, Remaining, Spawned;
        public PackedScene Scene;
        public double Wait, Interval;
        public bool Shielded, Stealthed;
        public string Name;
        public HBoxContainer Row;
        public Label Progress;
    }
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
        HBoxContainer flags = new(); box.AddChild(flags);
        _shielded = new() { Name = "Shielded", Text = "Shielded", TooltipText = "Uses this enemy type’s configured shield capacity." }; flags.AddChild(_shielded);
        _stealthed = new() { Name = "Stealthed", Text = "Stealthed" }; flags.AddChild(_stealthed);
        box.AddChild(UIButtonFactory.Create("Spawn batch", StartSelected, UIButtonStyle.Compact, 36));
        ScrollContainer active = new() { CustomMinimumSize = new(0, 110), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        box.AddChild(active); _batchRows = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; active.AddChild(_batchRows);
        box.AddChild(UIButtonFactory.Create("Cancel all remaining spawns", CancelBatch, UIButtonStyle.Compact, 32));
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
    private void StartSelected() => StartBatch(_enemy.Selected, (int)_amount.Value, _gap.Value, _shielded.ButtonPressed, _stealthed.ButtonPressed);
    #endregion

    #region Delivery
    // =========================================================
    // Each click captures an independent batch; editing controls cannot alter previously accepted deliveries.
    public bool StartBatch(int enemyIndex, int amount, double gap, bool shielded = false, bool stealthed = false)
    {
        if (enemyIndex < 0 || enemyIndex >= _scenes.Count || amount < 1 || amount > 10000
            || !double.IsFinite(gap) || gap < 0 || gap > 60) return false;
        if (!IsSpawning) Spawned = 0;
        Batch batch = new() { Id = ++_nextId, Scene = _scenes[enemyIndex], Remaining = amount, Interval = gap,
            Shielded = shielded, Stealthed = stealthed, Name = _enemy.GetItemText(enemyIndex) };
        batch.Row = new(); _batchRows.AddChild(batch.Row);
        batch.Progress = UIButtonFactory.Label("", 13); batch.Progress.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        batch.Progress.AutowrapMode = TextServer.AutowrapMode.WordSmart; batch.Row.AddChild(batch.Progress);
        batch.Row.AddChild(UIButtonFactory.Create("Cancel", () => CancelBatch(batch.Id), UIButtonStyle.Compact, 28));
        _batches.Add(batch); _lastFailure = ""; UpdateBatch(batch); SetPhysicsProcess(true); UpdateStatus(); return true;
    }
    // =========================================================
    // Independent timers share one global delivery budget and rotate fairly; positive gaps never catch up in bursts.
    public override void _PhysicsProcess(double delta)
    {
        if (!IsSpawning || GetTree().Paused || !double.IsFinite(delta) || delta <= 0) return;
        if (!GodotObject.IsInstanceValid(_spawns) || _spawns.IsQueuedForDeletion()) { CancelBatch(); return; }
        foreach (Batch batch in _batches) batch.Wait = System.Math.Max(0, batch.Wait - delta);
        int budget = Mathf.Clamp(MaxSpawnsPerTick, 1, 64), skipped = 0; bool changed = false;
        while (_batches.Count > 0 && budget > 0 && skipped < _batches.Count)
        {
            _cursor %= _batches.Count; Batch batch = _batches[_cursor];
            if (batch.Wait > 0) { _cursor = (_cursor + 1) % _batches.Count; skipped++; continue; }
            skipped = 0; budget--; changed = true;
            if (_spawns.SpawnRandomPortal(batch.Scene, _random, batch.Shielded, batch.Stealthed) == null)
            {
                _lastFailure = $"Batch #{batch.Id} stopped: {_spawns.LastMessage}";
                RemoveBatch(_cursor); continue;
            }
            batch.Remaining--; batch.Spawned++; Spawned++; batch.Wait = batch.Interval;
            if (batch.Remaining == 0) RemoveBatch(_cursor);
            else { UpdateBatch(batch); _cursor = (_cursor + 1) % _batches.Count; }
        }
        if (changed) UpdateStatus();
        if (!IsSpawning) SetPhysicsProcess(false);
    }
    // =========================================================
    // A row cancels only its own pending deliveries; other batches and existing ships remain independent.
    public void CancelBatch(int id)
    {
        int index = _batches.FindIndex(batch => batch.Id == id); if (index < 0) return;
        RemoveBatch(index); if (!IsSpawning) SetPhysicsProcess(false); UpdateStatus();
    }
    // =========================================================
    // Disable/unload callers and the explicit Cancel All action stop all pending work.
    public void CancelBatch()
    {
        foreach (Batch batch in _batches) batch.Row.QueueFree();
        _batches.Clear(); _cursor = 0; SetPhysicsProcess(false); UpdateStatus();
    }
    // =========================================================
    // Adjust the rotating cursor when any row retires; no completed row/scene reference is retained.
    private void RemoveBatch(int index)
    {
        _batches[index].Row.QueueFree(); _batches.RemoveAt(index);
        if (index < _cursor) _cursor--; if (_batches.Count == 0) _cursor = 0; else _cursor %= _batches.Count;
    }
    // =========================================================
    // Per-batch status shows captured options and progresses only on accepted deliveries.
    private static void UpdateBatch(Batch batch) => batch.Progress.Text =
        $"#{batch.Id} {batch.Name} · {batch.Spawned} spawned / {batch.Remaining} left · {batch.Interval:0.##}s" +
        (batch.Shielded ? " · shielded" : "") + (batch.Stealthed ? " · stealthed" : "");
    // =========================================================
    // Aggregate status changes only with batch state, not in an extra per-frame UI loop.
    private void UpdateStatus()
    {
        if (_status != null) _status.Text = $"Active batches {ActiveBatchCount} · spawned {Spawned} · remaining {Remaining}" +
            (_lastFailure.Length > 0 ? "\n" + _lastFailure : "");
    }
    #endregion
}
