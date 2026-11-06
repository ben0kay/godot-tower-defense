// Editor-only complete source overrides; no runtime state, polling or scene changes.
#if TOOLS
using Godot;
using System;
[Tool]
public partial class SpawnSourceEditor : VBoxContainer
{
    private Func<WaveSpawnSourceResource> _read;
    private Action<WaveSpawnSourceResource> _write;
    private Action _changed;
    private bool _refreshing;
    private CheckBox _override;
    private OptionButton _policy;
    private LineEdit _sector;
    private readonly SpinBox[] _weights = new SpinBox[4];
    private VBoxContainer _fields;
    public void Initialize(Func<WaveSpawnSourceResource> read, Action<WaveSpawnSourceResource> write, Action changed)
    { _read = read; _write = write; _changed = changed; }
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _override = new CheckBox { Text = "Override spawn source (otherwise inherit)" }; AddChild(_override);
        _fields = new VBoxContainer(); AddChild(_fields);
        _policy = new OptionButton();
        foreach (var policy in Enum.GetValues<SpawnSourcePolicy>()) _policy.AddItem(policy.ToString());
        _fields.AddChild(_policy);
        _sector = new LineEdit { PlaceholderText = "Required edge sector ID (blank = whole allowed sides)",
            TooltipText = "EdgesOnly. Define named side/range resources in the map's LevelConfig.EdgeSectors Inspector array." };
        _fields.AddChild(_sector);
        var grid = new GridContainer { Columns = 2 }; _fields.AddChild(grid);
        string[] sides = { "Top weight", "Bottom weight", "Left weight", "Right weight" };
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            grid.AddChild(new Label { Text = sides[i] });
            var number = new SpinBox { MinValue = 0, MaxValue = 1000000, Step = .1, Value = 1 };
            grid.AddChild(number); _weights[i] = number;
            number.ValueChanged += value => Edit(source =>
            {
                if (index == 0) source.TopWeight = value; else if (index == 1) source.BottomWeight = value;
                else if (index == 2) source.LeftWeight = value; else source.RightWeight = value;
            });
        }
        _fields.AddChild(new Label { Text = "Mixed: each eligible portal has weight 1; sides use these weights.\nPosition is sampled separately. RequiredPortal uses each entry's Portal field.\nLevel permissions still apply; sector IDs validate against the map at deployment.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart });
        _override.Toggled += enabled =>
        {
            if (_refreshing) return;
            _write(enabled ? new WaveSpawnSourceResource() : null); _changed(); RefreshValues();
        };
        _policy.ItemSelected += value => Edit(source =>
        {
            source.Policy = (SpawnSourcePolicy)value;
            if (source.Policy != SpawnSourcePolicy.EdgesOnly) source.EdgeSector = "";
        });
        _sector.TextChanged += value => Edit(source => source.EdgeSector = value);
        RefreshValues();
    }
    private void Edit(Action<WaveSpawnSourceResource> edit)
    {
        if (_refreshing || _read() == null) return;
        edit(_read()); _changed();
        _sector.Visible = _read().Policy == SpawnSourcePolicy.EdgesOnly;
    }
    public void RefreshValues()
    {
        if (_override == null) return;
        _refreshing = true;
        try
        {
            var source = _read(); _override.ButtonPressed = source != null; _fields.Visible = source != null;
            if (source == null) return;
            _policy.Select((int)source.Policy); _sector.Text = source.EdgeSector ?? "";
            _sector.Visible = source.Policy == SpawnSourcePolicy.EdgesOnly;
            _weights[0].Value = source.TopWeight; _weights[1].Value = source.BottomWeight;
            _weights[2].Value = source.LeftWeight; _weights[3].Value = source.RightWeight;
        }
        finally { _refreshing = false; }
    }
}
#endif
