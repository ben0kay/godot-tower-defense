# Enemy Portal

`EnemyPortal.tscn` is a reusable, visible placeholder with a `SpawnExit` Marker2D and `WorldObjectFootprint` script. The one-cell footprint reserves its location against towers but remains passable. The exit defaults to one cell right of the object root. Level001 now instances it.

Snap the root to a 64px cell center (32 + 64*n), move the exit to a reachable passable cell, and list the root-relative Portal path in the level's Settings/ActivePortals. Placeholder polygons and label can be replaced with art/VFX.

This object creates no enemies and owns no wave timers. `ENEMIES/Spawning` will consume approved sources from LevelConfig; edge-only levels need no Portal.
