// ET1 shared Inspector tuning for lightweight enemy engine exhaust, independent of combat or navigation.
using Godot;

[GlobalClass]
public partial class EnemyThrusterConfig : Resource
{
    [ExportGroup("Engine Exhaust")]
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, "0.05,0.5,0.01")] public float FlameWidthToHullHeight { get; set; } = .17f;
    [Export(PropertyHint.Range, "0,30,0.5")] public float BaseFlameLengthPixels { get; set; } = 4f;
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float LengthPerHullSize { get; set; } = .12f;
    [Export(PropertyHint.Range, "0.01,0.4,0.01")] public float LengthPerSpeed { get; set; } = .10f;
    [Export(PropertyHint.Range, "8,100,1")] public float MaxFlameLengthPixels { get; set; } = 55f;
    [Export(PropertyHint.Range, "0,10,0.5")] public float RearNozzleInsetPixels { get; set; } = 1f;

    [ExportGroup("GPU Particle Budget")]
    [Export] public bool GpuParticlesEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "1,128,1")] public int MaxActiveGpuEmitters { get; set; } = 64;
    [Export(PropertyHint.Range, "1,8,1")] public int ParticlesPerShip { get; set; } = 4;
    [Export(PropertyHint.Range, "0.1,4,0.1")] public float ParticleSpeedMultiplier { get; set; } = 1f;

    [ExportGroup("Movement and Stealth")]
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float MinimumSpeedFraction { get; set; } = .05f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float StealthIntensity { get; set; } = .3f;
}
