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
