// Shared economy-tier adapter into normal paid upgrades; no competing wallet or combat resolver.
using Godot;
using System;

public abstract partial class CreditProducerConfig : TowerConfig
{
    #region Authored Tiers
    [ExportGroup("Credit Production")]
    [Export] public Godot.Collections.Array<CreditProducerLevel> ProductionLevels { get; set; } = new();
    [Export] public bool ActiveWavesOnly { get; set; } = true;
    #endregion

    // =========================================================
    // Refresh once at catalog/preflight/copy time so nested Inspector resources are authoritative.
    public void PrepareLevels()
    {
        if (ProductionLevels == null || ProductionLevels.Count == 0) throw new ArgumentException("Producer tiers are missing.");
        Levels = new(); int capacity = 0;
        foreach (CreditProducerLevel row in ProductionLevels)
        {
            if (row == null) throw new ArgumentException("Producer tier is missing.");
            row.Validate();
            if (row.Capacity < capacity) throw new ArgumentException("Producer upgrade capacity cannot decrease.");
            capacity = row.Capacity;
            TowerStatBlock stats = new(row.RangePixels, 0, 0, 0, 0, 0, 0,
                MaxHealth: row.HullHealth, Production: new(row.Capacity, row.CreditsPerKill, row.CreditsPerCycle, row.CycleSeconds));
            Levels.Add(new TowerLevel(Levels.Count + 1, row.Price, stats, row.WorkSeconds));
        }
    }

    // =========================================================
    // Private tier Resources and ordinary recipes prevent one placed tower mutating another.
    public override TowerConfig CreateInstanceCopy()
    {
        CreditProducerConfig copy = (CreditProducerConfig)base.CreateInstanceCopy();
        copy.ProductionLevels = new();
        foreach (CreditProducerLevel row in ProductionLevels) copy.ProductionLevels.Add((CreditProducerLevel)row.Duplicate());
        copy.PrepareLevels(); return copy;
    }

    // =========================================================
    // Preview uses the same optional capability snapshot as construction and upgrades.
    public override TowerFinalStats ResolveStats(int level) { PrepareLevels(); return TowerStatPipeline.Resolve(this, level); }
    public override string DescribeStats(TowerFinalStats final)
    {
        if (final?.Production is not CreditProductionStats p) return "Credit producer";
        return p.PerKill > 0 ? $"Income: {p.PerKill} Credits / eligible kill\nKilling tower range: {final.RangePixels:0}px\nStorage: {p.Capacity}"
            : $"Income: {p.PerCycle} Credits / {p.CycleSeconds:0.#}s\nStorage: {p.Capacity}";
    }
}
