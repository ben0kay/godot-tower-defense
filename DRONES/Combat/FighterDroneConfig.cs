// Authored first-fighter hull/reserve/flight settings; autopilot movement only; no weapons or station service.
using Godot;
[GlobalClass]
public partial class FighterDroneConfig : Resource
{
    [Export(PropertyHint.Range, "1,10000,1")] public float MaxHealth { get; set; } = 300;
    [Export(PropertyHint.Range, "1,10000,1")] public int AmmoCapacity { get; set; } = 200;
    [Export(PropertyHint.Range, "32,2000,1")] public float FlightSpeed { get; set; } = 340;
    [Export(PropertyHint.Range, "32,10000,10")] public float Acceleration { get; set; } = 1000;
    [Export(PropertyHint.Range, "32,10000,10")] public float Deceleration { get; set; } = 1000;
    [Export(PropertyHint.Range, "30,720,1")] public float TurnDegreesPerSecond { get; set; } = 300;
    [Export(PropertyHint.Range, "64,600,1")] public float LaunchDistance { get; set; } = 180;
    [ExportGroup("Auto-pilot")]
    [Export(PropertyHint.Range, "0.1,2,0.05")] public float TargetSearchSeconds { get; set; } = .3f;
    [Export(PropertyHint.Range, "32,1200,1")] public float AttackDistance { get; set; } = 160;
    [Export(PropertyHint.Range, "24,600,1")] public float OrbitRadius { get; set; } = 100;
    [Export(PropertyHint.Range, "24,1000,1")] public float ManoeuvreSpeed { get; set; } = 170;
    [Export(PropertyHint.Range, "0,3,0.1")] public float InterceptLeadSeconds { get; set; } = .8f;
    public static float Safe(float value, float fallback, float min, float max) =>
        float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
}
