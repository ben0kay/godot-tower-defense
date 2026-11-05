## Repair Tower L2 / L3 split SVG artwork (2026-11-05)

The initial Level 1 `repair_tower_base.svg`, `repair_tower_head.svg`, combined icon `repair_tower.svg` and `Construction/foundation.svg` remain unchanged. New 64×64 SVGs in [Upgrades/](Upgrades/README.md) add subtly brighter white-silver panel work, slightly stronger aqua rings, and modest progressively more advanced forward-facing emitter tips for paid Level 2 and 3. Each finished level has independent **fixed base** and **rotating gun head** art at the original 32,32 pivot; the muzzle remains at local (0,-29).

The existing `TowerConstructionVisuals` replaces base/head textures on paid completion and uses stage-specific composites at 35% and 70% work progress. No new standalone visual scripts, per-frame texture loading or art-rebuild loops.

---

# Restoration Tower — RT1 native SVG artwork

Native 64×64 split `repair_tower_base.svg` (fixed white/silver octagonal platform) and `repair_tower_head.svg` (slender aqua-lens emitter, up/-Y) share centre (32,32). `repair_tower.svg` combines the same shapes for the catalog icon and finished overlay fallback. The physical emitter tip is canvas (32,3), matching scene `TurretPivot/Muzzle = (0,-29)`.

`Construction/foundation.svg` is the initial build image wired through RepairConstructionArt and the existing shared presenter. It hides finished layers during work and restores them on completion. No new work timer/drone or 35/70%/purchased upgrade artwork in RT1. Normal selection tint and range overlay are reused. Source SVG/resource review only; Godot import/zoom/readability acceptance remains for owner.
