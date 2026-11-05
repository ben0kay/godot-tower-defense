// A durable single peer belongs to this utility building; only the shared grid activates reciprocal pairs.
using Godot;
public partial class PowerTransmitter : PowerDistributionBuilding
{
    [Export] public PowerTransmitterConfig Config { get; set; }
    public string PeerSaveId { get; internal set; } = "";
    public override TowerConfig AuthoredConfig => Config;
    public override bool AcceptsConfig(TowerConfig config) => config is PowerTransmitterConfig;
    protected override void ApplyConfig(TowerConfig config) => Config = (PowerTransmitterConfig)config;
    public bool TryLink(PowerTransmitter other, out string failure)
    {
        if (PowerNetworks == null) { failure = "No live power network."; return false; }
        return PowerNetworks.TryLinkTransmitters(this, other, out failure);
    }
    public void Unlink() => PowerNetworks?.UnlinkTransmitter(this);
}
