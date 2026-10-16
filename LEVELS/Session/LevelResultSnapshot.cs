// Immutable-at-presentation mission outcome snapshot; progression and tallying are separate later passes.
using System;

public enum LevelOutcome { Victory, Defeat }

public sealed class LevelResultSnapshot
{
    public LevelOutcome Outcome { get; init; }
    public string LevelName { get; init; } = "UNKNOWN SECTOR";
    public int EnemiesKilled { get; init; }
    public int EnemiesBreached { get; init; }
    public int WavesCompleted { get; init; }
    public int TotalWaves { get; init; }
    public int CitadelHealth { get; init; }
    public int CitadelMaxHealth { get; init; }

    // A null field is intentionally UNTRACKED, not an invented zero.
    public int? TowersPlaced { get; init; }
    public double? MissionSeconds { get; init; }
    public int? CreditsEarned { get; init; }
    public int? CreditsSpent { get; init; }

    // =========================================================
    // Presentation never alters a frozen snapshot or grants rewards.
    public int? CitadelIntegrityPercent => CitadelMaxHealth > 0
        ? (int)Math.Round(100.0 * Math.Clamp(CitadelHealth, 0, CitadelMaxHealth) / CitadelMaxHealth)
        : null;
}
