// Optional travelling-wave capability; maximum radius is the existing shared RangePixels stat.
public readonly record struct ShockwaveStats(float DamagePerWave, float ExpansionSpeedPixelsPerSecond,
    float IntervalSeconds, float EdgeDamageFraction);
