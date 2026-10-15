# Interactive Terrain — proposed implementation pass (PLANNED ONLY)

**Status: DOCUMENTATION ONLY.** Do not implement until the current tower-targeting / shared spatial-query performance pass has completed and been pushed. This is a separate focused follow-up for Codex; **no terrain, gameplay, art or scene changes are made by this plan**.

## Purpose

Use `WORLD/TERRAIN/Interactive/` for **terrain tiles with gameplay-specific interactions**, rather than classifying them only by art or material. The first two families are:

1. **Foundation** — a buildable placement surface that has explicit construction/blocking rules, but no time-based tile effect.
2. **Lava** — a passable, non-buildable hazard that deals **immediate damage on entering a connected Lava region, then damage at configurable intervals while the enemy remains on Lava**. Adjacent Lava cells are one continuous hazard, not fresh entry hits.

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
      README.md                      # entry + periodic terrain hazard
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

### Damage semantics — immediate entry + periodic damage

**This supersedes the earlier entry-only Lava design.** A connected group of adjacent Lava cells is **one continuous damaging area for an individual enemy**, even if the enemy traverses several Lava cells.

1. **Enter Lava:** apply one immediate `DamageInfo` hit using the Lava config when the enemy first crosses from non-Lava into Lava (provided `DamageOnEntry=true`, the default).
2. **Remain on Lava:** apply **`DamagePerTick` every `DamageIntervalSeconds` of continued Lava exposure**, including when the enemy stands completely still. The periodic timer is tracked **per affected enemy**, never once per tile.
3. **Move between adjacent Lava cells:** retain the same exposure timer and **do not apply a new immediate entry hit**. Moving across several connected Lava cells in one physics tick must not multiply damage just because cell boundaries were crossed.
4. **Leave Lava:** stop applying periodic damage, remove/stop the enemy's exposure state, and reset its timer. No lingering damage after exit.
5. **Re-enter Lava:** apply a fresh immediate entry hit (when enabled), and restart the periodic timer from zero. A separate Lava region reached after crossing a normal tile is a new entry, even within one physics tick.
6. **Fast crossings:** inspect the **swept movement segment**, not just its final cell, so a fast enemy passing through a Lava strip receives its entry hit even if it leaves in the same tick. Periodic ticks are based on actual time spent on Lava; do not falsely credit time spent outside the hazard. No tunnelling through narrow strips or duplicate corner/cell-edge entries.
7. **Spawn or teleport onto Lava:** treat arrival on Lava as a single initial entry (and begin the periodic timer), but **do not** treat a teleport as travelling through all intervening Lava tiles. Explicitly test teleports and newly spawned stationary enemies.
8. **Death/removal:** once an enemy dies, arrives or exits the tree, stop its Lava state and further damage immediately; do not continue moving/attacking it later in the same movement loop. Repeated Lava hits must not duplicate death/rewards.

**Inspector-editable `LavaTileConfig.cs` and `DefaultLavaTileConfig.tres`:**

| Setting | Proposed playtest default | Meaning |
| --- | --- | --- |
| `DamagePerTick` | `5` base points | Damage per immediate hit and every periodic tick |
| `DamageIntervalSeconds` | `0.5` seconds | Interval between hits while continuously on Lava; must be finite and strictly positive |
| `DamageOnEntry` | `true` | Whether entering/re-entering immediately damages the enemy |
| `DamageType` | `Energy` | Temporary existing type until a deliberate Thermal/Burning design pass |

These are **authored settings on the Lava config**, not constants in `InteractiveTileManager`, `Enemy` or the global damage resolver. Use the same `DamagePerTick` for entry and periodic hits for this first version. If `DamageOnEntry=false`, the first hit happens after the configured interval instead. Validate all settings and safely handle large physics deltas without silently dropping owed periodic ticks.

**Example:** with `DamagePerTick=5`, `DamageIntervalSeconds=0.5`, `DamageOnEntry=true`, an enemy remaining continuously on Lava through the 2.0-second mark takes `5` on entry and `5` at each of 0.5, 1.0, 1.5 and 2.0 seconds: **25 base points total before shield/hull modifiers**. Walking from one Lava cell into another does not reset this sequence.

Lava should use the existing `Enemy.ApplyDamage(new DamageInfo(...))` pipeline for shield/hull effects and kill rewards. **Environmental hits have no tower `DamageSource` and award no tower XP.** Current `DamageType` values are Kinetic, Energy and Explosive, so use Energy as a clearly provisional type; do not add Thermal damage, enemy resistances, burning status, or a second damage resolver during this pass.

## Efficient implementation contract

The manager should read/categorize painted Interactive terrain cells **at level load and explicit terrain edits**, storing a fast per-cell type/config lookup. **No one-node-per-tile, `Area2D`, physics shape, per-tile timer, or scanning all painted Lava tiles/enemies every frame.**

- Use `EnemyMovement`'s actual `oldWorldPosition -> newWorldPosition` segments to discover **enter / still on / leave** transitions, including cells swept through between samples. Report transitions efficiently and without double-entering at grid-line/corner boundaries.
- Maintain only a **per-enemy active-Lava exposure state** (a current-region flag and elapsed time or next tick time), with a shared manager updating **only active Lava occupants** each physics tick, including stationary ones. Avoid allocating new objects in every movement check; release states on exit, death, arrival and unload.
- **Continuous adjacent cells:** do not reset entry or tick schedule when crossing from Lava to Lava. A non-Lava gap ends one exposure and entering another Lava cell begins the next. Handle multiple enter/exit transitions inside one long movement segment.
- **Precise intervals:** interval timing must be independent of movement speed and reasonably consistent at 30/60/120 Hz. Account for sub-frame Lava occupancy when a fast enemy enters/exits during a tick; do not count the entire frame as on Lava after an end-of-frame entry. Handle large `delta` and multiple due hits safely.
- Apply entry hits **once** and periodic hits **only while exposed** via existing typed `DamageInfo`. A hit can kill/unregister an enemy during a movement callback; stop subsequent movement and Lava ticks safely, and keep the ongoing shared enemy spatial-index membership consistent.
- Repainting/changing a live tile should rebuild affected interaction lookup and reconcile active occupants without duplicate hits or stale damage. Keep the existing WorldGrid as sole authority for blocking, build permissions and navigation updates.

Foundation remains **passive static terrain metadata**: no entry/tick callback and no per-cell processing. Keep the first implementation small enough to extend later to Electrical or Cryogenic hazards without creating a generic effects engine.

## Tests / acceptance for Codex

1. Editor palette exposes Foundation and Lava from `Terrain/Interactive` in **Level001 AND LevelTemplate**; visuals load; old painted foundation cells preserve their appearance and routing after explicit compatible migration.
2. Foundation retains current buildable/blocked semantics and working tower placement/occupancy/navigation. Lava is passable and nonbuildable, including over an otherwise buildable floor.
3. **Immediate and periodic:** Lava entry damages immediately by `DamagePerTick` if enabled; remaining exposed for 0.5/1.0/1.5/2.0 seconds at default interval delivers exactly the expected ticks; disabled entry does not deal early damage. Changing `DamagePerTick`, `DamageIntervalSeconds` and `DamageType` in the config changes behavior without hardcoded manager values.
4. **Continuous tiles:** crossing 2+ adjacent Lava cells, including in a single physics frame, does **not** reset cooldown or generate repeated entry hits; a stationary enemy continues to receive scheduled damage; moving onto ordinary terrain halts ticks; exit/re-entry resets timer and adds a fresh entry hit.
5. **Swept movement:** very fast traversals across a Lava strip cannot miss their immediate entry hit; crossing a separate Lava region after normal ground in the same frame produces a new entry; test diagonal/corner boundaries, short movement segments, spawn/teleport directly onto Lava, and physics 30/60/120 Hz.
6. Verify damage to shields then hull through the existing resolver, death/rewards once, **no tower XP**, no post-death hits, robust cleanup/unload, and no unrelated enemy movement regressions; test large delta and no silent lost periodic ticks.
7. Preserve the newly completed tower/area spatial-query optimization, existing 500-enemy navigation/repath, menu, waves, terrain-category and placement suites. No overwrite of in-flight combat changes.
8. Record actual C# build/headless test outcomes; preserve prior art and document rendered/editor manual checks that could not be run.

## Scope fence

**This follow-up pass implements only Interactive terrain Foundation + Lava immediate-entry and while-standing periodic damage, basic art and authoring**, not new enemy damage types, Cryo/Electric tiles, lingering DoT after leaving Lava, advanced shader animations, research, tower HP, routes that avoid lava, navigation cost weights or changes to the ongoing spatial-query performance pass.

When complete, update `WORLD/TERRAIN/README.md`, `WORLD/TERRAIN/Rules/README.md`, `WORLD/TERRAIN/TileSets/README.md`, `WORLD/TERRAIN/Tiles/README.md` and this file with **actual** implemented behavior instead of plans. Add/read `Foundation/README.md` and `Lava/README.md` for the first real tile types.

## Copy-ready Codex request

> Once the tower-targeting / AoE spatial optimization pass is merged, implement the **Interactive Terrain + Foundation/Lava** pass in `WORLD/TERRAIN/Interactive/README.md`. Preserve the current raised foundation tile's buildable/blocked semantics and migrate its editor category safely. Add an Interactive TileMapLayer/palette and a **predominantly bright orange 64x64 molten Lava tile with only tiny sparse flecks**. Lava must be passable/nonbuildable, apply **immediate damage on entering Lava** and then **periodic damage every configurable interval while still on Lava**, including when the enemy stands still. `LavaTileConfig.cs` owns `DamagePerTick`, `DamageIntervalSeconds`, `DamageOnEntry` and `DamageType` as Inspector-editable settings (provisional defaults 5, 0.5s, true, Energy). Adjacent Lava cells count as **one continuous hazard**, never multiple entry hits or timer resets. Exiting cancels exposure, re-entry resets it; fast swept crossings still apply an entry hit. Use **one efficient manager and per-active-enemy timers**, **no per-tile nodes or global enemy scans**. Route damage through existing `Enemy.ApplyDamage(DamageInfo)` with no tower XP and preserve the prior spatial-index optimization and navigation. Add the detailed tests, compile/run headless regressions, update documentation, commit and push with real results.
