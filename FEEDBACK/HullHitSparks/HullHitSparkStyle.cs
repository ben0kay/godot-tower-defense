// Shared Inspector settings for short, pooled hull-damage sparks. No gameplay damage or particles.
using Godot;

[GlobalClass]
public partial class HullHitSparkStyle : Resource
{
    [ExportGroup("Hull Hit Sparks")]
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, "16,512,16")] public int MaxActiveFragments { get; set; } = 320;
    [Export(PropertyHint.Range, "0.05,0.5,0.01")] public float MinLifetime { get; set; } = .12f;
    [Export(PropertyHint.Range, "0.05,0.5,0.01")] public float MaxLifetime { get; set; } = .2f;
    [Export(PropertyHint.Range, "10,250,1")] public float MinSpeed { get; set; } = 35f;
    [Export(PropertyHint.Range, "10,250,1")] public float MaxSpeed { get; set; } = 105f;
    [Export(PropertyHint.Range, "1,12,0.5")] public float FragmentLength { get; set; } = 4f;
    [Export(PropertyHint.Range, "0.5,3,0.1")] public float FragmentThickness { get; set; } = 1.3f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float Opacity { get; set; } = .95f;
}
