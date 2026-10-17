// Pure arithmetic for linear per-level XP costs; bounded binary search handles large grants.
using System;

public static class PlayerLevelCurve
{
    // =========================================================
    // Decimal intermediates avoid overflow while comparing cumulative thresholds.
    private static decimal Threshold(int level, long first, long step)
    {
        if (level < 1 || first <= 0 || step < 0) throw new ArgumentOutOfRangeException(nameof(level));
        decimal count = level - 1m;
        return count * first + count * (count - 1m) / 2m * step;
    }

    // =========================================================
    // Return the exact lifetime XP needed to enter a supported level.
    public static long TotalForLevel(int level, long first, long step) =>
        checked((long)Threshold(level, first, step));

    // =========================================================
    // Find earned level in at most 31 comparisons without one loop per promotion.
    public static int LevelAt(long xp, long first, long step)
    {
        if (xp < 0) throw new ArgumentOutOfRangeException(nameof(xp));
        Threshold(1, first, step);
        if (xp >= Threshold(int.MaxValue, first, step))
            throw new OverflowException("Player level exceeds supported range.");
        int low = 1, high = int.MaxValue - 1;
        while (low < high)
        {
            int middle = low + (int)(((long)high - low + 1) / 2);
            if (Threshold(middle, first, step) <= xp) low = middle;
            else high = middle - 1;
        }
        // A next-level cost must also fit the persisted numeric representation.
        checked { _ = first + (low - 1L) * step; }
        return low;
    }
}
