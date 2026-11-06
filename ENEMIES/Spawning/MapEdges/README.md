# Map Edge Spawning — baseline implemented, advanced authoring planned

**Current implementation (2026-11-06):** `LevelConfig` already exposes `SpawnMode = Portals | MapEdges | Both` and Inspector `AllowedEdges = Top | Bottom | Left | Right`. For **one-Citadel** levels, `SpawnManager` enumerates cell centres along selected map boundaries and keeps valid, reachable, clearance-safe entries as candidates. `MapEdges` can therefore spawn from **all four valid sides right now** by enabling all four flags; no large collection of placed Marker2D objects is needed. Current `Both` selects a portal when one is eligible, only falling back to an edge when no portal qualifies. Multi-Citadel edge spawning remains disabled.

**Not yet implemented:** explicit per-wave edge-side selectors/weights, sub-edge regions, true concurrent Portal+Edge distribution in Both mode, and materialization telegraphs. START/Wave N portal activation itself is now source implemented (SPAWN1; owner validation pending). These are specified in [SPAWN0–SPAWN5](../IMPLEMENTATION_PLAN.md). The original note below is retained for context.

---

# Map Edge Spawning (Future)

A **logical spawn source**, not an in-world Portal object and not a terrain tile.

Future SpawnManager uses per-level allowed edges (Top, Bottom, Left, Right), map bounds and terrain/nav data to choose a valid, reachable spawn position. Support edge-only maps and mixed Portal-plus-edge maps. Later waves can specify edge preferences and distribution weights.

Do not create hundreds of Marker2D nodes around the map perimeter, and do not hardcode edges into every level scene. A few optional editor hints may be added only if a level designer needs authored entry regions.

No scripts yet.
