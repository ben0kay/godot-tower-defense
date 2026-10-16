# Enemy Pathing — Corridor-Aware Individual Route Variation (Phase N4, IMPLEMENTED — visual playtest pending)

> **Codex implementation specification — NOT IMPLEMENTED.** This document lives in `ENEMIES/` and is linked from `ENEMIES/README.md`. It is a focused optional navigation feature, separate from Enemy E2A/E2B abilities and tower work. First read `NOTES/CodePreferences.md`, `ENEMIES/Movement/README.md`, `WORLD/NAVIGATION/README.md` and the existing C# scripts before changing code.

## Objective

The level designer can build a **narrow 1-cell corridor**, a **3-cell-wide hallway**, a larger open route, or a winding irregular passage. Enemies should use the available walkable space naturally rather than form a perfectly straight centerline whenever a Portal and the Citadel approach are horizontally aligned.

Preserve the **existing A* route toward the Citadel** as the authoritative safe path. Add an **optional, per-enemy, randomized corridor-aware waypoint refinement** that varies where some ships travel *within* the safe passage, with smooth, stable movement and no wall/tower clipping. An enemy does **not** need a personal A* grid.

The player/designer can choose what **percentage of spawned enemies** use this new refinement. Enemies not selected must follow the current route/refinement code **unchanged**, not a subtly modified approximation of it. Different Enemy001 ships from the **same portal** should be capable of visibly different routes even in an otherwise unobstructed 3-cell-wide corridor.

**Separate concepts:**

- `EnablePathDiversity` / `PathDiversityStrength` / `RouteProfileCount` (existing): smooth weighted **A*** preferences for choosing broad paths through the map.
- `EnablePathOffsets` / `PathOffsetMaxPixels` (existing): a small, persistent lateral offset (default maximum 8px; the grid cell is 64px).
- **New Phase N4 corridor variation:** stronger but validated **individual lateral waypoint choices** *within an existing safe route's traversable space*. This is what should make three-cell-wide passages visually populated across their width.
- `SeparationStrength` (existing reserved/inactive): future physical separation. **Do not implement crowd collision/separation as part of N4.**

This is **not** an authored route lane system, a new steering physics simulation, or a guarantee that every ship will follow a different path. A* may always choose the centerline; N4 allows selected ships to refine it safely. When only one viable route fits the enemy's actual radius, fall back to it.

## Exact Inspector contract — required

Add the following under a new `[ExportGroup("Corridor Path Variation")]` in **`ENEMIES/EnemyMasterConfig.cs`**, authored through the existing **`ENEMIES/DefaultEnemyConfig.tres`**:

| Inspector field | Type | Suggested default | Purpose |
| --- | --- | --- | --- |
| `EnableCorridorVariation` | bool | **false** (safe opt-in initially) | Master switch for new corridor-aware refinement |
| `CorridorVariationPercent` | float or int, 0–100 | **50** | Percentage chance an individual enemy is assigned the new refinement |
| `CorridorVariationStrength` | float, bounded 0–1 | **0.65** | How far a selected enemy may explore safe available corridor width |
| `CorridorVariationSpacingCells` | int, reasonable bounded range | **3** | Rough spacing between preferred lateral deviations, to avoid twitchy tile-by-tile zigzags |

Defaults are initial playtest suggestions, **not** final balance. Keep every setting Inspector-editable; clamp/validate malformed runtime values. Add only further tuning exports if a concrete test demonstrates they are necessary. Reuse the current navigation budget settings and existing `MaxPathCalculationsPerFrame`.

**Master-toggle and percentage behavior:**

| Enable | Percent | Required behavior |
| --- | ---: | --- |
| false | any value | **100% current behavior**, including normal weighted A*, current path offsets and repathing |
| true | 0 | **100% current behavior**; skip N4-specific route refinement |
| true | 25 | Expected ~25% selected for N4, ~75% use current behavior |
| true | 50 | Expected ~50% selected for N4, ~50% use current behavior |
| true | 100 | All successfully initialized ships are selected for N4; their particular routes may still fall back safely in narrow corridors |

The percentage is an **independent per-spawn probability**, not a strict exactly-50-of-every-100 quota. A wave of 100 at 50% might randomly assign 47. F2/debug ships may participate according to the **same** config unless an explicit test-only override is supplied. An enemy type should not need an N4-specific C# class.

### One assignment per individual ship

At spawn/initialization, assign **one stable** `UsesCorridorVariation` boolean using the master switch and percent. Give the selected ship a **stable independent random seed or a few small preference values** used when building its routes; don't sample fresh random values every physics tick, and don't reuse a global random selection on each repath.

- Prefer deterministic seeded assignment with a documented per-spawn seed/counter so automated tests can reproduce a distribution. Avoid using the global `GD.Randf()` state when doing so would perturb authored wave composition or unrelated RNG.
- A selected enemy **remains selected during repaths**, and an unselected one stays on original behavior for its lifetime. Preserve relevant lateral preferences through route replacement: don't abruptly flip to an unrelated lane whenever a tower is placed.
- The master switch must guarantee original behavior at **level startup** and for **new spawns**. For live Inspector edits during an ongoing wave, document and implement an explicit safe refresh or state that active cached routes only change on next safe rebuild/respawn; **do not force synchronous A*** for all living ships when a toggle flips.

**Suggested minimal API shape (illustrative, not literal required signatures):**

```csharp
// Initialized once, not rerolled during every physics frame:
bool useCorridorVariation = config.EnableCorridorVariation
    && seededRandom.Randf() < Mathf.Clamp(config.CorridorVariationPercent, 0f, 100f) / 100f;

// When accepting a valid A* route:
if (useCorridorVariation && config.EnableCorridorVariation)
    route.BuildCorridorVariation(...);
else
    route.Build(...); // Existing exact implementation unchanged
```

Don't maintain a separate long-lived `AStarGrid2D` for every enemy. Avoid a global branching rewrite of `NavigationGrid.FindPath` just to vary corridor offsets.

## Route generation: safety and visual goals

1. **Find the logical route as today.** The existing pooled weighted `AStarGrid2D`, PathRequestManager budget, portal selection, Citadel `ApproachPoints`, search fairness and route revision handling remain intact. The N4 code works primarily where `EnemyRoute.Build()` currently refines a newly accepted route.
2. **Sample usable sideways space.** At a sparse selection of interior route positions, measure candidate offsets on **both sides** of the local forward direction, with actual `Enemy.Definition.RadiusPixels` clearance against `WorldGrid` boundaries, walls, occupied tower cells, Portal/Citadel footprints and corners. Do not assume every 3-cell painted band permits a particular offset, and do not blindly infer width from rendered artwork.
3. **Pick individual but coherent deviations.** Choose seeded lateral preferences and modest waypoint separation so some ships favor upper space, some lower and some center; allow gradual changes over the route. Avoid a fixed 4-lane profile or a persistent "all even IDs go high" rule. Variation should be noticeable within a 3-cell-wide corridor yet **smooth rather than random zigzags**.
4. **Connect safely.** Validate every resulting travel segment—`HasClearance`, `SegmentClear` and `ConnectionClear`/corner rules as appropriate. Merely checking that a candidate waypoint lies in a free cell is insufficient: the straight line *between* points can cut a wall. For curved visuals, approximate the curve with safe traversable segments; never draw a spline and let movement cut across unsafe shortcuts.
5. **Maintain progress toward Citadel.** Keep original spawn/origin and valid goal endpoints, prefer forward progress, bound extra path length/detour amplitude and avoid oscillations/backtracking just for visual randomness. Detours must not force repeated A* or navigation goal switching. Movement uses the validated cached waypoints and its existing `MoveToward`/turn smoothing.
6. **Constrained corridors and big ships:** In a genuine 1-cell-wide corridor, use zero or tiny permissible deviation. In wide corridors, use more of the available width. A physically larger enemy gets less lateral freedom automatically because all checks use its own radius. If a proposed deviation cannot be validated after a small, bounded candidate budget, fall back to the original safe `EnemyRoute.Build` without a stuck ship.
7. **Route replacement and dynamic towers:** Preserve existing `ConnectRoute` handoff, still-safe prefix behavior, navigation revision watchers, dependency indexing, urgent replans and no-tunneling checks. After refinement, recompute `RouteDependencies`, waypoint suffix lengths and `RemainingPathDistance` from the **actual traveled path**, so tower "First" priority and tower-placement safety keep working. Do not allow a selected route to bypass blocked cells without a navigation revision.
8. **No visual jitter or overlaps requirement:** Variations are settled at path-build/repath time; ships do not do expensive per-tick nearby-ship checks. Multiple ships may still overlap; true separation/collision avoidance remains a separate optional feature.

**Important design decision:** Do **not** implement this by mutating and restoring random costly cells on a shared `AStarGrid2D` during every enemy query in N4. That could be a later separate experiment for broad route selection, but it adds multi-goal cost-consistency and shared-profile state risks. The first N4 version should use **bounded corridor-aware waypoint refinement on top of the current safe A***. If this cannot yield a sufficiently natural result without substantial detours, document the limitation before escalating complexity.

## Performance requirements

- No per-enemy A* grid, no per-frame A* and no per-tick random-offset regeneration.
- No per-frame full-world grid scans, enemy-pair interactions, constant array rebuilds or scene-tree lookups.
- Use a **small fixed candidate budget per selected waypoint**, sparse spacing, existing grid clearance APIs and cached world services; avoid quadratic comparisons over all route points.
- 0%, master-off and unselected enemies must incur **no N4 waypoint-sampling work**. Their normal path, navigation counts, movement and profile preferences should be behaviorally unchanged.
- Keep existing `MaxPathCalculationsPerFrame`, millisecond budgets, 500-enemy swarm validation and selective-repath performance behavior. Benchmark both **path-assignment cost** and **normal moving-frame cost** at 0/50/100%, for a straight 3-cell corridor and a constrained winding corridor. Record real timings; don't claim "free" or an exact FPS impact without measurement.
- A few different weighted `RouteProfile` values remain shared; N4 is **per-instance** and orthogonal to that profile assignment.

## Required Codex tests and playtest acceptance

1. **Toggle parity:** master off or percent 0 reproduces the same original path/refinement behavior, with no changed placements/goal arrivals or new computational work. Master on at 100 never chooses the old branch except an explicit **safe geometry fallback**.
2. **Distribution and stability:** at 50%, deterministic seeded tests assign roughly half of a large sample; no requirement for an exact quota. Different instances of the *same* Enemy001 may choose different routes from the same portal. Selection and preferences are stable under forced repaths; other RNG streams and wave composition are unchanged.
3. **Visual movement:** in a level fixture with a passable **3-cell-wide straight corridor** and horizontally aligned Portal/Citadel approach, some selected ships visibly use upper/lower/central traversable space over sustained stretches. The result must not be just a jitter of a few pixels or an every-cell zigzag. One-cell-wide corridors remain safe and essentially unvaried. Test a winding three-cell corridor, an open area, obstacles/tower placements and different `RadiusPixels`.
4. **Geometry and lifecycle:** no crossing blocked tiles, cutting corners, moving outside map, intersecting occupied tower cells, entering the solid Citadel footprint, stale target references, incorrect goal arrival or permanent stopped routes. Block a corridor *while ships are traveling* and verify safe route invalidation/handoff/rebuilding and unchanged Citadel health/rewards.
5. **Integration:** preserve `TowerTargeting.Priority.First` semantics, EnemyRegistry, wave-owned completion, shield-on-spawn, Enemy002 speed, ShieldGenerator abilities, death feedback, tower catalog/payment/upgrades, and menu/gameplay regressions. Do not reimplement E2A/E2B, tower 4C work or ship separation.
6. **Performance:** run existing `TESTS/Integration/EnemyIntegrationChecks.tscn`, `RepathMovementChecks`, `SmoothRepathChecks`, `SwarmNavigationChecks` and `PlacementPerformanceChecks.tscn`. Add a focused N4 fixture; verify 100 and 500 active ships with 0/50/100% selection and report measured path build/normal frame timings. Headless measurements are **not** a substitute for an in-editor moving-swarm visual inspection.

## Scope and delivery

**Preferred small implementation footprint:**

- `ENEMIES/EnemyMasterConfig.cs` + `DefaultEnemyConfig.tres`: Inspector toggle, percentage and a couple of bounded tuning knobs.
- `ENEMIES/Movement/EnemyMovement.cs`: one-time per-instance choice/seed and route-builder branch; keep the rest of movement intact.
- `ENEMIES/Movement/EnemyRoute.cs`: optional corridor-aware refinement with safety/fallback and correct cached dependencies/distances. Extract a **small helper** into `ENEMIES/Movement/` if it improves readability; do not create a second navigation subsystem.
- Existing `WORLD/NAVIGATION/` and path manager: only narrow adjustments if demonstrably necessary; preserve owned A* data, budget management and revision notifications.
- `TESTS/`: focused correctness, distribution and performance regression coverage.
- Update `ENEMIES/Movement/README.md`, `ENEMIES/README.md` and `TESTS/README.md` **after** implementing so documentation accurately describes what's live.

**Codex order:** First implement the master switch and deterministic per-instance percentage assignment (test 0/50/100%). Second, implement route refinement for selected ships and safety fallback (test width, obstacles and repaths). Third, run regression/performance checks and manually inspect moving ships. Commit/push only after verifying and report anything that cannot be tested.

**Done means:** With `EnableCorridorVariation = true` and `CorridorVariationPercent = 50`, approximately half of each large group of otherwise identical enemies uses safe, individually varied routes across available corridor width while the others retain the existing pathing. With the master toggle off, the game follows the current routing system without this feature. 

## Implementation status

N4 master/percentage assignment was implemented and verified independently before corridor refinement. Automated geometry, safety, stable preferences, disabled parity and 100/500-ship performance checks now pass; existing navigation/combat/tower regression suites are preserved. Implementation details are in Movement/README.md, configuration in EnemyMasterConfig/DefaultEnemyConfig, and measured results in TESTS/Performance/CorridorPerformance.md.

A moving-window visual playtest could not be performed in the headless execution environment. Visual acceptance remains pending: enable the feature, compare 0/50/100% in straight/winding three-cell corridors, inspect turns/spread and place towers while ships move. Do not interpret CPU-only timings as a rendered 60FPS guarantee.
