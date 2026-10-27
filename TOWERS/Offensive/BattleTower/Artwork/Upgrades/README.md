## CD4 — two upgrade assembly images while the drone beams (2026-10-27)

Each paid Battle upgrade now waits for a construction drone. The prior tier stays drawn while the drone travels. As soon as its beam is fully deployed, the ordinary **4s Level2 / 5s Level3** work bar starts; the composite upgrades at **35%** (`battle_upgrade_l2.svg` or `battle_upgrade_l3.svg`) and again at **70%** (`battle_upgrade_l2_70.svg` or `battle_upgrade_l3_70.svg`). The 70% artwork has partially assembled upgraded bracing/core details, visually distinct from the earlier scaffold. At 100%, the correct separate fixed base and rotating/recoiling gun textures are restored. The existing Level1 initial-build foundation/frame/gun steps are **not** used for an already-built tower. Configure or replace milestone textures in `../Construction/BattleConstructionArt.tres`; the shared `UpgradeStages` array accepts more thresholds. [Shared CD4 plan](../../../../Shared/Upgrades/UPGRADE_DISABLE_TIER_ART_PLAN.md).

---

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
