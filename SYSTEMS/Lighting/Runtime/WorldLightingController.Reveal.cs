// Camera-independent reveal ownership, source suppression and the shared world-space visual mask.
using Godot;
using System;
using System.Collections.Generic;

public partial class WorldLightingController
{
    private const double MovingRevealInterval = .10;
    private readonly LightRevealField _reveal = new();
    private readonly List<Source> _portals = new();
    private readonly HashSet<Node2D> _suppressed = new();
    private const double EnemyRevealPhaseSeconds = 1.0 / 60.0;
    private readonly HashSet<Enemy>[] _pendingEnemyReveal = { new(), new() };
    private int _enemyRevealPhase, _nextEnemyPhase;
    private double _enemyRevealElapsed;
    private bool _contactsDirty, _contactRedrawQueued;
    private bool HasPendingEnemyReveal => _pendingEnemyReveal[0].Count + _pendingEnemyReveal[1].Count > 0;

    private sealed class EnemyReveal(float original, int phase)
    {
        internal readonly float Original = original;
        internal readonly int Phase = phase;
        internal float Applied = original;
        internal uint FieldRevision;
        internal Vector2 SamplePosition;
        internal float ContactOpacity;
        internal Vector2 ContactPosition;
    }

    private readonly Dictionary<Enemy, EnemyReveal> _enemyAlpha = new();
    private readonly HashSet<ShaderMaterial> _smokeMaterials = new();
    private EnemyRegistry _revealEnemies;
    private readonly HashSet<EnemyReveal> _darkContacts = new();
    private ShaderMaterial _entityMask;
    private Node2D _contactOverlay;
    private const float ContactHalfSize = 6f;
    private Texture2D _contactTexture;
    private Transform2D _contactCanvas;
    private Rect2 _contactViewport;
    private Image _maskImage;
    private ImageTexture _maskTexture;
    private byte[] _maskPixels;
    private Rect2 _maskBounds;
    private int _maskWidth, _maskHeight;
    private bool _revealDirty = true, _maskDirty = true, _movingReveal;
    private double _revealRemaining;
    private uint _revealRevision;
    private bool RevealActive => _ambient != null && Config.RevealEnabled;
    private bool RevealVisible => RevealActive && _lightingEnabled;

    // =========================================================
    // Combat keeps the same world footprints with the camera, visual light pool or F6 comparison off.
    public bool IsRevealed(Vector2 position)
    {
        if (!RevealActive) return true;
        EnsureRevealField();
        return _reveal.Contains(position);
    }

    internal bool IsEnemyRevealed(Enemy enemy)
    {
        bool revealed = IsRevealed(enemy.GlobalPosition);
        if (revealed && RevealVisible && _enemyAlpha.TryGetValue(enemy, out EnemyReveal state) &&
            state.ContactOpacity >= 1f)
            RefreshEnemyRevealForCombat(enemy);
        return revealed;
    }

    // =========================================================
    // Blackout callers suppress the source owner, including both a tower's range and its native lamp.
    public void SetSourceSuppressed(Node2D sourceOwner, bool suppressed)
    {
        if (!GodotObject.IsInstanceValid(sourceOwner)) return;
        Node2D source = _disconnect.ContainsKey(sourceOwner) ? sourceOwner
            : sourceOwner.GetNodeOrNull<DroneLightAttachment>("Body/FlightLight");
        if (source == null || !_disconnect.ContainsKey(source)) return;
        if (suppressed ? _suppressed.Add(source) : _suppressed.Remove(source)) QueueRefresh();
    }

    // =========================================================
    // Reuse registered portal paths. Forming/open transitions refresh before the next target query.
    private void TrackPortals()
    {
        if (!HasOutput(Config.Portal) || Grid.Level.Settings?.ActivePortals == null) return;
        foreach (NodePath path in Grid.Level.Settings.ActivePortals)
        {
            if (Grid.Level.GetNodeOrNull<EnemyPortal>(path) is not { } portal || _disconnect.ContainsKey(portal)) continue;
            float radius = Config.Portal.RadiusPixels * Mathf.Sqrt(Mathf.Max(1, portal.SizeMultiplier));
            Source source = new(portal, Config.Portal, Config.Portal.LocalOffset, radius, () => portal.IsSpawnActive);
            Watch(_portals, source, portal);
            portal.LightActivationChanged += QueueRefresh;
            Action exit = () => Untrack(portal);
            portal.TreeExiting += exit;
            _disconnect[portal] += () =>
            {
                portal.LightActivationChanged -= QueueRefresh;
                portal.TreeExiting -= exit;
            };
        }
    }

    // =========================================================
    // The same eligibility predicate serves reveal and cosmetic source selection.
    private bool IsAvailable(Source source)
    {
        if (source.Eligible?.Invoke() == false) return false;
        return source.Anchor is not { } anchor ||
            (anchor.IsInsideTree() && !anchor.IsQueuedForDeletion() && anchor.IsVisibleInTree() && !_suppressed.Contains(anchor));
    }

    // =========================================================
    // Source events invalidate synchronously. Airborne drone positions refresh on one shared clock.
    private void EnsureRevealField()
    {
        if (!_revealDirty) return;
        _revealDirty = false;
        _revealRevision++;
        _maskDirty = true;
        _reveal.Clear();
        _movingReveal = false;
        AddRevealSources(_citadels);
        AddRevealSources(_portals);
        AddRevealSources(_towers);
        AddRevealSources(_lava);
        foreach (Source drone in _drones)
        {
            if (!IsAvailable(drone)) continue;
            _movingReveal = true;
            _reveal.Add(PositionFor(drone), RadiusFor(drone));
        }
    }

    private void AddRevealSources(List<Source> sources)
    {
        foreach (Source source in sources)
        {
            if (!IsAvailable(source)) continue;
            _reveal.Add(PositionFor(source), RadiusFor(source));
        }
    }

    // =========================================================
    // One bounded mask texture per world. Enemy roots fade together with health/status/weapon visuals.
    private void SetupRevealPresentation()
    {
        if (!Config.RevealEnabled) return;
        Grid.Lighting = this;
        Rect2 gridBounds = Grid.WorldBounds;
        Vector2 end = Grid.ToGlobal(gridBounds.End);
        _maskBounds = new Rect2(Grid.ToGlobal(gridBounds.Position), Vector2.Zero).Expand(end).Grow(256);
        _maskWidth = Mathf.Clamp(Mathf.CeilToInt(_maskBounds.Size.X / 16), 1, 512);
        _maskHeight = Mathf.Clamp(Mathf.CeilToInt(_maskBounds.Size.Y / 16), 1, 512);
        _maskPixels = new byte[_maskWidth * _maskHeight];
        _maskImage = Image.CreateFromData(_maskWidth, _maskHeight, false, Image.Format.R8, _maskPixels);
        _maskTexture = ImageTexture.CreateFromImage(_maskImage);
        _entityMask = NewRevealMaterial("res://SYSTEMS/Lighting/Shaders/RevealEntities.gdshader");
        _contactTexture = GD.Load<Texture2D>("res://SYSTEMS/Lighting/Textures/RadarContact.svg");
        _contactOverlay = new Node2D
        {
            Name = "DarkContacts", ZIndex = 5, TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded }
        };
        _contactOverlay.Draw += DrawDarkContacts;
        AddChild(_contactOverlay);

        _revealEnemies = GetParent().GetNodeOrNull<EnemyRegistry>("EnemySystems/Registry");
        if (_revealEnemies != null)
        {
            _revealEnemies.EnemyRegistered += TrackEnemyReveal;
            _revealEnemies.EnemyUnregistered += UntrackEnemyReveal;
            _revealEnemies.EnemyPositionChanged += OnEnemyRevealMoved;
            foreach (Enemy enemy in _revealEnemies.Living) TrackEnemyReveal(enemy);
        }
        BindExistingRevealVisuals(GetParent());
        GetTree().NodeAdded += BindRevealVisual;
    }

    private ShaderMaterial NewRevealMaterial(string path)
    {
        ShaderMaterial material = new() { Shader = GD.Load<Shader>(path) };
        BindMask(material);
        return material;
    }

    private void BindMask(ShaderMaterial material)
    {
        material.SetShaderParameter("reveal_mask", _maskTexture);
        material.SetShaderParameter("reveal_bounds", new Vector4(_maskBounds.Position.X, _maskBounds.Position.Y,
            _maskBounds.Size.X, _maskBounds.Size.Y));
        material.SetShaderParameter("reveal_enabled", RevealVisible);
    }

    private void BindExistingRevealVisuals(Node node)
    {
        BindRevealVisual(node);
        foreach (Node child in node.GetChildren()) BindExistingRevealVisuals(child);
    }

    // =========================================================
    // Mask real particle pixels and shared projectile draw surfaces. Preserve procedural portal smoke.
    private void BindRevealVisual(Node node)
    {
        if (node is not (GpuParticles2D or CpuParticles2D or BasicProjectile or SplashProjectile
            or FriendlyProjectileSystem or EnemyBulletRenderer or RicochetController or HullHitSparkController
            or WeaponVisualRenderer or ShockwaveEmitter or ShockwaveEffect)) return;
        if (!GetParent().IsAncestorOf(node) || node is not CanvasItem canvas) return;
        if (canvas.Material == null) canvas.Material = _entityMask;
        else if (canvas.Material is ShaderMaterial material &&
            material.Shader?.ResourcePath == "res://WORLD/PORTALS/EnemyPortal/Visuals/PortalSmoke.gdshader")
        {
            BindMask(material);
            _smokeMaterials.Add(material);
        }
    }

    private void TrackEnemyReveal(Enemy enemy)
    {
        if (!_enemyAlpha.TryGetValue(enemy, out EnemyReveal state))
        {
            state = new EnemyReveal(enemy.Modulate.A, _nextEnemyPhase);
            _nextEnemyPhase ^= 1;
            _enemyAlpha.Add(enemy, state);
        }
        UpdateEnemyReveal(enemy, state, enemy.GlobalPosition);
    }

    private void UntrackEnemyReveal(Enemy enemy)
    {
        if (!_enemyAlpha.Remove(enemy, out EnemyReveal state)) return;
        _pendingEnemyReveal[state.Phase].Remove(enemy);
        if (_darkContacts.Remove(state)) InvalidateContacts();
        if (!GodotObject.IsInstanceValid(enemy)) return;
        SetEnemyAlpha(enemy, state.Original);
        if (enemy.IsAlive) enemy.SetFogConcealed(false);
    }

    // =========================================================
    // Movement records the latest contact position. Only the staggered pass samples opacity.
    private void OnEnemyRevealMoved(Enemy enemy)
    {
        if (!RevealVisible || !_enemyAlpha.TryGetValue(enemy, out EnemyReveal state)) return;
        Vector2 position = enemy.GlobalPosition;
        if (state.ContactOpacity > .001f && state.ContactPosition != position)
        {
            state.ContactPosition = position;
            InvalidateContacts();
        }
        if (state.SamplePosition == position) return;
        bool wasIdle = !HasPendingEnemyReveal;
        _pendingEnemyReveal[state.Phase].Add(enemy);
        if (wasIdle) SetProcess(true);
    }

    // =========================================================
    // A shared 60 Hz phase clock visits each moving cohort at most 30 times per second.
    private void TickEnemyReveal(double delta)
    {
        if (!HasPendingEnemyReveal) { _enemyRevealElapsed = 0; return; }
        _enemyRevealElapsed += delta;
        int phases = (int)(_enemyRevealElapsed / EnemyRevealPhaseSeconds);
        if (phases == 0) return;
        _enemyRevealElapsed -= phases * EnemyRevealPhaseSeconds;
        phases = Math.Min(2, phases);
        for (int i = 0; i < phases; i++)
        {
            var pending = _pendingEnemyReveal[_enemyRevealPhase];
            _enemyRevealPhase ^= 1;
            foreach (Enemy enemy in pending)
            {
                EnemyReveal state = _enemyAlpha[enemy];
                Vector2 position = enemy.GlobalPosition;
                if (state.SamplePosition != position) UpdateEnemyReveal(enemy, state, position);
            }
            pending.Clear();
        }
    }

    // =========================================================
    // Combat feedback can refresh presentation without changing or caching gameplay detection.
    internal void RefreshEnemyRevealForCombat(Enemy enemy)
    {
        if (!RevealVisible || !_enemyAlpha.TryGetValue(enemy, out EnemyReveal state)) return;
        Vector2 position = enemy.GlobalPosition;
        if (!_revealDirty && state.FieldRevision == _revealRevision && state.SamplePosition == position) return;
        _pendingEnemyReveal[state.Phase].Remove(enemy);
        UpdateEnemyReveal(enemy, state, position);
    }

    private void UpdateEnemyReveal(Enemy enemy, EnemyReveal state, Vector2 position)
    {
        if (RevealVisible) EnsureRevealField();
        float reveal = RevealVisible ? _reveal.AlphaAt(position) : 1;
        state.SamplePosition = position;
        state.FieldRevision = _revealRevision;
        bool concealed = reveal <= 0f;
        enemy.SetFogConcealed(concealed);

        float alpha = state.Original * reveal;
        if (!Mathf.IsEqualApprox(alpha, state.Applied))
        {
            state.Applied = alpha;
            SetEnemyAlpha(enemy, alpha);
        }

        // The contact cross-fades inversely to the whole enemy root, including its health bar.
        float opacity = 1 - reveal;
        bool changed = opacity <= .001f ? _darkContacts.Remove(state) :
            _darkContacts.Add(state) || state.ContactPosition != position ||
            !Mathf.IsEqualApprox(state.ContactOpacity, opacity);
        state.ContactPosition = position;
        state.ContactOpacity = opacity;
        if (changed) InvalidateContacts();
    }

    private void InvalidateContacts()
    {
        if (_contactsDirty) return;
        _contactsDirty = true;
        SetProcess(true);
    }

    // =========================================================
    // Flush once from the render-frame loop after all movement and coverage updates.
    private void FlushContactRedraw()
    {
        if (_contactOverlay == null || _contactRedrawQueued) return;
        if (!_contactsDirty && (_darkContacts.Count == 0 ||
            (_contactCanvas == _contactOverlay.GetCanvasTransform() &&
             _contactViewport == _contactOverlay.GetViewportRect()))) return;
        _contactsDirty = false;
        _contactRedrawQueued = true;
        _contactOverlay.QueueRedraw();
    }

    // =========================================================
    // One shared SVG quad per visible contact. Keep the viewport margin large enough for the whole sprite.
    private void DrawDarkContacts()
    {
        _contactRedrawQueued = false;
        _contactsDirty = false;
        if (_darkContacts.Count == 0 || _contactTexture == null) return;
        _contactCanvas = _contactOverlay.GetCanvasTransform();
        _contactViewport = _contactOverlay.GetViewportRect();
        Transform2D toLocal = _contactOverlay.GlobalTransform.AffineInverse();
        Transform2D worldToScreen = _contactCanvas;
        Vector2 margin = (worldToScreen.X.Abs() + worldToScreen.Y.Abs()) * ContactHalfSize;
        Rect2 visible = new(_contactViewport.Position - margin, _contactViewport.Size + margin * 2);
        Vector2 half = Vector2.One * ContactHalfSize;
        _contactOverlay.DrawSetTransformMatrix(toLocal);
        foreach (EnemyReveal state in _darkContacts)
        {
            if (!visible.HasPoint(worldToScreen * state.ContactPosition)) continue;
            _contactOverlay.DrawTextureRect(_contactTexture, new Rect2(state.ContactPosition - half, half * 2),
                false, new Color(1f, 1f, 1f, state.ContactOpacity));
        }
        _contactOverlay.DrawSetTransformMatrix(Transform2D.Identity);
    }

    private static void SetEnemyAlpha(Enemy enemy, float alpha)
    {
        Color modulate = enemy.Modulate;
        if (Mathf.IsEqualApprox(modulate.A, alpha)) return;
        modulate.A = alpha;
        enemy.Modulate = modulate;
    }

    // =========================================================
    // Upload only when the field changes and is visible. F6 changes presentation without changing combat.
    private void UpdateRevealPresentation()
    {
        if (!RevealActive || _entityMask == null) return;
        EnsureRevealField();
        if (!RevealVisible || !_maskDirty) return;
        _maskDirty = false;
        _reveal.Rasterize(_maskBounds, _maskWidth, _maskHeight, _maskPixels);
        _maskImage.SetData(_maskWidth, _maskHeight, false, Image.Format.R8, _maskPixels);
        _maskTexture.Update(_maskImage);
        RefreshAllEnemyReveal();
    }

    private void ApplyRevealToggle()
    {
        if (_entityMask == null) return;
        _entityMask.SetShaderParameter("reveal_enabled", RevealVisible);
        foreach (ShaderMaterial material in _smokeMaterials)
            material.SetShaderParameter("reveal_enabled", RevealVisible);
        RefreshAllEnemyReveal();
    }

    private void RefreshAllEnemyReveal()
    {
        foreach (var pending in _pendingEnemyReveal) pending.Clear();
        foreach (var entry in _enemyAlpha)
            UpdateEnemyReveal(entry.Key, entry.Value, entry.Key.GlobalPosition);
    }

    private void TickMovingReveal(double delta)
    {
        if (!RevealActive || !_movingReveal) return;
        _revealRemaining -= delta;
        if (_revealRemaining > 0) return;
        _revealRemaining = MovingRevealInterval;
        _revealDirty = true;
        UpdateRevealPresentation();
        if (!RevealVisible) EnsureRevealField();
    }

    private void StopRevealPresentation()
    {
        if (GodotObject.IsInstanceValid(Grid) && Grid.Lighting == this) Grid.Lighting = null;
        if (_entityMask == null) return;
        GetTree().NodeAdded -= BindRevealVisual;
        if (GodotObject.IsInstanceValid(_revealEnemies))
        {
            _revealEnemies.EnemyRegistered -= TrackEnemyReveal;
            _revealEnemies.EnemyUnregistered -= UntrackEnemyReveal;
            _revealEnemies.EnemyPositionChanged -= OnEnemyRevealMoved;
        }
        foreach (var entry in _enemyAlpha)
        {
            if (!GodotObject.IsInstanceValid(entry.Key)) continue;
            SetEnemyAlpha(entry.Key, entry.Value.Original);
            if (entry.Key.IsAlive) entry.Key.SetFogConcealed(false);
        }
        foreach (var pending in _pendingEnemyReveal) pending.Clear();
        _contactOverlay.Draw -= DrawDarkContacts;
        _darkContacts.Clear();
        _enemyAlpha.Clear();
        _smokeMaterials.Clear();
        _suppressed.Clear();
        _reveal.Clear();
    }
}
