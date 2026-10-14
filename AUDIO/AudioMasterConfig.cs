// Shared Inspector-editable mix, culling and playback limits; individual sounds own their own playback rules.
using Godot;

[Tool, GlobalClass]
public partial class AudioMasterConfig : Resource
{
    #region Volume
    [ExportGroup("Mixer (dB)")]
    [Export(PropertyHint.Range, "-60,6,0.5")] public float MasterDb { get; set; }
    [Export(PropertyHint.Range, "-60,6,0.5")] public float MusicDb { get; set; }
    [Export(PropertyHint.Range, "-60,6,0.5")] public float AmbienceDb { get; set; }
    [Export(PropertyHint.Range, "-60,6,0.5")] public float WorldSfxDb { get; set; }
    [Export(PropertyHint.Range, "-60,6,0.5")] public float UiDb { get; set; }
    [Export(PropertyHint.Range, "-60,6,0.5")] public float VoiceDb { get; set; }
    #endregion

    #region Playback budgets
    [ExportGroup("One-Shot Budgets")]
    [Export(PropertyHint.Range, "1,128,1")] public int MaxWorldOneShots { get; set; } = 24;
    [Export(PropertyHint.Range, "1,32,1")] public int MaxUiOneShots { get; set; } = 6;
    [Export(PropertyHint.Range, "1,8,1")] public int MaxVoiceOneShots { get; set; } = 1;

    [ExportGroup("Loop Budgets")]
    [Export(PropertyHint.Range, "1,64,1")] public int MaxWorldLoops { get; set; } = 12;
    [Export(PropertyHint.Range, "1,32,1")] public int MaxAmbienceLoops { get; set; } = 6;
    [Export(PropertyHint.Range, "1,2048,1")] public int MaxRegisteredEmitters { get; set; } = 256;
    [Export(PropertyHint.Range, "0.05,2,0.05")] public float EmitterCheckSeconds { get; set; } = 0.25f;
    #endregion

    #region Positional sounds
    [ExportGroup("World Culling")]
    [Export] public bool DistanceCulling { get; set; } = true;
    [Export] public bool ScreenCulling { get; set; } = true;
    [Export(PropertyHint.Range, "0,600,10")] public float ScreenMarginPixels { get; set; } = 120f;
    [Export(PropertyHint.Range, "0,1000,10")] public float LoopResumeMarginPixels { get; set; } = 80f;
    [Export] public bool PauseWorldAudioWithGame { get; set; } = true;
    #endregion

    #region Voice assistant
    [ExportGroup("Voice Assistant")]
    [Export(PropertyHint.Range, "0,16,1")] public int VoiceQueueLimit { get; set; } = 4;
    [Export] public bool DuckMusicDuringVoice { get; set; } = true;
    [Export(PropertyHint.Range, "-30,0,1")] public float VoiceMusicDuckDb { get; set; } = -8f;
    #endregion
}
