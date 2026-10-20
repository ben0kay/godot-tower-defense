// Inspector-authored tracer appearance. Launch copies values, never a mutable per-shot Resource/material.
using Godot;

[GlobalClass]
public partial class ProjectileVisualStyle : Resource
{
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, "1,128,.5")] public float LengthPixels { get; set; } = 18;
    [Export(PropertyHint.Range, ".1,12,.1")] public float CoreThicknessPixels { get; set; } = 2;
    [Export(PropertyHint.Range, "1,4,.1")] public float GlowWidthMultiplier { get; set; } = 2.5f;
    [Export] public Color CoreColor { get; set; } = new(1, .98f, .8f);
    [Export] public Color BodyColor { get; set; } = new(1, .65f, .15f);
    [Export] public Color GlowColor { get; set; } = new(1, .4f, .05f, .2f);
    [Export] public bool BrightTip { get; set; } = true;
    [Export] public Texture2D Texture { get; set; }
    [ExportGroup("Hostile Energy Streak")]
    [Export] public bool Tapered { get; set; }
    [Export(PropertyHint.Range, "0,1,.01")] public float TailWidthFraction { get; set; } = .08f;
    [Export(PropertyHint.Range, "0,1,.01")] public float TailOpacity { get; set; } = .08f;
    [Export(PropertyHint.Range, "1,2,.05")] public float HeadWidthMultiplier { get; set; } = 1.25f;
    // =========================================================
    // Finite bounded values keep invalid Remote Inspector edits out of drawing APIs.
    public ProjectileVisualSnapshot Capture() => new(Enabled, WeaponVisualMath.Safe(LengthPixels, 1, 128),
        WeaponVisualMath.Safe(CoreThicknessPixels, .1f, 12), WeaponVisualMath.Safe(GlowWidthMultiplier, 1, 4),
        WeaponVisualMath.Color(CoreColor), WeaponVisualMath.Color(BodyColor), WeaponVisualMath.Color(GlowColor), BrightTip, Texture,
        Tapered, WeaponVisualMath.Safe(TailWidthFraction, 0, 1), WeaponVisualMath.Safe(TailOpacity, 0, 1),
        WeaponVisualMath.Safe(HeadWidthMultiplier, 1, 2));
}

public readonly record struct ProjectileVisualSnapshot(bool Enabled, float Length, float Width, float GlowWidth,
    Color Core, Color Body, Color Glow, bool BrightTip, Texture2D Texture,
    bool Tapered = false, float TailWidthFraction = .08f, float TailOpacity = .08f, float HeadWidthMultiplier = 1.25f);
