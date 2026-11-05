// Shared disabled-tower appearance; entirely cosmetic and independent of condition durations or power rules.
using Godot;

[GlobalClass]
public partial class TowerDisabledStyle : Resource
{
    [Export] public Color ArtTint { get; set; } = new(1f, 0.78f, 0.78f, 1f);
    [Export] public Color WarningColor { get; set; } = new(1f, 0.68f, 0.22f, 1f);
    [Export(PropertyHint.Range, "10,28,1")] public float WarningSizePixels { get; set; } = 16;
    [Export] public Vector2 CornerOffset { get; set; } = new(4, -4);
}
