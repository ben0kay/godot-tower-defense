// CV1 shared construction-site hologram settings; all footprints use the same Inspector resource.
using Godot;

[GlobalClass]
public partial class ConstructionSiteVisualConfig : Resource
{
    #region Enable and Timing
    [ExportGroup("Display")]
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, "8,48,1")] public float RefreshHz { get; set; } = 24f;
    [Export(PropertyHint.Range, "1,256,1")] public int MaxVisibleSites { get; set; } = 128;
    #endregion

    #region Holographic Palette
    [ExportGroup("Colors and Opacity")]
    [Export] public Color Tint { get; set; } = new Color(.25f, .82f, 1f);
    [Export(PropertyHint.Range, "0,1,.01")] public float WaitingOpacity { get; set; } = .43f;
    [Export(PropertyHint.Range, "0,1,.01")] public float BuildingOpacity { get; set; } = .68f;
    [Export(PropertyHint.Range, "0,1,.01")] public float BlueprintOpacity { get; set; } = .13f;
    #endregion

    #region Shape and Motion
    [ExportGroup("Shape and Animation")]
    [Export(PropertyHint.Range, "0,12,.5")] public float InsetPixels { get; set; } = 4f;
    [Export(PropertyHint.Range, "2,20,.5")] public float CornerLength { get; set; } = 11f;
    [Export(PropertyHint.Range, "0,40,.5")] public float RotationDegreesPerSecond { get; set; } = 18f;
    [Export(PropertyHint.Range, "0,3,.05")] public float PerimeterCyclesPerSecond { get; set; } = .23f;
    [Export(PropertyHint.Range, "0,5,.1")] public float PulseHz { get; set; } = 1.2f;
    [Export(PropertyHint.Range, "0,.5,.01")] public float PulseDepth { get; set; } = .14f;
    #endregion
}
