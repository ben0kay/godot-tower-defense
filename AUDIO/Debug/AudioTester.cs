// Optional DEBUG-only synthetic playback check; no WAV/OGG assets are needed.
using Godot;
using System;

public enum AudioTestScenario { CycleExamples, PriorityBurst, Intermittent, VoiceQueue }

public partial class AudioTester : Node
{
    [ExportGroup("Preview")]
    [Export] public bool Enabled { get; set; } = true;
    [Export] public Key PreviewKey { get; set; } = Key.F9;
    [Export] public AudioTestScenario Scenario { get; set; } = AudioTestScenario.CycleExamples;

#if DEBUG
    #region State
    private AudioManager _audio;
    private Node2D _source;
    private int _step;
    private bool _loopOn, _intermittentOn;
    private const string Click = "debug_audio_ui";
    private const string World = "debug_audio_world";
    private const string Hum = "debug_audio_hum";
    private const string Bird = "debug_audio_bird";
    private const string Voice = "debug_audio_voice";
    #endregion

    #region Setup
    // =========================================================
    // Register in-memory tones only for local test runs; authored catalog stays empty.
    public override void _Ready()
    {
        if (!Enabled) return;
        _audio = AudioManager.Find(this);
        if (_audio == null) return;
        _source = new Node2D { Name = "AudioTestSource" };
        AddChild(_source);
        Register(Click, 690f, GameAudioMode.OneShot, GameAudioCategory.UI, false, 50);
        Register(World, 415f, GameAudioMode.OneShot, GameAudioCategory.WorldSfx, true, 50);
        Register(Hum, 145f, GameAudioMode.Loop, GameAudioCategory.WorldSfx, true, 40);
        Register(Bird, 910f, GameAudioMode.Intermittent, GameAudioCategory.Ambience, true, 20);
        Register(Voice, 550f, GameAudioMode.OneShot, GameAudioCategory.Voice, false, 95);
    }

    // =========================================================
    // Stop registered loops before debug nodes are freed.
    public override void _ExitTree()
    {
        if (!GodotObject.IsInstanceValid(_audio) || !GodotObject.IsInstanceValid(_source)) return;
        _audio.StopEmitter(Hum, _source);
        _audio.StopEmitter(Bird, _source);
    }
    #endregion

    #region Input
    // =========================================================
    // F9 checks UI tone, left/right spatial panning and a repeating positional hum.
    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (!Enabled || _audio == null || input is not InputEventKey key ||
            !key.Pressed || key.Echo || key.Keycode != PreviewKey) return;
        Camera2D camera = GetViewport().GetCamera2D();
        Vector2 center = camera?.GetScreenCenterPosition() ?? Vector2.Zero;
        switch (Scenario)
        {
            case AudioTestScenario.PriorityBurst:
                for (int i = 0; i < 36; i++)
                    _audio.PlayAt(World, center + new Vector2((i % 9 - 4) * 90f, 0f));
                break;
            case AudioTestScenario.Intermittent:
                if (camera == null) break;
                _source.GlobalPosition = center + new Vector2(180f, 0);
                _intermittentOn = !_intermittentOn;
                if (_intermittentOn) _audio.StartEmitter(Bird, _source);
                else _audio.StopEmitter(Bird, _source);
                break;
            case AudioTestScenario.VoiceQueue:
                _audio.GetNode<VoiceAssistant>("VoiceAssistant").Say(Voice);
                break;
            default:
                switch (_step++ % 4)
                {
                    case 0: _audio.Play(Click); break;
                    case 1: _audio.PlayAt(World, center + new Vector2(-240f, 0f)); break;
                    case 2: _audio.PlayAt(World, center + new Vector2(240f, 0f)); break;
                    case 3:
                        if (camera == null) break;
                        _source.GlobalPosition = center + new Vector2(100f, 0f);
                        _loopOn = !_loopOn;
                        if (_loopOn) _audio.StartEmitter(Hum, _source);
                        else _audio.StopEmitter(Hum, _source);
                        break;
                }
                break;
        }
        GD.Print("AudioTester: ", Scenario, " / key step ", _step);
        GetViewport().SetInputAsHandled();
    }
    #endregion

    #region Synthetic clips
    // =========================================================
    // Temporary 16-bit mono PCM lets you verify audio before importing real clips.
    private void Register(string id, float frequency, GameAudioMode mode, GameAudioCategory category, bool spatial, int priority)
    {
        _audio.RegisterSound(new AudioDefinition
        {
            Id = id, Stream = Tone(frequency, mode == GameAudioMode.Loop),
            Mode = mode, Category = category, Positional = spatial,
            MaxDistance = 850f, Priority = priority, MaximumInstances = 40,
            IntervalMinSeconds = 2f, IntervalMaxSeconds = 4f,
            FadeInSeconds = 0.25f, FadeOutSeconds = 0.3f
        });
    }

    // =========================================================
    // Loop points are explicitly encoded; production sound imports need their own loop flags.
    private static AudioStreamWav Tone(float frequency, bool loop)
    {
        const int rate = 22050;
        int samples = loop ? rate : rate / 3;
        byte[] pcm = new byte[samples * 2];
        for (int i = 0; i < samples; i++)
        {
            double envelope = loop ? 1d : Math.Min(1d, Math.Min(i / 200d, (samples - i) / 300d));
            short value = (short)(Math.Sin(i * Math.PI * 2d * frequency / rate) * 6200d * envelope);
            pcm[i * 2] = (byte)(value & 255);
            pcm[i * 2 + 1] = (byte)((value >> 8) & 255);
        }
        return new AudioStreamWav
        {
            Data = pcm, Format = (AudioStreamWav.FormatEnum)1, MixRate = rate,
            Stereo = false, LoopMode = (AudioStreamWav.LoopModeEnum)(loop ? 1 : 0),
            LoopBegin = 0, LoopEnd = samples
        };
    }
    #endregion
#endif
}
