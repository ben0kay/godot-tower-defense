// Shared, Inspector-editable RTS-style camera configuration.
// Edit DefaultCameraConfig.tres rather than distributing numeric settings.
using Godot;

[GlobalClass]
public partial class CameraMasterConfig : Resource
{
    [ExportGroup("Movement")]
    [Export(PropertyHint.Range, "100,4000,50")] public float PanSpeed { get; set; } = 1300f;
    [Export(PropertyHint.Range, "0,40,0.5")] public float PanSmoothing { get; set; } = 14f;

    [ExportGroup("Zoom")]
    [Export(PropertyHint.Range, "0.1,2,0.05")] public float MinimumZoom { get; set; } = 0.4f;
    [Export(PropertyHint.Range, "0.5,5,0.1")] public float MaximumZoom { get; set; } = 2f;
    [Export(PropertyHint.Range, "0.1,3,0.05")] public float StartingZoom { get; set; } = 1f;
    [Export(PropertyHint.Range, "1.01,1.5,0.01")] public float ZoomStep { get; set; } = 1.15f;

    [ExportGroup("Map Bounds")]
    [Export] public bool KeepInsideMap { get; set; } = true;
}
