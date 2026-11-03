# Sniper Tower construction-art resource

`SniperConstructionArt.tres` uses the **existing shared** `TowerConstructionArt` / `TowerConstructionVisuals` resources, without custom code or new update loops. `DefaultSniperTowerConfig.tres` references this resource.

- **Level 1 initial build:** now has dedicated `stage0_foundation.svg` (0%: foundation and empty mount), `stage1_frame.svg` (35%: reinforced mechanical cradle), and `stage2_assembly.svg` (70%: partially assembled short precision barrel and dim energy core). At 100%, the presenter restores the original completed fixed base and full-length rotating/recoiling sniper gun. The existing four-second work timer and Citadel drone hold remain unchanged.
- **Level 2 upgrade:** existing construction-drone workflow; at 35% and 70% of active work, show `../Upgrades/sniper_upgrade_l2_35.svg` and `sniper_upgrade_l2_70.svg`, respectively, then L2 split base/gun.
- **Level 3 upgrade:** 35% / 70% `../Upgrades/sniper_upgrade_l3_35.svg` / `sniper_upgrade_l3_70.svg`, then L3 split base/gun.
- From payment through the drone's travel and until the 35% milestone, show the current completed tier. Work time, drone routing, disable state, save data, prices and stats are untouched.
- The overlay `ConstructionSprite` hides `BaseSprite` and `TurretPivot/RecoilRoot` only during its intermediate stages. The finished base and rotating/recoiling gun are restored automatically upon work completion.

The barrel artwork in the level-1 composite and each intermediate stage uses a **64×80** SVG with `viewBox="0 -8 64 80"`. The 64×64 fixed base stays centred; the extended muzzle is at local **(0, -39)** on the rotating `RecoilRoot`. This is an artwork-only canvas extension, not a footprint or gameplay-range adjustment.

The initial build stages share the finished composite's native 64×80 coordinate viewBox and visual centre. At 70% the shorter barrel is deliberately unfinished; the 100% finished long barrel remains unchanged.

See [upgrade artwork](../Upgrades/README.md). Source/resource checks only; the owner will visually approve the appearance in Godot.
