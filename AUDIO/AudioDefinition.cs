// Per-sound Inspector Resource; definitions live beside their owning feature's sound files.
using Godot;

public enum GameAudioMode { OneShot, Loop, Intermittent }
public enum GameAudioCategory { WorldSfx, UI, Music, Ambience, Voice }

[Tool, GlobalClass]
public partial class AudioDefinition : Resource
{
    #region Clip and routing
    [ExportGroup("Identity")]
    [Export] public string Id { get; set; } = "";
    [Export] public AudioStream Stream { get; set; }
    [Export] public GameAudioMode Mode { get; set; } = GameAudioMode.OneShot;
    [Export] public GameAudioCategory Category { get; set; } = GameAudioCategory.WorldSfx;
    #endregion

    #region Scheduling
    [ExportGroup("Playback Rules")]
    [Export(PropertyHint.Range, "0,100,1")] public int Priority { get; set; } = 50;
    [Export(PropertyHint.Range, "0,10,0.01")] public float CooldownSeconds { get; set; }
    [Export(PropertyHint.Range, "1,64,1")] public int MaximumInstances { get; set; } = 4;
    #endregion

    #region Position
    [ExportGroup("World Position")]
    [Export] public bool Positional { get; set; } = true;
    [Export] public bool AllowOffscreen { get; set; } = true;
    [Export(PropertyHint.Range, "1,10000,10")] public float MaxDistance { get; set; } = 800f;
    [Export(PropertyHint.Range, "0.1,8,0.1")] public float Attenuation { get; set; } = 1f;
    #endregion

    #region Mix
    [ExportGroup("Mix")]
    [Export(PropertyHint.Range, "-60,12,0.5")] public float VolumeDb { get; set; }
    [Export(PropertyHint.Range, "0.5,2,0.01")] public float PitchScale { get; set; } = 1f;
    #endregion

    #region Emitter options
    [ExportGroup("Continuous / Intermittent")]
    [Export(PropertyHint.Range, "0,5,0.05")] public float FadeInSeconds { get; set; } = 0.3f;
    [Export(PropertyHint.Range, "0,5,0.05")] public float FadeOutSeconds { get; set; } = 0.4f;
    [Export(PropertyHint.Range, "0.1,120,0.1")] public float IntervalMinSeconds { get; set; } = 8f;
    [Export(PropertyHint.Range, "0.1,120,0.1")] public float IntervalMaxSeconds { get; set; } = 18f;
    #endregion
}
