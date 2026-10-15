// Per-instance earned XP, independent of purchased levels; no Node, timer, save state or polling.
using System;
using System.Collections.Generic;

public sealed class TowerRankProgress
{
    public double TotalXp { get; private set; }
    public int RankIndex { get; private set; }
    private double _compensation;

    // =========================================================
    // The table is immutable type data; only this tower's total and selected row are mutable.
    public TowerRankDefinition Current(IReadOnlyList<TowerRankDefinition> ranks) => ranks[RankIndex];
    // =========================================================
    // Null marks maximum rank, never an invented sixth promotion.
    public TowerRankDefinition? Next(IReadOnlyList<TowerRankDefinition> ranks) => RankIndex + 1 < ranks.Count ? ranks[RankIndex + 1] : null;

    // =========================================================
    // Compensated summation retains fractional awards; one large award can cross several thresholds.
    internal bool Add(double amount, IReadOnlyList<TowerRankDefinition> ranks)
    {
        int before = RankIndex;
        double corrected = amount - _compensation, sum = TotalXp + corrected;
        if (!double.IsFinite(sum)) { TotalXp = double.MaxValue; _compensation = 0; }
        else { _compensation = (sum - TotalXp) - corrected; TotalXp = sum; }
        while (RankIndex + 1 < ranks.Count && TotalXp >= ranks[RankIndex + 1].TotalXp) RankIndex++;
        return RankIndex != before;
    }
}
