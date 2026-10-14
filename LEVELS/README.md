# LEVELS — Authored Maps, Settings and Waves

- `Level001/Level001.tscn`: the live terrain/placement test map, mounted as `World/Level`.
- `LevelTemplate.tscn`: empty paint-ready scene with a Citadel and shared TileSet; defaults to MapEdges and requires no Portal.
- `WorldLevel.cs`: typed scene root exposing terrain layers, level settings and destination.
- `LevelConfig.cs`: Inspector-editable Resource with Starting Resources / StartingCredits, `SpawnMode` (Portals / MapEdges / Both), `AllowedEdges`, root-relative `ActivePortals` NodePaths and `CitadelPath`.
- `DefaultLevelConfig.tres`: template defaults. Duplicate/make unique the settings resource for each new level.

Open a level in the 2D editor and paint Ground/Walls/Decorations. Save and press F5 to test the game. To choose another map, change the `Level` scene instance in `WORLD/World.tscn`, keeping the node name `Level`. Do not copy the camera/HUD/tower controller into individual maps.

`WorldMasterConfig` remains the single source for map dimensions and cell size: default 100×80, 64 px. Paint within those bounds. The shared TileSet is 64 px; its size must match the configured grid. Keep terrain layers aligned to the world grid without arbitrary scaling/rotation.

Physical Portals belong under `Portals`; the Citadel belongs under `Objectives`. Position object roots at cell centers. A Citadel root identifies the upper-left cell center of its footprint, rather than the overall footprint center. Place `SpawnExit` and `ApproachPoints` in unblocked cells. Paths in LevelConfig are relative to the level root, not Main.

Spawning lives in ENEMIES; the shared wave scheduler/catalog live in LEVELS/Waves. Level resources declare permitted sources and WaveSetId. Background sprites are independent of the grid. Empty space is unbuildable and passable. See `Level001/README.md` for controls and the atlas palette.

## Starting resources
Select the active level settings .tres and edit Starting Resources / Starting Credits. Level001Config.tres starts at 1,000; DefaultLevelConfig.tres starts at 0. Main/Economy loads this value into a fresh session wallet and pushes it to the HUD. Changing settings during play does not refill the wallet; restart the level to apply saved starting values. Credits are currently the only starting resource; optional tower material costs already use shared ResourceCost entries.

## Wave sets
LevelConfig / Waves / WaveSetId references a C# wave set in LEVELS/Waves/WaveCatalog.cs. Level001 uses level001; the empty default disables waves. Wave timing/counts are edited in LEVELS/Level001/Level001Waves.cs, not large Inspector arrays. Unknown set IDs show a setup error; there is no implicit fallback. ActivePortals remains the authoritative allowed-source list.


## Folder ownership

Each level folder is its authoring home: Level001/ contains Level001.tscn, Level001Config.tres and Level001Waves.cs, including matching script .uid files. Future levels follow the same pattern and register their wave set in Waves/WaveCatalog.cs.

Waves/ holds the shared scheduler, immutable data/builder helpers and catalog; Waves/Authoring.md describes batch editing. Debug/ holds shared level debug helpers. WorldLevel.cs, LevelConfig.cs, default settings and LevelTemplate.tscn remain shared at this root.

WORLD/ still owns reusable terrain, grid/navigation, portals, objectives and scenery. The runtime map is still mounted at World/Level by WORLD/World.tscn; moving files does not change scene-tree paths, level-relative portal references or gameplay behavior. Existing Godot .uid values are preserved.

---

## Planned cleanup phase — Separate terrain authoring palettes (Codex: ONE phase, not yet implemented)

**Purpose:** Simplify level painting so selecting a terrain scene node shows only the tiles appropriate for that category in the bottom Godot TileMap palette. The designer should never have to remember which of six mixed atlas tiles are walls versus floors. **This is an implementation plan only; do not confuse it with the current, working `Ground/Walls/Decorations` system described earlier in this README.** Make this a single, self-contained cleanup pass before adding more maps or elaborate final terrain art.

### Target editing experience

```text
Level001 (and LevelTemplate / future levels)
├── SpaceBackground/                     # Existing starfield/nebulae: visuals, not tile collision
├── Terrain/
│   ├── Passable       (TileMapLayer)      # Walkable/flyable terrain: buildable OR unbuildable
│   ├── Impassable     (TileMapLayer)      # Solid obstacles/platforms: buildable OR unbuildable
│   └── Decorations    (TileMapLayer)      # Purely cosmetic decals; no gameplay influence
├── Portals/
└── Objectives/
```

| Layer selected in scene tree | What its *own* bottom TileSet palette should offer | Gameplay meaning |
| --- | --- | --- |
| **Passable** (renamed from Ground) | Open metal floors, paths, open dirt surfaces; later any non-buildable path materials | Each tile has `blocks_enemies=false`, but may have `can_build=true/false` independently. |
| **Impassable** (renamed from Walls) | Metal walls, rocky obstacles, cliffs, raised solid build platforms | Each tile has `blocks_enemies=true`, but may have `can_build=true/false` independently. |
| **Decorations** (keeps name) | Surface decals, cracks, stains, markings and background *visual-only* details | Always ignored by `WorldGrid` for movement and construction, regardless of underlying tile. |

**Important:** `Passable` and `Impassable` represent **movement categories**, not building permission or art style. Metal, dirt and rock can appear in whichever category their actual gameplay rules require. A solid raised platform is **im**passable to enemies **but buildable for a tower**. A plain enemy path may be **passable but not buildable**. Do not incorrectly assume `can_build = !blocks_enemies` or force every non-buildable tile into Impassable. A tower placed on a passable buildable tile still becomes a runtime obstacle; a tower on an already-solid buildable platform follows existing occupancy/path rules.

**SpaceBackground stays separate.** Unpainted map cells currently mean **passable empty space but unbuildable for towers**, and this behavior must be retained. Decorations do not automatically make the cell buildable; they can be painted over empty space or either gameplay layer without changing that cell's rules.

### Shared TileSet resources — distinct palettes, common metadata

The three layers currently share `WORLD/TERRAIN/TileSets/WorldTerrain.tres` and `Tiles/Metal/prototype_atlas.svg`. This is **why Godot shows all six tiles on every selected layer**; a scene-node rename alone will not filter the bottom palette.

For this cleanup, create three independently assignable **TileSet resources** (for example under the existing `WORLD/TERRAIN/TileSets/`):

```text
WORLD/TERRAIN/TileSets/
├── PassableTerrain.tres           # 64×64, only passable authored tiles
├── ImpassableTerrain.tres         # 64×64, only solid authored tiles
├── DecorationTerrain.tres         # 64×64, only cosmetic authored tiles
└── WorldTerrain.tres              # Original legacy asset: retain until migration verified
```

Each TileMapLayer points to its matching TileSet, **not the same all-inclusive shared resource**. The three TileSets may share the original `prototype_atlas.svg` artwork; do **not** duplicate the actual SVG texture unnecessarily. Godot's `TileSetAtlasSource` must define **only the corresponding tile coordinates**, so that selecting Passable does not show impassable choices. Keep all gameplay TileSets at the authoritative **64×64** resolution with custom-data Boolean layers named exactly `can_build` and `blocks_enemies`; preserve each tile's exact gameplay values, source ID/atlas coordinate mapping where practical, and visuals.

**Existing six atlas tiles (current behavior, source 0):**

| Atlas coord | Current tile | Destination category | `can_build` | `blocks_enemies` |
| --- | --- | --- | --- | --- |
| `(0,0)` | Dark metal wall | Impassable | false | true |
| `(1,0)` | Raised metal platform | Impassable | true | true |
| `(2,0)` | Open metal floor | Passable | true | false |
| `(3,0)` | Rocky obstacle | Impassable | false | true |
| `(4,0)` | Open dirt floor | Passable | true | false |
| `(5,0)` | Transparent decal | Decorations | no gameplay effect | no gameplay effect |

The initial atlas does **not yet contain a passable-but-nonbuildable floor**. Keep support for that combination in the data model so additional artwork can be added later; do not quietly change the existing open dirt/floor tiles' build permissions to invent one.

### Migration requirements — DO NOT erase the authored map

This is **not** an invitation to wipe or repaint `LEVELS/Level001/Level001.tscn`. Codex must inspect its current painted tiles before touching nodes or TileSets, and preserve **every existing cell's effective visual appearance and gameplay behavior** as closely as possible, plus the existing level/background/Portals/Citadel/marker positions.

1. **Preserve the existing map first.** Record or snapshot the currently saved painted cells and effective `CanBuild` / `BlocksEnemies` results for sample and representative coordinates. Keep all previously edited Level001 data, including overlapping cells. Perform the migration with a verifiable remap (script/tool or carefully verified scene conversion), not by resetting TileMapLayers.
2. **Resolve old overlap semantics before sorting.** Currently `WorldGrid.ReadLayer(Ground)` then `ReadLayer(Walls)`, so the **Walls tile wins** if both are painted at the same cell—even when that Walls tile is passable! Determine each cell's **effective tile/rules** using this priority and migrate that effective gameplay tile to the matching new category. Do not simply rename Ground→Passable and Walls→Impassable; those old layers can contain either kind. Keep cosmetic details separately where feasible; flag any conflicting/ambiguous overlaps instead of silently losing tiles.
3. **Use the new TileSets and node names.** Convert both `Level001.tscn` and `LevelTemplate.tscn` to `Terrain/Passable`, `Terrain/Impassable` and `Terrain/Decorations`; assign each its category-filtered TileSet. Existing decoration cells should remain visible and gameplay-neutral.
4. **Update shared source readers.** Replace `WorldLevel.Ground` / `WorldLevel.Walls` references and scene paths in `LEVELS/WorldLevel.cs`, `WORLD/GRID/WorldGrid.cs` and all relevant consumers/tests. Keep the current array-cached movement/build metadata, terrain rebuild/update mechanism and dynamic tower occupancy. `WorldGrid` should read Passable then Impassable; **Impassable overrides Passable in an accidental overlap** as a deterministic safety fallback, but authors should be encouraged to keep gameplay layers mutually exclusive at each cell. Never read Decorations into collision/build caches.
5. **Guard authoring mistakes.** Add lightweight load-time/editor diagnostics where sensible: wrong-type tile on a category layer, null/wrong-size TileSet, both gameplay layers painted at the same location, or blocked/unreachable Portal SpawnExit or all Citadel approach markers. Don't add expensive per-frame scans or automatically delete/convert designer tiles at runtime. When changing a tile from solid to passable (or vice versa), **erase the old gameplay-layer cell** and then paint the replacement in the other layer, otherwise the solid overlay wins.
6. **Keep existing functionality intact.** Preserve each layer's visible ordering (Decorations on top, solid structures above open floors), 64px grid alignment, tileset UID/resource references where possible, portal/Citadel reservations, navigation generation, random wave/round-robin F2 spawning, placement validation and headless tests. Update old hardcoded `Ground/Walls` test fixtures, including `TESTS/EnemyIntegrationChecks.cs`, `TESTS/WaveChecks.cs` and other references. Do not rename these gameplay concepts by a blind find/replace in unrelated classes.

**Acceptance / required one-phase tests:**

- In Godot, selecting **Terrain/Passable** shows **only** open-floor tiles; **Terrain/Impassable** shows **only** metal wall, platform and rocky obstacle; **Terrain/Decorations** shows **only** cosmetic decals. The correct palette appears without manually changing the TileSet source, and all three layers can be painted/erased.
- Level001's saved map remains intact; its original tiles and current per-cell traversability/buildability are preserved, including any previously painted tiles on the surprising old layers. LevelTemplate also opens and paints correctly. The legacy shared atlas asset should not be deleted until there are no references needing it.
- Test **all four combinations**: passable/buildable, passable/non-buildable (using a temporary test tile/resource if needed), impassable/buildable platform, impassable/non-buildable wall. Test empty-space and Decorations-no-effect behavior, Impassable-over-Passable precedence, and portal/Citadel footprint reservation.
- F5/F2 and manual waves still spawn when a permitted source can reach a valid Citadel approach. Intentionally blocking **all** routes should still fail safely with a clear message. Tower placement must never seal the sole available enemy route.
- Run the existing Godot .NET headless integration checks and fix actual regressions introduced by this migration. Update `LEVELS/README.md`, `LEVELS/Level001/README.md`, `WORLD/TERRAIN/README.md`, `WORLD/TERRAIN/Rules/README.md`, `WORLD/TERRAIN/TileSets/README.md` and any affected test documentation to describe the *implemented* system, removing outdated mixed-palette authoring instructions.

**Scope:** Treat the entire restructure above as **one Codex cleanup phase**, not an open-ended terrain rewrite. No new procedural map generator, scenery art overhaul, extra navigation algorithm or gameplay economy changes. Keep the existing tiles/level art and make level painting intuitive while retaining current behavior. This section is only a specification until Codex implements and verifies it.
