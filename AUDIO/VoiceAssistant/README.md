## First real voice cue: profile greeting (2026-10-20; engine playback pending)

The uploaded `AUDIO/VoiceAssistant/Welcome_Commander.mp3` is now registered in `AUDIO/DefaultAudioCatalog.tres` through [Cues/Profiles/WelcomeCommander.tres](Cues/Profiles/WelcomeCommander.tres), ID `voice_profile_welcome`. `UI/Menus/Boot/BootScreen.cs` requests it through the existing persistent `VoiceAssistant.Say()` queue **after successful selection of a saved profile or creation of a new profile**, just before Main Menu opens. The greeting is a non-positional `Voice` one-shot on `CompanionVoice` bus; mixer ducking, limits, and completion remain the shared system's responsibility. Missing sound never prevents profile selection. Godot import/playback has not been tested here.

### Recommended organization for all future assistant speech

```text
AUDIO/VoiceAssistant/
├── VoiceAssistant.cs                # Shared priority-based playback; no per-category players
├── README.md
├── Welcome_Commander.mp3            # Current uploaded clip (temporary location)
└── Cues/
    ├── Profiles/
    │   ├── README.md
    │   ├── WelcomeCommander.tres    # Current AudioDefinition; references root MP3
    │   └── Welcome_Commander.mp3    # FUTURE desired clip location, not present yet
    ├── Waves/                       # FUTURE: WaveStart01, HeavyWave, BossIncoming etc.
    ├── Combat/                      # FUTURE: CitadelCritical, TowerDestroyed, NoAmmo
    ├── Systems/                     # FUTURE: ConstructionComplete, ResearchUnlocked
    └── Missions/                    # FUTURE: MissionVictory, MissionDefeat
```

**One clip and one `.tres` definition per line**, kept in the same subfolder once assets are moved. IDs should be meaningful, stable and independent of file location, e.g. `voice_profile_welcome`, `voice_wave_heavy`, `voice_citadel_critical`. `AUDIO/DefaultAudioCatalog.tres` holds references, not duplicated audio. No sound event-specific logic goes into `VoiceAssistant.cs`; the menu, wave presenter or combat event sends the ID. Voice cue priority controls interruption (profile welcome medium/50; critical emergency future 90+), with existing voice queue and optional music ducking.

Wave narrator `WavePresentation.VoiceCueId` already exists as a reserved field, but **no automatic audio on wave start yet**. Adding clips in `Cues/Waves/` and routing events later is a separate task. Maintain separate UI/Narration text, optional subtitle synchronization, and central voice audio.
 
---

# Voice Assistant — Tactical Announcement Queue

`VoiceAssistant.cs` is a child of the persistent `/root/GameAudio` manager. It adapts the original isometric-2D `CompanionVoice` approach for spoken tactical warnings in tower defense.

Voice cues are standard `AudioDefinition` Resources placed here in `Cues/` when real audio exists. Set `Category = Voice`, `Mode = OneShot` and `Positional = false`; give each an ID, assigned clip and appropriate priority, then add to `AUDIO/DefaultAudioCatalog.tres`. **The profile greeting is now registered; additional cues are optional.**

Use `AudioManager.Find(this)?.GetNode<VoiceAssistant>("VoiceAssistant")?.Say("voice_citadel_critical")` from a future event handler. Repeated identical cues do not queue; higher-priority cues can interrupt lower-priority speech. A bounded priority queue advances when the manager announces clip completion. Music volume is temporarily ducked during playback according to `DefaultAudioConfig.tres`.

Voice triggers, generated/TTS speech, captions, subtitles and on-screen alerts are intentionally **not** implemented here. Game systems/notifications decide *what happened*, UI decides *what to display*, and this component schedules an existing audio cue.
