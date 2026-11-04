// Inspector-editable motion and typography shared by level, rank and future status callouts.
using Godot;

[GlobalClass]
public partial class WorldCalloutStyle : Resource
{
    [ExportGroup("Animation")]
    [Export(PropertyHint.Range, "0.1,4,0.05")] public float DurationSeconds { get; set; } = 1.35f;
    [Export(PropertyHint.Range, "0.05,2,0.05")] public float FadeSeconds { get; set; } = 0.4f;
    [Export(PropertyHint.Range, "0,80,1")] public float RisePixels { get; set; } = 20f;

    [ExportGroup("Typography")]
    [Export(PropertyHint.Range, "8,32,1")] public int FontSize { get; set; } = 13;
}
