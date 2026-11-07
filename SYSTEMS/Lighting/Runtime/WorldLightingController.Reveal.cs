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
    private sealed class EnemyReveal(float original)
    {
        internal readonly float Original = original;
        internal float Applied = original;
        internal float ContactOpacity;
        internal Vector2 ContactPosition;
    }

    private readonly Dictionary<Enemy, EnemyReveal> _enemyAlpha = new();
    private readonly HashSet<ShaderMaterial> _smokeMaterials = new();
    private EnemyRegistry _revealEnemies;
    private readonly HashSet<EnemyReveal> _darkContacts = new();
    private ShaderMaterial _entityMask;
    private Node2D _contactOverlay;
    private static readonly Color ContactCore = new("#be4b50");
    private static readonly Color ContactHalo = new("#682c35");
    private Image _maskImage;
    private ImageTexture _maskTexture;
    private byte[] _maskPixels;
    private Rect2 _maskBounds;
    private int _maskWidth, _maskHeight;
    private bool _revealDirty = true, _maskDirty = true, _movingReveal;
    private double _revealRemaining;
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
        _contactOverlay = new Node2D
        {
            Name = "DarkContacts", ZIndex = 5,
            Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded }
        };
        _contactOverlay.Draw += DrawDarkContacts;
        AddChild(_contactOverlay);

        _revealEnemies = GetParent().GetNodeOrNull<EnemyRegistry>("EnemySystems/Registry");
        if (_revealEnemies != null)
        {
            _revealEnemies.EnemyRegistered += TrackEnemyReveal;
            _revealEnemies.EnemyUnregistered += UntrackEnemyReveal;
            _revealEnemies.EnemyPositionChanged += UpdateEnemyReveal;
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
        if (!_enemyAlpha.ContainsKey(enemy)) _enemyAlpha.Add(enemy, new EnemyReveal(enemy.Modulate.A));
        UpdateEnemyReveal(enemy);
    }

    private void UntrackEnemyReveal(Enemy enemy)
    {
        if (!_enemyAlpha.Remove(enemy, out EnemyReveal state)) return;
        if (_darkContacts.Remove(state)) _contactOverlay.QueueRedraw();
        if (GodotObject.IsInstanceValid(enemy)) SetEnemyAlpha(enemy, state.Original);
    }

    private void UpdateEnemyReveal(Enemy enemy)
    {
        if (!_enemyAlpha.TryGetValue(enemy, out EnemyReveal state)) return;
        Vector2 position = enemy.GlobalPosition;
        if (RevealVisible) EnsureRevealField();
        float reveal = RevealVisible ? _reveal.AlphaAt(position) : 1;
        float alpha = state.Original * reveal;
        if (!Mathf.IsEqualApprox(alpha, state.Applied))
        {
            state.Applied = alpha;
            SetEnemyAlpha(enemy, alpha);
        }

        // The contact cross-fades inversely to the whole enemy root, including its health bar.
        float opacity = 1 - reveal;
        if (opacity <= .001f)
        {
            if (_darkContacts.Remove(state)) _contactOverlay.QueueRedraw();
            return;
        }
        bool changed = _darkContacts.Add(state) || state.ContactPosition != position ||
            !Mathf.IsEqualApprox(state.ContactOpacity, opacity);
        if (!changed) return;
        state.ContactPosition = position;
        state.ContactOpacity = opacity;
        _contactOverlay.QueueRedraw();
    }

    // =========================================================
    // One unlit canvas draws only contacts that are currently fading or in darkness.
    private void DrawDarkContacts()
    {
        if (_darkContacts.Count == 0) return;
        _contactOverlay.DrawSetTransformMatrix(_contactOverlay.GlobalTransform.AffineInverse());
        foreach (EnemyReveal state in _darkContacts)
        {
            Color halo = ContactHalo, core = ContactCore;
            halo.A = state.ContactOpacity * .42f;
            core.A = state.ContactOpacity * .85f;
            _contactOverlay.DrawCircle(state.ContactPosition, 5, halo, true, -1, true);
            _contactOverlay.DrawCircle(state.ContactPosition, 3, core, true, -1, true);
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
        foreach (Enemy enemy in _enemyAlpha.Keys) UpdateEnemyReveal(enemy);
    }

    private void ApplyRevealToggle()
    {
        if (_entityMask == null) return;
        _entityMask.SetShaderParameter("reveal_enabled", RevealVisible);
        foreach (ShaderMaterial material in _smokeMaterials)
            material.SetShaderParameter("reveal_enabled", RevealVisible);
        foreach (Enemy enemy in _enemyAlpha.Keys) UpdateEnemyReveal(enemy);
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
            _revealEnemies.EnemyPositionChanged -= UpdateEnemyReveal;
        }
        ApplyRevealToggle();
        _contactOverlay.Draw -= DrawDarkContacts;
        _darkContacts.Clear();
        _enemyAlpha.Clear();
        _smokeMaterials.Clear();
        _suppressed.Clear();
        _reveal.Clear();
    }
}
