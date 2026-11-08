// Shared tuning for positional camera feedback, in screen pixels and world-space influence units.
using Godot;

[GlobalClass]
public partial class CameraShakeSettings : Resource
{
    public enum DistanceFalloff { Smoothstep, Smootherstep }

    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, "0,32,0.1")] public float MaximumDisplacement { get; set; } = 8f;
    [Export(PropertyHint.Range, "0,32,0.1")] public float DefaultIntensity { get; set; } = 1.5f;
    [Export(PropertyHint.Range, "0.01,1,0.01")] public float DefaultDuration { get; set; } = .12f;
    [Export(PropertyHint.Range, "0,2000,10,or_greater")] public float DefaultRadius { get; set; } = 400f;
    [Export] public DistanceFalloff Falloff { get; set; } = DistanceFalloff.Smoothstep;
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float SuppressionSeconds { get; set; } = .08f;
    [Export(PropertyHint.Range, "0.005,0.1,0.005")] public float AttackSeconds { get; set; } = .025f;
    [Export(PropertyHint.Range, "1,30,0.5")] public float FrequencyHz { get; set; } = 14f;
}
