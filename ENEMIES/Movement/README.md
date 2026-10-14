# Enemy movement and route refinement

EnemyMovement.cs follows shared weighted A* routes, preserving editable speed, radius and smooth facing. EnemyRoute.cs caches waypoint suffix distances and swept cell-to-segment dependencies. Physical RemainingPathDistance drives First tower targeting; weighted preference costs are separate.

On assignment, EnemyRoute applies one stable per-spawn lateral bias, up to 8 pixels by default. Interior corner directions use the incoming/outgoing bisector. Offset points and connecting segments must pass HasClearance and SegmentClear; unsafe offsets are halved until safe, or kept at the original center. Spawn and arrival endpoints stay on their logical route. No per-frame random jitter, steering physics, extra A* or local ship separation is added.

Each physics frame checks only immediate swept movement clearance plus a cheap audit timer. New blocked cells mark indexed remaining route dependencies, with passed cells ignored. Central bounded validations verify only relevant segments. Unaffected ships keep moving; confirmed affected ships stop and queue prioritized A*. If an immediate segment becomes unsafe before its queued check, the ship stops immediately and upgrades to urgent work.

Optional periodic audits default to three seconds with per-ship initial staggering. They inspect cached cell dependencies and perform geometric checks near blockers; a valid route never triggers A*. No-route cases retry through the path manager. Removing obstacles does not force valid ships to recalculate shorter paths. SeparationStrength/Radius remain inactive future settings.

## Moving replacement-path handoffs

Confirmed blockage now queues a replacement without stopping safe movement immediately. Ships retain old waypoints while waiting, with strict immediate swept/corner checks; they stop only when the next movement would be unsafe. Queue urgency follows the shrinking distance to the obstruction. Failed replacement searches retain any safe old prefix and use the existing retry interval.

Each multi-goal request uses one position snapshot, so its candidate costs are comparable while the ship moves. When the result arrives, EnemyMovement joins it near the ship's current progress, preferring a forward clearance-tested waypoint. A safe two-leg elbow can turn around a nearby wall without returning to the cell center. Both legs obey swept clearance and corner-safe cell transitions. If reconnection fails, a fresh request uses the same centralized budgets. Backtracking remains possible when the actual map requires it.

Safe offset refinement cannot turn the first new segment backward merely due to lateral bias. Existing speed/turning/offset settings, weighted profiles, congestion, auditing and count/time budgets are preserved. First targeting excludes routes known to need replacement until a valid handoff completes. No new pathfinding algorithm or per-frame A* is added.
