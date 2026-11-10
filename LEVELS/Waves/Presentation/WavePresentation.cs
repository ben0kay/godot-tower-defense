// Optional immutable authoring data for narration, voice-over and special warning banners.
using Godot;

public enum WaveAlertKind { Standard, Heavy, Final, Boss, None }

public sealed record WavePresentation(
    WaveAlertKind Alert = WaveAlertKind.Standard,
    string BannerTitle = "",
    string BannerDescription = "",
    string Speaker = "",
    string Text = "",
    string VoiceCueId = "",
    AudioStream VoiceClip = null);
