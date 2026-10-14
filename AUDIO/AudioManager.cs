// Persistent game audio authority: pooled 2D/flat playback, camera listening, priorities and owned emitters.
// Gameplay decides WHEN a sound is requested; this manager decides HOW and whether it is played.
using Godot;
using System;
using System.Collections.Generic;

public partial class AudioManager : Node2D
{
    #region Inspector and public API
    [ExportGroup("Resources")]
    [Export] public AudioMasterConfig Config { get; set; }
    [Export] public AudioCatalog Catalog { get; set; }
    public static AudioManager Instance { get; private set; }
    public event Action<string> SoundFinished;
    #endregion

    #region Runtime
    private sealed class Slot
    {
        public bool Spatial, Active, Loop, Stopping;
        public string Id = "", EmitterKey = "";
        public GameAudioCategory Category;
        public int Priority;
        public AudioStreamPlayer Flat;
        public AudioStreamPlayer2D World;
        public Tween Fade;
        public Node Player => Spatial ? World : Flat;
    }

    private sealed class Emitter
    {
        public string Key;
        public AudioDefinition Sound;
        public WeakReference<Node2D> Owner;
        public Slot Slot;
        public double NextPlay;
    }

    private readonly Dictionary<string, AudioDefinition> _sounds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Emitter> _emitters = new(StringComparer.Ordinal);
    private readonly Dictionary<(string, ulong), double> _lastPlay = new();
    private readonly List<string> _expiredKeys = new();
    private readonly List<(string, ulong)> _expiredCooldowns = new();
    private readonly List<Slot> _slots = new();
    private readonly RandomNumberGenerator _rng = new();
    private Camera2D _camera;
    private double _clock, _scanElapsed;
    private bool _paused;
    #endregion

    #region Lifecycle
    // =========================================================
    // Autoload stays alive through Boot/Main/Campaign/gameplay transitions.
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Instance = this;
        Config ??= GD.Load<AudioMasterConfig>("res://AUDIO/DefaultAudioConfig.tres") ?? new();
        Catalog ??= GD.Load<AudioCatalog>("res://AUDIO/DefaultAudioCatalog.tres") ?? new();
        _rng.Randomize();
        foreach (AudioDefinition sound in Catalog.Sounds) RegisterSound(sound);
        ApplyBusVolumes();
        RefreshCamera();
    }

    // =========================================================
    // Maintenance is periodic; there is no audio-specific per-tower physics loop.
    public override void _Process(double delta)
    {
        double elapsed = Math.Max(0d, delta);
        _clock += elapsed;
        bool pauseWorld = GetTree().Paused && Config.PauseWorldAudioWithGame;
        if (pauseWorld != _paused)
        {
            _paused = pauseWorld;
            foreach (Slot slot in _slots)
                if (slot.Active && (slot.Category == GameAudioCategory.WorldSfx || slot.Category == GameAudioCategory.Ambience))
                    SetPaused(slot, _paused);
        }
        _scanElapsed += elapsed;
        if (_scanElapsed < Mathf.Max(0.05f, Config.EmitterCheckSeconds)) return;
        _scanElapsed = 0d;
        RefreshCamera();
        if (!_paused) ScanEmitters();
        if (_lastPlay.Count > 0) PruneCooldowns();
    }

    // =========================================================
    // Leave mixer and static instance clean when the application shuts down.
    public override void _ExitTree()
    {
        foreach (Slot slot in _slots)
            if (slot.Active) Release(slot, false);
        _emitters.Clear();
        SetMusicDucking(false);
        if (Instance == this) Instance = null;
    }
    #endregion

    #region Registry and lookup
    // =========================================================
    // Find the single application-level audio authority from any menu or game Node.
    public static AudioManager Find(Node context) =>
        context != null && context.IsInsideTree() && GodotObject.IsInstanceValid(Instance) ? Instance : null;

    // =========================================================
    // Fast stable-ID lookup. Debug tests may register transient sounds without changing .tres assets.
    public bool RegisterSound(AudioDefinition sound)
    {
        if (sound == null || string.IsNullOrWhiteSpace(sound.Id) || sound.Id != sound.Id.Trim() ||
            _sounds.ContainsKey(sound.Id))
        {
            GD.PushWarning("Audio: ignored invalid or duplicate sound definition.");
            return false;
        }
        _sounds.Add(sound.Id, sound);
        return true;
    }

    // =========================================================
    // Missing definitions and unassigned streams are expected before audio assets exist.
    public AudioDefinition FindSound(string id) =>
        id != null && _sounds.TryGetValue(id, out AudioDefinition sound) ? sound : null;
    #endregion

    #region One-shot sounds
    // =========================================================
    // UI, alerts and non-spatial stingers.
    public bool Play(string id)
    {
        AudioDefinition sound = FindSound(id);
        return sound != null && !sound.Positional && PlayOneShot(sound, Vector2.Zero, false, 0UL);
    }

    // =========================================================
    // Static impact, explosion or other point in world coordinates.
    public bool PlayAt(string id, Vector2 position)
    {
        AudioDefinition sound = FindSound(id);
        return sound != null && sound.Positional && PlayOneShot(sound, position, true, 0UL);
    }

    // =========================================================
    // Moving or repeated source; owner-specific cooldown avoids suppressing other towers.
    public bool PlayFrom(string id, Node2D owner)
    {
        if (!GodotObject.IsInstanceValid(owner) || !owner.IsInsideTree()) return false;
        AudioDefinition sound = FindSound(id);
        return sound != null && sound.Positional &&
            PlayOneShot(sound, owner.GlobalPosition, true, owner.GetInstanceId());
    }

    // =========================================================
    // Enforce culling, per-owner cooldown, per-sound overlap and category budgets.
    private bool PlayOneShot(AudioDefinition sound, Vector2 position, bool positioned, ulong sourceId)
    {
        if (sound.Stream == null || sound.Mode == GameAudioMode.Loop || (sound.Positional && (!positioned || !IsAudible(sound, position)))) return false;
        var key = (sound.Id, sourceId);
        if (_lastPlay.TryGetValue(key, out double previous) &&
            _clock - previous < Mathf.Max(0f, sound.CooldownSeconds)) return false;

        Slot slot = Acquire(sound, false);
        if (slot == null) return false;
        Begin(slot, sound, position, false);
        _lastPlay[key] = _clock;
        return true;
    }

    // =========================================================
    // Explicit interruption used by higher-priority voice cues.
    public bool StopOneShot(string id)
    {
        foreach (Slot slot in _slots)
            if (slot.Active && !slot.Loop && slot.Id == id)
            { Release(slot); return true; }
        return false;
    }
    #endregion

    #region Loops and intermittent emitters
    // =========================================================
    // Register one repeating sound per owner and ID; call on state changes, NOT every frame.
    public bool StartEmitter(string id, Node2D owner = null)
    {
        AudioDefinition sound = FindSound(id);
        if (sound?.Stream == null || sound.Mode == GameAudioMode.OneShot) return false;
        if (sound.Positional && (!GodotObject.IsInstanceValid(owner) || !owner.IsInsideTree())) return false;
        string key = EmitterKey(id, owner);
        if (_emitters.ContainsKey(key) || _emitters.Count >= Mathf.Max(1, Config.MaxRegisteredEmitters)) return false;
        Emitter emitter = new()
        {
            Key = key, Sound = sound,
            Owner = owner == null ? null : new WeakReference<Node2D>(owner),
            NextPlay = _clock
        };
        _emitters.Add(key, emitter);
        if (!_paused) RefreshEmitter(emitter);
        return true;
    }

    // =========================================================
    // Explicitly remove a sound source when a tower or machine switches off.
    public bool StopEmitter(string id, Node2D owner = null)
    {
        string key = EmitterKey(id, owner);
        if (!_emitters.Remove(key, out Emitter emitter)) return false;
        if (emitter.Slot != null) FadeOut(emitter.Slot, emitter.Sound.FadeOutSeconds);
        return true;
    }

    // =========================================================
    // Scan registered emitters at a configured interval, not per enemy/tower tick.
    private void ScanEmitters()
    {
        _expiredKeys.Clear();
        foreach (Emitter emitter in _emitters.Values)
        {
            if (!RefreshEmitter(emitter)) _expiredKeys.Add(emitter.Key);
        }
        foreach (string key in _expiredKeys) _emitters.Remove(key);
    }

    // =========================================================
    // Invalid weak owners expire; audible loops acquire pooled players only while needed.
    private bool RefreshEmitter(Emitter emitter)
    {
        Node2D owner = null;
        if (emitter.Owner != null &&
            (!emitter.Owner.TryGetTarget(out owner) || !GodotObject.IsInstanceValid(owner) ||
             !owner.IsInsideTree() || owner.IsQueuedForDeletion()))
        {
            if (emitter.Slot != null) FadeOut(emitter.Slot, emitter.Sound.FadeOutSeconds);
            return false;
        }

        AudioDefinition sound = emitter.Sound;
        Vector2 position = owner?.GlobalPosition ?? Vector2.Zero;
        bool audible = !sound.Positional || IsAudible(sound, position,
            emitter.Slot == null ? 0f : Config.LoopResumeMarginPixels);
        if (!audible)
        {
            if (emitter.Slot != null)
            {
                FadeOut(emitter.Slot, sound.FadeOutSeconds);
                emitter.Slot = null;
            }
            return true;
        }

        if (sound.Mode == GameAudioMode.Intermittent)
        {
            if (_clock >= emitter.NextPlay)
            {
                PlayOneShot(sound, position, owner != null, owner?.GetInstanceId() ?? 0UL);
                float min = Mathf.Max(0.1f, sound.IntervalMinSeconds);
                emitter.NextPlay = _clock + _rng.RandfRange(min, Mathf.Max(min, sound.IntervalMaxSeconds));
            }
            return true;
        }

        if (emitter.Slot == null)
        {
            Slot slot = Acquire(sound, true);
            if (slot == null) return true;
            emitter.Slot = slot;
            slot.EmitterKey = emitter.Key;
            Begin(slot, sound, position, true);
            FadeIn(slot, sound.FadeInSeconds);
        }
        else if (emitter.Slot.World != null) emitter.Slot.World.GlobalPosition = position;
        return true;
    }

    private static string EmitterKey(string id, Node2D owner) =>
        (owner == null ? "global" : owner.GetInstanceId().ToString()) + ":" + id;
    #endregion

    #region Pooling and playback
    // =========================================================
    // A higher-priority sound can replace a lower-priority sound within its own budget.
    private Slot Acquire(AudioDefinition sound, bool loop)
    {
        int sameId = 0, sameCategory = 0, cap = CategoryBudget(sound.Category, loop);
        Slot weakest = null;
        foreach (Slot slot in _slots)
        {
            if (!slot.Active) continue;
            if (slot.Id == sound.Id) sameId++;
            if (slot.Category != sound.Category || slot.Loop != loop) continue;
            sameCategory++;
            if (weakest == null || slot.Priority < weakest.Priority) weakest = slot;
        }
        if (sameId >= Mathf.Max(1, sound.MaximumInstances)) return null;
        if (sameCategory >= cap)
        {
            if (weakest == null || weakest.Priority >= sound.Priority) return null;
            Release(weakest);
        }

        foreach (Slot slot in _slots)
            if (!slot.Active && slot.Spatial == sound.Positional) return slot;

        Slot created = new() { Spatial = sound.Positional };
        if (created.Spatial)
        {
            created.World = new AudioStreamPlayer2D { Name = "WorldSound", ProcessMode = ProcessModeEnum.Always };
            AddChild(created.World);
            created.World.Finished += () => OnFinished(created);
        }
        else
        {
            created.Flat = new AudioStreamPlayer { Name = "FlatSound", ProcessMode = ProcessModeEnum.Always };
            AddChild(created.Flat);
            created.Flat.Finished += () => OnFinished(created);
        }
        _slots.Add(created);
        return created;
    }

    // =========================================================
    // Configure and start a reused native Godot AudioStreamPlayer or AudioStreamPlayer2D.
    private void Begin(Slot slot, AudioDefinition sound, Vector2 position, bool loop)
    {
        StopFade(slot);
        slot.Active = true; slot.Stopping = false; slot.Loop = loop;
        slot.Id = sound.Id; slot.Category = sound.Category; slot.Priority = sound.Priority;
        string bus = BusName(sound.Category);
        if (slot.World != null)
        {
            slot.World.Stream = sound.Stream; slot.World.Bus = bus;
            slot.World.VolumeDb = sound.VolumeDb; slot.World.PitchScale = Mathf.Max(0.1f, sound.PitchScale);
            slot.World.MaxDistance = Mathf.Max(1f, sound.MaxDistance);
            slot.World.Attenuation = Mathf.Max(0.1f, sound.Attenuation);
            slot.World.GlobalPosition = position; slot.World.StreamPaused = false; slot.World.Play();
        }
        else
        {
            slot.Flat.Stream = sound.Stream; slot.Flat.Bus = bus;
            slot.Flat.VolumeDb = sound.VolumeDb; slot.Flat.PitchScale = Mathf.Max(0.1f, sound.PitchScale);
            slot.Flat.StreamPaused = false; slot.Flat.Play();
        }
        if (_paused && (sound.Category == GameAudioCategory.WorldSfx || sound.Category == GameAudioCategory.Ambience))
            SetPaused(slot, true);
        if (sound.Category == GameAudioCategory.Voice) RefreshVoiceDucking();
    }

    // =========================================================
    // Return the player to an idle slot and notify the voice queue if applicable.
    private void Release(Slot slot, bool notify = true)
    {
        if (!slot.Active) return;
        StopFade(slot);
        string id = slot.Id, emitterKey = slot.EmitterKey;
        GameAudioCategory category = slot.Category;
        slot.Active = false; slot.Loop = false; slot.Stopping = false;
        slot.Id = ""; slot.EmitterKey = "";
        if (slot.World != null)
        {
            slot.World.Stop(); slot.World.StreamPaused = false; slot.World.Stream = null;
        }
        else
        {
            slot.Flat.Stop(); slot.Flat.StreamPaused = false; slot.Flat.Stream = null;
        }
        if (emitterKey.Length > 0 && _emitters.TryGetValue(emitterKey, out Emitter emitter) && emitter.Slot == slot)
            emitter.Slot = null;
        if (category == GameAudioCategory.Voice) RefreshVoiceDucking();
        if (notify) SoundFinished?.Invoke(id);
    }

    private void OnFinished(Slot slot)
    {
        if (slot.Active && !slot.Loop && !slot.Stopping) Release(slot);
    }

    // =========================================================
    // Use independent caps; music stays exclusive and voices never consume world SFX slots.
    private int CategoryBudget(GameAudioCategory category, bool loop) => Mathf.Max(1, category switch
    {
        GameAudioCategory.Music => 1,
        GameAudioCategory.Voice => Config.MaxVoiceOneShots,
        GameAudioCategory.UI => Config.MaxUiOneShots,
        GameAudioCategory.Ambience => loop ? Config.MaxAmbienceLoops : Config.MaxWorldOneShots,
        _ => loop ? Config.MaxWorldLoops : Config.MaxWorldOneShots
    });

    // =========================================================
    // Fade optional registered loops; do not stop a recently allocated slot twice.
    private void FadeIn(Slot slot, float seconds)
    {
        if (seconds <= 0f) return;
        float target = slot.World != null ? slot.World.VolumeDb : slot.Flat.VolumeDb;
        if (slot.World != null) slot.World.VolumeDb = -60f;
        else slot.Flat.VolumeDb = -60f;
        slot.Fade = slot.Player.CreateTween();
        slot.Fade.TweenProperty(slot.Player, "volume_db", target, seconds);
    }

    private void FadeOut(Slot slot, float seconds)
    {
        if (!slot.Active || slot.Stopping) return;
        StopFade(slot);
        if (seconds <= 0f) { Release(slot); return; }
        slot.Stopping = true;
        slot.Fade = slot.Player.CreateTween();
        slot.Fade.TweenProperty(slot.Player, "volume_db", -60f, seconds);
        slot.Fade.TweenCallback(Callable.From(() => Release(slot)));
    }

    private static void StopFade(Slot slot)
    {
        if (slot.Fade != null && slot.Fade.IsValid()) slot.Fade.Kill();
        slot.Fade = null;
    }

    private static void SetPaused(Slot slot, bool paused)
    {
        if (slot.World != null) slot.World.StreamPaused = paused;
        else slot.Flat.StreamPaused = paused;
    }
    #endregion

    #region Camera and culling
    // =========================================================
    // The tower defense has no player actor: the active Camera2D is the audio listener.
    private void RefreshCamera()
    {
        Camera2D current = GetViewport().GetCamera2D();
        if (current == _camera && GodotObject.IsInstanceValid(_camera)) return;
        _camera = current;
        if (!GodotObject.IsInstanceValid(_camera)) return;
        AudioListener2D listener = _camera.GetNodeOrNull<AudioListener2D>("WorldAudioListener");
        if (listener == null)
        {
            listener = new AudioListener2D { Name = "WorldAudioListener" };
            _camera.AddChild(listener);
        }
        listener.MakeCurrent();
    }

    // =========================================================
    // Camera zoom/pan feed native distance attenuation; optionally reject offscreen sounds.
    private bool IsAudible(AudioDefinition sound, Vector2 position, float margin = 0f)
    {
        if (!sound.Positional) return true;
        if (!GodotObject.IsInstanceValid(_camera)) RefreshCamera();
        if (!GodotObject.IsInstanceValid(_camera)) return false;
        if (Config.DistanceCulling &&
            position.DistanceSquaredTo(_camera.GetScreenCenterPosition()) >
            Mathf.Pow(Mathf.Max(1f, sound.MaxDistance) + Mathf.Max(0f, margin), 2f)) return false;
        if (Config.ScreenCulling && !sound.AllowOffscreen)
        {
            Vector2 screenPoint = GetViewport().GetCanvasTransform() * position;
            Rect2 screen = GetViewport().GetVisibleRect().Grow(Mathf.Max(0f, Config.ScreenMarginPixels));
            if (!screen.HasPoint(screenPoint)) return false;
        }
        return true;
    }

    // =========================================================
    // Bound long-running cooldown bookkeeping even when thousands of short-lived enemies spawn.
    private void PruneCooldowns()
    {
        _expiredCooldowns.Clear();
        foreach (var pair in _lastPlay)
            if (_clock - pair.Value > 30d) _expiredCooldowns.Add(pair.Key);
        foreach (var key in _expiredCooldowns) _lastPlay.Remove(key);
    }
    #endregion

    #region Mixer
    // =========================================================
    // Mixer buses are editable in the Audio dock; call after editing live volume options.
    public void ApplyBusVolumes()
    {
        if (Config == null) return;
        SetBus("Master", Config.MasterDb);
        SetBus("Music", Config.MusicDb);
        SetBus("Ambience", Config.AmbienceDb);
        SetBus("WorldSFX", Config.WorldSfxDb);
        SetBus("UI", Config.UiDb);
        SetBus("CompanionVoice", Config.VoiceDb);
        RefreshVoiceDucking();
    }

    // =========================================================
    // Duck only music while speech is active; authored bus volume remains unchanged.
    private void RefreshVoiceDucking()
    {
        bool active = false;
        foreach (Slot slot in _slots)
            if (slot.Active && slot.Category == GameAudioCategory.Voice) { active = true; break; }
        SetMusicDucking(active);
    }

    private void SetMusicDucking(bool active)
    {
        if (Config == null) return;
        float offset = active && Config.DuckMusicDuringVoice ? Config.VoiceMusicDuckDb : 0f;
        SetBus("Music", Config.MusicDb + offset);
    }

    private static void SetBus(string name, float volumeDb)
    {
        int index = AudioServer.GetBusIndex(name);
        if (index >= 0) AudioServer.SetBusVolumeDb(index, volumeDb);
    }

    private static string BusName(GameAudioCategory category) => category switch
    {
        GameAudioCategory.Music => "Music",
        GameAudioCategory.Ambience => "Ambience",
        GameAudioCategory.UI => "UI",
        GameAudioCategory.Voice => "CompanionVoice",
        _ => "WorldSFX"
    };
    #endregion
}
