// Shared battlefield presenter: load-time mission intel and wave alerts reuse the same tactical narrator.
using Godot;

public partial class WavePresentationController : Node
{
    private WaveManager _waves;
    private SpawnManager _portalSpawns;
    private NotificationManager _alerts;
    private NarrationDisplay _narration;
    private MissionBriefingDefinition _missionBriefing;
    private VoiceAssistant _voice;
    private string _introVoiceId = "";
    private WavePresentation _pendingWaveNarration;

    // =========================================================
    // Battlefield-scoped subscription: works automatically for all authored WaveSetSource missions.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        _waves = GetNodeOrNull<WaveManager>("../Waves");
        _alerts = GetNodeOrNull<NotificationManager>("../UI/GameplayHUD/NotificationManager");
        _narration = GetNodeOrNull<NarrationDisplay>("../UI/GameplayHUD/NarrationDisplay");
        _voice = AudioManager.Find(this)?.GetNodeOrNull<VoiceAssistant>("VoiceAssistant");
        if (_voice != null)
        {
            _voice.CueStarted += OnVoiceCueStarted;
            _voice.CueEnded += OnVoiceCueEnded;
        }
        if (_waves != null) _waves.WaveStarted += OnWaveStarted;
        _portalSpawns = GetNodeOrNull<SpawnManager>("../World/EnemySystems/Spawning");
        if (_portalSpawns != null) _portalSpawns.PortalOpeningWarning += OnPortalOpening;

        // An optional mission intro is a load event, not a wave-start event.
        // A restored checkpoint skips it and keeps its own pause/restore lifecycle.
        if (GetParent() is BattlefieldLauncher launcher && launcher.RestoringCampaign?.Checkpoint != null) return;
        _missionBriefing = GetNodeOrNull<WorldLevel>("../World/Level")?.Settings?.MissionBriefing;
        if (_missionBriefing?.Enabled == true && !string.IsNullOrWhiteSpace(_missionBriefing.Message))
            Callable.From(ShowInitialMissionIntel).CallDeferred();
    }

    // =========================================================
    // Defer once until HUD/world Ready. Do not replace a wave message if Start was already pressed.
    // NarrationDisplay itself owns typewriter timing and non-blocking top-right presentation.
    private void ShowInitialMissionIntel()
    {
        if (!IsInsideTree() || !GodotObject.IsInstanceValid(_narration) ||
            !GodotObject.IsInstanceValid(_waves) || _waves.CurrentWave != 0 ||
            _waves.State != WaveManager.WaveState.Ready || _missionBriefing?.Enabled != true) return;
        // VoiceAssistant owns scheduling; its started/ended events keep the same visual
        // narrator aligned even if the cue initially has to wait behind another voice.
        string cueId = _missionBriefing.VoiceCueId?.Trim() ?? "";
        if (cueId.Length > 0 && GodotObject.IsInstanceValid(_voice))
        {
            _introVoiceId = cueId;
            if (_voice.Say(cueId)) return;
            _introVoiceId = ""; // Missing/unplayable clip retains normal text-only behaviour.
        }
        _narration.ShowMessage(_missionBriefing.Message, _missionBriefing.Speaker);
    }

    // =========================================================
    // A queued clip starts the typewriter when it actually begins playback.
    private void OnVoiceCueStarted(string id)
    {
        if (id != _introVoiceId || !GodotObject.IsInstanceValid(_narration) || _missionBriefing == null) return;
        _narration.ShowMessage(_missionBriefing.Message, _missionBriefing.Speaker, waitForVoiceEnd: true);
    }

    // =========================================================
    // Audio playback, not a guessed fixed timer, releases the on-screen transmission.
    private void OnVoiceCueEnded(string id)
    {
        if (id != _introVoiceId) return;
        _introVoiceId = "";
        if (!GodotObject.IsInstanceValid(_narration)) return;
        if (_pendingWaveNarration != null)
            _narration.ShowMessage(_pendingWaveNarration.Text, _pendingWaveNarration.Speaker);
        else
            _narration.ReleaseVoiceHold();
        _pendingWaveNarration = null;
    }

    // =========================================================
    // One batched later-wave warning, never one banner per portal or any deployment-intel interruption.
    private void OnPortalOpening(int count)
    {
        if (!GodotObject.IsInstanceValid(_alerts)) return;
        _alerts.ShowMajor(new MajorAlertDefinition
        {
            Id = $"portal_opening_{_waves.CurrentWave}",
            Title = count == 1 ? "NEW HOSTILE PORTAL" : $"{count} HOSTILE PORTALS OPENING",
            Description = "Dimensional breach detected. Prepare your defenses.",
            Footer = "SECTOR WARNING", BadgeText = "!",
            Tone = MajorAlertTone.Warning, Priority = 65, DurationSeconds = 2.2f,
            CooldownSeconds = 0, OncePerSession = false
        });
    }

    // =========================================================
    // Route independently: un-narrated waves clear the card; an explicitly suppressed alert can still speak.
    private void OnWaveStarted(WaveDefinition wave)
    {
        if (wave == null) return;
        WavePresentation presentation = wave.Presentation;
        if (GodotObject.IsInstanceValid(_narration))
        {
            // A wave can start during the briefing, but must not erase its subtitles
            // while the AI is still speaking. Authored wave text follows after it.
            if (_introVoiceId.Length > 0)
            {
                if (!string.IsNullOrWhiteSpace(presentation?.Text)) _pendingWaveNarration = presentation;
            }
            else _narration.ShowMessage(presentation?.Text, presentation?.Speaker);
        }
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
        if (GodotObject.IsInstanceValid(_portalSpawns)) _portalSpawns.PortalOpeningWarning -= OnPortalOpening;
        _portalSpawns = null;
        if (GodotObject.IsInstanceValid(_waves)) _waves.WaveStarted -= OnWaveStarted;
        if (GodotObject.IsInstanceValid(_voice))
        {
            _voice.CueStarted -= OnVoiceCueStarted;
            _voice.CueEnded -= OnVoiceCueEnded;
            if (_introVoiceId.Length > 0) _voice.Cancel(_introVoiceId);
        }
        _waves = null; _alerts = null; _narration = null; _missionBriefing = null;
        _voice = null; _introVoiceId = ""; _pendingWaveNarration = null;
    }
}
