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
    // Fixed compact world-space panel outside the full footprint, beneath normal screen HUD layers.
    private void DrawTelemetry(Rect2 bounds, string text, float opacity, ConstructionSiteVisualConfig c)
    {
        if (!c.TelemetryEnabled) return;
        int fontSize = Mathf.Clamp(c.TelemetryFontSize, 8, 18);
        float width = ConstructionDroneMasterConfig.Safe(c.TelemetryWidth, 132, 88, 220);
        Vector2 offset = c.TelemetryOffset.IsFinite() ? c.TelemetryOffset : new(8, -14);
        Rect2 panel = new(new Vector2(bounds.End.X, bounds.Position.Y) + offset, new(width, fontSize + 10));
        // Eight bounded vertical lanes avoid overlapping adjacent-site labels; excess cosmetic labels may skip.
        Vector2 anchor = panel.Position; bool placed = false;
        for (int lane = 0; lane < 8 && !placed; lane++)
        {
            int row = lane == 0 ? 0 : ((lane + 1) / 2) * (lane % 2 == 1 ? -1 : 1);
            panel.Position = anchor + Vector2.Down * (row * (panel.Size.Y + 3));
            placed = true;
            for (int i = 0; i < _readoutCount; i++) if (_readoutBounds[i].Intersects(panel.Grow(2))) { placed = false; break; }
        }
        if (!placed || _readoutCount >= _readoutBounds.Length) return;
        _readoutBounds[_readoutCount++] = panel;
        Color bg = c.TelemetryBackground, tint = c.TelemetryColor; bg.A *= opacity; tint.A *= opacity;
        DrawRect(panel, bg); DrawLine(panel.Position, panel.Position + Vector2.Down * panel.Size.Y, tint, 1, true);
        DrawString(c.TelemetryFont ?? ThemeDB.FallbackFont, panel.Position + new Vector2(6, fontSize + 2), text ?? "",
            HorizontalAlignment.Left, width - 10, fontSize, tint);
    }
    private void DrawCompletions(Rect2 visible, ConstructionSiteVisualConfig c)
    {
        float duration = ConstructionDroneMasterConfig.Safe(c.CompletionReadoutSeconds, 1, .8f, 1.2f);
        foreach (var site in _completions)
        {
            var tower = site.Tower; if (!GodotObject.IsInstanceValid(tower) || !tower.IsInsideTree() || tower.IsQueuedForDeletion()) continue;
            Vector2 size = (Vector2)tower.Footprint * _cellSize;
            Rect2 bounds = new(ToLocal(tower.GlobalPosition) - size * .5f, size);
            if (visible.Intersects(bounds.Grow(240))) DrawTelemetry(bounds, site.Text, Mathf.Clamp((duration - site.Age) / (duration * .4f), 0, 1), c);
        }
    }
}
