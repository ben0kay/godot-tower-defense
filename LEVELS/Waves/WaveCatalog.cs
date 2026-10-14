// Resolve explicit level wave sets and precache the existing enemy scene catalog.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static class WaveCatalog
{
    // =========================================================
    // Empty means waves disabled for this map; unknown nonempty IDs never fall back.
    public static WaveDefinition[] GetWaves(string id) => id switch
    {
        "" => Array.Empty<WaveDefinition>(),
        "level001" => Level001Waves.Create(),
        _ => throw new ArgumentException("Unknown WaveSetId: " + id)
    };

    // =========================================================
    // Load once during setup, never once per spawn.
    public static Dictionary<string, PackedScene> LoadScenes() => EnemyCatalog.Default.ToSceneMap();

    // =========================================================
    // Validate the full set before any wave runs, reporting exact wave/batch context.
    public static void Validate(WaveDefinition[] waves, IReadOnlyDictionary<string, PackedScene> scenes)
    {
        if (waves == null || scenes == null) throw new ArgumentException("Wave data/catalog is missing.");
        HashSet<int> numbers = new();
        foreach (WaveDefinition wave in waves)
        {
            if (wave == null || wave.Number <= 0 || !numbers.Add(wave.Number) || wave.Batches.IsDefaultOrEmpty)
                throw new ArgumentException("Wave numbers must be unique/positive, with at least one batch.");
            long count = 0;
            foreach (WaveBatch batch in wave.Batches)
            {
                if (batch == null || batch.Count <= 0 || !double.IsFinite(batch.Interval) || batch.Interval < 0
                    || !double.IsFinite(batch.After) || batch.After < 0 || batch.Entries.IsDefaultOrEmpty)
                    throw new ArgumentException($"Wave {wave.Number}: invalid count/timing/entries.");
                count += batch.Count;
                double weight = 0;
                foreach (WaveEnemyEntry entry in batch.Entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.EnemyId) || !scenes.TryGetValue(entry.EnemyId, out var scene) || scene == null
                        || !double.IsFinite(entry.Weight) || entry.Weight < 0 || entry.Portal == null
                        || (entry.Portal.Length > 0 && (entry.Portal.StartsWith("/") || entry.Portal.Split('/').Contains(".."))))
                        throw new ArgumentException($"Wave {wave.Number}: invalid enemy ID, weight or portal selector.");
                    weight += entry.Weight;
                }
                if (!double.IsFinite(weight) || weight <= 0 || count > int.MaxValue)
                    throw new ArgumentException($"Wave {wave.Number}: invalid total count/weight.");
            }
        }
        foreach (string id in waves.SelectMany(w => w.Batches).SelectMany(b => b.Entries).Select(e => e.EnemyId).Distinct())
        {
            Node node = scenes[id].Instantiate();
            bool valid = node is Enemy enemy && enemy.Definition?.EnemyId == id && enemy.GetNodeOrNull<EnemyMovement>("Movement") != null;
            try { if (valid) EnemyCatalog.ValidateDefinition(((Enemy)node).Definition); }
            finally { node.Free(); }
            if (!valid) throw new ArgumentException("Enemy scene/catalog identity mismatch: " + id);
        }
    }

    // =========================================================
    // Largest-remainder quotas, stable tie breaks, then a seeded Fisher-Yates shuffle.
    public static WaveEnemyEntry[] Compose(WaveBatch batch, RandomNumberGenerator random)
    {
        double total = batch.Entries.Sum(e => e.Weight);
        int[] counts = new int[batch.Entries.Length];
        double[] remainders = new double[counts.Length];
        int assigned = 0;
        for (int i = 0; i < counts.Length; i++)
        {
            double quota = batch.Count * (batch.Entries[i].Weight / total);
            counts[i] = (int)Math.Floor(quota); remainders[i] = quota - counts[i]; assigned += counts[i];
        }
        int[] order = Enumerable.Range(0, counts.Length).OrderByDescending(i => remainders[i]).ThenBy(i => i).ToArray();
        for (int i = 0; i < batch.Count - assigned; i++) counts[order[i % order.Length]]++;
        WaveEnemyEntry[] result = new WaveEnemyEntry[batch.Count];
        int at = 0;
        for (int i = 0; i < counts.Length; i++)
            for (int n = 0; n < counts[i]; n++) result[at++] = batch.Entries[i];
        for (int i = result.Length - 1; i > 0; i--)
        { int other = random.RandiRange(0, i); (result[i], result[other]) = (result[other], result[i]); }
        return result;
    }
}
