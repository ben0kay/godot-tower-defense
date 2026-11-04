# Resource Tower — Level 2 and 3 upgrade artwork

Resource Tower uses **one nonrotating finished Sprite2D**. Its two purchased upgrades use `TowerLevelArtwork.FinishedTexture` and 35%/70% staged composites. All assets retain the original 112×112 SVG dimensions and 64×64 viewBox registration.

| Stage | Artwork | Design |
| --- | --- | --- |
| Level 1 finished | `../resource_tower.svg` | Existing green compact production building |
| Level 2, 35% | `resource_upgrade_l2_35.svg` | L1 with temporary upgrade scaffold |
| Level 2, 70% | `resource_upgrade_l2_70.svg` | Reinforced generator and enlarged vents with scaffold |
| Level 2 finished | `resource_tower_l2.svg` | Heavier green housing and additional production panels |
| Level 3, 35% | `resource_upgrade_l3_35.svg` | L2 with higher-tier construction scaffold |
| Level 3, 70% | `resource_upgrade_l3_70.svg` | Full enhanced machinery with temporary guides |
| Level 3 finished | `resource_tower_l3.svg` | Largest green industrial housing, reinforced wall vents and bright energy details |

Existing values in `DefaultResourceTowerConfig.tres` are unchanged:

| Level | Cost | Credits / 5s | Storage | Max HP | Work |
| --- | ---: | ---: | ---: | ---: | ---: |
| 1 | 350 | 10 | 100 | 1500 | 2s |
| 2 | 250 | 15 | 150 | 1575 | 2s |
| 3 | 350 | 20 | 200 | 1653.75 | 2s |

Work stages are cosmetic. No new production logic, no tower physics updates, and no changes to the collection interface, stored-credit preservation or save-state logic. Owner visual approval in Godot pending.
