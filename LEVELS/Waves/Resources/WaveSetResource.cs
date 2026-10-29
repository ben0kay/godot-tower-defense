// Editable Godot wave set; constructs the same immutable definitions used by existing C# recipes.
using Godot;
using System;
using System.Collections.Generic;
using static WaveBuilder;

[Tool, GlobalClass]
public partial class WaveSetResource : WaveSetSource
{
    [Export] public Godot.Collections.Array<WaveResource> Waves { get; set; } = new();

    // =========================================================
    // Convert once when the level starts; live scheduling and enemy ownership remain in WaveManager.
    public override WaveDefinition[] Build()
    {
        var result = new List<WaveDefinition>();
        var seen = new HashSet<int>();
        if (Waves == null) throw new ArgumentException("Wave set has no Waves array.");
        foreach (WaveResource wave in Waves)
        {
            if (wave == null || wave.Number < 1 || !seen.Add(wave.Number) || wave.Batches == null || wave.Batches.Count == 0)
                throw new ArgumentException("Each wave needs a unique positive number and at least one batch.");
            var batches = new List<WaveBatch>();
            foreach (WaveBatchResource batch in wave.Batches)
            {
                if (batch == null || batch.Count < 1 || !double.IsFinite(batch.Interval) || batch.Interval < 0 ||
                    !double.IsFinite(batch.After) || batch.After < 0 || batch.Enemies == null || batch.Enemies.Count == 0)
                    throw new ArgumentException($"Wave {wave.Number}: invalid batch count, timings or enemy list.");
                var entries = new List<WaveEnemyEntry>();
                double totalWeight = 0;
                foreach (WaveEnemyResource enemy in batch.Enemies)
                {
                    if (enemy == null || string.IsNullOrWhiteSpace(enemy.EnemyId) ||
                        !double.IsFinite(enemy.Weight) || enemy.Weight < 0 || enemy.Portal == null)
                        throw new ArgumentException($"Wave {wave.Number}: invalid enemy ID, weight or portal.");
                    entries.Add(Enemy(enemy.EnemyId, enemy.Weight, enemy.Portal, enemy.Shield, enemy.Stealth));
                    totalWeight += enemy.Weight;
                }
                if (!double.IsFinite(totalWeight) || totalWeight <= 0) throw new ArgumentException($"Wave {wave.Number}: weights must sum to more than zero.");
                batches.Add(Batch(batch.Count, batch.Interval, batch.After, entries.ToArray()));
            }
            bool hasPresentation = wave.Alert != WaveAlertKind.Standard || !string.IsNullOrEmpty(wave.BannerTitle) ||
                !string.IsNullOrEmpty(wave.BannerDescription) || !string.IsNullOrEmpty(wave.Speaker) ||
                !string.IsNullOrEmpty(wave.NarrationText) || !string.IsNullOrEmpty(wave.VoiceCueId);
            result.Add(hasPresentation
                ? Wave(wave.Number, Presentation(wave.Alert, wave.BannerTitle, wave.BannerDescription,
                    wave.Speaker, wave.NarrationText, wave.VoiceCueId), batches.ToArray())
                : Wave(wave.Number, batches.ToArray()));
        }
        return result.ToArray();
    }
}
