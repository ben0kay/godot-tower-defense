# Sniper Tower construction-art resource

`SniperConstructionArt.tres` uses the **existing shared** `TowerConstructionArt` / `TowerConstructionVisuals` resources, without custom code or new update loops. `DefaultSniperTowerConfig.tres` references this resource.

- **Level 1 initial build:** unchanged gameplay/presentation. No `BuildStages` are authored, so the completed split gun/base remains visible throughout the existing four-second work timer (as it did before this visual pass). The new construction presenter only supplies upgrade-tier art.
- **Level 2 upgrade:** existing construction-drone workflow; at 35% and 70% of active work, show `../Upgrades/sniper_upgrade_l2_35.svg` and `sniper_upgrade_l2_70.svg`, respectively, then L2 split base/gun.
- **Level 3 upgrade:** 35% / 70% `../Upgrades/sniper_upgrade_l3_35.svg` / `sniper_upgrade_l3_70.svg`, then L3 split base/gun.
- From payment through the drone's travel and until the 35% milestone, show the current completed tier. Work time, drone routing, disable state, save data, prices and stats are untouched.
- The overlay `ConstructionSprite` hides `BaseSprite` and `TurretPivot/RecoilRoot` only during its intermediate stages. The finished base and rotating/recoiling gun are restored automatically upon work completion.

The barrel artwork in the level-1 composite and each intermediate stage uses a centred **64×80** SVG with `viewBox="0 -8 64 80"`. The 64×64 fixed base stays centred; the extended muzzle is at local **(0, -39)** on the rotating `RecoilRoot`. This is an artwork-only canvas extension, not a footprint or gameplay-range adjustment.

See [upgrade artwork](../Upgrades/README.md). Source/resource checks only; the owner will visually approve the appearance in Godot.
