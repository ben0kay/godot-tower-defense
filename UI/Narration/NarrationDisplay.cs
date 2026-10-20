// Reusable non-interactive tactical narration card, anchored top-right beneath the HUD.
// One RichTextLabel reveals characters only while a message is active; no per-character nodes or timers.
using Godot;
using System;

public partial class NarrationDisplay : CanvasLayer
{
    private enum Phase { Hidden, FadingIn, Typing, Holding, FadingOut }
    [Export] public NarrationConfig Config { get; set; }
    private PanelContainer _card;
    private Label _speaker;
    private RichTextLabel _message;
    private NotificationManager _alerts;
    private Phase _phase = Phase.Hidden;
    private float _elapsed, _typed;
    private int _totalCharacters;
    public bool IsActive => _phase != Phase.Hidden;
    public bool IsTyping => _phase == Phase.Typing;
    public int RevealedCharacters => _message?.VisibleCharacters ?? 0;
    public string CurrentText => _message?.Text ?? "";

    #region Lifecycle and Layout
    // =========================================================
    // Initialize once. A CanvasLayer above alerts/battlefield (but below Pause and Results) is mouse-transparent.
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Pausable;
        Config ??= GD.Load<NarrationConfig>("res://UI/Narration/DefaultNarrationConfig.tres") ?? new();
        _card = GetNode<PanelContainer>("Card");
        _speaker = GetNode<Label>("Card/Margin/Lines/Speaker");
        _message = GetNode<RichTextLabel>("Card/Margin/Lines/Message");
        _alerts = GetNodeOrNull<NotificationManager>("../NotificationManager");
        _card.MouseFilter = Control.MouseFilterEnum.Ignore;
        _speaker.MouseFilter = Control.MouseFilterEnum.Ignore;
        _message.MouseFilter = Control.MouseFilterEnum.Ignore;
        _message.BbcodeEnabled = false; _message.ScrollActive = false;
        StyleBoxFlat style = new() { BgColor = Config.Background, BorderColor = Config.Accent,
            BorderWidthLeft = 3, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1 };
        _card.AddThemeStyleboxOverride("panel", style);
        _speaker.AddThemeColorOverride("font_color", Config.Accent);
        _speaker.AddThemeFontSizeOverride("font_size", Config.SpeakerFontSize);
        _message.AddThemeColorOverride("default_color", Config.TextColor);
        _message.AddThemeFontSizeOverride("normal_font_size", Config.BodyFontSize);
        _message.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        GetViewport().SizeChanged += LayoutCard;
        LayoutCard(); Clear();
    }

    // =========================================================
    // Stay beneath the top-right TopBar, and on smaller viewports stack below the centered alert banner.
    private void LayoutCard()
    {
        if (!GodotObject.IsInstanceValid(_card)) return;
        NarrationConfig config = Config ?? new();
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        float right = Mathf.Max(4, config.RightMargin);
        float width = Mathf.Min(Mathf.Max(220, config.PanelWidth), Mathf.Max(1, viewport.X - right * 2));
        float x = Mathf.Max(0, viewport.X - right - width);
        float top = Mathf.Max(52, config.TopMargin);
        if (GodotObject.IsInstanceValid(_alerts) && _alerts.Settings != null)
        {
            NotificationSettings settings = _alerts.Settings;
            float alertWidth = Mathf.Min(settings.MajorWidth, Mathf.Max(260, viewport.X - 24));
            float alertLeft = (viewport.X - alertWidth) * .5f;
            if (x < alertLeft + alertWidth && x + width > alertLeft)
                top = Mathf.Max(top, settings.MajorTopMargin + settings.MajorHeight + config.AlertClearance);
        }
        float height = Mathf.Min(Mathf.Max(80, config.PanelHeight), Mathf.Max(48, viewport.Y - top - 12));
        _card.Position = new Vector2(x, top);
        _card.Size = new Vector2(width, height);
    }

    // =========================================================
    // Disconnect on HUD unload; no timer or per-wave event stays registered inside the display.
    public override void _ExitTree()
    {
        GetViewport().SizeChanged -= LayoutCard;
        SetProcess(false);
    }
    #endregion

    #region Typewriter
    // =========================================================
    // A new transmission replaces the previous one. Empty/disabled text removes the card immediately.
    public void ShowMessage(string text, string speaker = "")
    {
        if (Config?.Enabled != true || string.IsNullOrWhiteSpace(text)) { Clear(); return; }
        text = text.Trim();
        _speaker.Text = string.IsNullOrWhiteSpace(speaker) ? "TACTICAL COMMAND" : speaker.Trim();
        _message.Text = text;
        _totalCharacters = _message.GetTotalCharacterCount();
        _message.VisibleCharacters = 0;
        _elapsed = 0; _typed = 0; _phase = Phase.FadingIn;
        _card.Modulate = new Color(1, 1, 1, 0);
        LayoutCard(); _card.Show(); SetProcess(true);
    }

    // =========================================================
    // Optional future skip input may call this; no new keyboard binding consumes player controls in NP2.
    public void FinishTyping()
    {
        if (!IsActive || _phase == Phase.FadingOut) return;
        _message.VisibleCharacters = _totalCharacters;
        _typed = _totalCharacters; _elapsed = 0; _phase = Phase.Holding;
        _card.Modulate = Colors.White;
    }

    // =========================================================
    // Clear on an un-narrated wave, on mission result, or when a replacement is dismissed.
    public void Clear()
    {
        _phase = Phase.Hidden; _elapsed = 0; _typed = 0; _totalCharacters = 0;
        if (GodotObject.IsInstanceValid(_card)) _card.Hide();
        SetProcess(false);
    }

    // =========================================================
    // No idle processing. Only the active transmission advances its fade, typed characters and lifetime.
    public override void _Process(double delta)
    {
        if (_phase == Phase.Hidden || !double.IsFinite(delta) || delta <= 0) return;
        float dt = (float)Math.Min(delta, .25);
        _elapsed += dt;
        switch (_phase)
        {
            case Phase.FadingIn:
                _card.Modulate = new Color(1, 1, 1, Mathf.Clamp(_elapsed / Mathf.Max(.01f, Config.FadeInSeconds), 0, 1));
                if (_elapsed >= Config.FadeInSeconds) { _elapsed = 0; _phase = Phase.Typing; }
                break;
            case Phase.Typing:
                _typed += dt * Mathf.Max(1f, Config.CharactersPerSecond);
                _message.VisibleCharacters = Mathf.Min(_totalCharacters, Mathf.FloorToInt(_typed));
                if (_message.VisibleCharacters >= _totalCharacters) { _elapsed = 0; _phase = Phase.Holding; }
                break;
            case Phase.Holding:
                if (_elapsed >= Mathf.Max(0f, Config.HoldSeconds)) { _elapsed = 0; _phase = Phase.FadingOut; }
                break;
            case Phase.FadingOut:
                _card.Modulate = new Color(1, 1, 1, 1 - Mathf.Clamp(_elapsed / Mathf.Max(.01f, Config.FadeOutSeconds), 0, 1));
                if (_elapsed >= Config.FadeOutSeconds) Clear();
                break;
        }
    }
    #endregion
}
