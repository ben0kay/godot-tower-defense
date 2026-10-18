// Event-driven side-by-side objective meters; health and Credits stay in their respective authoritative models.
using Godot;
using System;
using System.Collections.Generic;

public partial class CitadelHudRow : Control
{
    #region Bound Meters
    private sealed class Meter
    {
        internal CitadelDestination Destination;
        internal Control Cell;
        internal Label Name, Health, Credits;
        internal ProgressBar Bar, CreditBar;
        internal CreditBuffer Buffer;
        internal StyleBoxFlat Fill;
        internal Action<int, int> Changed, CreditsChanged;
        internal Control.GuiInputEventHandler Clicked;
        internal Tween BarTween, FlashTween;
        internal int Previous = -1, PreviousMaximum;
    }
    private readonly List<Meter> _meters = new();
    private UIMasterConfig _config;
    private CitadelSession _session;

    // =========================================================
    // Bind actual objective identities and both health/credit model events; each cell collects its own Credits.
    public void Configure(UIMasterConfig config, IReadOnlyList<CitadelDestination> objectives, CitadelSession session = null)
    {
        _config = config; _session = session; MouseFilter = MouseFilterEnum.Pass; ClipContents = true;
        foreach (var destination in objectives)
        {
            Meter meter = new() { Destination = destination, Cell = new Control { MouseFilter = MouseFilterEnum.Stop, FocusMode = FocusModeEnum.All },
                Buffer = session?.CreditsFor(destination) };
            AddChild(meter.Cell);
            meter.Name = new Label { Name = "Name", Text = destination.DisplayName, HorizontalAlignment = HorizontalAlignment.Center,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, MouseFilter = MouseFilterEnum.Ignore };
            meter.Name.AddThemeColorOverride("font_color", destination.DisplayColor); meter.Cell.AddChild(meter.Name);
            meter.Health = new Label { Name = "Health", HorizontalAlignment = HorizontalAlignment.Center,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, MouseFilter = MouseFilterEnum.Ignore };
            meter.Cell.AddChild(meter.Health);
            meter.Bar = new ProgressBar { Name = "Bar", ShowPercentage = false, MouseFilter = MouseFilterEnum.Ignore };
            meter.Fill = new StyleBoxFlat { BgColor = config.HealthHealthyColor }; meter.Bar.AddThemeStyleboxOverride("fill", meter.Fill);
            meter.Bar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = config.PanelColor }); meter.Cell.AddChild(meter.Bar);
            meter.Credits = new Label { Name = "Credits", HorizontalAlignment = HorizontalAlignment.Center,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, MouseFilter = MouseFilterEnum.Ignore };
            meter.Credits.AddThemeColorOverride("font_color", config.CreditRewardColor); meter.Cell.AddChild(meter.Credits);
            meter.CreditBar = new ProgressBar { Name = "CreditBar", ShowPercentage = false, MouseFilter = MouseFilterEnum.Ignore };
            meter.CreditBar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = config.CreditRewardColor });
            meter.CreditBar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = config.PanelColor });
            meter.Cell.AddChild(meter.CreditBar);
            meter.Changed = (current, maximum) => UpdateMeter(meter, current, maximum);
            meter.CreditsChanged = (current, capacity) => UpdateCredits(meter);
            meter.Clicked = input =>
            {
                if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
                { _session?.TryCollectCredits(meter.Destination); meter.Cell.AcceptEvent(); }
                else if (input is InputEventKey { Pressed: true, Echo: false } key && key.Keycode is Key.Enter or Key.KpEnter or Key.Space)
                { _session?.TryCollectCredits(meter.Destination); meter.Cell.AcceptEvent(); }
            };
            meter.Cell.GuiInput += meter.Clicked;
            destination.Health.HealthChanged += meter.Changed;
            if (meter.Buffer != null) meter.Buffer.Changed += meter.CreditsChanged;
            _meters.Add(meter);
            UpdateMeter(meter, destination.Health.CurrentHealth, destination.Health.MaxHealth);
            UpdateCredits(meter);
        }
        Resized += LayoutMeters; LayoutMeters(); SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Same horizontal row: squeeze labels slightly to fit both distinct 3–5px meter bars.
    private void LayoutMeters()
    {
        if (_meters.Count == 0) return;
        float gap = Mathf.Min(Mathf.Max(0, _config.CitadelCellGap), Size.X / (_meters.Count * 4f));
        float width = Mathf.Max(0, (Size.X - gap * (_meters.Count - 1)) / _meters.Count);
        int font = Mathf.Clamp((int)(width / 9f), 10, 13);
        float barHeight = Mathf.Clamp(_config.CitadelBarHeight, 3, 5);
        for (int i = 0; i < _meters.Count; i++)
        {
            Meter meter = _meters[i]; meter.Cell.Position = new(i * (width + gap), 0); meter.Cell.Size = new(width, Size.Y);
            meter.Name.Position = Vector2.Zero; meter.Name.Size = new(width, 16);
            meter.Name.AddThemeFontSizeOverride("font_size", font);
            meter.Health.Position = new(0, 13); meter.Health.Size = new(width, 15); meter.Health.AddThemeFontSizeOverride("font_size", font);
            meter.Bar.Position = new(0, 27); meter.Bar.Size = new(width, barHeight);
            meter.Credits.Position = new(0, 28 + barHeight); meter.Credits.Size = new(width, Mathf.Max(8, Size.Y - (31 + barHeight)));
            meter.Credits.AddThemeFontSizeOverride("font_size", Mathf.Max(9, font - 2));
            meter.CreditBar.Position = new(0, Mathf.Max(0, Size.Y - 3));
            meter.CreditBar.Size = new(width, 3);
        }
    }

    // =========================================================
    // Health remains unchanged; a Credit update cannot restart HP tweens/flash.
    private void UpdateMeter(Meter meter, int current, int maximum)
    {
        int health = maximum > 0 ? Mathf.Clamp(current, 0, maximum) : 0;
        float fraction = maximum > 0 ? (float)health / maximum : 0;
        bool damage = meter.Previous >= 0 && maximum == meter.PreviousMaximum && health < meter.Previous;
        meter.Previous = health; meter.PreviousMaximum = maximum;
        meter.Health.Text = maximum > 0 ? $"{health:N0} / {maximum:N0}" : "--";
        UpdateTooltip(meter);
        meter.BarTween?.Kill(); meter.FlashTween?.Kill();
        if (damage && _config.CitadelBarTweenSeconds > 0)
        { meter.BarTween = CreateTween(); meter.BarTween.TweenProperty(meter.Bar, "value", fraction * 100.0, _config.CitadelBarTweenSeconds).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out); }
        else meter.Bar.Value = fraction * 100;
        meter.Health.Modulate = damage ? _config.CitadelDamageFlashColor : Colors.White;
        if (damage && _config.CitadelDamageFlashSeconds > 0)
        { meter.FlashTween = CreateTween(); meter.FlashTween.TweenProperty(meter.Health, "modulate", Colors.White, _config.CitadelDamageFlashSeconds); }
        else meter.Health.Modulate = Colors.White;
        meter.Fill.BgColor = fraction <= _config.HealthCriticalThreshold ? _config.HealthCriticalColor
            : fraction <= _config.HealthWarningThreshold ? _config.HealthWarningColor : _config.HealthHealthyColor;
    }

    // =========================================================
    // Only the affected Credits label/bar and tooltip refresh; no per-HUD-frame polling.
    private void UpdateCredits(Meter meter)
    {
        int credits = meter.Buffer?.Current ?? 0, cap = meter.Buffer?.Capacity ?? 0;
        meter.Credits.Text = $"¢ {credits:N0} / {cap:N0}";
        meter.CreditBar.Value = cap > 0 ? 100.0 * credits / cap : 0;
        UpdateTooltip(meter);
    }

    // =========================================================
    // Narrow cells retain full independent numbers and collection instructions in tooltips.
    private static void UpdateTooltip(Meter meter) => meter.Cell.TooltipText =
        $"{meter.Destination.DisplayName}\nHP: {meter.Destination.Health.CurrentHealth:N0} / {meter.Destination.Health.MaxHealth:N0}" +
        $"\nCredits: {meter.Buffer?.Current ?? 0:N0} / {meter.Buffer?.Capacity ?? 0:N0}\nClick to collect Credits";

    // =========================================================
    // Unhook both models, cell input and animations on rebinding/unload.
    public override void _ExitTree()
    {
        Resized -= LayoutMeters;
        foreach (var meter in _meters)
        {
            if (GodotObject.IsInstanceValid(meter.Destination.Health)) meter.Destination.Health.HealthChanged -= meter.Changed;
            if (meter.Buffer != null) meter.Buffer.Changed -= meter.CreditsChanged;
            if (GodotObject.IsInstanceValid(meter.Cell)) meter.Cell.GuiInput -= meter.Clicked;
            meter.BarTween?.Kill(); meter.FlashTween?.Kill();
        }
        _meters.Clear(); _session = null;
    }
    #endregion
}
