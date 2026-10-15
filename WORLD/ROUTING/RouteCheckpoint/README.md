# Required Route Checkpoint

A freely placed Node2D scene with an Inspector-editable **Radius Pixels** (default128 world pixels). It costs nothing, reserves no cells and never changes build permissions or navigation solidity. Its circle is visible in the editor and in F3 for linked portals; it is hidden in ordinary gameplay. There are no physics shapes, Area2D nodes, processing callbacks or enemy scans.

## Place and link

1. Open your level scene. Optionally add a `RouteCheckpoints` Node2D group under the level.
2. Drag `WORLD/ROUTING/RouteCheckpoint/RouteCheckpoint.tscn` into that group. Rename it (e.g. TopCheckpoint) and freely position it like a portal. It does not need tile painting or cell snapping.
3. Set **Radius Pixels** to cover a useful passable area near the route you want enemies to enter.
4. Select the source EnemyPortal → **Route Checkpoint → Checkpoint Path**. Use the node picker to select your checkpoint (a relative path from the portal, e.g. `../../RouteCheckpoints/TopCheckpoint`). Multiple portals can reference the same node.
5. Run Main/the campaign normally. Press F3 and Simulate to see Portal → checkpoint entry → Citadel, the checkpoint circle and entry markers. ALL PORTALS respects each individual link.

An empty portal path preserves existing direct routing. A nonempty but broken path is an editor configuration warning and a hard unavailable destination, not permission to skip the checkpoint. New spawns capture their selected portal's link before their first route request. Existing enemies retain that assignment; changing the portal link affects future spawns. Moving/resizing their assigned node affects pending/accepted stage-A routing; removing it makes unfinished enemies safely wait. Completed enemies never return to stage A.

## Physical and routing rules

An assigned enemy must physically move its **center** into a clearance-valid part of the circle; touching an exact center point is not required. Circle membership has a0.01px boundary tolerance for floating-point movement. Spawn already inside the circle starts stage B. Swept segment-circle intersection clips fast movement at entry, so low physics rates cannot skip a region. Canonical swept Lava processing runs on that actual clipped movement before a living enemy is promoted. A planned line alone never completes gameplay progress.

Stage A uses a bounded deterministic set of at most33 candidate positions within the region (center plus two rings of snapped cell centers), filtered by enemy-radius clearance and cached by world/radius/navigation revision/geometry. Shared A* searches choose a reachable candidate with the existing weighted/neutral costs. An unsnapped terminal connector must pass swept/corner clearance. No grid per enemy and no A* per enclosed tile is created. Region size does not increase probe count.

Stage B requests the normal Citadel goals through the same bounded PathRequestManager. Profile, corridor seed/selection and stable offsets persist; the three global/type permission gates apply independently to both legs. Old safe-prefix handoff, immediate collision checks, selective invalidation, retries and hard calculation/soft time budgets remain. First targeting continues to use the physical remaining chain of the **current leg**; the future Citadel chain is not known until entry. Unassigned enemies retain the exact old distance/routing behavior.

If no sampled target is clearance-valid/reachable, the enemy waits/retries; it never silently bypasses the region. Move/enlarge the marker if a narrow passage intersects a large circle but misses its bounded sample positions. Required regions can become inaccessible after building: checkpoint markers do not reserve/protect their cells, and the existing placement validator still protects ordinary Citadel connectivity. Restore access or edit the marker. `EnemyMovement.RouteStatus` and F3 expose unavailable destinations.

F3 reuses the same targets, query adapter, refinement builders and analytic entry clipping; both preview legs share the same sample preferences. It remains a route plan, not an enemy timing prediction. Weighted preferences can still choose downstream detours **after** visiting the required area; this is not a global detour limit or designer centerline.

Existing Level001/002 maps, waves and portals are not automatically linked or repositioned. Place/link only where you want this new routing requirement. See `TESTS/Checkpoints.md` for verification.
