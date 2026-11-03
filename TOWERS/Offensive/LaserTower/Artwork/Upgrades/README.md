# Laser Tower — Level 2 purchased artwork

Laser has **only two authored purchased levels**, so only the Level 2 art pass is included. Unlike split-base turrets, the current Laser scene intentionally uses a single rotating `TurretPivot/Sprite`; all artwork transitions happen through that existing sprite.

| Stage | Texture | Description |
| --- | --- | --- |
| Level 1 finished | `../laser_tower.svg` | Original silver/gunmetal housing with gold emitter |
| Level 2 upgrade at 35% | `laser_upgrade_l2_35.svg` | Existing completed weapon with yellow/gold scaffolding and alignment markings |
| Level 2 upgrade at 70% | `laser_upgrade_l2_70.svg` | Reinforced new emitter, silver side plating and final calibration brackets |
| Level 2 finished | `laser_tower_l2.svg` | Armoured silver chassis, cooling rails, enlarged yellow core and high-output focusing lens |

All SVG assets retain native **64×64** dimensions, original up-facing alignment and `TurretPivot/Muzzle=(0,-29)`. The full finished L2 image is selected via `TowerLevelArtwork.FinishedTexture`, not `FinishedBaseTexture`/`FinishedGunTexture`, because this tower currently has a single visual layer. The shared `TowerConstructionVisuals` uses its existing paid-work events to change the sprite and stops observing when idle.

The original **175-Credit, 5-second** Level 2 upgrade remains unchanged, along with beam DPS, shield preference, energy response, ammunition, heat, target/turn calculation and beam visual renderer. No Level 3 was added. To inspect the art sequence, upgrade a placed Laser and look for 35%/70%/100% transitions; also confirm the independent beam still begins at the existing `Muzzle` Marker2D. Owner Godot visual approval pending.
