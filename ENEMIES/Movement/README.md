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

Configuration changes are read at the next accepted route. They do not invalidate all routes or synchronously rebuild them. Existing lifetime selection survives repaths and live toggle changes. Path assignment has measurable extra CPU cost; the path-manager count cap is retained and its millisecond budget remains soft because an individual assignment cannot be interrupted. See TESTS/CorridorPerformance.md.

## Per-enemy movement variation permissions — implemented

EnemyDefinition exposes three independent Inspector booleans under **Movement Variation**, all default true: `AllowCorridorVariation`, `AllowPathOffsets`, `AllowPathDiversity`. Existing authored enemy definitions and master settings are unchanged. Permissions are captured at initialization; edit a type before spawning to change its permissions.

- Corridor permission gates lifetime deterministic selection. Ineligible ships skip N4 builders, width probes and corridor-only connection checks. Eligible ships retain the existing global master/percentage selection, stable seed and strength rules; repaths never reroll selection.
- Offset permission gates the offset passed to either builder, independently of corridor permission. Disallowed offsets pass zero; allowed offsets preserve the original stable spawn bias and global clamp.
- Diversity permission selects the existing assigned weighted profile or reserved neutral profile `-1`. Every spawn still advances the original profile and corridor ordinal streams, so changing permissions cannot shift later eligible ships' existing assignments or wave RNG.

Global masters remain authoritative. Master changes apply through existing request/accepted-route semantics, without mass invalidation. Corridor percentage affects future spawn selection only. An all-false type follows an unoffset, unvaried shortest navigable route for its start/goal and clearance; this is not a designer-authored centerline.

Navigation owns one extra shared neutral A* grid, never one per ship. Its point costs are exactly one, with no preference or congestion weights; multi-goal comparisons use the same neutral costs. It receives identical solidity/revision/incremental updates and diagonal rules. Weighted profile zero remains weighted. Central budgets, handoffs, clearance, dependency indexing and terrain movement hooks are unchanged.

`TESTS/MovementPermissionChecks.cs` runs within the isolated corridor suite: all eight flags × master off/on × 0/50/100%, mixed Enemy001/002/004 definitions plus an all-false synthetic identity, exact ordinary/corridor offset-builder parity, stable repaths, neutral shortest/crowd/cost/occupancy/removal/different-goal/24px-clearance checks. SwarmNavigationChecks also compares neutral routing to the master-off alternative-corridor oracle. See `TESTS/MovementPermissions.md` for measured outcomes.

## Swept Interactive Terrain

Each clearance-approved movement segment reports its actual endpoints, travel duration (distance/speed) and offset within the physics tick to the world's InteractiveTileManager when Lava exists. A lethal entry/periodic encounter stops the loop before committing the planned endpoint. Ordinary movement retains the shared registry's spatial update; direct position assignments remain teleports and do not traverse intervening hazards. The late terrain manager accounts for only the remaining stationary time of active occupants, including stopped/waiting enemies. No Lava routing weights or A* changes are introduced. See WORLD/TERRAIN/Interactive/README.md.

## F3 route preview — implemented

`RoutePreviewJob` performs bounded debug queries against the production shared A* profiles, then optional production EnemyRoute refinements. `RoutePreviewOverlay` owns one cached world drawing and a debug-only control panel. Open F3, choose portal/type/sample count and Raw/Refined, then Simulate Paths; Clear/F3 removes results. Neutral comparison, distance overhead, duplicate labels and failure diagnostics are included. Samples do not consume live assignment streams or wave RNG, spawn ships or modify live routes. Input/settings/navigation changes discard cached output; an active congestion snapshot change cancels partial work. No checkpoints are implemented. See pathoverlay_tweaks.md and TESTS/RoutePreview.md.

F3 also supports **ALL PORTALS**, **1–50 samples per portal**, and preview-only Path Diversity enable/strength/profile-count controls. The global profile limit is now 16 (default remains 4). Debug settings and weighted caches are privately owned; no .cs/.tres files, live settings, live profile grids or live routes are changed. Reset copies game settings back into the panel. All portal jobs share one debug work budget and one snapshot pool; color families and unique counts distinguish portals.
