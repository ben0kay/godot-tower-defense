# Tower Placement (Future)

Shared placement pipeline for **all** tower categories. Intended to connect `UI` build-menu selection to `WORLD/GRID` and a future navigation/occupancy system.

First pass: select tower, preview its footprint on valid grid cells, left-click to place one placeholder sprite, and deduct cost when an economy system exists. Reject out-of-bounds and occupied cells.

Once blocking/pathfinding exists: reject tower placements that disconnect required portal/edge spawns from goals, and invalidate routes after accepted placements. Avoid copying placement logic into each tower's folder.

**Documentation only.** Next gameplay milestone.
