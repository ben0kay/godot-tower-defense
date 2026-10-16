# WAVES — Hand-Authored Enemy Waves

> **Identity convention:** The actual wave/catalog IDs are `basic_mk1` (asset Enemy001), `scout_mk1` (asset Enemy002) and `shield_emitter_mk1` (asset ShieldGenerator). Display names are independent. Numbered filenames do not determine spawn/wave order. Any old identifiers mentioned in historical prose below refer to those development asset names.

> **Current status (October 2026):** Finite manual waves are implemented. Level001's **three authored waves still use Enemy001 only**, but the validated enemy catalog already registers **Enemy001, Enemy002 and ShieldGenerator** for mixed waves. Exact weighted quotas, random eligible portals, per-entry required portals, opt-in full starting shields, enemy kill rewards, Citadel arrival damage, wave-owned completion and the HUD Start/Next controls are active. **Enemy003, general elite/stealth wave modifiers, wave-completion bonuses and wave persistence are future work.** Sections below distinguish existing APIs from future ideas.

## Implemented Phase 1 playtest

F5 loads **WAVE 00 / 03** without spawning. Press **START WAVE** below the top-right status. Wave 1 uses the requested 10 ships at 0.1s, 2s post-delay, 5 ships at 0.3s, 5s post-delay, then 20 ships at 0.2s; all are Enemy001 for now. The button shows remaining scheduled + alive ships while disabled. After every owned ship is killed or arrives, **NEXT WAVE** becomes available. F2 debug ships are unowned and cannot hold waves open. The final wave displays COMPLETE without auto-restarting.

Edit `LEVELS/Level001/Level001Waves.cs` for encounter counts, type IDs and timing; `WaveCatalog.cs` maps `LevelConfig.WaveSetId` to authored level wave sets. **Register additional enemy scenes in `ENEMIES/DefaultEnemyCatalog.tres`**, not WaveCatalog. Empty `WaveSetId` disables waves; unknown IDs report `SETUP ERROR`. `ENEMIES/DefaultEnemyConfig.tres` exposes wave scheduling limits (default two spawn attempts/frame, 0.5s retry and seed 12345). A blocked spawn preserves its planned entry.

SpawnManager.SpawnWave uses uniform random eligible portals for each ship, preserves level source permissions, prefers portals in Both mode and falls back to permitted edges only when no portal is eligible. An optional entry Portal field already supports a named registered entrance without fallback; advanced portal policies/modifiers remain future work. It uses existing revision-cached connectivity and per-enemy spawn clearance, with no extra synchronous A* requests.

WaveManager uses one countdown and no per-ship timers. Composition is prepared once per batch using exact largest-remainder quotas and seeded shuffle. Positive intervals run from successful spawns; final post-delay is ignored. Overshot frame time is discarded rather than producing catch-up bursts; zero-interval batches remain attempt-bounded. Waiting/ready/completed states have no scheduler ticks. Cancellation/reconfiguration unsubscribes owned ships; unloading Main removes all services.

TESTS/Integration/WaveChecks.cs exercises timing, 2s/5s pauses, exact 80/20 quotas as pure data, manual progression, ownership, once-only death/arrival completion, random two-portal traffic, F2 round-robin, blocked/required sources, map-edge levels, caps/retries and lag/zero-interval limits. Existing HUD, credits, tower and 500-enemy regression checks remain in place.

## Implemented design and future wave features

This document describes the **implemented C# wave system**, with future capabilities clearly marked. Preserve the current `ENEMIES/Spawning`, navigation, `EnemyRegistry`, F2 debug spawning and HUD. Author encounters in compact C# files rather than Inspector arrays. For a concise working guide, see [Authoring.md](Authoring.md).

## 1. Gameplay design and authoring goals

A **level** has an ordered finite list of waves. Each **wave** is an ordered list of **batches**. A batch specifies:
- **Count**: the total enemies to spawn.
- **Enemy entries**: one type or an exact weighted mix (e.g. 80% Enemy001 / 20% Enemy002).
- **Spawn interval**: seconds *between successful spawns* within this batch. The first enemy starts immediately when the batch begins (unless a separate pre-wave delay is configured).
- **Delay after batch**: starts **after the final successful spawn**, then the next batch starts; do **not** wait for those ships to die before starting the next batch.
- **Already supported per-entry choices**: optional named portal (`portal:`) and full starting shields (`shield: true`). Batch-wide portal strategies and additional spawn modifiers remain future work.

The **wave completes only when all scheduled enemies have successfully spawned AND every enemy owned by that wave has been killed or arrived at the Citadel**. Then a manual **Start/Next Wave** button becomes available. Do not automatically start the next wave by default.

Wave scheduling is independent of movement, portal visual nodes, navigation and tower AI. A wave should be easy to read in a single C# source file containing successive waves for a particular level.

## 2. Mixed-wave example (supported syntax, not Level001's current authored content)

**Wave 1:**
1. Batch A: **10 × Enemy001**, **0.1s** between spawns; wait **2s after last spawn**.
2. Batch B: **5 × Enemy002**, **0.3s** between spawns; wait **5s after last spawn**.
3. Batch C: **20 enemies**, exactly **80% Enemy001 (16)** and **20% Enemy002 (4)**, randomly shuffled within the batch; **0.2s** between spawns.
4. No more scheduled spawns: **wait for all wave-owned enemies to be resolved** (killed or arrived), including any still travelling from earlier batches.
5. Mark wave complete and enable **Next Wave**. The player may build/upgrade towers at their own pace before pressing it.

**Clarifications:**
- A 10-enemy batch at 0.1s interval means first spawn at t=0, last around t=0.9s, THEN the 2s post-batch delay (assuming no failed spawn attempts).
- A batch's post-delay is **not** a minimum lifetime, does not require killing enemies, and is skipped/irrelevant after the final batch unless a later design explicitly adds a final cooldown.
- Enemy type percentages should **default to exact counts** (deterministic largest-remainder rounding for non-divisible ratios and seeded shuffle). This avoids a 20% entry sometimes spawning 0 or 9 ships by chance. A future `WeightedRandom` mode may allow statistically random counts, but distinguish it explicitly.
- **Enemy002 already exists** (30 HP, 160 px/s speed, 40 optional shield capacity); ShieldGenerator is also registered. **Enemy003 has not been added.** The mixed example is a valid *alternative authored encounter*, not the active Level001 wave list.

### Existing readable `WaveBuilder` C# authoring format

**`Wave`, `Batch` and `Enemy` already exist** in `LEVELS/Waves/WaveDefinition.cs`. This illustrative example uses real registered enemy IDs but does not match the current Level001 waves:

```csharp
// Example only: replace the level's authored waves intentionally to use this mix.
using static WaveBuilder;
public static class Level001Waves
{
    public static WaveDefinition[] Create() =>
    [
        Wave(1,
            Batch(count: 10, interval: 0.1f, after: 2f,
                Enemy("basic_mk1", weight: 100)),

            Batch(count: 5, interval: 0.3f, after: 5f,
                Enemy("scout_mk1", weight: 100)),

            Batch(count: 20, interval: 0.2f, after: 0f,
                Enemy("basic_mk1", weight: 80),
                Enemy("scout_mk1", weight: 20))
        ),

        Wave(2,
            Batch(count: 20, interval: 0.15f, after: 3f,
                Enemy("basic_mk1", weight: 70),
                Enemy("scout_mk1", weight: 30))
        )
    ];
}
```

Optional shielded support ships and explicit entrance selectors already use this simple authoring syntax:

```csharp
Batch(count: 1, interval: 0, after: 0,
    Enemy("shield_emitter_mk1", portal: "Portals/EnemyPortal2", shield: true))
```

Here `shield: true` fills the individual ship's existing `MaxShield` at spawn; the separately implemented `ShieldEmitter` component can later restore nearby allies' shields. **General `EnemySpawnFlags`, `flags:`, Elite and stealth modifiers do not exist.** Do not copy older hypothetical `EnemySpawnFlags.Shielded | EnemySpawnFlags.Elite` examples as compiled C#.

Use canonical lowercase `EnemyDefinition.EnemyId` values. `ENEMIES/DefaultEnemyCatalog.tres` registers scenes; `WaveCatalog` resolves level wave sets. Unknown enemy IDs fail setup without a silent fallback.

## 3. Portal selection — random or named source (implemented)

**Implemented:** regular wave ships pick a random eligible portal **per spawn**; F2 debug spawns retain their separate round-robin behavior.

- Existing `LEVELS/LevelConfig.cs` owns `SpawnMode` (`Portals`, `MapEdges`, `Both`) and root-relative `ActivePortals` NodePaths. **These remain authoritative.** Adding an actual portal scene without registering its path in ActivePortals does not make it a wave source.
- **Default for portal-based waves: `RandomEligiblePortal`.** Pick uniformly from the enabled, reachable portal instances whose `SpawnExit` has sufficient radius clearance for the chosen enemy. Two equally eligible portals mean *approximately* 50/50 across many spawns, **not exact alternation or fixed quotas**. Pick for **each actual spawn**, not once per batch or wave. Use a repeatable RNG seed for tests if desired.
- Never choose blocked, invalid, missing, unregistered or unreachable portal exits. Do not place an enemy through terrain. Reuse the current `SpawnManager` navigation/clearance checks; validate sources against **that enemy's radius**.
- **MapEdges-only levels** must continue to work without portal nodes; choose among eligible configured edge entries. For `Both` levels, define/document a safe default that prefers random eligible portals when available but respects configured edge permissions; an explicit `RandomAllowedSource` policy can later include edges as well. Do not hardcode `Level001` or switch edge-only maps to portal spawning.
- **Implemented per-entry required portal:** `Enemy("scout_mk1", portal: "Portals/EnemyPortal2")` uses that registered, permitted level-root-relative portal and never falls back to another when blocked. **Not implemented:** batch-wide portal defaults, inherited selection policies, or a separate typed `PortalSelection` policy.
- **Future optional policies:** `RandomAmong(portalIds)`, weighted portals (70/30), an explicit `MapEdge(side)` selector and batch-wide portal overrides. Add only when needed; existing default wave source selection already supports configured map edges.
- **Forced portal unavailable?** Do NOT silently move a boss to a different portal. Pause/retry and report the invalid or blocked required source; provide an explicit author-configured fallback only if desired later.
- **Implemented:** `SpawnManager.Spawn(scene, portalOnly)` keeps F2's round-robin source choice; `SpawnManager.SpawnWave(...)` handles wave requests, independent random source choice and optional required portal. Do not merge these behaviors.
- Current `Level001Config.tres` lists `Portals/EnemyPortal` and `Portals/EnemyPortal2`; both need actual authored scene nodes and reachable exits for the expected two-portal random behavior.

**Separation:** `WaveManager` decides the enemy type, timing and optional preferred source; **`SpawnManager` decides/validates the final physical source and instantiates the enemy**. Do not spawn directly from the WaveManager or store wave timing in Portal scenes.

## 4. Implemented folder structure

Wave scheduling helpers live in **LEVELS/Waves/**; authored encounters live beside their map:

| Location | Contents |
| --- | --- |
| LEVELS/ | LevelConfig.cs, WorldLevel.cs, defaults, template and README |
| LEVELS/Level001/ | Level001.tscn, Level001Config.tres, Level001Waves.cs and README |
| LEVELS/Waves/ | WaveManager.cs, WaveDefinition.cs (including batch/entry/builder types), WaveCatalog.cs, Authoring.md and this README |
| LEVELS/Debug/ | Shared level debug controller and README |

Future levels keep their own Level002Waves.cs beside Level002.tscn and settings, then register their WaveSetId in WaveCatalog. All maps reuse the shared scheduler/data helpers; enemy scenes, spawning and movement remain under ENEMIES.

### Ownership boundaries

| Component | Owns | Does not own |
| --- | --- | --- |
| `WaveManager` | Current wave index, state, next spawn time, batch index, remaining planned spawns, wave-owned enemy tracking, start/complete events | Choosing navigable spawn positions, A*, tower combat |
| `WaveDefinition`, `WaveBatch`, `WaveEnemyEntry` | Immutable authored wave data; helper/validation functions | Spawning nodes or gameplay timers |
| `WaveCatalog` | Maps a level's stable wave-set ID to authored C# definitions; validates known scene IDs through the catalog adapter | Spawning/timing or per-type enemy configuration |
| `ENEMIES/Shared/EnemyCatalog` | Inspector-authored scene registration, scene/definition validation and stable EnemyId lookup | Selecting which waves run on which level |
| `SpawnManager` | Per-spawn validation, permitted portal/edge selection, scene instantiation | Batch schedules / auto-advance |
| `EnemyRegistry`, `Enemy.Removed` | Live enemy membership and resolved-enemy notifications | Defining wave counts / reward schedules |
| `LEVELS/LevelConfig.cs` | Per-level `WaveSetId` reference plus existing portal/map-edge rules | Hundreds of individual wave Inspector fields |
| `UI/GameplayHUD/` | Wave text, optional small remaining count, manual Start/Next button | Timing/enemy selection or current-wave truth |
| `ECONOMY/` | Implemented per-enemy kill Credits; optional **future** wave-completion bonuses | Wave scheduling logic |

`LevelConfig.WaveSetId` and `WaveCatalog` already exist. An unknown set or duplicate wave number reports a setup error rather than using another level's waves. Preserve `StartingCredits`, `SpawnMode`, `ActivePortals` and `CitadelPath`. New **enemy types** belong in `DefaultEnemyCatalog.tres`.

## 5. State machine and manual progression

Implemented `WaveManager.WaveState` values:

1. **Ready** — level loaded; Wave 1 is prepared but not started; Start Wave button is enabled.
2. **Spawning** — running sequential batch spawn intervals and post-batch delays, even while earlier enemies are moving.
3. **WaitingForClear** — *all scheduled spawns succeeded*; wait until remaining **wave-owned** enemies are resolved.
4. **Completed** — completion event raised **once**; button becomes **Next Wave** (or `Start Wave 2`), manual control enabled again.
5. **AllWavesComplete** — final wave completed; allow a future victory flow, do not restart or spawn a nonexistent wave.
6. **SpawnBlocked / PausedError** — optional explicit state for invalid enemy IDs, disabled portal, impossible spawn or prolonged failure; show a clear error, never incorrectly mark wave completed.

**Important behavior:**
- `StartWave()` should be accepted only while Ready/Completed (and there is another wave). Guard against rapid double-clicks, duplicate timers, and starting the same wave twice.
- Start the first ship immediately at the batch's start. `intervalSeconds` measures time between successful spawns. `afterSeconds` applies once following that batch's final successful spawn; **do not add the batch's interval again after its last spawn**.
- At the end of the final batch, enter WaitingForClear, unless zero wave-owned enemies remain (complete immediately). No automatic Next Wave unless a future explicit `AutoStartWaves` option is on.
- **Track exactly the enemies this WaveManager spawned**, not `EnemyRegistry.Living.Count`. Existing F2 debug spawns must NOT delay completion or be counted in the wave remaining total. Use returned Enemy references plus `Enemy.Removed` (currently called on kill *or* Citadel arrival); unsubscribe/clean up safely.
- **Future death-spawn children:** adopt children into the parent's wave **before** removing that parent from wave ownership, so the wave cannot complete prematurely. This E3A enemy ability is not yet implemented.
- HUD counts should distinguish **scheduled but not yet spawned** from **wave-owned alive**. A useful `EnemiesRemaining` is planned-to-spawn + alive wave-owned. During a blocked spawn, do not decrement the planned count until an enemy is successfully instantiated. Never report a negative count.
- On level reload, cancellation or leaving the scene, stop outstanding scheduling, clear signals/subscriptions and recreate clean state. Don't continue scheduling in background.
- **Citadel health/damage is implemented** in the separate objective/session system. An arrival applies the ship's `CitadelDamage` once and resolves wave ownership once.

## 6. Weighted enemy mixes: exact composition first

For each batch with Count N and entries `(EnemyId, Weight)`:

1. Validate `Count > 0`, at least one entry, known IDs/scenes, finite nonnegative times and **positive total weight**. Reject negative weights, unknown modifiers, malformed portal selectors and duplicates as appropriate.
2. Normalize relative weights (authors can write 80/20, 8/2, 0.8/0.2, etc. based on your chosen numeric API; document chosen units).
3. Compute quotas `N * weight / totalWeight`; take each floor; assign leftover slots to largest fractional remainders with stable tie-breaking by entry order/ID.
4. Build a *small per-batch* enemy-type sequence of exactly N slots and shuffle with a stable seed. For 20 at 80/20 the sequence contains **16 Enemy001 + 4 Enemy002**, in a mixed order.
5. Spawn sequentially at batch intervals. Don't re-roll the enemy types per attempt when a spawn fails; retain the pending selected entry and retry its spawn.
6. A future explicitly-named `SelectionMode.WeightedRandom` could sample independently per ship instead. **Do not confuse** exact type composition with independently random portal choice.

Deterministic per-run seed (optional per-wave override) is recommended so testers can replay the same mix and portal selection order. Randomness should not depend on Godot node order/frame timing. Don't allocate/reshuffle a giant list every physics frame.

## 7. Current entry options and future modifiers

**Implemented:** `Enemy(id, weight:, portal:, shield:)`; per-type stats from `EnemyDefinition`; ShieldGenerator's optional `ShieldEmitter` component. The table below distinguishes this working shield flag from **future** general modifiers. Do not treat its other rows as an existing `EnemySpawnFlags` API:

| Modifier / behavior | Status or proposed effect |
| --- | --- |
| `shield: true` | **Implemented**: spawn with full existing per-type `MaxShield` capacity |
| `Stealthed` | Requires tower detection to be targetable |
| `Armored` | Damage reduction or different damage interaction |
| `Fast` | Increased speed multiplier |
| `Regenerating` | Repairs health over time |
| `Resistant` | Damage-type-specific resistance |
| `Splitting` | Spawns other enemies on death |
| `Elite` / `Boss` | Stat multipliers, UI highlight, optional boss portal |
| `Jammer` | Interferes with turret accuracy/target acquisition |
| `ShieldEmitter` support | **Implemented** on ShieldGenerator for replenishing eligible nearby shields; other buffs/heals remain future |

**Possible future APIs only:** general `[Flags] EnemySpawnFlags` and numeric `EnemySpawnModifiers`. Neither exists today, and there is no reason to build either until a concrete modifier needs it.

Starting shields and ability-driven shield restoration already function. Stealth, generic regeneration flags and other wave stat overrides do not. Future modifiers must alter spawned **instance** state only, never mutate shared `EnemyDefinition.tres` resources.

## 8. Existing HUD and economy integration

- **Implemented:** `GameplayHUD` displays `WAVE 00 / 03` before Level001 Wave 1 starts; `WaveManager.SetWaveControls(...)` updates button text/state and wave counts.
- **Implemented:** the top-right **Start Wave / Next Wave** button appears when an authored wave is available, disables during active waves and shows blocked/error states. The existing Credits, Citadel health, build tray and Inspector are preserved.
- **Implemented:** scheduled plus wave-owned living ship counts appear in the wave control/status tooltip. The wave label remains top-right.
- **Implemented:** `WaveManager.Publish()` pushes state/count changes to the HUD and emits `Changed`.
- **Implemented:** `EconomyManager` handles session Credits and tower construction; each enemy pays its `KillRewardCredits` exactly once on death through `EnemyRegistry.EnemyKilled` (Enemy001: 3; Enemy002: 2; ShieldGenerator: 5). Arrivals do not pay kill rewards. **Future:** optional wave-completion bonuses would pay once through the existing wallet.
- Allow optional later `PreWaveDelaySeconds`, `AutoStartWaves`, wave bonus and encounter announcements without complicating Phase 1.

## 9. Performance, error handling and safety

- One `WaveManager` scheduler for the active level. Use a single timer/countdown/state machine (or a lightweight `_Process`) rather than a Timer/Coroutine node *per enemy*.
- Precache/resolve needed `PackedScene` refs for known enemy IDs before beginning a wave; do not `GD.Load` each spawn. Keep wave-definition data separate from mutable runtime counters.
- Respect `EnemyMasterConfig.MaxActiveEnemies` and the `PathRequestManager` A* budget. Spawn at authored intervals without forcing hundreds of path calculations onto one frame.
- If the active enemy cap is reached, the chosen enemy cannot be placed safely, or a required portal is unavailable: **do not count the spawn as completed**. Wait and retry with a bounded interval/backoff or enter a clearly reported blocked state; avoid a tight retry loop and silent loss of planned enemies. Protect against permanent invisible deadlocks with explicit diagnostic/status feedback.
- No wall clipping, invalid source fallback, bypassing `SpawnMode`, or spawning at the Citadel. Keep tower placement's route-preservation protections intact.
- Avoid an unbounded same-frame catch-up burst after a lag spike or pause. Cap spawn attempts/successes per physics frame (configurable if necessary), preserve ordering, and permit zero-interval batches without freezing the game.
- All results and transitions must be idempotent; never double-complete, double-count a kill/arrival, accidentally start multiple concurrent batches or leave orphaned event subscriptions.

## 10. Phase status — no need to reimplement completed work

### Phase 1 — Basic working finite waves (COMPLETED)

Existing: immutable `WaveDefinition`/`WaveBatch`/`WaveEnemyEntry`, `WaveBuilder`, `WaveCatalog`, `WaveManager`, `SpawnManager.SpawnWave`, Level001 authored encounters, exact quotas, deterministic shuffle, random eligible portals, bounded retries, wave ownership, manual Start/Next and HUD updates.

### Phase 2 — Registered mixed enemy types and spawn options (PARTIALLY COMPLETED)

**Implemented:** enemy catalog with Enemy001, Enemy002 and ShieldGenerator; validated mixed-type spawning (see `TESTS/Integration/EnemyCatalogChecks.cs`); explicit per-entry portal selection; optional full starting shield; ShieldGenerator's independent aura ability.

**Future:** Enemy003 or other types as authored, enabling mixed types in Level001's *default* wave content, extra portal strategies, Elite/stealth/speed modifiers and advanced boss mechanics. These are **content/features**, not reasons to rebuild the scheduler.

### Phase 3 — More wave progression (FUTURE)

Optional completed-wave Credits bonuses, automatic next waves/pre-wave countdown, boss announcements, scripted encounter events, finite campaign victory, enemy preview/history UI and save/load of in-progress wave ownership. **Kill Credits and Citadel health already work** and are not part of Phase 3.

## 11. Required regression checks

- A 10-ship 0.1s batch spawns 10 (not 11); first occurs at start, post-delay begins after final spawn.
- Correct **2s then 5s** inter-batch pauses, with no requirement to kill previous batch's enemies before next batch begins.
- **Implemented:** a pure 80/20 composition fixture yields 16/4 with repeatable shuffle. `TESTS/Integration/EnemyCatalogChecks.cs` additionally executes a real 75/25 Enemy001/Enemy002 (6/2) wave with owned completion, shield/portal/reward tests.
- **Random eligible portals** receive approximately balanced traffic over many spawns with two valid sources; sequences are not forcibly alternating. F2 remains round-robin.
- A forced boss portal spawns only from its named permitted exit or reports blocked/error; never silently switches to another.
- Enemy cap/blocked exit delays or errors safely, without dropping planned enemies or finishing prematurely.
- Finishing the last scheduled spawn does **not** complete the wave while wave-owned ships still live.
- F2 debug enemies do **not** keep waves open; wave-owned arrival and death each resolve once.
- Clicking Start/Next repeatedly does not spawn two waves. Button disabled while wave is active and re-enabled after completion; final wave transitions cleanly.
- Changing/restarting levels uses that level's wave set and releases old event subscriptions.
- Existing navigation, tower placement, Battle Tower combat, Credits/HUD and headless tests still work.

**Current authored encounters:** Level001 remains Enemy001-only. Enemy002 and ShieldGenerator are **registered and available** for future Level001 edits or other authored wave sets; shield-on-spawn and ShieldEmitter restoration are implemented. Additional modifiers, wave-completion bonuses, advanced progression and saving remain later passes.

## Optional full spawn shields (implemented)

Each `WaveEnemyEntry` now includes `SpawnWithShield` (default `false`) and `WaveBuilder.Enemy(..., shield: true)` can request full shields on that entry. `WaveManager` forwards it to `SpawnManager.SpawnWave`; `Enemy.Initialize` sets the **individual** ship's `CurrentShield` to its type's `MaxShield` only when requested. Default wave/F2 spawns are still unshielded. Mixed entries sharing the same `EnemyId` remain distinct through the exact-quota shuffle; `TESTS/Combat/EnemyShieldChecks.cs` covers this, shield-first hits, damage overflow and no shared-definition mutation. Level001 wave contents are unchanged. **Ability-driven ShieldEmitter restoration is implemented**, but generic passive shield regeneration, dedicated shield visuals and a broad spawn-modifier engine are not.


## E2A/E2B enemy registration (IMPLEMENTED)

`WaveCatalog.LoadScenes()` adapts `EnemyCatalog.Default` and `WaveManager` uses the actual `SpawnManager.Catalog` scene map. Register new enemy scenes in **`ENEMIES/DefaultEnemyCatalog.tres`** instead of a WaveCatalog dictionary. The IDs `enemy001`, `enemy002` and `shieldgenerator` already work; default Level001 waves are deliberately unchanged. Quotas, per-wave RNG, required portals, bounded retries, full starting-shield option and wave-owned completion work for registered enemies. `ShieldEmitter` restores *instance* shields without altering authored resources or wave ownership. E3 death-spawn/attack mechanics remain future work.

## Environmental damage at spawn

A valid spawn can be killed immediately by an Interactive Lava entry. The scheduler still consumes that planned spawn and advances the batch, but subscribes living ownership only for an alive, nonqueued ship. This prevents waiting forever on a death that occurred before ownership could subscribe; kill Credits and removal remain in Enemy. InteractiveTerrainChecks includes a lethal authored-portal Lava spawn and verifies wave completion.
