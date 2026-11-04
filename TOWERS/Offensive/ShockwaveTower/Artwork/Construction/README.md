# Shockwave Tower — initial construction artwork

The tower uses one stationary, **nonrotating** root-level `Sprite` node. The existing shared event-driven `TowerConstructionVisuals` presenter points directly to `../Sprite` and switches its texture during paid construction. No separate base/gun, turret pivot, muzzle marker, bespoke scripts or per-frame updates are introduced.

| Build progress | SVG | Appearance |
| --- | --- | --- |
| 0% | `stage0_foundation.svg` | Unpowered purple platform, empty central wave socket |
| 35% | `stage1_frame.svg` | Ring mounting braces, supports, and unfinished central resonator |
| 70% | `stage2_assembly.svg` | Partly energized emitter ring and dim purple core |
| 100% | `../shockwave_tower.svg` | Existing original fully assembled Level 1 tower |

The 48×48 SVGs share the original purple/lilac colour and original registration. The construction drone and existing **5-second** Level 1 work time are unchanged. The same `ShockwaveConstructionArt.tres` also holds the purchased Level 2 visuals.

See [upgrade artwork](../Upgrades/README.md). Source checks only; in-game appearance pending.
