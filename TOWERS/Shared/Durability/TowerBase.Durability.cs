// Shared per-instance tower hull state. Reuses combat damage math and the existing event-driven DefenseBar.
using Godot;
using System;

public abstract partial class TowerBase
{
    #region Durability State
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public bool IsDestroyed { get; private set; }
    public event Action HealthChanged;
    public event Action Destroyed;
    private bool _healthInitialized;
    private DefenseBar _defenseBar;
    #endregion

    // =========================================================
    // Ready's signal runs after concrete weapon/passive setup, including direct F6 scene instances.
    public override void _EnterTree() => Ready += InitializeDurability;

    // =========================================================
    // One shared visual stays on the fixed tower root, independent of the rotating gun pivot.
    private void InitializeDurability()
    {
        SynchronizeHealth();
        if (_defenseBar == null)
        {
            _defenseBar = GD.Load<PackedScene>("res://FEEDBACK/DefenseBars/DefenseBar.tscn").Instantiate<DefenseBar>();
            _defenseBar.Name = "DefenseBar"; AddChild(_defenseBar);
            float cellSize = GetNodeOrNull<WorldGrid>("../../Grid")?.Config.CellSize ?? 64;
            _defenseBar.SetOwnerVisualHeight(Footprint.Y * cellSize);
        }
        RefreshDefenseBar();
    }

    // =========================================================
    // Promotions/upgrades preserve missing HP: extra capacity is granted without erasing damage.
    private void SynchronizeHealth()
    {
        if (IsDestroyed) return;
        float authored = FinalStats?.MaxHealth ?? AuthoredConfig?.MaxHealth ?? 100;
        float maximum = float.IsFinite(authored) ? Mathf.Max(1, authored) : 1;
        if (_healthInitialized && MaxHealth == maximum) return;
        float missing = _healthInitialized ? MaxHealth - Health : 0;
        MaxHealth = maximum; Health = Mathf.Clamp(maximum - missing, 0, maximum); _healthInitialized = true;
        if (Health == 0) { DestroyHull(); return; }
        PublishHealthChanged();
    }

    // =========================================================
    // Typed enemy/environment hits use the same hull coefficients as the existing enemy damage pipeline.
    public void ApplyDamage(DamageInfo hit)
    {
        if (IsDestroyed || !IsInsideTree() || IsQueuedForDeletion() || GetTree().Paused) return;
        SynchronizeHealth();
        if (IsDestroyed) return;
        float next = DamageResolver.Resolve(hit, 0, Health).Health;
        if (next == Health) return;
        Health = next;
        if (Health == 0) DestroyHull(); else PublishHealthChanged();
    }

    // =========================================================
    // Future repair producers can restore hull explicitly; there is no regeneration or repair timer.
    public void RestoreHealth(float amount)
    {
        if (!float.IsFinite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (IsDestroyed || !IsInsideTree() || IsQueuedForDeletion() || GetTree().Paused) return;
        SynchronizeHealth();
        if (IsDestroyed) return;
        float next = Mathf.Min(MaxHealth, Health + amount);
        if (next == Health) return;
        Health = next; PublishHealthChanged();
    }

    // =========================================================
    // Death is latched before callbacks; existing TreeExiting owners release occupancy, work and logistics.
    private void DestroyHull()
    {
        if (IsDestroyed) return;
        IsDestroyed = true; SetPhysicsProcess(false); CancelConstruction();
        QueueFree(); PublishHealthChanged(); Destroyed?.Invoke();
    }

    // =========================================================
    // Damage/repair events update the shared visual and selected Inspector, never rebuild weapon stats.
    private void PublishHealthChanged()
    { RefreshDefenseBar(); HealthChanged?.Invoke(); NotifyRuntimeChanged(); }

    // =========================================================
    // Towers do not move/rotate their root, so no per-frame alignment callback is needed.
    private void RefreshDefenseBar()
    {
        if (!GodotObject.IsInstanceValid(_defenseBar)) return;
        _defenseBar.SetHealth(Health, MaxHealth); _defenseBar.AlignWithOwner(Rotation);
    }

    // =========================================================
    // Add hull to the existing compact runtime label without changing Inspector layout resources.
    public string DescribeInspectionRuntime()
    {
        string runtime = DescribeRuntime();
        return $"HP: {Health:0.#} / {MaxHealth:0.#}" + (string.IsNullOrEmpty(runtime) ? "" : " · " + runtime);
    }
}
