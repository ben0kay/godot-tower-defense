// Shared cosmetic-only drone engine and contrail settings; dispatch, speed and cargo remain with each role.
using Godot;
[GlobalClass]
public partial class DroneVisualConfig : Resource
{
    [ExportGroup("Twin Thrusters")]
    [Export] public bool ThrustersEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0.2,0.35,0.01")] public float IgnitionSeconds { get; set; } = .28f;
    [Export] public Color CoreTint { get; set; } = new(.8f, .98f, 1, .95f);
    [Export] public Color HaloTint { get; set; } = new(.15f, .8f, 1, .24f);
    [Export(PropertyHint.Range, "0,3,.05")] public float Intensity { get; set; } = 1;
    [Export(PropertyHint.Range, ".2,3,.05")] public float EngineScale { get; set; } = 1;
    [Export(PropertyHint.Range, "1,4,.1")] public float IgnitionScale { get; set; } = 2.2f;
    [ExportGroup("Ion Contrail")]
    [Export] public bool TrailEnabled { get; set; } = true;
    [Export] public Color TrailColor { get; set; } = new(.25f, .85f, 1, 1);
    [Export(PropertyHint.Range, "0,1,.01")] public float TrailAlpha { get; set; } = .32f;
    [Export(PropertyHint.Range, ".5,2,.1")] public float TrailWidth { get; set; } = 1.25f;
    [Export(PropertyHint.Range, "0,35,1")] public float MinimumTrailLength { get; set; } = 15;
    [Export(PropertyHint.Range, "0,64,1")] public float MaximumTrailLength { get; set; } = 35;
    [Export(PropertyHint.Range, ".25,3,.05")] public float SpeedResponse { get; set; } = 1;
}
