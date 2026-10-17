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
    public event Action RuntimeChanged;
    public TowerAmmunition Ammunition { get; private set; }
    public event Action TargetPriorityChanged;
    public TowerTargeting.Priority? TargetPriority => AuthoredConfig is ITowerTargetingPolicy policy ? policy.TargetPriority : null;
    // =========================================================
    // Selection policy changes cost nothing and never rebuild the combat stat cache or touch cooldown/ammo.
    public bool SetTargetPriority(TowerTargeting.Priority priority)
    {
        if (!IsInsideTree() || IsQueuedForDeletion() || GetTree().Paused || !System.Enum.IsDefined(priority)
            || AuthoredConfig is not ITowerTargetingPolicy policy) return false;
        if (policy.TargetPriority == priority) return true;
        policy.TargetPriority = priority; OnTargetPriorityChanged(); TargetPriorityChanged?.Invoke(); return true;
    }
    // =========================================================
    // Aimed weapons request one fresh acquisition on their next normal physics tick.
    protected virtual void OnTargetPriorityChanged() { }
    public virtual string DescribeRuntime() => Ammunition?.Describe() ?? "";
    internal void NotifyRuntimeChanged() => RuntimeChanged?.Invoke();
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
    internal void NotifyStatsChanged()
    {
        SynchronizeAmmunition(); SynchronizeConditions(); StatsChanged?.Invoke();
    }

    // =========================================================
    // Optional weapon reserves initialize once; passive buildings have no artificial ammo capability.
    private void SynchronizeAmmunition()
    {
        if (FinalStats?.Ammunition is not AmmunitionStats stats) return;
        if (Ammunition != null) { Ammunition.Synchronize(stats); return; }
        Ammunition = new TowerAmmunition(stats, AuthoredConfig.InitialAmmoFraction);
        Ammunition.Changed += NotifyRuntimeChanged;
        if (IsOperational) GetNodeOrNull<AmmunitionLogistics>("../../AmmunitionLogistics")?.RegisterConsumer(this);
    }

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
    // Wallet callback schedules a specific paid tier silently; public notifications happen only after payment.
    internal bool BeginUpgrade(int expectedLevel, TowerLevel next, TowerFinalStats instant)
    {
        if (!IsInsideTree() || IsQueuedForDeletion() || IsBusy || UpgradeLevel != expectedLevel || NextUpgrade != next || instant == null) return false;
        TowerConfig.ValidateWorkSeconds(next.WorkSeconds);
        if (next.WorkSeconds == 0)
        {
            UpgradeLevel = next.Level; FinalStats = instant; WorkSeconds = ElapsedWorkSeconds = 0;
            return true;
        }
        ConstructionWorkManager manager = GetNodeOrNull<ConstructionWorkManager>("../../ConstructionWork");
        if (!GodotObject.IsInstanceValid(manager) || !manager.IsInsideTree() || manager.IsQueuedForDeletion()) return false;
        try { manager.Register(this); } catch (InvalidOperationException) { return false; }
        _construction = manager; WorkPhase = TowerWorkPhase.Upgrading; PendingUpgradeLevel = next.Level;
        WorkSeconds = next.WorkSeconds; ElapsedWorkSeconds = _lastWorkNotice = 0;
        return true;
    }

    // =========================================================
    // Observers see the paid wallet and coherent work state; zero-duration upgrades apply exactly once.
    internal void PublishUpgradePurchase(bool instant)
    {
        if (!IsInsideTree() || IsQueuedForDeletion()) return;
        if (WorkPhase == TowerWorkPhase.Upgrading) WorkStarted?.Invoke();
        else if (instant)
        {
            NotifyStatsChanged();
            if (GodotObject.IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion()) WorkCompleted?.Invoke();
        }
    }

    #region Construction
    public TowerWorkPhase WorkPhase { get; private set; } = TowerWorkPhase.Operational;
    public int PendingUpgradeLevel { get; private set; }
    public double ElapsedWorkSeconds { get; private set; }
    public double WorkSeconds { get; private set; }
    public double RemainingWorkSeconds => System.Math.Max(0, WorkSeconds - ElapsedWorkSeconds);
    public double WorkProgress => WorkSeconds <= 0 ? 1 : System.Math.Clamp(ElapsedWorkSeconds / WorkSeconds, 0, 1);
    public bool IsOperational => WorkPhase != TowerWorkPhase.Constructing;
    public bool IsBusy => WorkPhase != TowerWorkPhase.Operational;
    public event Action WorkStarted;
    public event Action WorkProgressChanged;
    public event Action WorkCompleted;
    private ConstructionWorkManager _construction;
    private bool _operationalPhysics;
    private double _lastWorkNotice;
    public TowerWorkState CaptureWorkState() => new(WorkPhase, ElapsedWorkSeconds, WorkSeconds, PendingUpgradeLevel);

    // =========================================================
    // Paid placement calls this before AddChild/Ready; direct scene instances remain operational by default.
    internal void StartConstruction(ConstructionWorkManager manager)
    {
        if (IsInsideTree() || IsBusy) throw new InvalidOperationException("Construction must start before placement.");
        double seconds = AuthoredConfig.BuildSeconds;
        TowerConfig.ValidateWorkSeconds(seconds);
        if (seconds > 0 && (!GodotObject.IsInstanceValid(manager) || !manager.IsInsideTree() || manager.IsQueuedForDeletion()))
            throw new InvalidOperationException("Construction requires a live world manager.");
        WorkSeconds = seconds; ElapsedWorkSeconds = 0; _lastWorkNotice = 0;
        if (seconds == 0) return; // Zero-duration compatibility has no inactive tick or job.
        WorkPhase = TowerWorkPhase.Constructing; _construction = manager;
        manager.Register(this); WorkStarted?.Invoke();
    }

    // =========================================================
    // Ready records normal capability processing once; unfinished structures have no weapon/production ticks.
    protected void SetOperationalPhysicsProcess(bool enabled)
    { _operationalPhysics = enabled; SetPhysicsProcess(enabled && IsOperational); }

    // =========================================================
    // Exact boundary tolerance prevents an extra physics frame after sums such as120 increments of1/60.
    internal void AdvanceWork(ConstructionWorkManager manager, double delta)
    {
        if (_construction != manager || !IsBusy) return;
        double remaining = RemainingWorkSeconds;
        if (delta >= remaining || remaining - delta <= 1e-9)
        {
            bool upgrading = WorkPhase == TowerWorkPhase.Upgrading;
            // Resolve the purchased absolute tier now, using all rank promotions earned during work.
            TowerFinalStats upgraded = upgrading ? ResolveLevel(PendingUpgradeLevel) : null;
            if (upgrading) { UpgradeLevel = PendingUpgradeLevel; FinalStats = upgraded; }
            ElapsedWorkSeconds = WorkSeconds; WorkPhase = TowerWorkPhase.Operational; PendingUpgradeLevel = 0;
            _construction = null; manager.Unregister(this);
            if (upgrading) NotifyStatsChanged();
            else { SetPhysicsProcess(_operationalPhysics); OnBecameOperational(); }
            // A stat/inventory observer may have removed this building during activation.
            if (GodotObject.IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion()) WorkCompleted?.Invoke();
            return;
        }
        ElapsedWorkSeconds += delta;
        if (ElapsedWorkSeconds - _lastWorkNotice >= .1 - 1e-9)
        { _lastWorkNotice = ElapsedWorkSeconds; WorkProgressChanged?.Invoke(); }
    }

    // =========================================================
    // Activation publishes existing ammunition stock without refilling, stat rebuild or navigation mutation.
    protected virtual void OnBecameOperational()
    {
        if (Ammunition != null) GetNodeOrNull<AmmunitionLogistics>("../../AmmunitionLogistics")?.RegisterConsumer(this);
    }

    // =========================================================
    // Manager shutdown/rollback stops the clock without turning an unfinished site into a working building.
    internal void DetachConstruction(ConstructionWorkManager manager)
    { if (_construction == manager) _construction = null; }
    internal void CancelConstruction()
    {
        ConstructionWorkManager manager = _construction; _construction = null;
        if (GodotObject.IsInstanceValid(manager)) manager.Unregister(this);
    }
    public override void _ExitTree() { _conditions?.Clear(); _conditions = null; CancelConstruction(); }
    #endregion
}
