// Shared active-only holographic canvas for paid construction and upgrade work on any tower footprint.
// Registration comes from the shared work manager, including queued drones and restored work.
using Godot;
using System.Collections.Generic;

public partial class ConstructionSiteHologram : Node2D
{
    #region Configuration and Active Sites
    private ConstructionSiteVisualConfig _config;
    [Export] public ConstructionSiteVisualConfig Config
    {
        get => _config;
        set { _config = value; if (IsNodeReady()) RefreshProcessing(); }
    }
    private readonly HashSet<TowerBase> _sites = new();
    private ConstructionWorkManager _work;
    private float _cellSize = 64, _clock, _tick;
    public int ActiveSites => _sites.Count;
    public bool HasSite(TowerBase tower) => _sites.Contains(tower);

    // =========================================================
    // World-local effect owns no sprites/timers per building; idle maps do not redraw or process.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _readoutCanvas = new Node2D
        {
            Name = "UnlitTelemetry",
            Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded }
        };
        _readoutCanvas.Draw += DrawReadouts;
        AddChild(_readoutCanvas);
        _work = GetNodeOrNull<ConstructionWorkManager>("../../ConstructionWork");
        _builders = GetNodeOrNull<ConstructionDroneManager>("../../ConstructionDrones");
        _cellSize = GetNodeOrNull<WorldGrid>("../../Grid")?.Config?.CellSize ?? 64;
        if (_work != null)
        {
            _work.JobRegistered += OnRegistered;
            _work.JobUnregistered += OnUnregistered;
        }
        RefreshProcessing();
    }

    // =========================================================
    // Both paid builds and upgrades show the same footprint hologram; upgrades retain their existing tower art.
    // Called before AddChild for initial placements, and during registered upgrades/restored jobs.
    private void OnRegistered(TowerBase tower)
    {
        if (tower == null || !tower.IsBusy || _sites.Contains(tower)) return;
        if (_sites.Count >= Mathf.Clamp(Config?.MaxVisibleSites ?? 128, 1, 256)) return;
        TrackTelemetry(tower); _sites.Add(tower);
        RefreshProcessing();
    }

    // =========================================================
    // Work completion, cancellation, rollback and destruction all unregister authoritatively.
    private void OnUnregistered(TowerBase tower)
    {
        if (!_sites.Remove(tower)) return;
        RetireTelemetry(tower);
        RefreshProcessing();
    }

    // =========================================================
    // Resource changes or a new paid job can wake the visual; an empty/disabled world stays asleep.
    public void RefreshProcessing()
    {
        if (Config?.Enabled != true) DiscardCompletions();
        else if (Config.TelemetryEnabled)
            foreach (var tower in _sites) if (_telemetry.TryGetValue(tower, out var site)) UpdateReadout(site);
        bool active = IsInsideTree() && Config?.Enabled == true && (_sites.Count > 0 || _completions.Count > 0);
        if (active) _tick = 0;
        SetProcess(active);
        QueueVisualRedraw();
    }
    #endregion

    #region Bounded Animation
    // =========================================================
    // A single ~24Hz cosmetic clock updates only while there are active sites, not one tick per tower.
    public override void _Process(double delta)
    {
        if ((_sites.Count == 0 && _completions.Count == 0) || Config?.Enabled != true)
        { SetProcess(false); QueueVisualRedraw(); return; }
        if (GetTree().Paused) return;
        if (!double.IsFinite(delta) || delta <= 0) return;
        AdvanceTelemetry((float)System.Math.Min(delta, 3600));
        _clock += (float)Mathf.Min((float)delta, .25f);
        _tick += (float)Mathf.Min((float)delta, .25f);
        float interval = 1f / Mathf.Clamp(Config.RefreshHz, 8f, 48f);
        if (_tick < interval) return;
        _tick %= interval;
        QueueVisualRedraw();
    }

    // =========================================================
    // Draw from live fixed tower footprints; a site has no visual work once its job unregisters.
    public override void _Draw() => DrawSites(telemetry: false);

    private void DrawReadouts() => DrawSites(telemetry: true);

    private void QueueVisualRedraw()
    {
        QueueRedraw();
        _readoutCanvas?.QueueRedraw();
    }

    private void DrawSites(bool telemetry)
    {
        ConstructionSiteVisualConfig c = Config;
        if (c?.Enabled != true || (telemetry && !c.TelemetryEnabled)) return;
        if (telemetry) _readoutCount = 0;
        float inset = Mathf.Clamp(c.InsetPixels, 0, 12);
        float corner = Mathf.Clamp(c.CornerLength, 2, 20);
        if (!telemetry && _artInset != inset)
        {
            _artInset = inset;
            _scannerArtwork.Clear();
            _footprintLines.Clear();
        }
        if (!telemetry && _artCorner != corner)
        {
            _artCorner = corner;
            _footprintLines.Clear();
        }
        // Camera-space culling prevents offscreen sites from issuing canvas draw calls.
        Transform2D toWorldCanvas = GetGlobalTransformWithCanvas().AffineInverse();
        Vector2 viewA = toWorldCanvas * Vector2.Zero;
        Vector2 viewB = toWorldCanvas * GetViewportRect().Size;
        Rect2 visible = new(new Vector2(Mathf.Min(viewA.X, viewB.X), Mathf.Min(viewA.Y, viewB.Y)),
            new Vector2(Mathf.Abs(viewB.X - viewA.X), Mathf.Abs(viewB.Y - viewA.Y)));
        float pulse = telemetry ? 1f : 1f + Mathf.Clamp(c.PulseDepth, 0, .5f) * Mathf.Sin(_clock * Mathf.Tau * Mathf.Clamp(c.PulseHz, 0, 5));
        foreach (TowerBase tower in _sites)
        {
            if (!GodotObject.IsInstanceValid(tower) || !tower.IsInsideTree() ||
                tower.IsQueuedForDeletion() || tower.IsDestroyed ||
                !tower.IsBusy) continue;
            Vector2 cells = (Vector2)tower.Footprint;
            if (cells.X <= 0 || cells.Y <= 0) continue;
            Vector2 size = cells * _cellSize;
            Vector2 center = ToLocal(tower.GlobalPosition);
            Rect2 bounds = new(center - size * .5f + Vector2.One * inset, size - Vector2.One * (inset * 2));
            if (bounds.Size.X <= 2 || bounds.Size.Y <= 2 || !visible.Intersects(bounds.Grow(telemetry ? 240 : 12))) continue;
            if (!telemetry)
            {
                float locked = GodotObject.IsInstanceValid(_builders) ? _builders.AcquisitionProgressFor(tower) : -1;
                DrawSite(bounds, tower.AwaitingConstructionDrone, c, pulse, locked);
            }
            else if (_telemetry.TryGetValue(tower, out var site))
            {
                UpdateReadout(site); // Recover authoritative progress if telemetry is enabled while a job is held.
                DrawTelemetry(bounds, site, 1, c);
            }
        }
        if (telemetry) DrawCompletions(visible, c);
    }

    // =========================================================
    // A faint full-footprint blueprint, fixed perimeter, travelling indicator and rotating engineering HUD.
    private void DrawSite(Rect2 r, bool waiting, ConstructionSiteVisualConfig c, float pulse, float locked)
    {
        float baseAlpha = Mathf.Clamp(waiting ? c.WaitingOpacity : c.BuildingOpacity, 0, 1);
        float alpha = Mathf.Min(1, baseAlpha * pulse);
        bool acquiring = locked >= 0 && c.AcquisitionCueEnabled;
        if (acquiring) alpha = Mathf.Clamp(alpha + .45f * Mathf.Sin(Mathf.Pi * Mathf.Clamp(locked, 0, 1)), 0, 1);
        Color tint = acquiring ? c.AcquisitionTint : c.Tint;
        Color line = new(tint.R, tint.G, tint.B, alpha * .52f);
        Color bright = new(tint.R, tint.G, tint.B, alpha);
        Color fade = new(tint.R, tint.G, tint.B, alpha * .15f);
        Vector2 mid = r.GetCenter();

        // Keep the underlying foundation readable; no opaque filled construction tile is added.
        DrawRect(r, new Color(tint.R, tint.G, tint.B, alpha * Mathf.Clamp(c.BlueprintOpacity, 0, 1) * .4f));
        DrawRect(r, fade, false, 4, true);
        DrawRect(r, line, false, 1.25f, true);
        float radius = Mathf.Clamp(Mathf.Min(r.Size.X, r.Size.Y) * .27f, 9, 42);
        FootprintLines lines = GetFootprintLines(r.Size, radius);
        DrawSetTransform(mid);
        DrawMultiline(lines.Cross, fade, 1, true);
        DrawMultiline(lines.Brackets, bright, 2, true);
        DrawSetTransform(Vector2.Zero);

        float perimeter = 2 * (r.Size.X + r.Size.Y);
        float progress = Mathf.PosMod(_clock * Mathf.Clamp(c.PerimeterCyclesPerSecond, 0, 3) * perimeter, perimeter);
        Vector2 indicator = PerimeterPoint(r, progress);
        DrawCircle(indicator, 2.5f, bright);
        DrawCircle(indicator, 5f, fade);

        float angle = Mathf.DegToRad(_clock * Mathf.Clamp(c.RotationDegreesPerSecond, 0, 40));
        ScannerArtwork scanner = GetScannerArtwork(radius);
        DrawRotatingRing(scanner.Outer, mid, angle, Vector2.One, line);
        DrawRotatingRing(scanner.Inner, mid, -angle, Vector2.One, fade);
        DrawSetTransform(mid);
        if (acquiring)
        {
            float converge = (1 - Mathf.Clamp(locked, 0, 1)) * Mathf.Min(r.Size.X, r.Size.Y) * .6f;
            foreach (Vector2 direction in AcquisitionDirections)
            {
                Vector2 side = direction.Orthogonal() * 4;
                Vector2 point = direction * converge;
                DrawLine(point - side, point + side, bright, 1, true);
            }
        }
        DrawCircle(Vector2.Zero, 2, new Color(.72f, .96f, 1, alpha * .7f));
        DrawMultiline(lines.ScannerCross, fade, 1, true);
        DrawSetTransform(Vector2.Zero);
    }

    // =========================================================
    // One moving perimeter dot follows the entire rectangular footprint, never just the first tile.
    private static Vector2 PerimeterPoint(Rect2 rect, float distance)
    {
        float w = rect.Size.X, h = rect.Size.Y;
        if (distance < w) return rect.Position + new Vector2(distance, 0);
        distance -= w;
        if (distance < h) return new Vector2(rect.End.X, rect.Position.Y + distance);
        distance -= h;
        if (distance < w) return new Vector2(rect.End.X - distance, rect.End.Y);
        return new Vector2(rect.Position.X, rect.End.Y - (distance - w));
    }
    #endregion

    #region Cleanup
    // =========================================================
    // Do not carry site references/subscriptions into retries, menu transitions or restored worlds.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_work))
        {
            _work.JobRegistered -= OnRegistered;
            _work.JobUnregistered -= OnUnregistered;
        }
        ClearTelemetry(); _builders = null;
        _work = null;
        _sites.Clear();
        SetProcess(false);
    }
    #endregion
}
