// Map dimensions and visual grid settings for the first 2D battlefield.
// Tune this resource via DefaultWorldConfig.tres in the Inspector.
using Godot;

[GlobalClass]
public partial class WorldMasterConfig : Resource
{
    [ExportGroup("Grid Dimensions")]
    [Export(PropertyHint.Range, "8,500,1")] public int WidthCells { get; set; } = 100;
    [Export(PropertyHint.Range, "8,500,1")] public int HeightCells { get; set; } = 80;
    [Export(PropertyHint.Range, "16,256,8")] public int CellSize { get; set; } = 64;

    [ExportGroup("Grid Visuals")]
    [Export] public bool ShowGrid { get; set; } = true;
    [Export] public bool ShowHoverCell { get; set; } = true;
    [Export] public Color GridColor { get; set; } = new Color(0.17f, 0.34f, 0.43f, 0.36f);
    [Export] public Color BorderColor { get; set; } = new Color(0.34f, 0.79f, 0.89f, 0.72f);
    [Export] public Color HoverColor { get; set; } = new Color(0.22f, 0.70f, 0.85f, 0.16f);
}
