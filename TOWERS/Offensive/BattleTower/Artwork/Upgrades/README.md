# Battle Tower — purchased-level upgrade art (DU2)

**Source-implemented 2026-10-27; Godot appearance review pending.** All assets are 64×64 upward-facing SVGs matching the original centered `battle_base.svg` and `battle_gun.svg`, with the same single muzzle at local (0, -28.5) under `TurretPivot/RecoilRoot/Muzzles/MuzzleA`.

| Purchased level | Fixed base | Rotating/recoiling gun | Work-phase composite |
| --- | --- | --- | --- |
| Level1 | `../battle_base.svg` (unchanged) | `../battle_gun.svg` (unchanged) | Initial build C4 stages (unchanged) |
| Level2 | `battle_base_l2.svg` | `battle_gun_l2.svg` | `battle_upgrade_l2.svg`, from 35% |
| Level3 | `battle_base_l3.svg` | `battle_gun_l3.svg` | `battle_upgrade_l3.svg`, from 35% |

Level2 adds angular reinforced side rails, cyan cooling accents, and an upgraded single-barrel focusing arm. Level3 builds on the same design with larger armoured bracing, gold/ice-blue reactor trim and a heavier *single-barrel* shroud. Neither tier moves the fixed centre, real muzzle marker, hit/footprint geometry, projectile trajectory, recoil controller or tower scale. This is an initial graphic style pilot, not final art approval.

Both custom pairs are assigned as `FinishedBaseTexture` and `FinishedGunTexture` in `../Construction/BattleConstructionArt.tres`; the temporary **upgrade** composite uses the existing `UpgradeTexture` / `UpgradeProgress` threshold. While waiting for 35%, the previous completed tier stays visible but the tower is already disabled. After 35%, the composite scaffold replaces the split visuals. At 100%, the new split base+gun instantly returns, allowing normal aiming/recoil. No rendering tick is added: the existing `TowerConstructionVisuals` work-event subscriptions drive swaps.

Edit Level2/Level3 textures through `DefaultBattleTowerConfig.tres → Construction Artwork → Level Artwork`. Keep different purchased levels independent from combat *Levels.cs and rank art (ranks are a different system). If a tier texture is missing, the presenter preserves the nearest earlier finished split texture. Other towers currently need no changes.

Related: [shared upgrade pass](../../../../Shared/Upgrades/UPGRADE_DISABLE_TIER_ART_PLAN.md) · [C4 initial construction](../Construction/README.md). Smoke-check scale, pivot rotation, muzzle, recoil and save/resume locally in Godot.
