// Event-driven side-by-side objective meters; health stays in CitadelHealth, with no HUD polling.
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
        internal Label Name, Health;
        internal ProgressBar Bar;
        internal StyleBoxFlat Fill;
        internal Action<int, int> Changed;
        internal Tween BarTween, FlashTween;
        internal int Previous = -1, PreviousMaximum;
    }
    private readonly List<Meter> _meters = new();
    private UIMasterConfig _config;
    // =========================================================
    // Bind once at deployment; shared configs are read-only and each meter owns its mutable style/tweens.
    public void Configure(UIMasterConfig config, IReadOnlyList<CitadelDestination> objectives)
    {
        _config = config; MouseFilter = MouseFilterEnum.Ignore; ClipContents = true;
        foreach (var destination in objectives)
        {
            Meter meter = new() { Destination = destination, Cell = new Control { MouseFilter = MouseFilterEnum.Pass } };
            AddChild(meter.Cell);
            meter.Name = new Label { Name = "Name", Text = destination.DisplayName, HorizontalAlignment = HorizontalAlignment.Center,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, MouseFilter = MouseFilterEnum.Ignore };
            meter.Name.AddThemeColorOverride("font_color", destination.DisplayColor); meter.Cell.AddChild(meter.Name);
            meter.Health = new Label { Name = "Health", HorizontalAlignment = HorizontalAlignment.Center,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, MouseFilter = MouseFilterEnum.Ignore }; meter.Cell.AddChild(meter.Health);
            meter.Bar = new ProgressBar { Name = "Bar", ShowPercentage = false, MouseFilter = MouseFilterEnum.Ignore };
            meter.Fill = new StyleBoxFlat { BgColor = config.HealthHealthyColor }; meter.Bar.AddThemeStyleboxOverride("fill", meter.Fill);
            meter.Bar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = config.PanelColor }); meter.Cell.AddChild(meter.Bar);
            meter.Changed = (current, maximum) => UpdateMeter(meter, current, maximum);
            destination.Health.HealthChanged += meter.Changed; _meters.Add(meter);
            UpdateMeter(meter, destination.Health.CurrentHealth, destination.Health.MaxHealth);
        }
        Resized += LayoutMeters; LayoutMeters(); SetProcess(false); SetPhysicsProcess(false);
    }
    // =========================================================
    // Keep equal-width columns in one row at every count; small screens truncate text, never wrap or stack.
    private void LayoutMeters()
    {
        if (_meters.Count == 0) return;
        float gap = Mathf.Min(Mathf.Max(0, _config.CitadelCellGap), Size.X / (_meters.Count * 4f));
        float width = Mathf.Max(0, (Size.X - gap * (_meters.Count - 1)) / _meters.Count);
        int font = Mathf.Clamp((int)(width / 9f), 10, 13);
        for (int i = 0; i < _meters.Count; i++)
        {
            Meter meter = _meters[i]; meter.Cell.Position = new(i * (width + gap), 0); meter.Cell.Size = new(width, Size.Y);
            meter.Name.Position = Vector2.Zero; meter.Name.Size = new(width, 18);
            meter.Name.AddThemeFontSizeOverride("font_size", font);
            meter.Health.Position = new(0, 17); meter.Health.Size = new(width, 18); meter.Health.AddThemeFontSizeOverride("font_size", font);
            meter.Bar.Position = new(0, Mathf.Min(36, Mathf.Max(0, Size.Y - _config.CitadelBarHeight)));
            meter.Bar.Size = new(width, _config.CitadelBarHeight);
        }
    }
    // =========================================================
    // Only the changed objective animates; tweened presentation never replaces authoritative numeric HP.
    private void UpdateMeter(Meter meter, int current, int maximum)
    {
        int health = maximum > 0 ? Mathf.Clamp(current, 0, maximum) : 0;
        float fraction = maximum > 0 ? (float)health / maximum : 0;
        bool damage = meter.Previous >= 0 && maximum == meter.PreviousMaximum && health < meter.Previous; meter.Previous = health; meter.PreviousMaximum = maximum;
        meter.Health.Text = maximum > 0 ? $"{health:N0} / {maximum:N0}" : "--";
        meter.Cell.TooltipText = $"{meter.Destination.DisplayName}\nHP: {health:N0} / {maximum:N0}";
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
    // Unhook model events and stop animations on restart/unload; no old HUD is retained by an objective.
    public override void _ExitTree()
    {
        foreach (var meter in _meters)
        {
            if (GodotObject.IsInstanceValid(meter.Destination.Health)) meter.Destination.Health.HealthChanged -= meter.Changed;
            meter.BarTween?.Kill(); meter.FlashTween?.Kill();
        }
    }
    #endregion
}
