// Optional complete source override; null inherits entry -> batch -> wave -> legacy.
using Godot;
[Tool, GlobalClass]
public partial class WaveSpawnSourceResource : Resource
{
    [Export] public SpawnSourcePolicy Policy { get; set; }
    [Export] public string EdgeSector { get; set; } = "";
    [Export(PropertyHint.Range, "0,1000000,.1")] public double TopWeight { get; set; } = 1;
    [Export(PropertyHint.Range, "0,1000000,.1")] public double BottomWeight { get; set; } = 1;
    [Export(PropertyHint.Range, "0,1000000,.1")] public double LeftWeight { get; set; } = 1;
    [Export(PropertyHint.Range, "0,1000000,.1")] public double RightWeight { get; set; } = 1;
    public SpawnSourceOptions Capture() => new(Policy, EdgeSector, TopWeight, BottomWeight, LeftWeight, RightWeight);
    public WaveSpawnSourceResource Copy() => new()
    { Policy = Policy, EdgeSector = EdgeSector, TopWeight = TopWeight, BottomWeight = BottomWeight, LeftWeight = LeftWeight, RightWeight = RightWeight };
}
