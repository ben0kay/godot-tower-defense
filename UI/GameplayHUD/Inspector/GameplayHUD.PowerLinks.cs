// Selected-transmitter actions only; placement validates world clicks and owns linking, with no UI polling.
using Godot;
using System;

public partial class GameplayHUD
{
    public event Action PowerLinkRequested, PowerUnlinkRequested;
    private HBoxContainer _powerLinkActions;
    private Button _powerLink, _powerUnlink;
    private void SetupPowerLinkActions()
    {
        _powerLinkActions = new HBoxContainer { Name = "PowerLinkActions", CustomMinimumSize = new Vector2(0, 26) };
        _powerLink = UIButtonFactory.Create("LINK", () => PowerLinkRequested?.Invoke(), UIButtonStyle.Compact, 26, Config);
        _powerUnlink = UIButtonFactory.Create("UNLINK", () => PowerUnlinkRequested?.Invoke(), UIButtonStyle.Compact, 26, Config);
        _powerLinkActions.AddChild(_powerLink); _powerLinkActions.AddChild(_powerUnlink);
        GetNode<VBoxContainer>("BottomArea/Inspector/Insets/Body/Columns/Side").AddChild(_powerLinkActions);
        _powerLinkActions.Hide();
    }
    private void SetPowerLinkActions(TowerBase tower)
    {
        if (_powerLinkActions == null) return;
        bool distribution = GodotObject.IsInstanceValid(tower) && tower is PowerDistributionBuilding && !_buildPreview;
        _runtimeStats.CustomMinimumSize = new Vector2(0, distribution ? 48 : 0);
        _runtimeStats.MaxLinesVisible = distribution ? 3 : -1;
        _runtimeStats.ClipText = distribution;
        var transmitter = GodotObject.IsInstanceValid(tower) ? tower as PowerTransmitter : null;
        _powerLinkActions.Visible = transmitter != null && !_buildPreview;
        if (transmitter == null) return;
        _powerLink.Disabled = !transmitter.CanOperate || transmitter.IsBusy || transmitter.PeerSaveId.Length > 0;
        _powerUnlink.Disabled = transmitter.PeerSaveId.Length == 0;
        _powerLink.TooltipText = "Choose Link, then click a completed, unpaired Transmitter on another conducting island. Right-click or Escape cancels.";
    }
}
