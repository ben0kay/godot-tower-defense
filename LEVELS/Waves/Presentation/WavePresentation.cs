// Optional immutable authoring data for future narration and special warning banners.
// Presentation never controls spawning, wave completion, audio playback or gameplay balance.
public enum WaveAlertKind { Standard, Heavy, Final, Boss, None }

public sealed record WavePresentation(
    WaveAlertKind Alert = WaveAlertKind.Standard,
    string BannerTitle = "",
    string BannerDescription = "",
    string Speaker = "",
    string Text = "",
    string VoiceCueId = "");
