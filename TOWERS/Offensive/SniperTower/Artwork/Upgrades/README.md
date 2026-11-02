# Sniper Tower — Level 2 and 3 upgrade artwork

**Source implemented 2026-11-02; Godot visual acceptance pending.** Reuses the same **event-driven** shared `TowerConstructionVisuals` and `TowerLevelArtwork` used by Battle and Machine Gun towers. No Sniper-specific art controller, timer or process loop.

| Purchased level | Fixed base | Rotating/recoiling precision gun | Work-in-progress composites |
| --- | --- | --- | --- |
| 1 | `../sniper_tower_base.svg` | `../sniper_tower_gun.svg` (barrel lengthened) | Existing finished artwork during initial build |
| 2 | `sniper_base_l2.svg` | `sniper_gun_l2.svg` | `sniper_upgrade_l2_35.svg` and `sniper_upgrade_l2_70.svg` |
| 3 | `sniper_base_l3.svg` | `sniper_gun_l3.svg` | `sniper_upgrade_l3_35.svg` and `sniper_upgrade_l3_70.svg` |

## Art direction and barrel alignment

- **Level 1:** the existing barrel extends from near the central mount to a revised muzzle at SVG **y = -7**, around 10 pixels farther forward than its prior cap, with the same barrel alignment and core style. The placement/catalog composite `../sniper_tower.svg` is updated too.
- **Level 2:** reinforced side stabilizers and cooler pads on the base, plus a heavier long-barrel mounting yoke and cyan precision channels.
- **Level 3:** stronger structural bracing and a layered heavy barrel shroud, with gold/ice-blue targeting accents and an upgraded precision core.
- **All muzzle-bearing SVGs** (L1 gun and composite, both L2/L3 guns, all 35%/70% composites) use `width="64" height="80" viewBox="0 -8 64 80"`, leaving the original artwork centre at the same local `(0,0)` but allowing the extended barrel above the original 64×64 area. The fixed bases remain 64×64. The **single shared** `MuzzleA` marker moves from `(0,-27)` to `(0,-39)` under `TurretPivot/RecoilRoot/Muzzles` for all purchased levels. Future physical projectile mode, flashes and aiming use the updated muzzle marker.
- A larger artwork canvas **does not** expand placement, collision, target-acquisition range or damage, though the barrel protrudes visually farther toward the neighbouring cell.

## Presentation behaviour

`../Construction/SniperConstructionArt.tres` owns two `TowerLevelArtwork` rows with a fixed base + rotating gun and two `TowerConstructionStage` milestones each. At 35% the original completed tier transitions to a blueprint/scaffold composite; at 70% the assembled new tier appears with calibration rails; at 100% the new split art appears with its normal turret rotation and recoil. Work timers remain **L2 5 seconds / L3 6 seconds** and are held until the construction drone's active beam starts work. No artificial render ticks.

`SniperTower.tscn` now includes `TurretPivot/ConstructionSprite` and an existing shared `ConstructionVisuals` script node with explicit fixed/rotating sprite references. The shared system handles save/resume/purchased-tier displays, disables while upgrading, and ensures the tier art is determined by the purchased upgrade level, **not** earned XP rank.

**Unchanged:** instant-target shot delivery, damage, target selection, ammo, heat, projectile options, tower XP and regular recoil controller.

**Owner check:** upgrade an active Sniper from Level 1 to Level 2 and then 3; inspect both intermediate stages, the extended barrel and muzzle flash in-game, barrel rotation/recoil and art after a save/load. Source review performed, not a Godot visual or runtime acceptance test.
