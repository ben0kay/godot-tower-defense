// World-space Citadel credit indicator; updates on one buffer's change event, never polls gameplay.
using Godot;
using System;

public partial class CitadelCreditDisplay : Control
{
    private CreditBuffer _buffer;
    private Label _amount;
    private ProgressBar _bar;

    // =========================================================
    // The visual lives below its owning Citadel footprint and never intercepts world clicks.
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _amount = new Label { Name = "Amount", HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore };
        _amount.AddThemeFontSizeOverride("font_size", 11);
        _amount.AddThemeColorOverride("font_color", new Color("#69e4a3"));
        _amount.Position = Vector2.Zero; _amount.Size = new Vector2(144, 17); AddChild(_amount);
        _bar = new ProgressBar { Name = "Bar", ShowPercentage = false, MouseFilter = MouseFilterEnum.Ignore };
        _bar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color("#20333a") });
        _bar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = new Color("#69e4a3") });
        _bar.Position = new Vector2(8, 18); _bar.Size = new Vector2(128, 4); AddChild(_bar);
        Refresh();
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // A view may be rebound for tests; the previous buffer never retains a stale listener.
    public void Bind(CreditBuffer buffer)
    {
        if (_buffer != null) _buffer.Changed -= OnChanged;
        _buffer = buffer;
        if (_buffer != null) _buffer.Changed += OnChanged;
        Refresh();
    }

    // =========================================================
    // Refresh only on balance mutation, including collections from the distant top HUD.
    private void OnChanged(int current, int capacity) => Refresh();
    private void Refresh()
    {
        if (_amount == null || _bar == null) return;
        int current = _buffer?.Current ?? 0, capacity = _buffer?.Capacity ?? 0;
        _amount.Text = $"CREDITS  {current:N0} / {capacity:N0}";
        _bar.Value = capacity > 0 ? 100.0 * current / capacity : 0;
        TooltipText = "Click the Citadel to collect its stored Credits.";
    }

    // =========================================================
    // Do not leak a session buffer into a removed/replaced world visual.
    public override void _ExitTree() { if (_buffer != null) _buffer.Changed -= OnChanged; _buffer = null; }
}
