// Shared Inspector settings; runtime slots retain sanitized values rather than Resource copies.
using Godot;
[GlobalClass]
public partial class RicochetStyle : Resource
{
    #region Controls
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,100,1")] public float ChancePercent { get; set; } = 35;
    [Export(PropertyHint.Range, "0,1,.01")] public float SpeedRetention { get; set; } = .45f;
    [Export(PropertyHint.Range, "0,1,.01")] public float Deceleration { get; set; } = .85f;
    [Export(PropertyHint.Range, ".05,2,.01")] public float LifetimeSeconds { get; set; } = .55f;
    [Export(PropertyHint.Range, "0,20,1")] public float CurveDegrees { get; set; } = 12;
    [Export(PropertyHint.Range, "0,3,.1")] public float AngleJitterDegrees { get; set; } = 2;
    [Export(PropertyHint.Range, ".1,2,.05")] public float LengthMultiplier { get; set; } = .8f;
    [Export(PropertyHint.Range, "0,1,.01")] public float Opacity { get; set; } = .65f;
    [Export(PropertyHint.Range, "1,512,1")] public int MaxActiveRicochets { get; set; } = 128;
    // =========================================================
    // Inspector ranges alone do not reject NaN or programmatic values.
    public static float Safe(float value, float fallback, float min, float max) => float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
    #endregion
}
