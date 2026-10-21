## E1/E2 implementation — 2026-10-21

E1 clips both64×64 SVG atlas cells independently, removes the dust's repeated gradient/sediment bands in favour of low-contrast seamless grain, and softens the rock's repeating black strokes. Source IDs, atlas coordinates, TileData flags and saved maps are unchanged.

E2 now has a shared, material-profile-driven renderer. WORLD/World.tscn attaches one TerrainEdges controller/renderer after Grid startup. It classifies painted materials in existing layer precedence, builds deterministic cached L8 silhouette masks, and applies a shared lightweight mask shader to existing TileMapLayers. The opaque square tile pixels are actually cut away. Tiny recorded texture quads on one helper per participating layer fill rounded inward corners around holes. No per-cell nodes, enemy checks, terrain sampling in physics/process callbacks, full-map per-frame image updates, tile repainting, or navigation rebuild requests are added. Towers only change occupancy, so placing them does not regenerate these visual masks.

**Opt-in rollout:** DefaultTerrainEdgeConfig.tres remains Enabled=false for desktop GPU/appearance review, especially given the current unrelated rendering-stall investigation. Set Enabled=true and restart the level to try E2. MarsDustEdge is the initial Soft profile; MarsRockEdge.RenderEdges=false and jagged/material transitions remain E3. To opt another tile material in, create a TerrainEdgeProfile, set a stable MaterialId, Layer, SourceId and AtlasCells, choose Shape=Soft / RenderEdges=true, then add it to the master Profiles array. No renderer code references Mars names. Same-material variants merge; other painted materials protect joins instead of introducing transparent gaps. Straight profiles keep original rendering. EdgeTint and a dedicated Jagged geometry style remain future E3 work.

MaskPixelsPerCell defaults16 (8–32); MaximumMaskMegapixels defaults16 per layer, falling back to original rendering if exceeded. Master disable plus controller.Rebuild (or level restart) restores exact original layer material references and releases masks/fills. Changing profile/settings values requires an explicit rebuild/restart. All generation is on attachment or WorldGrid.TerrainRebuilt; future local hints/chunk invalidation remain deferred. Native rendering still samples the mask each drawn pixel; caching removes CPU generation, not GPU draw cost.

Supported MVP: static64×64 square atlas artwork, zero texture origin, grid-aligned source layers, including transformed worlds and whole-cell source translation. Existing custom layer/tile materials, tint/Z overrides, Y sorting, subcell/rotated/scaled source layers, animated or oversized opted-in atlas tiles are left untouched with a warning. Existing layers retain animation/tint/atlas handling for unprofiled tiles through a nearest-filtered per-cell applicability texture; profiling masks never replace gameplay or collision data.

Validation: C# build/import and TESTS/Terrain/TerrainEdgeRenderingChecks.tscn pass, covering generic second-material opt-in, isolated/connected tiles, corner/hole math, cached deterministic output, transformed source mapping, unsupported-transform fallback, disable/restoration, no navigation/AStar from visual rebuilds, no mask rebuild on tower occupancy, and memory-budget fallback. Runtime-only Level002 fixtures are discarded; no saved level was repainted. A4800-tile fixture generated in32.69ms after interior-row/empty-cell fast paths (earlier unoptimized runs270–307ms) on this host with1,269,837 bytes of masks and one surface; this is a startup CPU result, not desktop FPS. Godot SVG raster/mask outputs and a CPU composite were visually inspected; desktop shader rendering/filtering at real gameplay zoom is still pending. Prior future-plan text below is retained as history and superseded where E1/E2 are described as unimplemented.

---

# Terrain Edges — foundation (not rendering yet)

**Status: scaffold only.** Nothing in any live world/level scene instantiates or attaches the controller or renderer. The default config is **Enabled=false**. This commit does not modify Mars artwork, paint maps, smooth corners, hide squared cells, change TileSets, or enable masking. The visible seams/black marks in the current Mars atlas remain a separate artwork issue.

## Files and ownership

| File | Purpose | Current status |
| --- | --- | --- |
| `TerrainEdgeProfile.cs` | Authored material identity (layer + source ID + one or more atlas coordinates) and future edge appearance | Working resource |
| `TerrainEdgeMasterConfig.cs` / `DefaultTerrainEdgeConfig.tres` | One config with deterministic seed and opt-in profile list | Working, disabled |
| `Profiles/MarsDustEdge.tres` | Passable source2 (0,0), soft ochre dust | Defined |
| `Profiles/MarsRockEdge.tres` | Impassable source2 (1,0), angular darker rock | Defined |
| `Profiles/MetalEdge.tres` | Passable source1 (2,0) + (3,0), clean metal sample; not registered in default config yet | Future option |
| `TerrainEdgeNeighbourhood.cs` | Eight-direction matching-material mask, pure function | Working |
| `TerrainEdgeController.cs` | Optional explicit `Attach(WorldGrid)`, source ID → material cache, correct layer precedence, subscribe to `TerrainRebuilt` | Working, not attached |
| `TerrainEdgeRenderer.cs` | Optional renderer subscription/lifetime shell | **Does not draw** |
| `IMPLEMENTATION_PLAN.md` | Ordered Codex tasks, visual masking pitfalls, acceptance tests | Planned work |

**Material identity is not gameplay metadata.** `mars_dust` is visually similar between its present tile and future dust variants; all of those coordinates should share a single `MaterialId`, but every TileData retains its own `can_build` and `blocks_enemies` flags. No gameplay permission is inferred from profile, color or edge geometry.

`TerrainEdgeController.Rebuild()` reads painted `TileMapLayer` data **only on explicit calls**, respecting the existing `Passable → Interactive → Impassable` precedence and ignoring cosmetics. Unknown higher-layer materials conceal lower-layer material identity. It does *not* inspect runtime towers: tower occupancy/route blocking must remain with `WorldGrid`. The controller's material dictionary contains only mapped, visible painted cells; unknown/empty cells are background. Eight-neighbour masks supply both outer and inner corners; no user scene includes the controller yet.

## Future integration

Once the masking implementation exists, add **one** controller + renderer per world, load `DefaultTerrainEdgeConfig.tres`, attach to the sibling `Grid` after its `_Ready`, and place the renderer in the level's intended draw order. Keep all scene modifications in the Codex render pass. Don't attach a visible renderer that merely draws outlines while the original solid square TileMap underneath remains visible — masking/replacement is mandatory for actual organic boundaries.

See **[IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md)** for the next exact coding steps. Tests: `TESTS/Terrain/TerrainEdgeFoundationChecks.cs` through the existing integration runner; Godot execution still needs verification.
