// Pure stored-energy arithmetic: prepaid attacks, idle expenditure and hysteretic restart.
using System;
using System.IO;

public sealed class PowerCapacitor
{
    public PowerCapacitorSettings Settings { get; private set; }
    public double Energy { get; private set; }
    public double ActiveSecondsRemaining { get; private set; }
    public bool RecoveryRequired { get; private set; }
    public double RestartEnergy { get; private set; }
    public bool UsesEnergy => Settings.Idle > 0 || Settings.Active > 0;
    public PowerCapacitor(PowerCapacitorSettings settings)
    { Settings = settings; Energy = settings.Capacity * settings.InitialFill; Reconcile(); }
    // =========================================================
    // Upgrades/config edits retain absolute stored energy, never reapply the initial fill.
    public void Configure(PowerCapacitorSettings settings)
    { Settings = settings; Energy = Math.Min(Energy, settings.Capacity); RestartEnergy = 0; Reconcile(); }
    // =========================================================
    // Each projectile prepays one firing interval at TOTAL active draw, replacing idle for that interval.
    public bool TryPayAttack(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        double cost = Settings.Active * seconds;
        if (!double.IsFinite(cost) || cost > Energy)
        { RecoveryRequired = true; RestartEnergy = double.IsFinite(cost) ? cost : double.MaxValue; return false; }
        Energy = Math.Max(0, Energy - cost);
        ActiveSecondsRemaining = Math.Max(ActiveSecondsRemaining, seconds);
        Reconcile(); return true;
    }
    // =========================================================
    // Caller has capped continuous work by available energy; spend once and retain the paid idle exclusion.
    internal void PayWork(double seconds)
    {
        Energy = Math.Max(0, Energy - Settings.Active * seconds);
        ActiveSecondsRemaining = Math.Max(ActiveSecondsRemaining, seconds);
        Reconcile();
    }
    // =========================================================
    // No global wave flag: only time outside a paid attack interval incurs the idle rate.
    public void Advance(double seconds, bool idleAllowed)
    {
        double idleSeconds = Math.Max(0, seconds - ActiveSecondsRemaining);
        ActiveSecondsRemaining = Math.Max(0, ActiveSecondsRemaining - seconds);
        if (idleAllowed) Energy = Math.Max(0, Energy - Settings.Idle * idleSeconds);
        Reconcile();
    }
    // =========================================================
    // The grid owns credited amounts; this class cannot create source energy or overflow capacity.
    public void Credit(double amount) { Energy = Math.Min(Settings.Capacity, Energy + Math.Max(0, amount)); Reconcile(); }
    private void Reconcile()
    {
        if (!UsesEnergy) { RecoveryRequired = false; RestartEnergy = 0; return; }
        if (Energy <= 0) RecoveryRequired = true;
        if (RecoveryRequired && Energy >= Math.Max(Settings.Capacity * Settings.Restart, RestartEnergy))
        { RecoveryRequired = false; RestartEnergy = 0; }
    }
    // =========================================================
    // Also used to roll back a reservation if ammunition rejects the shot, without energy loss.
    public PowerSaveData Capture(int priority) => new(Energy, priority, RecoveryRequired, RestartEnergy, ActiveSecondsRemaining);
    public void Restore(PowerSaveData data)
    {
        if (!double.IsFinite(data.Energy) || data.Energy < 0 || data.Energy > Settings.Capacity
            || !double.IsFinite(data.RestartEnergy) || data.RestartEnergy < 0
            || !double.IsFinite(data.ActiveSecondsRemaining) || data.ActiveSecondsRemaining < 0 || data.Priority < 0 || data.Priority > 100)
            throw new InvalidDataException("Invalid saved capacitor state.");
        Energy = data.Energy; RecoveryRequired = data.RecoveryRequired; RestartEnergy = data.RestartEnergy;
        ActiveSecondsRemaining = data.ActiveSecondsRemaining; Reconcile();
    }
}
