// Shared cosmetic ring: autonomous impact animation or externally driven gameplay radius; never damage.
using Godot;
using System;

public partial class ShockwaveEffect : Node2D
{
    private static PackedScene _scene;
    private ShockwaveStyle _style;
    private bool _ready, _external, _finished;
    private float _elapsed, _progress;
    private float _maximum = -1;
    public float CurrentRadius { get; private set; }
    [Export] public ShockwaveStyle Style
    {
        get => _style;
        set
        {
            if (_style == value) return;
            if (_ready && GodotObject.IsInstanceValid(_style)) _style.Changed -= Refresh;
            _style = value;
            if (!_ready) return;
            if (_style != null) _style.Changed += Refresh;
            Refresh();
        }
    }

    // =========================================================
    // Missing feedback is harmless. The weapon supplies its style; the scene has a neutral fallback.
    public static ShockwaveEffect Spawn(Node2D parent, Vector2 position, ShockwaveStyle style,
        float? radius = null, bool externallyDriven = false)
    {
        if (!GodotObject.IsInstanceValid(parent) || !parent.IsInsideTree() || parent.IsQueuedForDeletion()) return null;
        _scene ??= GD.Load<PackedScene>("res://FEEDBACK/Shockwave/ShockwaveEffect.tscn");
        ShockwaveEffect effect = _scene.Instantiate<ShockwaveEffect>();
        if (style != null) effect.Style = style;
        effect._external = externallyDriven;
        effect._maximum = radius.HasValue ? Safe(radius.Value) : Safe(effect.Style?.VisualRadius ?? 0);
        parent.AddChild(effect); effect.GlobalPosition = position;
        return effect;
    }

    // =========================================================
    // A private style copy isolates live visual tuning from other effects and authored defaults.
    public override void _Ready()
    {
        _style = _style == null ? new ShockwaveStyle() : (ShockwaveStyle)_style.Duplicate(true);
        _ready = true; _style.Changed += Refresh;
        if (_maximum < 0) _maximum = Safe(_style.VisualRadius);
        CurrentRadius = _external ? 0 : _maximum * Unit(_style.StartRadiusFraction);
        SetProcess(!_external); QueueRedraw();
    }

    // =========================================================
    // Blaster uses this cosmetic clock only after its instantaneous damage has completed.
    public override void _Process(double delta)
    {
        if (_external || _finished || !IsInsideTree() || IsQueuedForDeletion() || !double.IsFinite(delta) || delta <= 0) return;
        _elapsed += (float)delta;
        _progress = Unit(_elapsed / Mathf.Max(.001f, Safe(Style.ExpansionDuration)));
        CurrentRadius = Mathf.Lerp(_maximum * Unit(Style.StartRadiusFraction), _maximum, _progress);
        QueueRedraw();
        if (_progress >= 1) Complete();
    }

    // =========================================================
    // External geometry follows simulation exactly; style radius/duration cannot change the wavefront.
    public void SetWavefront(float radius, float progress)
    {
        if (!_external || _finished) return;
        CurrentRadius = Safe(radius); _progress = Unit(progress); QueueRedraw();
    }

    // =========================================================
    // Both modes release the same lightweight effect without scheduling any combat work.
    public void Complete() { if (_finished) return; _finished = true; SetProcess(false); QueueFree(); }

    // =========================================================
    // Style events redraw this visual only; they never touch a gameplay emitter or stat cache.
    private void Refresh() => QueueRedraw();

    // =========================================================
    // Optional glow, fill and artwork have no influence on the supplied simulation geometry.
    public override void _Draw()
    {
        if (Style == null || CurrentRadius <= 0) return;
        float fadeStart = Unit(Style.FadeStartFraction);
        float fade = _progress <= fadeStart ? 1 : 1 - (_progress - fadeStart) / Mathf.Max(.001f, 1 - fadeStart);
        float alpha = Unit(Style.Opacity) * Mathf.Pow(Unit(fade), Mathf.Max(.01f, Safe(Style.FadePower)));
        Color ring = Style.RingColor; ring.A *= alpha;
        Color glow = Style.GlowColor; glow.A *= alpha;
        if (Safe(Style.FillOpacity) > 0)
        { Color fill = ring; fill.A *= Unit(Style.FillOpacity); DrawCircle(Vector2.Zero, CurrentRadius, fill); }
        if (Safe(Style.GlowWidth) > 0)
            DrawArc(Vector2.Zero, CurrentRadius, 0, Mathf.Tau, 96, glow, Safe(Style.RingWidth) + Safe(Style.GlowWidth), true);
        if (Safe(Style.RingWidth) > 0)
            DrawArc(Vector2.Zero, CurrentRadius, 0, Mathf.Tau, 96, ring, Safe(Style.RingWidth), true);
        if (Style.Artwork != null)
            DrawTextureRect(Style.Artwork, new Rect2(Vector2.One * -CurrentRadius, Vector2.One * CurrentRadius * 2), false, ring);
    }

    // =========================================================
    // Presentation accepts malformed tuning safely without changing combat or passing NaN to drawing.
    private static float Safe(float value) => float.IsFinite(value) ? Mathf.Max(0, value) : 0;
    // =========================================================
    // Fractions are bounded for drawing only.
    private static float Unit(float value) => Mathf.Min(1, Safe(value));
    // =========================================================
    // Manual unload releases the visual resource subscription and stops animation.
    public override void _ExitTree()
    { if (GodotObject.IsInstanceValid(_style)) _style.Changed -= Refresh; _ready = false; _finished = true; }
}
