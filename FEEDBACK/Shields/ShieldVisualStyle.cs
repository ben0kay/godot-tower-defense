// Inspector settings for the reusable shield aura; display only, never shield strength or regeneration.
using Godot;

[GlobalClass]
public partial class ShieldVisualStyle : Resource
{
    [ExportGroup("Shield Aura")]
    [Export] public Color Tint { get; set; } = new("#ff666d");
    [Export(PropertyHint.Range, "1,2,0.01")] public float SizeMultiplier { get; set; } = 1.23f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float IdleOpacity { get; set; } = 0.32f;

    [ExportGroup("Impact Flash")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float HitOpacity { get; set; } = 0.82f;
    [Export(PropertyHint.Range, "0.05,2,0.01")] public float HitFadeSeconds { get; set; } = 0.24f;

    [ExportGroup("Shield Break")]
    [Export(PropertyHint.Range, "0,1,0.01")] public float BreakOpacity { get; set; } = 0.95f;
    [Export(PropertyHint.Range, "0.05,2,0.01")] public float BreakFadeSeconds { get; set; } = 0.32f;
}
