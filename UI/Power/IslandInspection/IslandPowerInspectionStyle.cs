// Authored island-boundary interaction and diagnostic colours; no simulation settings.
using Godot;
[GlobalClass]
public partial class IslandPowerInspectionStyle : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, ".05,1,.05")] public float HoverSeconds { get; set; } = .25f;
    [Export(PropertyHint.Range, "0,.5,.05")] public float LeaveGraceSeconds { get; set; } = .15f;
    [Export(PropertyHint.Range, "3,16,1")] public float HitWidthPixels { get; set; } = 7;
    [Export(PropertyHint.Range, ".5,3,.25")] public float OutlineWidthPixels { get; set; } = 1;
    [Export(PropertyHint.Range, ".1,1,.05")] public float SampleSeconds { get; set; } = .25f;
    [Export(PropertyHint.Range, ".5,3,.1")] public float ShortageHoldSeconds { get; set; } = 1;
    [Export] public Color Outline { get; set; } = new(.25f, .85f, .95f, .18f);
    [Export] public Color HoverOutline { get; set; } = new(.4f, .95f, 1, .6f);
    [Export] public Color Shortage { get; set; } = new(1, .65f, .15f, .8f);
    [Export] public Color Depleted { get; set; } = new(1, .22f, .18f, .85f);
}
