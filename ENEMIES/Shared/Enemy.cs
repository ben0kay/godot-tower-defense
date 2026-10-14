// Shared lightweight enemy: health, lifecycle and registry membership, independent of type.
using Godot;
using System;

public partial class Enemy : Node2D
{
    [Export] public EnemyDefinition Definition { get; set; }
    public float Health { get; private set; }
    public bool IsAlive { get; private set; }
    public EnemyMovement Movement { get; private set; }
    public float RemainingPathDistance => Movement?.RemainingPathDistance ?? float.PositiveInfinity;
    public event Action<Enemy> Removed;
    private EnemyRegistry _registry;
    private DefenseBar _defenseBar;

    // =========================================================
    // Called by SpawnManager after adding the scene, using world-scoped services.
    public void Initialize(EnemyRegistry registry, PathRequestManager paths)
    {
        _registry = registry; Health = Mathf.Max(1f, Definition.MaxHealth); IsAlive = true;
        _defenseBar = GetNodeOrNull<DefenseBar>("DefenseBar");
        _defenseBar?.SetHealth(Health, Definition.MaxHealth);
        Movement = GetNode<EnemyMovement>("Movement");
        _registry.Register(this);
        Movement.Initialize(this, paths);
    }

    // =========================================================
    // Armor is reserved for a later damage model; current bullets deal direct damage.
    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f) return;
        Health = Mathf.Max(0f, Health - amount);
        _defenseBar?.SetHealth(Health, Definition.MaxHealth);
        if (Health <= 0f) Finish(false);
    }

    // =========================================================
    // Only a damaged visible defense bar needs to counter-rotate as this ship turns.
    public void UpdateDefenseBarTransform() => _defenseBar?.AlignWithOwner(Rotation);

    // =========================================================
    // Arrival is counted and removes the ship; Citadel health is intentionally future work.
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
