// Reusable seconds-based production clock; inactive waves never advance or queue missed payouts.
using System;

public sealed class CreditProductionTimer
{
    public int CreditsPerCycle { get; }
    public double CycleSeconds { get; }
    public double ElapsedSeconds { get; private set; }

    // =========================================================
    // Balance lives in the producer's configuration, not the wallet or frame rate.
    public CreditProductionTimer(int creditsPerCycle, double cycleSeconds)
    {
        if (creditsPerCycle <= 0) throw new ArgumentOutOfRangeException(nameof(creditsPerCycle));
        if (!double.IsFinite(cycleSeconds) || cycleSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(cycleSeconds));
        CreditsPerCycle = creditsPerCycle; CycleSeconds = cycleSeconds;
    }

    // =========================================================
    // Freeze time while inactive/full; batch elapsed cycles with a capacity bound, no catch-up burst.
    public void Advance(double delta, bool active, CreditBuffer buffer)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (!active || buffer.IsFull || !double.IsFinite(delta) || delta <= 0) return;
        double elapsed = ElapsedSeconds + delta;
        if (!double.IsFinite(elapsed)) elapsed = double.MaxValue;
        int cycles = (int)Math.Min(Math.Floor(elapsed / CycleSeconds),
            Math.Ceiling((buffer.Capacity - buffer.Current) / (double)CreditsPerCycle));
        if (cycles <= 0) { ElapsedSeconds = elapsed; return; }
        int amount = (int)Math.Min((long)cycles * CreditsPerCycle, int.MaxValue);
        buffer.Produce(amount);
        ElapsedSeconds = buffer.IsFull ? 0 : Math.Max(0, elapsed - cycles * CycleSeconds);
    }

    // =========================================================
    // New level/restart explicitly discards fractional progress from the previous session.
    public void Reset() => ElapsedSeconds = 0;

    // =========================================================
    // Checkpoint restore sets source state directly; callers validate before publishing the battlefield.
    internal void RestoreSeconds(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0 || seconds >= CycleSeconds) throw new ArgumentOutOfRangeException(nameof(seconds));
        ElapsedSeconds = seconds;
    }
}
