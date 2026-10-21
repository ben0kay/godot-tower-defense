## E1/E2 implementation — 2026-10-21

E1 clips both64×64 SVG atlas cells independently, removes the dust's repeated gradient/sediment bands in favour of low-contrast seamless grain, and softens the rock's repeating black strokes. Source IDs, atlas coordinates, TileData flags and saved maps are unchanged.

E2 now has a shared, material-profile-driven renderer. WORLD/World.tscn attaches one TerrainEdges controller/renderer after Grid startup. It classifies painted materials in existing layer precedence, builds deterministic cached L8 silhouette masks, and applies a shared lightweight mask shader to existing TileMapLayers. The opaque square tile pixels are actually cut away. Tiny recorded texture quads on one helper per participating layer fill rounded inward corners around holes. No per-cell nodes, enemy checks, terrain sampling in physics/process callbacks, full-map per-frame image updates, tile repainting, or navigation rebuild requests are added. Towers only change occupancy, so placing them does not regenerate these visual masks.

**Opt-in rollout:** DefaultTerrainEdgeConfig.tres remains Enabled=false for desktop GPU/appearance review, especially given the current unrelated rendering-stall investigation. Set Enabled=true and restart the level to try E2. MarsDustEdge is the initial Soft profile; MarsRockEdge.RenderEdges=false and jagged/material transitions remain E3. To opt another tile material in, create a TerrainEdgeProfile, set a stable MaterialId, Layer, SourceId and AtlasCells, choose Shape=Soft / RenderEdges=true, then add it to the master Profiles array. No renderer code references Mars names. Same-material variants merge; other painted materials protect joins instead of introducing transparent gaps. Straight profiles keep original rendering. EdgeTint and a dedicated Jagged geometry style remain future E3 work.

MaskPixelsPerCell defaults16 (8–32); MaximumMaskMegapixels defaults16 per layer, falling back to original rendering if exceeded. Master disable plus controller.Rebuild (or level restart) restores exact original layer material references and releases masks/fills. Changing profile/settings values requires an explicit rebuild/restart. All generation is on attachment or WorldGrid.TerrainRebuilt; future local hints/chunk invalidation remain deferred. Native rendering still samples the mask each drawn pixel; caching removes CPU generation, not GPU draw cost.

Supported MVP: static64×64 square atlas artwork, zero texture origin, grid-aligned source layers, including transformed worlds and whole-cell source translation. Existing custom layer/tile materials, tint/Z overrides, Y sorting, subcell/rotated/scaled source layers, animated or oversized opted-in atlas tiles are left untouched with a warning. Existing layers retain animation/tint/atlas handling for unprofiled tiles through a nearest-filtered per-cell applicability texture; profiling masks never replace gameplay or collision data.

Validation: C# build/import and TESTS/Terrain/TerrainEdgeRenderingChecks.tscn pass, covering generic second-material opt-in, isolated/connected tiles, corner/hole math, cached deterministic output, transformed source mapping, unsupported-transform fallback, disable/restoration, no navigation/AStar from visual rebuilds, no mask rebuild on tower occupancy, and memory-budget fallback. Runtime-only Level002 fixtures are discarded; no saved level was repainted. A4800-tile fixture generated in32.69ms after interior-row/empty-cell fast paths (earlier unoptimized runs270–307ms) on this host with1,269,837 bytes of masks and one surface; this is a startup CPU result, not desktop FPS. Godot SVG raster/mask outputs and a CPU composite were visually inspected; desktop shader rendering/filtering at real gameplay zoom is still pending. Prior future-plan text below is retained as history and superseded where E1/E2 are described as unimplemented.

---

## Terrain Edges scaffold (2026-10-18)

`Edges/` now contains dormant, Inspector-editable material profiles, an eight-neighbour mask helper, an explicit `TerrainRebuilt`-based material cache controller, and a renderer hook. **Nothing draws or attaches yet**; all edge visuals and the current Mars SVG bleed fix are pending. Start with [Edges README](Edges/README.md) and give Codex the [phased implementation plan](Edges/IMPLEMENTATION_PLAN.md). No changes to the grid/navigation/TileSet metadata or existing map art.

---

## Mars terrain tiles (2026-10-18)

The new `Tiles/Mars/mars_ground_atlas.svg` adds **bright walkable/nonbuildable dust** and **dark impassable/nonbuildable rock** via source2 in the existing Passable/Impassable palettes. Artwork is a separate expandable 64px-cell vector atlas, not a replacement for any metal/old desert tiles. Nothing is painted into levels automatically; see `TileSets/README.md` and `Tiles/Mars/README.md`.

---

# Terrain — category-filtered authoring

Levels expose Terrain/Passable, Impassable, Interactive and Decorations TileMapLayers with matching TileSets under TileSets/. Passable contains metal/dirt floors; Impassable contains structural walls/rocks; Interactive contains raised Foundation and Lava; Decorations contains cosmetic decals. Boolean can_build/blocks_enemies rules are independent of appearance. Empty space is passable/unbuildable; decorations contribute no rules.

WorldGrid caches Passable → Interactive → Impassable, then applies Portal/Citadel reservations and separate runtime tower occupancy. Intentional Interactive-over-floor overlays are supported, including Lava's false construction permission. Structural blockers cannot become walkable through Lava overlays. Raised Foundation preserves its buildable/solid semantics and original artwork; all74 saved platforms moved explicitly to Interactive. See TESTS/Terrain/InteractiveTerrainMigration.md.

One world-scoped InteractiveTileManager handles immediate-entry and periodic Lava damage for only active occupants, with actual swept-segment exposure and no per-tile nodes or full-world enemy scans. Adjacent Lava retains one timer, exit removes it, teleports only enter their destination. Inspector LavaTileConfig owns damage/interval/entry toggle/type. Artwork is vivid orange liquid with tiny sparse highlights. Paint Lava in Terrain/Interactive source1; it has not been arbitrarily added to the playable map. See Interactive/README.md for complete behavior and tests.

Explicit WorldGrid.RebuildTerrain updates rules and reconciles hazards; hazard/build-permission-only changes do not unnecessarily rebuild A*. Blocking/goal changes retain navigation invalidation. Tower placement/removal retains incremental occupancy behavior. WorldTerrain.tres remains a legacy mixed palette; current scenes use the category palettes.
