// Passive upper-right auto-start telemetry; WaveManager owns timing and publishes only displayed-second changes.
using Godot;
public partial class GameplayHUD
{
    private Label _autoStart;
    public void SetWaveAutoStart(int seconds)
    {
        if (_autoStart == null) return;
        _autoStart.Visible = seconds >= 0;
        if (seconds < 0) return;
        _autoStart.Text = $"AUTO START · {seconds / 60:00}:{seconds % 60:00}";
        _autoStart.TooltipText = "Next wave starts automatically when this countdown ends. Press NEXT WAVE to start now. Pausing also pauses the countdown.";
    }
    // =========================================================
    // Same row, immediately left of Start/Next Wave; stays within the viewport on narrower windows.
    private void LayoutWaveCountdown(float margin, float height, int fontSize)
    {
        if (_autoStart == null) return;
        float width = Mathf.Min(240, Mathf.Max(0, _startWave.Position.X - margin - 10));
        _autoStart.Position = new Vector2(_startWave.Position.X - width - 10, height + 6);
        _autoStart.Size = new Vector2(width, 26);
        _autoStart.AddThemeFontSizeOverride("font_size", Mathf.Max(10, fontSize - 3));
        _autoStart.AddThemeColorOverride("font_color", Config.AccentColor);
    }
}
