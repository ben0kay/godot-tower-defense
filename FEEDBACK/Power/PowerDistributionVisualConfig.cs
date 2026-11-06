// Shared cosmetic tuning for selected Relay coverage/routes and one culled visual per Transmitter pair.
using Godot;

[GlobalClass]
public partial class PowerDistributionVisualConfig : Resource
{
    [ExportGroup("Selected Relay")]
    [Export] public Color CoverageFill { get; set; } = new(.15f, .85f, 1f, .10f);
    [Export] public Color CoverageEdge { get; set; } = new(.25f, .9f, 1f, .24f);
    [Export] public Color SourceRoute { get; set; } = new(.55f, 1f, 1f, .72f);
    [Export(PropertyHint.Range, "2,16,1")] public float DashLength { get; set; } = 8;
    [ExportGroup("Transmitter Link")]
    [Export] public Color PoweredCore { get; set; } = new(.25f, .8f, 1f, .55f);
    [Export] public Color PoweredHalo { get; set; } = new(.15f, .65f, 1f, .09f);
    [Export] public Color Unpowered { get; set; } = new(.35f, .48f, .6f, .25f);
    [Export] public Color Suspended { get; set; } = new(.9f, .58f, .2f, .32f);
    [Export(PropertyHint.Range, "0.5,3,0.1")] public float CoreWidth { get; set; } = 1.5f;
    [Export(PropertyHint.Range, "2,10,0.5")] public float HaloWidth { get; set; } = 6;
    [Export(PropertyHint.Range, "1,6,0.1")] public double PulseSeconds { get; set; } = 2.8;
    [Export(PropertyHint.Range, "5,30,1")] public double AnimationHz { get; set; } = 15;
    private static PowerDistributionVisualConfig _shared;
    public static PowerDistributionVisualConfig Shared => _shared ??= GD.Load<PowerDistributionVisualConfig>("res://FEEDBACK/Power/DefaultPowerDistributionVisuals.tres");
}
