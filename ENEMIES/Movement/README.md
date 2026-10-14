# Enemy movement and route refinement

EnemyMovement.cs follows shared weighted A* routes, preserving editable speed, radius and smooth facing. EnemyRoute.cs caches waypoint suffix distances and swept cell-to-segment dependencies. Physical RemainingPathDistance drives First tower targeting; weighted preference costs are separate.

On assignment, EnemyRoute applies one stable per-spawn lateral bias, up to 8 pixels by default. Interior corner directions use the incoming/outgoing bisector. Offset points and connecting segments must pass HasClearance and SegmentClear; unsafe offsets are halved until safe, or kept at the original center. Spawn and arrival endpoints stay on their logical route. No per-frame random jitter, steering physics, extra A* or local ship separation is added.

Each physics frame checks only immediate swept movement clearance plus a cheap audit timer. New blocked cells mark indexed remaining route dependencies, with passed cells ignored. Central bounded validations verify only relevant segments. Unaffected ships keep moving; confirmed affected ships stop and queue prioritized A*. If an immediate segment becomes unsafe before its queued check, the ship stops immediately and upgrades to urgent work.

Optional periodic audits default to three seconds with per-ship initial staggering. They inspect cached cell dependencies and perform geometric checks near blockers; a valid route never triggers A*. No-route cases retry through the path manager. Removing obstacles does not force valid ships to recalculate shorter paths. SeparationStrength/Radius remain inactive future settings.
