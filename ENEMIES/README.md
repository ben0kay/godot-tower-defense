# ENEMIES — Enemy Ships, Navigation, Spawning, Waves and Targeting Integration

This top-level folder owns **enemy gameplay mechanics** in the Godot 4 C#/.NET space tower-defense project. The first enemy will be a deliberately simple **basic test ship** named **`Enemy001` internally** — **not** "ScoutShip". This is the baseline implementation that subsequent enemies can reuse.

**Implemented baseline (October 2026):** Enemy001, shared enemy state/registry, budgeted AStarGrid2D, portal/edge spawning, debug F2 and Battle Tower targeting/projectiles are now present. Level001's Debug parent owns F2 and the debug HUD. Existing terrain, camera, HUD and placement remain intact. See Types/Enemy001/README.md and LEVELS/Level001/README.md for playtesting.

The planning sections below describe the architecture and acceptance goals. Current targeting defaults to **First** (shortest remaining route), with **Closest** selectable in BattleTowerConfig. Manual finite waves and the credit wallet are active; armor mechanics and Citadel health remain future work.

## Folder and file structure

```text
ENEMIES/
├── EnemyMasterConfig.cs                # Global enemy-system tuning (not per-type balance)
├── DefaultEnemyConfig.tres
├── README.md
│
├── Shared/
│   ├── Enemy.cs                        # Reusable enemy instance: health, damage, death, state
│   ├── EnemyDefinition.cs              # [GlobalClass] Resource: per-type identity/stats/scene data
│   ├── EnemyRegistry.cs                # Active living enemies for tower target queries
│   └── README.md
│
├── Types/
│   ├── README.md
│   └── Enemy001/
│       ├── Enemy001.tscn               # First basic test ship scene
│       ├── Enemy001.tres               # Editable EnemyDefinition Resource
│       ├── Artwork/
│       │   └── enemy001.svg            # Simple top-down test artwork
│       └── README.md
│
├── Movement/
│   ├── EnemyMovement.cs                # Waypoint following / facing / speed
│   └── README.md
│
├── Spawning/
│   ├── SpawnManager.cs                 # Spawn *mechanics* (Portal or map-edge sources)
│   ├── README.md
│   └── MapEdges/
│       └── README.md
```

The folder names and filenames **`Enemy001/`, `Enemy001.tscn`, `Enemy001.tres` and `enemy001.svg` are intentional**. Additional variants will follow `Enemy002`, `Enemy003`, etc.; they should usually share `Enemy.cs` and `EnemyMovement.cs` rather than receiving a duplicate C# script for each type.

### Identity: filename vs editable game metadata

- **Internal file/folder identity:** `Enemy001` — stable even if the ship is renamed for players later.
- **Enemy key / spawn lookup ID:** a field in `EnemyDefinition`, provisionally `enemy001`, editable for future registration/spawn configuration. Keep IDs stable once referenced by saves or wave definitions.
- **Display name:** a separate editable string in `Enemy001.tres`, provisionally `Enemy 001` or `Basic Enemy`. This is not a requirement to rename the scene or files.
- **Type:** ordinary/basic hostile ship **for testing**. Do **not** bake a "Scout" archetype into filenames, source classes or permanent mechanics.

## First Enemy001 settings (starting values, not final balance)

Create an Inspector-editable `[GlobalClass] EnemyDefinition : Resource`; assign it via `Enemy001.tres`. Suggested grouped exports:

| Setting | Initial test value | Notes |
| --- | --- | --- |
| DisplayName | Enemy 001 | Temporary; editable |
| Key / EnemyId | enemy001 | Separate from display name |
| MaxHealth | 60 | Expose for tuning |
| MoveSpeed | 100 world pixels/second | Simple continuous movement |
| CitadelDamage | 1 | Applied only once arrival/health rules exist |
| Armor | 0 | Can remain unused in the first test |
| Visuals | 1 small SVG Sprite2D | Smaller than a 64px grid cell |

These are placeholders. Favor **Resource-driven data** over hardcoded stats inside `Enemy.cs`. Per-type settings belong in `Enemy001.tres`; global limits/debug/timing settings belong in `EnemyMasterConfig.cs` and `DefaultEnemyConfig.tres`. Add only meaningful global exports, not duplicate per-enemy properties.

## Responsibilities and integration boundaries

| Location | What it does | What it must NOT own |
| --- | --- | --- |
| `Shared/Enemy.cs` | Alive/dead state; current/max health; `TakeDamage`; death event/removal; exposes position/definition for targeting | Spawn-wave scheduling, level tile rules, portal art |
| `Shared/EnemyRegistry.cs` | Registers/unregisters living enemies for nearby tower target selection | Per-frame global rescan of the scene tree |
| `Movement/EnemyMovement.cs` | Follows a valid waypoint route, turns smoothly, stops safely if route unavailable/stale | Its own independent tile occupancy map |
| `Spawning/SpawnManager.cs` | Uses level settings to select valid Portal/map-edge spawn positions and instantiate enemies | Wave composition/timing; Portal scene internals |
| `LEVELS/Waves/` | Shared wave scheduling requests spawns from per-level authored definitions | Choosing physical map entry positions |
| `WORLD/PORTALS/` | Reusable physical Portal art and `SpawnExit` Marker2D | Enemy type selection or spawn schedule |
| `WORLD/OBJECTIVES/Citadel/` | Placed goal, solid footprint and reachable `ApproachPoints` | Enemy navigation algorithms |
| `TOWERS/` | Battle Tower targeting/projectile/damage logic | Enemy identity and path requests |

**Level spawn configuration already exists:** `LEVELS/LevelConfig.cs` exports `SpawnMode` = `Portals` / `MapEdges` / `Both`, `AllowedEdges`, root-relative `ActivePortals` NodePaths, and `CitadelPath`. Respect these settings rather than hardcoding Level001 or a single source type. Portal enemies emerge at a valid `SpawnExit`; edge-only levels do not require a Portal.

## Navigation is a prerequisite: follow the existing performance plan

Read `NOTES/NAVIGATION_PERFORMANCE_PLAN.md` and `WORLD/README.md`. The existing `WORLD/GRID/WorldGrid.cs` already caches terrain/tower blocking through `BlocksEnemies(cell)` and exposes a `NavigationRevision` updated after terrain/occupancy changes. **Reuse that authoritative grid**; do not create a competing interpretation of metal walls, open floors or tower blockers.

Suggested navigation implementation lives under **`WORLD/NAVIGATION/`** rather than per enemy:

1. Build a lightweight `NavigationGrid.cs` using the cached WorldGrid blocked cells; use Godot `AStarGrid2D` initially, preventing diagonal corner cuts.
2. Use a central `PathRequestManager.cs` with configurable **max path calculations per frame**. Enemies request paths on spawn or when invalidated, not every frame.
3. Find a reachable Citadel `ApproachPoint`, not the center of its blocked 3×3 footprint.
4. Follow path waypoints smoothly; turn the SVG toward travel direction.
5. If a path fails or becomes unsafe after a tower is placed, **stop and wait/retry safely**. Never move directly through walls as fallback.
6. Before a player builds on a currently enemy-passable tile, eventually reject placements that seal all required routes from active Portal exits and/or permitted map edges to the Citadel.

For this first pass favor correctness and simplicity. No RigidBody2D per ship, tile-by-tile scene nodes, per-enemy full A* each frame, always-on all-vs-all checks, or complex flow-field implementation.

## Tower targeting and basic damage (follow-up integration)

The active `TOWERS/Offensive/BattleTower/` baseline has editable stats: 256px range, 1 shot/second and 12 damage. These now work against `Enemy001`.

- Put **reusable targeting** in `TOWERS/Shared/Targeting/`, and **reusable basic projectiles** in `TOWERS/Shared/Projectiles/`; do not embed projectile behavior in Enemy001.
- Default to **First** (shortest remaining valid route) among living enemies within range; **Closest** remains selectable. Use `EnemyRegistry` and cache the chosen target; compare again on staggered intervals so overtaking can update First; validate cached range/alive state between queries.
- First projectile can fire from the **center** of the current one-piece Battle Tower sprite (no rotating multi-part tower/art overhaul yet).
- Projectile hits call `Enemy.TakeDamage(amount)`. Zero health marks the enemy dead, deregisters it and removes it cleanly. Avoid targeting or damaging already dead enemies.
- Leave stronger/weaker/first/last targeting priorities, upgrades, credits/rewards, special damage types, VFX and armor mechanics for later; First and Closest are already available.

## Suggested Codex implementation passes

### Pass 1 — Basic Enemy001 + correct movement

Implement `EnemyDefinition`, `Enemy.cs`, `Enemy001.tscn`/`.tres`, small `enemy001.svg`, EnemyRegistry, navigation and basic spawning. **No complete WaveManager needed yet.** Provide a simple Inspector-editable or debug-only way to spawn one or a few Enemy001 ships using Level001's Portal and later test permitted map edges. A ship should move around solid painted obstacles/occupied tower cells, arrive at a *reachable* Citadel approach point and be removed/marked arrived. No Citadel health implementation required yet.

### Pass 2 — Battle Tower automatic targeting and firing

Implement reusable target selection, basic bullet/projectile and damage flow. Spawn test enemies; placing Battle Towers near their route should cause automatic firing and eventual kills. The current HUD, world click-to-place/select, and Inspector must still work.

### Pass 3 — Correctness and scale validation

Test multiple ships, multiple buildable placements, navigation revision/repath behavior and valid spawn-source-to-goal connectivity. Add pathfinding/targeting budgeting according to the performance plan; confirm map-edge or mixed spawn modes use level settings. **Do not start full waves/economy/campaign logic yet.** Scale toward large swarms after measuring performance, not by prematurely overengineering.

## Minimum acceptance tests

- `Enemy001` exists on disk by that name, uses the shared C# enemy code, and its display name/key/stats can be edited without renaming scene/art files.
- In F5 play, the ship can emerge at Level001's Portal exit, navigate around **solid** painted tiles and reach a valid Citadel approach.
- Ships never cross blocked terrain, the Citadel's blocked footprint or a tower; failed routes stop safely.
- After Pass 2, a placed Battle Tower detects Enemy001 in range, shoots at its baseline configured rate and damages/kills it; out-of-range enemies are not targeted.
- Buildable passable cells occupied by towers invalidate affected paths; placement does not silently create an unwinnable sealed map once route checks are connected.
- Empty/open space stays traversable but unbuildable; current terrain painting, camera controls, Ctrl tower tray, Inspector and Portal/Citadel placements remain intact.
- Godot C# compiles and the scene can be playtested without missing resources or node-path errors. Document any deliberate unimplemented features.

## Project conventions

**C#/.NET only, not GDScript.** Follow `NOTES/CodePreferences.md`: short header comments explaining scripts, `// =========================================================` above methods, reasonable `#region` blocks, Inspector `[Export]` / `[ExportGroup]`, compact reusable code, and a README in each feature folder. Keep per-enemy nodes minimal; one central registry and a bounded navigation-request manager support future hundreds of ships. Refer to the user's existing `isometric-new-2d` project when appropriate.

**Current status:** Basic Enemy001 and Battle Tower combat are implemented. TESTS/EnemyIntegrationChecks.tscn validates movement, budgets, targeting, damage and placement connectivity. Larger-ship routing, ship separation, flow fields, waves/economy/objective health and advanced target priorities remain future work.

## Shared weighted route diversity

EnemyMasterConfig / ENEMIES/DefaultEnemyConfig.tres now exposes EnablePathDiversity (true), PathDiversityStrength (0.6), RouteProfileCount (4, capped at 8), PreferencePatchCells (8), and RoutePreferenceSeed. Ships receive a stable round-robin profile on spawn. The navigation service caches a small pool of AStarGrid2D instances and smooth seeded weight arrays, shared by all ships; it never allocates a navigation grid per ship. WorldGrid remains the sole authority for solids and NavigationRevision.

Weights are at least 1, keeping the octile heuristic admissible. Jumping is explicitly disabled because it bypasses weighted cells. Approach selection compares weighted costs; First tower targeting still compares the physical remaining route distance, not preference/congestion cost. Broad cost patches encourage alternate corridors without per-tile random jitter. Movement speed, smooth facing and swept obstacle checks remain unchanged. A single navigable corridor still produces shared routes; safe waypoint offsets are now implemented; local separation remains future work.

CongestionInfluence defaults to 0 (off). When enabled, living-ship density is sampled every CongestionSnapshotSeconds (0.5), with capped costs in occupied/neighbor cells. Only sparse affected weights are refreshed per profile; old snapshots are restored before replacement. Profiles never borrow another profile's temporary state. New/repath requests consider snapshots; existing routes are not recalculated merely because crowds move. Earlier goal candidates are rescored against the current snapshot before comparison, without additional A*. Map changes invalidate solids and clear stale congestion. All actual A* searches remain within the configured frame budget.

SeparationStrength and SeparationRadius are reserved Inspector settings with no movement effect in this pass. A later separation implementation must use spatial buckets and swept obstacle clearance. Change configuration in the saved Resource before F5; runtime weight edits affect new/pending routes without forcibly repathing existing ships. Use terrain/occupancy revision changes to test repathing.

Tests include deterministic alternate corridors, swept clearance, ordinary paths with diversity disabled/zero strength, congestion/no weight leakage, 500 queued ships, budget limits and tower revision repathing. The headless timing output is a diagnostic, not a rendered 60 FPS guarantee; a synchronous 500-node spawn batch can still exceed a 16.7ms frame and future waves should spread spawn creation across frames.

## Incremental navigation and selective repathing

Tower placement/removal now emits a cell change: NavigationGrid updates that one cell across the shared weighted profiles. Full grid updates remain for startup, terrain reloads and profile changes. Route dependencies index potentially affected ships; exact remaining-route validations are bounded separately. Unaffected ships keep moving. Affected ships request prioritized A* and keep moving along their safe old prefix; per-tick immediate clearance prevents crossing newly blocked terrain while checks/repaths wait.

EnemyMasterConfig adds soft path-work/validation millisecond budgets, a validation count limit, fair oldest-request service, optional staggered three-second audits, and EnablePathOffsets/PathOffsetMaxPixels (default true/8px). Audits do not calculate A* for valid routes. One stable lateral bias per ship is applied to interior waypoints with corner bisectors and clearance-tested tapering; no extra A* or per-frame jitter. Local separation remains inactive.

Placement connectivity is preserved using reusable flat flood-fill storage. Level Debug shows separate placement, grid, native A*, total path-work and validation timings. TESTS/RepathMovementChecks.cs covers 500 affected ships, unaffected movement, budgets, fairness, offsets, audits and safety. TESTS/PlacementPerformanceChecks.tscn measures identical 100/500-ship fixtures; TESTS/NavigationPerformance.md records before/after results. Time budgets are soft: a single native search/path assignment cannot be interrupted, and rendered 60 FPS still requires hardware testing.

## Moving replacement-path handoffs

Confirmed blockage now queues a replacement without stopping safe movement immediately. Ships retain old waypoints while waiting, with strict immediate swept/corner checks; they stop only when the next movement would be unsafe. Queue urgency follows the shrinking distance to the obstruction. Failed replacement searches retain any safe old prefix and use the existing retry interval.

Each multi-goal request uses one position snapshot, so its candidate costs are comparable while the ship moves. When the result arrives, EnemyMovement joins it near the ship's current progress, preferring a forward clearance-tested waypoint. A safe two-leg elbow can turn around a nearby wall without returning to the cell center. Both legs obey swept clearance and corner-safe cell transitions. If reconnection fails, a fresh request uses the same centralized budgets. Backtracking remains possible when the actual map requires it.

Safe offset refinement cannot turn the first new segment backward merely due to lateral bias. Existing speed/turning/offset settings, weighted profiles, congestion, auditing and count/time budgets are preserved. First targeting excludes routes known to need replacement until a valid handoff completes. No new pathfinding algorithm or per-frame A* is added.

## Manual waves (Phase 1)
LEVELS/Waves contains the shared immutable data, catalog and one Main/Waves scheduler; LEVELS/Level001/Level001Waves.cs contains this map's authored encounters. WaveSetId in LevelConfig chooses a finite set; the HUD Start/Next button controls progression. Wave spawns use random eligible sources, preserve F2 round-robin debugging, retain blocked pending ships and complete only after scheduled and wave-owned ships resolve. DefaultEnemyConfig exposes attempt cap/retry/seed. No rewards, new enemy types or modifiers yet. See LEVELS/Waves/README.md.
