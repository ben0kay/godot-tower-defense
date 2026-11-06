// Shared portal opening quality/timing; disabling motion never bypasses the mechanical readiness delay.
using Godot;
[GlobalClass]
public partial class PortalMaterializationStyle : Resource
{
    [ExportGroup("Opening")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public bool ReducedMotion { get; set; }
    [Export(PropertyHint.Range, ".2,6,.1")] public float DurationSeconds { get; set; } = 1.1f;
    [Export(PropertyHint.Range, "0,.2,.01")] public float StartStaggerSeconds { get; set; }
    [Export] public bool WarningEnabled { get; set; } = true;
    [ExportGroup("Appearance")]
    [Export(PropertyHint.Range, "0,2,.1")] public float VortexIntensity { get; set; } = 1;
    [Export(PropertyHint.Range, "30,70,1")] public float RingRadius { get; set; } = 46;
    [Export(PropertyHint.Range, "0,2,.1")] public float FlashStrength { get; set; } = 1;
    [ExportGroup("Bounded Particles")]
    [Export(PropertyHint.Range, "0,96,1")] public int ParticleAmount { get; set; } = 72;
    [Export(PropertyHint.Range, "0,6,1")] public int MaxDetailedOpenings { get; set; } = 6;
    public static float Safe(float value, float fallback, float min, float max) =>
        float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
}
