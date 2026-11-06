// Portal-local cosmetic spawn response; no readiness, spawn timing or navigation settings.
using Godot;
[GlobalClass]
public partial class PortalSpawnFeedbackStyle : Resource
{
    [ExportGroup("Breathing Pulse")]
    [Export] public bool PulseEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,.3,.01")] public float ExpansionFraction { get; set; } = .12f;
    [Export(PropertyHint.Range, ".2,2,.05")] public float PulseSeconds { get; set; } = .8f;
    [Export(PropertyHint.Range, "0,1,.05")] public float BrightnessStrength { get; set; } = .15f;
    [Export(PropertyHint.Range, "0,.65,.05")] public float Irregularity { get; set; } = .4f;
    [ExportGroup("Dimensional Smoke")]
    [Export] public bool SmokeEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "4,40,1")] public int PuffCount { get; set; } = 18;
    [Export(PropertyHint.Range, "48,180,1")] public float PuffSize { get; set; } = 110;
    [Export(PropertyHint.Range, "0,.5,.05")] public float PuffSizeSpread { get; set; } = .4f;
    [Export(PropertyHint.Range, "2,8,.25")] public float LifetimeSeconds { get; set; } = 5;
    [Export(PropertyHint.Range, "4,60,1")] public float OutwardSpeed { get; set; } = 18;
    [Export(PropertyHint.Range, "0,15,1")] public float SwirlAcceleration { get; set; } = 3;
    [Export(PropertyHint.Range, "0,1,.05")] public float Opacity { get; set; } = .38f;
    [Export] public Color Tint { get; set; } = new(.52f, .22f, .76f, 1);
}
