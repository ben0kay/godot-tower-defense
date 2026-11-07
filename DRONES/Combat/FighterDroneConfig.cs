// Fighter hull, fixed-gun attack-run and flight tuning; station resupply remains separate.
using Godot;
[GlobalClass]
public partial class FighterDroneConfig : Resource
{
    [Export(PropertyHint.Range, "1,10000,1")] public float MaxHealth { get; set; } = 300;
    [Export(PropertyHint.Range, "1,10000,1")] public int AmmoCapacity { get; set; } = 200;
    [Export(PropertyHint.Range, "32,2000,1")] public float FlightSpeed { get; set; } = 300;
    [Export(PropertyHint.Range, "32,10000,10")] public float Acceleration { get; set; } = 600;
    [Export(PropertyHint.Range, "32,10000,10")] public float Deceleration { get; set; } = 500;
    [Export(PropertyHint.Range, "30,720,1")] public float TurnDegreesPerSecond { get; set; } = 90;
    [Export(PropertyHint.Range, "64,600,1")] public float LaunchDistance { get; set; } = 180;
    [ExportGroup("Auto-pilot")]
    [Export(PropertyHint.Range, "0.1,2,0.05")] public float TargetSearchSeconds { get; set; } = .3f;
    [Export(PropertyHint.Range, "32,1200,1")] public float AttackDistance { get; set; } = 220;
    [Export(PropertyHint.Range, "24,600,1")] public float OrbitRadius { get; set; } = 210;
    [Export(PropertyHint.Range, "24,1000,1")] public float ManoeuvreSpeed { get; set; } = 220;
    [Export(PropertyHint.Range, "0,3,0.1")] public float InterceptLeadSeconds { get; set; } = .8f;
    [Export(PropertyHint.Range, "80,800,1")] public float PassExitDistance { get; set; } = 220;
    [Export(PropertyHint.Range, ".1,2,.05")] public float SteeringResponseSeconds { get; set; } = .65f;
    [Export(PropertyHint.Range, ".1,2,.05")] public float AccelerationResponseSeconds { get; set; } = .6f;
    [Export(PropertyHint.Range, "0,5,.1")] public float TargetHoldSeconds { get; set; } = 1.5f;
    [ExportGroup("Auto-patrol")]
    [Export(PropertyHint.Range, "32,500,1")] public float PatrolWidth { get; set; } = 96;
    [Export(PropertyHint.Range, "32,600,1")] public float PatrolSpeed { get; set; } = 145;
    [Export(PropertyHint.Range, "30,720,1")] public float PatrolTurnDegreesPerSecond { get; set; } = 180;
    [Export(PropertyHint.Range, "16,160,1")] public float PatrolArrivalTolerance { get; set; } = 48;
    [Export(PropertyHint.Range, "48,300,1")] public float PatrolLeash { get; set; } = 110;
    [ExportGroup("Fighter Weapon (FD4)")]
    [Export(PropertyHint.Range, "16,1600,1")] public float WeaponRangePixels { get; set; } = 275;
    [Export(PropertyHint.Range, "0,1000,0.5")] public float DamagePerShot { get; set; } = 4;
    [Export(PropertyHint.Range, "0.1,30,0.1")] public float ShotsPerSecond { get; set; } = 9;
    [Export(PropertyHint.Range, "40,3000,10")] public float BulletSpeed { get; set; } = 1200;
    [Export(PropertyHint.Range, "0,360,1")] public float FiringArcDegrees { get; set; } = 180;
    [Export(PropertyHint.Range, ".1,2,.05")] public float WeaponSearchSeconds { get; set; } = .2f;
    [Export(PropertyHint.Range, "0,2,.05")] public float WeaponTargetHoldSeconds { get; set; } = .25f;
    [Export(PropertyHint.Range, "0,.9,.05")] public float AutoReturnAmmoFraction { get; set; } = .25f;
    [Export(PropertyHint.Range, "0,.9,.05")] public float AutoReturnHealthFraction { get; set; } = .35f;
    [Export] public ProjectileVisualStyle ProjectileVisual { get; set; }
    [Export] public MuzzleFlashStyle MuzzleFlash { get; set; }
    public static float Safe(float value, float fallback, float min, float max) =>
        float.IsFinite(value) ? Mathf.Clamp(value, min, max) : fallback;
}
