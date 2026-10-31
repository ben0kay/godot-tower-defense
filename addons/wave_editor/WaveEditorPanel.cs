// Editor-only, event-driven authoring panel for editable WaveSetResource .tres files.
// No game-state changes or per-frame polling: all edits are explicit, saved by the user.
#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class WaveEditorPanel : PanelContainer
{
    #region Document and Selection
    private const string ExamplePath = "res://LEVELS/Level003/Level003Waves.tres";
    private WaveSetResource _source;
    private string _path = "";
    private int _waveIndex, _batchIndex, _enemyIndex;
    private bool _refreshing, _dirty;
    private Label _fileLabel, _status, _totals;
    private Button _saveButton;
    private ItemList _waves, _batches, _enemies;
    private VBoxContainer _waveFields, _batchFields, _enemyFields, _presentationFields, _compositionPreview;
    private SpinBox _waveNumber, _count, _interval, _after, _weight;
    private OptionButton _alert, _enemyId;
    private LineEdit _bannerTitle, _bannerDescription, _speaker, _narration, _voiceCue, _portal;
    private EnemyCatalog _enemyCatalog;
    private List<(string Id, string Name)> _enemyChoices = new();
    private CheckBox _shield, _stealth;
    private EditorFileDialog _openFile, _newFile;
    private ConfirmationDialog _discard, _confirmDelete;
    private Action _pendingDelete;
    private WaveResource Wave => _source != null && _waveIndex >= 0 && _waveIndex < _source.Waves.Count ? _source.Waves[_waveIndex] : null;
    private WaveBatchResource Batch => Wave != null && _batchIndex >= 0 && _batchIndex < Wave.Batches.Count ? Wave.Batches[_batchIndex] : null;
    private WaveEnemyResource Entry => Batch != null && _enemyIndex >= 0 && _enemyIndex < Batch.Enemies.Count ? Batch.Enemies[_enemyIndex] : null;
    #endregion

    #region Interface Construction
    // =========================================================
    // Build the interface once, with resizable wave/batch columns and a scrollable details pane.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        var margin = new MarginContainer();
        foreach (string edge in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride("margin_" + edge, 12);
        AddChild(margin);
        var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        root.AddThemeConstantOverride("separation", 10); margin.AddChild(root);

        var title = new HBoxContainer();
        root.AddChild(title);
        title.AddChild(new Label { Text = "WAVE EDITOR", ThemeTypeVariation = "HeaderSmall", CustomMinimumSize = new Vector2(120, 0) });
        _fileLabel = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill, ClipText = true };
        title.AddChild(_fileLabel);
        title.AddChild(Button("Open .tres", Open));
        title.AddChild(Button("New .tres", New));
        _saveButton = Button("Save", Save); title.AddChild(_saveButton);
        title.AddChild(Button("Reload", Reload));

        var split = new HSplitContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        root.AddChild(split);
        var wavePane = Pane("Waves", 205); split.AddChild(wavePane);
        _waves = CreateItemList(wavePane, index => { _waveIndex = index; _batchIndex = _enemyIndex = 0; Refresh(); });
        Buttons(wavePane, ("+ Wave", AddWave), ("Duplicate", DuplicateWave), ("Delete", DeleteWave));
        Buttons(wavePane, ("Move ↑", () => MoveWave(-1)), ("Move ↓", () => MoveWave(1)));
        var second = new HSplitContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        split.AddChild(second);
        var batchPane = Pane("Batches", 245); second.AddChild(batchPane);
        _batches = CreateItemList(batchPane, index => { _batchIndex = index; _enemyIndex = 0; Refresh(); });
        Buttons(batchPane, ("+ Batch", AddBatch), ("Duplicate", DuplicateBatch), ("Delete", DeleteBatch));
        Buttons(batchPane, ("Move ↑", () => MoveBatch(-1)), ("Move ↓", () => MoveBatch(1)));

        var detailsScroll = new ScrollContainer { CustomMinimumSize = new Vector2(345, 0),
            SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        second.AddChild(detailsScroll);
        var details = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        details.AddThemeConstantOverride("separation", 10);
        detailsScroll.AddChild(details);

        _waveFields = Pane("Selected wave", 0); details.AddChild(_waveFields);
        _waveNumber = Number(_waveFields, "Wave number", 1, 10000, 1, v => { Wave.Number = (int)v; RefreshComposition(); });
        _waveNumber.TooltipText = "Unique positive wave number. Waves play in the list order shown on the left; moving them does not renumber them.";

        _batchFields = Pane("Selected batch", 0); details.AddChild(_batchFields);
        _count = Number(_batchFields, "Total enemies", 1, 100000, 1, v => { Batch.Count = (int)v; RefreshComposition(); });
        _count.TooltipText = "Total ships across all enemy entries in this batch; weights divide this count. Does not add ships per entry.";
        _interval = Number(_batchFields, "Seconds between spawns", 0, 3600, .01, v => { Batch.Interval = v; RefreshComposition(); });
        _interval.TooltipText = "Seconds between SUCCESSFUL spawns. The first ship starts immediately. Failed/blocked spawns retry and may extend this duration.";
        _after = Number(_batchFields, "Delay after batch (seconds)", 0, 3600, .01, v => Batch.After = v);
        _after.TooltipText = "Seconds after the last successful spawn before the next batch. Does not wait for enemies to die. Ignored for the final batch.";
        _batchFields.AddChild(new Label { Text = "Enemy mix — weights are relative (e.g. 75 / 25).",
            AutowrapMode = TextServer.AutowrapMode.WordSmart });
        _enemies = CreateItemList(_batchFields, index => { _enemyIndex = index; Refresh(); });
        _enemies.CustomMinimumSize = new Vector2(0, 135);
        Buttons(_batchFields, ("+ Enemy", AddEnemy), ("Duplicate", DuplicateEnemy), ("Delete", DeleteEnemy));
        var preview = Pane("Exact composition preview", 0);
        _batchFields.AddChild(preview);
        _compositionPreview = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _compositionPreview.AddThemeConstantOverride("separation", 3);
        preview.AddChild(_compositionPreview);
        preview.TooltipText = "Guaranteed number of each entry in this batch. Counts use the real largest-remainder rule, then the game shuffles spawn order.";

        _enemyFields = Pane("Selected enemy entry", 0); details.AddChild(_enemyFields);
        var enemyPicker = new HBoxContainer();
        Field(_enemyFields, "Enemy type", enemyPicker);
        _enemyId = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = "Choose a registered enemy. Names come from DefaultEnemyCatalog.tres; stable IDs are saved." };
        enemyPicker.AddChild(_enemyId);
        enemyPicker.AddChild(Button("Refresh list", ReloadEnemyCatalog));
        _enemyId.ItemSelected += index =>
        {
            if (_refreshing || Entry == null || index < 0 || index >= _enemyChoices.Count) return;
            string id = _enemyChoices[(int)index].Id;
            if (Entry.EnemyId == id) return;
            Entry.EnemyId = id; Dirty(); Refresh();
        };
        _weight = Number(_enemyFields, "Weight", 0, 10000, .01, v => { Entry.Weight = v; RefreshComposition(); });
        _weight.TooltipText = "Relative share of this batch, not a ship count or an independent spawn chance. Example 75 and 25 with Count 20 means exactly 15 and 5, shuffled.";
        _portal = Text(_enemyFields, "Required portal (blank = any)", v => Entry.Portal = v);
        _portal.TooltipText = "Optional level-root-relative portal path (e.g. Portals/EnemyPortal1). Blank lets the spawn system pick a valid source.";
        _shield = Check(_enemyFields, "Spawn with shield", v => { Entry.Shield = v; RefreshComposition(); });
        _shield.TooltipText = "Start these ships with their configured shield full; this does not add shields to an enemy whose MaxShield is zero.";
        _stealth = Check(_enemyFields, "Spawn with stealth", v => { Entry.Stealth = v; RefreshComposition(); });
        _stealth.TooltipText = "Request starting stealth for this enemy entry. This flag is independent of the Shield flag.";
        _presentationFields = Pane("Wave presentation (optional)", 0); details.AddChild(_presentationFields);
        _alert = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (WaveAlertKind alert in Enum.GetValues<WaveAlertKind>()) _alert.AddItem(alert.ToString());
        Field(_presentationFields, "Alert", _alert);
        _alert.TooltipText = "Wave announcement style only: Standard, Heavy, Final, Boss or None. It does not modify enemy strength or contents.";
        _alert.ItemSelected += i => { if (!_refreshing && Wave != null) { Wave.Alert = (WaveAlertKind)i; Dirty(); } };
        _bannerTitle = Text(_presentationFields, "Banner title", v => Wave.BannerTitle = v);
        _bannerDescription = Text(_presentationFields, "Banner description", v => Wave.BannerDescription = v);
        _speaker = Text(_presentationFields, "Speaker", v => Wave.Speaker = v);
        _narration = Text(_presentationFields, "Narration", v => Wave.NarrationText = v);
        _voiceCue = Text(_presentationFields, "Voice cue ID", v => Wave.VoiceCueId = v);
        _bannerTitle.TooltipText = "Optional custom wave-start banner heading. Blank uses WAVE NN INCOMING.";
        _bannerDescription.TooltipText = "Optional secondary text below the wave-start banner.";
        _speaker.TooltipText = "Optional speaker name shown with the tactical narration.";
        _narration.TooltipText = "Optional text displayed by the tactical narration panel when the wave starts.";
        _voiceCue.TooltipText = "Optional registered voice cue ID for the wave-start alert. Does not create or record audio.";
        details.AddChild(new Label { Text = "Enemy names and IDs are read from DefaultEnemyCatalog.tres.\n" +
            "Wave order = list order; counts are per batch, not per enemy entry.", AutowrapMode = TextServer.AutowrapMode.WordSmart });

        var footer = new HBoxContainer(); root.AddChild(footer);
        _status = new Label { Text = "Choose a .tres wave set.", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _totals = new Label(); footer.AddChild(_status); footer.AddChild(_totals);
        SetupDialogs(); ReloadEnemyCatalog(); LoadPath(ExamplePath);
    }

    // =========================================================
    // Small composable widgets keep the editor structure readable without a bulky .tscn tree.
    private static Button Button(string label, Action clicked)
    { var button = new Button { Text = label }; button.Pressed += clicked; return button; }
    private static VBoxContainer Pane(string heading, float minWidth)
    {
        var pane = new VBoxContainer { CustomMinimumSize = new Vector2(minWidth, 0), SizeFlagsVertical = SizeFlags.ExpandFill };
        pane.AddThemeConstantOverride("separation", 7);
        pane.AddChild(new Label { Text = heading.ToUpperInvariant() }); return pane;
    }
    private static void Buttons(VBoxContainer parent, params (string label, Action clicked)[] items)
    { var row = new HBoxContainer(); parent.AddChild(row);
        foreach (var item in items) { Button b = Button(item.label, item.clicked); b.SizeFlagsHorizontal = SizeFlags.ExpandFill; row.AddChild(b); }
    }
    private static ItemList CreateItemList(VBoxContainer parent, Action<int> selected)
    {
        var list = new ItemList { SelectMode = ItemList.SelectModeEnum.Single, SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill };
        parent.AddChild(list); list.ItemSelected += index => selected((int)index); return list;
    }
    private static void Field(VBoxContainer parent, string label, Control control)
    {
        var row = new HBoxContainer(); parent.AddChild(row);
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(175, 0) });
        control.SizeFlagsHorizontal = SizeFlags.ExpandFill; row.AddChild(control);
    }
    private SpinBox Number(VBoxContainer parent, string label, double min, double max, double step, Action<double> changed)
    {
        var box = new SpinBox { MinValue = min, MaxValue = max, Step = step, Rounded = step >= 1 };
        Field(parent, label, box);
        box.ValueChanged += v => { if (!_refreshing && _source != null) { changed(v); Dirty(); } };
        return box;
    }
    private LineEdit Text(VBoxContainer parent, string label, Action<string> changed)
    {
        var edit = new LineEdit { PlaceholderText = "Optional" };
        Field(parent, label, edit);
        edit.TextChanged += v => { if (!_refreshing && _source != null) { changed(v); Dirty(); } };
        edit.TextSubmitted += _ => Refresh();
        return edit;
    }
    private CheckBox Check(VBoxContainer parent, string label, Action<bool> changed)
    {
        var check = new CheckBox { Text = label }; parent.AddChild(check);
        check.Toggled += v => { if (!_refreshing && _source != null) { changed(v); Dirty(); } };
        return check;
    }

    // =========================================================
    // Load enemy identity and readable display names from the authoritative scene catalogue on demand.
    // Scene definitions are cached for UI changes; never instantiate enemies on every selection or frame.
    private void ReloadEnemyCatalog()
    {
        try
        {
            var catalog = ResourceLoader.Load<EnemyCatalog>("res://ENEMIES/DefaultEnemyCatalog.tres",
                cacheMode: ResourceLoader.CacheMode.Replace);
            if (catalog == null) throw new InvalidOperationException("DefaultEnemyCatalog.tres could not be loaded.");
            catalog.Rebuild();
            var choices = catalog.ToSceneMap().Keys.Select(id =>
                (Id: id, Name: catalog.DefinitionFor(id)?.DisplayName ?? id))
                .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Id, StringComparer.Ordinal).ToList();
            _enemyCatalog = catalog; _enemyChoices = choices;
            Refresh(); Message("Enemy list refreshed: " + choices.Count + " registered types.");
        }
        catch (Exception ex)
        {
            _enemyCatalog = null; _enemyChoices.Clear();
            Refresh(); Message("Enemy list unavailable: " + ex.Message);
        }
    }

    // =========================================================
    // Keep enemy list entries readable while preserving stable IDs in the underlying resource.
    private string EnemyLabel(string id)
    {
        int index = _enemyChoices.FindIndex(e => e.Id == id);
        return index < 0 ? "[Unregistered] " + id : _enemyChoices[index].Name + " (" + id + ")";
    }

    // =========================================================
    // Preserve and visibly flag stale IDs without silently replacing data or marking the .tres dirty.
    private void SelectEnemyId(string id)
    {
        _enemyId.Clear();
        for (int i = 0; i < _enemyChoices.Count; i++)
        {
            var item = _enemyChoices[i];
            _enemyId.AddItem(item.Name + " (" + item.Id + ")");
        }
        int selected = _enemyChoices.FindIndex(e => e.Id == id);
        if (selected < 0)
        {
            _enemyId.AddItem(string.IsNullOrEmpty(id) ? "[Missing enemy ID]" : "[Unregistered] " + id);
            selected = _enemyId.ItemCount - 1;
        }
        _enemyId.Select(selected);
        _enemyId.Disabled = _enemyChoices.Count == 0;
    }
    #endregion

    #region Document Loading and Saving
    // =========================================================
    // Use resource-aware file pickers so this editor never touches the user's profile saves.
    private void SetupDialogs()
    {
        _openFile = new EditorFileDialog { Access = EditorFileDialog.AccessEnum.Resources,
            FileMode = EditorFileDialog.FileModeEnum.OpenFile, Title = "Open wave set" };
        _openFile.AddFilter("*.tres", "Godot Resources");
        AddChild(_openFile); _openFile.FileSelected += LoadPath;
        _newFile = new EditorFileDialog { Access = EditorFileDialog.AccessEnum.Resources,
            FileMode = EditorFileDialog.FileModeEnum.SaveFile, Title = "Create wave set" };
        _newFile.AddFilter("*.tres", "Godot Resources");
        AddChild(_newFile); _newFile.FileSelected += CreatePath;
        _discard = new ConfirmationDialog { Title = "Discard wave edits", DialogText = "Discard unsaved changes and reload this wave set?" };
        AddChild(_discard); _discard.Confirmed += () => { _dirty = false; LoadPath(_path); };
        _confirmDelete = new ConfirmationDialog { Title = "Confirm deletion" };
        AddChild(_confirmDelete);
        _confirmDelete.GetOkButton().Text = "Delete";
        _confirmDelete.Confirmed += () => { Action action = _pendingDelete; _pendingDelete = null; action?.Invoke(); };
    }
    private void Open()
    {
        if (_dirty) { Message("Save or Reload unsaved changes before opening another file."); return; }
        _openFile.PopupCenteredRatio(.75f);
    }
    private void New()
    {
        if (_dirty) { Message("Save or Reload unsaved changes before creating another file."); return; }
        _newFile.CurrentFile = "NewLevelWaves.tres";
        _newFile.PopupCenteredRatio(.75f);
    }
    private void Reload()
    { if (string.IsNullOrEmpty(_path)) return;
        if (_dirty) _discard.PopupCentered(); else LoadPath(_path); }

    // =========================================================
    // A new set has one immediately editable wave/batch/entry and stays separate from existing C# sets.
    private void CreatePath(string path)
    {
        if (!path.StartsWith("res://", StringComparison.Ordinal) || !path.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
        { Message("Choose a project .tres path."); return; }
        _source = new WaveSetResource();
        var wave = new WaveResource();
        var batch = new WaveBatchResource();
        batch.Enemies.Add(new WaveEnemyResource()); wave.Batches.Add(batch); _source.Waves.Add(wave);
        _path = path; _waveIndex = _batchIndex = _enemyIndex = 0; _dirty = true;
        Refresh(); Save();
    }

    // =========================================================
    // Load fresh resource data; never alter an existing C# WaveSetSource through this editor.
    private void LoadPath(string path)
    {
        if (_dirty) { Message("Save changes before switching files, or Reload to discard."); return; }
        try
        {
            Resource resource = ResourceLoader.Load(path, cacheMode: ResourceLoader.CacheMode.Replace);
            if (resource is not WaveSetResource set) { Message("Not a WaveSetResource .tres: " + path); return; }
            _source = set; _path = path; _waveIndex = _batchIndex = _enemyIndex = 0; _dirty = false;
            Refresh(); Message("Loaded " + path);
        }
        catch (Exception ex) { Message("Cannot open: " + ex.Message); }
    }

    // =========================================================
    // Validate using the production wave catalog before writing; invalid edits stay in the editor for repair.
    private void Save()
    {
        if (_source == null || string.IsNullOrEmpty(_path)) { Message("Open or create a wave set first."); return; }
        try
        {
            WaveDefinition[] waves = _source.Build();
            if (_enemyCatalog == null) throw new InvalidOperationException("Enemy catalogue unavailable. Click Refresh list.");
            WaveCatalog.Validate(waves, _enemyCatalog.ToSceneMap());
            Error error = ResourceSaver.Save(_source, _path);
            if (error != Error.Ok) { Message("Save failed: " + error); return; }
            _dirty = false; _fileLabel.Text = _path; _saveButton.Disabled = true;
            EditorInterface.Singleton.GetResourceFilesystem().Scan();
            Message("Saved " + waves.Length + " waves to " + _path);
        }
        catch (Exception ex) { Message("Cannot save: " + ex.Message); }
    }

    private void Dirty()
    {
        _dirty = true;
        _fileLabel.Text = _path + "  * UNSAVED";
        _saveButton.Disabled = false;
        if (_source != null) _totals.Text = $"{_source.Waves.Count} waves  ·  " +
            $"{_source.Waves.Sum(w => w?.Batches.Sum(b => b?.Count ?? 0) ?? 0)} enemies";
    }
    private void Message(string text) { if (_status != null) _status.Text = text; }

    // =========================================================
    // Use one modal for all destructive editor actions; capture exact resource identities to avoid stale deletions.
    private void ConfirmDelete(string message, Action delete)
    {
        _pendingDelete = delete;
        _confirmDelete.DialogText = message;
        _confirmDelete.PopupCentered(new Vector2I(480, 160));
    }
    #endregion

    #region Editable Wave / Batch / Enemy Operations
    // =========================================================
    // Content edits change only the selected .tres source, never runtime WaveManager.
    private void AddWave()
    {
        if (_source == null) return;
        var wave = new WaveResource { Number = _source.Waves.Count == 0 ? 1 : _source.Waves.Max(w => w.Number) + 1 };
        var batch = new WaveBatchResource(); batch.Enemies.Add(new WaveEnemyResource());
        wave.Batches.Add(batch); _source.Waves.Add(wave); _waveIndex = _source.Waves.Count - 1; _batchIndex = _enemyIndex = 0;
        Dirty(); Refresh();
    }
    private void DuplicateWave()
    {
        if (Wave == null) return;
        var copy = Wave.Copy(); copy.Number = _source.Waves.Max(w => w.Number) + 1;
        _source.Waves.Insert(_waveIndex + 1, copy); _waveIndex++; _batchIndex = _enemyIndex = 0; Dirty(); Refresh();
    }
    private void DeleteWave()
    {
        WaveSetResource source = _source;
        WaveResource selected = Wave;
        if (selected == null) return;
        ConfirmDelete($"Delete Wave {selected.Number} and all {selected.Batches.Count} batches? Reload before saving to discard the change.", () =>
        {
            if (_source != source) return;
            int index = source.Waves.IndexOf(selected);
            if (index < 0) return;
            source.Waves.RemoveAt(index);
            _waveIndex = Math.Clamp(index, 0, source.Waves.Count - 1);
            _batchIndex = _enemyIndex = 0; Dirty(); Refresh();
        });
    }
    private void MoveWave(int direction)
    {
        int next = _waveIndex + direction;
        if (Wave == null || next < 0 || next >= _source.Waves.Count) return;
        (_source.Waves[_waveIndex], _source.Waves[next]) = (_source.Waves[next], _source.Waves[_waveIndex]);
        _waveIndex = next; Dirty(); Refresh();
    }
    private void AddBatch()
    {
        if (Wave == null) return;
        var batch = new WaveBatchResource(); batch.Enemies.Add(new WaveEnemyResource());
        Wave.Batches.Add(batch); _batchIndex = Wave.Batches.Count - 1; _enemyIndex = 0; Dirty(); Refresh();
    }
    private void DuplicateBatch()
    {
        if (Batch == null) return;
        Wave.Batches.Insert(_batchIndex + 1, Batch.Copy()); _batchIndex++; _enemyIndex = 0; Dirty(); Refresh();
    }
    private void DeleteBatch()
    {
        WaveSetResource source = _source;
        WaveResource wave = Wave;
        WaveBatchResource selected = Batch;
        if (selected == null) return;
        ConfirmDelete($"Delete this batch ({selected.Count} enemies) from Wave {wave.Number}?", () =>
        {
            if (_source != source || !source.Waves.Contains(wave)) return;
            int index = wave.Batches.IndexOf(selected);
            if (index < 0) return;
            wave.Batches.RemoveAt(index);
            _waveIndex = source.Waves.IndexOf(wave);
            _batchIndex = Math.Clamp(index, 0, wave.Batches.Count - 1);
            _enemyIndex = 0; Dirty(); Refresh();
        });
    }
    private void MoveBatch(int direction)
    {
        int next = _batchIndex + direction;
        if (Batch == null || next < 0 || next >= Wave.Batches.Count) return;
        (Wave.Batches[_batchIndex], Wave.Batches[next]) = (Wave.Batches[next], Wave.Batches[_batchIndex]);
        _batchIndex = next; Dirty(); Refresh();
    }
    private void AddEnemy()
    {
        if (Batch == null) return; Batch.Enemies.Add(new WaveEnemyResource());
        _enemyIndex = Batch.Enemies.Count - 1; Dirty(); Refresh();
    }
    private void DuplicateEnemy()
    {
        if (Entry == null) return; Batch.Enemies.Insert(_enemyIndex + 1, Entry.Copy()); _enemyIndex++; Dirty(); Refresh();
    }
    private void DeleteEnemy()
    {
        WaveSetResource source = _source;
        WaveResource wave = Wave;
        WaveBatchResource batch = Batch;
        WaveEnemyResource selected = Entry;
        if (selected == null) return;
        ConfirmDelete($"Delete {EnemyLabel(selected.EnemyId)} from this batch?", () =>
        {
            if (_source != source || !source.Waves.Contains(wave) || !wave.Batches.Contains(batch)) return;
            int index = batch.Enemies.IndexOf(selected);
            if (index < 0) return;
            batch.Enemies.RemoveAt(index);
            _waveIndex = source.Waves.IndexOf(wave);
            _batchIndex = wave.Batches.IndexOf(batch);
            _enemyIndex = Math.Clamp(index, 0, batch.Enemies.Count - 1); Dirty(); Refresh();
        });
    }
    #endregion

    #region View Updates
    // =========================================================
    // Preview the exact deterministic counts for this batch using the SAME quota calculation as WaveManager.
    // It rebuilds only when fields/selection change; no physics, random shuffles or background updates.
    private void RefreshComposition()
    {
        if (_compositionPreview == null) return;
        foreach (Node child in _compositionPreview.GetChildren())
        { _compositionPreview.RemoveChild(child); child.QueueFree(); }
        WaveBatchResource batch = Batch;
        _compositionPreview.Visible = batch != null;
        if (batch == null) return;
        if (batch.Enemies == null || batch.Enemies.Count == 0)
        { _compositionPreview.AddChild(new Label { Text = "Add an enemy entry to preview this batch." }); return; }
        try
        {
            WaveEnemyEntry[] entries = batch.Enemies.Select(e => WaveBuilder.Enemy(
                e?.EnemyId ?? "", e?.Weight ?? 0, e?.Portal ?? "", e?.Shield ?? false, e?.Stealth ?? false)).ToArray();
            int[] counts = WaveCatalog.CompositionCounts(WaveBuilder.Batch(batch.Count, batch.Interval, batch.After, entries));
            _compositionPreview.AddChild(new Label { Text = $"{batch.Count} total enemies · exact quotas · shuffled spawn order",
                TooltipText = "The game determines exact numbers using largest remainders (earlier entries win ties), then shuffles their order." });
            for (int i = 0; i < counts.Length; i++)
            {
                WaveEnemyResource enemy = batch.Enemies[i];
                string label = EnemyLabel(enemy?.EnemyId ?? "");
                if (enemy?.Shield == true) label += " [shield]";
                if (enemy?.Stealth == true) label += " [stealth]";
                if (!string.IsNullOrEmpty(enemy?.Portal)) label += " [" + enemy.Portal + "]";
                var row = new HBoxContainer();
                _compositionPreview.AddChild(row);
                row.AddChild(new Label { Text = label, SizeFlagsHorizontal = SizeFlags.ExpandFill,
                    ClipText = true, TooltipText = label });
                row.AddChild(new Label { Text = $"{counts[i]} / {batch.Count}  ({100.0 * counts[i] / batch.Count:0.#}%)" });
                _compositionPreview.AddChild(new ProgressBar { MinValue = 0, MaxValue = batch.Count, Value = counts[i],
                    ShowPercentage = false, CustomMinimumSize = new Vector2(0, 9), SizeFlagsHorizontal = SizeFlags.ExpandFill });
            }
        }
        catch (ArgumentException error)
        { _compositionPreview.AddChild(new Label { Text = "Preview unavailable: " + error.Message,
            AutowrapMode = TextServer.AutowrapMode.WordSmart }); }
        // Keep list counts and flags current while editing numeric fields without rebuilding the whole selection UI.
        if (Wave != null && _waveIndex >= 0 && _waveIndex < _waves.ItemCount)
            _waves.SetItemText(_waveIndex, $"Wave {Wave.Number}    ({Wave.Batches.Sum(b => b?.Count ?? 0)} ships)");
        if (_batchIndex >= 0 && _batchIndex < _batches.ItemCount)
            _batches.SetItemText(_batchIndex, $"Batch {_batchIndex + 1}    {batch.Count} ships   {batch.Interval:0.##}s");
        if (Entry != null && _enemyIndex >= 0 && _enemyIndex < _enemies.ItemCount)
            _enemies.SetItemText(_enemyIndex, $"{EnemyLabel(Entry.EnemyId)}   weight {Entry.Weight:0.##}" +
                (Entry.Shield ? "  [shield]" : "") + (Entry.Stealth ? "  [stealth]" : ""));
        if (_source != null) _totals.Text = $"{_source.Waves.Count} waves  ·  " +
            $"{_source.Waves.Sum(w => w?.Batches.Sum(b => b?.Count ?? 0) ?? 0)} enemies";
    }

    // =========================================================
    // Editor controls refresh only on document/selection edits, never in _Process.
    private void Refresh()
    {
        _refreshing = true;
        try
        {
            _waves.Clear(); _batches.Clear(); _enemies.Clear();
            if (_source != null) foreach (var w in _source.Waves)
                _waves.AddItem(w == null ? "Missing wave" : $"Wave {w.Number}    ({w.Batches.Sum(b => b?.Count ?? 0)} ships)");
            if (_waveIndex >= 0 && _waveIndex < _waves.ItemCount) _waves.Select(_waveIndex);
            var wave = Wave;
            if (wave != null) foreach (var batch in wave.Batches)
                _batches.AddItem(batch == null ? "Missing batch" : $"Batch {_batches.ItemCount + 1}    {batch.Count} ships   {batch.Interval:0.##}s");
            if (_batchIndex >= 0 && _batchIndex < _batches.ItemCount) _batches.Select(_batchIndex);
            var batchSelected = Batch;
            if (batchSelected != null) foreach (var enemy in batchSelected.Enemies)
                _enemies.AddItem(enemy == null ? "Missing enemy" : $"{EnemyLabel(enemy.EnemyId)}   weight {enemy.Weight:0.##}" +
                    (enemy.Shield ? "  [shield]" : "") + (enemy.Stealth ? "  [stealth]" : ""));
            if (_enemyIndex >= 0 && _enemyIndex < _enemies.ItemCount) _enemies.Select(_enemyIndex);
            _waveFields.Visible = wave != null; _batchFields.Visible = batchSelected != null;
            _enemyFields.Visible = Entry != null; _presentationFields.Visible = wave != null;
            if (wave != null)
            {
                _waveNumber.Value = wave.Number; _alert.Select((int)wave.Alert);
                _bannerTitle.Text = wave.BannerTitle ?? ""; _bannerDescription.Text = wave.BannerDescription ?? "";
                _speaker.Text = wave.Speaker ?? ""; _narration.Text = wave.NarrationText ?? "";
                _voiceCue.Text = wave.VoiceCueId ?? "";
            }
            if (batchSelected != null) { _count.Value = batchSelected.Count; _interval.Value = batchSelected.Interval; _after.Value = batchSelected.After; }
            if (Entry != null)
            {
                SelectEnemyId(Entry.EnemyId); _weight.Value = Entry.Weight; _portal.Text = Entry.Portal ?? "";
                _shield.ButtonPressed = Entry.Shield; _stealth.ButtonPressed = Entry.Stealth;
            }
            RefreshComposition();
            _fileLabel.Text = _path + (_dirty ? "  * UNSAVED" : "");
            _saveButton.Disabled = !_dirty || _source == null;
            _totals.Text = _source == null ? "" : $"{_source.Waves.Count} waves  ·  " +
                $"{_source.Waves.Sum(w => w?.Batches.Sum(b => b?.Count ?? 0) ?? 0)} enemies";
        }
        finally { _refreshing = false; }
    }
    #endregion
}
#endif
