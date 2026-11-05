// Optional authored capacitor settings; quantities and rates stay separate from combat stats.
using Godot;
using System;

[GlobalClass]
public partial class TowerPowerConfig : Resource
{
    private double _capacity = 100, _recharge = 10, _idle, _active = 12, _initial, _restart = .15;
    [Export(PropertyHint.Range, "0.01,1000,1,or_greater")] public double Capacity { get => _capacity; set => Set(ref _capacity, value); }
    [Export(PropertyHint.Range, "0,100,0.1,or_greater")] public double RechargeRatePerSecond { get => _recharge; set => Set(ref _recharge, value); }
    [Export(PropertyHint.Range, "0,100,0.1,or_greater")] public double IdleDrawPerSecond { get => _idle; set => Set(ref _idle, value); }
    [Export(PropertyHint.Range, "0,100,0.1,or_greater")] public double ActiveDrawPerSecond { get => _active; set => Set(ref _active, value); }
    [Export(PropertyHint.Range, "0,1,0.01")] public double InitialFillFraction { get => _initial; set => Set(ref _initial, value); }
    [Export(PropertyHint.Range, "0.01,1,0.01")] public double RestartFraction { get => _restart; set => Set(ref _restart, value); }
    // =========================================================
    // Resource edits wake the same runtime service; initial fill is only applied on first creation.
    private void Set(ref double field, double value) { if (field == value) return; field = value; EmitChanged(); }
    // =========================================================
    // Zero capacity is invalid; omit Power entirely for a non-power tower. Zero draw never disables a tower.
    public PowerCapacitorSettings Settings()
    {
        if (!double.IsFinite(Capacity) || Capacity <= 0 || !ValidRate(RechargeRatePerSecond) || !ValidRate(IdleDrawPerSecond) || !ValidRate(ActiveDrawPerSecond)
            || !double.IsFinite(InitialFillFraction) || InitialFillFraction < 0 || InitialFillFraction > 1
            || !double.IsFinite(RestartFraction) || RestartFraction <= 0 || RestartFraction > 1)
            throw new InvalidOperationException("Capacitor needs positive finite capacity, nonnegative finite rates and valid fill/restart fractions.");
        return new(Capacity, RechargeRatePerSecond, IdleDrawPerSecond, ActiveDrawPerSecond, InitialFillFraction, RestartFraction);
    }
    private static bool ValidRate(double value) => double.IsFinite(value) && value >= 0;
}
public readonly record struct PowerCapacitorSettings(double Capacity, double Recharge, double Idle, double Active, double InitialFill, double Restart);
