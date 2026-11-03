# Laser Tower — staged construction with fixed base and rotating emitter

Laser uses the shared event-driven `TowerConstructionVisuals` for **one temporary composite** and **two finished sprites**:

- `BaseSprite` lives directly under the LaserTower scene root and **does not rotate**.
- `TurretPivot/Sprite` is the independently **rotating emitter**; the existing `TurretPivot/Muzzle` at `(0,-29)` rotates with it.
- `TurretPivot/ConstructionSprite` contains complete temporary staged art and is hidden when finished.

The presenter has `SpritePath=../TurretPivot/ConstructionSprite`, `FinishedBaseSpritePath=../BaseSprite`, `FinishedGunSpritePath=../TurretPivot/Sprite`, and both finished sprite paths in `FinishedVisualPaths`. It switches completed split artwork for a temporary single composite during initial build and upgrade stages, without changing the existing work/progress scheduling.

| Level 1 initial build | Temporary image |
| --- | --- |
| 0% | `stage0_foundation.svg` (empty chassis mount) |
| 35% | `stage1_frame.svg` (emitter support) |
| 70% | `stage2_assembly.svg` (partial emitter) |
| 100% | Separate original fixed base and rotating gold emitter |

The existing `DefaultLaserTowerConfig.tres` references `LaserConstructionArt.tres`. Level 2 and 3 completed split base/gun art is also authored in that same resource; see [upgrade artwork](../Upgrades/README.md).

This pass preserves Laser's 4-second initial build, construction drones, beam render controller, ammo, heat, target preference, shield multipliers, Level 2/3 economics and tower footprint. The original full `laser_tower.svg` remains the catalog icon and composite fallback. No additional per-frame work. Source-only sanity checked; owner should verify physical beam/rotation and stage appearance in Godot.
