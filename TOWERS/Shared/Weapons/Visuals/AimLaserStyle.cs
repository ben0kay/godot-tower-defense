// Optional thin targeting aid. It draws along the barrel and never performs queries or applies damage.
using Godot;
[GlobalClass]
public partial class AimLaserStyle : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export] public Color Color { get; set; } = new(1, .12f, .15f, .45f);
    [Export(PropertyHint.Range, ".1,3,.1")] public float WidthPixels { get; set; } = .7f;
    [Export(PropertyHint.Range, "0,3,.1")] public float TipRadiusPixels { get; set; } = .9f;
    // =========================================================
    // Aiming appearance is independent of range/accuracy/cooldown and cannot modify them.
    public AimLaserSnapshot Capture() => new(Enabled, WeaponVisualMath.Color(Color),
        WeaponVisualMath.Safe(WidthPixels, .1f, 3), WeaponVisualMath.Safe(TipRadiusPixels, 0, 3));
}
public readonly record struct AimLaserSnapshot(bool Enabled, Color Color, float Width, float TipRadius);
