## Fortress artwork — 2026-10-20

`Citadel.tscn` now uses a cached `Sprite2D` referencing `Visuals/CitadelFortress.svg` instead of the temporary Hull/Core polygons. The top-down armored 192px octagonal fortress is centered at local `(64,64)` to retain the existing 3×3 footprint alignment; its cyan reactor is decorative only. The `WorldObjectFootprint` root, 4 approach markers, health component, label, collision/navigation, clicking and multi-Citadel identity remain untouched. It has no runtime geometry generation or animation. Edit the SVG in this Citadel's `Visuals/` folder to swap it later.

---

## CP1B collection UI — code implemented 2026-10-18 (verification pending)

**World Citadel click + matching top-centre health HUD click** now call the same per-objective `TryCollectCredits` wallet command. Every Citadel has an independently event-updated green world Credit label/bar and a green HUD bar/amount inside its existing health panel (single legacy layout or one horizontal multi-objective row). Only the clicked Citadel pays; collection is still possible between waves, although no new Credits are generated then. Tower placement has click precedence, so Citadel clicks never sneak in during construction placement. Existing HP bars/route gameplay/kill rewards are unaffected.

Added `CitadelCreditDisplay.cs`, reusable `CitadelHudRow` meter binding and an isolated `TESTS/UI/CitadelCollectionChecks.tscn` runner. Code is committed for testing; **Godot/.NET compilation, headless tests and desktop presentation review are pending**. Refer to `ECONOMY/Production/README.md` for CP1A/CP1B contract and CP2/CP3 plans.

---

## CP1A — Citadel Credits backend implemented 2026-10-18 (runtime verification pending)

CitadelSession owns **one independent CreditBuffer and CreditProductionTimer per stable CitadelDestination**, without changing the scene's health nodes, navigation or current top-centre HUD. Inspector-exported Citadel Credit Production values default to **80 stored Credits**, **+10 every 2 seconds**. The timer runs only during WaveManager's Spawning/SpawnBlocked/WaitingForClear, freezes between waves and on game pause, and stops on mission-ending destruction. `CreditsFor(destination)` exposes the buffer's event for forthcoming visual displays; `TryCollectCredits(destination)` collects only the selected Citadel, via the existing EconomyManager/Rewards, and preserves uncollected Credits if the wallet refuses a grant.

**This is backend-only.** Clicking the world Citadel or its corresponding top-centre health panel will NOT collect yet; those interaction surfaces and green credit meters are CP1B. Headless regression scene `TESTS/Economy/CitadelCreditChecks.tscn` is authored but not yet executed; desktop visuals are not assessed. See `ECONOMY/Production/README.md`.

---

## CP1 collection UX refinement (PLANNED, 2026-10-18)

Each Citadel should have **two equivalent clickable collection surfaces**: (1) its actual world-space Citadel and (2) **its own top-centre health HUD cell**, which gains a compact, distinct green stored-Credits bar/number. Both call the **same Citadel-specific collection method**. Buffers, timers and indicators remain completely separate for multiple Citadels; collecting one never drains others. Existing multi-Citadel HUD stays **one horizontal row**, with responsive dimensions and no health meter regression. Events refresh both displays; no per-frame polling or double grants. Collect even between waves if already stored, but **only produce while a wave runs**. See [CP1 interaction decision](../../../ECONOMY/Production/README.md#cp1-interaction-decision--collect-from-world-citadel-or-its-top-centre-hud-cell-2026-10-18-planned). Not implemented.

---

## Automatic editor placement — 2026-10-18

This scene now inherits editor-only cell-center snapping from `WorldObjectFootprint`. Build C#, then drag or duplicate instances within a map normally. **Editor Placement → Snap To Grid In Editor** defaults on; disable it for intentional free placement. Uses the map's actual `Terrain/Passable` TileSet and transforms. Child-marker offsets and footprint settings are preserved; standalone asset origins and gameplay movement remain unchanged. Off-grid saved instances align when opened in the editor; save the level to persist that correction. See `WORLD/GRID/README.md` from the repository root for full authoring behavior.

---

## MC3 HUD + F3 — implemented 2026-10-18

Multiple Citadels now share **one horizontal row** in the top-center HUD: equal-width cells for two, four or more objectives, never stacked vertically. A one-Citadel level keeps the original nodes/layout/SetCitadelHealth behavior. Each multi meter shows its stable CITADEL number, independent current/max HP, threshold-colored fill and damage-only animation/flash. Zero values and maximum resets are supported; rebind/unload disconnect model events and stop old tweens. The full Citadel number/HP is available in each cell's tooltip. No HUD health polling or gameplay authority is introduced.

UIMasterConfig exposes MultiCitadelBlockWidth (560 default) and CitadelCellGap (6), alongside existing bar height/palette/timing settings. The center remains viewport-centered and capped to the existing responsive center zone (46% of width); Credits and Wave retain separate side zones. Short cells truncate text rather than wrap into extra rows. Very large counts/HP values can require the tooltip for full text. Existing Inspector/build tray geometry is unchanged.

F3 now passes each portal's **actual cached live Citadel identity** into RoutePreviewJob. Raw/refined paths, neutral comparisons and required checkpoint chains end at that assigned objective, not the primary Citadel. Legend/source labels show portal → CITADEL N; destination approach markers and one label per objective use the same stable identity colors/numbers as the HUD. ALL PORTALS keeps the existing shared preview query/time budget and private profile pool. Missing assignments appear as UNASSIGNED diagnostics with no invented fallback route. All destination goals/availability and cached assignments participate in invalidation, including secondary objectives. Closing/Clear releases private preview resources only.

Explicit TerrainRebuilt now refreshes source assignments during the authoring rebuild itself, so F3 reads current cached ownership without triggering a live assignment calculation. Ordinary tower occupancy still cannot reassign portals, and preview settings/simulation never spawn enemies or alter live route queues/profile pools/random streams.

Validation: compiled with the installed .NET SDK/MSBuild; MultiCitadelHudChecks passed one/two/four objective presentation at 1920×1080, 1366×768 and 960×540, independent HP/colors/zero/maximum reset, rebind, actual assigned F3 endpoints, secondary-goal invalidation and live-state isolation. Existing RoutePreviewChecks (both maps/raw/refined/checkpoints/50 samples/500 enemies), MultiCitadelChecks, CheckpointChecks, ResultsChecks, MissionXPChecks, PlacementHudChecks and CompactInspectorChecks passed. See TESTS/UI/MultiCitadelHud.md. Desktop rendered appearance/playfeel still needs user review; headless geometry checks are not a screenshot or full-suite/FPS claim. Existing gameplay maps/waves were not changed. This amendment supersedes earlier MC3-pending/primary-only-preview statements without deleting the original pass plan.

---

## MC1 + MC2 — implemented 2026-10-18; MC3 HUD/F3 now implemented; desktop review pending

Multiple independently damageable Citadels now have stable session destination objects. LevelConfig exposes ordered CitadelPaths; an empty array preserves CitadelPath. Actual additional Health/ApproachPoints Citadels are discovered in authored child order so a second objective is not silently ignored. WorldGrid reserves all physical footprints/approaches. All objectives currently share the level's authored CitadelMaxHealth, but own separate health state; the existing deployment research percentage initializes each one. Changing/adding the Citadel roster requires restarting the level. Geometry/terrain rebuilds can refresh goals and assignments for future spawns; already spawned enemies and released children keep their original identities. Removing an assigned objective retires it: enemies stop, stale distance scores become unavailable, and there is no redirect to another objective.

**Single-Citadel fast path:** assign the sole objective directly, with zero destination-comparison A*. Ordinary non-checkpoint placement retains its original single scratch flood. No per-enemy objective searches, per-frame goal scans, per-Citadel AStar grids or extra movement nodes. Available/retired state is cached through objective lifecycle events, not native-object queries in every scoring/movement call.

**Multi-Citadel policy:** PortalCitadelAssignments compares neutral navigable physical route lengths once at setup/explicit terrain rebuild, with authored-order ties. Selection uses cell-center clearance (radius zero); each actual source/radius is additionally cached and validated against clearance-aware connectivity on navigation revisions. Unsupported or blocked ships fail/retry, never select another objective. MapEdges is disabled on multi-Citadel levels; Both uses authored portals only and reports that edge sources are disabled. Single-Citadel edge/Both behavior is retained.

**Required checkpoints:** assignment minimizes the worst branch's neutral journey using deterministic, reachable canonical checkpoint representatives (first valid cached target in the source component that reaches an assigned approach). This is the documented discrete selection policy, not a claim of globally optimizing continuous circle-entry geometry. Every captured branch must remain connected. Runtime branch choices/random streams and varied routes are unchanged. Journey tail caches include destination identity; normal path requests/repaths use the assigned approach goals under the existing combined query/time budgets. DeathSpawn always inherits Citadel identity, including when InheritCheckpointProgress is false.

**Placement/outcomes:** click-time validation shares component maps per radius, protects each usable portal's assigned goal, all remaining mandatory checkpoint branches and live enemy clearance/remaining obligations. It cannot accept a different reachable Citadel as a substitute. Losing any objective emits a single session event into the existing LevelSessionController defeat/Results/mission-XP lifecycle. Results uses the failed objective's HP, never an aggregate pool that hides failure. Existing HUD still shows the primary Citadel; independent multi-objective HUD/F3 destination labels and desktop visual verification are MC3 work. Existing Level001/Level020 maps, waves and Inspector layouts were not changed.

See TESTS/Navigation/MultiCitadel.md for executed checks, measured setup/batch behavior and limitations. Original MC1–MC3 instructions below are retained; this status supersedes their earlier NOT IMPLEMENTED wording.

---

# The Citadel — Defended Destination

`Citadel.tscn` is a reusable visible placeholder, instanced in Level001 and the empty template. `WorldObjectFootprint` reserves and blocks a 3×3 grid footprint. Root position is the upper-left cell center; the visual core is offset (64,64) to the footprint center. Snap the root to 32 + 64*n on each axis.

Four editable `ApproachPoints` Marker2Ds sit one cell outside the footprint on each side. Keep them passable and reachable when painting terrain. The level's Settings/CitadelPath identifies the objective relative to the level root. Future AI should navigate toward reachable approach points, not the solid footprint.

Placeholder polygons/label are replaceable. Health, damage, defeat signals, enemy AI and combat remain unimplemented. Spawning belongs in ENEMIES.

## Live Citadel health (implemented)

`Citadel.tscn` retains its `WorldObjectFootprint` root, visuals and four navigation approach markers. The new child `Health` (`CitadelHealth.cs`) owns session HP, `Initialize(maxHealth)`, `TakeDamage(amount)`, read-only `CurrentHealth`/`MaxHealth`, `HealthChanged(current,maximum)`, and the **one-time `Destroyed` event** on transition to 0 HP. HP clamps at zero; zero/negative damage does nothing; a new level initialization restores full HP. The component itself does not implement a defeat menu, stop waves or destroy the physical objective.

The reusable `CitadelSession.cs` is attached to `Main.tscn/CitadelSession`. It reads the active `World/Grid.Level.Settings.CitadelMaxHealth`, finds the level's `Citadel/Health` via the existing `CitadelPath`, subscribes to `EnemyRegistry.EnemyArrived` (not `EnemyKilled`) and applies the arriving ship's `EnemyDefinition.CitadelDamage`. It connects `HealthChanged` to the existing HUD `SetCitadelHealth` setter. It unhooks both event sources during teardown. **F2 and wave enemies share the same arrival behavior, and kill rewards remain kill-only.** Citadel HP stays entirely owned by this component; do not store it in the HUD or EnemyDefinition.

**Per-level balance:** `LEVELS/LevelConfig.cs` exposes **Destination / Citadel Max Health** (default 1,000), saved per level in `LEVELS/Level001/Level001Config.tres` (currently 1,000) or other LevelXXX config Resources. Do not put a competing maximum-health default in this reusable Citadel scene. If a level eventually requires starting partially damaged, add a separate level-setting explicitly; today each level starts at full maximum.

**Future work:** a level/session game-state controller should subscribe to `CitadelHealth.Destroyed` and decide defeat/retry/game-over, and subscribe to final-wave completion for victory. This health-only implementation intentionally leaves waves and gameplay running at 0 HP until that pass. Add any future Citadel shields/armor as clearly separated mechanics rather than mixing them with base HP.

---

## PLANNED — MC1–MC3: Multiple Citadels with fixed portal destinations

**Status: MC1/MC2 and MC3 HUD/F3 implemented; desktop review pending.** The original accepted design brief is retained below; the current policy/verification update at the top is authoritative. Preserve all currently working single-Citadel levels, route/checkpoint features and targeting behavior. Do not treat this section as completed functionality.

### Core gameplay contract

A level can contain **two or more independently damageable Citadels**. Each registered enemy **portal** is assigned **one Citadel at level initialization**: whichever valid Citadel has the shortest **navigable neutral path** from that portal (not straight-line distance and not a randomized path-diversity profile). Cache a single **portal → Citadel** assignment and reuse it for every enemy spawning there; **never perform this destination-selection comparison per spawned enemy**.

- Once spawned, an enemy receives a **fixed AssignedCitadel** identity/reference. It never changes to another Citadel because of proximity, congestion, tower construction, path failure or even destruction of its objective. A blocked route waits/retries toward the assigned destination; it must not silently switch objectives.
- Enemies keep current corridor preference/variation, weighted profile/path diversity, lateral offsets, A* request budgeting, repathing, movement checks and optional checkpoint chains. **Destination selection is neutral; actual movement remains normally varied.**
- **First / Last tower priorities stay global:** First means the in-range eligible enemy with the **smallest remaining valid physical journey to its own assigned Citadel**; Last means the largest. Neither prioritizes a particular Citadel or switches destinations. Existing shield preference, effect eligibility, tie-break and stale/invalid-route exclusion remain in force.
- Each Citadel retains its **own CitadelHealth** and approach markers. Enemy arrival damages **only the enemy's assigned Citadel** by its existing \`EnemyDefinition.CitadelDamage\`, never all Citadels or whichever happens to be nearest at arrival.
- For v1 multi-Citadel mission rules, **losing any Citadel triggers defeat**; future mission rules (all lost / one protected / primary objective) can be added later, not as part of MC1. The current single-Citadel display and result/research integrations must continue working.
- The designer expects routes to remain buildably traversable. **Tower-placement validation must nonetheless protect each portal's assigned destination**, not merely connectivity to any Citadel; a different Citadel being reachable is not sufficient.

### Existing code and single-Citadel assumptions (checked 2026-10-18)

| Area | Current behavior | Required multi-Citadel change |
| --- | --- | --- |
| \`LEVELS/LevelConfig.cs\` | One \`CitadelPath\` and \`CitadelMaxHealth\` | Add Inspector-authored objective references (and per-objective health configuration if desired), maintaining a migration/default adapter for current single-Citadel scenes |
| \`WORLD/GRID/WorldGrid.cs\` / \`WorldLevel\` | Level-wide \`Citadel\` and reserved footprint / approach handling | Discover/reserve all actual Citadels; retain old single-Citadel behavior and don't silently drop a second objective |
| \`WORLD/NAVIGATION/NavigationGrid.cs\` | Shared \`Goals\` collected from \`Grid.Level.Citadel.ApproachPoints\` | Supply cached, destination-specific valid approach-goal sets; preserve one shared profile/AStar pool, avoid an AStarGrid2D per Citadel |
| \`ENEMIES/Spawning/SpawnManager.cs\` | Portal/edge sources; generic reachability to the current Citadel | At level setup compute and cache each portal's chosen Citadel by neutral navigable route length; assign it on real F2/wave spawns; make destination validation explicit |
| \`ENEMIES/Shared/Enemy.cs\` + \`ENEMIES/Movement/EnemyMovement.cs\` | Enemy has no Citadel identity; movement/checkpoint completion requests level's single goal set | Store immutable per-spawn destination, select that Citadel's approach goals on every route/repath, preserve across checkpoint transitions |
| \`WORLD/ROUTING/RouteCheckpoint/\` | Portal-required checkpoint chains and deterministic branch choice | Honor mandatory checkpoints and chosen branches when determining valid reachable destination; don't shortcut a checkpoint or reroll the target on repath |
| \`WORLD/OBJECTIVES/Citadel/CitadelSession.cs\` | Single \`Health\`, single HUD \`SetCitadelHealth\`; \`EnemyArrived\` damages it | Own/manage multiple independent \`CitadelHealth\` subscriptions, route arrival to the assigned one, cleanly unsubscribe; retain single-level HUD/Research compatibility |
| \`TOWERS/Shared/Targeting/TowerTargeting.cs\` | \`First\`/\`Last\` compare \`Enemy.RemainingJourneyDistance\` | Keep the scoring rule; ensure it remains correct for the enemy's chosen goal and full checkpoint/journey obligations |
| Tower placement / world navigation checks | Protect route to the one authored Citadel | Reject any construction that blocks an assigned portal → mandatory checkpoint chain → Citadel route |
| \`LEVELS/Debug/\` / F3 Route Preview | Samples routes for the current goal, plus checkpoints | Display cached portal → Citadel assignment, distinguish each Citadel's route; allow checking each portal's actual destination |

**Important:** Current shared \`NavigationGrid.Goals\` and \`EnemyMovement.RemainingJourneyDistance\` already exist; reuse these concepts instead of introducing a parallel enemy distance-tracking component. There is **no need for per-enemy objective-discovery A*** and no per-enemy per-frame scanning.

### MC1 — Objectives and cached routing foundation

- [x] Add editable **multiple Citadel references** and discover/validate their separate footprint, approach markers and health nodes. Existing maps with one \`CitadelPath\` must work **without reauthoring**; existing IDs/paths/session ownership should not be broken.
- [x] Define a stable per-level **Citadel identity** for an enemy's assigned destination. Store the resolved reference/ID on Enemy, and pass it through spawning and movement initialization. Explicitly define behavior if an authored Citadel is removed mid-session: no fallback reroute to a different Citadel.
- [x] During initial portal/source setup, compare **shortest path length through walkable grid cells** from each portal spawn exit to each eligible Citadel approach region using the **neutral** navigation profile and appropriate clearance. Cache \`portal → Citadel\`; choose a **deterministic authored-order tie-break** for equal costs. If none is reachable, report an authoring error and do not invent a destination.
- [x] Handle **different enemy radii** without changing the portal's assignment: use a documented supported-radius policy for the selection comparison and validate each spawned type's actual clearance separately. A large ship that cannot use its portal's assigned route should fail/retry safely rather than choose another Citadel. Keep this independent of the current maximum radius safeguard.
- [x] Decide explicit behavior for **MapEdges/Both** (no unique physical portal on edge): prefer cached assignments per eligible **edge entry cell / grouped source** at level setup, or clearly disable multi-Citadel edge mode until supported. Do not calculate closest Citadel independently per enemy or default all edge spawns silently to Citadel 1.
- [x] Refactor path queries to accept destination-specific approach goals. Reuse existing shared neutral/weighted AStar grids, bounded \`PathRequestManager\`, goal snapshots/caching and revision invalidation. **Do not create one AStar grid per Citadel.**
- [x] For portal-linked mandatory **checkpoint networks**, compute assignment based on the constrained route, not a forbidden direct shortcut. The current deterministic branching can yield different checkpoints per enemy, so define one deterministic **portal-level destination-selection** rule (e.g. a documented canonical checkpoint chain or reachable-chain aggregate); once assigned, individual branches must still be validated against that destination. Do not alter existing branch randomness or skip mandatory checkpoints just to get a shorter path.
- [x] Preserve assigned destination on **Transporter DeathSpawn** children via its existing route-handoff contract; children must not select a fresh Citadel at their release position.
- [x] Cache assignments for the whole level/session and recompute only on **explicit terrain/map authoring rebuild or session restart** (with clearly defined handling of already spawned enemies). **Ordinary tower placement/removal must not reassign portals.** Prefer immutable destination data plus explicit invalidation rather than checking every enemy every frame.

### MC2 — Navigation, placement and target correctness

- [x] After checkpoint completion, failed route/repath, route invalidation or congestion change, an enemy still requests goals of **AssignedCitadel**. Its assigned destination never changes. Validate the full remaining checkpoint-to-Citadel chain.
- [x] Extend tower placement connectivity to each **assigned** portal's destination, including mandatory checkpoint transitions and live enemies' remaining committed routes. Do not accept a build that merely leaves the *other* Citadel reachable. Continue using existing incremental grid update and route-validity budgets.
- [x] Confirm First/Last compare \`RemainingJourneyDistance\` **across Citadels**: e.g. A→Citadel1 remaining 300, B→Citadel2 remaining 120, C→Citadel1 remaining 500 ⇒ First=B, Last=C. Preserve First/Last shield preference and status-target eligibility. Invalidate stale estimates when destinations/checkpoint goals change.
- [x] Keep wave ownership, death rewards, kill XP, Citadel damage-on-arrival, status/weapon mechanics and normal spawn safety unchanged. Reject nonfinite or unreachable route scores per current targeting contract.
- [x] Multi-Citadel destruction should use existing mission/Results lifecycle: v1 **any Citadel destroyed → defeat** exactly once; ensure no duplicate Results/XP grants or lingering enemy/wave signals. No second overarching HP pool that masks individual Citadel failure.

### MC3 — HUD, authoring preview and verification

- [x] Update the HUD to display **each Citadel's HP** with legible labels and adapt to two or more objectives, while preserving the single-Citadel layout and existing profile/research Citadel HP bonus semantics. Define whether the research percentage affects every Citadel (proposed yes), and test it.
- [x] F3 Route Preview: show destination assignment per portal (e.g. colored destination routes/labels) alongside existing weighted corridor/path and checkpoint visualization; no extra simulation spawning or gameplay navigation mutation.
- [x] Create a **new** two-Citadel test level/fixture; do not move/modify the existing Level001/Level020 Citadel(s) or gameplay waves as a side effect of foundational support.
- [ ] Regression coverage: single-Citadel backward compatibility; one/two/three portal assignments; equal-cost tie; blocked/no valid destination; multiple approach points; neutral shortest navigable-vs-Euclidean selection; checkpoint chains and deterministic branching; portal/edge sources; shielded/unshielded enemies; Transporter children; F2 and wave spawns; dynamic tower placements; occupied/narrow corridors; per-Citadel arrival and destruction; cross-Citadel First/Last; HUD/research/Results; unload/restart/reload and performance with 500+ enemies.
- [ ] Headless Godot .NET tests plus F5 visual inspection. Record actual passes/failures and measured path setup/runtime overhead in \`TESTS/\`; do not claim these checks completed before execution.

### Implementation constraints / definition of done

Follow \`NOTES/CodePreferences.md\`: simple reusable code, explanatory script header, \`// =========================================================\` before methods, sensible \`#region\` organization and Inspector-friendly config, update relevant READMEs. Avoid extra per-enemy nodes, per-frame goal scans and wholesale refactors. Implement in **small reviewable MC1/MC2/MC3 phases** and use **one GitHub commit per implementation job**.

**Done only when:** multiple independently damageable Citadels function in an authored test mission; every spawn gets its portal's cached fixed destination; normal varied movement, mandatory checkpoints and DeathSpawn inheritance respect that destination; First/Last compares remaining journeys correctly; tower placement cannot cut the assigned route; independent HP/HUD and one-time defeat work; and existing one-Citadel levels/test suites still pass.


## Planned click-to-collect Citadel Credits (CP1; NOT IMPLEMENTED)

Detailed shared mechanics and implementation checklist: [`ECONOMY/Production/README.md`](../../../ECONOMY/Production/README.md). Verified old GameMaker code uses per-Citadel **80 Credits capacity, +10 every 2 seconds, active-wave-only**, click to collect; its collection function resets **all** Citadels at once. **New design instead uses independent buffers and collection per Citadel**, shared wallet `Grant`, existing wave pause/start/clear states, and an event-driven green credit meter without duplicating the Citadel health system or intercepting unrelated build/select clicks. Don't mistake the older childhood notes' 40/15s for the final coded old-game values.
