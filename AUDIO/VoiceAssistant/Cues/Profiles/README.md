# Profile voice cues

Profile selection/creation speech lives in this folder as **AudioDefinition** resources. The first registered cue is `WelcomeCommander.tres` (stable ID `voice_profile_welcome`), referencing the user-provided `res://AUDIO/VoiceAssistant/Welcome_Commander.mp3`. The original binary MP3 is intentionally **not moved or renamed** in this pass; the GitHub connector cannot manipulate its raw bytes safely. The definition and `AUDIO/DefaultAudioCatalog.tres` are now registered so the existing `VoiceAssistant.Say(id)` queue can play it.

The `UI/Menus/Boot/BootScreen.cs` profile picker requests this cue once after a successful existing-profile choice **or creation of a new profile**, before loading Main Menu. Invalid names/selections cannot play it. Because `/root/GameAudio` is a persistent Autoload, scene changes should not cut the greeting off. No gameplay or profile persistence changes are required.

For future profile lines put each audio clip and its matching `.tres` in this folder (e.g. `ProfileLoaded.mp3` / `ProfileLoaded.tres`) and register the `.tres` resource in the shared catalog. When you eventually move `Welcome_Commander.mp3` here, do it through the Godot editor or a local Git move and update `WelcomeCommander.tres`'s `Stream` path; keep the stable ID unchanged. Do not duplicate the file under both paths.
