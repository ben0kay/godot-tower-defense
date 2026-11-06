// One cached readout per tracked site, drawn by the existing bounded hologram canvas; no site GUI nodes/timers.
using Godot;
using System;
using System.Collections.Generic;
public partial class ConstructionSiteHologram
{
    private sealed class Readout
    {
        public TowerBase Tower;
        public string Text;
        public int Percent = -1;
        public float DisplayedPercent;
        public bool DisplayInitialized;
        public int LastDrawnPercent = -1;
        public string PercentLabel;
        public bool Waiting, Upgrading;
        public float Age;
        public Action Progress, Completed, Removed;
    }
    private readonly Dictionary<TowerBase, Readout> _telemetry = new();
    private readonly List<Readout> _completions = new(64);
    private ConstructionDroneManager _builders;
    private readonly Rect2[] _readoutBounds = new Rect2[320];
    private int _readoutCount;
    public int CompletionReadouts => _completions.Count;
    // =========================================================
    // Registration/restore use the real work source; progress strings change only on an authoritative event.
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
        bool waiting = tower.AwaitingConstructionDrone, upgrading = tower.WorkPhase == TowerWorkPhase.Upgrading;
        if (site.Text != null && site.Percent == percent && site.Waiting == waiting && site.Upgrading == upgrading) return;
        site.Percent = percent; site.Waiting = waiting; site.Upgrading = upgrading;
        // New or restored work starts with its authoritative progress; future changes count upward.
        if (!site.DisplayInitialized) { site.DisplayInitialized = true; site.DisplayedPercent = percent; }
        site.Text = waiting ? "DRONE INBOUND" : (upgrading ? "UPGRADING " : "CONSTRUCTING ") + percent + "%";
    }
    private void RetireTelemetry(TowerBase tower)
    {
        if (!_telemetry.TryGetValue(tower, out var site)) return;
        // Unregister precedes WorkCompleted; only that successful event may create SYSTEM ONLINE.
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
        site.Text = "SYSTEM ONLINE"; site.Age = 0;
        _completions.Add(site); RefreshProcessing();
    }
    private void AdvanceTelemetry(float dt)
    {
        // Reuse the shared site animation clock; no per-readout process or timer.
        float rate = ConstructionDroneMasterConfig.Safe(Config.TelemetryCountUpPerSecond, 160, 20, 400);
        foreach (Readout site in _telemetry.Values)
        {
            if (!site.DisplayInitialized || site.Waiting || site.Text == "SYSTEM ONLINE") continue;
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
    // Draw either a circular construction percentage or bare, unboxed status text.
    // Keep the old bounded eight-lane placement to prevent overlap on crowded sites.
    private void DrawTelemetry(Rect2 bounds, Readout site, bool acquiring, float opacity, ConstructionSiteVisualConfig c)
    {
        if (!c.TelemetryEnabled || site.Text == null || opacity <= 0) return;
        bool progress = !acquiring && !site.Waiting && site.Text != "SYSTEM ONLINE";
        float radius = ConstructionDroneMasterConfig.Safe(c.TelemetryRingRadius, 40, 26, 56);
        int fontSize = Mathf.Clamp(c.TelemetryFontSize, 8, 18);
        float width = progress ? (radius + 9) * 2 : ConstructionDroneMasterConfig.Safe(c.TelemetryWidth, 132, 88, 220);
        float height = progress ? (radius + 9) * 2 + 15 : fontSize + 10;
        Vector2 offset = c.TelemetryOffset.IsFinite() ? c.TelemetryOffset : new(8, -14);
        Rect2 panel = new(new Vector2(bounds.End.X, bounds.Position.Y) + offset, new(width, height));
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
        if (progress) DrawCircularProgress(panel, radius, site, opacity, font, c);
        else DrawBareStatus(panel, acquiring ? "SITE LOCKED" : site.Text, opacity, font, fontSize,
            acquiring ? c.AcquisitionTint : c.TelemetryColor);
    }

    // =========================================================
    // DRONE INBOUND, SITE LOCKED and SYSTEM ONLINE retain brief text, never a black rectangle.
    private void DrawBareStatus(Rect2 panel, string label, float opacity, Font font, int fontSize, Color tint)
    {
        Vector2 pos = panel.Position + new Vector2(0, fontSize + 1);
        Color shadow = new(.015f, .045f, .065f, opacity * .9f);
        tint.A *= opacity;
        DrawString(font, pos + Vector2.One, label, HorizontalAlignment.Left, panel.Size.X, fontSize, shadow);
        DrawString(font, pos, label, HorizontalAlignment.Left, panel.Size.X, fontSize, tint);
    }

    // =========================================================
    // Outer arc and dotted ion dust turn anti-clockwise; both inner arcs turn clockwise.
    // The percent climbs with the existing authoritative work progress, not a new fake timer.
    private void DrawCircularProgress(Rect2 panel, float radius, Readout site, float opacity, Font font, ConstructionSiteVisualConfig c)
    {
        Vector2 center = panel.Position + new Vector2(panel.Size.X * .5f, radius + 9);
        float outer = -Mathf.DegToRad(_clock * ConstructionDroneMasterConfig.Safe(c.TelemetryOuterDegreesPerSecond, 30, 0, 180));
        float middle = Mathf.DegToRad(_clock * ConstructionDroneMasterConfig.Safe(c.TelemetryMiddleDegreesPerSecond, 48, 0, 180));
        float inner = Mathf.DegToRad(_clock * ConstructionDroneMasterConfig.Safe(c.TelemetryInnerDegreesPerSecond, 72, 0, 180));
        Color baseTint = c.TelemetryColor;
        Color dim = new(baseTint.R, baseTint.G, baseTint.B, baseTint.A * opacity * .18f);
        Color cyan = new(.12f, .92f, .96f, opacity * .85f);
        Color pale = new(.77f, 1f, 1f, opacity * .93f);
        Color fill = c.TelemetryBackground; fill.A *= opacity * .4f;
        DrawCircle(center, radius * .81f, fill); // Circular tint, not the old rectangular panel.
        DrawArc(center, radius * .82f, 0, Mathf.Tau, 56, dim, 1.15f, true);
        float done = Mathf.Clamp(site.DisplayedPercent / 100f, 0, 1);
        if (done > .001f)
            DrawArc(center, radius * .88f, -Mathf.Pi * .5f,
                -Mathf.Pi * .5f + Mathf.Tau * done, 56, new Color(.14f, .94f, 1f, opacity * .55f), 1.5f, true);

        DrawArc(center, radius + 1, outer, outer + 2.25f, 32, pale, 1.6f, true);
        DrawArc(center, radius * .75f, middle, middle + 4.4f, 44, cyan, 1.55f, true);
        DrawArc(center, radius * .59f, inner, inner + 2.85f, 30, pale, 1.25f, true);
        for (int i = 0; i < 20; i++)
        {
            float fraction = i / 19f;
            float angle = outer + fraction * 2.4f - .25f;
            float distance = radius + 3 + (i % 4) * 2.1f;
            float alpha = opacity * (.16f + .54f * (1 - fraction));
            DrawCircle(center + Vector2.FromAngle(angle) * distance, i % 6 == 0 ? 1.05f : .6f,
                new Color(.35f, .97f, 1, alpha));
        }

        // Format the number only when the displayed integer changes.
        int shown = Mathf.Clamp(Mathf.RoundToInt(site.DisplayedPercent), 0, 100);
        if (shown != site.LastDrawnPercent)
        {
            site.LastDrawnPercent = shown;
            site.PercentLabel = shown.ToString() + "%";
        }
        int numberSize = Mathf.Clamp(c.TelemetryFontSize + 8, 16, 28);
        Vector2 size = font.GetStringSize(site.PercentLabel, HorizontalAlignment.Left, -1, numberSize);
        float baseline = center.Y + (font.GetAscent(numberSize) - font.GetDescent(numberSize)) * .5f;
        DrawString(font, new Vector2(center.X - size.X * .5f, baseline), site.PercentLabel,
            HorizontalAlignment.Left, -1, numberSize, pale);

        const int captionSize = 9;
        string caption = site.Upgrading ? "UPGRADING" : "CONSTRUCTING";
        float captionWidth = font.GetStringSize(caption, HorizontalAlignment.Left, -1, captionSize).X;
        DrawString(font, new Vector2(center.X - captionWidth * .5f, panel.End.Y - 3), caption,
            HorizontalAlignment.Left, -1, captionSize, new Color(.52f, .94f, 1f, opacity * .92f));
    }
    private void DrawCompletions(Rect2 visible, ConstructionSiteVisualConfig c)
    {
        float duration = ConstructionDroneMasterConfig.Safe(c.CompletionReadoutSeconds, 1, .8f, 1.2f);
        foreach (var site in _completions)
        {
            var tower = site.Tower; if (!GodotObject.IsInstanceValid(tower) || !tower.IsInsideTree() || tower.IsQueuedForDeletion()) continue;
            Vector2 size = (Vector2)tower.Footprint * _cellSize;
            Rect2 bounds = new(ToLocal(tower.GlobalPosition) - size * .5f, size);
            if (visible.Intersects(bounds.Grow(240))) DrawTelemetry(bounds, site, false, Mathf.Clamp((duration - site.Age) / (duration * .4f), 0, 1), c);
        }
    }
}
