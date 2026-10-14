# WAVES — Hand-Authored Enemy Waves (Codex Implementation Plan)

> **Status: planned; no WaveManager exists yet.** This README is the canonical handoff for implementing **finite, carefully authored waves** in Godot 4 C#/.NET. Preserve the existing ENEMIES/Spawning, navigation, EnemyRegistry, F2 debug spawn and HUD. The goal is to edit wave composition directly in **clear, compact C# files**, not large Godot Inspector arrays.

## 1. Gameplay design and authoring goals

A **level** has an ordered finite list of waves. Each **wave** is an ordered list of **batches**. A batch specifies:
- **Count**: the total enemies to spawn.
- **Enemy entries**: one type or a weighted/exact mixed composition (e.g. 80% Enemy001 / 20% Enemy003).
- **Spawn interval**: seconds *between successful spawns* within this batch. The first enemy starts immediately when the batch begins (unless a separate pre-wave delay is configured).
- **Delay after batch**: starts **after the final successful spawn**, then the next batch starts; do **not** wait for those ships to die before starting the next batch.
- **Optional future override**: specific portal(s), individual/batch modifiers, etc.

The **wave completes only when all scheduled enemies have successfully spawned AND every enemy owned by that wave has been killed or arrived at the Citadel**. Then a manual **Start/Next Wave** button becomes available. Do not automatically start the next wave by default.

Wave scheduling is independent of movement, portal visual nodes, navigation and tower AI. A wave should be easy to read in a single C# source file containing successive waves for a particular level.

## 2. Full authored example (design target)

**Wave 1:**
1. Batch A: **10 × Enemy001**, **0.1s** between spawns; wait **2s after last spawn**.
2. Batch B: **5 × Enemy002**, **0.3s** between spawns; wait **5s after last spawn**.
3. Batch C: **20 enemies**, exactly **80% Enemy001 (16)** and **20% Enemy003 (4)**, randomly shuffled within the batch; **0.2s** between spawns.
4. No more scheduled spawns: **wait for all wave-owned enemies to be resolved** (killed or arrived), including any still travelling from earlier batches.
5. Mark wave complete and enable **Next Wave**. The player may build/upgrade towers at their own pace before pressing it.

**Clarifications:**
- A 10-enemy batch at 0.1s interval means first spawn at t=0, last around t=0.9s, THEN the 2s post-batch delay (assuming no failed spawn attempts).
- A batch's post-delay is **not** a minimum lifetime, does not require killing enemies, and is skipped/irrelevant after the final batch unless a later design explicitly adds a final cooldown.
- Enemy type percentages should **default to exact counts** (deterministic largest-remainder rounding for non-divisible ratios and seeded shuffle). This avoids a 20% entry sometimes spawning 0 or 9 ships by chance. A future `WeightedRandom` mode may allow statistically random counts, but distinguish it explicitly.
- **Enemy002 and Enemy003 do not yet exist** in the current game. This mixed example is the planned authoring format; Phase 1 must actually run with Enemy001-only test waves and must not reference missing PackedScenes or invent enemy types.

### Desired readable C# authoring format

**Illustrative intended syntax — `Wave`, `Batch` and `Enemy` are helper functions to be built by Codex; not currently compiled source.** Exact helper signatures are flexible as long as the result is similarly concise and typed.

```csharp
// ENEMIES/Waves/Definitions/Level001Waves.cs
public static class Level001Waves
{
    public static WaveDefinition[] Create() =>
    [
        Wave(1,
            Batch(count: 10, interval: 0.1f, after: 2f,
                Enemy("enemy001", weight: 100)),

            Batch(count: 5, interval: 0.3f, after: 5f,
                Enemy("enemy002", weight: 100)),

            Batch(count: 20, interval: 0.2f, after: 0f,
                Enemy("enemy001", weight: 80),
                Enemy("enemy003", weight: 20))
        ),

        Wave(2,
            Batch(count: 20, interval: 0.15f, after: 3f,
                Enemy("enemy001", weight: 70),
                Enemy("enemy002", weight: 30))
        )
    ];
}
```

Later, author special ships without copying Enemy.cs:

```csharp
// Future example only; shield/stealth mechanics are NOT active yet.
Batch(count: 1, interval: 0f, after: 0f,
    Enemy("enemy003", weight: 100,
        flags: EnemySpawnFlags.Shielded | EnemySpawnFlags.Elite,
        portal: "Portals/EnemyPortal2"))
```

Prefer **stable lowercase enemy IDs** matching `EnemyDefinition.EnemyId` and a simple scene catalog mapping IDs to existing scenes. Validate missing IDs before a wave begins. Do not force authors to paste long .tscn paths repeatedly into every batch.

## 3. Portal selection — random normally, explicit boss entrances later

**Important updated requirement:** ordinary wave enemies should **pick a random valid portal for each spawn**, *NOT* take the F2 debug spawner's current round-robin rotation.

- Existing `WORLD/LEVELS/LevelConfig.cs` owns `SpawnMode` (`Portals`, `MapEdges`, `Both`) and root-relative `ActivePortals` NodePaths. **These remain authoritative.** Adding an actual portal scene without registering its path in ActivePortals does not make it a wave source.
- **Default for portal-based waves: `RandomEligiblePortal`.** Pick uniformly from the enabled, reachable portal instances whose `SpawnExit` has sufficient radius clearance for the chosen enemy. Two equally eligible portals mean *approximately* 50/50 across many spawns, **not exact alternation or fixed quotas**. Pick for **each actual spawn**, not once per batch or wave. Use a repeatable RNG seed for tests if desired.
- Never choose blocked, invalid, missing, unregistered or unreachable portal exits. Do not place an enemy through terrain. Reuse the current `SpawnManager` navigation/clearance checks; validate sources against **that enemy's radius**.
- **MapEdges-only levels** must continue to work without portal nodes; choose among eligible configured edge entries. For `Both` levels, define/document a safe default that prefers random eligible portals when available but respects configured edge permissions; an explicit `RandomAllowedSource` policy can later include edges as well. Do not hardcode `Level001` or switch edge-only maps to portal spawning.
- **Future per-entry/per-batch portal override:** allow `portal: "Portals/EnemyPortal2"` or a typed `PortalSelection` policy. This must refer to an allowed level-root-relative path from `ActivePortals`, not a positional list index or arbitrary world coordinate. A **boss** or special enemy can require a named portal while all other enemies still spawn randomly. Entry-level override wins over a batch default; an unset entry inherits its batch/wave default.
- Optional later selection: `RandomAmong(portalIds)`, weighted portals (e.g. 70% left / 30% right), or `MapEdge(side)`. Do **not** implement a giant portal-strategy framework during Phase 1; architect for additions without hardcoded branches in each wave definition.
- **Forced portal unavailable?** Do NOT silently move a boss to a different portal. Pause/retry and report the invalid or blocked required source; provide an explicit author-configured fallback only if desired later.
- The existing **F2 debug** behavior should remain unchanged: its current `SpawnManager.Spawn(scene, portalOnly)` uses round-robin sources. Add a compact, backwards-compatible request/policy API for *wave spawns* rather than changing the F2 method unexpectedly.
- Current `Level001Config.tres` lists `Portals/EnemyPortal` and `Portals/EnemyPortal2`; both need actual authored scene nodes and reachable exits for the expected two-portal random behavior.

**Separation:** `WaveManager` decides the enemy type, timing and optional preferred source; **`SpawnManager` decides/validates the final physical source and instantiates the enemy**. Do not spawn directly from the WaveManager or store wave timing in Portal scenes.

## 4. Proposed folder structure

All wave logic belongs under existing **`ENEMIES/Waves/`**. These files are **proposed**, not present yet:

```text
ENEMIES/
├── EnemyMasterConfig.cs                  # Exists: global enemy/path limits
├── Shared/
│   ├── Enemy.cs                          # Exists: state and Removed event
│   ├── EnemyDefinition.cs                # Exists: per-type stats and EnemyId
│   ├── EnemyRegistry.cs                  # Exists: world-level living enemies
│   └── Modifiers/                        # Future shared spawn-instance effects
│       ├── EnemySpawnFlags.cs            # Future, not implemented
│       ├── EnemySpawnModifiers.cs        # Future parameterized modifiers
│       └── README.md
├── Spawning/
│   └── SpawnManager.cs                   # Exists; extend with optional spawn request/policy
├── Types/
│   ├── Enemy001/                         # Exists
│   ├── Enemy002/                         # Future
│   └── Enemy003/                         # Future
└── Waves/
    ├── README.md                         # This specification
    ├── WaveManager.cs                    # Runtime state machine/scheduler
    ├── WaveDefinition.cs                 # One wave: number + batches + optional metadata
    ├── WaveBatch.cs                      # Count, interval, post-delay, entries
    ├── WaveEnemyEntry.cs                 # EnemyId, weight, optional modifiers/portal
    ├── WaveCatalog.cs                    # Level's wave set / enemy-scene lookup integration
    ├── WaveBuilder.cs                    # Optional tiny C# helper DSL: Wave/Batch/Enemy
    └── Definitions/
        ├── Level001Waves.cs              # One readable authored file for Level001
        ├── Level002Waves.cs              # Future
        └── README.md                     # C# authoring conventions
```

Keep names uncomplicated. `WaveCatalog` and optional `WaveBuilder` can be combined if a smaller implementation is clearer. Avoid an excessive inheritance hierarchy or Inspector Resources for every batch. Add short README files as each actual subfolder is created.

### Ownership boundaries

| Component | Owns | Does not own |
| --- | --- | --- |
| `WaveManager` | Current wave index, state, next spawn time, batch index, remaining planned spawns, wave-owned enemy tracking, start/complete events | Choosing navigable spawn positions, A*, tower combat |
| `WaveDefinition`, `WaveBatch`, `WaveEnemyEntry` | Immutable authored wave data; helper/validation functions | Spawning nodes or gameplay timers |
| `WaveCatalog` | Maps a level's stable wave-set ID to its C# definition list and known enemy IDs/scenes | Editing level geometry or doing wave timing |
| `SpawnManager` | Per-spawn validation, permitted portal/edge selection, scene instantiation | Batch schedules / auto-advance |
| `EnemyRegistry`, `Enemy.Removed` | Live enemy membership and resolved-enemy notifications | Defining wave counts / reward schedules |
| `WORLD/LEVELS/LevelConfig.cs` | Per-level `WaveSetId` reference plus existing portal/map-edge rules | Hundreds of individual wave Inspector fields |
| `UI/GameplayHUD/` | Wave text, optional small remaining count, manual Start/Next button | Timing/enemy selection or current-wave truth |
| `ECONOMY/` | Future wave-completion credits, if/when specified | Wave scheduling logic |

A small new `[Export] string WaveSetId` (e.g. `"level001"`) in `LevelConfig` is reasonable. A `WaveCatalog` maps that ID to the respective `Definitions/Level001Waves.cs`. Missing sets or duplicate wave numbers must report a useful setup error, not silently run a default level's waves. Keep `StartingCredits`, `SpawnMode`, `ActivePortals` and `CitadelPath` untouched.

## 5. State machine and manual progression

Proposed states:

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
- Consider future split-on-death children: they must be assigned wave ownership if the intended win condition counts them. For Phase 1, only normal Enemy001 needs to work.
- HUD counts should distinguish **scheduled but not yet spawned** from **wave-owned alive**. A useful `EnemiesRemaining` is planned-to-spawn + alive wave-owned. During a blocked spawn, do not decrement the planned count until an enemy is successfully instantiated. Never report a negative count.
- On level reload, cancellation or leaving the scene, stop outstanding scheduling, clear signals/subscriptions and recreate clean state. Don't continue scheduling in background.
- The Citadel-health/loss mechanic is a separate system; arriving ships resolve normally for wave completion even before Citadel damage is implemented.

## 6. Weighted enemy mixes: exact composition first

For each batch with Count N and entries `(EnemyId, Weight)`:

1. Validate `Count > 0`, at least one entry, known IDs/scenes, finite nonnegative times and **positive total weight**. Reject negative weights, unknown modifiers, malformed portal selectors and duplicates as appropriate.
2. Normalize relative weights (authors can write 80/20, 8/2, 0.8/0.2, etc. based on your chosen numeric API; document chosen units).
3. Compute quotas `N * weight / totalWeight`; take each floor; assign leftover slots to largest fractional remainders with stable tie-breaking by entry order/ID.
4. Build a *small per-batch* enemy-type sequence of exactly N slots and shuffle with a stable seed. For 20 at 80/20 the sequence always contains **16 Enemy001 + 4 Enemy003**, in a mixed order.
5. Spawn sequentially at batch intervals. Don't re-roll the enemy types per attempt when a spawn fails; retain the pending selected entry and retry its spawn.
6. A future explicitly-named `SelectionMode.WeightedRandom` could sample independently per ship instead. **Do not confuse** exact type composition with independently random portal choice.

Deterministic per-run seed (optional per-wave override) is recommended so testers can replay the same mix and portal selection order. Randomness should not depend on Godot node order/frame timing. Don't allocate/reshuffle a giant list every physics frame.

## 7. Optional flags/modifiers: future-ready, minimal initially

Build the wave data model to **allow** optional modifiers later without hard-coding wave-specific behavior into `Enemy001`. Example conceptual flags and features:

| Modifier | Future effect |
| --- | --- |
| `Shielded` | Extra shield pool before taking hull damage |
| `Stealthed` | Requires tower detection to be targetable |
| `Armored` | Damage reduction or different damage interaction |
| `Fast` | Increased speed multiplier |
| `Regenerating` | Repairs health over time |
| `Resistant` | Damage-type-specific resistance |
| `Splitting` | Spawns other enemies on death |
| `Elite` / `Boss` | Stat multipliers, UI highlight, optional boss portal |
| `Jammer` | Interferes with turret accuracy/target acquisition |
| `Support` | Buffs or heals nearby enemy ships |

Suggested future separation: a compact `[Flags] EnemySpawnFlags` for toggles **and** optional `EnemySpawnModifiers` instance data for numeric parameters (shield amount, speed multiplier, etc.). Flags alone are not enough for configurable strength.

**Do not fake functional shields/stealth just by displaying a flag**. Actual defenses, targeting visibility and damage rules belong in reusable `ENEMIES/Shared/` components and appropriate TOWERS targeting helpers, implemented in a later phase. When applied, modifiers change **spawned instance state only**, never mutate shared `EnemyDefinition.tres` resources or other enemies of the same type.

## 8. Existing HUD and economy integration

- `UI/GameplayHUD/GameplayHUD.cs` already has `SetWave(int current, int total = 0)` and top-right Wave status. On level load show `WAVE --` or `WAVE 00 / total` until wave 1 actually starts; never imply Wave 1 started on load.
- Add a compact **Start Wave / Next Wave** button near the top-right Wave display (or in a small dedicated adjacent panel), enabled **only when a new wave is available**. Disable during spawning/clearing; visually show blocked/error states. Preserve the top-left Credits, true-centered Citadel health, Ctrl build tray and bottom-right Inspector.
- Optional small `remaining / total` wave-enemy count, possibly `12 remaining`, should not displace the centered Citadel display. Wave number remains top-right, as previously requested.
- Emit event/signal updates on state changes and count changes rather than polling wave state every GUI frame.
- **Economy foundation now active:** `ECONOMY/Wallet/EconomyManager.cs` handles session Credits and tower construction, while wave-completion rewards, kill rewards and enemy income generation are still later work. When implemented, pay a completed-wave bonus **once** through the existing wallet; do not implement a competing wallet or new credits UI.
- Allow optional later `PreWaveDelaySeconds`, `AutoStartWaves`, wave bonus and encounter announcements without complicating Phase 1.

## 9. Performance, error handling and safety

- One `WaveManager` scheduler for the active level. Use a single timer/countdown/state machine (or a lightweight `_Process`) rather than a Timer/Coroutine node *per enemy*.
- Precache/resolve needed `PackedScene` refs for known enemy IDs before beginning a wave; do not `GD.Load` each spawn. Keep wave-definition data separate from mutable runtime counters.
- Respect `EnemyMasterConfig.MaxActiveEnemies` and the `PathRequestManager` A* budget. Spawn at authored intervals without forcing hundreds of path calculations onto one frame.
- If the active enemy cap is reached, the chosen enemy cannot be placed safely, or a required portal is unavailable: **do not count the spawn as completed**. Wait and retry with a bounded interval/backoff or enter a clearly reported blocked state; avoid a tight retry loop and silent loss of planned enemies. Protect against permanent invisible deadlocks with explicit diagnostic/status feedback.
- No wall clipping, invalid source fallback, bypassing `SpawnMode`, or spawning at the Citadel. Keep tower placement's route-preservation protections intact.
- Avoid an unbounded same-frame catch-up burst after a lag spike or pause. Cap spawn attempts/successes per physics frame (configurable if necessary), preserve ordering, and permit zero-interval batches without freezing the game.
- All results and transitions must be idempotent; never double-complete, double-count a kill/arrival, accidentally start multiple concurrent batches or leave orphaned event subscriptions.

## 10. Codex implementation phases

### Phase 1 — Basic working waves (IMPLEMENT THIS FIRST)

1. Read `NOTES/CodePreferences.md`, this README, `ENEMIES/Spawning/SpawnManager.cs`, `ENEMIES/Shared/Enemy.cs`, `ENEMIES/Shared/EnemyRegistry.cs`, `WORLD/LEVELS/LevelConfig.cs`, `WORLD/LEVELS/README.md` and `UI/GameplayHUD/README.md`.
2. Create minimal wave data model and legible **`Definitions/Level001Waves.cs`**, with a typed enemy ID → PackedScene catalog. Use **only currently implemented Enemy001** in runnable Phase 1 definitions; keep the full Enemy002/003 mixture above documented as the target.
3. Add a stable per-level wave-set reference (`WaveSetId` or equivalent) in `LevelConfig`. Connect only Level001 first, but support more levels without copying the WaveManager.
4. Implement sequential `WaveManager` batch scheduler, exact proportions, delays, states and wave-owned completion tracking.
5. Extend `SpawnManager` with **random valid portal selection for wave requests**, **without changing F2 round-robin debugging**. Keep map-edge support and level permissions. Build a small per-request optional specific-portal field or reserve its data shape for Phase 2; do not bury source choice in WaveManager.
6. Connect manual Start/Next Wave button and HUD state updates. Do not create a full waves menu or change the existing Credits/Citadel/tray layout.
7. Test using F5: pause before first wave, press Start, spawn batches in order, leave overlapping enemies alive across batches, complete only when all planned spawns and owned enemies resolve, enable Next, preserve F2/placement/shooting.
8. Update tests and relevant READMEs when implementation actually exists. Keep editor build and existing headless regression suite working.

### Phase 2 — Diverse ships, explicit portal overrides and modifiers

- Add actual `Enemy002` and `Enemy003` scenes/definitions independently, then enable the complete authored Wave 1 example (10/5/20, 80/20 mix).
- Allow an individual enemy entry or whole batch to **require a specific portal** (boss entrance), validate the allowed ID/path and safely handle missing/blocked portals **without fallback**.
- Add initial real modifiers such as Shielded and Fast in reusable enemy-side components, plus any tower detection integration needed for Stealthed.
- Add per-portal weighting/allowed-set selection only when levels require it; the Phase 1 random portal policy remains the default.

### Phase 3 — Rewards, advanced control and progression

- Optional per-wave Credits rewards, paid through `ECONOMY/` once.
- Auto-start mode, pre-wave countdown, boss announcements, scripted encounter events, custom wave completion conditions and finite-campaign victory.
- Save/load of wave index, state, pending counts and active wave ownership once a level persistence system needs this.
- More extensive wave UI (enemy preview, modifiers, wave history) only if useful.

## 11. Required regression checks

- A 10-ship 0.1s batch spawns 10 (not 11); first occurs at start, post-delay begins after final spawn.
- Correct **2s then 5s** inter-batch pauses, with no requirement to kill previous batch's enemies before next batch begins.
- Exact 80/20 mixture produces 16 Enemy001 and 4 Enemy003 after those types exist; seed produces a repeatable shuffled order.
- **Random eligible portals** receive approximately balanced traffic over many spawns with two valid sources; sequences are not forcibly alternating. F2 remains round-robin.
- A forced boss portal spawns only from its named permitted exit or reports blocked/error; never silently switches to another.
- Enemy cap/blocked exit delays or errors safely, without dropping planned enemies or finishing prematurely.
- Finishing the last scheduled spawn does **not** complete the wave while wave-owned ships still live.
- F2 debug enemies do **not** keep waves open; wave-owned arrival and death each resolve once.
- Clicking Start/Next repeatedly does not spawn two waves. Button disabled while wave is active and re-enabled after completion; final wave transitions cleanly.
- Changing/restarting levels uses that level's wave set and releases old event subscriptions.
- Existing navigation, tower placement, Battle Tower combat, Credits/HUD and headless tests still work.

**Explicit scope:** this README is an implementation plan; no wave scripts/scenes have been added by this documentation update. Start with **Phase 1** only and don't add fictitious Enemy002/003 runtime assets or placeholder shield/stealth gameplay.
