// Generic tower-owned disabling API; weapons share one gate while construction keeps its original meaning.
using Godot;
using System;

public abstract partial class TowerBase
{
    private TowerConditions _conditions;
    public bool IsDisabled => _conditions?.Count > 0;
    public bool CanOperate => IsOperational && !IsDisabled;
    public bool CanAttack => CanOperate && IsInsideTree() && !IsQueuedForDeletion() && !GetTree().Paused;
    public float CurrentHeat => _conditions?.Heat ?? 0;
    public bool IsOverheated => _conditions?.IsOverheated ?? false;
    public int DisableCount => _conditions?.Count ?? 0;
    public event Action DisableStateChanged;
    public event Action HeatChanged;
    // =========================================================
    // Ordinary unaffected towers allocate no condition node, timer or collection.
    private TowerConditions Conditions()
    {
        if (_conditions != null) return _conditions;
        _conditions = new() { Name = "CombatConditions" }; _conditions.Initialize(this); AddChild(_conditions);
        _conditions.Synchronize(FinalStats?.Heat); return _conditions;
    }
    // =========================================================
    // Null duration is condition-based; source zero is independent, nonzero refreshes that source/reason.
    public TowerDisableHandle ApplyDisable(TowerDisableReason reason, double? durationSeconds = null, ulong sourceId = 0)
    {
        if (!IsInsideTree() || IsQueuedForDeletion()) throw new InvalidOperationException("Disable requires a live placed tower.");
        return Conditions().Apply(reason, durationSeconds, sourceId);
    }
    // =========================================================
    // Foreign/expired handles are safe no-ops. Already emitted attacks are independently owned.
    public bool RemoveDisable(TowerDisableHandle handle) => _conditions?.Remove(handle) ?? false;
    // =========================================================
    // Explicit sustained-source cleanup; pulses can instead expire independently after their source dies.
    public void RemoveDisablesFromSource(ulong sourceId) => _conditions?.RemoveSource(sourceId);
    // =========================================================
    // HUD3 can read reasons on events without allocating a fresh collection each physics tick.
    public TowerDisableReason GetDisableReason(int index) => _conditions.ReasonAt(index);
    // =========================================================
    // Shared notification is immediate for beam/aim shutdown; ongoing timers/cooling stay enabled.
    internal void PublishDisableChanged() { OnDisableChanged(); DisableStateChanged?.Invoke(); NotifyRuntimeChanged(); }
    // =========================================================
    // Derived presentation/targeting cleanup belongs to its weapon, not to the generic state container.
    protected virtual void OnDisableChanged() { }
    // =========================================================
    // Heat changes never invalidate FinalStats. The selected Inspector can subscribe in HD3.
    internal void PublishHeatChanged() => HeatChanged?.Invoke();
    // =========================================================
    // Synchronize only when effective inputs change; cold heatless towers remain allocation-free.
    private void SynchronizeConditions()
    { if (FinalStats?.Heat != null || _conditions != null) Conditions().Synchronize(FinalStats?.Heat); }
    // =========================================================
    // Called after a committed launch/volley/pulse; one shared helper covers every offensive weapon.
    protected void RecordAttackHeat(int shots = 1, double beamSeconds = 0)
    { if (FinalStats?.Heat != null) Conditions().Charge(shots, beamSeconds); }
    // =========================================================
    // Deterministic explicit stepping is also useful to gameplay tools; engine normally owns this clock.
    public void AdvanceConditions(double delta) => _conditions?.Advance(delta);
}
