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
    [ExportGroup("Large Faint Clouds")]
    [Export] public bool LargeCloudsEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "2,12,1")] public int LargeCloudCount { get; set; } = 8;
    [Export(PropertyHint.Range, "1.5,4,.1")] public float LargeCloudSizeMultiplier { get; set; } = 2.2f;
    [Export(PropertyHint.Range, "4,12,.5")] public float LargeCloudLifetime { get; set; } = 8;
    [Export(PropertyHint.Range, "0,.3,.01")] public float LargeCloudOpacity { get; set; } = .12f;
    [Export] public Color LargeCloudTint { get; set; } = new(.26f, .10f, .38f, 1);
    [ExportGroup("Smoke Colour")]
    [Export] public Color Tint { get; set; } = new(.52f, .22f, .76f, 1);
}
