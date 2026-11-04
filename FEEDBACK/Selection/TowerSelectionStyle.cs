// Inspector tuning for the shared selected-tower screen and world highlights.
using Godot;

[GlobalClass]
public partial class TowerSelectionStyle : Resource
{
    [ExportGroup("State Colours")]
    [Export] public Color NormalColor { get; set; } = new("#50d9dc");
    [Export] public Color DisabledColor { get; set; } = new("#f06b76");

    [ExportGroup("Screen Overlay")]
    [Export] public bool ScreenTintEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,0.2,0.005")] public float TintOpacity { get; set; } = 0.075f;
    [Export] public bool FaintGridEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "32,200,4")] public int GridSpacingPixels { get; set; } = 96;
    [Export(PropertyHint.Range, "0,0.25,0.005")] public float GridOpacity { get; set; } = 0.10f;
    [Export(PropertyHint.Range, "10,28,1")] public int NameFontSize { get; set; } = 18;
    [Export(PropertyHint.Range, "4,80,1")] public float NameRightOffsetPixels { get; set; } = 16f;

    [ExportGroup("World Hologram")]
    [Export] public bool HologramEnabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,32,1")] public float FootprintPaddingPixels { get; set; } = 8f;
    [Export(PropertyHint.Range, "4,32,1")] public float CornerLengthPixels { get; set; } = 14f;
    [Export(PropertyHint.Range, "0,0.25,0.005")] public float FootprintFillOpacity { get; set; } = 0.065f;
    [Export(PropertyHint.Range, "0.05,1,0.05")] public float BorderOpacity { get; set; } = 0.78f;
    [Export(PropertyHint.Range, "0.5,8,0.1")] public float PulseSeconds { get; set; } = 2.8f;
    [Export(PropertyHint.Range, "8,30,1")] public int RedrawHertz { get; set; } = 20;
}
