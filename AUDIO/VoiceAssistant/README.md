# Voice Assistant — Tactical Announcement Queue

`VoiceAssistant.cs` is a child of the persistent `/root/GameAudio` manager. It adapts the original isometric-2D `CompanionVoice` approach for spoken tactical warnings in tower defense.

Voice cues are standard `AudioDefinition` Resources placed here in `Cues/` when real audio exists. Set `Category = Voice`, `Mode = OneShot` and `Positional = false`; give each an ID, assigned clip and appropriate priority, then add to `AUDIO/DefaultAudioCatalog.tres`. **No cues are registered yet.**

Use `AudioManager.Find(this)?.GetNode<VoiceAssistant>("VoiceAssistant")?.Say("voice_citadel_critical")` from a future event handler. Repeated identical cues do not queue; higher-priority cues can interrupt lower-priority speech. A bounded priority queue advances when the manager announces clip completion. Music volume is temporarily ducked during playback according to `DefaultAudioConfig.tres`.

Voice triggers, generated/TTS speech, captions, subtitles and on-screen alerts are intentionally **not** implemented here. Game systems/notifications decide *what happened*, UI decides *what to display*, and this component schedules an existing audio cue.
