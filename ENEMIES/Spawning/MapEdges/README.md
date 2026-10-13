# Map Edge Spawning (Future)

A **logical spawn source**, not an in-world Portal object and not a terrain tile.

Future SpawnManager uses per-level allowed edges (Top, Bottom, Left, Right), map bounds and terrain/nav data to choose a valid, reachable spawn position. Support edge-only maps and mixed Portal-plus-edge maps. Later waves can specify edge preferences and distribution weights.

Do not create hundreds of Marker2D nodes around the map perimeter, and do not hardcode edges into every level scene. A few optional editor hints may be added only if a level designer needs authored entry regions.

No scripts yet.
