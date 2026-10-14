// Shared button creation and appearance. Gameplay actions stay with the owning UI.
using Godot;
using System;

public enum UIButtonStyle { Menu, Action, TowerTile, Compact, Danger }

public static class UIButtonFactory
{
    #region Creation and Styling
    // =========================================================
    // For dynamically generated menu buttons; existing .tscn buttons should use Apply.
    public static Button Create(string text, Action pressed, UIButtonStyle style = UIButtonStyle.Menu,
        float height = 48f, UIMasterConfig config = null)
    {
        Button button = new()
        {
            Text = text,
            CustomMinimumSize = new Vector2(0f, height),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
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
