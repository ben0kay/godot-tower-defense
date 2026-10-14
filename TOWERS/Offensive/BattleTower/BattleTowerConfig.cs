// Editable single-barrel starter turret stats for automatic targeting and firing.
// The shared DefaultBattleTowerConfig.tres is editable in the Godot Inspector.
using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class BattleTowerConfig : Resource
{
    [ExportGroup("Identity")]
    [Export] public string DisplayName { get; set; } = "Battle Tower";
    [Export] public string Description { get; set; } = "Versatile baseline turret: one barrel and balanced range/fire rate.";

    [ExportGroup("Construction")]
    [Export] public Godot.Collections.Array<ResourceCost> ConstructionCosts { get; set; } = new();

    [ExportGroup("Combat")]
    [Export(PropertyHint.Range, "32,1024,16")] public float RangePixels { get => _rangePixels; set => SetStat(ref _rangePixels, value); }
    [Export(PropertyHint.Range, "0,10,0.1")] public float ShotsPerSecond { get => _shotsPerSecond; set => SetStat(ref _shotsPerSecond, value); }
    [Export(PropertyHint.Range, "1,100,1")] public int DamagePerShot { get => _damagePerShot; set => SetStat(ref _damagePerShot, value); }
    [Export(PropertyHint.Range, "50,3000,25")] public float ProjectileSpeed { get => _projectileSpeed; set => SetStat(ref _projectileSpeed, value); }
    [ExportGroup("Aiming")]
    [Export(PropertyHint.Range, "0,720,5")] public float RotationSpeedDegrees { get => _rotationSpeedDegrees; set => SetStat(ref _rotationSpeedDegrees, value); }
    [Export(PropertyHint.Range, "0,180,1")] public float AimToleranceDegrees { get => _aimToleranceDegrees; set => SetStat(ref _aimToleranceDegrees, value); }
    [Export] public bool RotateWhileTracking { get; set; } = true;

    [ExportGroup("Targeting")]
    [Export] public TowerTargeting.Priority TargetPriority { get; set; } = TowerTargeting.Priority.First;
    [Export(PropertyHint.Range, "0.05,2,0.05")] public float TargetSearchInterval { get => _targetSearchInterval; set => SetStat(ref _targetSearchInterval, value); }

    #region Stat Change Notifications
    private float _rangePixels = 256f;
    private float _shotsPerSecond = 1f;
    private int _damagePerShot = 12;
    private float _projectileSpeed = 600f;
    private float _rotationSpeedDegrees = 180f;
    private float _aimToleranceDegrees = 12f;
    private float _targetSearchInterval = 0.15f;

    // =========================================================
    // Inspector and C# edits invalidate listening instances without per-frame polling.
    private void SetStat<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value; EmitChanged();
    }
    #endregion
}
