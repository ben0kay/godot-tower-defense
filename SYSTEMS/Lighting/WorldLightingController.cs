// Owns world ambient light, placed-source membership, budget, and the debug lighting comparison.
using Godot;
using System;
using System.Collections.Generic;

public partial class WorldLightingController : Node2D
{
    [Export] public LightingMasterConfig Config { get; set; }
    [Export] public Texture2D Falloff { get; set; }
    [Export] public Node2D PlacedTowers { get; set; }
    [Export] public WorldGrid Grid { get; set; }

    private sealed record Source(Node2D Owner, WorldLightSource Light, Func<bool> IsReady, Action Disconnect);
    private readonly List<Source> _citadels = new(), _towers = new();
    private CanvasModulate _ambient;
    private bool _lightingEnabled;
    private double _refreshRemaining;

    // =========================================================
    // The world mounts this after its map, grid and tower container are ready.
    public override void _Ready()
    {
        SetProcess(false);
        SetProcessUnhandledKeyInput(false);
        if (Config?.Enabled != true || Falloff == null || PlacedTowers == null || Grid?.Level == null) return;

        _ambient = new CanvasModulate { Name = "Ambient", Color = Config.AmbientTint, Visible = false };
        AddChild(_ambient);
        foreach (Node2D citadel in Grid.Level.Citadels) TrackCitadel(citadel);
        foreach (Node child in PlacedTowers.GetChildren()) TrackTower(child);
        PlacedTowers.ChildEnteredTree += OnTowerEntered;
        PlacedTowers.ChildExitingTree += Untrack;
        SetProcessUnhandledKeyInput(OS.IsDebugBuild() && Config.AllowDebugComparison &&
            GetParent().GetParent() is BattlefieldLauncher);
        SetLightingEnabled(Config.StartLit);
    }

    // =========================================================
    // OFF releases ambient modulation and disables every registered native light.
    public void SetLightingEnabled(bool enabled)
    {
        if (_ambient == null) return;
        _lightingEnabled = enabled && Config.Enabled;
        _ambient.Visible = _lightingEnabled;
        RefreshLights();
    }

    // =========================================================
    // F6 is scoped to a running battlefield and ignores releases, echoes and modified shortcuts.
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.F6,
            ShiftPressed: false, CtrlPressed: false, AltPressed: false, MetaPressed: false }) return;
        SetLightingEnabled(!_lightingEnabled);
        GetViewport().SetInputAsHandled();
    }

    // =========================================================
    // Camera updates are throttled and sleep when there is no eligible light work.
    public override void _Process(double delta)
    {
        _refreshRemaining -= delta;
        if (_refreshRemaining <= 0) RefreshLights();
    }

    #region Membership
    // =========================================================
    // Wait until the tower's children have entered the tree, then verify it still belongs to this world.
    private void OnTowerEntered(Node child)
    {
        if (child is not TowerBase || child.GetNodeOrNull<WorldLightSource>("WorldLight") == null) return;
        Callable.From(() =>
        {
            if (_ambient == null || !GodotObject.IsInstanceValid(child) ||
                child.IsQueuedForDeletion() || child.GetParent() != PlacedTowers) return;
            TrackTower(child);
            RefreshLights();
        }).CallDeferred();
    }

    // =========================================================
    // Reuse the placed-tower container and work events. Previews never enter this container.
    private void TrackTower(Node child)
    {
        if (child is not TowerBase tower || tower.GetNodeOrNull<WorldLightSource>("WorldLight") is not { HasOutput: true } light ||
            _towers.Exists(source => source.Owner == tower)) return;
        light.Configure(Falloff);
        light.VisibilityChanged += RefreshLights;
        tower.WorkStarted += RefreshLights;
        tower.WorkCompleted += RefreshLights;
        tower.Destroyed += RefreshLights;
        _towers.Add(new Source(tower, light, () => tower.IsOperational, () =>
        {
            light.VisibilityChanged -= RefreshLights;
            tower.WorkStarted -= RefreshLights;
            tower.WorkCompleted -= RefreshLights;
            tower.Destroyed -= RefreshLights;
        }));
    }

    // =========================================================
    // Use the map's authored objective order, including secondary Citadels.
    private void TrackCitadel(Node2D citadel)
    {
        if (citadel.GetNodeOrNull<WorldLightSource>("WorldLight") is not { HasOutput: true } light ||
            citadel.GetNodeOrNull<CitadelHealth>("Health") is not { } health) return;
        light.Configure(Falloff);
        light.VisibilityChanged += RefreshLights;
        health.Destroyed += RefreshLights;
        Action onExit = () => Untrack(citadel);
        citadel.TreeExiting += onExit;
        _citadels.Add(new Source(citadel, light, () => !health.IsDestroyed, () =>
        {
            light.VisibilityChanged -= RefreshLights;
            health.Destroyed -= RefreshLights;
            citadel.TreeExiting -= onExit;
        }));
    }

    // =========================================================
    // A removed source releases its budget slot immediately.
    private void Untrack(Node owner)
    {
        if (!RemoveSource(_towers, owner) && !RemoveSource(_citadels, owner)) return;
        RefreshLights();
    }

    // =========================================================
    // Disconnect exactly the callbacks installed at registration.
    private static bool RemoveSource(List<Source> sources, Node owner)
    {
        int index = sources.FindIndex(source => source.Owner == owner);
        if (index < 0) return false;
        Source source = sources[index];
        sources.RemoveAt(index);
        ReleaseSource(source);
        return true;
    }

    // =========================================================
    private static void ReleaseSource(Source source)
    {
        source.Disconnect();
        if (GodotObject.IsInstanceValid(source.Light)) source.Light.Enabled = false;
    }
    #endregion

    #region Budget and Cleanup
    // =========================================================
    // Visible Citadels take priority, then towers in placement order. Existing lights keep stable priority.
    private void RefreshLights()
    {
        if (_ambient == null) return;
        _refreshRemaining = Mathf.Clamp(Config.RefreshSeconds, 0.25f, 0.5f);
        int remaining = _lightingEnabled ? Mathf.Clamp(Config.MaxActiveLights, 0, 64) : 0;
        bool cull = Config.CullOffscreenLights && remaining > 0;
        Rect2 viewport = cull ? GetViewportRect().Grow(Mathf.Max(0, Config.ScreenMarginPixels)) : default;
        bool hasCandidates = ApplyBudget(_citadels, cull, viewport, ref remaining);
        hasCandidates |= ApplyBudget(_towers, cull, viewport, ref remaining);
        SetProcess(cull && hasCandidates);
    }

    // =========================================================
    // Disabled/unfinished/offscreen sources never consume a slot. Skip redundant render-state writes.
    private static bool ApplyBudget(List<Source> sources, bool cull, Rect2 viewport, ref int remaining)
    {
        bool hasCandidates = false;
        foreach (Source source in sources)
        {
            bool eligible = (cull || remaining > 0) && source.Light.IsInsideTree() &&
                !source.Owner.IsQueuedForDeletion() && source.Light.IsVisibleInTree() && source.IsReady();
            hasCandidates |= eligible;
            bool enable = eligible && remaining > 0 && (!cull || source.Light.OverlapsViewport(viewport));
            if (source.Light.Enabled != enable) source.Light.Enabled = enable;
            if (enable) remaining--;
        }
        return hasCandidates;
    }

    // =========================================================
    // Removing this component or leaving the world releases every subscription and render state.
    public override void _ExitTree()
    {
        if (_ambient == null) return;
        PlacedTowers.ChildEnteredTree -= OnTowerEntered;
        PlacedTowers.ChildExitingTree -= Untrack;
        foreach (Source source in _citadels) ReleaseSource(source);
        foreach (Source source in _towers) ReleaseSource(source);
        _citadels.Clear();
        _towers.Clear();
        _ambient = null;
    }
    #endregion
}
