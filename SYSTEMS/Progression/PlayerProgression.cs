// Immutable profile progression snapshot; separate from identity, Credits and tower rank XP.
using System;
using System.Collections.Immutable;

public sealed record PlayerProgression
{
    public int Version { get; init; } = 2;
    public ImmutableArray<string> PurchasedResearchIds { get; init; } = ImmutableArray<string>.Empty;
    public string ProfileId { get; init; } = "";
    public long LifetimePlayerXP { get; init; }
    public long UnspentResearchPoints { get; init; }
    public long FirstLevelXP { get; init; } = 500;
    public long LevelStepXP { get; init; } = 250;
    public int Level => PlayerLevelCurve.LevelAt(LifetimePlayerXP, FirstLevelXP, LevelStepXP);
    public long CurrentXP => LifetimePlayerXP - PlayerLevelCurve.TotalForLevel(Level, FirstLevelXP, LevelStepXP);
    public long RequiredXP => checked(FirstLevelXP + (Level - 1L) * LevelStepXP);

    // =========================================================
    // Produce a candidate only; the service must save it before notifying readers.
    public PlayerProgression AddXP(long amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        long total = checked(LifetimePlayerXP + amount);
        int nextLevel = PlayerLevelCurve.LevelAt(total, FirstLevelXP, LevelStepXP);
        return this with { LifetimePlayerXP = total,
            UnspentResearchPoints = checked(UnspentResearchPoints + nextLevel - Level) };
    }
}
