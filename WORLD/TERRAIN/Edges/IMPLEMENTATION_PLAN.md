## E1/E2 implementation — 2026-10-21

E1 clips both64×64 SVG atlas cells independently, removes the dust's repeated gradient/sediment bands in favour of low-contrast seamless grain, and softens the rock's repeating black strokes. Source IDs, atlas coordinates, TileData flags and saved maps are unchanged.

E2 now has a shared, material-profile-driven renderer. WORLD/World.tscn attaches one TerrainEdges controller/renderer after Grid startup. It classifies painted materials in existing layer precedence, builds deterministic cached L8 silhouette masks, and applies a shared lightweight mask shader to existing TileMapLayers. The opaque square tile pixels are actually cut away. Tiny recorded texture quads on one helper per participating layer fill rounded inward corners around holes. No per-cell nodes, enemy checks, terrain sampling in physics/process callbacks, full-map per-frame image updates, tile repainting, or navigation rebuild requests are added. Towers only change occupancy, so placing them does not regenerate these visual masks.

**Opt-in rollout:** DefaultTerrainEdgeConfig.tres remains Enabled=false for desktop GPU/appearance review, especially given the current unrelated rendering-stall investigation. Set Enabled=true and restart the level to try E2. MarsDustEdge is the initial Soft profile; MarsRockEdge.RenderEdges=false and jagged/material transitions remain E3. To opt another tile material in, create a TerrainEdgeProfile, set a stable MaterialId, Layer, SourceId and AtlasCells, choose Shape=Soft / RenderEdges=true, then add it to the master Profiles array. No renderer code references Mars names. Same-material variants merge; other painted materials protect joins instead of introducing transparent gaps. Straight profiles keep original rendering. EdgeTint and a dedicated Jagged geometry style remain future E3 work.

MaskPixelsPerCell defaults16 (8–32); MaximumMaskMegapixels defaults16 per layer, falling back to original rendering if exceeded. Master disable plus controller.Rebuild (or level restart) restores exact original layer material references and releases masks/fills. Changing profile/settings values requires an explicit rebuild/restart. All generation is on attachment or WorldGrid.TerrainRebuilt; future local hints/chunk invalidation remain deferred. Native rendering still samples the mask each drawn pixel; caching removes CPU generation, not GPU draw cost.

Supported MVP: static64×64 square atlas artwork, zero texture origin, grid-aligned source layers, including transformed worlds and whole-cell source translation. Existing custom layer/tile materials, tint/Z overrides, Y sorting, subcell/rotated/scaled source layers, animated or oversized opted-in atlas tiles are left untouched with a warning. Existing layers retain animation/tint/atlas handling for unprofiled tiles through a nearest-filtered per-cell applicability texture; profiling masks never replace gameplay or collision data.

Validation: C# build/import and TESTS/Terrain/TerrainEdgeRenderingChecks.tscn pass, covering generic second-material opt-in, isolated/connected tiles, corner/hole math, cached deterministic output, transformed source mapping, unsupported-transform fallback, disable/restoration, no navigation/AStar from visual rebuilds, no mask rebuild on tower occupancy, and memory-budget fallback. Runtime-only Level002 fixtures are discarded; no saved level was repainted. A4800-tile fixture generated in32.69ms after interior-row/empty-cell fast paths (earlier unoptimized runs270–307ms) on this host with1,269,837 bytes of masks and one surface; this is a startup CPU result, not desktop FPS. Godot SVG raster/mask outputs and a CPU composite were visually inspected; desktop shader rendering/filtering at real gameplay zoom is still pending. Prior future-plan text below is retained as history and superseded where E1/E2 are described as unimplemented.

---

# Codex implementation handoff — Terrain Edges

**Goal:** soften and vary the visible silhouette of Mars Dust against space/empty cells and later between other materials, without touching passability, tower building, routing, saved tile IDs, or painted level data. Keep 64×64 gameplay cells. The new typed config/material-cache/neighbour scaffolding in `Edges/` is implemented; **actual edge rendering and artwork correction are NOT implemented**.

## Constraints / invariants

1. Read `README.md`, `NOTES/CodePreferences.md`, `WORLD/TERRAIN/README.md`, `Tiles/Mars/README.md` and this file first. Aim for **one atomic GitHub commit per requested Codex job**, with docs and tests. Do not rename/reorder existing atlas sources or change active levels merely to exercise this feature.
2. Existing `WorldGrid` owns cached gameplay `can_build`, `blocks_enemies`, reservations, tower occupancy and navigation revisions. **Do not** move or rewrite those rules or mutate the existing `TileMapLayer` cells to draw curved visual edges.
3. Materials are keyed by `TerrainEdgeProfile.MaterialId`, not tile texture/color or whether the cell is passable. Source+atlas coordinates distinguish authored variants, all variants of one material ID blend internally. Layer precedence is `Passable → Interactive → Impassable`; Decorations do not affect gameplay or edge classification.
4. World-scoped and **opt-in**. No per-cell `Node`, no enemy checks, no `_Process` or `_PhysicsProcess` terrain sampling. Update once on load/explicit `WorldGrid.TerrainRebuilt`, and eventually only changed cells + 1-cell neighbourhood when authoring APIs provide local-change hints. Avoid calling `WorldGrid.RebuildTerrain` just to regenerate visuals.
5. Neighbourhood coordinates must use `WorldGrid.WorldToCell(layer.ToGlobal(layer.MapToLocal(tile)))` for transformed maps. Out-of-bounds/empty/unprofiled cells represent a different material. Use all 8 neighbours; handle diagonal-only contacts, 90-degree corners, holes and narrow 1-tile strips.
6. Visual edges **must actually hide/replace the square shape**, not just paint an organic-looking line over opaque existing squares. Explore a reusable tile-overlay mask/shader or baked cached `ImageTexture`/surface; avoid expensive full-map Image updates every frame. Confirm opacity, clipping, antialiasing, draw order and pixel filtering at the real in-game zoom.
7. `TerrainEdgeMasterConfig.Enabled` is currently false and the system is not instantiated anywhere. Enable/integrate only after the visuals pass tests; keep a switch to disable edges for performance/diagnostics and preserve original map appearance.

## Suggested sequential implementation passes

### E1 — Fix current Mars atlas artifacts (art-only)

- Clip each 64×64 region inside `Tiles/Mars/mars_ground_atlas.svg` with an explicit SVG clip path (e.g. `x=0..64` for dust, `x=64..128` for rock). Some rock shapes have negative coordinates or extend beyond 64px, which can bleed into the neighbouring atlas cell.
- Remove the recurring dark vertical slits, over-obvious horizontal gradient bands and near-identical texture repetition in long painted areas. Ensure dust stays light enough for dark enemies, rock stays darker/angular.
- Verify SVG rasterization in Godot, no neighboring-UV bleed when sampled from a packed atlas; if texture filtering leaks, consider a safely padded separate texture/source for a material. Avoid changing existing tile IDs and artwork filenames.
- Capture a zoomed-out before/after of multiple adjacent tiles and check all four edges.

### E2 — One-material organic silhouette MVP

- Build the *actual* renderer inside `TerrainEdgeRenderer.cs`, consuming `TerrainEdgeController.Materials` and `TerrainEdgeNeighbourhood.Matching`.
- Start with `mars_dust` against **unpainted/space** only. Respect `MarsDustEdge.tres` softness/irregularity/seed. Inside contiguous dust, no visible internal seams; only the **external silhouette** becomes subtly rounded/uneven.
- Choose one concrete solution for occluding opaque square backgrounds (cached precomposed masked dust layer, tile shader with neighbour bitmask/atlas, or carefully controlled overlay/replacement). Document how the original TileMap dust pixels are hidden wherever the mask cuts them away. The implementation must work across multi-tile regions and update on terrain change. For MVP, assume all cells are 64×64.
- Use deterministic variation derived from `VariationSeed` + cell coords; do not animate, randomize every run, or recalc in draw loop. Build cache once and redraw on changes.
- Add a world-scoped `TerrainEdges` grouping Node2D with controller/renderer under the **world** (consistent with existing `Grid` and `InteractiveTiles`), but do not clutter every level tile cell with nodes. Ensure proper z-order: backgrounds, terrain, edge compositor, towers, enemies, VFX; do not cover tower/ememy sprites, Portals or Citadel.
- When disabled, existing unmodified TileMap visuals return exactly; materials/profile assets should remain available for future edit.
- Tests: isolated dust tile, 2×2 block, one-cell corridor, L shape, inner concave hole, large contiguous region, map edge, zoom transitions, no visual seam/flicker at 64px boundaries, no extra navigation rebuilds. Start at Level002 with a small temporary runtime fixture and restore it; do not repaint saved scenes.

### E3 — Material boundaries and expansion

- Add `mars_rock`: rugged cliff-like jagged boundary, no alteration to its `blocks_enemies` rule. Avoid accidental soft dust glow around rocks.
- Define explicit **material priority / transition rules** where dust and rock or dust and metal overlap/meet. Adjacent plain tile layers are not automatically a continuous texture; decide which material covers which before implementing blends. Do not draw alpha-cut space gaps between two fully painted different materials.
- Add metal styles using the unregistered `MetalEdge.tres` as a starting point; keep clean straight edges for buildings/foundations. Extend with distinct metal walls if desired. A metal tile only matches its explicit layer+source+atlas mapping.
- Author future dust/rock variations by adding new atlas cells and registering their coordinates in the same `TerrainEdgeProfile.AtlasCells`, preserving atlas IDs and gameplay TileData. Eventually add optional seeded variants in the *visual* layer without rewriting user-painted gameplay terrain.
- Optional later: cached chunk updates for very large maps, multiple biomes, editor gizmos for corner diagnostics, and debugging labels. Not needed for E2.

## Acceptance before marking visual edges implemented

- Feature disabled: pixel-identical existing terrain; gameplay identical.
- Enabled: no visible black slits, 64px tile grid seams, clipped rock textures bleeding into dust, or opaque square dust outline; outer/inner corner visuals are correct.
- Neighbour masks are stable under painting order; a variant of the same material creates no edge. Results are deterministic across restarts using a fixed seed.
- Level001/002/020 navigation, construction rules, portals, objectives, hazard precedence and previously authored tiles remain unchanged. Benchmark one large map against feature off to ensure no per-frame generation.
- Add headless tests for material lookup, masking pattern selection and change invalidation; visual verification requires screenshots at real gameplay zoom because headless tests cannot assert appearance.
- Update this plan and `Edges/README.md` with actual approach and limitations after each Codex pass. Do not claim this plan's future items are done until verified.
