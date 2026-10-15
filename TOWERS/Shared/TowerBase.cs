// Lightweight placed-tower progression and effective snapshots; no mandatory targeting/firing loop.
using Godot;
using System;

public abstract partial class TowerBase : Node2D
{
    public TowerDefinition Definition { get; private set; }
    public string TowerTypeId => Definition?.TowerId ?? "";
    public Vector2I GridCell { get; private set; }
    public Vector2I Footprint => Definition?.Footprint ?? Vector2I.One;
    public abstract TowerConfig AuthoredConfig { get; }
    public virtual float? InspectionRange => null;
    public int UpgradeLevel { get; private set; } = 1;
    public TowerFinalStats FinalStats { get; protected set; }
    public event Action StatsChanged;
    public abstract System.Collections.Generic.List<TowerLevel> Levels { get; }
    public TowerLevel NextUpgrade => Levels != null && UpgradeLevel < Levels.Count
        ? Levels[UpgradeLevel] : null;

    // =========================================================
    // The type owns config compatibility; generic placement never assumes weapon node names.
    public abstract bool AcceptsConfig(TowerConfig config);

    // =========================================================
    // Optional type-specific scene preflight keeps malformed weapon scenes out of the world.
    public virtual bool ValidateScene() => true;

    // =========================================================
    // Apply a private source copy before Ready; definition remains shared authored metadata.
    public void InitializeDefinition(TowerDefinition definition)
    {
        if (definition == null || !AcceptsConfig(definition.DefaultConfig))
            throw new InvalidOperationException("Tower/config mismatch.");
        Definition = definition;
        ApplyConfig(definition.DefaultConfig.CreateInstanceCopy());
    }

    // =========================================================
    // Derived types apply only their compatible source config.
    protected abstract void ApplyConfig(TowerConfig config);

    // =========================================================
    // Fixed grid identity is independent of any rotating weapon artwork.
    public void SetPlacement(Vector2I cell, Vector2 center)
    { GridCell = cell; GlobalPosition = center; }

    // =========================================================
    // Passive visuals can use root tint; weapon types may override their visual selection hook.
    public virtual void SetSelected(bool selected) => Modulate = selected ? new Color(1f, 0.9f, 0.68f) : Colors.White;

    // =========================================================
    // Derived tower definitions supply their inputs; preview and application use the same resolver.
    public abstract TowerFinalStats ResolveLevel(int level);

    // =========================================================
    // Publish once after committed changes; wallet/controller retain payment ownership.
    internal void NotifyStatsChanged() => StatsChanged?.Invoke();

    // =========================================================
    // Commit pre-resolved data silently so observers see a fully paid upgrade, never partial state.
    internal bool CommitUpgrade(int expectedLevel, TowerFinalStats final)
    {
        if (!IsInsideTree() || UpgradeLevel != expectedLevel || final == null) return false;
        UpgradeLevel++; FinalStats = final;
        return true;
    }
}
