# Restoration Tower — RT1 native SVG artwork

Native 64×64 split `repair_tower_base.svg` (fixed white/silver octagonal platform) and `repair_tower_head.svg` (slender aqua-lens emitter, up/-Y) share centre (32,32). `repair_tower.svg` combines the same shapes for the catalog icon and finished overlay fallback. The physical emitter tip is canvas (32,3), matching scene `TurretPivot/Muzzle = (0,-29)`.

`Construction/foundation.svg` is the initial build image wired through RepairConstructionArt and the existing shared presenter. It hides finished layers during work and restores them on completion. No new work timer/drone or 35/70%/purchased upgrade artwork in RT1. Normal selection tint and range overlay are reused. Source SVG/resource review only; Godot import/zoom/readability acceptance remains for owner.
