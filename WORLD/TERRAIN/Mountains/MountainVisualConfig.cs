// Shared load-time mountain appearance; tile metadata selects where this visual applies.
using Godot;

[GlobalClass]
public partial class MountainVisualConfig : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public int Seed { get; set; } = 713;
    [Export(PropertyHint.Range, "0.2,2,0.05")] public float HeightCells { get; set; } = 1.22f;
    [Export(PropertyHint.Range, "0,0.15,0.01")] public float EdgeVariationCells { get; set; } = 0.08f;
    [Export(PropertyHint.Range, "0,0.35,0.01")] public float RockDetailStrength { get; set; } = 0.14f;
    [Export] public Color RockColor { get; set; } = new("a66e50");
    [Export] public Color ShadowColor { get; set; } = new(0.12f, 0.07f, 0.06f, 0.28f);
}

