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
    [ExportGroup("Fighter Weapon (FD4)")]
    [Export(PropertyHint.Range, "16,1600,1")] public float WeaponRangePixels { get; set; } = 275;
    [Export(PropertyHint.Range, "0,1000,0.5")] public float DamagePerShot { get; set; } = 4;
    [Export(PropertyHint.Range, "0.1,30,0.1")] public float ShotsPerSecond { get; set; } = 5;
    [Export(PropertyHint.Range, "40,3000,10")] public float BulletSpeed { get; set; } = 1200;
    [Export(PropertyHint.Range, "0,1440,5")] public float GunTurnDegreesPerSecond { get; set; } = 540;
    [Export(PropertyHint.Range, "0,45,1")] public float AimToleranceDegrees { get; set; } = 10;
    [Export] public ProjectileVisualStyle ProjectileVisual { get; set; }
    [Export] public MuzzleFlashStyle MuzzleFlash { get; set; }
    public static float Safe(float value, float fallback, float min, float max) =>
        float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
}
