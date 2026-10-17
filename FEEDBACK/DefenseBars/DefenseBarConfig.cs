// Shared Inspector-configurable appearance for enemy and future tower defense bars.
// Logarithmic max-health scaling makes high-HP bars longer without growing indefinitely.
using Godot;

[GlobalClass]
public partial class DefenseBarConfig : Resource
{
    [ExportGroup("Sizing")]
    [Export(PropertyHint.Range, "12,80,1")] public float MinWidthPixels { get; set; } = 28f;
    [Export(PropertyHint.Range, "24,160,1")] public float MaxWidthPixels { get; set; } = 74f;
    [Export(PropertyHint.Range, "1,10000,1")] public float ReferenceHealth { get; set; } = 60f;
    [Export(PropertyHint.Range, "0,20,0.5")] public float PixelsPerHealthDoubling { get; set; } = 7f;
    [Export(PropertyHint.Range, "2,16,1")] public float HeightPixels { get; set; } = 5f;
    [Export(PropertyHint.Range, "0,3,1")] public float BorderPixels { get; set; } = 1f;
    [Export(PropertyHint.Range, "4,96,1")] public float VerticalOffsetPixels { get; set; } = 23f;

    [ExportGroup("Colors")]
    [Export] public Color BackgroundColor { get; set; } = new("#10232e");
    [Export] public Color BorderColor { get; set; } = new("#030d16");
    [Export] public Color HealthyColor { get; set; } = new("#53d4a4");
    [Export] public Color WarningColor { get; set; } = new("#f4bd61");
    [Export] public Color CriticalColor { get; set; } = new("#f36b72");

    [ExportGroup("Shield Meter")]
    [Export(PropertyHint.Range, "2,12,1")] public float ShieldHeightPixels { get; set; } = 4f;
    [Export(PropertyHint.Range, "0,12,1")] public float ShieldGapPixels { get; set; } = 2f;
    [Export] public Color ShieldColor { get; set; } = new("#ff865b");

    [ExportGroup("Thresholds")]
    [Export(PropertyHint.Range, "0.05,0.95,0.05")] public float WarningFraction { get; set; } = 0.5f;
    [Export(PropertyHint.Range, "0.05,0.95,0.05")] public float CriticalFraction { get; set; } = 0.25f;
}
