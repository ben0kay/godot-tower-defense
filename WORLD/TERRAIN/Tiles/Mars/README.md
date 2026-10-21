## E1/E2 implementation — 2026-10-21

E1 clips both64×64 SVG atlas cells independently, removes the dust's repeated gradient/sediment bands in favour of low-contrast seamless grain, and softens the rock's repeating black strokes. Source IDs, atlas coordinates, TileData flags and saved maps are unchanged.

E2 now has a shared, material-profile-driven renderer. WORLD/World.tscn attaches one TerrainEdges controller/renderer after Grid startup. It classifies painted materials in existing layer precedence, builds deterministic cached L8 silhouette masks, and applies a shared lightweight mask shader to existing TileMapLayers. The opaque square tile pixels are actually cut away. Tiny recorded texture quads on one helper per participating layer fill rounded inward corners around holes. No per-cell nodes, enemy checks, terrain sampling in physics/process callbacks, full-map per-frame image updates, tile repainting, or navigation rebuild requests are added. Towers only change occupancy, so placing them does not regenerate these visual masks.

**Opt-in rollout:** DefaultTerrainEdgeConfig.tres remains Enabled=false for desktop GPU/appearance review, especially given the current unrelated rendering-stall investigation. Set Enabled=true and restart the level to try E2. MarsDustEdge is the initial Soft profile; MarsRockEdge.RenderEdges=false and jagged/material transitions remain E3. To opt another tile material in, create a TerrainEdgeProfile, set a stable MaterialId, Layer, SourceId and AtlasCells, choose Shape=Soft / RenderEdges=true, then add it to the master Profiles array. No renderer code references Mars names. Same-material variants merge; other painted materials protect joins instead of introducing transparent gaps. Straight profiles keep original rendering. EdgeTint and a dedicated Jagged geometry style remain future E3 work.

MaskPixelsPerCell defaults16 (8–32); MaximumMaskMegapixels defaults16 per layer, falling back to original rendering if exceeded. Master disable plus controller.Rebuild (or level restart) restores exact original layer material references and releases masks/fills. Changing profile/settings values requires an explicit rebuild/restart. All generation is on attachment or WorldGrid.TerrainRebuilt; future local hints/chunk invalidation remain deferred. Native rendering still samples the mask each drawn pixel; caching removes CPU generation, not GPU draw cost.

Supported MVP: static64×64 square atlas artwork, zero texture origin, grid-aligned source layers, including transformed worlds and whole-cell source translation. Existing custom layer/tile materials, tint/Z overrides, Y sorting, subcell/rotated/scaled source layers, animated or oversized opted-in atlas tiles are left untouched with a warning. Existing layers retain animation/tint/atlas handling for unprofiled tiles through a nearest-filtered per-cell applicability texture; profiling masks never replace gameplay or collision data.

Validation: C# build/import and TESTS/Terrain/TerrainEdgeRenderingChecks.tscn pass, covering generic second-material opt-in, isolated/connected tiles, corner/hole math, cached deterministic output, transformed source mapping, unsupported-transform fallback, disable/restoration, no navigation/AStar from visual rebuilds, no mask rebuild on tower occupancy, and memory-budget fallback. Runtime-only Level002 fixtures are discarded; no saved level was repainted. A4800-tile fixture generated in32.69ms after interior-row/empty-cell fast paths (earlier unoptimized runs270–307ms) on this host with1,269,837 bytes of masks and one surface; this is a startup CPU result, not desktop FPS. Godot SVG raster/mask outputs and a CPU composite were visually inspected; desktop shader rendering/filtering at real gameplay zoom is still pending. Prior future-plan text below is retained as history and superseded where E1/E2 are described as unimplemented.

---

# Mars ground tiles

`mars_ground_atlas.svg` is an independent, editable **128×64 SVG atlas** with two 64×64 top-down terrain cells, designed to complement the dark-blue metal terrain and stay readable beneath dark enemy ships.

| Atlas coordinate | Look | Godot palette | can_build | blocks_enemies |
| --- | --- | --- | --- | --- |
| (0,0) | Bright orange/tan Martian dust, shallow wind-shaped sediment, sparse flecks | PassableTerrain source **2** | false | false |
| (1,0) | Dark burnt-orange rocky mountain ground, overlapping angular slabs and crevices | ImpassableTerrain source **2** | false | true |

Paint **dust on Terrain/Passable**, not on the cosmetic-only Decorations layer, because it must contribute unbuildable/walkable terrain metadata. Paint **rock on Terrain/Impassable** so it blocks A* navigation and building placement.

## Adding variations later

Expand the vector atlas in whole 64px cells (e.g. width192 for a third variant) and add a new `N:0/0 = 0` entry under the corresponding `TileSetAtlasSource` in the category palette, with gameplay metadata on that tile. If you need many variants, add another row of 64px cells (adjust SVG height) or a dedicated source for each Mars material family. Retain old source IDs and coordinates; **do not renumber 0/1/2** or change previously painted tiles. Every variation can remain independently playable/buildable based on its TileData, without any WorldGrid or pathfinding changes.

These are two initial hand-authored tiles, not automatic terrain transitions or procedural tile picking. Future auto-tiling, transitions, noise variants, and biomes may be added separately; editing the SVG alone does not add new palette cells.

## Edge styling (planned, not enabled)

The optional profile resources `../../Edges/Profiles/MarsDustEdge.tres` and `MarsRockEdge.tres` declare visual materials for later organic transitions. `../../Edges/DefaultTerrainEdgeConfig.tres` stays disabled and no scene uses the renderer yet. The known dust/rock SVG overflow and visible tiling bands are **not fixed by the scaffold**: see [Edges/IMPLEMENTATION_PLAN.md](../../Edges/IMPLEMENTATION_PLAN.md), E1 then E2. Gameplay flags and existing painted coordinates remain authoritative.
