// Event-driven distribution lifecycle; cosmetic helpers own selected coverage and bounded link animation.
using Godot;
using System;
using System.Collections.Generic;

public abstract partial class PowerDistributionBuilding : TowerBase
{
    public PowerGridManager PowerNetworks { get; private set; }
    public override List<TowerLevel> Levels => AuthoredConfig.Levels;
    public override TowerFinalStats ResolveLevel(int level) => null;
    public override bool ValidateScene() => AuthoredConfig is PowerDistributionConfig config && config.Levels.Count == 0 && float.IsFinite(config.MaxHealth) && config.MaxHealth > 0 && GetNodeOrNull<Sprite2D>("Sprite") != null && GetNodeOrNull<Sprite2D>("Core") != null;
    private Sprite2D _core;
    private PowerTransmitterLinkVisual _link;
    private PowerRelaySelectionVisual _relaySelection;
    private bool _selected;
    private PowerDistributionBuilding _linkPeer;
    private event Action LinkSelectionChanged;
    internal bool DistributionSelected => _selected;
    private string _details = "";
    private string _status = "Preview · no live power network";
    public override string DescribeRuntime()
    {
        int separator = _status.IndexOf(" · ", System.StringComparison.Ordinal);
        string heading = separator >= 0 ? _status[..separator] : _status;
        return heading + (_details.Length > 0 ? "\n" + _details : "");
    }
    public override void SetSelected(bool selected)
    {
        base.SetSelected(selected);
        bool changed = _selected != selected; _selected = selected;
        if (this is PowerTransmitter && changed) { RefreshPresentation(); LinkSelectionChanged?.Invoke(); }
        if (this is PowerRelay relay)
        {
            if (selected && _relaySelection == null) { _relaySelection = new PowerRelaySelectionVisual { Name = "RelayCoverage" }; AddChild(_relaySelection); }
            _relaySelection?.Present(relay, selected);
        }
    }
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
            ObserveLinkPeer(owns ? peer : null);
            if (owns)
            {
                if (_link == null) { _link = new PowerTransmitterLinkVisual { Name = "PowerLink" }; AddChild(_link); }
                _link.Present(ToLocal(peer.GlobalPosition), true, PowerNetworks.LinkVisualState(transmitter), _selected || peer.DistributionSelected);
            }
            if (!owns) _link?.Present(Vector2.Zero, false, PowerLinkVisualState.Suspended);
        }
        if (this is PowerRelay relay) _relaySelection?.Present(relay, _selected);
        string details = PowerNetworks.DistributionDetails(this);
        if (_status == next && _details == details) return; _status = next; _details = details; NotifyRuntimeChanged();
    }
    // Only the pair's visual owner observes its peer's selection; unlink/re-pair/teardown detach the old event.
    private void ObserveLinkPeer(PowerDistributionBuilding peer)
    {
        if (_linkPeer == peer) return;
        if (GodotObject.IsInstanceValid(_linkPeer)) _linkPeer.LinkSelectionChanged -= RefreshPresentation;
        _linkPeer = peer;
        if (GodotObject.IsInstanceValid(_linkPeer)) _linkPeer.LinkSelectionChanged += RefreshPresentation;
    }
    public override void _ExitTree()
    {
        ObserveLinkPeer(null);
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
