// One shared battlefield adapter; wave numbers become alerts without changing scheduling or enemy spawns.
using Godot;

public partial class WavePresentationController : Node
{
    private WaveManager _waves;
    private NotificationManager _alerts;

    // =========================================================
    // Battlefield-scoped subscription: works automatically for all authored WaveSetSource missions.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _waves = GetNodeOrNull<WaveManager>("../Waves");
        _alerts = GetNodeOrNull<NotificationManager>("../UI/GameplayHUD/NotificationManager");
        if (_waves != null) _waves.WaveStarted += OnWaveStarted;
    }

    // =========================================================
    // Present one silent, nonblocking placeholder at each accepted wave start.
    // Authored text and voice IDs are reserved; narrator display and sound come in NP2.
    private void OnWaveStarted(WaveDefinition wave)
    {
        if (wave == null || !GodotObject.IsInstanceValid(_alerts)) return;
        WavePresentation presentation = wave.Presentation;
        WaveAlertKind kind = presentation?.Alert ?? WaveAlertKind.Standard;
        if (kind == WaveAlertKind.None) return;
        MajorAlertDefinition banner = new()
        {
            Id = $"wave_start_{wave.Number}",
            Title = string.IsNullOrWhiteSpace(presentation?.BannerTitle)
                ? $"WAVE {wave.Number:00} INCOMING" : presentation.BannerTitle,
            Description = presentation?.BannerDescription ?? "",
            Footer = "HOSTILE WAVE ALERT",
            BadgeText = "!",
            Tone = kind switch
            {
                WaveAlertKind.Boss => MajorAlertTone.Danger,
                WaveAlertKind.Heavy or WaveAlertKind.Final => MajorAlertTone.Warning,
                _ => MajorAlertTone.Information
            },
            Priority = kind == WaveAlertKind.Boss ? 95 : kind == WaveAlertKind.Final ? 85 : 50,
            DurationSeconds = 3f,
            CooldownSeconds = 0f,
            OncePerSession = false,
            VoiceCueId = presentation?.VoiceCueId ?? ""
        };
        _alerts.ShowMajor(banner);
    }

    // =========================================================
    // Avoid cross-session event listeners when a mission reloads or returns to the campaign.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_waves)) _waves.WaveStarted -= OnWaveStarted;
        _waves = null; _alerts = null;
    }
}
