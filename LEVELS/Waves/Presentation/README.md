## Status — NP1 complete; NP2 narrator foundation implemented in source (2026-10-20)

- **NP1 complete in source:** existing optional `WavePresentation` metadata + `WaveManager.WaveStarted` exactly once for an accepted start. Default `WAVE NN INCOMING` banners use the existing `NotificationManager` major-alert queue. The user has visually confirmed the simple banner looks fine.
- **NP2 source now added:** one `UI/Narration/NarrationDisplay.tscn` and style Resource, placed **top-right underneath the wave controls**, automatically shifted below the central banner on narrow screens. The NP1 `WavePresentationController` now routes `Speaker` and `Text` independently of the banner. Level020 waves **1,4,6,7** demonstrate tactical text; 2/3/5 remain banner-only. `VoiceCueId` stays metadata only. The scheduler, wave data, enemy counts, and spawn delays were not modified.
- **Still pending:** Godot/.NET build, headless tests and desktop NP2 visual/layout validation; NP3 extended end-to-end tests, optional skip binding, portraits and future actual audio. See [UI/Narration/README.md](../../../UI/Narration/README.md) for config, layout and tests.

---

## First usable slice — automatic numbered wave banners (2026-10-20)

**NP1 source implemented and user visually approved; older historical limitations in this section are superseded by the status update above.** The optional immutable `WavePresentation` / `WaveAlertKind` authoring contract and backwards-compatible `WaveBuilder.Wave(number, Presentation(...), Batch(...))` overload exist. `WaveManager.WaveStarted` fires **once when `StartWave()` is accepted**, before the first existing immediate spawn. A single `WavePresentationController` under the shared `LEVELS/Runtime/Battlefield.tscn` subscribes and sends a **silent top-centre `WAVE 01 INCOMING`** (appropriate number for each wave) through the existing `NotificationManager` major-alert pipeline. Without metadata, every wave gets the standard banner automatically. Omitted text has no narrator UI, and optional `Text`, `Speaker`, `VoiceCueId` are reserved for later NP2 only; no audio plays.

The alert system's existing `ShowMajor(string id)` still serves registered named alerts; an overload `ShowMajor(MajorAlertDefinition)` now accepts *transient*, valid major alerts through the same queue, cooldown and priority rules, avoiding seven permanent catalog entries. Current banner duration is **3 seconds**, with the existing shared major-alert fade/slide timings and pause policy. It is not a pre-wave timer: spawning still begins immediately. A plain existing recipe `Wave(3, Batch(...))` is unchanged and gets a number-only announcement. Level001/002/020 do **not** need their data updated.

The coordinator already supports an optional `WaveAlertKind.None` to suppress a particular banner and optional custom `BannerTitle` / `BannerDescription`, with Heavy/Final/Boss tones; **no level currently sets these overrides.** Do not infer boss status from an enemy ID. The `NotificationManager` queue may delay a banner if another major alert is on screen; it does not interrupt critical higher-priority warnings. This first slice has **no new UI view**, only uses existing major alerts, and remains silent.

Regression assertions were added to `TESTS/Integration/WaveChecks.cs` for default metadata compatibility, number-only banner on first start, immediate spawn and once-only event semantics. **These were not executed in Godot here**; validate build/tests and visual presentation at 1920×1080 and 960×540. Retrying/unloading the battlefield recreates the scoped controller and shared HUD; listeners release on exit. Future NP2 narrator/typewriter + Level020 authored dialogue and NP3 thorough coverage below remain pending.

---

# Wave Presentation — narrator, typewriter transmissions and wave announcements

**Original implementation plan (2026-10-20; retained for context).** NP1's data/event and a small *banner-only* part of NP2 are now source-implemented as described above. The remaining narrator/typewriter/audio plans below are future work; example code and file trees may be historical.

## Intent

Level authors should write optional tactical/narrator messages **next to the enemy batches in each level's wave recipe**. The HUD should show a short **wave-start banner** for every accepted wave, with optional special warning types (heavy, final, boss) and an optional typewritten narrator message. Some waves have narration; many do not. The same infrastructure must be reusable across Level001, Level002, Level020 and future missions, without one UI implementation per level. Reserve an optional future voice cue ID without implementing voice playback now.

Example behaviour:
- Level020 Wave 1: standard `WAVE 01 INCOMING` banner + introductory transmission.
- Wave 2: standard banner **only**; no narrator box if no text is authored.
- Wave 4: `TRANSPORTERS DETECTED` warning + transporter guidance.
- Wave 6: `HEAVY HOSTILES INCOMING` warning + advice about appropriate towers.
- Wave 7: final/critical banner + optional commander's last message.

**Scope:** presentation only. Enemy identity, spawn order/weights, intervals, wave ownership, start-button rules, Credits, victory, Citadel damage, targeting and performance must be unchanged. Do not add a pre-wave countdown in this pass.

## Read and respect current code first

- Root `README.md` and `NOTES/CodePreferences.md`: C# style, comments/regions, modularity, performance, **one commit per job**.
- `LEVELS/Waves/WaveDefinition.cs`: immutable `WaveDefinition(int Number, ImmutableArray<WaveBatch> Batches)` plus `WaveBuilder.Wave(number, params batches)`. `WaveEnemyEntry` already supports `shield:` and `stealth:`.
- `LEVELS/Waves/WaveManager.cs`: finite manually started waves. `StartWave()` returns false when it cannot start; on accepted start it sets up the first batch, calls `Publish()`, and **immediately attempts the first spawn** via `Advance(0,1)`. Existing `WaveCompleted(int)` and `Changed` events must remain compatible.
- `LEVELS/Waves/WaveSetSource.cs` and `LEVELS/LevelConfig.cs`: each mission's `WaveSource` is an Inspector-assignable Resource; `Level020Waves : WaveSetSource` exposes `Build() => Create()`. Do **not** restore an obsolete `WaveCatalog` switch or create a second level-wide wave array.
- `LEVELS/Runtime/Battlefield.tscn`: shared battlefield contains a single `Waves` manager and `UI/GameplayHUD` (all levels share these services). One presentation coordinator belongs here.
- `UI/Alerts/NotificationManager.cs`: existing single CanvasLayer with bounded/priority-based major alerts and toasts; `ShowMajor(string id)` currently accepts **catalogued static IDs only**. `UI/Alerts/MajorAlerts/MajorAlertDefinition.cs` already has titles, descriptions, tone, priority, duration, once-session/cooldown, and a **future-only** `VoiceCueId`. Do not duplicate the alert manager.
- `UI/GameplayHUD/GameplayHUD.tscn`: shared HUD already instances `NotificationManager` and has a bottom-aligned build tray/selected-tower Inspector. The narration panel must not obscure interactive controls.
- `UI/Menus/Pause` and `UI/Menus/Results`: visual layers above Alerts (22); do not let dialogue persist into Results or across retries.
- `TESTS/Integration/WaveChecks.cs`, `Level020Checks.cs` and relevant `TESTS/UI/`: retain their existing authored-wave and interface assertions.

## Proposed folder structure

```text
LEVELS/Waves/
├── WaveDefinition.cs               (existing; optional immutable metadata)
├── WaveManager.cs                  (existing; new once-per-start signal)
└── Presentation/                  (NEW)
    ├── README.md                  (this plan)
    ├── WavePresentation.cs        (authoring DTO + enum)
    └── WavePresentationController.cs  (battlefield-scoped event adapter)

UI/
├── Alerts/                        (existing; reuse major banners)
└── Narration/                     (NEW)
    ├── README.md                  (authoring/UI lifecycle)
    ├── NarrationDisplay.cs        (typewriter view)
    ├── NarrationDisplay.tscn       (shared HUD instance)
    ├── NarrationConfig.cs         (Inspector Resource)
    └── DefaultNarrationConfig.tres

TESTS/
├── Integration/WavePresentationChecks.cs   (wave/trigger invariants)
└── UI/NarrationChecks.cs                   (reveal/skip/pause/cleanup)
```

This is an intended design, not a claim the listed new files exist yet. A very small adapter may live under `LEVELS/Waves/Presentation` rather than in the HUD; the UI view should not know enemy catalogs, mission IDs, or the WaveManager scheduler.

## Proposed authoring API (pseudocode; not yet compilable)

Add an optional `WavePresentation` to the existing immutable wave definition and **preserve** the exact existing `Wave(number, params WaveBatch[])` helper. Add a separate overload `Wave(int number, WavePresentation presentation, params WaveBatch[] batches)`; existing Level001/002/020 recipes and test fixtures must compile unchanged.

Example of authoring a wave with text and a special banner:

```csharp
Wave(6,
    Presentation(
        alert: WaveAlertKind.Heavy,
        bannerTitle: "HEAVY HOSTILES INCOMING",
        speaker: "TACTICAL COMMAND",
        text: "Commander, heavy enemy armour is approaching. Consider using Blaster Cannons.",
        voiceCueId: "level020_wave06" // optional metadata only: no playback yet
    ),
    Batch(count: 30, interval: 0.34, after: 3,
        Enemy("medium_mk1", 40), Enemy("heavy_bulwark_mk1", 60))
),
Wave(7,
    Batch(count: 10, interval: 1.2, after: 0, Enemy("cruiser_mk1", shield: true))
)
```

This is just the API shape; **do not replace Level020's existing batches with these example numbers**. In a real demo, retain the existing seven waves and enemy counts exactly. Example of ordinary wave without text: `Wave(2, Batch(...))` — still gets the default `WAVE 02 INCOMING` banner and **no** narrator panel.

Suggested `WaveAlertKind`: `Standard`, `Heavy`, `Final`, `Boss`, `None`. `None` intentionally suppresses the banner; omitted presentation defaults to `Standard`. Do not automatically classify a Cruiser as a game-mechanical boss; only explicitly authored `Boss` should show a boss warning. Optional `Speaker`, `Text`, `BannerTitle`, `BannerDescription`, `VoiceCueId`; no text or whitespace means no narrator panel. Keep presentation metadata immutable per wave and never store its runtime typing state in Resource/definition objects.

## Pass NP1 — data contract and true wave-start event

1. Add `WavePresentation`/enum, nullable optional presentation on `WaveDefinition`, backwards-compatible builder overload, and reasonable validation of authored metadata (non-null text, bounded maximum length, safe default titles). Do not change WaveBatch, Enemy, WaveCatalog composition or wave status math.
2. Add a strongly typed **`WaveStarted` event** on `WaveManager` (e.g. event `Action<WaveDefinition>`); emit **once for each accepted `StartWave()`**, after transitioning to Spawning and **before** its immediate first-spawn attempt. Invalid/repeated starts, wave setup errors, retries of blocked spawns, reopening the HUD, `Publish()` state changes and F2 debug enemies must not emit it.
3. Preserve pause, start/complete and all-wave victory behaviour. `Configure()` should never fire narration merely from loading the mission. Event listeners should be unsubscribed on teardown/retry; no duplicate connections or static cross-level presentation state.

## Pass NP2 — reuse existing major banners + shared narrator UI

1. Introduce a battlefield-scoped `WavePresentationController` that subscribes to `WaveManager.WaveStarted`; show the default wave title for ordinary waves and optional heavy/final/boss overrides. Keep `WaveManager` independent of UI classes (no `NotificationManager.Find` inside scheduler). Add one coordinator to shared `LEVELS/Runtime/Battlefield.tscn`, not to every map.
2. **Reuse** `NotificationManager`'s existing queue, priorities, visual frame and pause handling. Its current `ShowMajor(string)` only resolves catalog definitions; implement a **safe dynamic/one-off alert entry point** (e.g. overload taking a validated ephemeral definition or immutable snapshot) which passes through the *same* queue/dedup/priority rules. Do not mutate shared `.tres` definitions or create seven permanent catalog resources for numeric wave announcements. Give each wave alert a unique nonpersistent ID scoped to mission deployment and wave number; cooldown=0 and once-per-session=false for ordinary wave-start announcements. Preserve existing `ShowMajor("boss_detected")` and other callers.
3. Add one `NarrationDisplay.tscn` under the shared HUD: compact futuristic tactical-transmission card with optional speaker label, `RichTextLabel`, configurable characters per second, short hold duration after typing, simple fade/slide, and ability to complete typing immediately when skipped. Expose style, speed, position and durations via `DefaultNarrationConfig.tres`. Use `VisibleCharacters`/visible ratio rather than rebuilding strings; no animation nodes/timers per character.
4. Position narration **above** the bottom-docked build tray/Inspector, clear of top-left toasts/top-centre warnings and compatible with small screens (e.g. 960×540). Non-interactive card should use `MouseFilter=Ignore`. Any optional skip input must use an existing scoped input action with proper binding/unbinding, must not steal placement controls and should be optional. Ensure narrator never paints over Results or PauseMenu (keep below their canvas layers).
5. Work on UI presentation only while a message is visible/typing; idle view stops processing. Decide and document a simple replacement policy: a new wave's authored narration **replaces** the prior transmission (complete previous animation/voice placeholder and start latest), rather than stacking cards or retaining old queued messages. If text absent, clear/hide as appropriate and show only the alert.
6. Pausing should freeze typing/dismiss countdowns by default; resuming continues. Results, retry, return to Campaign and scene exit must cancel typing and clean up listeners, tweens and UI promptly. An optional `VoiceCueId` must survive in authored metadata but **must not attempt playback**; future audio can consume that stable key through a separate AUDIO subsystem.
7. Content demonstration: author **only Level020 waves 1, 4, 6, 7** with short helpful sentences and matching banners; waves 2/3/5 retain default generic banners and **no text**. Do not alter existing wave batches, timings, shields/stealth, enemy IDs, counts, currencies, map, or objective. Level001/002 should show standard default banners without authored narrator text.

## Pass NP3 — regression tests, visual check and docs

- Assert 001/002/020 unchanged wave counts/composition after adding metadata; use the existing validator, not a second data provider.
- Confirm one `WaveStarted` per accepted manual start; no duplicate for invalid start, blocked-spawn retry, `Changed`, scene reload, or F2. Test first spawn still occurs on the same accepted-start call.
- Confirm with/without metadata cases: default banner, explicitly suppressed banner, special overrides, null/empty narrator text, correct speaker and optional voice key; no text should create no visible narrator.
- Test alert queue compatibility with critical alerts and ID uniqueness across waves/missions; original named alert APIs still function.
- Exercise quick repeated messages, skip-to-end, pause/unpause, resize, unload/retry, Results overlay and long texts. Check no active idle `_Process`, no per-enemy/per-character Node or allocations in wave spawning.
- Visually review at **1920×1080** and **960×540**, including when the bottom tray and selected Inspector are open. Do not mark visual quality verified from headless tests alone.
- Update `LEVELS/Waves/Presentation/README.md`, `LEVELS/Waves/README.md`, `UI/Alerts/README.md`, `UI/Narration/README.md`, Level020 notes and relevant test docs with **actual** implemented/tested status. Follow single-commit-per-job preference.

## Intentionally postponed

Pre-wave approach countdowns/delaying the first spawn, cinematic pause, camera movement, portraits/animated avatars, full conversation trees, localization, audio playback/TTS, wave-specific cutscenes, audio ducking, mission-end storytelling and narrated enemy-entry triggers. The current scheduler starts spawning immediately on manual acceptance; display text should say **INCOMING** as a same-time announcement, not imply a distinct warning lead time.

**Acceptance goal:** a player presses Start Wave; the usual enemy spawn proceeds exactly as before; a wave banner appears, and an optional typewriter tactical message appears only for waves whose author explicitly supplied text. Every level gets this automatically through its shared battlefield.
