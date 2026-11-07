// Reusable point-light art settings. Radius is measured in world pixels at unit parent scale.
using Godot;

[GlobalClass]
public partial class LightSourceConfig : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public Color Tint { get; set; } = Colors.White;
    [Export(PropertyHint.Range, "0,2,0.01,or_greater")] public float Energy { get; set; } = 0.5f;
    [Export(PropertyHint.Range, "1,512,1,or_greater")] public float RadiusPixels { get; set; } = 192;
    [Export] public Vector2 LocalOffset { get; set; }
}
