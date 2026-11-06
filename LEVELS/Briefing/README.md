# Optional mission intel — pre-wave typewriter (2026-11-06)

**Source implemented / owner Godot visual check pending.** Mission load intros reuse the **existing top-right `UI/Narration/NarrationDisplay`**, including its typewriter reveal, fade timings, compact cyan-bordered design, pause behaviour and auto-hide. There is **no centred popup, Continue button, input capture or automatic pause**. The player can move the camera, build and inspect towers, and press **START WAVE** while the message is being typed. When Wave 1 begins, the existing wave presenter replaces/clears the intro in the same card and issues the normal wave banner. This is not a spawn delay.

## Authored by each level

- `LevelConfig.MissionBriefing` is an optional `MissionBriefingDefinition` resource. `Enabled` and nonempty `Message` are required to show it.
- On a **fresh** battlefield, `LEVELS/Waves/Presentation/WavePresentationController` schedules one deferred `NarrationDisplay.ShowMessage(Message, Speaker)` after HUD/world initialization, only while `WaveManager` is still at wave 0 and Ready. This avoids replacing a wave announcement if the player starts exceptionally quickly. It has no repeating timer/physics processing.
- `Speaker` is the small heading and `Message` is the typed body, exactly like per-wave tactical messages. Keep the message concise enough for the shared 390×136px narrator card. `Title` and `Objectives` remain editable in the resource for future extended layouts but are **not separately displayed** in the compact narrator.
- `VoiceCueId` is a **reserved metadata field** for future voice narration through the existing shared `VoiceAssistant` and AudioCatalog. Setting it does not synthesize, load or play voice; a recorded/generated voice clip and dedicated event wiring will be required later. `Level001` currently reserves ID `voice_mission_level001_intro`, with no audio resource yet.
- `Level001/Level001Briefing.tres` is the current enabled example. Other levels keep the default null briefing and normal loading.
- Checkpoint **Continue** skips the intro via existing `BattlefieldLauncher.RestoringCampaign?.Checkpoint`; fresh Retry starts a new mission and may show the briefing again. The game's existing wave state, save schema and profile voice greeting remain untouched.

The former `UI/MissionBriefing` centered modal from the first pass is **removed**; its gameplay-pausing behaviour is obsolete. The existing NP2 typewriter is the single visual authority and still sleeps when not showing a message.

**Minimal owner checks:** F5 Level001 and confirm a top-right typed line at WAVE 00 with free movement/building; pressing START WAVE immediately replaces/clears that line and spawns normally; Level002 shows no intro; Continue checkpoint skips it; pause during typing freezes/unfreezes the existing narrator correctly. No Godot runtime or visual test claimed.
