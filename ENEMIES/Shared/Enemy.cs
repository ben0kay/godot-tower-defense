// Shared lightweight enemy: health, lifecycle and registry membership, independent of type.
using Godot;
using System;

public partial class Enemy : Node2D
{
    [Export] public EnemyDefinition Definition { get; set; }
    public float Health { get; private set; }
    public float CurrentShield { get; private set; }
    public bool IsAlive { get; private set; }
    public EnemyMovement Movement { get; private set; }
    public float RemainingPathDistance => Movement?.RemainingPathDistance ?? float.PositiveInfinity;
    public event Action<Enemy> Removed;
    public event Action<Enemy> Initialized;
    public EnemyRegistry Registry => _registry;
    private EnemyRegistry _registry;
    private DefenseBar _defenseBar;

    // =========================================================
    // Called by SpawnManager after adding the scene, using world-scoped services.
    public void Initialize(EnemyRegistry registry, PathRequestManager paths, bool spawnWithShield = false)
    {
        _registry = registry; Health = Mathf.Max(1f, Definition.MaxHealth); IsAlive = true;
        CurrentShield = spawnWithShield && float.IsFinite(Definition.MaxShield)
            ? Mathf.Max(0f, Definition.MaxShield) : 0f;
        _defenseBar = GetNodeOrNull<DefenseBar>("DefenseBar");
        _defenseBar?.SetHealth(Health, Definition.MaxHealth);
        Movement = GetNode<EnemyMovement>("Movement");
        _registry.Register(this);
        Movement.Initialize(this, paths);
        Initialized?.Invoke(this);
    }

    // =========================================================
    // Shields absorb hits first; any spillover damages hull. Armor remains future work.
    public void TakeDamage(float amount)
    {
        if (!IsAlive || !float.IsFinite(amount) || amount <= 0f) return;
        CommitDamage(DamageResolver.ResolveLegacy(amount, CurrentShield, Health));
    }

    // =========================================================
    // Typed attacks supply base units; the global resolver owns all defense-layer math.
    public void ApplyDamage(DamageInfo hit)
    {
        if (!IsAlive || hit.Amount == 0) return;
        CommitDamage(DamageResolver.Resolve(hit, CurrentShield, Health));
    }

    // =========================================================
    // One lifecycle path updates hull feedback and publishes a kill at most once.
    private void CommitDamage(DamageResult result)
    {
        float oldHealth = Health;
        CurrentShield = result.Shield; Health = result.Health;
        if (Health == oldHealth) return;
        _defenseBar?.SetHealth(Health, Definition.MaxHealth);
        if (Health <= 0f) Finish(false);
    }

    // =========================================================
    // Optional support restores instance shield points only; capacity/health/kill state never change.
    public float RestoreShield(float amount)
    {
        if (!IsAlive || !float.IsFinite(amount) || amount <= 0 || Definition == null ||
            !float.IsFinite(Definition.MaxShield) || Definition.MaxShield <= CurrentShield) return 0;
        float before = CurrentShield;
        CurrentShield = (float)Math.Min(Definition.MaxShield, (double)CurrentShield + amount);
        return CurrentShield - before;
    }

    // =========================================================
    // Only a damaged visible defense bar needs to counter-rotate as this ship turns.
    public void UpdateDefenseBarTransform() => _defenseBar?.AlignWithOwner(Rotation);

    // =========================================================
    // Arrival is counted once; CitadelSession applies its configured damage.
    public void Arrive() => Finish(true);

    // =========================================================
    // Immediately remove from targeting before deferred Godot deletion.
    private void Finish(bool arrived)
    {
        if (!IsAlive) return;
        IsAlive = false; Movement.SetPhysicsProcess(false);
        if (!arrived)
        {
            Sprite2D visual = GetNodeOrNull<Sprite2D>("Sprite");
            float fallback = Mathf.Max(1f, Definition?.RadiusPixels * 2f ?? 32f);
            DeathExplosion.Spawn(GetNodeOrNull<Node2D>("../../Effects"), GlobalPosition,
                DeathExplosion.MeasureVisualSize(visual, fallback), Definition?.DeathExplosionMultiplier ?? 1f);
        }
        _registry.Unregister(this); _registry.RecordOutcome(arrived, this);
        Removed?.Invoke(this); QueueFree();
    }

    // =========================================================
    // Also clean up when the whole level is unloaded.
    public override void _ExitTree() { IsAlive = false; _registry?.Unregister(this); }
}
