// Authored first-fighter hull/reserve/flight settings; no weapon, autopilot or magical station service.
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
    public static float Safe(float value, float fallback, float min, float max) =>
        float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
}
