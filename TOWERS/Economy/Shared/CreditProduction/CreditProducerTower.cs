// Shared tower lifecycle, buffered manual collection and optional active-wave production clock.
using Godot;
using System.Collections.Generic;

public abstract partial class CreditProducerTower : TowerBase
{
    #region State
    public CreditBuffer Buffer { get; private set; }
    protected CreditProducerConfig ProducerConfig => (CreditProducerConfig)AuthoredConfig;
    public override List<TowerLevel> Levels => AuthoredConfig.Levels;
    protected WaveManager Waves;
    protected EconomyManager Wallet;
    public virtual bool UsesManualCollection => true;
    private LevelSessionController _session;
    private CreditProductionTimer _timer;
    public bool CanCollect => UsesManualCollection && Buffer?.Current > 0 && Available && !IsBusy;
    protected bool Available => IsInsideTree() && !IsQueuedForDeletion() && CanOperate && !GetTree().Paused
        && _session?.State == LevelSessionState.Playing;
    protected bool CanProduce => Available && !IsBusy && (!ProducerConfig.ActiveWavesOnly || Waves?.State is
        WaveManager.WaveState.Spawning or WaveManager.WaveState.SpawnBlocked or WaveManager.WaveState.WaitingForClear);
    #endregion

    #region Lifecycle
    // =========================================================
    // Independent initial scene loads also validate their nested economy tier resources.
    public override bool ValidateScene() { try { ProducerConfig?.PrepareLevels(); return ProducerConfig != null; } catch { return false; } }
    public override TowerFinalStats ResolveLevel(int level) => ResolveRankedLevel(level);

    // =========================================================
    // Cache authoritative world services once; production never scans enemies or writes directly to HUD Credits.
    public override void _Ready()
    {
        ProducerConfig.PrepareLevels(); FinalStats = ResolveLevel(UpgradeLevel);
        Node battle = GetParent()?.GetParent()?.GetParent();
        Wallet = battle?.GetNodeOrNull<EconomyManager>("Economy");
        _session = battle?.GetNodeOrNull<LevelSessionController>("LevelSession");
        Waves = battle?.GetNodeOrNull<WaveManager>("Waves");
        Buffer = new CreditBuffer(FinalStats.Production.Value.Capacity); Buffer.Changed += OnBufferChanged;
        StatsChanged += SynchronizeProduction; WorkStarted += NotifyRuntimeChanged; WorkCompleted += NotifyRuntimeChanged;
        SynchronizeProduction(); SetProcess(false);
    }

    // =========================================================
    // Upgrades retain stored income; their new periodic clock starts fresh, without retroactive payouts.
    private void SynchronizeProduction()
    {
        CreditProductionStats p = FinalStats.Production.Value;
        Buffer.ResizeCapacity(p.Capacity);
        _timer = p.PerCycle > 0 ? new CreditProductionTimer(p.PerCycle, p.CycleSeconds) : null;
        SetOperationalPhysicsProcess(_timer != null); NotifyRuntimeChanged();
    }

    // =========================================================
    // The existing pause-aware producer clock freezes while inactive, disabled, busy or full.
    public override void _PhysicsProcess(double delta) => _timer?.Advance(delta, CanProduce, Buffer);
    private void OnBufferChanged(int current, int capacity) => NotifyRuntimeChanged();

    // =========================================================
    // The Inspector button calls one idempotent wallet-backed action; full-wallet rejection preserves storage.
    public bool TryCollect()
    {
        if (!CanCollect || !GodotObject.IsInstanceValid(Wallet)) return false;
        return Buffer.TryCollect(Wallet, Wallet.CreditsResource);
    }

    // =========================================================
    // Keep the final line useful in the compact runtime section used by the existing Inspector.
    public override string DescribeRuntime() => $"Credits stored: {Buffer?.Current ?? 0} / {Buffer?.Capacity ?? 0} · collect below";

    // =========================================================
    // Every subscription is instance-scoped and detached before a scene/tower disappears.
    public override void _ExitTree()
    {
        if (Buffer != null) Buffer.Changed -= OnBufferChanged;
        StatsChanged -= SynchronizeProduction; WorkStarted -= NotifyRuntimeChanged; WorkCompleted -= NotifyRuntimeChanged;
        base._ExitTree();
    }
    #endregion
}
