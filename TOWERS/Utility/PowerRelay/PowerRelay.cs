// Local-only utility specialization; shared distribution lifecycle owns registration and core presentation.
using Godot;
public partial class PowerRelay : PowerDistributionBuilding
{
    [Export] public PowerRelayConfig Config { get; set; }
    public override TowerConfig AuthoredConfig => Config;
    public override bool AcceptsConfig(TowerConfig config) => config is PowerRelayConfig;
    protected override void ApplyConfig(TowerConfig config) => Config = (PowerRelayConfig)config;
}
