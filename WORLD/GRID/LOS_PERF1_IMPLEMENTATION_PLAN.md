# LOS-PERF1 — Safe Map-Aware Weapon LOS Fast Paths

**Repo:** `ben0kay/godot-tower-defense` · **Branch:** `dev` only  
**Status:** Planned — Codex-ready **small performance pass**, not implemented  
**Main owner:** `WORLD/GRID/`. No new LOS service, terrain index, world singleton or extra weapon semantics.

## 1. Why this is a narrow optimization

The game **already implements** the owner's requested automatic all-clear-map bypass. In `WORLD/GRID/WorldGridSight.cs`:

```csharp
if (TowerSightBlockerCount == 0) return true;
```

`WorldGrid.RebuildTerrain()` already computes `TowerSightBlockerCount` and the exact `_towerSightBlocked[]` mask from the authoritative Boolean tile metadata `blocks_tower_sight` on gameplay terrain. No need to scan for "mountain art" or add a second no-mountain toggle. The check also updates when the map is explicitly rebuilt. `Decorations`, `blocks_enemies`, ordinary walls/buildings and placement occupancy are not automatically LOS blockers.

**Confirmed remaining issue:** `BattleTower._PhysicsProcess`, `ProjectileTurretBase._PhysicsProcess` and other aimed towers call `TowerTargeting.Valid` while locked on a target, *before* the fire-cooldown gate. On terrain with blockers this can repeat exact line traversal each physics tick. `TowerTargeting.Find` also checks only qualifying candidate contenders, and hostile mounts use the same shared weapon sight API. An empty map already makes the call almost free; removing the call alone is unlikely to yield much real-world FPS.

**Goal:** avoid tile-by-tile line traversal when a target's sight corridor cannot contain any sight-blocking terrain, while retaining immediate/accurate occlusion behaviour on actual mountain sight lines.

## 2. Proposed implementation — cached conservative corridor broadphase

### 2A. Retain the existing zero-blocker fast path

- Keep current finite-coordinate rejection, then `TowerSightBlockerCount == 0` immediately returns true. Do not read or scan tiles for a blocker-free level.
- **Do not add** a manual gameplay toggle that disables LOS even when mountains exist; that would allow firing through obstacles.
- Do not use the navigation `_terrainBlocked` or placement `_occupied` masks as weapon sight; use **only** the existing `_towerSightBlocked` facts.

### 2B. One cached 2D summed-area blocker table only if there are blockers

Owner files: `WORLD/GRID/WorldGrid.cs`, `WORLD/GRID/WorldGridSight.cs` (partial class).

- After all terrain layers have been read in `RebuildTerrain()` and the final `_towerSightBlocked[]` mask/count is authoritative, construct a compact **(width + 1) × (height + 1) integral/prefix-sum table** of sight-blocker counts. Do this **only when `TowerSightBlockerCount > 0`**. For zero-blocker maps, keep the table null/empty and use the existing immediate return.
- Rebuild the table **on every explicit terrain rebuild**; clear it as soon as blockers are removed. The updated table must be live before `TerrainRebuilt` events/callbacks. This is `O(map cells)` per rebuild, *not* per tower, enemy, frame or shot. Avoid unnecessary allocations/rebuilds when the authoritative mask is unchanged if straightforward; do not sacrifice correctness for caching.
- In `HasWeaponLineOfSight`, preserve existing finite input checks and map-bound handling for blocker-containing maps. Transform endpoints and compute their grid cells once using the existing code.
- Before the current DDA/supercover loop, query a **conservative axis-aligned cell rectangle enclosing the whole segment** with a **one-cell padding on all sides** (clamp to map bounds). This padding protects exact tile-edge and corner-grazing checks. If the prefix table reports **zero sight blockers in this superset**, then the line is guaranteed clear and the function can return true without executing the existing DDA. If the rectangle contains one or more sight blockers, run the **existing exact traversal unchanged**.
- Prefix queries must be constant-time (four prefix reads, plus bounds math), allocation-free, finite and overflow-safe. Use the same grid indexing, coordinates, transforms and width/height as `WorldGrid`. Check integer allocation size for pathological map dimensions, and preserve a correctness-safe fallback to the original DDA if the table is unavailable.
- **Never** use a per-target “same cell means still visible” cache: endpoints can move within a cell so a corner sight line can become blocked without a cell crossing. Do not skip live fire checks based on stale LOS.
- The new broadphase is purely an optimization: **exact visible/blocked answers cannot change** versus the previous `HasWeaponLineOfSight` for any finite valid segment. Maintain current no-blocker-map semantics; do not redefine out-of-bounds or exact-corner behaviour.

### 2C. Keep weapon call sites and semantics stable in this pass

Primary consumers to inspect:

- `TOWERS/Shared/Targeting/TowerTargeting.cs` (`Find` and `Valid`).
- `TOWERS/Offensive/BattleTower/BattleTower.cs`.
- `TOWERS/Shared/Weapons/MultiBarrel/ProjectileTurretBase.cs`, Laser and other continuous targeting towers.
- `ENEMIES/Weapons/Shared/EnemyTowerTargeting.cs`, `EnemyWeaponMount.cs`; hostile gun muzzle origin and reveal remain authoritative.
- Any selected-tower range visibility overlays or other `HasWeaponLineOfSight` consumers.

Do **not** move LOS validation behind cooldown, add an LOS timer, or change firing/aim/beam gating in LOS-PERF1. This would relax the owner's rule that units cannot **see** targets through mountains while waiting to shoot, and might allow beams to tick through newly occluded targets. Instead speed up their existing shared grid query. Do not duplicate a `TowerSightBlockerCount` branch across every tower class.

Optional tiny cleanup **only if benchmark shows value**: cache the shared grid pointer in a typed caller when already available (not a second blocker-count snapshot) and avoid repeated registry dereferences. No global target-result cache, per-tower subscriptions or new per-frame work.

## 3. Correctness, lifecycle and scope

- Mountains and **any future tile flagged `blocks_tower_sight`** must still obstruct both friendly and enemy weapons; all other tile flags remain independent.
- Both start/end tiles, horizontal/vertical lines, reverse direction, exact-corner intersections, shallow diagonals, map bounds, transformed grid and neighboring blockers behave exactly as before.
- No terrain blocker maps must execute **zero DDA traversals**, with no extra startup blocker scan beyond `RebuildTerrain()`.
- A mountain painted/erased and followed by `RebuildTerrain()` must immediately update count, prefix data and combat visibility (existing `RebuildTerrain` contract). Removing last blocker frees the prefix table and returns to no-blocker fast path.
- Runtime tower placement, walls, constructed buildings and ammo/power infrastructure must **not** trigger broadphase invalidation unless their terrain metadata changes; ordinary occupancy never blocks sight.
- Maintain enemy stealth, lighting reveal, target priority, turret rotation, hit delivery, projectile physics, shields, and save/continue behaviour. No enemy/tower balance changes.
- If `RebuildTerrain()` can be called during editor preview or setup, keep the table valid before subsequent callbacks; no partial stale state or use-before-init.

## 4. Minimal observability and focused verification

If useful, add cheap **debug-only** read-only counts for `NoBlockerReturns`, `EmptyCorridorReturns`, and `ExactTraversals` on the `WorldGrid` (do not make them active expensive profiling, never count once per tile, and do not add a second performance overlay). Print or view them through existing diagnostics when warranted. This is optional; the main job is the safe fast path.

Use `TESTS/Terrain/MountainSightChecks.cs` as the existing narrow regression reference. Extend **only if necessary** with a compact fixture against the original DDA oracle for blocker-present + blocker-absent + terrain-rebuilt maps. Make sure a candidate just outside a mountain has unchanged LOS; ensure hostile Gunner/Capital muzzle sight and continuous Laser damage retain exact behaviour. No big test framework.

Owner desktop checks:
1. A level with **zero** marked sight blockers: tower/hostile weapon targeting behaves normally, DDA is never entered.
2. One mountain very far away: most local non-mountain shooting sight queries skip DDA via the new empty-corridor check.
3. Mountain between units: both directions block targeting and actual shot/beam; moving around it restores sight, including grazing corners.
4. Rebuild map to add/remove the last blocker; no restart needed and no stale bypass.
5. Compare existing F2/Shift+F2/F3 profiler captures (current debug controls) before/after under the same enemies/towers/map. If LOS does not materially contribute, do not claim an FPS gain.

**Validation policy:** source/diff sanity review, at most one practical C# build if available, no broad stress suites or extended tests. The owner will validate gameplay and desktop FPS.

## 5. Delivery

Read `CodePreferences.md`, `project.md`, `WORLD/GRID/WorldGrid.cs`, `WorldGridSight.cs`, `TESTS/Terrain/MountainSightChecks.cs`, and shared friendly/hostile targeting callers. Implement **LOS-PERF1 only** on `dev`; keep the source owner in `WORLD/GRID/`, with no independent line-of-sight cache in towers or enemies.

Update the existing `WORLD/GRID/README.md` briefly, append a short `NOTES/WORK.md` entry, commit and push one focused change, report commit SHA plus what was and was not validated. Leave target-lock scheduling changes as a separate later, explicitly approved performance experiment.

**This plan is documentation only; no runtime optimization has been implemented by writing this file.**
