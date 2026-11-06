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

    [ExportGroup("Site Telemetry")]
    [Export] public bool TelemetryEnabled { get; set; } = true;
    [Export] public Font TelemetryFont { get; set; }
    [Export(PropertyHint.Range, "8,18,1")] public int TelemetryFontSize { get; set; } = 11;
    [Export] public Color TelemetryColor { get; set; } = new(.8f, .98f, 1, .95f);
    [Export] public Color TelemetryBackground { get; set; } = new(.02f, .06f, .1f, .75f);
    [Export] public Vector2 TelemetryOffset { get; set; } = new(8, -14);
    [Export(PropertyHint.Range, "88,220,1")] public float TelemetryWidth { get; set; } = 132;
    [ExportGroup("Circular Site Progress")]
    [Export(PropertyHint.Range, ".4,1.5,.05")] public float TelemetryScale { get; set; } = .8f;
    [Export(PropertyHint.Range, "26,56,1")] public float TelemetryRingRadius { get; set; } = 40;
    [Export(PropertyHint.Range, "0,180,1")] public float TelemetryOuterDegreesPerSecond { get; set; } = 30;
    [Export(PropertyHint.Range, "0,180,1")] public float TelemetryMiddleDegreesPerSecond { get; set; } = 48;
    [Export(PropertyHint.Range, "0,180,1")] public float TelemetryInnerDegreesPerSecond { get; set; } = 72;
    [Export(PropertyHint.Range, "20,400,10")] public float TelemetryCountUpPerSecond { get; set; } = 160;
    [Export(PropertyHint.Range, ".8,1.2,.05")] public float CompletionReadoutSeconds { get; set; } = 1;
    [Export(PropertyHint.Range, "1,64,1")] public int MaxCompletionReadouts { get; set; } = 32;
    [ExportGroup("Site Acquisition")]
    [Export] public bool AcquisitionCueEnabled { get; set; } = true;
    [Export] public Color AcquisitionTint { get; set; } = new(.65f, 1, 1, 1);

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
