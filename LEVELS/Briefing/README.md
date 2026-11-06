## Level002 foundation-platform mission intro — 2026-11-06

Fresh Level002 deployment now displays `Level002/Level002Briefing.tres` through the same top-right typewriter as Level001. The short briefing introduces raised foundations as buildable, enemy-blocking tiles that can redirect routes; the script lives in `Message`. `Level002Config.tres.MissionBriefing` opts in. Level002 now uses the owner's `Level002Briefing.mp3` recording, registered by `Level002BriefingVoice.tres` in `AUDIO/DefaultAudioCatalog.tres` with cue `voice_mission_level002_intro`. The existing shared `VoiceAssistant` begins the top-right typewriter alongside playback and holds text until the clip ends. If audio is unavailable, the intro falls back to text-only. Title/Objectives are reserved metadata, not separately displayed. Checkpoint Continue skips it; wave starts, paths, balance and Level001 are unchanged.

---

## Audible Level001 intel and voice-held subtitles — 2026-11-06

**Source implemented; gameplay/audio-device acceptance pending.** Each mission briefing already has an optional Inspector `VoiceCueId`; Level001 now uses `voice_mission_level001_intro`, registered in `AUDIO/DefaultAudioCatalog.tres` by `LEVELS/Level001/Level001BriefingVoice.tres` and referencing the owner's newly pushed `Level001Briefing.mp3`. The authored spoken text in `Level001Briefing.tres` was retained exactly.

- On fresh Level001 load the existing shared `WavePresentationController` requests the voice through `VoiceAssistant.Say`. `VoiceAssistant.CueStarted` begins the existing **top-right** typewriter (even if another voice caused the clip to queue); no popup, mouse capture, pause, manual wave start or extra audio player.
- `NarrationDisplay.ShowMessage(..., waitForVoiceEnd: true)` prevents fade-out while the voice is queued/playing. After `CueEnded` from the real audio player, the existing readable hold/fade applies; text that is still typing can finish naturally. The display is still mouse-transparent and event-driven, without per-frame audio length polling.
- Pressing START WAVE during a voiced intro does not erase the speaking AI's text. The normal wave banner still appears and waves spawn normally. If that wave has its own authored narration, its text replaces the intro **after** speech ends. With no wave dialogue, the briefing fades naturally after playback.
- If a VoiceCueId is blank/unregistered/unplayable, narration remains text-only with its original timings. Checkpoint restores skip the intro and voice. Unloading a battlefield cancels a pending/active mission intro to avoid late speech in menus; the shared voice queue keeps its priority/interruption behaviour.
- `Title` and `Objectives` remain metadata for a later optional mission objectives panel and are not yet displayed here.

**Check in Godot:** fresh Level001 plays voice with its typewriter; the typed card remains visible until after the actual clip finishes; movement/placement/START WAVE work throughout; starting Wave 1 early retains subtitles without a second voice; pause and checkpoint Continue do not replay; speech ends on scene exit. Source-only sanity checks; no audio playback or performance testing claimed.---

# Optional mission intel — pre-wave typewriter (2026-11-06)

**Source implemented / owner Godot visual check pending.** Mission load intros reuse the **existing top-right `UI/Narration/NarrationDisplay`**, including its typewriter reveal, fade timings, compact cyan-bordered design, pause behaviour and auto-hide. There is **no centred popup, Continue button, input capture or automatic pause**. The player can move the camera, build and inspect towers, and press **START WAVE** while the message is being typed. When Wave 1 begins, the existing wave presenter replaces/clears the intro in the same card and issues the normal wave banner. This is not a spawn delay.

## Authored by each level

- `LevelConfig.MissionBriefing` is an optional `MissionBriefingDefinition` resource. `Enabled` and nonempty `Message` are required to show it.
- On a **fresh** battlefield, `LEVELS/Waves/Presentation/WavePresentationController` schedules one deferred `NarrationDisplay.ShowMessage(Message, Speaker)` after HUD/world initialization, only while `WaveManager` is still at wave 0 and Ready. This avoids replacing a wave announcement if the player starts exceptionally quickly. It has no repeating timer/physics processing.
- `Speaker` is the small heading and `Message` is the typed body, exactly like per-wave tactical messages. The shared narrator uses a 390px preferred width and grows vertically to fit the complete wrapped message; its 136px configured height is a minimum. `Title` and `Objectives` remain editable in the resource for future extended layouts but are **not separately displayed** in the compact narrator.
- `VoiceCueId` is a **reserved metadata field** for future voice narration through the existing shared `VoiceAssistant` and AudioCatalog. Setting it does not synthesize, load or play voice; a recorded/generated voice clip and dedicated event wiring will be required later. `Level001` currently reserves ID `voice_mission_level001_intro`, with no audio resource yet.
- `Level001/Level001Briefing.tres` is the current enabled example. Other levels keep the default null briefing and normal loading.
- Checkpoint **Continue** skips the intro via existing `BattlefieldLauncher.RestoringCampaign?.Checkpoint`; fresh Retry starts a new mission and may show the briefing again. The game's existing wave state, save schema and profile voice greeting remain untouched.

The former `UI/MissionBriefing` centered modal from the first pass is **removed**; its gameplay-pausing behaviour is obsolete. The existing NP2 typewriter is the single visual authority and still sleeps when not showing a message.

**Minimal owner checks:** F5 Level001 and confirm a top-right typed line at WAVE 00 with free movement/building; pressing START WAVE immediately replaces/clears that line and spawns normally; Level002 shows no intro; Continue checkpoint skips it; pause during typing freezes/unfreezes the existing narrator correctly. No Godot runtime or visual test claimed.
