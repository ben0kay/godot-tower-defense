// Human-authored sustained-fire targets; resolve once from the selected shot, pulse or beam cadence.
using System;
public readonly record struct HeatTiming(double OverheatSeconds, double RecoverySeconds, float RestartFraction = .35f)
{
    private const float Capacity = 100;

    // =========================================================
    // Common cooling/restart policy, with nonfinite timing disabled rather than creating invalid heat.
    private bool TryBaseline(out float restart, out double cooling)
    {
        restart = float.IsFinite(RestartFraction) ? Math.Clamp(RestartFraction, 0, .99f) : .35f;
        cooling = 0;
        if (!double.IsFinite(OverheatSeconds) || OverheatSeconds <= 0 || !double.IsFinite(RecoverySeconds) || RecoverySeconds <= 0) return false;
        cooling = Capacity * (1 - restart) / RecoverySeconds;
        return double.IsFinite(cooling);
    }

    // =========================================================
    // Projectiles and emitted shockwaves heat once per funded launch; account for the first immediate shot.
    public HeatStats? Resolve(float shotsPerSecond)
    {
        if (!TryBaseline(out float restart, out double cooling) || !float.IsFinite(shotsPerSecond) || shotsPerSecond <= 0) return null;
        double perShot = (Capacity + cooling * OverheatSeconds) / (shotsPerSecond * OverheatSeconds + 1);
        return HeatStats.Sanitize(new(Capacity, (float)perShot, 0, (float)cooling, restart));
    }

    // =========================================================
    // Continuous damaging beam time adds heat every funded second; passive cooling runs at the same time.
    public HeatStats? ResolveBeam()
    {
        if (!TryBaseline(out float restart, out double cooling)) return null;
        double perSecond = cooling + Capacity / OverheatSeconds;
        return HeatStats.Sanitize(new(Capacity, 0, (float)perSecond, (float)cooling, restart));
    }
}
