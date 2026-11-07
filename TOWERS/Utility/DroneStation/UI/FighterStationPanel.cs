// Selected-station roster/actions only. Runtime and wallet events refresh it; there is no UI tick or world scan.
using Godot;
public partial class FighterStationPanel : VBoxContainer
{
    public UIMasterConfig ThemeConfig { get; set; }
    private DroneStation _station;
    private EconomyManager _wallet;
    private OptionButton _roster;
    private Label _status, _error, _stock;
    private Button _buy, _launch, _recall;
    private int _slot;
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false); AddThemeConstantOverride("separation", 3);
        _roster = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _roster.AddThemeFontSizeOverride("font_size", 12);
        _roster.ItemSelected += index => { CancelPatrolEdit(); _slot = (int)index; Refresh(); };
        AddChild(_roster);
        _stock = UIButtonFactory.Label("", 11, ThemeConfig); _stock.AutowrapMode = TextServer.AutowrapMode.WordSmart; AddChild(_stock);
        _status = UIButtonFactory.Label("", 11, ThemeConfig); AddChild(_status);
        _buy = UIButtonFactory.Create("BUY FIGHTER", Buy, UIButtonStyle.Compact, 24, ThemeConfig); AddChild(_buy);
        var actions = new HBoxContainer(); AddChild(actions);
        _launch = UIButtonFactory.Create("LAUNCH", Launch, UIButtonStyle.Compact, 24, ThemeConfig);
        _recall = UIButtonFactory.Create("RECALL", Recall, UIButtonStyle.Compact, 24, ThemeConfig);
        actions.AddChild(_launch); actions.AddChild(_recall);
        BuildPatrolControls();
        _error = UIButtonFactory.Label("", 10, ThemeConfig); _error.AutowrapMode = TextServer.AutowrapMode.WordSmart; AddChild(_error);
        _launch.TooltipText = "Launch using the selected fighter order. Fixed twin guns engage nearby valid targets. A partial manual launch is allowed when rounds remain.";
        _recall.TooltipText = "Return this fighter to its own reserved dock. Dock service draws real station ammunition and paid repair; manual recall stays docked.";
        Hide();
    }
    public void Bind(DroneStation station)
    {
        if (station == _station) { if (station != null) Refresh(); return; }
        Disconnect(); _station = GodotObject.IsInstanceValid(station) && !station.IsQueuedForDeletion() ? station : null;
        if (_station == null) { Hide(); return; }
        _slot = 0; _error.Text = ""; _error.Hide();
        _station.RuntimeChanged += Refresh; _station.TreeExiting += LostStation;
        _wallet = _station.Wallet;
        if (GodotObject.IsInstanceValid(_wallet)) _wallet.BalanceChanged += OnBalance;
        AttachPatrolPreview();
        Show(); Refresh();
    }
    private void OnBalance(string resource, int amount) => Refresh();
    private void Refresh()
    {
        if (!GodotObject.IsInstanceValid(_station) || _station.IsQueuedForDeletion()) { Bind(null); return; }
        int capacity = Mathf.Clamp(_station.Config.FighterCapacity, 1, 4);
        _slot = Mathf.Clamp(_slot, 0, capacity - 1); _roster.Clear();
        for (int i = 0; i < capacity; i++)
        {
            var fighter = _station.Fighters?.At(i);
            _roster.AddItem($"F{i + 1} · {(fighter == null ? "EMPTY" : _station.Fighters.DescribeService(i))}", i);
        }
        _roster.Select(_slot); _stock.Text = _station.DescribeAmmo();
        FighterDrone selected = _station.Fighters?.At(_slot);
        if (_editing != null && _editing != selected) CancelPatrolEdit();
        RefreshPatrolControls(selected);
        _status.Text = selected == null ? "Empty fighter slot" : $"HP {selected.Health:0}/{selected.MaxHealth:0} · AMMO {selected.Ammo}/{selected.AmmoCapacity}";
        _status.TooltipText = selected == null ? "" : "Fighter " + selected.SaveId;
        _buy.Text = $"BUY · {Mathf.Clamp(_station.Config.FighterCredits, 0, 1000000)} C"; _buy.Disabled = !_station.CanBuyFighter;
        _launch.Disabled = !_station.CanAttack || _station.IsBusy || selected?.State != FighterDroneState.Docked || selected.Ammo <= 0;
        _recall.Disabled = selected == null || selected.State == FighterDroneState.Docked || selected.State == FighterDroneState.Returning && !_station.Fighters.IsAutoReturning(_slot) || GetTree().Paused;
    }
    private void Buy()
    {
        if (!GodotObject.IsInstanceValid(_station)) return;
        if (_station.TryBuyFighter(out var fighter, out string error)) { _slot = fighter.Slot; error = ""; }
        _error.Text = error; _error.Visible = error.Length > 0; Refresh();
    }
    private void Launch() { _station?.LaunchFighter(_slot); Refresh(); }
    private void Recall() { _station?.RecallFighter(_slot); Refresh(); }
    private void LostStation() => Bind(null);
    private void Disconnect()
    {
        DropPatrolPreview();
        if (GodotObject.IsInstanceValid(_station)) { _station.RuntimeChanged -= Refresh; _station.TreeExiting -= LostStation; }
        if (GodotObject.IsInstanceValid(_wallet)) _wallet.BalanceChanged -= OnBalance;
        _station = null; _wallet = null;
    }
    public override void _ExitTree() => Disconnect();
}
