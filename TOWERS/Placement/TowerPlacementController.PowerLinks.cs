// Manual transmitter pairing is an explicit selected-tower action and a single indexed world click.
using Godot;

public partial class TowerPlacementController
{
    private PowerTransmitter _linkSource;
    private void BindPowerLinkActions()
    { _hud.PowerLinkRequested += BeginPowerLink; _hud.PowerUnlinkRequested += UnlinkPower; }
    private void UnbindPowerLinkActions()
    {
        if (GodotObject.IsInstanceValid(_hud)) { _hud.PowerLinkRequested -= BeginPowerLink; _hud.PowerUnlinkRequested -= UnlinkPower; }
        _linkSource = null;
    }
    private void BeginPowerLink()
    {
        if (GetTree().Paused || !_hud.IsInspectionVisible || _placing || _selected is not PowerTransmitter source
            || !source.CanOperate || source.IsBusy || source.PeerSaveId.Length > 0) return;
        _linkSource = source;
        _hud.SetRuntimeDetails("LINK: click another island's unpaired Transmitter. Right-click / Esc cancels.");
    }
    private bool CancelPowerLink()
    {
        if (_linkSource == null) return false;
        _linkSource = null;
        if (_hud.IsInspectionVisible && GodotObject.IsInstanceValid(_selected)) _hud.SetDefenseDetails(_selected);
        return true;
    }
    private bool TryPowerLinkClick(Vector2I cell)
    {
        if (_linkSource == null) return false;
        if (GetTree().Paused) return true;
        if (!GodotObject.IsInstanceValid(_linkSource) || _linkSource.IsQueuedForDeletion() || _selected != _linkSource)
        { CancelPowerLink(); return true; }
        if (!_linkSource.TryLink(TowerAt(cell) as PowerTransmitter, out string failure))
        { _hud.SetRuntimeDetails(failure + " Choose another Transmitter, or right-click to cancel."); return true; }
        CancelPowerLink(); return true;
    }
    private void UnlinkPower()
    {
        if (GetTree().Paused || !_hud.IsInspectionVisible || _selected is not PowerTransmitter transmitter) return;
        CancelPowerLink(); transmitter.Unlink(); _hud.SetDefenseDetails(transmitter);
    }
}
