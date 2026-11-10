## Reference-inspired precision laser artwork — 2026-11-09

The L1/L2/L3 base and rotating gun SVGs plus their combined illustrations and L2/L3 35/70% upgrade textures now share a consistent top-down, silver-and-amber precision laser design. The circular ring grows reinforced anchors and focus nodes with each level while the weapon keeps a long, **thin** forward barrel. All remain 64×64 and retain the existing fixed-base/rotating-gun presentation and local muzzle position `(0,-29)`. Gameplay stats, prices, upgrade work, scene paths and import metadata are unchanged; in-editor visual acceptance remains for the owner.

---

# Laser Tower — split finished artwork, purchased Levels 1–3

The Laser Tower has a **stationary base** and **separate rotating emitter sprite** at each level. The earlier monolithic 64×64 artwork remains as combined catalog art and the temporary full assembly basis, but is no longer rotated as a complete finished tower.

| Completed level | Fixed base | Rotating emitter | Temporary upgrade art |
| --- | --- | --- | --- |
| Level 1 | `../laser_tower_base.svg` | `../laser_tower_gun.svg` | initial construction `../Construction/stage0_foundation.svg`, `stage1_frame.svg`, `stage2_assembly.svg` |
| Level 2 | `laser_base_l2.svg` | `laser_gun_l2.svg` | `laser_upgrade_l2_35.svg` / `laser_upgrade_l2_70.svg` |
| Level 3 | `laser_base_l3.svg` | `laser_gun_l3.svg` | `laser_upgrade_l3_35.svg` / `laser_upgrade_l3_70.svg` |

All textures use the existing **64×64** canvas with a consistent emitter orientation; the `TurretPivot/Muzzle` remains at **(0,-29)** for actual beam visuals. The shared `TowerLevelArtwork.FinishedBaseTexture` and `FinishedGunTexture` choose completed art based on the purchased level. `UpgradeStages` preserve the 35% and 70% construction overlays, which hide both finished sprites until the current construction stage finishes.

The split retains the existing appearance motifs: Level 1 silver armour and gold emitter; Level 2 reinforced silver fins and brighter core; Level 3 advanced side bracing, heavy focusing array and golden high-output emitter.

| Purchased level | Credits | Work | Beam DPS | Range | Max health |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 250 | 4 s | 12 | 323.4 px | 1000 |
| 2 | 175 | 5 s | 14 | 334.4 px | 1050 |
| 3 | 250 | 6 s | 16 | 345.4 px | 1102.5 |

Combat logic, original `LaserTower.cs` targeting and beam rotation, shield preference, heat, ammunition and balance are unchanged. Only a cached fixed base and construction sprite have been added for consistent selection highlighting; no new runtime loops. Manual verification in Godot is still pending.
