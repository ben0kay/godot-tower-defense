// Standalone VE5C bloom settings; no changes to gameplay or project rendering configuration.
using Godot;

[GlobalClass]
public partial class BloomTestConfig : Resource
{
    [ExportGroup("Cinematic Blue Bloom")]
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,2,0.01")] public float Intensity { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float Bloom { get; set; } = 0.055f;
    [Export(PropertyHint.Range, "0,2,0.01")] public float Threshold { get; set; } = 0.82f;
    [ExportGroup("Backdrop")]
    [Export] public Color BackgroundTint { get; set; } = new Color("#091629");
    [Export] public Color HaloTint { get; set; } = new Color("#3da8db");
    [Export(PropertyHint.Range, "0,1,0.01")] public float HaloOpacity { get; set; } = 0.11f;
}
