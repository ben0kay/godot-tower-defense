## Shared finished-route pool — 2026-10-20

Enemies still use the existing weighted/neutral profiles, but compatible requests now also reuse raw A* paths and a finite set of finished corridor/offset variations. This is a performance change, not more profiles or new movement physics. No saved terrain, wave recipes, speeds, combat or corridor master settings were changed.

Settings: select `ENEMIES/DefaultEnemyConfig.tres` → **Shared Route Pool**. `EnableSharedRoutePool` defaults true; `SharedRouteVariants` defaults8 (1–32); `SharedRouteCacheEntries` defaults256 per cache (16–2048). Master off restores the original per-ship seed/offset builders for subsequent assignments. Existing accepted routes continue safely until their ordinary next assignment. Corridor/offset/diversity per-type permissions and global masters remain independent.

The pool belongs to each NavigationGrid/world, including a private pool for F3 preview grids. Raw keys use start cell, goal cell, profile and enabled congestion snapshot; returned arrays are copied and costs are rescored for the actual start position. Finished keys include exact logical tail, start cell, physical clearance radius, optional lateral offset and selected corridor variant/settings. Hash matches are verified against actual coordinates. Equivalent geometry can be reused across destinations, but no Citadel/checkpoint identity, branch decision or progress is stored in a template; each ship still owns those obligations.

Each eligible ship chooses a stable seed slot. Optional offsets use that slot's bounded lateral position instead of a unique continuous offset; disabled offsets stay zero. Corridor selection still uses the original per-ship seed and probability, while its geometry uses a finite variant seed. Consequently trajectories can repeat more often; broad movement appearance and authored variation limits remain, but pixel-identical paths are not promised. The slot does not change wave RNG, spawn ordinals or checkpoint branch choices.

The first request builds a template through the existing budgeted path queue; later compatible ships reuse it. **Warming is on demand, not an exhaustive startup bake.** This avoids startup hitches and speculative routes for unknown branch/spawn positions. Cold variants, new radii/destinations and unsafe join cases still require work. Hits retain the scheduler's existing conservative query-count budget and soft time budget. Reuse avoids repeated corridor probes, while private per-ship waypoint arrays, suffix distances, corner/dependency caches and progress remain for correctness.

A reused route gets a checked connector from the actual (possibly jittered) spawn/repath position. A backwards or unsafe connector falls back to building for that ship. Immediate swept movement checks and the existing queued safe-prefix/repath behavior are retained. Placing a tower invalidates only cached routes whose conservative cell dependencies intersect its cells; live ships retain their own dependency subscriptions. Removing a blocker clears raw searches to permit discovering shorter alternatives, without forcing live valid routes to repath. Full terrain/profile resets clear the pool. Capacity evicts one template at a time; caches never grow without limit.

F5 PF2 report metadata now includes cumulative `sharedRawHits`, `sharedRefinedHits` and `sharedTemplateBuilds`. These are world-lifetime counters, not just the retained five-second timing window. Runtime verification and actual headless benchmark results: `TESTS/Performance/SharedRoutePool.md`. They are not a promise of desktop FPS or a complete solution to unrelated spawn/render/movement costs.

---

## PF1 — notification-owned movement updates (source implemented; unverified)

EnemyMovement no longer explicitly updates the registry after SetMovementPosition; Enemy's existing local/global transform notifications retain spatial indexing and reveal callbacks. Clearance, terrain traversal and swept history remain unchanged. See `TESTS/Performance/MovementRegistryChecks.tscn` and `NOTES/AGENT_FINDINGS/Performance.md`. Build, runtime regressions and timing measurements could not run because the execution environment was unavailable. No measured speedup is claimed.

## MC3 HUD + F3 — implemented 2026-10-18

Multiple Citadels now share **one horizontal row** in the top-center HUD: equal-width cells for two, four or more objectives, never stacked vertically. A one-Citadel level keeps the original nodes/layout/SetCitadelHealth behavior. Each multi meter shows its stable CITADEL number, independent current/max HP, threshold-colored fill and damage-only animation/flash. Zero values and maximum resets are supported; rebind/unload disconnect model events and stop old tweens. The full Citadel number/HP is available in each cell's tooltip. No HUD health polling or gameplay authority is introduced.

UIMasterConfig exposes MultiCitadelBlockWidth (560 default) and CitadelCellGap (6), alongside existing bar height/palette/timing settings. The center remains viewport-centered and capped to the existing responsive center zone (46% of width); Credits and Wave retain separate side zones. Short cells truncate text rather than wrap into extra rows. Very large counts/HP values can require the tooltip for full text. Existing Inspector/build tray geometry is unchanged.

F3 now passes each portal's **actual cached live Citadel identity** into RoutePreviewJob. Raw/refined paths, neutral comparisons and required checkpoint chains end at that assigned objective, not the primary Citadel. Legend/source labels show portal → CITADEL N; destination approach markers and one label per objective use the same stable identity colors/numbers as the HUD. ALL PORTALS keeps the existing shared preview query/time budget and private profile pool. Missing assignments appear as UNASSIGNED diagnostics with no invented fallback route. All destination goals/availability and cached assignments participate in invalidation, including secondary objectives. Closing/Clear releases private preview resources only.

Explicit TerrainRebuilt now refreshes source assignments during the authoring rebuild itself, so F3 reads current cached ownership without triggering a live assignment calculation. Ordinary tower occupancy still cannot reassign portals, and preview settings/simulation never spawn enemies or alter live route queues/profile pools/random streams.

Validation: compiled with the installed .NET SDK/MSBuild; MultiCitadelHudChecks passed one/two/four objective presentation at 1920×1080, 1366×768 and 960×540, independent HP/colors/zero/maximum reset, rebind, actual assigned F3 endpoints, secondary-goal invalidation and live-state isolation. Existing RoutePreviewChecks (both maps/raw/refined/checkpoints/50 samples/500 enemies), MultiCitadelChecks, CheckpointChecks, ResultsChecks, MissionXPChecks, PlacementHudChecks and CompactInspectorChecks passed. See TESTS/UI/MultiCitadelHud.md. Desktop rendered appearance/playfeel still needs user review; headless geometry checks are not a screenshot or full-suite/FPS claim. Existing gameplay maps/waves were not changed. This amendment supersedes earlier MC3-pending/primary-only-preview statements without deleting the original pass plan.

---

# Enemy movement and route refinement

EnemyMovement.cs follows shared weighted A* routes, preserving editable speed, radius and smooth facing. EnemyRoute.cs caches waypoint suffix distances and swept cell-to-segment dependencies. Physical RemainingPathDistance drives First tower targeting; weighted preference costs are separate.

On assignment, EnemyRoute applies one stable per-spawn lateral bias, up to 8 pixels by default. Interior corner directions use the incoming/outgoing bisector. Offset points and connecting segments must pass HasClearance and SegmentClear; unsafe offsets are halved until safe, or kept at the original center. Spawn and arrival endpoints stay on their logical route. No per-frame random jitter, steering physics, extra A* or local ship separation is added.

Each physics frame checks only immediate swept movement clearance plus a cheap audit timer. New blocked cells mark indexed remaining route dependencies, with passed cells ignored. Central bounded validations verify only relevant segments. Unaffected ships keep moving; confirmed affected ships stop and queue prioritized A*. If an immediate segment becomes unsafe before its queued check, the ship stops immediately and upgrades to urgent work.

Optional periodic audits default to three seconds with per-ship initial staggering. They inspect cached cell dependencies and perform geometric checks near blockers; a valid route never triggers A*. No-route cases retry through the path manager. Removing obstacles does not force valid ships to recalculate shorter paths. SeparationStrength/Radius remain inactive future settings.

## Moving replacement-path handoffs

Confirmed blockage now queues a replacement without stopping safe movement immediately. Ships retain old waypoints while waiting, with strict immediate swept/corner checks; they stop only when the next movement would be unsafe. Queue urgency follows the shrinking distance to the obstruction. Failed replacement searches retain any safe old prefix and use the existing retry interval.

Each multi-goal request uses one position snapshot, so its candidate costs are comparable while the ship moves. When the result arrives, EnemyMovement joins it near the ship's current progress, preferring a forward clearance-tested waypoint. A safe two-leg elbow can turn around a nearby wall without returning to the cell center. Both legs obey swept clearance and corner-safe cell transitions. If reconnection fails, a fresh request uses the same centralized budgets. Backtracking remains possible when the actual map requires it.

Safe offset refinement cannot turn the first new segment backward merely due to lateral bias. Existing speed/turning/offset settings, weighted profiles, congestion, auditing and count/time budgets are preserved. First targeting excludes routes known to need replacement until a valid handoff completes. No new pathfinding algorithm or per-frame A* is added.

## Phase N4 implementation

`CorridorPreference` assigns lifetime selection and a stable seed independently from shared weighted profile assignment. `EnemyMovement.AcceptPath` calls the original `EnemyRoute.Build` unchanged for master-off, zero-strength and unselected ships; selected ships use `BuildCorridor`. A zero selection percentage only affects future spawns.

`CorridorRouteRefinement` probes both sides of sparse anchors (at most seven probes per side, bounded to two cells), combines stable per-ship bias with smooth spatial noise, then interpolates/tapers lateral changes. Each proposed link must preserve forward progress, remain at most 1.35× its logical incoming length and pass radius/corner clearance. Unsafe proposals are reduced or use the baseline. Endpoints stay unchanged; short handoff elbows and narrow passages retain the existing route where safe. No per-frame RNG, A* requests, weight mutation or extra steering is introduced.

`CorridorClearance` supplements existing navigation checks with analytic segment-to-solid-rectangle distance for selected route connections/handoffs. This closes a sampled-clearance grazing case that could leave a ship safely stopped but repeatedly repathing beside a tower corner. It runs only during route construction/connection, not normal physics movement.

`EnemyRoute` caches physical suffix distances and swept cell dependencies from the actual traveled chain for First targeting and selective revision handling. Selected routes additionally cache diagonal transition side cells from that actual geometry, preserving strict corner rules after tower placement without new per-frame route scans. Existing immediate clearance, safe-prefix movement, prioritized handoff and staggered audits remain authoritative.

Configuration changes are read at the next accepted route. They do not invalidate all routes or synchronously rebuild them. Existing lifetime selection survives repaths and live toggle changes. Path assignment has measurable extra CPU cost; the path-manager count cap is retained and its millisecond budget remains soft because an individual assignment cannot be interrupted. See TESTS/Performance/CorridorPerformance.md.

## Per-enemy movement variation permissions — implemented

EnemyDefinition exposes three independent Inspector booleans under **Movement Variation**, all default true: `AllowCorridorVariation`, `AllowPathOffsets`, `AllowPathDiversity`. Existing authored enemy definitions and master settings are unchanged. Permissions are captured at initialization; edit a type before spawning to change its permissions.

- Corridor permission gates lifetime deterministic selection. Ineligible ships skip N4 builders, width probes and corridor-only connection checks. Eligible ships retain the existing global master/percentage selection, stable seed and strength rules; repaths never reroll selection.
- Offset permission gates the offset passed to either builder, independently of corridor permission. Disallowed offsets pass zero; allowed offsets preserve the original stable spawn bias and global clamp.
- Diversity permission selects the existing assigned weighted profile or reserved neutral profile `-1`. Every spawn still advances the original profile and corridor ordinal streams, so changing permissions cannot shift later eligible ships' existing assignments or wave RNG.

Global masters remain authoritative. Master changes apply through existing request/accepted-route semantics, without mass invalidation. Corridor percentage affects future spawn selection only. An all-false type follows an unoffset, unvaried shortest navigable route for its start/goal and clearance; this is not a designer-authored centerline.

Navigation owns one extra shared neutral A* grid, never one per ship. Its point costs are exactly one, with no preference or congestion weights; multi-goal comparisons use the same neutral costs. It receives identical solidity/revision/incremental updates and diagonal rules. Weighted profile zero remains weighted. Central budgets, handoffs, clearance, dependency indexing and terrain movement hooks are unchanged.

`TESTS/Navigation/MovementPermissionChecks.cs` runs within the isolated corridor suite: all eight flags × master off/on × 0/50/100%, mixed Enemy001/002/004 definitions plus an all-false synthetic identity, exact ordinary/corridor offset-builder parity, stable repaths, neutral shortest/crowd/cost/occupancy/removal/different-goal/24px-clearance checks. SwarmNavigationChecks also compares neutral routing to the master-off alternative-corridor oracle. See `TESTS/Navigation/MovementPermissions.md` for measured outcomes.

## Swept Interactive Terrain

Each clearance-approved movement segment reports its actual endpoints, travel duration (distance/speed) and offset within the physics tick to the world's InteractiveTileManager when Lava exists. A lethal entry/periodic encounter stops the loop before committing the planned endpoint. Ordinary movement retains the shared registry's spatial update; direct position assignments remain teleports and do not traverse intervening hazards. The late terrain manager accounts for only the remaining stationary time of active occupants, including stopped/waiting enemies. No Lava routing weights or A* changes are introduced. See WORLD/TERRAIN/Interactive/README.md.

## F3 route preview — implemented

`RoutePreviewJob` performs bounded debug queries against the production shared A* profiles, then optional production EnemyRoute refinements. `RoutePreviewOverlay` owns one cached world drawing and a debug-only control panel. Open F3, choose portal/type/sample count and Raw/Refined, then Simulate Paths; Clear/F3 removes results. Neutral comparison, distance overhead, duplicate labels and failure diagnostics are included. Samples do not consume live assignment streams or wave RNG, spawn ships or modify live routes. Input/settings/navigation changes discard cached output; an active congestion snapshot change cancels partial work. No checkpoints are implemented. See pathoverlay_tweaks.md and TESTS/Navigation/RoutePreview.md.

F3 also supports **ALL PORTALS**, **1–50 samples per portal**, and preview-only Path Diversity enable/strength/profile-count controls. The global profile limit is now 16 (default remains 4). Debug settings and weighted caches are privately owned; no .cs/.tres files, live settings, live profile grids or live routes are changed. Reset copies game settings back into the panel. All portal jobs share one debug work budget and one snapshot pool; color families and unique counts distinguish portals.

## Required portal checkpoints — implemented

Portals may link a freely positioned `WORLD/ROUTING/RouteCheckpoint/RouteCheckpoint.tscn` with a circular RadiusPixels region. SpawnManager passes the actual selected portal's assignment into Enemy.Initialize before the initial route request. EnemyMovement tracks required/completed state independently of weighted profile, corridor selection and offset. Only physical center-entry (analytic swept segment test, after actual terrain traversal) advances to normal Citadel goals. Planning/accepting an A* route never completes it. No assigned checkpoint means unchanged existing routing.

Checkpoint targets are bounded and cached per world/radius/revision/geometry; CheckpointRoute shares the terminal connector and preview entry clipping. PathRequestManager retains its queues/count/time limits, restarting stale candidate work on navigation/cost/stage/marker changes. Inaccessible/broken requirements wait/retry with RouteStatus diagnostics. Geometry edits stop unfinished old-stage movement for a fresh request; completed checkpoints remain completed. First distance reports the physical current-leg chain.

F3 now previews both legs through each actual portal checkpoint, draws its circle/entry markers, respects type/global permissions and invalidates old output on link/geometry changes. Authoring and limitations: WORLD/ROUTING/RouteCheckpoint/README.md. Tests: TESTS/Navigation/Checkpoints.md.

## Checkpoint chains and branches

A checkpoint's NextCheckpoints list may be empty (Citadel), single (chain) or multiple (uniform random branch). CheckpointNetwork validates a bounded acyclic graph and captures outgoing links for each spawn/preview. Branch choices use a separate deterministic hash of the existing stable seed and completed count, never wave RNG. Movement keeps the selected node across repaths and retains all profile/corridor/offset preferences. Every physical area entry advances one link; missing/cyclic/duplicate networks or removed/unreachable selected nodes wait instead of bypassing/rerolling. Link edits affect future snapshots; live geometry edits still affect the current stage. The existing path manager and budgets are unchanged. F3 returns full sequence/entry diagnostics, with cache invalidation for downstream edits. See WORLD/ROUTING/RouteCheckpoint/README.md and TESTS/Navigation/CheckpointChains.md.

## Effective status speed

Cryo CT1 changes only the travel-speed input to Enemy.EffectiveMoveSpeed. No effect mutates authored speed or requests a new route. Existing clearance, checkpoint progress, spatial notifications and swept Lava segment timing use the same movement code. Permanent Slow needs no timer; stationary Lava damage remains owned by InteractiveTileManager.

## IT4 full-journey targeting score

RemainingPathDistance remains current-leg refined physical distance. RemainingJourneyDistance adds a cached, shared downstream checkpoint estimate; both First and Last exclude unavailable/stale scores. A valid normal Citadel route retains its old score. PathRequestManager computes tails only within spare existing budgets. Captured checkpoint choices, actual travel/corridor offsets, clearance, Lava and route handoffs are unchanged. See TOWERS/Shared/Targeting/README.md.
