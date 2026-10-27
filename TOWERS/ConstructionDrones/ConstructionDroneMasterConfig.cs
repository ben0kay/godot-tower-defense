// Shared builder settings; flight is independent of ground routing and work remains owned by ConstructionWorkManager.
using Godot;
[GlobalClass]
public partial class ConstructionDroneMasterConfig : Resource
{
    #region Feature and Dispatch
    [Export] public bool Enabled { get; set; } = true;
    [Export] public bool ShowVisuals { get; set; } = true;
    [Export] public bool RequireDroneToBuild { get; set; }
    [Export(PropertyHint.Range, "1,64,1")] public int MaxActiveDrones { get; set; } = 8;
    [Export(PropertyHint.Range, "1,16,1")] public int DispatchesPerTick { get; set; } = 4;
    #endregion
    #region Flight and Hologram
    [Export(PropertyHint.Range, "32,2000,1")] public float TravelSpeed { get; set; } = 280;
    [Export(PropertyHint.Range, "1,24,1")] public float ArrivalRadius { get; set; } = 8;
    [Export(PropertyHint.Range, "0,1,.05")] public float BeamDeploymentSeconds { get; set; } = .25f;
    [Export(PropertyHint.Range, "0,20,1")] public float HoverRadius { get; set; } = 12;
    [Export(PropertyHint.Range, "16,96,1")] public float HoverHeight { get; set; } = 48;
    [Export(PropertyHint.Range, ".05,.5,.01")] public float BeamOpacity { get; set; } = .26f;
    [Export] public Color BeamColor { get; set; } = new(.2f, .9f, 1);
    #endregion
    #region Organic Movement
    [ExportGroup("Organic Movement")]
    [Export(PropertyHint.Range, "0,400,1")] public float FlightCurvePixels { get; set; } = 100;
    [Export(PropertyHint.Range, "30,720,5")] public float TurnDegreesPerSecond { get; set; } = 240;
    [Export(PropertyHint.Range, "8,160,1")] public float OrbitRadius { get; set; } = 28;
    [Export(PropertyHint.Range, "5,180,1")] public float OrbitDegreesPerSecond { get; set; } = 55;
    [Export(PropertyHint.Range, ".05,10,.05")] public float HoverPauseMinimum { get; set; } = .6f;
    [Export(PropertyHint.Range, ".05,15,.05")] public float HoverPauseMaximum { get; set; } = 1.4f;
    #endregion
    #region CD3 Hologram Animation
    [Export(PropertyHint.Range, "0,8,1")] public int BeamScanBands { get; set; } = 4;
    [Export(PropertyHint.Range, "0,6,.1")] public float BeamScanCyclesPerSecond { get; set; } = 1.5f;
    [Export(PropertyHint.Range, "0,6,.1")] public float BeamPulseHz { get; set; } = 2f;
    [Export(PropertyHint.Range, "0,.35,.01")] public float BeamPulseDepth { get; set; } = .13f;
    #endregion
    #region CD3 Completion Flash
    [Export] public bool CompletionFlashEnabled { get; set; } = true;
    [Export(PropertyHint.Range, ".1,1,.01")] public float CompletionFlashSeconds { get; set; } = .28f;
    [Export(PropertyHint.Range, "0,1,.01")] public float CompletionFlashOpacity { get; set; } = .65f;
    #endregion
    #region Validation
    // =========================================================
    // Clamp programmatic and Remote Inspector inputs too, not just the editor's sliders.
    public static float Safe(float value, float fallback, float min, float max) => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
    #endregion
}
