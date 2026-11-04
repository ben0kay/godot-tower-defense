// General world-space floating text feedback; dormant between explicit events.
using Godot;

public enum WorldCalloutSymbol { None, ThickUpArrow }

public partial class WorldCallout : Node2D
{
    private static readonly Vector2[] Arrow =
    {
        new(0, 0), new(9, -10), new(18, 0),
        new(13, 0), new(13, 12), new(5, 12), new(5, 0)
    };
    private static FeedbackMasterConfig _master;
    private static WorldCalloutStyle _style;
    private string _message = "";
    private Color _color;
    private WorldCalloutSymbol _symbol;
    private float _timeLeft, _duration, _fade, _fadeIn, _rise, _startScale, _endScale;
    private int _fontSize;

    // =========================================================
    // A single shared toggle controls all future callout uses, checked when emitted.
    public static bool Enabled
    {
        get
        {
            _master ??= GD.Load<FeedbackMasterConfig>("res://FEEDBACK/DefaultFeedbackConfig.tres");
            return _master?.WorldCalloutsEnabled ?? true;
        }
    }

    // =========================================================
    // TopLevel keeps the message world-upright and independent from tower rotation/tint.
    public override void _Ready()
    {
        TopLevel = true; ZIndex = 50; Visible = false;
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Reusable event call: text and colour are caller-owned; no dependency on tower ranks or heat.
    public void Play(Vector2 worldPosition, string message, Color tint, WorldCalloutSymbol symbol = WorldCalloutSymbol.None)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(message)) return;
        _style ??= GD.Load<WorldCalloutStyle>("res://FEEDBACK/WorldCallouts/DefaultWorldCalloutStyle.tres");
        _duration = Mathf.Max(0.1f, _style?.DurationSeconds ?? 1.35f);
        _fadeIn = Mathf.Clamp(_style?.FadeInSeconds ?? 0.15f, 0.01f, _duration * 0.5f);
        _fade = Mathf.Clamp(_style?.FadeSeconds ?? 0.48f, 0.01f, _duration - _fadeIn);
        _rise = Mathf.Max(0f, _style?.RisePixels ?? 20f);
        _startScale = Mathf.Clamp(_style?.StartScale ?? 0.96f, 0.5f, 1f);
        _endScale = Mathf.Clamp(_style?.EndScale ?? 1.14f, 1f, 1.5f);
        _fontSize = Mathf.Clamp(_style?.FontSize ?? 13, 8, 32);
        _message = message; _color = tint; _symbol = symbol; _timeLeft = _duration;
        GlobalPosition = worldPosition;
        Visible = true; SetProcess(true); QueueRedraw();
    }

    // =========================================================
    // Animate only while visible; no idle timer or global update loop.
    public override void _Process(double delta)
    {
        _timeLeft -= (float)delta;
        if (_timeLeft <= 0f)
        {
            _timeLeft = 0f; Visible = false; SetProcess(false);
            return;
        }
        QueueRedraw();
    }

    // =========================================================
    // Static polygon points plus local draw transforms avoid redraw-time array allocations.
    public override void _Draw()
    {
        if (_timeLeft <= 0f) return;
        float elapsed = _duration - _timeLeft;
        float progress = Mathf.Clamp(elapsed / _duration, 0f, 1f);
        float upward = _rise * (1f - (1f - progress) * (1f - progress)); // Ease out toward the top.
        float entrance = Mathf.SmoothStep(0f, 1f, Mathf.Clamp(elapsed / _fadeIn, 0f, 1f));
        float exit = Mathf.SmoothStep(0f, 1f, Mathf.Clamp(1f - _timeLeft / _fade, 0f, 1f));
        float alpha = entrance * (1f - exit);
        float scale = Mathf.Lerp(_startScale, 1f, entrance) + (_endScale - 1f) * exit;
        Vector2 drawScale = Vector2.One * scale;
        Color foreground = new(_color.R, _color.G, _color.B, alpha);
        Color shadow = new(0f, 0.05f, 0.08f, alpha * 0.8f);
        float textX = _symbol == WorldCalloutSymbol.ThickUpArrow ? 23f : 0f;
        Font font = ThemeDB.FallbackFont;
        // Shared transform keeps the arrow and text together as they swell and softly dissolve.
        DrawSetTransform(new Vector2(1f, 1f - upward), 0f, drawScale);
        if (_symbol == WorldCalloutSymbol.ThickUpArrow) DrawColoredPolygon(Arrow, shadow);
        if (font != null) DrawString(font, new Vector2(textX, 7f), _message, HorizontalAlignment.Left, -1, _fontSize, shadow);
        DrawSetTransform(new Vector2(0f, -upward), 0f, drawScale);
        if (_symbol == WorldCalloutSymbol.ThickUpArrow) DrawColoredPolygon(Arrow, foreground);
        if (font != null) DrawString(font, new Vector2(textX, 7f), _message, HorizontalAlignment.Left, -1, _fontSize, foreground);
    }
}
