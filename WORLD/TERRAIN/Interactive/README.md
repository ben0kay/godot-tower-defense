# Interactive Terrain — proposed implementation pass (PLANNED ONLY)

**Status: DOCUMENTATION ONLY.** Do not implement until the current tower-targeting / shared spatial-query performance pass has completed and been pushed. This is a separate focused follow-up for Codex; **no terrain, gameplay, art or scene changes are made by this plan**.

## Purpose

Use `WORLD/TERRAIN/Interactive/` for **terrain tiles with gameplay-specific interactions**, rather than classifying them only by art or material. The first two families are:

1. **Foundation** — a buildable placement surface that has explicit construction/blocking rules, but no time-based tile effect.
2. **Lava** — a passable, non-buildable hazard that deals **immediate damage when an enemy enters or crosses a lava cell**, not periodic damage while stationary.

Future Electrical, Cryogenic, Regeneration, Teleporter, Conveyor etc. can join as independently authored families sharing the smallest necessary manager/cell-access contract. Do not implement them in the Lava pass.

## Existing architecture to preserve

- Current levels have `Level/ Terrain/{Passable,Impassable,Decorations}` `TileMapLayer` nodes with separate `WORLD/TERRAIN/TileSets/` resources; `LEVELS/WorldLevel.cs` exposes those layers.
- `WORLD/GRID/WorldGrid.cs` caches `can_build` and `blocks_enemies` independently in compact arrays; navigation uses `BlocksEnemies` and player placement uses `CanBuild` plus reservations, occupancy, enemy clearance and path connectivity. The map uses configurable **64 px** cells by default.
- Today, the **existing raised/buildable platform** is already represented by a tile in `ImpassableTerrain.tres` (atlas coordinate `(1,0)`, `can_build=true`, `blocks_enemies=true`). Other existing Passable tiles also support construction. This plan must **not silently change those semantics or invalidate painted maps**.
- `ENEMIES/Movement/EnemyMovement.cs` advances enemy world positions through swept safe movement, potentially multiple short segments or waypoints per tick. `ENEMIES/Shared/Enemy.ApplyDamage(DamageInfo)` already owns shields, hull, death, rewards and optional tower damage attribution. Reuse it; do **not** duplicate damage math.
- The ongoing separate combat spatial-query pass may modify `EnemyRegistry` and movement notifications: **re-read latest HEAD and integrate with its final API** rather than overwriting its work.

## Proposed folder layout

```text
WORLD/TERRAIN/
  Interactive/
    README.md                        # this design/implementation plan
    InteractiveTileManager.cs       # ONE lightweight world/level-scoped effect dispatcher
    Foundation/
      README.md                      # placement-surface behavior; no repeated tick
    Lava/
      README.md                      # entry-triggered terrain hazard
      LavaTileConfig.cs              # per-type Godot Resource with tweakable damage
      DefaultLavaTileConfig.tres     # Inspector-authored baseline, future effect options
  TileSets/
    InteractiveTerrain.tres         # new palette for interactive tile types
    README.md
  Tiles/
    Interactive/
      Foundation/
        [placeholder / existing foundation artwork, migrate carefully]
      Lava/
        lava.svg                     # 64x64 tile art or atlas-compatible texture
LEVELS/
  Level001/Level001.tscn             # Terrain/Interactive TileMapLayer
  LevelTemplate.tscn                 # same layer for authoring new levels
TESTS/
  InteractiveTerrainChecks.cs       # tests in current production headless harness
```

Use the **existing world/level scene** for a single manager, not one Node, `Area2D`, `CollisionShape2D` or timer for each painted tile. Follow `NOTES/CodePreferences.md` for method separators, readability, resource organization and test documentation.

## Palette and layering / migration rules

Add `Terrain/Interactive` as its own paintable `TileMapLayer` with `InteractiveTerrain.tres`, containing **Foundation** and **Lava** tiles, rather than mixing them into Decorations. Their category means *interactive game rules*, not that every tile runs a script.

- The new layer can sit above the normal Passable floor to show foundation/lava art. Design **clear gameplay-rule precedence**: when an Interactive tile occupies a cell, its authored `can_build`/`blocks_enemies` values take precedence over ordinary Passable rules, while **Impassable structural blockers and Portal/Citadel reservations must never accidentally become walkable/buildable** because of an overlay.
- **Foundation:** preserve the current raised/buildable platform semantics when moving its editor choice into Interactive: `can_build=true`, `blocks_enemies=true` for the existing raised/platform variety. A future different foundation subtype may be buildable + walkable before construction, but **do not implicitly change existing platform collision/pathing**. An occupied tower still obeys the existing placement, incremental occupancy and navigation rules. Foundation does not inflict damage or require per-frame processing.
- **Lava:** `can_build=false`, `blocks_enemies=false`, so enemies are allowed to path through it but players cannot place towers there. If painted on an underlying Passable cell with `can_build=true`, the Lava overlay's `false` must actually deny placement. Impassable blockers must not become passable due to overlapping Lava.
- Migrate the existing foundation tile carefully (current impassable atlas coordinate `(1,0)`): **retain compatibility with already painted Level001/template cells, or perform a tested explicit map migration**. Never remove or remap its old TileSet entry before saved scenes have been updated. Avoid silently double-painting/overlapping gameplay layers. Keep stable visual appearance unless a new foundation art request is made.
- Extend `WorldLevel` and `WorldGrid.RebuildTerrain` minimally to load/validate/cache the Interactive layer, preserving existing `NavigationRevision`, `NavigationChanged`, structural placement checks and terrain rebuild semantics. Terrain hazard-only edits should not cause unnecessary A* repathing when `blocks_enemies` is unchanged.
- Use consistent 64x64 tile regions / current `WorldMasterConfig.CellSize`, editor-visible palette choices, source art and custom data. **No visual material colour test may control gameplay logic.**

## Lava tile — initial behavior

**Lava is primarily bright orange molten liquid**, not a dark rocky/cracked-ground tile.

### Lava art direction

- At normal top-down gameplay zoom, **most of each tile's visible area should be rich, vivid orange** (with warm amber/yellow variations). It should unmistakably read as a flowing pool of lava.
- Use **only tiny, sparse molten flecks/streaks/bubbles**, subtle bright yellow highlights and limited dark accents. Avoid huge black crust patches, dominant dark-red/brown rock, thick cracked ground, or blotchy noisy textures.
- Keep silhouette/texture legible at 64x64, seamless/visually coherent when adjacent Lava tiles are painted, suitable for a clean sci-fi/vector-ish battlefield. Start with one attractive placeholder atlas tile; fancier animation/shaders can come later. Avoid adding unnecessary per-cell particle nodes.

### Damage semantics

- A living enemy takes **one immediate hit when entering each distinct painted Lava cell**, including entering across a cell boundary while moving quickly.
- If an enemy crosses multiple Lava cells in a single physics tick, **apply each entered cell's hit**, in traversal order. Do not rely only on the enemy's final cell; no tunnelling through thin lava strips. Carefully handle diagonal/corner crossings and each movement subsegment in `EnemyMovement`.
- **No repeated damage just for standing still** on lava, no global repeating tile timer, and no damage on every ordinary movement tick while the enemy remains in the *same* lava cell. Re-entering a previously exited lava cell is another entry and can damage again.
- Decide and test spawn/teleport behavior explicitly: an enemy spawning/teleporting **onto** Lava should trigger at most one initial entry hit, rather than silently remaining immune or applying damage several times. Do not accidentally treat a long teleport as walking through every intervening cell unless intended.
- Lava can damage enemy shields then hull, following the exact existing `Enemy.ApplyDamage(DamageInfo)` pipeline; dead/arrived/unloaded enemies must not be damaged again. If Lava kills an enemy, do not keep moving or applying more Lava hits in the same tick. Preserve existing death/reward logic and don't award tower XP for unattributed terrain damage.
- Existing damage types are currently only `Kinetic`, `Energy`, `Explosive`. For the first Lava pass, use **Energy as an explicitly temporary damage-type setting** in `LavaTileConfig`; do not introduce a global Thermal/Burning type or modify the damage resolver without a separate design decision.
- Expose configurable **DamagePerEntry** (for example, provisional `5` base points) and **DamageType** on one Inspector-editable `LavaTileConfig` resource. They are playtest defaults, not hardcoded global multipliers. Defer burning/DoT, tile-based timers, resistances, area multipliers and player damage.

## Efficient implementation contract

The manager should read/categorize painted interactive cells **at level load and explicit terrain edits**, storing cheap per-cell type/config lookup. On actual enemy movement, inspect only the grid cells crossed by that enemy's movement segment. An ordinary non-interactive cell check should be O(1) or bounded to crossed cells; no per-tile tick, no list scan over all painted Lava tiles or all living enemies every frame.

Prefer a small, clear movement hook that consumes `oldWorldPosition -> newWorldPosition` for **each actual moved segment**, using `WorldGrid.WorldToCell` / consistent transforms. Include an entry-state mechanism to prevent duplicate hits for the same cell when subsegments/tick boundaries touch it. Trigger before or immediately after movement with safe re-entrancy: terrain damage can kill/unregister/QueueFree an enemy during the movement loop. Update the new spatial-index membership (if installed by the preceding pass) in a consistent order; do not introduce position/index desynchronization.

Foundation is **passive static terrain metadata**: no entry/tick callback and no per-cell processing. The manager should be extensible without an elaborate generic effects framework or a new system duplicating `WorldGrid`'s occupancy/navigation authority.

## Tests / acceptance for Codex

1. Editor palette exposes Foundation and Lava from `Terrain/Interactive` in **Level001 AND LevelTemplate**; visuals load; old painted foundation cells preserve their appearance and routing after explicit compatible migration.
2. Foundation remains buildable with its current enemy-blocking semantics, and tower placement/occupancy/navigation continue to work; Lava is passable and non-buildable, even when over an ordinarily buildable Passable floor.
3. Enemy enters Lava -> immediate configured damage once; remains within same cell -> no extra damage; exits and re-enters -> new hit; crosses 2+ Lava cells in one frame -> hits every entered cell; an adjacent normal cell -> no damage.
4. Test diagonal and high-speed movement, partial/corner intersections, short segments, stationary enemies, spawn/teleport onto Lava and 30/60/120 Hz outcomes.
5. Verify shield-first/hull response, death once, kill reward once, no tower XP, removal/unload safety and no other enemy movement regressions; adjust per-entry base value solely via the config resource.
6. Validate unchanged First/Closest tower targeting and the new spatial-query optimization, existing 500-enemy repath/navigation checks, menu, wave spawning, tile category and placement fixtures. Do not overwrite in-flight combat optimization changes.
7. Record actual build/headless test results; keep old tile art working; document any manual in-editor / rendered checks not run.

## Scope fence

**This follow-up pass implements only Interactive terrain foundation + Lava entry damage, basic art and authoring**, not new enemy damage types, Cryo/Electric tiles, passive DOT, advanced shader animations, research, tower HP, routes that avoid lava, navigation cost weights or changes to the ongoing spatial-query performance pass.

When complete, update `WORLD/TERRAIN/README.md`, `WORLD/TERRAIN/Rules/README.md`, `WORLD/TERRAIN/TileSets/README.md`, `WORLD/TERRAIN/Tiles/README.md` and this file with **actual** implemented behavior instead of plans. Add/read `Foundation/README.md` and `Lava/README.md` for the first real tile types.

## Copy-ready Codex request

> Once the tower targeting / AoE spatial optimization pass is merged, implement the **Interactive Terrain + Foundation/Lava** pass specified in `WORLD/TERRAIN/Interactive/README.md`. Preserve the current raised foundation tile's buildable/blocked semantics and migrate its authoring category safely. Add a new interactive TileMapLayer, editor palette and a vivid predominantly orange Lava 64x64 tile with only tiny molten flecks. Lava must be walkable/nonbuildable and deal one immediate `DamageInfo` hit per crossed/entered cell, including fast swept movement, but no repeated idle/tick damage. Use one efficient manager, no per-tile physics nodes, and the existing enemy damage/shield/death machinery. Include Inspector-configured base damage, thorough correctness/navigation/placement performance regression tests, documentation updates, real compilation and headless runs. Commit and push, report actual results.
