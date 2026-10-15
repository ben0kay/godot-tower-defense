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
    public event Action ExperienceChanged;
    public TowerRankProgress RankProgress { get; } = new();
    public TowerRankDefinition CurrentRank => RankProgress.Current(AuthoredConfig.Ranks);
    public TowerRankDefinition? NextRank => RankProgress.Next(AuthoredConfig.Ranks);
    private TowerMasterConfig _experienceRules;
    private DamageSource _damageSource;
    public TowerMasterConfig ExperienceRules => _experienceRules ?? TowerRankController.DefaultRules;
    public DamageSource DamageSource => _damageSource ??= new DamageSource(this);
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
    public void InitializeDefinition(TowerDefinition definition, TowerMasterConfig rules = null)
    {
        if (definition == null || !AcceptsConfig(definition.DefaultConfig))
            throw new InvalidOperationException("Tower/config mismatch.");
        Definition = definition; _experienceRules = rules;
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
    // Both live stats and purchased-level previews retain this instance's currently earned rank.
    protected TowerFinalStats ResolveRankedLevel(int level) => TowerStatPipeline.Resolve(AuthoredConfig, level, rank: CurrentRank);

    // =========================================================
    // XP-only changes publish progress, while promotions rebuild effective stats exactly once.
    internal void ReceiveExperience(double amount)
    {
        double before = RankProgress.TotalXp;
        bool promoted = RankProgress.Add(amount, AuthoredConfig.Ranks);
        if (promoted) { FinalStats = ResolveLevel(UpgradeLevel); NotifyStatsChanged(); }
        if (RankProgress.TotalXp != before) ExperienceChanged?.Invoke();
    }

    // =========================================================
    // Commit pre-resolved data silently so observers see a fully paid upgrade, never partial state.
    internal bool CommitUpgrade(int expectedLevel, TowerFinalStats final)
    {
        if (!IsInsideTree() || UpgradeLevel != expectedLevel || final == null) return false;
        UpgradeLevel++; FinalStats = final;
        return true;
    }
}
