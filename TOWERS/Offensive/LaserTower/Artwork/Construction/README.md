# Laser Tower construction artwork

The existing **single rotating Sprite2D** at `TurretPivot/Sprite` is the only artwork layer the tower uses. The shared event-driven `TowerConstructionVisuals` now targets that node directly; it swaps its texture during initial construction and paid upgrades. There is **no separate construction-overlay Sprite2D, split base/gun, animation loop or new attack controller**. The original 64×64 icon is also the completed Level 1 art.

| Initial build progress | Texture | Appearance |
| --- | --- | --- |
| 0% | `stage0_foundation.svg` | Original silver chassis and empty mounting socket |
| 35% | `stage1_frame.svg` | Emitter support structure and unpowered cradle |
| 70% | `stage2_assembly.svg` | Partial silver energy emitter and dim gold core |
| 100% | `../laser_tower.svg` | Finished existing Level 1 laser assembly |

The existing **4-second** construction work is unchanged and remains gated by the Citadel drone. No shot/beam delivery, heat, ammo, purchase or targeting behaviour is changed. The original `TurretPivot/Muzzle` marker stays at **(0,-29)**; beam visuals retain their existing independent `BeamRenderer`.

The same `LaserConstructionArt.tres` config also owns Level 2 artwork and 35/70% staged upgrade textures. See [upgrade documentation](../Upgrades/README.md). Godot visual acceptance pending.
