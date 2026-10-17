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
    public float RemainingJourneyDistance => Movement?.RemainingJourneyDistance ?? float.PositiveInfinity;
    public float RemainingPathDistance => Movement?.RemainingPathDistance ?? float.PositiveInfinity;
    public event Action<Enemy> Removed;
    public event Action<Enemy> KilledBeforeRemoval;
    public int SpawnGeneration { get; private set; }
    public event Action<Enemy> Initialized;
    public EnemyRegistry Registry => _registry;
    private EnemyStatusEffects _statuses;
    private Sprite2D _statusSprite;
    private Color _originalStatusColor;
    public float EffectiveMoveSpeed => Mathf.Max(0, Definition.MoveSpeed) * (_statuses?.MovementMultiplier ?? 1);
    public bool HasStatus(StatusEffectId id) => _statuses?.Has(id) == true;
    public bool CanReceiveStatus(StatusEffectId id) => IsAlive && IsInsideTree() && !IsQueuedForDeletion()
        && GodotObject.IsInstanceValid(_registry) && _registry.Contains(this)
        && Definition != null && Definition.StatusImmunities?.Contains(id) != true;

    // =========================================================
    // Shared impact API changes instance state only; status application itself grants no XP or Credits.
    public bool ApplyStatus(StatusEffectPayload effect)
    {
        if (!CanReceiveStatus(effect.Id) || effect.Strength <= 0 || !float.IsFinite(effect.Strength)
            || effect.Strength >= 1 || !System.Enum.IsDefined(effect.Id)) return false;
        _statuses ??= new EnemyStatusEffects();
        if (!_statuses.Apply(effect)) return false;
        if (_statusSprite == null)
        {
            _statusSprite = GetNodeOrNull<Sprite2D>("Sprite");
            if (_statusSprite != null) _originalStatusColor = _statusSprite.SelfModulate;
        }
        if (GodotObject.IsInstanceValid(_statusSprite))
            _statusSprite.SelfModulate = _originalStatusColor * new Color(.65f, .95f, 1f);
        return true;
    }

    // =========================================================
    // Release instance effects on every lifecycle exit without retaining their source tower.
    private void ClearStatuses()
    {
        _statuses = null;
        if (GodotObject.IsInstanceValid(_statusSprite)) _statusSprite.SelfModulate = _originalStatusColor;
        _statusSprite = null;
    }
    private EnemyRegistry _registry;
    internal InteractiveTileManager Terrain { get; private set; }
    private Vector2 _terrainPosition;
    private bool _terrainMovement;
    private DefenseBar _defenseBar;
    private ShieldVisual _shieldVisual;
    private static PackedScene _shieldVisualScene;
    private double _initialDurability, _damageConsumed, _damageXpPool, _killXp;
    private int _xpCredits;
    private bool _xpRulesCaptured;

    // =========================================================
    // Called by SpawnManager after adding the scene, using world-scoped services.
    public void Initialize(EnemyRegistry registry, PathRequestManager paths, bool spawnWithShield = false, RouteCheckpoint checkpoint = null, bool requiresCheckpoint = false, EnemyRouteHandoff routeHandoff = null, int generation = 0)
    {
        ClearStatuses();
        SpawnGeneration = generation; _registry = registry; Health = Mathf.Max(1f, Definition.MaxHealth); IsAlive = true;
        CurrentShield = spawnWithShield && float.IsFinite(Definition.MaxShield)
            ? Mathf.Max(0f, Definition.MaxShield) : 0f;
        _initialDurability = (double)Health + CurrentShield;
        _damageConsumed = 0; _damageXpPool = 0; _killXp = 0; _xpRulesCaptured = false;
        _xpCredits = Math.Max(0, Definition.KillRewardCredits);
        _defenseBar = GetNodeOrNull<DefenseBar>("DefenseBar");
        _defenseBar?.SetHealth(Health, Definition.MaxHealth);
        SyncShieldVisual(0f, CurrentShield, false);
        Movement = GetNode<EnemyMovement>("Movement");
        SetNotifyLocalTransform(true); SetNotifyTransform(true);
        _registry.Register(this);
        Movement.Initialize(this, paths, checkpoint, requiresCheckpoint, routeHandoff);
        Terrain = paths.Navigation.Grid.InteractiveTiles; _terrainPosition = GlobalPosition;
        Terrain?.Reposition(this);
        Initialized?.Invoke(this);
    }

    // =========================================================
    // Keep proximity buckets current for movement and direct position assignments, with no polling scan.
    public override void _Notification(int what)
    {
        if ((what == NotificationLocalTransformChanged || what == NotificationTransformChanged) && IsAlive
            && GodotObject.IsInstanceValid(_registry))
        {
            _registry.UpdatePosition(this);
            if (_terrainMovement) return;
            Vector2 position = GlobalPosition;
            if (position != _terrainPosition)
            {
                _terrainPosition = position;
                if (!_terrainMovement && GodotObject.IsInstanceValid(Terrain)) Terrain.Reposition(this);
            }
        }
    }

    // =========================================================
    // Actual movement keeps spatial notifications but bypasses teleport semantics in terrain notifications.
    internal void SetMovementPosition(Vector2 position)
    {
        _terrainMovement = true;
        try { GlobalPosition = position; _terrainPosition = position; }
        finally { _terrainMovement = false; }
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
        if (!IsAlive || !IsInsideTree() || IsQueuedForDeletion() || hit.Amount == 0
            || !GodotObject.IsInstanceValid(_registry) || !_registry.IsInsideTree() || !_registry.Contains(this)) return;
        CommitDamage(DamageResolver.Resolve(hit, CurrentShield, Health), hit.Source);
    }

    // =========================================================
    // One lifecycle path updates hull feedback and publishes a kill at most once.
    private void CommitDamage(DamageResult result, DamageSource source = null)
    {
        float oldHealth = Health;
        double actual = Math.Max(0, (double)Health - result.Health) + Math.Max(0, (double)CurrentShield - result.Shield);
        double eligible = Math.Min(actual, Math.Max(0, _initialDurability - _damageConsumed));
        _damageConsumed += eligible; // Unattributed damage also consumes the finite budget; healing cannot refill it.
        double xp = 0;
        if (actual > 0 && source != null && source.TryGetRules(out TowerMasterConfig rules))
        {
            if (!_xpRulesCaptured)
            {
                _damageXpPool = _xpCredits * TowerRankController.SafeMultiplier(rules.DamageXpMultiplier);
                _killXp = _xpCredits * TowerRankController.SafeMultiplier(rules.KillXpMultiplier);
                _xpRulesCaptured = true;
            }
            if (_initialDurability > 0 && double.IsFinite(_initialDurability)) xp = _damageXpPool * (eligible / _initialDurability);
            if (oldHealth > 0 && result.Health <= 0) xp += _killXp;
        }
        float oldShield = CurrentShield;
        CurrentShield = result.Shield; Health = result.Health;
        if (CurrentShield != oldShield) SyncShieldVisual(oldShield, CurrentShield);
        if (Health != oldHealth)
        {
            _defenseBar?.SetHealth(Health, Definition.MaxHealth);
            if (Health <= 0f) Finish(false);
        }
        if (xp > 0) source?.Credit(xp);
    }

    // =========================================================
    // Optional support restores instance shield points only; capacity/health/kill state never change.
    public float RestoreShield(float amount)
    {
        if (!IsAlive || !float.IsFinite(amount) || amount <= 0 || Definition == null ||
            !float.IsFinite(Definition.MaxShield) || Definition.MaxShield <= CurrentShield) return 0;
        float before = CurrentShield;
        CurrentShield = (float)Math.Min(Definition.MaxShield, (double)CurrentShield + amount);
        if (CurrentShield != before) SyncShieldVisual(before, CurrentShield);
        return CurrentShield - before;
    }

    // =========================================================
    // Shield feedback is a lazy child of shielded recipients only; ordinary enemies allocate no effect.
    // A future damageable tower can use ShieldVisual.SetShield directly without any Enemy dependency.
    private void SyncShieldVisual(float before, float after, bool animate = true)
    {
        if (after > 0f && (_shieldVisual == null || !GodotObject.IsInstanceValid(_shieldVisual)))
        {
            _shieldVisualScene ??= GD.Load<PackedScene>("res://FEEDBACK/Shields/ShieldVisual.tscn");
            if (_shieldVisualScene == null) return;
            _shieldVisual = _shieldVisualScene.Instantiate<ShieldVisual>();
            _shieldVisual.Name = "ShieldVisual";
            AddChild(_shieldVisual);

            Sprite2D sprite = GetNodeOrNull<Sprite2D>("Sprite");
            Vector2 dimensions = sprite != null && sprite.Texture != null
                ? sprite.GetRect().Size * new Vector2(Mathf.Abs(sprite.Scale.X), Mathf.Abs(sprite.Scale.Y))
                : Vector2.One * Mathf.Max(8f, (Definition?.RadiusPixels ?? 16f) * 2f);
            _shieldVisual.Position = sprite?.Position ?? Vector2.Zero;
            _shieldVisual.SetVisualSize(dimensions);
        }
        if (GodotObject.IsInstanceValid(_shieldVisual))
            _shieldVisual.SetShield(before, after, animate);
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
        IsAlive = false; ClearStatuses(); Movement.SetPhysicsProcess(false);
        if (!arrived)
        {
            KilledBeforeRemoval?.Invoke(this);
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
    public override void _ExitTree()
    {
        ClearStatuses();
        if (GodotObject.IsInstanceValid(_shieldVisual)) _shieldVisual.ResetVisual();
        _shieldVisual = null;
        bool unresolved = IsAlive; IsAlive = false; _registry?.Unregister(this);
        Terrain = null; if (unresolved) Removed?.Invoke(this);
    }
}
