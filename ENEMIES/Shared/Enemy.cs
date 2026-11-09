// Shared lightweight enemy: health, lifecycle and registry membership, independent of type.
using Godot;
using System;

[Tool]
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
    public DamageSource KillingSource { get; private set; }
    public int SpawnGeneration { get; private set; }
    public event Action<Enemy> Initialized;
    public EnemyRegistry Registry => _registry;
    public CitadelDestination AssignedCitadel { get; private set; }
    private Transform2D? _authoredHullTransform;
    private Vector2 _authoredHullSize, _visualCentre;
    internal float AppliedVisualScale { get; private set; } = 1f;
    internal Vector2 VisualWorldCentre => ToGlobal(_visualCentre);

    // =========================================================
    // Apply from the authored transform so reinitializing never compounds the multiplier.
    // The debug preview uses this same entry point on its off-tree source.
    internal void ApplyHullVisualScale(Sprite2D hull)
    {
        AppliedVisualScale = Definition?.VisualScale ?? 1f;
        if (hull == null) return;
        _authoredHullTransform ??= hull.Transform;
        hull.Transform = _authoredHullTransform.Value.Scaled(Vector2.One * AppliedVisualScale);
    }

    // =========================================================
    // Weapon range and terrain sight retain the authored mount geometry in gameplay space.
    internal Vector2 AuthoredWeaponPoint(Vector2 worldPoint) => AppliedVisualScale == 1f ? worldPoint
        : GlobalPosition + (worldPoint - GlobalPosition) / AppliedVisualScale;

    #region Status Effects
    private EnemyStatusEffects _statuses;
    private TimedStatusScheduler _statusClock;
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
        if (!CanReceiveStatus(effect.Id) || !effect.IsValid) return false;
        _statuses ??= new EnemyStatusEffects();
        if (!_statuses.Apply(effect)) return false;
        if (_statusSprite == null)
        {
            _statusSprite = GetNodeOrNull<Sprite2D>("Sprite");
            if (_statusSprite != null) _originalStatusColor = _statusSprite.SelfModulate;
        }
        if (_statuses.HasTimed) { _statusClock ??= TimedStatusScheduler.For(_registry); _statusClock.Register(this); }
        RefreshStatusColor();
        StatusEffectFeedback.Applied(this, effect.Id);
        _thruster?.SetEffectiveSpeed(EffectiveMoveSpeed);
        return true;
    }

    // =========================================================
    // Release instance effects on every lifecycle exit without retaining their source tower.
    private void ClearStatuses()
    {
        if (GodotObject.IsInstanceValid(_statusClock)) _statusClock.Unregister(this);
        _statusClock = null; _statuses = null;
        if (GodotObject.IsInstanceValid(_statusSprite)) _statusSprite.SelfModulate = _originalStatusColor;
        _statusSprite = null;
    }
    // =========================================================
    // Expiry changes cached movement/presentation only, never routing or terrain clocks.
    internal bool OwnsStatuses(EnemyStatusEffects statuses) => IsAlive && IsInsideTree() && !IsQueuedForDeletion() && _statuses == statuses;
    internal bool AdvanceStatuses(double seconds)
    {
        if (!IsAlive || !IsInsideTree() || IsQueuedForDeletion() || _statuses == null) return false;
        if (GetTree().Paused) return _statuses.HasTimed;
        EnemyStatusEffects holder = _statuses;
        float movement = holder.MovementMultiplier;
        bool frozen = HasStatus(StatusEffectId.Freeze), corrupted = HasStatus(StatusEffectId.Corruption);
        bool active = holder.Advance(seconds, this);
        if (!GodotObject.IsInstanceValid(this) || !OwnsStatuses(holder)) return false;
        if (movement != holder.MovementMultiplier || frozen != HasStatus(StatusEffectId.Freeze) || corrupted != HasStatus(StatusEffectId.Corruption))
        { RefreshStatusColor(); _thruster?.SetEffectiveSpeed(EffectiveMoveSpeed); }
        return active;
    }
    // =========================================================
    // One resolver composes concurrent Slow and Freeze without touching DefenseBar or Modulate.
    private void RefreshStatusColor()
    {
        if (!GodotObject.IsInstanceValid(_statusSprite)) return;
        Color tint = HasStatus(StatusEffectId.Slow) ? new(.65f, .95f, 1f) : Colors.White;
        if (HasStatus(StatusEffectId.Freeze)) tint *= new Color(.4f, 1f, .45f);
        if (HasStatus(StatusEffectId.Corruption)) tint *= new Color(1f, .65f, .95f);
        _statusSprite.SelfModulate = _originalStatusColor * tint;
    }
    #endregion
    private EnemyRegistry _registry;
    internal InteractiveTileManager Terrain { get; private set; }
    private Vector2 _terrainPosition;
    private bool _terrainMovement;
    private DefenseBar _defenseBar;
    private EnemyDamageAppearanceProfile _damageAppearance;
    private Sprite2D _damageAppearanceSprite;
    private int _damageAppearanceStage = -1;
    private ShieldVisual _shieldVisual;
    private EnemyThrusterVisual _thruster;
    private static PackedScene _shieldVisualScene;
    private double _initialDurability, _damageConsumed, _damageXpPool, _killXp;
    private int _xpCredits;
    private bool _xpRulesCaptured;

    // =========================================================
    // Called by SpawnManager after adding the scene, using world-scoped services.
    public void Initialize(EnemyRegistry registry, PathRequestManager paths, bool spawnWithShield = false, RouteCheckpoint checkpoint = null, bool requiresCheckpoint = false, EnemyRouteHandoff routeHandoff = null, int generation = 0, CitadelDestination assignedCitadel = null, bool spawnWithStealth = false)
    {
        if (GodotObject.IsInstanceValid(_registry) && _registry.Contains(this)) _registry.Unregister(this);
        AssignedCitadel = routeHandoff?.Destination ?? assignedCitadel ?? paths.Navigation.SingleDestination;
        ClearStatuses();
        KillingSource = null; SpawnGeneration = generation; _registry = registry; Health = Mathf.Max(1f, Definition.MaxHealth); IsAlive = true;
        CurrentShield = spawnWithShield && float.IsFinite(Definition.MaxShield)
            ? Mathf.Max(0f, Definition.MaxShield) : 0f;
        _initialDurability = (double)Health + CurrentShield;
        _damageConsumed = 0; _damageXpPool = 0; _killXp = 0; _xpRulesCaptured = false;
        _xpCredits = Math.Max(0, Definition.KillRewardCredits);
        _defenseBar = GetNodeOrNull<DefenseBar>("DefenseBar");
        // Large hulls get extra bar clearance once, without per-type scene overrides.
        Sprite2D sprite = GetNodeOrNull<Sprite2D>("Sprite");
        ApplyHullVisualScale(sprite);
        _damageAppearance = Definition.DamageAppearance;
        _damageAppearanceSprite = _damageAppearance != null && _damageAppearance.Stages != null
            && _damageAppearance.Stages.Count > 0 ? sprite : null;
        _damageAppearanceStage = -1;
        RefreshDamageAppearance();
        Rect2 hullRect = sprite?.GetRect() ?? default;
        _authoredHullSize = sprite?.Texture != null
            ? hullRect.Size * sprite.Scale.Abs() / AppliedVisualScale
            : Vector2.One * Mathf.Max(8f, Definition.RadiusPixels * 2f);
        _visualCentre = sprite != null ? sprite.Transform * hullRect.GetCenter() : Vector2.Zero;
        _thruster = GetNodeOrNull<EnemyThrusterVisual>("Thruster");
        _thruster?.Configure(sprite, Definition, AppliedVisualScale);
        if (_defenseBar != null)
        {
            _defenseBar.Scale = Vector2.One * AppliedVisualScale;
            _defenseBar.SetOwnerVisualHeight(sprite?.Texture != null ? _authoredHullSize.Y : 0f, AppliedVisualScale);
        }
        FitShieldVisual();
        _defenseBar?.SetHealth(Health, Definition.MaxHealth);
        SyncShieldVisual(0f, CurrentShield, false);
        _defenseBar?.AlignWithOwner(Rotation);
        InitializeStealth(spawnWithStealth);
        Movement = GetNode<EnemyMovement>("Movement");
        SetNotifyLocalTransform(true); SetNotifyTransform(true);
        _registry.Register(this);
        Movement.Initialize(this, paths, checkpoint, requiresCheckpoint, routeHandoff);
        Terrain = paths.Navigation.Grid.InteractiveTiles; _terrainPosition = GlobalPosition;
        Terrain?.Reposition(this);
        Initialized?.Invoke(this);
    }

    // =========================================================
    // Optional per-type damage art is refreshed only on spawn and hull HP changes, not while moving.
    private void RefreshDamageAppearance()
    {
        if (!GodotObject.IsInstanceValid(_damageAppearanceSprite) || _damageAppearance?.Stages == null) return;
        int stage = EnemyDamageAppearance.StageForHealth(Health, Definition.MaxHealth, _damageAppearance.Stages.Count);
        if (stage < 0 || stage == _damageAppearanceStage) return;
        Texture2D next = _damageAppearance.Stages[stage];
        if (!GodotObject.IsInstanceValid(next)) return;
        _damageAppearanceStage = stage;
        if (_damageAppearanceSprite.Texture != next) _damageAppearanceSprite.Texture = next;
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
    public void ApplyDamage(DamageInfo hit) => ApplyDamageWithShieldAbsorption(hit);

    // =========================================================
    // Return actual committed absorption without resolving twice; capture before lethal callbacks can remove this enemy.
    public float ApplyDamageWithShieldAbsorption(DamageInfo hit)
    {
        if (!IsAlive || !IsInsideTree() || IsQueuedForDeletion() || hit.Amount == 0
            || !GodotObject.IsInstanceValid(_registry) || !_registry.IsInsideTree() || !_registry.Contains(this)) return 0;
        var result = DamageResolver.Resolve(hit, CurrentShield, Health);
        float absorbed = Mathf.Max(0, CurrentShield - result.Shield);
        CommitDamage(result, hit.Source); return absorbed;
    }

    // =========================================================
    // Presentation geometry is optional and captured before damage/death callbacks.
    public bool TryGetShieldRim(out Transform2D ellipse)
    {
        ellipse = default;
        return GodotObject.IsInstanceValid(_shieldVisual) && _shieldVisual.TryGetRim(out ellipse);
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
        if (oldHealth > 0 && Health <= 0) KillingSource = source;
        if (CurrentShield != oldShield) SyncShieldVisual(oldShield, CurrentShield);
        if (Health != oldHealth)
        {
            RefreshDamageAppearance();
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
    // Send the same authoritative shield update to the existing stacked HUD bar
    // and the lazy shield aura. Neither presentation system owns combat data.
    // =========================================================
    private void SyncShieldVisual(float before, float after, bool animate = true)
    {
        _defenseBar?.SetShield(after, Definition?.MaxShield ?? 0f);
        if (after > 0f && (_shieldVisual == null || !GodotObject.IsInstanceValid(_shieldVisual)))
        {
            _shieldVisualScene ??= GD.Load<PackedScene>("res://FEEDBACK/Shields/ShieldVisual.tscn");
            if (_shieldVisualScene == null) return;
            _shieldVisual = _shieldVisualScene.Instantiate<ShieldVisual>();
            _shieldVisual.Name = "ShieldVisual";
            AddChild(_shieldVisual);

            FitShieldVisual();
        }
        if (GodotObject.IsInstanceValid(_shieldVisual))
            _shieldVisual.SetShield(before, after, animate);
        RefreshStealthVisual();
    }

    // =========================================================
    // Use the pristine hull frame for stable placement across damage textures and reuse.
    private void FitShieldVisual()
    {
        if (!GodotObject.IsInstanceValid(_shieldVisual)) return;
        _shieldVisual.Position = _visualCentre;
        _shieldVisual.Scale = Vector2.One * AppliedVisualScale;
        _shieldVisual.SetVisualSize(_authoredHullSize);
    }

    // =========================================================
    // Only visible health/shield meters counter-rotate; idle or absent meters do no work.
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
        _thruster?.Stop();
        if (!arrived)
        {
            KilledBeforeRemoval?.Invoke(this);
            Sprite2D visual = GetNodeOrNull<Sprite2D>("Sprite");
            float fallback = Mathf.Max(1f, (Definition?.RadiusPixels * 2f ?? 32f) * AppliedVisualScale);
            DeathExplosion.Spawn(GetNodeOrNull<Node2D>("../../Effects"), VisualWorldCentre,
                DeathExplosion.MeasureVisualSize(visual, fallback), Definition?.DeathExplosionMultiplier ?? 1f);
        }
        _registry.Unregister(this); _registry.RecordOutcome(arrived, this);
        Removed?.Invoke(this); QueueFree();
    }

    // =========================================================
    // Also clean up when the whole level is unloaded.
    public override void _ExitTree()
    {
        ClearStatuses(); ClearStealth();
        _damageAppearance = null; _damageAppearanceSprite = null; _damageAppearanceStage = -1;
        if (GodotObject.IsInstanceValid(_shieldVisual)) _shieldVisual.ResetVisual();
        _shieldVisual = null; _thruster = null;
        bool unresolved = IsAlive; IsAlive = false; _registry?.Unregister(this);
        Terrain = null; if (unresolved) Removed?.Invoke(this);
    }
}
