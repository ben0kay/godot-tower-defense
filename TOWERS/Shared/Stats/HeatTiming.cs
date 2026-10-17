// Human-authored sustained-fire target; resolves once with the selected weapon cadence, never each shot.
using System;
public readonly record struct HeatTiming(double OverheatSeconds, double RecoverySeconds, float RestartFraction = .35f)
{
    // =========================================================
    // Account for continuous passive cooling and the first immediate shot; zero/invalid targets disable heat.
    public HeatStats? Resolve(float shotsPerSecond)
    {
        if (!double.IsFinite(OverheatSeconds) || OverheatSeconds <= 0 || !double.IsFinite(RecoverySeconds)
            || RecoverySeconds <= 0 || !float.IsFinite(shotsPerSecond) || shotsPerSecond <= 0) return null;
        const float capacity = 100;
        float restart = float.IsFinite(RestartFraction) ? Math.Clamp(RestartFraction, 0, .99f) : .35f;
        double cooling = capacity * (1 - restart) / RecoverySeconds;
        double perShot = (capacity + cooling * OverheatSeconds) / (shotsPerSecond * OverheatSeconds + 1);
        return HeatStats.Sanitize(new(capacity, (float)perShot, 0, (float)cooling, restart));
    }
}
