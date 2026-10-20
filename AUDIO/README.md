## First imported voice cue: Welcome Commander (2026-10-20; engine QA pending)

Registered the uploaded `VoiceAssistant/Welcome_Commander.mp3` with the shared audio catalog as `voice_profile_welcome`. Boot selection and successful new-profile creation trigger `VoiceAssistant.Say()` through the persistent GameAudio manager; the cue plays during transition into Main Menu. See [VoiceAssistant organization](VoiceAssistant/README.md) for the planned `Cues/Profiles`, `Waves`, `Combat`, `Systems`, `Missions` grouping. No redundant audio manager, profile save change or wave narration/audio hook was added. Godot MP3 import and audible playback require local verification.

---

# AUDIO — Shared Playback, Mixing and Voice Foundation

**Foundation implemented (October 2026). The first recorded voice cue is now registered and requested on valid profile entry; other sound events remain unconnected.** This is the shared audio service, adapted from `ben0kay/isometric-new-2d/AUDIO/` for a camera-controlled, many-tower space defense game. `AUDIO/` owns sound playback and policies; each gameplay feature owns **its own clips and per-sound .tres definitions**. See `NOTES/CodePreferences.md`.

## Folder ownership

| File/folder | Purpose |
| --- | --- |
| `AudioManager.cs / AudioManager.tscn` | Autoloaded central audio authority; pooled playback, one-shots, looping/intermittent emitters, priorities, distance/screen culling and per-owner cooldowns. |
| `AudioMasterConfig.cs / DefaultAudioConfig.tres` | Global Inspector-editable mixer volumes, voice ducking, budgets, culling, scan interval and pause behavior. |
| `AudioDefinition.cs` | Per-sound Inspector Resource: stable ID, optional AudioStream, category, mode, priority, polyphony, cooldown, positional rules, volume/pitch and fades. |
| `AudioCatalog.cs / DefaultAudioCatalog.tres` | Central list of references to definitions **located in feature folders**, indexed by ID on startup. Currently contains the profile greeting definition. |
| `AudioBuses.tres` | Godot native Master, Music, Ambience, WorldSFX, UI and CompanionVoice buses. |
| `VoiceAssistant/` | Priority voice queue and interruptible speech playback; no prerecorded lines yet. |
| `Debug/` | DEBUG-only F9 synthetic sound tester; no WAV/OGG needed. |
| `Music/`, `Ambience/` | Global music and ambient tracks owned by AUDIO, separate from per-feature sounds. |

**Keep sound assets beside their owner**, for example:
- `TOWERS/Offensive/BattleTower/Sounds/Fire.wav` and `Fire.tres`
- `ENEMIES/Types/Enemy001/Sounds/EngineLoop.ogg`
- `WORLD/PORTALS/EnemyPortal/Sounds/PortalHum.ogg`
- `UI/Menus/Main/Sounds/ButtonClick.wav`

Do **not** create duplicate Tower/Enemy audio managers, a second `AUDIO/Sounds/Towers` collection, or an extra economy/player save system. `AUDIO/` can own shared impact/explosion clips only if genuinely shared by multiple features.

## Scene lifecycle and Inspector settings

`project.godot` autoloads `AUDIO/AudioManager.tscn` as `/root/GameAudio`. It is available in Boot, Main Menu, Campaign and gameplay, and remains alive between menu/level scene changes. You do **not** instance another copy in `Main.tscn` or `WORLD/World.tscn`.

The current gameplay `CameraRig/Camera2D` is the **positional listener** (there is no player character). The manager attaches a `WorldAudioListener` to the current Camera2D when present, including after scene transitions. Menus without a world camera can still play UI, music and voice audio. Positional sounds are not played when no world camera is active.

Open `AUDIO/DefaultAudioConfig.tres` in the Inspector to adjust global levels and limits; open `AUDIO/AudioBuses.tres` or Godot's Audio dock for bus routing/effects; open `AUDIO/DefaultAudioCatalog.tres` to register sounds. `AudioManager.ApplyBusVolumes()` reapplies config values if changed during a run; player preferences are **not persisted yet**.

## Sound playback API

Add a new `AudioDefinition` Resource beside the owning clip and register that Resource in the `Sounds` array of `DefaultAudioCatalog.tres`. Each `Id` must be unique and stable. Reference it from the gameplay owner via an optional exported sound ID or audio profile in a future integration pass.

```csharp
AudioManager audio = AudioManager.Find(this);
audio?.Play("ui_build_confirm");                     // Non-positional one-shot
audio?.PlayAt("enemy_explosion", GlobalPosition);     // Positional one-shot
audio?.PlayFrom("tower_battle_fire", this);           // Positional; per-tower cooldown

audio?.StartEmitter("tower_generator_hum", this);    // Positional Loop/Intermittent
audio?.StopEmitter("tower_generator_hum", this);     // Explicitly turn it off
audio?.StartEmitter("menu_theme");                    // Non-positional global Loop
audio?.StopEmitter("menu_theme");

audio?.GetNode<VoiceAssistant>("VoiceAssistant")?.Say("voice_wave_incoming");
```

These IDs are **examples**, not currently registered audio. Missing IDs or unset audio streams safely return `false`, not a dummy placeholder or sound. **Nothing makes production sound until clips are imported and gameplay event hooks are added.**

### Rules

- **OneShot:** played on demand, returns to the audio pool on completion. `Play` only accepts non-positional definitions; `PlayAt` and `PlayFrom` accept positional definitions.
- **Loop:** register once on activation, unregister once on deactivation; the manager pools players and stops distant sounds while retaining the source registration. Loops **must actually be marked looped** in Godot's WAV/OGG import settings.
- **Intermittent:** register a source once; play discrete one-shots after randomized configured intervals. No permanently running stream between calls.
- **Owner identity:** `PlayFrom` cooldown is per (sound ID, owner instance), so many towers with the same firing clip don't incorrectly silence one another. `PlayAt` and `Play` use one shared per-ID cooldown. Global per-ID polyphony and category budgets prevent audio spam. Excess requests may be rejected, or higher-priority requests may replace lower-priority playback.
- **Distance and screen rules:** use Godot's native `AudioStreamPlayer2D` pan/attenuation, center on the active camera. Distance uses world pixels and `MaxDistance` per definition; an optional per-sound offscreen rule checks the viewport and margin. Pan/zoom do not trigger extra sound events.
- **Owner lifetime:** loop/intermittent registrations hold weak references to `Node2D` owners. Deleted towers/enemies clean up on the next periodic scan. No per-frame lookup in tower code.
- **Pausing:** WorldSFX and Ambience playback pause with the game (if enabled). UI/Voice/Music continue. Autoload and its pooled players process during pause.
- **Voice:** non-positional one-shots on the CompanionVoice bus, queued by priority (higher priority can interrupt), with temporary music ducking. Speech generation, subtitles, notifications and event triggers are not yet implemented.
- **Catalog edits:** audio IDs are indexed at scene start. Restart a running scene after editing the saved catalog. For synthetic tests, `RegisterSound` adds an **in-memory** definition only.

## Testing before importing any clips

Build in Godot .NET and use **F5** (Boot/menus) or **F6** with `Main.tscn` (battlefield). In a DEBUG build, `AudioTester` is loaded under the persistent AudioManager. Press **F9** to cycle a non-positional test beep, left/right positional beeps, and toggle a world hum (the latter 3 need the battlefield camera). Set the `AudioTester` node's `Scenario` in `AudioManager.tscn` to test a burst of 36 requests, intermittent spatial beeps, or priority voice playback. These test tones are synthesized in memory and are not game assets. The tester is inactive outside DEBUG builds.

If the volume is muted, check the Audio dock and OS output, plus the `DefaultAudioConfig.tres` mixer values. Real imported clips are intentionally absent.

## Suggested next stages (not implemented)

1. Add actual clips/definitions within their owning `TOWERS/.../Sounds`, `ENEMIES/.../Sounds`, `UI/.../Sounds` folders and register them in the catalog. Keep paths/IDs stable.
2. Bind actual events (tower firing, enemy hits/deaths, upgrade/build actions, portal activation, UI controls) to audio requests. Avoid calling `StartEmitter` or `Play` unconditionally in `_Process`.
3. Add menu music, ambient backgrounds and real voice assistant clips, with an optional UI subtitle/announcement contract.
4. Bind profile-level audio preferences via `SYSTEMS/Profiles`; saving settings is a different responsibility from producing sound. Profile persistence is not changed by this foundation.
5. With hundreds of active towers/enemies, benchmark voice budgets, culling and pooled audio before adding more features.

## Boundaries and verification

Only `AUDIO/` and `project.godot` are changed in this foundation. Existing towers, enemy combat, menus, HUD, wave timing, map navigation and profile saves retain ownership of their own rules. No gameplay audio events are wired in this pass.

This change was checked against source files and Godot's native audio API, but requires a local Godot .NET build and in-engine playback test to confirm compilation/audio device behavior.
