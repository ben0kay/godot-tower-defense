// Immutable C# authoring data; runtime counters live exclusively in WaveManager.
using System.Collections.Immutable;

public sealed record WaveEnemyEntry(string EnemyId, double Weight = 100, string Portal = "", bool SpawnWithShield = false, bool SpawnWithStealth = false);
public sealed record WaveBatch(int Count, double Interval, double After, ImmutableArray<WaveEnemyEntry> Entries);
public sealed record WaveDefinition(int Number, ImmutableArray<WaveBatch> Batches);

public static class WaveBuilder
{
    // =========================================================
    // Compact helpers copy arrays into immutable data, keeping authored waves read-only.
    public static WaveDefinition Wave(int number, params WaveBatch[] batches) => new(number, batches.ToImmutableArray());

    // =========================================================
    // Count is total ships; interval follows successful spawns, after follows the final one.
    public static WaveBatch Batch(int count, double interval, double after, params WaveEnemyEntry[] enemies) =>
        new(count, interval, after, enemies.ToImmutableArray());

    // =========================================================
    // Optional portal is level-root-relative; shield defaults OFF unless the wave opts in.
    public static WaveEnemyEntry Enemy(string id, double weight = 100, string portal = "", bool shield = false, bool stealth = false) =>
        new(id, weight, portal, shield, stealth);
}
