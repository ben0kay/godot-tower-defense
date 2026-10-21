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
    [Export(PropertyHint.Range, "32,2000,1")] public float TravelSpeed { get; set; } = 420;
    [Export(PropertyHint.Range, "1,24,1")] public float ArrivalRadius { get; set; } = 8;
    [Export(PropertyHint.Range, "0,1,.05")] public float BeamDeploymentSeconds { get; set; } = .25f;
    [Export(PropertyHint.Range, "0,20,1")] public float HoverRadius { get; set; } = 12;
    [Export(PropertyHint.Range, "16,96,1")] public float HoverHeight { get; set; } = 48;
    [Export(PropertyHint.Range, ".05,.5,.01")] public float BeamOpacity { get; set; } = .26f;
    [Export] public Color BeamColor { get; set; } = new(.2f, .9f, 1);
    // =========================================================
    // Clamp programmatic and Remote Inspector inputs too, not just the editor's sliders.
    public static float Safe(float value, float fallback, float min, float max) => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
    #endregion
}
