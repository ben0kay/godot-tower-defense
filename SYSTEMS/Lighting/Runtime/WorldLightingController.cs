// Owns world ambient, source selection, the bounded native light pool and the F6 comparison.
using Godot;
using System;
using System.Collections.Generic;

public partial class WorldLightingController : Node2D
{
    [Export] public LightingMasterConfig Config { get; set; }
    [Export] public Texture2D Falloff { get; set; }
    [Export] public Node2D PlacedTowers { get; set; }
    [Export] public WorldGrid Grid { get; set; }

    private sealed record Source(Node2D Anchor, LightSourceConfig Profile, Vector2 Offset,
        float Radius, Func<bool> Eligible, Func<bool> UnderConstruction = null, DroneLightAttachment Follower = null, ProjectileLight Projectile = null)
    {
        public Slot Slot;
    }
    private sealed class Slot(PointLight2D light)
    {
        public readonly PointLight2D Light = light;
        public Source Source;
    }

    private readonly List<Source> _citadels = new(), _towers = new(), _drones = new(), _lava = new();
    private readonly Dictionary<Node2D, Action> _disconnect = new();
    private readonly List<Slot> _slots = new();
    private readonly List<Source> _selected = new();
    private CanvasModulate _ambient;
    private bool _lightingEnabled, _refreshQueued;
    private double _refreshRemaining;

    // =========================================================
    // The world mounts lighting after the grid and interactive terrain have initialized.
    public override void _Ready()
    {
        SetProcess(false);
        SetProcessUnhandledKeyInput(false);
        if (Config?.Enabled != true || Falloff == null || PlacedTowers == null || Grid?.Level == null) return;

        _ambient = new CanvasModulate { Name = "Ambient", Color = Config.AmbientTint, Visible = false };
        AddChild(_ambient);
        foreach (Node2D citadel in Grid.Level.Citadels) TrackCitadel(citadel);
        foreach (Node child in PlacedTowers.GetChildren()) TrackTower(child);
        PlacedTowers.ChildEnteredTree += TrackTower;
        PlacedTowers.ChildExitingTree += OnTowerExiting;
        Grid.TerrainRebuilt += RebuildLava;
        RebuildLava();
        TrackPortals();
        SetupRevealPresentation();
        SetProcessUnhandledKeyInput(OS.IsDebugBuild() && Config.AllowDebugComparison &&
            GetParent().GetParent() is BattlefieldLauncher);
        SetLightingEnabled(Config.StartLit);
    }

    // =========================================================
    // OFF releases ambient modulation and every allocated native light.
    public void SetLightingEnabled(bool enabled)
    {
        if (_ambient == null) return;
        _lightingEnabled = enabled && Config.Enabled;
        _ambient.Visible = _lightingEnabled;
        RefreshLights();
        ApplyRevealToggle();
    }

    // =========================================================
    // F6 is scoped to a running debug battlefield.
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.F6,
            ShiftPressed: false, CtrlPressed: false, AltPressed: false, MetaPressed: false }) return;
        SetLightingEnabled(!_lightingEnabled);
        GetViewport().SetInputAsHandled();
    }

    // =========================================================
    // Camera culling is throttled. Native transforms keep admitted drone lights moving between checks.
    public override void _Process(double delta)
    {
        TickMovingReveal(delta);
        _refreshRemaining -= delta;
        if (_refreshRemaining <= 0) RefreshLights();
    }

    #region Source Membership
    // =========================================================
    // Coalesce event bursts and leave scene-tree mutations outside entry/exit callbacks.
    private void QueueRefresh()
    {
        _revealDirty = true;
        if ((!_lightingEnabled && !RevealActive) || _refreshQueued) return;
        _refreshQueued = true;
        Callable.From(FlushRefresh).CallDeferred();
    }

    private void FlushRefresh()
    {
        _refreshQueued = false;
        RefreshLights();
    }

    // =========================================================
    // Real placement is the opt-in. A missing override inherits the shared profile.
    private void TrackTower(Node child)
    {
        if (child is not TowerBase tower || !HasOutput(Config.Tower) ||
            tower.Definition?.EmitsWorldLight == false || _disconnect.ContainsKey(tower)) return;
        TowerDefinition definition = tower.Definition;
        float radius = definition?.LightRadiusOverride ?? 0;
        if (!float.IsFinite(radius) || radius <= 0) radius = Config.Tower.RadiusPixels;
        Vector2 offset = Config.Tower.LocalOffset + (definition?.LightOffset ?? Vector2.Zero);
        Source source = new(tower, Config.Tower, offset, radius, () => !tower.IsDestroyed,
            () => tower.WorkPhase == TowerWorkPhase.Constructing);
        Watch(_towers, source, tower);
        tower.SpatialChanged += OnRevealTowerMoved;
        tower.WorkStarted += QueueRefresh;
        tower.WorkCompleted += QueueRefresh;
        tower.Destroyed += QueueRefresh;
        _disconnect[tower] += () =>
        {
            tower.SpatialChanged -= OnRevealTowerMoved;
            tower.WorkStarted -= QueueRefresh;
            tower.WorkCompleted -= QueueRefresh;
            tower.Destroyed -= QueueRefresh;
        };
        QueueRefresh();
    }

    private void OnRevealTowerMoved(TowerBase tower) => QueueRefresh();

    private void OnTowerExiting(Node child)
    {
        if (child is TowerBase tower) Untrack(tower);
    }

    // =========================================================
    // Use the authored objective roster and the Citadel profile's reactor offset.
    private void TrackCitadel(Node2D citadel)
    {
        if (!HasOutput(Config.Citadel) || citadel.GetNodeOrNull<CitadelHealth>("Health") is not { } health) return;
        Source source = new(citadel, Config.Citadel, Config.Citadel.LocalOffset, Config.Citadel.RadiusPixels,
            () => !health.IsDestroyed);
        Watch(_citadels, source, citadel);
        health.Destroyed += QueueRefresh;
        Action exit = () => Untrack(citadel);
        citadel.TreeExiting += exit;
        _disconnect[citadel] += () =>
        {
            health.Destroyed -= QueueRefresh;
            citadel.TreeExiting -= exit;
        };
    }

    // =========================================================
    // The shared drone body publishes light eligibility transitions for builders, couriers and fighters.
    internal void TrackDrone(DroneLightAttachment attachment, DroneBody body)
    {
        if (_ambient == null || !HasOutput(Config.Drone) || _disconnect.ContainsKey(attachment)) return;
        attachment.Position = Config.Drone.LocalOffset;
        Source source = new(attachment, Config.Drone, Vector2.Zero, Config.Drone.RadiusPixels,
            () => body.IsLightPresented && !body.GetParent().IsQueuedForDeletion(), Follower: attachment);
        Watch(_drones, source, attachment);
        body.LightPresentationChanged += QueueRefresh;
        _disconnect[attachment] += () => body.LightPresentationChanged -= QueueRefresh;
        QueueRefresh();
    }

    // =========================================================
    // Release any occupied slot before dropping the source from its list.
    internal void Untrack(Node2D key)
    {
        if (!_disconnect.Remove(key, out Action disconnect)) return;
        disconnect();
        _suppressed.Remove(key);
        QueueRefresh();
    }

    private void Watch(List<Source> sources, Source source, Node2D key)
    {
        sources.Add(source);
        key.VisibilityChanged += QueueRefresh;
        _disconnect.Add(key, () =>
        {
            ReleaseSlot(source);
            sources.Remove(source);
            key.VisibilityChanged -= QueueRefresh;
        });
    }

    // =========================================================
    // Cache grouped lava candidates only when the canonical hazard cache is rebuilt.
    private void RebuildLava()
    {
        foreach (Source source in _lava) ReleaseSlot(source);
        _lava.Clear();
        if (HasOutput(Config.Lava) && Grid.InteractiveTiles is { } terrain)
            foreach (Vector2I cell in LavaLightRegions.FindAnchors(terrain.EnumerateLavaCells()))
                _lava.Add(new Source(null, Config.Lava, Grid.CellToWorldCenter(cell) + Config.Lava.LocalOffset,
                    Config.Lava.RadiusPixels, null));
        QueueRefresh();
    }

    private static bool HasOutput(LightSourceConfig profile) =>
        profile is { Enabled: true, Energy: > 0, RadiusPixels: > 0 } &&
        float.IsFinite(profile.RadiusPixels) && float.IsFinite(profile.Energy);
    #endregion

    #region Light Budget
    // =========================================================
    // Keep Citadels first, reserve a tower slot when available, and cap drone/lava shares.
    private void RefreshLights()
    {
        if (_ambient == null) return;
        _refreshRemaining = Mathf.Clamp(Config.RefreshSeconds, 0.25f, 0.5f);
        int cap = _lightingEnabled ? Mathf.Clamp(Config.MaxActiveLights, 0, 64) : 0;
        bool cull = Config.CullOffscreenLights && cap > 0;
        Transform2D canvas = cull ? GetCanvasTransform() : default;
        Rect2 viewport = cull ? GetViewportRect().Grow(Mathf.Max(0, Config.ScreenMarginPixels)) : default;
        _selected.Clear();
        bool hasCandidates = Select(_citadels, cap, cull, canvas, viewport);
        hasCandidates |= Select(_portals, cap, cull, canvas, viewport);
        if (_selected.Count < cap) hasCandidates |= Select(_towers, _selected.Count + 1, cull, canvas, viewport);
        hasCandidates |= Select(_drones, Mathf.Min(cap, _selected.Count + Mathf.Clamp(Config.MaxDroneLights, 0, 16)), cull, canvas, viewport);
        hasCandidates |= Select(_lava, Mathf.Min(cap, _selected.Count + Mathf.Clamp(Config.MaxLavaLights, 0, 16)), cull, canvas, viewport);
        hasCandidates |= Select(_towers, cap, cull, canvas, viewport);

        Select(_projectileSources, Mathf.Min(cap, _selected.Count + Mathf.Clamp(Config.MaxProjectileLights, 0, 8)), cull, canvas, viewport);
        for (int i = _projectileSources.Count - 1; i >= 0; i--)
            if (!_selected.Contains(_projectileSources[i])) RemoveProjectile(i);

        foreach (Slot slot in _slots)
            if (slot.Source != null && !_selected.Contains(slot.Source)) Release(slot);
        foreach (Source source in _selected) Apply(source);

        UpdateRevealPresentation();
        SetProcess((RevealActive && _movingReveal) || (cull && (hasCandidates || _projectileSources.Count > 0)));
    }

    // =========================================================
    // Selection uses the light's footprint, so an emitter just outside the screen can still cast spill.
    private bool Select(List<Source> sources, int limit, bool cull, Transform2D canvas, Rect2 viewport)
    {
        bool hasCandidates = false;
        foreach (Source source in sources)
        {
            if (_selected.Count >= limit) break;
            if (_selected.Contains(source) || !IsAvailable(source)) continue;
            hasCandidates = true;
            if (cull && !Overlaps(source, canvas, viewport)) continue;
            _selected.Add(source);
        }
        return hasCandidates;
    }

    private bool Overlaps(Source source, Transform2D canvas, Rect2 viewport) =>
        OverlapsLight(PositionFor(source), RadiusFor(source), canvas, viewport);

    private static bool OverlapsLight(Vector2 position, float radius, Transform2D canvas, Rect2 viewport)
    {
        Vector2 centre = canvas * position;
        Vector2 extent = (canvas.X.Abs() + canvas.Y.Abs()) * radius;
        return viewport.Intersects(new Rect2(centre - extent, 2 * extent), true);
    }

    private static Vector2 PositionFor(Source source) => source.Projectile?.Light?.GlobalPosition ?? source.Anchor?.ToGlobal(source.Offset) ?? source.Offset;

    private float RadiusFor(Source source) => source.Radius *
        (source.UnderConstruction?.Invoke() == true ? Mathf.Clamp(Config.ConstructionRadiusFactor, 0.1f, 1) : 1);

    // =========================================================
    // Only admitted sources own a pooled light. Keep stable assignments through pans and work transitions.
    private void Apply(Source source)
    {
        Slot slot = source.Slot;
        bool acquired = slot == null;
        if (acquired) slot = AcquireSlot();
        PointLight2D light = slot.Light;
        if (acquired || source.Follower == null)
        {
            Vector2 position = PositionFor(source);
            if (light.GlobalPosition != position) light.GlobalPosition = position;
        }
        float scale = 2 * RadiusFor(source) / Falloff.GetWidth();
        float energy = source.Profile.Energy *
            (source.UnderConstruction?.Invoke() == true ? Mathf.Clamp(Config.ConstructionEnergyFactor, 0.1f, 1) : 1);
        if (light.TextureScale != scale) light.TextureScale = scale;
        if (light.Energy != energy) light.Energy = energy;
        if (!acquired) return;
        slot.Source = source;
        source.Slot = slot;
        light.Color = source.Projectile?.Tint ?? source.Profile.Tint;
        if (source.Projectile is { } projectile) projectile.Light = light;
        source.Follower?.FollowWith(light);
        light.Enabled = true;
    }

    private Slot AcquireSlot()
    {
        foreach (Slot slot in _slots)
            if (slot.Source == null) return slot;
        PointLight2D light = new() { Enabled = false, Texture = Falloff, ShadowEnabled = false };
        AddChild(light);
        Slot created = new(light);
        _slots.Add(created);
        return created;
    }

    private void ReleaseSlot(Source source)
    {
        if (source.Slot is { } slot) Release(slot);
    }

    private static void Release(Slot slot)
    {
        slot.Light.Enabled = false;
        slot.Source.Follower?.StopFollowing();
        slot.Source.Slot = null;
        slot.Source = null;
    }
    #endregion

    // =========================================================
    // Native nodes are world children. Detach event handlers and remote links on world teardown.
    public override void _ExitTree()
    {
        if (_ambient == null) return;
        _lightingEnabled = false;
        StopRevealPresentation();
        PlacedTowers.ChildEnteredTree -= TrackTower;
        PlacedTowers.ChildExitingTree -= OnTowerExiting;
        Grid.TerrainRebuilt -= RebuildLava;
        foreach (Action disconnect in _disconnect.Values) disconnect();
        _disconnect.Clear();
        foreach (Slot slot in _slots)
            if (slot.Source != null) Release(slot);
        for (int i = _projectileSources.Count - 1; i >= 0; i--) RemoveProjectile(i);
        _selected.Clear();
        _lava.Clear();
        _ambient = null;
    }
}
