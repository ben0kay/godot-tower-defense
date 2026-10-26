// CV1: one pooled-in-place, active-only holographic canvas for every unfinished paid tower footprint.
// Registration comes from the shared work manager, including queued drones and saved construction restore.
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
        _work = GetNodeOrNull<ConstructionWorkManager>("../../ConstructionWork");
        _cellSize = GetNodeOrNull<WorldGrid>("../../Grid")?.Config?.CellSize ?? 64;
        if (_work != null)
        {
            _work.JobRegistered += OnRegistered;
            _work.JobUnregistered += OnUnregistered;
        }
        RefreshProcessing();
    }

    // =========================================================
    // Only initial paid construction has a site; upgrades retain their original active artwork.
    // Called before AddChild for new placements, after AddChild for resumed saved jobs.
    private void OnRegistered(TowerBase tower)
    {
        if (tower == null || tower.WorkPhase != TowerWorkPhase.Constructing || _sites.Contains(tower)) return;
        if (_sites.Count >= Mathf.Clamp(Config?.MaxVisibleSites ?? 128, 1, 256)) return;
        _sites.Add(tower);
        RefreshProcessing();
    }

    // =========================================================
    // Work completion, cancellation, rollback and destruction all unregister authoritatively.
    private void OnUnregistered(TowerBase tower)
    {
        if (!_sites.Remove(tower)) return;
        RefreshProcessing();
    }

    // =========================================================
    // Resource changes or a new paid job can wake the visual; an empty/disabled world stays asleep.
    public void RefreshProcessing()
    {
        bool active = IsInsideTree() && Config?.Enabled == true && _sites.Count > 0;
        if (active) _tick = 0;
        SetProcess(active);
        QueueRedraw();
    }
    #endregion

    #region Bounded Animation
    // =========================================================
    // A single ~24Hz cosmetic clock updates only while there are active sites, not one tick per tower.
    public override void _Process(double delta)
    {
        if (_sites.Count == 0 || Config?.Enabled != true)
        { SetProcess(false); QueueRedraw(); return; }
        if (GetTree().Paused) return;
        if (!double.IsFinite(delta) || delta <= 0) return;
        _clock += (float)Mathf.Min((float)delta, .25f);
        _tick += (float)Mathf.Min((float)delta, .25f);
        float interval = 1f / Mathf.Clamp(Config.RefreshHz, 8f, 48f);
        if (_tick < interval) return;
        _tick %= interval;
        QueueRedraw();
    }

    // =========================================================
    // Draw from live fixed tower footprints; a site has no visual work once its job unregisters.
    public override void _Draw()
    {
        ConstructionSiteVisualConfig c = Config;
        if (c?.Enabled != true) return;
        float inset = Mathf.Clamp(c.InsetPixels, 0, 12);
        // Camera-space culling prevents offscreen sites from issuing canvas draw calls.
        Transform2D toWorldCanvas = GetGlobalTransformWithCanvas().AffineInverse();
        Vector2 viewA = toWorldCanvas * Vector2.Zero;
        Vector2 viewB = toWorldCanvas * GetViewportRect().Size;
        Rect2 visible = new(new Vector2(Mathf.Min(viewA.X, viewB.X), Mathf.Min(viewA.Y, viewB.Y)),
            new Vector2(Mathf.Abs(viewB.X - viewA.X), Mathf.Abs(viewB.Y - viewA.Y)));
        foreach (TowerBase tower in _sites)
        {
            if (!GodotObject.IsInstanceValid(tower) || !tower.IsInsideTree() ||
                tower.IsQueuedForDeletion() || tower.IsDestroyed ||
                tower.WorkPhase != TowerWorkPhase.Constructing) continue;
            Vector2 cells = (Vector2)tower.Footprint;
            if (cells.X <= 0 || cells.Y <= 0) continue;
            Vector2 size = cells * _cellSize;
            Vector2 center = ToLocal(tower.GlobalPosition);
            Rect2 bounds = new(center - size * .5f + Vector2.One * inset, size - Vector2.One * (inset * 2));
            if (bounds.Size.X <= 2 || bounds.Size.Y <= 2 || !visible.Intersects(bounds.Grow(8))) continue;
            DrawSite(bounds, tower.AwaitingConstructionDrone, c);
        }
    }

    // =========================================================
    // A faint full-footprint blueprint, fixed perimeter, travelling indicator and rotating engineering HUD.
    private void DrawSite(Rect2 r, bool waiting, ConstructionSiteVisualConfig c)
    {
        float baseAlpha = Mathf.Clamp(waiting ? c.WaitingOpacity : c.BuildingOpacity, 0, 1);
        float pulse = 1f + Mathf.Clamp(c.PulseDepth, 0, .5f) * Mathf.Sin(_clock * Mathf.Tau * Mathf.Clamp(c.PulseHz, 0, 5));
        float alpha = Mathf.Min(1, baseAlpha * pulse);
        Color tint = c.Tint;
        Color line = new(tint.R, tint.G, tint.B, alpha * .52f);
        Color bright = new(tint.R, tint.G, tint.B, alpha);
        Color fade = new(tint.R, tint.G, tint.B, alpha * .15f);
        Vector2 mid = r.GetCenter();

        // Keep the underlying foundation readable; no opaque filled construction tile is added.
        DrawRect(r, new Color(tint.R, tint.G, tint.B, alpha * Mathf.Clamp(c.BlueprintOpacity, 0, 1) * .4f));
        DrawRect(r, fade, false, 4, true);
        DrawRect(r, line, false, 1.25f, true);
        DrawLine(new Vector2(r.Position.X, mid.Y), new Vector2(r.End.X, mid.Y), fade, 1, true);
        DrawLine(new Vector2(mid.X, r.Position.Y), new Vector2(mid.X, r.End.Y), fade, 1, true);

        float bracket = Mathf.Min(Mathf.Clamp(c.CornerLength, 2, 20), Mathf.Min(r.Size.X, r.Size.Y) * .3f);
        DrawCorner(r.Position, Vector2.Right, Vector2.Down, bracket, bright);
        DrawCorner(new Vector2(r.End.X, r.Position.Y), Vector2.Left, Vector2.Down, bracket, bright);
        DrawCorner(r.End, Vector2.Left, Vector2.Up, bracket, bright);
        DrawCorner(new Vector2(r.Position.X, r.End.Y), Vector2.Right, Vector2.Up, bracket, bright);

        float perimeter = 2 * (r.Size.X + r.Size.Y);
        float progress = Mathf.PosMod(_clock * Mathf.Clamp(c.PerimeterCyclesPerSecond, 0, 3) * perimeter, perimeter);
        DrawCircle(PerimeterPoint(r, progress), 2.5f, bright);
        DrawCircle(PerimeterPoint(r, progress), 5f, fade);

        // Central HUD remains circular even for large rectangular footprints.
        float radius = Mathf.Clamp(Mathf.Min(r.Size.X, r.Size.Y) * .27f, 9, 42);
        float angle = Mathf.DegToRad(_clock * Mathf.Clamp(c.RotationDegreesPerSecond, 0, 40));
        for (int i = 0; i < 10; i++)
        {
            float a = angle + i * Mathf.Tau / 10;
            DrawArc(mid, radius, a, a + .40f, 5, line, 1.25f, true);
            DrawArc(mid, radius * .7f, -a, -a + .24f, 4, fade, 1, true);
        }
        DrawCircle(mid, 2, new Color(.72f, .96f, 1, alpha * .7f));
        DrawLine(mid + Vector2.Left * radius * .4f, mid + Vector2.Right * radius * .4f, fade, 1, true);
        DrawLine(mid + Vector2.Up * radius * .4f, mid + Vector2.Down * radius * .4f, fade, 1, true);
    }

    // =========================================================
    // Precomputed axis signs keep brackets crisp for both 1x1 towers and 3x2 factories.
    private void DrawCorner(Vector2 anchor, Vector2 x, Vector2 y, float length, Color tint)
    {
        DrawLine(anchor, anchor + x * length, tint, 2, true);
        DrawLine(anchor, anchor + y * length, tint, 2, true);
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
        _work = null;
        _sites.Clear();
        SetProcess(false);
    }
    #endregion
}
