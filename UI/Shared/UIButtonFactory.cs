// Shared button creation and appearance. Gameplay actions stay with the owning UI.
using Godot;
using System;

public enum UIButtonStyle { Menu, Action, TowerTile, Compact, Danger }

public static class UIButtonFactory
{
    private const string FeedbackBoundMeta = "ui_audio_feedback_bound";
    private const string BackMeta = "ui_audio_back_navigation";
    private const string TowerCardMeta = "ui_audio_tower_card";
    private const string HoverSoundId = "ui_button_hover";
    private const string ClickSoundId = "ui_button_click";
    private const string BackSoundId = "ui_button_back";

    #region Creation and Styling
    // =========================================================
    // For dynamically generated menu buttons; existing .tscn buttons should use Apply.
    public static Button Create(string text, Action pressed, UIButtonStyle style = UIButtonStyle.Menu,
        float height = 48f, UIMasterConfig config = null, bool backNavigation = false)
    {
        Button button = new()
        {
            Text = text,
            CustomMinimumSize = new Vector2(0f, height),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        if (style == UIButtonStyle.TowerTile) button.SetMeta(TowerCardMeta, true);
        if (backNavigation) button.SetMeta(BackMeta, true);
        Apply(button, style, config);
        if (pressed != null) button.Pressed += pressed;
        return button;
    }

    // =========================================================
    // Restyle a button already authored in a .tscn without replacing it or rebinding its signal.
    public static void Apply(Button button, UIButtonStyle style = UIButtonStyle.Menu, UIMasterConfig config = null)
    {
        if (button == null) throw new ArgumentNullException(nameof(button));
        config ??= GD.Load<UIMasterConfig>("res://UI/DefaultUIConfig.tres") ?? new UIMasterConfig();
        Color accent = config.AccentColor, text = config.TextColor, muted = config.MutedTextColor;
        Color normal = style == UIButtonStyle.Menu ? config.PanelColor : config.ButtonColor;
        Color edge = style == UIButtonStyle.Danger ? new Color("#b96262") : accent;
        Color hover = normal.Lightened(0.20f), pressed = normal.Lightened(0.32f);
        if (style == UIButtonStyle.Danger) { normal = new Color("#3b242a"); hover = new Color("#60343a"); pressed = new Color("#733f42"); }
        if (style == UIButtonStyle.Compact) { normal = config.PanelColor; hover = config.ButtonColor; }
        int fontSize = style switch
        {
            UIButtonStyle.Menu => 20, UIButtonStyle.Action => 15,
            UIButtonStyle.TowerTile => 13, UIButtonStyle.Compact => 12, _ => 16
        };
        button.Flat = false;
        button.FocusMode = Control.FocusModeEnum.All;
        button.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeColorOverride("font_color", text);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        button.AddThemeColorOverride("font_focus_color", Colors.White);
        button.AddThemeColorOverride("font_disabled_color", muted);
        button.AddThemeStyleboxOverride("normal", Box(normal, style == UIButtonStyle.TowerTile ? config.ButtonColor.Lightened(0.30f) : edge));
        button.AddThemeStyleboxOverride("hover", Box(hover, edge));
        button.AddThemeStyleboxOverride("pressed", Box(pressed, edge));
        button.AddThemeStyleboxOverride("disabled", Box(config.TopBarColor, muted));
        StyleBoxFlat focus = Box(Colors.Transparent, accent);
        focus.DrawCenter = false;
        button.AddThemeStyleboxOverride("focus", focus);
        AttachFeedback(button);
    }

    // =========================================================
    // Attach once, even if a Results or modal button is restyled repeatedly.
    // Only menu-screen buttons and in-game TowerTile cards make sound.
    private static void AttachFeedback(Button button)
    {
        if (button.HasMeta(FeedbackBoundMeta)) return;
        button.SetMeta(FeedbackBoundMeta, true);
        button.MouseEntered += () =>
        {
            if (!button.Disabled && button.IsInsideTree() && button.IsVisibleInTree() && HasFeedback(button))
                AudioManager.Find(button)?.Play(HoverSoundId);
        };
        // Subscribe before the owning action: navigating away cannot cut off the UI cue.
        button.Pressed += () =>
        {
            if (!button.IsInsideTree() || !HasFeedback(button)) return;
            AudioManager.Find(button)?.Play(button.HasMeta(BackMeta) ? BackSoundId : ClickSoundId);
        };
    }

    // =========================================================
    // Scene-local menu buttons, plus the separate HUD's TowerTile card buttons.
    private static bool HasFeedback(Button button)
    {
        if (button.HasMeta(TowerCardMeta)) return true;
        string scene = button.GetTree().CurrentScene?.SceneFilePath ?? "";
        return scene.StartsWith("res://UI/Menus/", StringComparison.Ordinal);
    }

    // =========================================================
    // Keyboard Back uses the same audio ID as the explicit Back buttons.
    public static void PlayBack(Node context) => AudioManager.Find(context)?.Play(BackSoundId);

    // =========================================================
    // Opt-in bright halo for selected/hovered mission cards and future featured buttons.
    // Native StyleBox shadows render only in hover/focus/pressed states; no per-frame UI tick.
    public static void ApplySelectionGlow(Button button, Color idleFill, Color activeFill, Color accent)
    {
        if (button == null) throw new ArgumentNullException(nameof(button));
        StyleBoxFlat idle = Box(idleFill, new Color(accent.R, accent.G, accent.B, 0.65f));
        idle.ContentMarginLeft = idle.ContentMarginRight = 0;
        idle.ContentMarginTop = idle.ContentMarginBottom = 0;

        StyleBoxFlat hover = Box(activeFill, accent.Lightened(0.25f));
        hover.SetBorderWidthAll(2);
        hover.ShadowColor = new Color(accent.R, accent.G, accent.B, 0.55f);
        hover.ShadowSize = 13;
        hover.ContentMarginLeft = hover.ContentMarginRight = 0;
        hover.ContentMarginTop = hover.ContentMarginBottom = 0;

        StyleBoxFlat pressed = (StyleBoxFlat)hover.Duplicate();
        pressed.BgColor = activeFill.Lightened(0.13f);
        pressed.ShadowSize = 9;

        StyleBoxFlat focus = (StyleBoxFlat)hover.Duplicate();
        focus.DrawCenter = false;
        focus.SetBorderWidthAll(3);
        focus.ShadowColor = new Color(accent.R, accent.G, accent.B, 0.70f);
        focus.ShadowSize = 15;

        button.AddThemeStyleboxOverride("normal", idle);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("focus", focus);
    }

    #endregion

    #region Shared Surfaces
    // =========================================================
    // Consistent panel geometry, reusable by menus and the gameplay HUD.
    public static StyleBoxFlat Box(Color fill, Color edge) => new()
    {
        BgColor = fill, BorderColor = edge,
        BorderWidthLeft = 1, BorderWidthTop = 1,
        BorderWidthRight = 1, BorderWidthBottom = 1,
        ContentMarginLeft = 12, ContentMarginRight = 12,
        ContentMarginTop = 8, ContentMarginBottom = 8,
        CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
        CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4
    };

    // =========================================================
    // Shared menu label palette; headings can override the size.
    public static Label Label(string value, int size = 18, UIMasterConfig config = null)
    {
        config ??= GD.Load<UIMasterConfig>("res://UI/DefaultUIConfig.tres") ?? new UIMasterConfig();
        Label label = new() { Text = value, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", config.TextColor);
        return label;
    }
    #endregion
}
