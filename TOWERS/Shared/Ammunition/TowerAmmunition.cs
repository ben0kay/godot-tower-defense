// Per-instance reserves and prepaid beam time; the shared AmmoInventory remains the sole stock counter.
using System;

public sealed class TowerAmmunition
{
    public AmmoInventory Inventory { get; }
    public AmmunitionStats Stats { get; private set; }
    public bool ResupplyRequested { get; private set; }
    public bool DeliveryInFlight { get; private set; }
    public long ConsumedRounds { get; private set; }
    public long DiscardedRounds { get; private set; }
    private double _beamCredit;
    public event Action Changed;

    // =========================================================
    // Starting stock is explicit creation-time balance; rebuilds and purchases never refill it.
    public TowerAmmunition(AmmunitionStats stats, float initialFraction)
    {
        Stats = stats;
        initialFraction = float.IsFinite(initialFraction) ? Math.Clamp(initialFraction, 0, 1) : 1;
        Inventory = new AmmoInventory(stats.Capacity, (int)Math.Round(stats.Capacity * (double)initialFraction, MidpointRounding.AwayFromZero));
        Inventory.Changed += () => Changed?.Invoke();
    }

    // =========================================================
    // Preserve stock on capacity growth; explicitly account for discarded excess on shrink.
    public void Synchronize(AmmunitionStats stats)
    {
        if (Stats == stats) return;
        Stats = stats;
        int excess = Math.Max(0, Inventory.Amount - stats.Capacity);
        if (excess > 0) { DiscardedRounds += excess; Inventory.TryConsume(excess); }
        Inventory.TrySetCapacity(stats.Capacity); Changed?.Invoke();
    }

    // =========================================================
    // Called only after target/cooldown/launch preconditions succeed, never for impact or each splash recipient.
    public bool TryFire()
    {
        if (Stats.PerShot <= 0) return true;
        if (!Inventory.TryConsume(Stats.PerShot)) return false;
        ConsumedRounds += Stats.PerShot; return true;
    }

    // =========================================================
    // Buy exact integer rounds before delivering beam damage; retain unused fractions across ticks/targets.
    public double FundBeamSeconds(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds <= 0) return 0;
        if (!float.IsFinite(Stats.PerSecond) || Stats.PerSecond <= 0) return 0;
        double funded = Math.Min(seconds, (Inventory.Amount + _beamCredit) / Stats.PerSecond);
        double cost = funded * Stats.PerSecond;
        int rounds = (int)Math.Min(Inventory.Amount, Math.Ceiling(Math.Max(0, cost - _beamCredit)));
        if (rounds > 0 && !Inventory.TryConsume(rounds)) return 0;
        ConsumedRounds += rounds;
        _beamCredit = Math.Max(0, _beamCredit + rounds - cost);
        return funded;
    }

    // =========================================================
    // Presentation state is separate from inventory so logistics cannot create stock through a status change.
    public void SetDeliveryStatus(bool requested, bool inFlight)
    {
        if (ResupplyRequested == requested && DeliveryInFlight == inFlight) return;
        ResupplyRequested = requested; DeliveryInFlight = inFlight; Changed?.Invoke();
    }

    public string Describe() => $"Ammo: {Inventory.Amount:N0} / {Inventory.Capacity:N0}\n" +
        (Inventory.Amount == 0 ? "Out of ammo — " : "") + (DeliveryInFlight ? "Resupply drone in flight" : ResupplyRequested ? "Resupply requested — waiting for stock/drone" : Inventory.Amount == 0 ? "Awaiting supply" : "Ammo ready");
}
