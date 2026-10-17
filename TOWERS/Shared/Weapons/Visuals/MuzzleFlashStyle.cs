// Reusable primitive families, not tower identities. Each tower owns a read-only .tres preset.
using Godot;
public enum MuzzleFlashShape { Cone, Starburst, Ring, Texture }

[GlobalClass]
public partial class MuzzleFlashStyle : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public MuzzleFlashShape Shape { get; set; } = MuzzleFlashShape.Starburst;
    [Export(PropertyHint.Range, ".01,1,.01")] public float DurationSeconds { get; set; } = .08f;
    [Export(PropertyHint.Range, "1,64,.5")] public float LengthPixels { get; set; } = 13;
    [Export(PropertyHint.Range, "1,48,.5")] public float WidthPixels { get; set; } = 9;
    [Export] public Color CoreColor { get; set; } = new(1, 1, .8f);
    [Export] public Color OuterColor { get; set; } = new(1, .55f, .05f, .6f);
    [Export(PropertyHint.Range, "0,1,.05")] public float Opacity { get; set; } = 1;
    [Export] public Texture2D Texture { get; set; }
    // =========================================================
    // Snapshot value types keep animation state away from shared authored resources.
    public MuzzleFlashSnapshot Capture() => new(Enabled, System.Enum.IsDefined(Shape) ? Shape : MuzzleFlashShape.Starburst,
        WeaponVisualMath.Safe(DurationSeconds, .01f, 1), WeaponVisualMath.Safe(LengthPixels, 1, 64),
        WeaponVisualMath.Safe(WidthPixels, 1, 48), WeaponVisualMath.Color(CoreColor), WeaponVisualMath.Color(OuterColor),
        WeaponVisualMath.Safe(Opacity, 0, 1), Texture);
}
public readonly record struct MuzzleFlashSnapshot(bool Enabled, MuzzleFlashShape Shape, float Duration,
    float Length, float Width, Color Core, Color Outer, float Opacity, Texture2D Texture);
