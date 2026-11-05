// Optional shared capacitor seam; each supported weapon must fund its real attacks before emitting them.
using System;
using System.IO;

public abstract partial class TowerBase
{
    public TowerPowerConsumer Power { get; private set; }
    public virtual bool SupportsPowerCapacitor => false;
    // =========================================================
    // Explicit opt-in protects unadapted beams, volleys and utility buildings from free powered operation.
    private void SynchronizePower()
    {
        TowerPowerConfig config = AuthoredConfig?.Power;
        if (config == null) { Power?.Dispose(); Power = null; return; }
        if (!SupportsPowerCapacitor) throw new InvalidOperationException("This tower has no funded power attack seam.");
        config.Settings();
        if (Power == null) Power = new TowerPowerConsumer(this, config);
        else Power.Synchronize(config);
    }
    // =========================================================
    // Ammunition rejection rolls back energy; successful depletion is published AFTER projectile creation.
    protected bool TryFundProjectileAttack(double seconds, int shots = 1) => Power?.TryFundAttack(seconds, Ammunition, shots) ?? (Ammunition?.TryFire(shots) != false);
    // Continuous weapons and repair reuse the same capacitor/payment/inspection owner, without new polling.
    protected double FundPoweredWork(double seconds, bool useBeamAmmo = false) => Power?.FundWorkSeconds(seconds, useBeamAmmo ? Ammunition : null)
        ?? (useBeamAmmo ? Ammunition?.FundBeamSeconds(seconds) ?? seconds : seconds);
    protected void CompletePowerAttack() => Power?.Publish();
    private PowerSaveData? CapturePowerCheckpoint() => Power?.Capture();
    private void RestorePowerCheckpoint(PowerSaveData? saved)
    {
        if (Power == null && saved.HasValue) throw new InvalidDataException("Unexpected saved power capability.");
        Power?.Restore(saved);
    }
}
