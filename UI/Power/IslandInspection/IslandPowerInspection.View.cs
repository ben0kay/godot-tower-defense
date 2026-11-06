// Cursor tooltip and temporary footprint highlights; never changes a tower's selection, damage or disabled tint.
using Godot;
using System;
using System.Collections.Generic;

public partial class IslandPowerInspection
{
    private PanelContainer _tooltip;
    private Label _text;
    private readonly List<IslandTowerPowerSample> _samples = new();
    private readonly Dictionary<TowerBase, Warning> _warnings = new();
    private readonly HashSet<TowerBase> _seen = new();
    private readonly List<TowerBase> _stale = new();
    private sealed class Warning
    {
        internal double Consumed, Recharge;
        internal float ShortageSeconds;
        internal bool Red, Amber;
    }
    private void CreateTooltip()
    {
        var layer = new CanvasLayer { Name = "IslandPowerTooltip", Layer = 10 }; AddChild(layer);
        _tooltip = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var box = new StyleBoxFlat
        {
            BgColor = new Color(.025f, .055f, .075f, .96f), BorderColor = new Color(.2f, .8f, .9f, .65f),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
        };
        _tooltip.AddThemeStyleboxOverride("panel", box);
        var margin = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        foreach (string side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 10);
        _text = new Label { MouseFilter = Control.MouseFilterEnum.Ignore, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _text.AddThemeFontSizeOverride("font_size", 12);
        _text.AddThemeColorOverride("font_color", new Color(.78f, .95f, 1));
        layer.AddChild(_tooltip); _tooltip.AddChild(margin); margin.AddChild(_text); _tooltip.Hide();
    }
    private void ShowInspection(int region)
    {
        _shown = region; _leave = 0; _warnings.Clear();
        _tooltip.Show(); SampleIsland(Math.Max(.1f, Style.SampleSeconds)); QueueRedraw();
    }
    private void HideInspection()
    {
        bool redraw = _shown > 0;
        _shown = _candidate = 0; _dwell = _leave = _sampleElapsed = 0;
        _tooltip?.Hide(); _warnings.Clear(); _samples.Clear();
        if (redraw && IsInsideTree()) QueueRedraw();
    }

    // =========================================================
    // Smoothed observed rates and a sustained deficit threshold avoid flashing warnings on each projectile.
    private void SampleIsland(float seconds)
    {
        var state = _power.InspectIslandPower(_shown, _samples);
        _sampleElapsed = 0; _seen.Clear();
        int red = 0, amber = 0; double consumption = 0, recharge = 0;
        float blend = 1 - Mathf.Exp(-seconds / .75f);
        foreach (var row in _samples)
        {
            _seen.Add(row.Tower);
            if (!_warnings.TryGetValue(row.Tower, out var warning))
                _warnings[row.Tower] = warning = new Warning();
            warning.Consumed += (row.Consumed - warning.Consumed) * blend;
            warning.Recharge += (row.Recharge - warning.Recharge) * blend;
            bool draining = !row.Working && warning.Consumed > warning.Recharge + .1;
            warning.ShortageSeconds = draining ? warning.ShortageSeconds + seconds : Math.Max(0, warning.ShortageSeconds - seconds * 2);
            // A newly built capacitor that is receiving charge normally is not presented as a power failure.
            warning.Red = !row.Working && row.Recovering && row.Recharge <= .001;
            warning.Amber = !warning.Red && warning.ShortageSeconds >= Math.Max(.5f, Style.ShortageHoldSeconds);
            if (warning.Red) red++; else if (warning.Amber) amber++;
            consumption += warning.Consumed; recharge += warning.Recharge;
        }
        _stale.Clear();
        foreach (var tower in _warnings.Keys) if (!_seen.Contains(tower)) _stale.Add(tower);
        foreach (var tower in _stale) _warnings.Remove(tower);
        string shared = state.LinkedIslands > 1
            ? $"Shared network supply: {state.NetworkSupply:0.#}/s ({state.LinkedIslands} islands)\n" : "";
        _text.Text = $"ISLAND POWER\nLocal generation: {state.Generation:0.#}/s\n" + shared +
            $"Actual consumption: {consumption:0.#}/s\nTower recharge: {recharge:0.#}/s\n" +
            $"Tower buffers: {state.Stored:0.#} / {state.Capacity:0.#}\n" +
            $"{state.Consumers} power consumers\nRED: {red} depleted / no recharge\nAMBER: {amber} buffers draining";
        _tooltip.ResetSize(); PositionTooltip(); QueueRedraw();
    }
    private void PositionTooltip()
    {
        Vector2 viewport = GetViewportRect().Size;
        _tooltip.CustomMinimumSize = new(Mathf.Max(100, Mathf.Min(300, viewport.X - 24)), 0);
        Vector2 extent = _tooltip.GetCombinedMinimumSize().Max(_tooltip.Size);
        Vector2 pointer = GetViewport().GetMousePosition();
        Vector2 position = pointer + new Vector2(18, 18);
        if (position.X + extent.X > viewport.X - 8) position.X = pointer.X - extent.X - 18;
        if (position.Y + extent.Y > viewport.Y - 8) position.Y = pointer.Y - extent.Y - 18;
        _tooltip.Position = new(Mathf.Clamp(position.X, 8, Mathf.Max(8, viewport.X - extent.X - 8)),
            Mathf.Clamp(position.Y, 8, Mathf.Max(8, viewport.Y - extent.Y - 8)));
    }

    // =========================================================
    // Retained merged edges and diagnostic rectangles only; source hull materials/modulation remain untouched.
    public override void _Draw()
    {
        if (Style == null || !Style.Enabled || _grid == null) return;
        float width = Mathf.Clamp(Style.OutlineWidthPixels, .5f, 3) / _pixelScale;
        foreach (int region in _occupied)
            if (_boundary.Lines.TryGetValue(region, out var lines))
                DrawMultiline(lines, region == _shown ? Style.HoverOutline : Style.Outline, width, true);
        if (_shown <= 0) return;
        foreach (var pair in _warnings)
        {
            TowerBase tower = pair.Key; Warning warning = pair.Value;
            if ((!warning.Red && !warning.Amber) || !GodotObject.IsInstanceValid(tower) || tower.IsDestroyed || tower.IsQueuedForDeletion()) continue;
            Vector2 half = (Vector2)tower.Footprint * (_grid.Config.CellSize * .5f);
            Rect2 rect = new(_grid.ToLocal(tower.GlobalPosition) - half, half * 2);
            Color edge = warning.Red ? Style.Depleted : Style.Shortage;
            Color fill = edge; fill.A = .08f;
            DrawRect(rect, fill); DrawRect(rect.Grow(2 / _pixelScale), edge, false, 2 / _pixelScale);
        }
    }
}
