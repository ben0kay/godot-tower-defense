// Immutable, signed XP line items and captured per-level policy; no UI, saving or combat dependencies.
using System;
using System.Collections.Immutable;

public sealed record MissionXPLine(string Id, string Label, long Amount);
public sealed record MissionXPBreakdown(ImmutableArray<MissionXPLine> Lines)
{
    // =========================================================
    // Later bonuses/deductions can add signed rows; an award never removes previously earned player levels.
    public long TotalXP
    {
        get { long total = 0; foreach (var line in Lines) total = checked(total + line.Amount); return Math.Max(0, total); }
    }
}

public readonly record struct MissionXPPolicy(int VictoryXP, int DefeatXP)
{
    // =========================================================
    // Copy authored inputs at deployment so live Resource edits cannot change an already-started run.
    public static MissionXPPolicy Capture(LevelConfig config)
    {
        if (config == null || config.VictoryBaseXP < 0 || config.DefeatBaseXP < 0)
            throw new ArgumentException("Mission XP settings must be nonnegative.");
        return new(config.VictoryBaseXP, config.DefeatBaseXP);
    }

    // =========================================================
    // Current foundation awards only the outcome baseline; actual stat contributors belong here later.
    public MissionXPBreakdown Calculate(LevelResultSnapshot snapshot)
    {
        if (snapshot == null || !Enum.IsDefined(snapshot.Outcome) || VictoryXP < 0 || DefeatXP < 0)
            throw new ArgumentException("Invalid mission XP inputs.");
        bool won = snapshot.Outcome == LevelOutcome.Victory;
        return new(ImmutableArray.Create(new MissionXPLine("outcome_base", won ? "Victory baseline" : "Defeat baseline", won ? VictoryXP : DefeatXP)));
    }
}

public sealed record MissionRewardTicket(string ProfileId, long Number, string LevelId, MissionXPPolicy Policy);
public sealed record MissionAwardReceipt(long MissionNumber, string LevelId, long XPBefore, long ResearchPointsBefore,
    MissionXPBreakdown Breakdown);
public sealed record MissionRewardResult(MissionXPBreakdown Breakdown, PlayerProgression Before, PlayerProgression After, bool AlreadySaved);
