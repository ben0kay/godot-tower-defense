# Blaster Cannon — Level 2 upgrade artwork

**Source implemented 2026-11-02; awaiting in-game visual approval.** Uses the original event-driven `TowerConstructionArt`, `TowerLevelArtwork` and `TowerConstructionVisuals` without adding another timer, node or physics loop.

| Purchased level | Fixed base | Rotating/recoiling cannon | Construction work images |
| --- | --- | --- | --- |
| Level 1 | `../blaster_cannon_base.svg` | `../blaster_cannon_gun.svg` | Existing C4 0%, 35%, 70% initial build images |
| Level 2 | `blaster_base_l2.svg` | `blaster_gun_l2.svg` | `blaster_upgrade_l2_35.svg` (35%), `blaster_upgrade_l2_70.svg` (70%) |

Level 2 has strengthened graphite steel and gold-toned side armor, additional bearing rings and a thicker **single** energy-cannon shroud, preserving the original **64×64 registration and (0,-30) muzzle marker**. The cannon's two separate finished layers remain on their existing scene nodes.

During the existing 6-second, 200-Credit paid L2 upgrade, the original completed Level 1 remains visible before 35% of work; at 35% a blueprint/scaffold composite appears over current hardware; at 70% the new upgraded hardware is present with calibration brackets; and at 100% the split L2 base/gun sprites appear so aim/recoil can resume. The builder drone, attack disable and square work-site hologram are owned by existing systems. No stats, projectiles or targeting changes. The initial Level 1 construction stages are untouched.

Source configuration: `../Construction/BlasterConstructionArt.tres`, linked from `DefaultBlasterCannonConfig.tres`. Scene wiring: `BlasterCannon.tscn` specifies `FinishedBaseSpritePath` and `FinishedGunSpritePath`. There is no L3 purchased level or artwork until a separate balance feature authorizes one.

**Owner checks in Godot:** L2 during 35/70/100 work, fixed base and rotating cannon, muzzle flash/recoil, after save/load, and at gameplay zoom. This implementation was not runtime-tested.
