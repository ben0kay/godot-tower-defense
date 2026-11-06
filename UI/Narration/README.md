## Content-sized narration cards — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** The shared card now grows vertically to fit the full wrapped message, including Level001's multi-paragraph briefing. `PanelWidth` remains the preferred width (clamped on narrow windows); `PanelHeight` is now the minimum height. RichTextLabel fits content and reveals characters after shaping, reserving the full message layout before typing so the box does not grow per character. Container minimum-size and viewport resize events recalculate height; shorter replacements shrink back to their own content/minimum. Existing typography, mouse transparency, voice hold, fade and idle sleep remain unchanged. Very long messages may extend past a very short viewport; no automatic scrolling or text truncation is introduced.

Source/scene review only; .NET is unavailable. Owner: open fresh Level001, check the final paragraph remains inside the cyan border, resize the window, then check a short wave transmission returns to a compact card.

---

## Voice-aware typewriter hold — 2026-11-06

The existing narrator now accepts optional `ShowMessage(text, speaker, waitForVoiceEnd: true)` for spoken mission intros. It stays in the ordinary top-right card and does not fade during a voiced line; `ReleaseVoiceHold()` runs only after the voice playback-end event and restarts the usual hold/fade. The optional flag leaves all existing two-argument wave narration unchanged. The narrator never owns sound playback and keeps processing disabled while idle. See [mission briefing](../../LEVELS/Briefing/README.md).---

## Mission-load briefings reuse this narrator — 2026-11-06

**Source implemented; runtime verification pending.** `LevelConfig.MissionBriefing` opt-in now calls this same top-right `NarrationDisplay.ShowMessage(Message, Speaker)` **once after fresh level initialization**, before any wave starts. It is the identical NP2 typewriter, not a modal. Gameplay remains responsive (camera, build tray, tower selection, START WAVE). The same wave presenter replaces or clears the intro on an accepted wave start; no additional narrator/pause/audio code exists. The retired centred `UI/MissionBriefing` popup was removed. `VoiceCueId` is future metadata only; female AI voice playback is not yet implemented. See [briefing owner](../../LEVELS/Briefing/README.md).

---

# Narration — shared tactical transmissions (NP2)

**Implemented in source 2026-10-20. Godot runtime/build/desktop visual verification pending.** This is the reusable *visual-only* tactical narrator displayed under the top-right Wave/Start controls; it is not an audio system.

## Files

- `NarrationDisplay.tscn`: one mouse-transparent CanvasLayer (layer 23) and a compact text card with a speaker label and RichTextLabel.
- `NarrationDisplay.cs`: event-triggered typed-character reveal, fade-in, post-typing hold, fade-out, message replacement, public FinishTyping() for future skip binding and Clear() for wave/results/unload. Stops _Process completely when idle; only one display per HUD.
- `NarrationConfig.cs` / `DefaultNarrationConfig.tres`: Inspector settings for enable, characters/second, hold and fade times, card size, top/right margins and colours/font sizes. Defaults: **34 chars/sec, 5s hold**, 390px preferred width / 136px minimum height, **top-right, 64px down / 18px from right**, 0.18s entrance / 0.3s exit. Edit the default resource in Godot; no recompilation required.
- `LEVELS/Waves/Presentation/WavePresentationController.cs`: existing NP1 WaveStarted event now independently routes optional `Text` and `Speaker` to narrator, alongside the existing major wave banner. No extra polling or timers in WaveManager.

## Layout

The TopBar is roughly 46px tall and contains Wave/Start on the right. The narration box starts at y=64 to clear it. Since the NP1 top-centre major alert may overlap the right-hand panel on narrower displays, `NarrationDisplay.LayoutCard` computes horizontal overlap against existing `NotificationSettings`. If there is an overlap, the narration panel is placed **under the entire major-alert area**, rather than cover the announcement. Reflows on viewport resize. Test both 1920×1080 and 960×540 manually, and keep the panel mouse-transparent so placement/selection continue normally.

The display is on CanvasLayer 23, above Alerts 22 and HUD 0, below PauseMenu 100 / Results 150. Pause suspends its processing. On victory/defeat the LevelSessionController explicitly clears narration before pausing for Results. Future custom cutscene priorities can be added without changing this shared HUD layout.

## Per-level wave authoring

`WaveDefinition` already supports optional `Presentation(...)` beside batches (NP1), including `WaveAlertKind.Standard/Heavy/Final/Boss/None`, `bannerTitle`, `speaker`, `text` and `voiceCueId`. The actual speaker/body text is only displayed for waves with nonblank `Text`. No text: normal wave banner remains but narrator is hidden. `None` alert suppresses the major banner **without suppressing authored narration**. The same authoring works for any level's `WaveSetSource` with no per-level HUD nodes.

Initial sample: Level020 waves 1/4/6/7 display short tactical messages; waves 2/3/5 show the regular numbered alert only. Existing enemy batches, shield flags, spawn timings, Citadel settings and economy are unchanged. `VoiceCueId` strings are future reference identifiers: **no sound, speech or audio loading happens in NP2**.

## Limitations / testing

- `FinishTyping()` is a public API for a future optional skip control; **no new keyboard shortcut** is bound in this pass (preserves placement and pause controls).
- A new authored message replaces the previous one. Unnarrated wave starts clear the card. There is no history, subtitles queue, voiceover, pre-wave delay, cinematics, portraits or TTS.
- Light checks are in `TESTS/UI/NarrationChecks.cs` via `TESTS/Integration/WaveChecks.cs`: initial hidden/sleep state, typewriter advancement, finish, replacement, clearing and Level020 opt-in content. A full NP3 visual/input suite remains later.
- Run Godot .NET compile and integration runner; verify actual normal-zoom typography, resizing, pause/result lifecycle and coexistence with banner/toast in a desktop playtest. **No execution results should be claimed until performed.**
