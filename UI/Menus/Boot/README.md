## Welcome Commander profile audio (2026-10-20)

When a saved profile is successfully selected—or a new one is successfully created—the Boot menu requests the **recorded `voice_profile_welcome`** cue via the persistent `/root/GameAudio/VoiceAssistant`. The MP3 is `res://AUDIO/VoiceAssistant/Welcome_Commander.mp3`, registered by `AUDIO/VoiceAssistant/Cues/Profiles/WelcomeCommander.tres` in `DefaultAudioCatalog.tres`. Playback is nonblocking and optional; invalid entries never trigger it. The Autoload should allow the greeting to continue through the Main Menu transition. No UI timer, per-menu audio player or new save behavior. Local Godot audio-device listening test pending.

---

# Boot — Local Profile Picker (implemented)

`BootScreen.tscn` is the normal project startup scene. `BootScreen.cs` builds the heading, scrollable saved-profile list, **New Profile** modal and Exit button using `UIButtonFactory` / `MenuLayout`.

Selecting a saved profile calls `ProfileStore.Select(id)` then opens `MainMenu`. Creating one validates the name and writes `user://Profiles/profiles.json` before navigating. Failed validation and I/O errors are shown without inventing a profile. No profile is automatically selected at startup; switching from Main returns here. Reads display backup recovery notices.

Data model and disk recovery live in `SYSTEMS/Profiles/`, not this scene. This is a profile selector, **not** an account login or campaign load screen. No gameplay World/HUD nodes are loaded until the player enters Level001.
