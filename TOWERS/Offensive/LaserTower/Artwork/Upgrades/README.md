# Laser Tower — Levels 2 and 3 purchased artwork

Laser now has three authored purchased levels. The tower continues using the original **single rotating `TurretPivot/Sprite`**, without splitting the scene into base/gun sprites or modifying its existing beam origin.

| Stage | Texture | Description |
| --- | --- | --- |
| Level 1 finished | `../laser_tower.svg` | Original silver/gunmetal housing with golden emitter |
| Level 2 at 35% | `laser_upgrade_l2_35.svg` | L1 under new assembly scaffolding |
| Level 2 at 70% | `laser_upgrade_l2_70.svg` | Reinforced L2 emitter under calibration |
| Level 2 finished | `laser_tower_l2.svg` | Armoured silver chassis, cooling rails and enlarged yellow core |
| Level 3 at 35% | `laser_upgrade_l3_35.svg` | Completed L2 emitter with a larger high-output construction scaffold |
| Level 3 at 70% | `laser_upgrade_l3_70.svg` | L3's heavy silver armour and enlarged golden emitter beneath final calibration guides |
| Level 3 finished | `laser_tower_l3.svg` | Advanced silver/gunmetal armour, additional side braces, large focusing lens and high-output central laser |

The 64×64 SVGs preserve centre registration, orientation and the existing `TurretPivot/Muzzle=(0,-29)` beam origin. `TowerLevelArtwork.FinishedTexture` handles the completed single-sprite versions, and the shared presenter uses its existing event-driven 35/70% upgrade textures without extra per-frame processing. The initial construction stages and Level 2 visuals are unchanged.

**Authored upgrade rows** in `LaserTowerLevels.cs`:

| Purchased level | Credits | Work | Beam DPS | Range | Max health |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 250 | 4 s | 12 | 323.4 px | 1000 |
| 2 | 175 | 5 s | 14 | 334.4 px | 1050 |
| 3 | 250 | 6 s | 16 | 345.4 px | 1102.5 |

L3 retains the same shielding multipliers, Energy damage type, turn/aim/search parameters, 1000 ammo capacity and 2 ammo units per second, heat policy and shared beam/damage logic as L2. The Level 3 purchase adds a new row; Level 1/2 stats and prices remain unchanged. Owner Godot check: L2 → L3 upgrade work milestones and restored artwork after saving/reloading; live runtime and SVG import verification pending.
