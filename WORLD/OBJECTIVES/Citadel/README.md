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

**Status: NOT IMPLEMENTED.** This is the accepted design brief for a future Codex pass. Preserve all currently working single-Citadel levels, route/checkpoint features and targeting behavior. Do not treat this section as completed functionality.

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

- [ ] Add editable **multiple Citadel references** and discover/validate their separate footprint, approach markers and health nodes. Existing maps with one \`CitadelPath\` must work **without reauthoring**; existing IDs/paths/session ownership should not be broken.
- [ ] Define a stable per-level **Citadel identity** for an enemy's assigned destination. Store the resolved reference/ID on Enemy, and pass it through spawning and movement initialization. Explicitly define behavior if an authored Citadel is removed mid-session: no fallback reroute to a different Citadel.
- [ ] During initial portal/source setup, compare **shortest path length through walkable grid cells** from each portal spawn exit to each eligible Citadel approach region using the **neutral** navigation profile and appropriate clearance. Cache \`portal → Citadel\`; choose a **deterministic authored-order tie-break** for equal costs. If none is reachable, report an authoring error and do not invent a destination.
- [ ] Handle **different enemy radii** without changing the portal's assignment: use a documented supported-radius policy for the selection comparison and validate each spawned type's actual clearance separately. A large ship that cannot use its portal's assigned route should fail/retry safely rather than choose another Citadel. Keep this independent of the current maximum radius safeguard.
- [ ] Decide explicit behavior for **MapEdges/Both** (no unique physical portal on edge): prefer cached assignments per eligible **edge entry cell / grouped source** at level setup, or clearly disable multi-Citadel edge mode until supported. Do not calculate closest Citadel independently per enemy or default all edge spawns silently to Citadel 1.
- [ ] Refactor path queries to accept destination-specific approach goals. Reuse existing shared neutral/weighted AStar grids, bounded \`PathRequestManager\`, goal snapshots/caching and revision invalidation. **Do not create one AStar grid per Citadel.**
- [ ] For portal-linked mandatory **checkpoint networks**, compute assignment based on the constrained route, not a forbidden direct shortcut. The current deterministic branching can yield different checkpoints per enemy, so define one deterministic **portal-level destination-selection** rule (e.g. a documented canonical checkpoint chain or reachable-chain aggregate); once assigned, individual branches must still be validated against that destination. Do not alter existing branch randomness or skip mandatory checkpoints just to get a shorter path.
- [ ] Preserve assigned destination on **Transporter DeathSpawn** children via its existing route-handoff contract; children must not select a fresh Citadel at their release position.
- [ ] Cache assignments for the whole level/session and recompute only on **explicit terrain/map authoring rebuild or session restart** (with clearly defined handling of already spawned enemies). **Ordinary tower placement/removal must not reassign portals.** Prefer immutable destination data plus explicit invalidation rather than checking every enemy every frame.

### MC2 — Navigation, placement and target correctness

- [ ] After checkpoint completion, failed route/repath, route invalidation or congestion change, an enemy still requests goals of **AssignedCitadel**. Its assigned destination never changes. Validate the full remaining checkpoint-to-Citadel chain.
- [ ] Extend tower placement connectivity to each **assigned** portal's destination, including mandatory checkpoint transitions and live enemies' remaining committed routes. Do not accept a build that merely leaves the *other* Citadel reachable. Continue using existing incremental grid update and route-validity budgets.
- [ ] Confirm First/Last compare \`RemainingJourneyDistance\` **across Citadels**: e.g. A→Citadel1 remaining 300, B→Citadel2 remaining 120, C→Citadel1 remaining 500 ⇒ First=B, Last=C. Preserve First/Last shield preference and status-target eligibility. Invalidate stale estimates when destinations/checkpoint goals change.
- [ ] Keep wave ownership, death rewards, kill XP, Citadel damage-on-arrival, status/weapon mechanics and normal spawn safety unchanged. Reject nonfinite or unreachable route scores per current targeting contract.
- [ ] Multi-Citadel destruction should use existing mission/Results lifecycle: v1 **any Citadel destroyed → defeat** exactly once; ensure no duplicate Results/XP grants or lingering enemy/wave signals. No second overarching HP pool that masks individual Citadel failure.

### MC3 — HUD, authoring preview and verification

- [ ] Update the HUD to display **each Citadel's HP** with legible labels and adapt to two or more objectives, while preserving the single-Citadel layout and existing profile/research Citadel HP bonus semantics. Define whether the research percentage affects every Citadel (proposed yes), and test it.
- [ ] F3 Route Preview: show destination assignment per portal (e.g. colored destination routes/labels) alongside existing weighted corridor/path and checkpoint visualization; no extra simulation spawning or gameplay navigation mutation.
- [ ] Create a **new** two-Citadel test level/fixture; do not move/modify the existing Level001/Level002 Citadel(s) or gameplay waves as a side effect of foundational support.
- [ ] Regression coverage: single-Citadel backward compatibility; one/two/three portal assignments; equal-cost tie; blocked/no valid destination; multiple approach points; neutral shortest navigable-vs-Euclidean selection; checkpoint chains and deterministic branching; portal/edge sources; shielded/unshielded enemies; Transporter children; F2 and wave spawns; dynamic tower placements; occupied/narrow corridors; per-Citadel arrival and destruction; cross-Citadel First/Last; HUD/research/Results; unload/restart/reload and performance with 500+ enemies.
- [ ] Headless Godot .NET tests plus F5 visual inspection. Record actual passes/failures and measured path setup/runtime overhead in \`TESTS/\`; do not claim these checks completed before execution.

### Implementation constraints / definition of done

Follow \`NOTES/CodePreferences.md\`: simple reusable code, explanatory script header, \`// =========================================================\` before methods, sensible \`#region\` organization and Inspector-friendly config, update relevant READMEs. Avoid extra per-enemy nodes, per-frame goal scans and wholesale refactors. Implement in **small reviewable MC1/MC2/MC3 phases** and use **one GitHub commit per implementation job**.

**Done only when:** multiple independently damageable Citadels function in an authored test mission; every spawn gets its portal's cached fixed destination; normal varied movement, mandatory checkpoints and DeathSpawn inheritance respect that destination; First/Last compares remaining journeys correctly; tower placement cannot cut the assigned route; independent HP/HUD and one-time defeat work; and existing one-Citadel levels/test suites still pass.
