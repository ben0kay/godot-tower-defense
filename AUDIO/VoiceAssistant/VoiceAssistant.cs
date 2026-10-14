// Queued non-positional tactical voice cues; subtitles/gameplay alerts remain UI responsibilities.
using Godot;
using System.Collections.Generic;

public partial class VoiceAssistant : Node
{
    #region Playback state
    private AudioManager _audio;
    private string _currentId = "";
    private int _currentPriority;
    private readonly List<AudioDefinition> _waiting = new();
    #endregion

    #region Lifecycle
    // =========================================================
    // Reuse the shared manager's voice channel and completion event.
    public override void _Ready()
    {
        _audio = GetParent<AudioManager>();
        _audio.SoundFinished += OnSoundFinished;
    }

    // =========================================================
    // Avoid callbacks into a freed scene.
    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_audio)) _audio.SoundFinished -= OnSoundFinished;
    }
    #endregion

    #region Queue
    // =========================================================
    // One voice at a time; higher-priority emergency lines can interrupt lesser ones.
    public bool Say(string cueId)
    {
        AudioDefinition sound = _audio?.FindSound(cueId);
        if (sound?.Stream == null || sound.Category != GameAudioCategory.Voice ||
            sound.Mode != GameAudioMode.OneShot || sound.Positional) return false;
        if (_currentId == cueId || _waiting.Exists(x => x.Id == cueId)) return false;
        if (_currentId.Length == 0) return Begin(sound);
        if (sound.Priority > _currentPriority)
        {
            string interrupted = _currentId;
            _currentId = ""; _currentPriority = 0;
            _audio.StopOneShot(interrupted);
            return Begin(sound);
        }
        int cap = Mathf.Max(0, _audio.Config.VoiceQueueLimit);
        if (cap == 0) return false;
        if (_waiting.Count >= cap)
        {
            if (sound.Priority <= _waiting[_waiting.Count - 1].Priority) return false;
            _waiting.RemoveAt(_waiting.Count - 1);
        }
        int index = _waiting.FindIndex(x => x.Priority < sound.Priority);
        if (index < 0) _waiting.Add(sound);
        else _waiting.Insert(index, sound);
        return true;
    }

    // =========================================================
    // Advance only after the audio manager has successfully begun the voice line.
    private bool Begin(AudioDefinition sound)
    {
        if (!_audio.Play(sound.Id)) return false;
        _currentId = sound.Id; _currentPriority = sound.Priority;
        return true;
    }

    // =========================================================
    // Ignore unrelated sound completions and skip any now-unplayable queued cues.
    private void OnSoundFinished(string id)
    {
        if (_currentId.Length == 0 || id != _currentId) return;
        _currentId = ""; _currentPriority = 0;
        while (_waiting.Count > 0)
        {
            AudioDefinition next = _waiting[0];
            _waiting.RemoveAt(0);
            if (Begin(next)) break;
        }
    }
    #endregion
}
