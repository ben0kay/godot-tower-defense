# Enemy Movement — Route Preview Overlay and Portal Checkpoint Areas

> **STATUS: ROUTE PREVIEW IMPLEMENTED; CHECKPOINTS STILL PLANNED.** F3 opens the debug route panel. The first-pass direct portal-to-Citadel overlay is implemented; checkpoint nodes/stages and checkpoint-aware previews below remain future specifications. Read the latest GitHub commit, `NOTES/CodePreferences.md`, `ENEMIES/Movement/README.md`, `WORLD/NAVIGATION/README.md`, `WORLD/PORTALS/EnemyPortal/README.md` and the relevant scripts before implementing. Preserve existing movement, combat, terrain, wave, and placement behavior.

## Why this is needed

Example Level002: five widely separated enemy portals are in an open western area; the eastern half is a maze with several long, parallel routes, some three cells wide and some just one cell. All enemies must reach a Citadel on the east.

The current **Path Diversity** system uses multiple deterministic, smoothly weighted A* route profiles across the **entire map**, not a portal-relative route restriction. With strong weights, a ship from the **top** portal may choose a long detour to enter a **bottom** maze corridor. This can be technically valid but visually illogical.

Desired result:
- Enemies from upper portals **generally use upper routes**; middle portals favor the middle; lower portals favor lower routes.
- Different enemies from the *same* portal can still choose several sensible routes.
- Existing **Path Diversity** (macro route choices), **Corridor Variation** (wider geometric spreading inside a walkable route) and **Path Offsets** (small lateral displacement) continue to operate **independently**.
- A level designer can **preview many possible routes immediately**, rather than repeatedly spawning enemies.

The user has requested **two features**: (1) a path-simulation/debug overlay, and (2) optional portal-linked checkpoint areas. They should integrate cleanly but remain useful independently.

## Current systems to reuse — already implemented

- `ENEMIES/EnemyMasterConfig.cs` / `DefaultEnemyConfig.tres`: `EnablePathDiversity`, `PathDiversityStrength`, `RouteProfileCount`, `PreferencePatchCells`, `RoutePreferenceSeed`, `EnableCorridorVariation`, `CorridorVariationPercent`, `CorridorVariationStrength`, `EnablePathOffsets`, `PathOffsetMaxPixels`, and navigation work budgets.
- `EnemyDefinition` already has **three independent per-enemy permissions**, all default true: `AllowPathDiversity`, `AllowCorridorVariation`, `AllowPathOffsets`. They are **implemented**, not part of this task.
- `NavigationGrid` has shared weighted A* profiles **and a truly neutral unweighted profile** (`NeutralProfile = -1`). Do not use weighted profile 0 as a substitute for shortest unweighted routing.
- `PathRequestManager` owns bounded A* work, multiple Citadel approach goals, route cost comparison, queued requests and selective invalidation.
- `EnemyMovement` / `EnemyRoute` own per-ship assignments, valid handoffs, offset/corridor refinement, cached dependencies, no-clipping movement, etc.
- `EnemyPortal` owns a visible portal/SpawnExit, and `SpawnManager` knows the **actual portal chosen for each spawned enemy**. `WorldLevel` owns the authored level scene and portals.

**Do not replace the shared A* engine, build one A* grid per enemy, force every enemy to use all three variation styles, or add per-frame random path jitter.**

---

## Feature 1 — Route Preview / Path Simulation Overlay

### Implemented first-pass controls

Load Main/Level001 or MainLevel002/Campaign Level002 normally, then press **F3**. Choose a registered active portal, catalog enemy type, **1–50 paths per portal**, and **Final Refined** (default) or **Raw A***. Click **Simulate Paths**. Toggle the white neutral-shortest comparison; use **Clear Paths** or F3 to remove the lines. F3 closes both panel and overlay and cancels pending work. No gameplay enemies are spawned.

Colored samples show profile ID, physical distance, percentage difference from neutral and exact duplicate labels. The neutral baseline uses the same start/approach goals and radius with unweighted costs and no refinements. Samples use preview-only deterministic seeds and offsets; they illustrate plans, not exact spawn jitter, future traffic or timing. Start is the selected portal's real centered SpawnExit. Congestion is frozen when Simulate is pressed; live crowd updates cannot change a batch halfway through.

Separate Inspector debug budgets on the level's Debug node default to one A* query / soft 1 ms per physics frame. Completed lines are cached. Input, map, source or relevant Inspector setting changes clear old results; press Simulate again. Changing inputs mid-calculation never combines old/new samples. No background work while closed and no permanent gameplay HUD panel. Runtime debug gameplay is supported; editor-only F6 map scenes without world services do not provide the panel. Checkpoint controls remain deferred. ALL PORTALS is implemented.

### Preview-only experimentation

Enable/disable Path Diversity, set Strength (0–3), and choose Weighted Profiles (1–16) inside F3. These use a private settings copy and private shared preview navigation pool, never writing .cs/.tres files or changing live enemy path weights. Reset to Game Settings recopies current normal settings. The legend shows tested values for manual configuration later. Select ALL PORTALS for 1–50 paths **per portal**, color-grouped with unique counts. No Apply-to-Game or save action exists.

### Purpose

In a running debug game (and ideally usable with existing level editing workflows), allow selecting one portal and drawing a chosen number of **candidate paths from that portal toward the Citadel** over the actual map. The preview should use the game's real routing weights, map obstacles, tower occupancy, Citadel approach goals, current settings and optional checkpoint links. It is **not** a synthetic sketch or full simulated ship movement.

### Suggested user controls

- Master debug **Show Route Preview** on/off; **no overlay in normal gameplay when disabled**.
- Choose a portal from the level's registered active portals; **All Portals** is available.
- Choose a reference enemy type or clearance radius and respect its three movement permission flags; default to a basic enemy.
- Select a bounded **sample count**, 1–50 previews per portal. Show **profile ID** (including neutral), distinct line colors, a concise legend, and clearly labeled start/checkpoint/Citadel.
- Modes: **Raw A* route** (priority), then optional **Final Refined Route** (showing Path Offsets + Corridor Variation with deterministic sample seeds). A distinct toggle for checkpoint circles and stage boundaries.
- A **Refresh Preview** action should redraw using live config; optional refresh on relevant settings/level edits or changed tower occupancy is acceptable if debounced/budgeted.
- Display useful diagnostics: calculated/failed paths, selected profile, approximate physical route length vs neutral shortest route, checkpoint state, and errors such as unreachable checkpoints.

### Required behavior

1. Query real `NavigationGrid` / existing route builders, with deterministic **preview-only** profile/seed assignments. Draw the actual returned waypoints as polylines. Samples need not all differ: if profiles choose the same route, show this honestly (or indicate duplicates), rather than inventing variation.
2. Keep shared live spawn/profile counters, wave RNG, enemy placement, tower stats, wave scheduling, route queues, source definition resources, enemy registry and **live enemy routes unchanged**. Running a preview must not spawn actual enemies or consume gameplay RNG. Preview-only routing may use existing shared profiles in a read-only/controlled way.
3. Use the **actual chosen portal SpawnExit**, current map cell solidity, the correct enemy radius and the current Citadel approach markers. If a checkpoint is linked, preview **Portal → checkpoint area → Citadel**, not direct-to-Citadel.
4. Respect global master controls and per-type permissions. If diversity is denied, show **neutral A***; disabling offsets/corridor should show appropriately unrefined route geometry. Draw safety-valid lines only.
5. Do not compute 50 routes × all portals × every Citadel/checkpoint target **each frame**. Bound preview calculations per frame using separate debug work budgets and cache results until the portal, settings, selected enemy, checkpoint geometry or navigation revision changes. It must remain usable with 500+ enemies. Debug drawing should be batched efficiently in one owner/overlay, not a node per path segment.
6. Show clearly when a preview is a **route plan** rather than a prediction of exact enemy timing/traffic. With the optional congestion influence, indicate the snapshot nature of previewed weights.

**Implementation suggestion:** a level/debug-owned `RoutePreviewOverlay.cs` under `ENEMIES/Movement/` (or a small `LEVELS/Debug/` counterpart if cleaner), drawing via `_Draw` in world coordinates; optional debug UI controls in the existing level Debug node, not a new permanent gameplay HUD panel. Do not modify `WorldGrid` into a giant debugger.

---

## Feature 2 — Optional portal-linked RouteCheckpoint areas

### Goal and example

Let the level designer place **invisible in-game checkpoint nodes** that are visible as circles/gizmos in the editor or debug mode. Each portal can optionally reference a checkpoint area.

Example:
- Top portal → **Top Checkpoint** near the upper maze entrance → Citadel.
- Middle portal → **Middle Checkpoint** near central lanes → Citadel.
- Bottom portal → **Bottom Checkpoint** near the lower maze entrance → Citadel.

For enemies spawned from a linked portal, the broad route must visit the assigned checkpoint **area** before proceeding to the Citadel. This prevents strong weighted Path Diversity from taking a top-portal enemy all the way to an unrelated lower corridor **before reaching its upper checkpoint**, while still allowing natural diversity in each stage.

### Authoring and node ownership

- Add a reusable, lightweight `RouteCheckpoint` Node2D/scene, with an Inspector-editable `RadiusPixels` (or `RadiusCells`) for a **region**, not an exact point. Consider a designer-friendly editor gizmo/circle that is hidden in regular gameplay. This **does not need an Area2D/physics collision** to function.
- Store an **optional checkpoint reference/path per EnemyPortal**. Use Godot-safe relative NodePaths or another explicit inspected scene reference that works across instanced level scenes. A missing/unassigned checkpoint means **100% existing normal routing**.
- The scene should group markers under an optional `RouteCheckpoints` Node2D; multiple portals may share a checkpoint; do not introduce unnecessary global IDs or per-enemy script subclasses.
- Use the **actual portal chosen by SpawnManager** to give the spawned enemy its route plan **before its initial path is requested**. No inference based on enemy type, filename, proximity to a portal, or catalog order.
- Treat `WorldObjectFootprint` and build/occupancy reservations carefully: checkpoint markers are **guidance only**, do not block enemies, reserve cells, create walls or change build permission.

### Routing behavior: staged, radius-aware, and safe

1. **Stage A: Portal → Checkpoint area.** Choose one or more **reachable, clearance-valid** candidate cells/waypoints **inside the circle**. This should be a soft geometric target area with a **hard requirement to visit it**, not a hard exact-pixel waypoint. The A* path must be actually routed into the area, not merely detect proximity to the checkpoint center or draw a straight connector through walls. Deterministic per-enemy selection or cost-based candidates may spread traffic within the area.
2. **Stage B: Checkpoint area → Citadel.** Once the enemy **physically reaches a valid part of the checkpoint area**, permanently mark that checkpoint complete for that enemy and request a normal route toward any valid Citadel approach marker. Do not repeatedly switch back to Stage A, oscillate at the radius boundary, or credit a checkpoint merely because a calculated line crosses it before the ship moves.
3. The **selected A* profile**, per-enemy permissions, corridor refinement, offset preferences, physical clearance and corner validation apply to **both legs**. Keep the same stable per-enemy preferences across the stage transition and ordinary repaths; do not reroll profiles each leg. Final routes must avoid walls, towers and invalid diagonal corners.
4. **Unreachable checkpoint policy (initial version):** do not silently bypass a *required* checkpoint and proceed to the Citadel. Fail gracefully (wait/retry with existing bounded path-request behavior) and expose an informative warning/preview diagnostic. Define configuration validation so accidentally placed-in-wall or disconnected checkpoints are visible to the designer. Optional **preferred/soft checkpoint** fallback could be added in a *future* pass, not mixed into the first implementation.
5. Route changes caused by building towers must still honor **Portal → Checkpoint → Citadel**; replan toward whichever stage is currently active. Preserve existing movement safe-prefix/handoff and path-invalidation rules. The checkpoint is not marked complete by a route calculation or the enemy reaching an unrelated path near it.
6. A large checkpoint radius must not cause huge repeated scans or a costly A* per enclosed tile. Validate/cache a bounded set of possible targets when checkpoints/terrain change, respect enemy clearance, reuse shared A* and central count/time budgets, and avoid any per-tick global scan. Checking the current ship against **its own** checkpoint region during movement is fine.
7. Portal-less/map-edge spawning continues unchanged (no checkpoint by default). F2/debug portal spawns and authored wave spawns should honor the same portal's checkpoint if present.

### Clarification: area radius versus global route deviation

A checkpoint defines a **required region** through which the ship must travel; it does **not** guarantee every later segment remains near that region, nor does it force a designer-defined centerline. After reaching the top checkpoint a ship may still take a longer downstream route if weights strongly favor it. If playtesting reveals that is still visually implausible, evaluate a **separate, optional maximum weighted-route detour bound** relative to a neutral shortest route. Do not silently add such a constraint or force unrelated portal routes into rigid lanes during this initial pass.

---

## Recommended implementation order

1. **Completed first pass: debug route overlay** using the existing weighted and neutral profiles; show direct portal-to-Citadel routes, editable sample count and profile/length diagnostics. This provides a quick way to see whether the current strong Path Diversity is creating strange detours.
2. **Second pass: portal checkpoint areas**, with Inspector links, per-ship stage state, reachable radius-based targets, validation and repath correctness.
3. **Third pass: checkpoint-aware previews**, reusing the *actual* checkpoint routing plan, not a separate simulator. Show both route legs and the checkpoint radius.

This order helps debug each feature separately. Combining them in one Codex change is possible, but keep code modular and independently testable.

## Acceptance checks / regressions

- With **no checkpoint** assigned, the old system's portal → Citadel path and wave/F2 behavior remain unchanged.
- With top/middle/bottom portal checkpoint links, spawned ships must visit the correct **checkpoint region** before heading to Citadel; none can pass a checkpoint in Stage A merely by A* planning it.
- Checkpoint **radius** permits several actual valid waypoint choices; targets inside solid/unreachable cells are rejected. Checkpoint intersection/entry must respect real movement and enemy radius.
- Test all combinations of existing per-enemy **Diversity / Corridor / Offset** permission flags and their **global master toggles**, including the shared neutral profile. Checkpoint stage changes must not reset seeded preferences.
- Tower placement can close a segment or make a checkpoint temporarily inaccessible; affected ships replan or safely wait/retry. Existing route validity, multi-goal selection and arrival logic continue functioning.
- Preview produces stable, accurate routes from a chosen portal with repeated settings; it never affects subsequent enemy profile assignments, wave RNG, spawns, Credits, rank XP or other gameplay.
- Preview supports **1-cell and 3-cell corridors**, small and medium ship radii, disconnected destinations and zero possible routes. Never draw unsafe connectors across impassable tiles.
- Performance: no per-checkpoint physics nodes; no per-frame route regeneration, per-enemy A* grids, unbounded radius target searches or separate duplicate pathfinding implementations. Reuse centralized path budgets and viewport-limited debug drawing.
- Run Godot .NET compile and relevant `TESTS/MovementPermissionChecks.cs`, corridor, navigation, wave, portal-spawn, placement, interactive terrain and full integration suites; report actual results. Document new exported fields and how to place/link a checkpoint in the editor.

## What Codex should deliver when requested

A clear, reproducible **route-preview debug mode** plus **optional portal-associated checkpoint regions**, with self-contained small reusable files, Inspector authoring, tests, and brief updates to `ENEMIES/Movement/README.md` and `WORLD/PORTALS/EnemyPortal/README.md`. Do **not** refactor the entire enemy navigation system or alter existing Level001/Level002 art/waves just to add the feature.

**Route preview is implemented and tested; checkpoint functionality remains planned.** See `TESTS/RoutePreview.md` for actual verification and known upstream issues.
