# Machine Gun Tower construction artwork

The Machine Gun Tower now opts into the **existing shared C4 event-driven construction presenter** using three dedicated 64×64 SVGs. Its original finished composite `../machine_gun_tower.svg` remains unchanged and remains the placement/ghost/catalog icon.

| Build progress | Artwork | At 4s authored work time |
| --- | --- | --- |
| 0% | `stage0_foundation.svg` — octagonal pad, empty central mount | 0s |
| 35% | `stage1_frame.svg` — circular structural frame, two empty gun brackets | 1.4s |
| 70% | `stage2_assembly.svg` — both short unfinished barrels, dim core | 2.8s |
| 100% | Finished base + independent rotating/recoiling gun | 4s |

A paid placement first waits for its Citadel construction drone (if builders are enabled); the above durations begin **only after the beam releases the held work clock**. During construction, the full composite stage temporarily replaces the split `BaseSprite` and `TurretPivot/RecoilRoot` graphics via `ConstructionVisuals`; when work completes, the overlay hides and the unchanged finished sprites resume. Muzzle markers, projectile origins, recoil, aiming, costs and stats are untouched. No per-frame animation or new work timer is added.

Edit `MachineGunConstructionArt.tres` (linked from `DefaultMachineGunTowerConfig.tres` → Construction Artwork) to change stage textures/thresholds. Empty Level2/Level3 upgrade slots are deliberately provided for optional future artwork and do not change current upgrade visuals. Keep any replacement art on the **same 64×64 viewBox**, with the base and upward-facing guns aligned to the existing finished composite. Direct-instantiated operational towers use the finished artwork immediately.

Source wiring verified; normal gameplay zoom/viewport visual acceptance is left to the desktop Godot editor.
