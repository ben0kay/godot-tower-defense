// Shared one-shot death puff settings. Visual scale is derived from a destroyed object's Sprite2D.
using Godot;

[GlobalClass]
public partial class DeathExplosionConfig : Resource
{
    [ExportGroup("Automatic Scaling")]
    [Export(PropertyHint.Range, "1,256,1")] public float ReferenceVisualSizePixels { get; set; } = 32f;
    [Export(PropertyHint.Range, "0.1,5,0.05")] public float GlobalSizeMultiplier { get; set; } = 1f;
    [Export(PropertyHint.Range, "0.1,5,0.05")] public float MinimumScale { get; set; } = 0.55f;
    [Export(PropertyHint.Range, "0.1,10,0.1")] public float MaximumScale { get; set; } = 3f;

    [ExportGroup("Burst")]
    [Export(PropertyHint.Range, "1,80,1")] public int SmokeParticleCount { get; set; } = 16;
    [Export(PropertyHint.Range, "1,40,1")] public int FlashParticleCount { get; set; } = 5;
    [Export(PropertyHint.Range, "0.05,3,0.05")] public float SmokeLifetimeSeconds { get; set; } = 0.7f;
    [Export(PropertyHint.Range, "0.05,1,0.01")] public float FlashLifetimeSeconds { get; set; } = 0.18f;

    [ExportGroup("Performance")]
    [Export(PropertyHint.Range, "1,500,1")] public int MaxConcurrentExplosions { get; set; } = 64;
}
