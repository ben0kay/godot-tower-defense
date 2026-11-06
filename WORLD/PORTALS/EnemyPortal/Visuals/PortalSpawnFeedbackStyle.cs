// Portal-local cosmetic spawn response; no readiness, spawn timing or navigation settings.
using Godot;
[GlobalClass]
public partial class PortalSpawnFeedbackStyle : Resource
{
    [ExportGroup("Breathing Pulse")]
    [Export] public bool PulseEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,.3,.01")] public float ExpansionFraction { get; set; } = .16f;
    [Export(PropertyHint.Range, ".2,2,.05")] public float PulseSeconds { get; set; } = .56f;
    [Export(PropertyHint.Range, "0,1,.05")] public float BrightnessStrength { get; set; } = .22f;
    [Export(PropertyHint.Range, "0,.65,.05")] public float Irregularity { get; set; } = .55f;
    [ExportGroup("Dimensional Smoke")]
    [Export] public bool SmokeEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "4,40,1")] public int PuffCount { get; set; } = 22;
    [Export(PropertyHint.Range, "48,240,1")] public float PuffSize { get; set; } = 125;
    [Export(PropertyHint.Range, "0,.5,.05")] public float PuffSizeSpread { get; set; } = .4f;
    [Export(PropertyHint.Range, "2,8,.25")] public float LifetimeSeconds { get; set; } = 5.5f;
    [Export(PropertyHint.Range, "4,80,1")] public float OutwardSpeed { get; set; } = 26;
    [Export(PropertyHint.Range, "0,20,1")] public float SwirlAcceleration { get; set; } = 6;
    [Export(PropertyHint.Range, "0,1,.05")] public float Opacity { get; set; } = .38f;
    [ExportGroup("Large Faint Clouds")]
    [Export] public bool LargeCloudsEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "2,12,1")] public int LargeCloudCount { get; set; } = 10;
    [Export(PropertyHint.Range, "1.5,4,.1")] public float LargeCloudSizeMultiplier { get; set; } = 2.7f;
    [Export(PropertyHint.Range, "4,12,.5")] public float LargeCloudLifetime { get; set; } = 9;
    [Export(PropertyHint.Range, "0,.3,.01")] public float LargeCloudOpacity { get; set; } = .11f;
    [Export] public Color LargeCloudTint { get; set; } = new(.26f, .10f, .38f, 1);
    [ExportGroup("Smoke Colour")]
    [Export] public Color Tint { get; set; } = new(.52f, .22f, .76f, 1);
}
