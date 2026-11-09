# Mission Results V2 — phased Codex implementation plan

**Repository:** `ben0kay/godot-tower-defense` · **Branch:** `dev` only
**Status:** Approved as a planning roadmap, **not implemented** · **Updated:** 2026-11-08
**Reference:** User's Level003 victory screenshot (91 destroyed, 7 breached, 4 placed / 3 constructed, four completed waves, 99% Citadel integrity, 500 XP; both Credit rows unavailable).

## Goal

Turn the existing sci-fi Mission Debrief into a readable after-action report with an optional detailed tactical breakdown. Preserve the current neon cyan victory/crimson defeat styling, viewport-responsive single shared Results scene, official combat attribution, mission completion rules, one immutable result snapshot, live victory hold, skippable reveal/count-up, already-committed profile XP, and safe navigation. **Do not replace R1/R2, rebuild the results system, or implement all phases in one pass.**

The screenshot's ten wide full-row statistics leave a large empty area above the fixed XP/actions footer. The intended design is: **top-level three-stat summary (Enemies Destroyed, Citadel Integrity, Waves Completed); compact secondary actual metrics; optional wave chart when real historical telemetry exists; current saved XP/reward track and actions at the bottom.** DETAILS opens a separate view within the same results overlay rather than inventing a new campaign scene.

### Source of truth and current status (verify on fresh `dev` before implementation)

- `UI/Menus/Results/LevelResults.cs` currently renders the 10 statistic rows, XP itemization/bar and disabled DETAILS button; it owns **presentation only**. `LevelResults.tscn` is the shared CanvasLayer; `ResultsVectorBackdrop.cs` owns passive decoration. R2 entrance/staggered reveal, count-up, Skip/Escape and saved-XP animation **already exist**.
- `LEVELS/Session/LevelSessionController.cs` owns victory/defeat arbitration, wave-owned kill/breach counts, immutable `LevelResultSnapshot`, session freeze, reward invocation and result navigation. Never grant XP from UI.
- `LEVELS/Session/LevelRunStats.cs` already tracks actual paid placements, first completed constructions, purchased upgrades and unpaused mission time. It currently lacks Credits and breakdown telemetry. The screenshot's 4 placed / 3 constructed is **possible and not necessarily a bug**.
- `ECONOMY/Wallet/EconomyManager.cs` is the authoritative source for successful credits `Grant` and `TryPurchase`, including kill rewards, production and selling paths. Its existing `BalanceChanged` reports balances, **not gross earned/spent transactions**. Both Credits fields in `LevelResultSnapshot` are nullable and currently left unassigned.
- `LEVELS/Waves/WaveManager.cs` emits `WaveStarted` and `WaveCompleted`. `SpawnManager` and `EnemyRegistry` own spawning/kills/breaches; `LevelSessionController` already filters official wave-owned enemies and counts wave-death-spawn children, excluding F2/unowned spawns.
- `COMBAT/Attribution/DamageSource.cs`, `ENEMIES/Shared/Enemy.cs` and shared damage resolution own combat credit. **Do not** infer actual tower damage or kills from damage requests, bullets fired or final enemy health. Fighter sources are separate from towers.
- `SYSTEMS/Progression/Missions/MissionXP.cs` currently awards only the level-authored baseline (typically 500 XP victory / 0 defeat). `MissionRewardLedger` already provides once-only atomic awards and retry handling; first-clear and replay policies are **not active**. Do not silently change rewards.
- Checkpoint records under `SYSTEMS/Saving/` already preserve some run counts. Any new results history must be explicitly versioned/optional for Continue and old checkpoint compatibility, with no transactions/awards replayed during restore.
- Existing owner documents: `UI/Menus/Results/README.md`, `SYSTEMS/Progression/Missions/README.md`, `NOTES/IMPLEMENTATION_BACKLOG.md`. Historical lines in these files may predate already-delivered features.

## Ordered implementation slices

### R3-A — Real Credits earned/spent (first; small)

1. Add an event-driven, **committed-transaction-only** Credits counter owned by `LevelRunStats` or a small level-owned helper. Instrument authoritative `EconomyManager` grant/payment success boundaries; do not derive gross flow from wallet's closing balance or parse HUD text.
2. Count Credits **actually received** (kill rewards, periodic generation, sale/refunds and any legitimate future grants) and Credits **actually paid** (buildings, paid upgrades and other wallet purchases). No initial wallet starting balance, failed/rolled-back purchase, affordability check, saved balance restore, refund double-count, non-Credits resource or abandoned transaction. Reuse successful transactional paths. If meaningful, include breakdown by source in a later optional detail, not in the core total.
3. Record cumulative lifetime **run** totals across a between-wave Continue via compatible checkpoint fields; restoring wallet/telemetry must not publish phantom transactions. Use safe widened counters or explicit overflow protection; show 0 for tracked zero and `—` only for genuinely unavailable legacy data.
4. Freeze totals at official outcome and assign `LevelResultSnapshot.CreditsEarned/Spent` without unfreezing during the existing 4-second victory hold. Don't alter wallet balance, prices, kill rewards, XP or power/ammunition economy.
5. Acceptance: new mission and Continue, grants and expenses, sell refunds, failed purchases, victory/defeat, repeated Results/Skip; earned/spent must be real, immutable and independent from starting Credits. Lightweight source review/basic build only; owner gameplay validates.

### R2-B — Compact overview layout (after R3-A; small–medium)

1. Keep the **same shared `LevelResults` scene**, outcome palette, vector background, XP section and guarded navigation. Replace the excessive full-width stat list layout with **three visually prominent top metrics** (Destroyed, Citadel Integrity, Waves), then compact 2-column rows/cards for Breached, Towers Placed, Towers Constructed, Upgrades Purchased, Duration, Credits Earned and Credits Spent. No new or invented statistics in this phase.
2. Avoid giant blank centre space at 1920×1080. Let the body expand naturally while **XP and action buttons remain clearly anchored at the bottom**. Preserve flexible scrolling in 1366×768 and 960×540; no clipped labels or horizontal scroll. Defeat must use identical composition with appropriate crimson accents and correct labels.
3. Keep current count-up, staggered reveal, immediate Skip, Escape, focus, error dialogs and saved-XP bar (including multiple-level crossings, reward-save failure and profileless state). Rebind animation to identified stat widgets instead of brittle index assumptions if layout changes.
4. No trend graph or fake per-wave numbers until R3-B supplies actual history. Preserve currently disabled DETAILS button until R3-C2.
5. Acceptance: real 10 fields (including Credits), no overlapping footer/XP, 0 versus `—` handled distinctly, victory and defeat, normal reveal versus Skip.

### R3-B — Per-wave combat history and factual mini-chart (after R3-A; medium)

1. Define small **session-owned per-wave records** (stable authored wave identity/order; started/finished; successful wave-owned ships actually spawned, destroyed, breached and unaccounted/alive if defeat interrupts). Capture start/end using the existing **unpaused mission clock**. Never use current scene enemy count, prospective quota, or the F2 debug spawner as actual outcomes.
2. Associate enemy lifecycle with its originating wave at creation, including reserved transporter/death-spawn children. Avoid switching an old still-alive enemy to the newest wave, counting a ship twice, conflating portal-open attempts with successful spawns, or counting a child's spawn once again as a parent ship. Distinguish authored scheduled spawns from actual spawned offspring when showing totals.
3. Leverage existing `WaveManager.WaveStarted/WaveCompleted`, session wave ownership, `SpawnManager` and `EnemyRegistry` hooks; maintain a lightweight dictionary/aggregate on events, **not per-enemy per-frame polling or scanning the world**.
4. Freeze read-only history with the result. For a defeat, identify the interrupted wave; incomplete duration is reported honestly. Reconcile sum of legitimate per-wave kills/breaches to authoritative Results totals, including waves with late kills. Preserve run history in versioned/optional checkpoint fields for between-wave Continue; old saves must still load and may show history **unavailable**.
5. Show a clean small **Wave Performance** chart (destroyed vs breached) in the overview's central space, with a corresponding factual table/details. If no valid history exists, omit the chart or show “Wave history unavailable”, never fabricate values.
6. Later option: add a separate authoring-side **Fleet Threat Index** from `addons/wave_editor/PLAN.md` to each row. It is an estimated fleet property, **not the player's measured battle performance**; don't require it for R3-B.
7. Acceptance: sequential waves, parallel SPAWN4 groups, transporter offspring, debug spawns, interwave delay, death during active wave, Continue, defeated partial wave and victory. No unsupported time/kill attribution.

### R3-C1 — Actual damage and individual tower attribution (after R3-B; medium)

1. Add narrowly scoped, **event-driven** telemetry at the existing final damage commit/resolver boundary, using the shared `DamageSource` and actual post-mitigation shield/hull deltas. Count only effective damage; prevent double counting for splash, pierce, chain effects, DOT, duplicate callbacks, overkill and zero damage. Record separate hull/shield damage; attribute unowned/fighter damage separately instead of mislabeling it as tower damage.
2. Capture successful kills by actual last responsible source at the authoritative death boundary, respecting tower removal and no inappropriate kill transfer. Keep historical identity of sold/destroyed towers without retaining nodes (immutable tower instance ID/type/display name/rank as available); don't interfere with tower-rank XP. Aggregates by **tower instance** and **tower type**; stable deterministic tie-break for MVP. Be explicit when effects do not carry a source and show uncredited separately.
3. Shots fired/hit, accuracy, uptime, ammunition consumed and damage efficiency are **optional later additions only** if they can be counted at authoritative events. Do not call an estimated shot/kill ratio “accuracy.”
4. Extend frozen results data with optional compact snapshots for this instrumentation; old checkpoints can show incomplete/unavailable historical details. Don't change combat damage values, projectile simulation or target selection.
5. Acceptance: bullet, laser/beam, splash, DOT where applicable, death-spawn child, fighter shot, tower sell/destruction, Continue and profileless mission; no new per-frame tower scans.

### R3-C2 — Enable DETAILS (after R3-C1; small–medium)

1. Make the existing disabled DETAILS button open a **subview/panel inside the same Results overlay**. Include factual tabs/sections: **Waves** (R3-B), **Towers** (R3-C1 grouped by type and individual; MVP with name/rank, real hull+shield damage and kills), and **Economy** (R3-A earned/spent). Prefer compact scrolling, sorting and simple bars to a dense spreadsheet.
2. The main overview remains accessible via a prominent BACK control; Escape should close DETAILS first, not navigate away or re-run/reward the mission. Keep ActionsAvailable, Skip, pause, Continue/retry, navigation failure recovery and Results unload clean. Opening/closing detail pages has no gameplay processing or reward side effects.
3. Missing metrics explicitly say `Not tracked` rather than zero. Stat sorting and all chart bars use frozen snapshot objects only. Preserve low resolution/keyboard navigation; no modal UI intercepts game exit.
4. Do not require logistics, awards/medals, save-format redesign or new gameplay systems to enable the first real DETAILS screen.

### R3-D — Optional logistics report (later, separately authorized)

Add a further Details subsection for genuinely recorded ammunition manufactured/delivered/consumed, energy produced/spent/delivered, repair expenditure and credits by source **only when existing services expose trustworthy committed deltas**. Use shared ammo/power/repair authority, no observer per projectile, no aggregate guesses or polling. Keep it out of R3-C.

### R4-A — First-clear, replay and performance-based player XP (separate approval for balance)

1. **Choose balance rules with the owner before coding reward changes.** Existing 500 baseline and the atomic ticket/receipt mechanism must remain unchanged until approval. Historical `SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md` contains proposed integrity/time/breach and replay 25% examples, **not live rules or automatic authorization**.
2. Once approved, extend `MissionXPPolicy.Calculate` with deterministic, named signed rows for first clear/replay, Citadel health, breaches, completion time and perhaps no-loss/performance. Capture level-specific tuning once at deployment; don't add kill-based farming or pressure players to waste towers. Don't silently tie bonus XP to uncalibrated Fleet Threat Index.
3. Persist first-clear/replay history per selected profile, compatible with the **existing once-only award ledger** and current profile store. Never award twice from reopen/Skip/retry/save error, never award to F6/profileless sessions, never take already-earned XP/levels away, and never mix profile XP with tower-rank XP or spendable mission Credits.
4. Display the **actual saved breakdown** rather than recalculating it in Results. Give incomplete/failed persistence an explicit state. Include an early/late mission time modifier only when a valid authored target exists.
5. Check victory, defeat, first clear/replay, failed write/Retry XP Save and two profiles; don't introduce new levels/campaign unlocks as part of this slice.

### R4-B — Level-up celebration, medals and personal bests (split internally)

- **R4-B1 Presentation:** Add a brief, skippable celebratory `LEVEL UP` / `+1 RESEARCH POINT` message **only when the persisted `MissionRewardResult` actually crosses levels**; handle multiple level-ups correctly. Optionally show derived, nonrewarding badges such as Zero Breaches / Citadel Untouched, only if truthfully satisfied by frozen mission data. Reuse current R2 XP sequence/animation; don't duplicate another saved XP system.
- **R4-B2 Records (requires explicit saved-data design):** Per-profile/per-level personal-best time, breaches, integrity and victories/attempts; choose how legacy saves and invalid/unregistered mission IDs work, and save without corrupting or duplicating existing progression. Never assume every faster finish is a better score; present **separate, factual** records.
- A future **Mission Efficiency** score (player performance) may be proposed, but define and calibrate it separately from the Wave Editor's **Fleet Threat Index** (fleet composition). Neither is implemented by this plan alone.

### Later presentation ideas — not authorization to implement

Optional small victory/defeat commentary chosen randomly from approved audio/message assets; a final battlefield snapshot (only if a safe viewport capture/save path is deliberately designed); a top tower artwork highlight; zero-breach medals; and XP sound cues. Keep expensive screenshot capture, persistent galleries, optional records and sound catalog changes **out of the R3/R4 core passes**. In particular, don't introduce a hard missing-audio dependency that can break `DefaultAudioCatalog.tres`.

## Shared engineering rules / Definition of done per phase

- **One approved slice per Codex job**. Read root `CodePreferences.md`, `project.md`, this plan, Results README and the affected source files; stay on `dev`. A plan row is a roadmap, not permission for all remaining phases.
- Prefer source-owned data from session/wallet/waves/combat. Extend `LevelRunStats` and immutable `LevelResultSnapshot` with optional fields as required; UI only renders. Do not attach full enemy/tower scene instances to saved snapshots, retain disposed objects or run scene-wide polling.
- Freeze stats **when outcome latches**, not after the live victory screen delay; late effects during the 4-second hold may animate but may not mutate the receipt. No fabricated historical totals; `0` means tracked zero and `—` means not tracked.
- Checkpoint/Continue: explicitly consider new fields and old-save defaults in each telemetry phase; never replay grants/purchases/kills on restore or give duplicate XP. Keep damage/wave/power/combat systems independent of UI.
- Preserve existing official kill/breach semantics, wave counts, Citadel health, saved XP and R2 reveal/Skip behaviour. Defeat and victory use the same scene. Avoid speculative shaders, custom rendering per tick, new unrelated managers or changing authored mission balance.
- Minimum validation: focused source/diff review and at most one practical basic build under CodePreferences, then owner gameplay review (1280/1366/1920 widths, victory/defeat, Skip, no-profile, checkpoint, saved XP). One concise `NOTES/WORK.md` entry and one commit/push to `dev` per implementation slice. Report what was genuinely verified.

**Suggested order:** R3-A → R2-B → R3-B → R3-C1 → R3-C2 → R4-A (after owner balance approval) → R4-B1/B2. R3-D and later ideas are opt-in only.
