## Mission XP connection — implemented 2026-10-17

User approved a provisional, tunable mission-to-profile XP loop before finalized scoring. **This amendment supersedes older statements that missions never grant XP; preserve the original P1–P5/R4 design below as future scope.**

- **Delivered remaining P1/P2/R4 foundation:** each level exposes VictoryBaseXP / DefeatBaseXP (500 / 0 initially); deterministic itemized scoring; captured deployment profile/settings; persisted once-only mission numbers/receipt; atomic XP + level-up RP + award marker; honest failed-save UI and retry. v1/v2 progression migrates to v3 without resetting XP, research, RP or stored curve.
- **Delivered Results/Campaign loop:** saved XP itemization, actual level changes/RP and final within-level bar; show/Skip/reopen never grants. RETURN TO CAMPAIGN opens the existing hub. Research remains Campaign-only. Profileless F6/preview says NOT SAVED.
- **Intentionally deferred original scoring:** first-clear/replay history, replay reduction/anti-farming policy, ETA/time modifiers, kill/tower/performance bonuses and deductions. Replays currently earn the same baseline; signed line-item data is ready for later reward deductions floored at zero, without removing earned levels.
- **Still open:** separate P3 Levels area, mission unlock progression, detailed XP count-up/sounds/R2 polish, Credits telemetry, campaign/battlefield Save–Load/slots and full P5 regressions. Do not mark the combined P1–P5/R4 long-term brief fully complete.
- **Verification:** .NET SDK build (0 warnings/errors), MissionXPChecks, PlayerProgressionChecks, ResultsChecks, ResearchFoundationChecks, ResearchMenuChecks and BattleResearchChecks passed. Results-frame geometry verified at 1920×1080, 1366×768 and 960×540; desktop visual review remains. Existing unrelated historical full-suite blockers remain recorded below.
- **Failure boundary:** leaving a mission whose XP could not save does not persist a recoverable pending battlefield/reward; use RETRY XP SAVE before leaving. Monotonic watermarks assume the existing single active battlefield/application flow.

Authoring/ownership: SYSTEMS/Progression/Missions/README.md. Validation: TESTS/Progression/MissionXP.md.

---

# Mission Debrief / End-of-Level Results — R1 implemented

## Current statistics / Research-first amendment — 2026-10-17

Real Results now displays **ENEMIES DESTROYED, ENEMIES BREACHED, TOWERS PLACED, TOWERS CONSTRUCTED, UPGRADES PURCHASED, MISSION DURATION, WAVES COMPLETED, CITADEL INTEGRITY, CREDITS EARNED, CREDITS SPENT**. Placement/first construction/purchased upgrades/time come from frozen LevelRunStats; kills/breaches remain official wave-only R1 tallies. Credits still display —. A 220px vertical stat scroller holds the expanded rows; skip/reveal/outcome/navigation behavior is preserved.

Player XP exists at profile/menu level, but **mission XP rewards are deliberately not configured or granted**. The Results XP placeholder says this explicitly. No Research action or shortcut was added: players return to Main → Campaign → Research to buy between levels. The R3/R4/P1/P2 plans below remain for later completion; do not infer finalized scoring, reward persistence or replay handling from the Research foundation.

Validation: TESTS/Research/README.md plus the updated existing R1 outcome expectations. Desktop animation/scroll appearance remains an R2 review task.

---

R0's shared visual foundation is now connected to real mission outcomes in both battlefield launchers. Player XP, rewards, saving and advanced statistics remain deferred. Follow `NOTES/CodePreferences.md`.

## Ownership and lifecycle

- `LevelResults.tscn` remains the shared Always-processing CanvasLayer at layer 150, hidden until presented. Both `Main.tscn` and `MainLevel020.tscn` instance it; saved previews remain off.
- `LevelResults.cs` owns presentation and guarded button intentions only: sequential stat/XP-placeholder reveals, Skip Reveal, outcome accents, disabled Details and navigation errors. It never decides victory, awards XP or pauses gameplay.
- `LEVELS/Session/LevelSessionController.cs` owns Playing/Victory/Defeat, basic wave-only tallies, one immutable `LevelResultSnapshot`, battlefield pause and navigation. No autoload or per-frame outcome polling.
- `LEVELS/Session/PlayableLevelCatalog.cs` resolves the immediately following three-digit level through explicit playable launcher registrations, with scene existence/shape validation. A raw map is never a next-level destination.

## R1 — Complete

- [x] Shared controller/results in both launchers, preserving existing service paths.
- [x] Victory requires a nonempty authored wave set reporting AllWavesComplete and positive Citadel HP. An empty/disabled wave set cannot win automatically.
- [x] Citadel destruction latches Defeat and freezes immediately, including while Pause is open. Victory resolution is deferred and rechecks health; defeat wins the final lethal breach race. Defeat presentation/snapshot waits only for the same synchronous arrival/wave callback cascade to settle, so the lethal breach and any final completed wave are included.
- [x] Exactly one snapshot/outcome. Later callbacks cannot change displayed results. Unloading before deferred presentation is safe.
- [x] Placement and ghost canceled, build tray closed, Pause locked out, results animations/UI still processing. Escape skips reveal or dismisses its error; Ctrl is consumed; global F11 remains available. Gameplay/debug nodes inherit battlefield pause.
- [x] Actual wave-owned enemies killed/breached, completed/total waves and Citadel integrity. SpawnManager supplies provenance before Initialize, including spawn-time Lava deaths; unowned F2 kills/breaches are excluded. WaveManager exposes completed-wave telemetry without owning mission outcomes.
- [x] Towers constructed, mission duration, Credits earned/spent stay nullable and display `—`. Player Experience remains explicitly coming soon, empty and unrewarded.
- [x] Victory: NEXT LEVEL; Defeat: RETRY MISSION. Both: disabled DETAILS (coming-soon tooltip), EXIT TO MAIN MENU, EXIT GAME. Actions become available after natural reveal or Skip; missing-next stays visible/disabled with a tooltip.
- [x] Real Retry/Next/Menu/Exit navigation, unpause and failed-navigation recovery, including directly running either launcher without a selected profile. Scoped subscriptions/input retire on exit.
- [x] Regression coverage for actual kills/arrivals, race arbitration, repeated events, frozen snapshots, reveal/Skip, controls, both launchers and actual scene transitions. See `TESTS/UI/Results.md` for commands and actual verification limits.

## Adding a playable level

Register its numeric ID and existing battlefield launcher in `PlayableLevelCatalog.Entries`; keep the shared World/Waves/CitadelSession/LevelSession/LevelResults structure. Current registrations: Level001 → `res://Main.tscn`, Level020 → `res://MainLevel020.tscn`. Level020 currently has no next playable level. IDs progress 009 → 010 and 099 → 100; 999 has no supported successor. Missing immediate levels are not skipped.

## Remaining passes

### R2 — Presentation polish

- [ ] Test the current sequence in the real viewport at common resolutions; tune layout/scrolling, enter/slide animations, number count-up, sounds and spacing (several visible rows before XP).
- [ ] Keep all animations **skippable immediately** without changing gameplay/outcome, XP, counters or level state. Any focus/keyboard skip must not also dismiss the whole screen.
- [ ] Defeat and victory share one scene/layout with appropriate color and copy; no new full-screen replacement scene. Optional debug preview should be off in saved level scenes.

### R3 — Additional actual statistics and scoring inputs

- [ ] Add event-driven `LevelRunStats` under `LEVELS/Session/`: successful towers built, upgrades bought, Credits **earned**, Credits **spent**, elapsed *unpaused* mission time, waves survived and properly attributed enemy kills/breaches. Count only successful placements/transactions, not clicks, ghosts or failed purchases. Avoid full-scene/per-frame scans.
- [ ] Add optional damage/tower-type/efficiency breakdown later when real damage or firing telemetry exists. Don't compute metrics from estimated final wallet balance or invent values.
- [x] E3A now reserves wave offspring and publishes their provenance before initialization. Official R1 counts include wave-child kills/breaches and exclude F2 descendants; victory waits through pending releases. Additional R3 statistics remain deferred.
- [ ] Result snapshots must be immutable after ending the mission; events coming from deferred cleanup cannot keep incrementing displayed totals.

### R4 — Player XP, bonuses and persistence (FUTURE)

- [ ] Separate **player/profile experience** from already-implemented **per-tower rank XP**. Calculate a future XP breakdown (mission victory, eligible kills, wave survival, Citadel preservation, zero-breaches; potentially time/efficiency only with anti-farming rules).
- [ ] Show baseline/bonus XP separately; animate the XP bar **after** the stat reveal; support crossing player levels only when actual progression/rank/save behavior is designed.
- [ ] Do not silently persist XP to `ProfileStore` or invent save schema now; integrate later with approved `SYSTEMS/Saving/` and profile progression. A defeat can have distinct reduced XP when explicitly designed.

## Presentation and verification

The existing shared navy/teal layout and sequential reveals are preserved, with matching red failure accents. `PreviewOnReady` / `ShowPreview()` are explicit sample-only tools. There is no rewards calculation or persistence hidden in the UI.

R1 has C# source-generator compilation and Godot .NET headless coverage. Ordinary SDK build and the monolithic integration suite have environment/preexisting blockers described in `TESTS/UI/Results.md`. Real desktop visual review remains R2; headless tests do not certify viewport polish.

## Planned XP awarding extension (P1/P2; NOT IMPLEMENTED)

See [the phased Player XP / Research implementation brief](../../../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md). Replace the existing XP placeholder with **authored victory baseline + signed performance modifiers** (Citadel integrity, editable target mission completion time, breaches), a once-only persistent profile award, player level-ups and +1 RP per level. The calculator/store owns awards; LevelResults only reveals the frozen breakdown. This implements a small defined subset of existing R3/R4, without implying all optional presentation/scoring is finished.

## Player progression foundation (2026-10-17)

Separate per-profile XP/levels/Research Points now live in SYSTEMS/Progression (see its README). Main and Campaign show the selected profile's level, XP bar and RP. Missing progression starts at Level 1; saved grants survive restart and profile switching. Identity/index format is unchanged. The optional menu DEBUG grant is Inspector-controlled and off by default. Mission rewards/Results XP, replay rules, research purchasing and full campaign/battlefield saving remain deferred; this does not complete P1–P5 or Results R4.
