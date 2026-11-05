// Event-driven passive Relay/Transmitter lifecycle and static core/link visuals; no processing loop.
using Godot;
using System.Collections.Generic;

public abstract partial class PowerDistributionBuilding : TowerBase
{
    public PowerGridManager PowerNetworks { get; private set; }
    public override List<TowerLevel> Levels => AuthoredConfig.Levels;
    public override TowerFinalStats ResolveLevel(int level) => null;
    public override bool ValidateScene() => AuthoredConfig is PowerDistributionConfig config && config.Levels.Count == 0 && float.IsFinite(config.MaxHealth) && config.MaxHealth > 0 && GetNodeOrNull<Sprite2D>("Sprite") != null && GetNodeOrNull<Sprite2D>("Core") != null;
    private Sprite2D _core;
    private Line2D _link;
    private string _status = "Preview · no live power network";
    public override string DescribeRuntime() => _status;
    public override void _Ready()
    {
        SetProcess(false); SetOperationalPhysicsProcess(false);
        _core = GetNode<Sprite2D>("Core");
        PowerNetworks = GetNodeOrNull<PowerGridManager>("../../PowerNetworks");
        WorkStarted += RefreshRegistration; WorkCompleted += RefreshRegistration; HealthChanged += RefreshRegistration;
        DisableStateChanged += RefreshRegistration; SpatialChanged += Moved; ObserveSpatialChanges(true);
        AuthoredConfig.Changed += ConfigChanged;
        if (PowerNetworks != null)
        { PowerNetworks.AccountingChanged += RefreshPresentation; PowerNetworks.NetworksChanged += RefreshRegistration; PowerNetworks.DistributionChanged += RefreshPresentation; }
        RefreshRegistration();
    }
    private void Moved(TowerBase tower) => RefreshRegistration();
    private void ConfigChanged() { NotifyStatsChanged(); RefreshRegistration(); }
    private void RefreshRegistration()
    {
        if (GodotObject.IsInstanceValid(PowerNetworks)) PowerNetworks.SetDistribution(this);
        RefreshPresentation();
    }
    // =========================================================
    // One lower-SaveId endpoint draws a static line; geometry/status only update on actual input changes.
    private void RefreshPresentation()
    {
        if (!GodotObject.IsInstanceValid(PowerNetworks) || !IsInsideTree() || IsQueuedForDeletion()) return;
        string next = PowerNetworks.DescribeDistribution(this, out bool powered);
        if (_core != null) _core.Visible = !IsBusy;
        if (_core != null) _core.SelfModulate = powered ? new Color(.45f, 1f, 1f) : new Color(.12f, .22f, .27f);
        if (this is PowerTransmitter transmitter)
        {
            PowerTransmitter peer = PowerNetworks.TransmitterPeer(transmitter);
            bool owns = peer != null && string.CompareOrdinal(SaveId, peer.SaveId) < 0;
            if (owns)
            {
                if (_link == null) { _link = new Line2D { Name = "PowerLink", Width = 1.5f, ZIndex = -1, Antialiased = true }; AddChild(_link); }
                _link.Points = new[] { Vector2.Zero, ToLocal(peer.GlobalPosition) };
                _link.DefaultColor = powered ? new Color(.2f, .75f, 1f, .5f) : new Color(.5f, .44f, .24f, .22f);
            }
            if (_link != null) _link.Visible = owns;
        }
        if (_status == next) return; _status = next; NotifyRuntimeChanged();
    }
    public override void _ExitTree()
    {
        WorkStarted -= RefreshRegistration; WorkCompleted -= RefreshRegistration; HealthChanged -= RefreshRegistration;
        DisableStateChanged -= RefreshRegistration; SpatialChanged -= Moved;
        if (GodotObject.IsInstanceValid(AuthoredConfig)) AuthoredConfig.Changed -= ConfigChanged;
        if (GodotObject.IsInstanceValid(PowerNetworks))
        {
            PowerNetworks.AccountingChanged -= RefreshPresentation; PowerNetworks.NetworksChanged -= RefreshRegistration; PowerNetworks.DistributionChanged -= RefreshPresentation;
            PowerNetworks.RemoveDistribution(this);
        }
        PowerNetworks = null; base._ExitTree();
    }
}
