// Pure reusable beam presentation: accepts world endpoints/active state; knows no enemy or damage API.
using Godot;
public partial class BeamRenderer : Node2D
{
    #region Visual Configuration
    private BeamStyle _style;
    private bool _ready;
    [Export] public BeamStyle Style
    {
        get => _style;
        set
        {
            if (_style == value) return;
            if (_ready && GodotObject.IsInstanceValid(_style)) _style.Changed -= RefreshStyle;
            _style = value ?? new BeamStyle();
            if (!_ready) return;
            if (_style != null) _style.Changed += RefreshStyle;
            RefreshStyle();
        }
    }
    public bool IsActive { get; private set; }
    private Line2D _outer, _core;
    private float _time;
    #endregion
    #region Lifecycle and Presentation
    // =========================================================
    // Allocate the two line endpoints once and isolate mutable per-instance visual tuning.
    public override void _Ready()
    {
        _outer = GetNode<Line2D>("Outer"); _core = GetNode<Line2D>("Core");
        _outer.AddPoint(Vector2.Zero); _outer.AddPoint(Vector2.Zero);
        _core.AddPoint(Vector2.Zero); _core.AddPoint(Vector2.Zero);
        _style = _style == null ? new BeamStyle() : (BeamStyle)_style.Duplicate(true);
        _ready = true; _style.Changed += RefreshStyle; RefreshStyle(); SetBeam(Vector2.Zero, Vector2.Zero, false);
    }
    // =========================================================
    // Endpoints are world-space; fixed renderer placement decouples visuals from turret rotation.
    public void SetBeam(Vector2 fromWorld, Vector2 toWorld, bool active)
    {
        IsActive = active && fromWorld.IsFinite() && toWorld.IsFinite();
        if (!_ready) return;
        Visible = IsActive; SetProcess(IsActive && Safe(Style.PulseAmount) > 0 && Safe(Style.PulseFrequency) > 0);
        Vector2 start = IsActive ? ToLocal(fromWorld) : Vector2.Zero;
        Vector2 end = IsActive ? ToLocal(toWorld) : Vector2.Zero;
        _outer.SetPointPosition(0, start); _outer.SetPointPosition(1, end);
        _core.SetPointPosition(0, start); _core.SetPointPosition(1, end);
    }
    // =========================================================
    // Restrained width pulse is entirely cosmetic and runs only while visible.
    public override void _Process(double delta)
    { _time += (float)delta; ApplyWidths(); }
    // =========================================================
    // Restyling touches only line appearance and optional cosmetic processing.
    private void RefreshStyle()
    {
        if (!_ready || Style == null) return;
        _core.DefaultColor = Style.CoreColor; _outer.DefaultColor = Style.OuterColor; ApplyWidths();
        SetProcess(IsActive && Safe(Style.PulseAmount) > 0 && Safe(Style.PulseFrequency) > 0);
    }
    // =========================================================
    // Invalid widths/pulses fall back safely without changing any combat input.
    private void ApplyWidths()
    {
        float factor = 1 + Mathf.Min(0.5f, Safe(Style.PulseAmount)) * Mathf.Sin(_time * Mathf.Tau * Safe(Style.PulseFrequency));
        _core.Width = Safe(Style.CoreWidth) * factor; _outer.Width = Safe(Style.OuterWidth) * factor;
    }
    // =========================================================
    // Nonfinite/negative presentation numbers never reach line geometry.
    private static float Safe(float value) => float.IsFinite(value) ? Mathf.Max(0, value) : 0;
    // =========================================================
    // Unloading clears visual state and releases resource subscriptions.
    public override void _ExitTree()
    { if (GodotObject.IsInstanceValid(_style)) _style.Changed -= RefreshStyle; IsActive = false; Visible = false; _ready = false; }
    #endregion
}
