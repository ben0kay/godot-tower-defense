// Inspector-editable motion and typography shared by level, rank and future status callouts.
using Godot;

[GlobalClass]
public partial class WorldCalloutStyle : Resource
{
    [ExportGroup("Animation")]
    [Export(PropertyHint.Range, "0.1,4,0.05")] public float DurationSeconds { get; set; } = 1.35f;
    [Export(PropertyHint.Range, "0,80,1")] public float RisePixels { get; set; } = 20f;

    [ExportGroup("Scale")]
    [Export(PropertyHint.Range, "0.5,1.5,0.01")] public float StartScale { get; set; } = 1f;
    [Export(PropertyHint.Range, "1,1.5,0.01")] public float EndScale { get; set; } = 1.14f;

    [ExportGroup("Typography")]
    [Export(PropertyHint.Range, "8,32,1")] public int FontSize { get; set; } = 13;
}
