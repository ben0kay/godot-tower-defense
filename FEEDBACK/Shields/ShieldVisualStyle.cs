// Inspector settings for the reusable shield aura; display only, never shield strength or regeneration.
using Godot;

[GlobalClass]
public partial class ShieldVisualStyle : Resource
{
    [ExportGroup("Shield Aura")]
    [Export] public Color Tint { get; set; } = new("#ff7352");
    [Export(PropertyHint.Range, "1,2,0.01")] public float SizeMultiplier { get; set; } = 1.65f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float IdleOpacity { get; set; } = 0.82f;

    [ExportGroup("Impact Flash")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float HitOpacity { get; set; } = 1.0f;
    [Export(PropertyHint.Range, "0.05,2,0.01")] public float HitFadeSeconds { get; set; } = 0.28f;

    [ExportGroup("Shield Break")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float BreakOpacity { get; set; } = 1.0f;
    [Export(PropertyHint.Range, "0.05,2,0.01")] public float BreakFadeSeconds { get; set; } = 0.38f;
}
