// One shared battlefield adapter; wave numbers become alerts without changing scheduling or enemy spawns.
using Godot;

public partial class WavePresentationController : Node
{
    private WaveManager _waves;
    private NotificationManager _alerts;
    private NarrationDisplay _narration;

    // =========================================================
    // Battlefield-scoped subscription: works automatically for all authored WaveSetSource missions.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _waves = GetNodeOrNull<WaveManager>("../Waves");
        _alerts = GetNodeOrNull<NotificationManager>("../UI/GameplayHUD/NotificationManager");
        _narration = GetNodeOrNull<NarrationDisplay>("../UI/GameplayHUD/NarrationDisplay");
        if (_waves != null) _waves.WaveStarted += OnWaveStarted;
    }

    // =========================================================
    // Route independently: un-narrated waves clear the card; an explicitly suppressed alert can still speak.
    private void OnWaveStarted(WaveDefinition wave)
    {
        if (wave == null) return;
        WavePresentation presentation = wave.Presentation;
        if (GodotObject.IsInstanceValid(_narration))
            _narration.ShowMessage(presentation?.Text, presentation?.Speaker);
        if (!GodotObject.IsInstanceValid(_alerts)) return;
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
        _waves = null; _alerts = null; _narration = null;
    }
}
