// One cached readout per tracked site, drawn by a shared unlit canvas; no site GUI nodes/timers.
using Godot;
using System;
using System.Collections.Generic;
public partial class ConstructionSiteHologram
{
    private Node2D _readoutCanvas;

    private sealed class Readout
    {
        public TowerBase Tower;
        public int Percent = -1;
        public float DisplayedPercent;
        public bool DisplayInitialized;
        public int LastDrawnPercent = -1;
        public string PercentLabel;
        public Font MeasuredFont;
        public int MeasuredFontSize;
        public Vector2 LabelPosition;
        public Vector2[] ProgressPoints;
        public float CachedProgress = -1, CachedRadius = -1;
        public bool Waiting;
        public float Age, Opacity;
        public Action Progress, Completed, Removed;
    }
    private readonly Dictionary<TowerBase, Readout> _telemetry = new();
    private readonly List<Readout> _completions = new(64);
    private ConstructionDroneManager _builders;
    private readonly Rect2[] _readoutBounds = new Rect2[320];
    private int _readoutCount;
    public int CompletionReadouts => _completions.Count;
    // =========================================================
    // Registration and work events update the authoritative target for the cosmetic count-up.
    private void TrackTelemetry(TowerBase tower)
    {
        if (_telemetry.TryGetValue(tower, out var old)) RemoveTelemetry(old);
        var site = new Readout { Tower = tower };
        site.Progress = () => UpdateReadout(site); site.Completed = () => CompleteReadout(site); site.Removed = () => RemoveTelemetry(site);
        _telemetry.Add(tower, site);
        tower.WorkProgressChanged += site.Progress; tower.WorkCompleted += site.Completed;
        tower.TreeExiting += site.Removed; tower.Destroyed += site.Removed;
        UpdateReadout(site);
    }
    private void UpdateReadout(Readout site)
    {
        var tower = site.Tower;
        if (!GodotObject.IsInstanceValid(tower) || Config?.Enabled != true || !Config.TelemetryEnabled) return;
        int percent = Mathf.Clamp(Mathf.FloorToInt((float)(tower.WorkProgress * 100)), 0, 100);
        bool waiting = tower.AwaitingConstructionDrone;
        if (site.DisplayInitialized && site.Percent == percent && site.Waiting == waiting) return;
        site.Percent = percent; site.Waiting = waiting;
        // New or restored work starts with its authoritative progress; future changes count upward.
        if (!site.DisplayInitialized) { site.DisplayInitialized = true; site.DisplayedPercent = percent; }
    }
    private void RetireTelemetry(TowerBase tower)
    {
        if (!_telemetry.TryGetValue(tower, out var site)) return;
        // Unregister precedes WorkCompleted; only that successful event may show a completed 100% widget.
        if (!GodotObject.IsInstanceValid(tower) || tower.IsBusy || tower.IsDestroyed || tower.IsQueuedForDeletion()) RemoveTelemetry(site);
    }
    private void CompleteReadout(Readout site)
    {
        var tower = site.Tower;
        if (!_telemetry.TryGetValue(tower, out var current) || current != site) return;
        if (GodotObject.IsInstanceValid(tower) && tower.IsBusy) return; // An earlier completion cannot retire a new paid job.
        if (!GodotObject.IsInstanceValid(tower) || tower.IsDestroyed || tower.IsQueuedForDeletion()
            || Config?.Enabled != true || !Config.TelemetryEnabled) { RemoveTelemetry(site); return; }
        int cap = Mathf.Clamp(Config.MaxCompletionReadouts, 1, 64);
        while (_completions.Count >= cap) RemoveTelemetry(_completions[0]);
        site.Percent = 100; site.Waiting = false;
        if (!site.DisplayInitialized) { site.DisplayInitialized = true; site.DisplayedPercent = 100; }
        site.Age = 0;
        _completions.Add(site); RefreshProcessing();
    }
    private void AdvanceTelemetry(float dt)
    {
        // Reuse the shared site animation clock; no per-readout process or timer.
        float rate = ConstructionDroneMasterConfig.Safe(Config.TelemetryCountUpPerSecond, 160, 20, 400);
        float revealDistance = ConstructionDroneMasterConfig.Safe(Config.TelemetryRevealDistance, 140, 32, 300);
        float fadeSeconds = ConstructionDroneMasterConfig.Safe(Config.TelemetryFadeInSeconds, .25f, .05f, 1);
        foreach (Readout site in _telemetry.Values)
        {
            bool reveal = !site.Waiting || (GodotObject.IsInstanceValid(_builders) && _builders.IsNearSite(site.Tower, revealDistance));
            // Latch visibility once approached; orbit motion cannot flicker the widget off again.
            if (reveal || site.Opacity > 0) site.Opacity = Mathf.MoveToward(site.Opacity, 1, dt / fadeSeconds);
            if (!site.DisplayInitialized || site.Waiting) continue;
            site.DisplayedPercent = Mathf.MoveToward(site.DisplayedPercent, site.Percent, rate * dt);
        }
        float duration = ConstructionDroneMasterConfig.Safe(Config.CompletionReadoutSeconds, 1, .8f, 1.2f);
        for (int i = _completions.Count - 1; i >= 0; i--)
        {
            var site = _completions[i]; site.Age += dt;
            if (!Config.TelemetryEnabled || site.Age >= duration || !GodotObject.IsInstanceValid(site.Tower)
                || !site.Tower.IsInsideTree() || site.Tower.IsDestroyed || site.Tower.IsQueuedForDeletion()) RemoveTelemetry(site);
        }
    }
    private void RemoveTelemetry(Readout site)
    {
        if (!_telemetry.TryGetValue(site.Tower, out var current) || current != site) return;
        _telemetry.Remove(site.Tower); _completions.Remove(site); _sites.Remove(site.Tower);
        Detach(site); RefreshProcessing();
    }
    private static void Detach(Readout site)
    {
        if (!GodotObject.IsInstanceValid(site.Tower)) return;
        site.Tower.WorkProgressChanged -= site.Progress; site.Tower.WorkCompleted -= site.Completed;
        site.Tower.TreeExiting -= site.Removed; site.Tower.Destroyed -= site.Removed;
    }
    private void DiscardCompletions()
    {
        foreach (var site in _completions) { _telemetry.Remove(site.Tower); Detach(site); }
        _completions.Clear();
    }
    private void ClearTelemetry()
    { foreach (var site in _telemetry.Values) Detach(site); _telemetry.Clear(); _completions.Clear(); }
    // =========================================================
    // Draw only the circular percentage, including a brief 100% completion fade.
    // Keep the old bounded eight-lane placement to prevent overlap on crowded sites.
    private void DrawTelemetry(Rect2 bounds, Readout site, float opacity, ConstructionSiteVisualConfig c)
    {
        opacity *= site.Opacity;
        if (!c.TelemetryEnabled || !site.DisplayInitialized || opacity <= 0) return;
        float scale = ConstructionDroneMasterConfig.Safe(c.TelemetryScale, .72f, .4f, 1.5f);
        float radius = ConstructionDroneMasterConfig.Safe(c.TelemetryRingRadius, 40, 26, 56);
        float diameter = (radius + 9) * 2;
        float width = diameter * scale, height = diameter * scale;
        Vector2 offset = c.TelemetryOffset.IsFinite() ? c.TelemetryOffset : new(-8, -4);
        // Place the complete widget above/right of the footprint corner, rather than alongside it.
        Rect2 panel = new(new Vector2(bounds.End.X, bounds.Position.Y - height) + offset, new(width, height));
        Vector2 anchor = panel.Position;
        bool placed = false;
        for (int lane = 0; lane < 8 && !placed; lane++)
        {
            int row = lane == 0 ? 0 : ((lane + 1) / 2) * (lane % 2 == 1 ? -1 : 1);
            panel.Position = anchor + Vector2.Down * (row * (height + 3));
            placed = true;
            for (int i = 0; i < _readoutCount; i++)
                if (_readoutBounds[i].Intersects(panel.Grow(2))) { placed = false; break; }
        }
        if (!placed || _readoutCount >= _readoutBounds.Length) return;
        _readoutBounds[_readoutCount++] = panel;
        Font font = c.TelemetryFont ?? ThemeDB.FallbackFont;
        Vector2 center = panel.GetCenter();
        DrawCircularProgress(center, Vector2.One * scale, radius, site, opacity, font, c);
        _readoutCanvas.DrawSetTransform(Vector2.Zero);
    }

    // =========================================================
    // Outer artwork turns anticlockwise. Both inner arcs turn clockwise.
    private void DrawCircularProgress(Vector2 center, Vector2 scale, float radius, Readout site,
        float opacity, Font font, ConstructionSiteVisualConfig c)
    {
        EnsureTelemetryArtwork(radius);
        float outer = -Mathf.DegToRad(_clock * ConstructionDroneMasterConfig.Safe(c.TelemetryOuterDegreesPerSecond, 30, 0, 180));
        float middle = Mathf.DegToRad(_clock * ConstructionDroneMasterConfig.Safe(c.TelemetryMiddleDegreesPerSecond, 48, 0, 180));
        float inner = Mathf.DegToRad(_clock * ConstructionDroneMasterConfig.Safe(c.TelemetryInnerDegreesPerSecond, 72, 0, 180));
        Color baseTint = c.TelemetryColor;
        Color dim = new(baseTint.R, baseTint.G, baseTint.B, baseTint.A * opacity * .18f);
        Color cyan = new(.12f, .92f, .96f, opacity * .85f);
        Color pale = new(.77f, 1f, 1f, opacity * .93f);
        Color fill = c.TelemetryBackground; fill.A *= opacity * .4f;
        _readoutCanvas.DrawSetTransform(center, 0, scale);
        _readoutCanvas.DrawCircle(Vector2.Zero, radius * .81f, fill);
        _readoutCanvas.DrawPolyline(_telemetryTrack, dim, 1.15f, true);
        float done = Mathf.Clamp(site.DisplayedPercent / 100f, 0, 1);
        if (done > .001f)
        {
            site.ProgressPoints ??= new Vector2[56];
            if (site.CachedProgress != done || site.CachedRadius != radius)
            {
                FillArcPoints(site.ProgressPoints, radius * .88f, -Mathf.Pi * .5f, Mathf.Tau * done);
                site.CachedProgress = done;
                site.CachedRadius = radius;
            }
            _readoutCanvas.DrawPolyline(site.ProgressPoints, new Color(.14f, .94f, 1f, opacity * .55f), 1.5f, true);
        }

        DrawRotatingRing(_telemetryOuter, center, outer, scale, new Color(1, 1, 1, opacity), _readoutCanvas);
        DrawRotatingRing(_telemetryMiddle, center, middle, scale, cyan, _readoutCanvas);
        DrawRotatingRing(_telemetryInner, center, inner, scale, pale, _readoutCanvas);

        int shown = Mathf.Clamp(Mathf.RoundToInt(site.DisplayedPercent), 0, 100);
        bool numberChanged = shown != site.LastDrawnPercent;
        if (numberChanged)
        {
            site.LastDrawnPercent = shown;
            site.PercentLabel = shown.ToString() + "%";
        }
        int numberSize = Mathf.Clamp(c.TelemetryFontSize + 8, 16, 28);
        if (numberChanged || site.MeasuredFont != font || site.MeasuredFontSize != numberSize)
        {
            site.MeasuredFont = font;
            site.MeasuredFontSize = numberSize;
            Vector2 size = font.GetStringSize(site.PercentLabel, HorizontalAlignment.Left, -1, numberSize);
            site.LabelPosition = new Vector2(-size.X * .5f, (font.GetAscent(numberSize) - font.GetDescent(numberSize)) * .5f);
        }
        _readoutCanvas.DrawSetTransform(center, 0, scale);
        _readoutCanvas.DrawString(font, site.LabelPosition, site.PercentLabel, HorizontalAlignment.Left, -1, numberSize, pale);
    }

    private void DrawCompletions(Rect2 visible, ConstructionSiteVisualConfig c)
    {
        float duration = ConstructionDroneMasterConfig.Safe(c.CompletionReadoutSeconds, 1, .8f, 1.2f);
        foreach (var site in _completions)
        {
            var tower = site.Tower; if (!GodotObject.IsInstanceValid(tower) || !tower.IsInsideTree() || tower.IsQueuedForDeletion()) continue;
            Vector2 size = (Vector2)tower.Footprint * _cellSize;
            Rect2 bounds = new(ToLocal(tower.GlobalPosition) - size * .5f, size);
            if (visible.Intersects(bounds.Grow(240))) DrawTelemetry(bounds, site, Mathf.Clamp((duration - site.Age) / (duration * .4f), 0, 1), c);
        }
    }
}
