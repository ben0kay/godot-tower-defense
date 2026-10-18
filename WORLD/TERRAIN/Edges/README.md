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
