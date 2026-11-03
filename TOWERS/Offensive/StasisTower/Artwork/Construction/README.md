# Stasis Tower — construction artwork

This tower now opts into the **existing shared** `TowerConstructionArt` / `TowerConstructionVisuals` system. `StasisConstructionArt.tres` is referenced by `DefaultStasisTowerConfig.tres`; the scene presenter points at `TurretPivot/ConstructionSprite` and the existing split base/gun sprites.

| Initial Level 1 construction progress | Artwork |
| --- | --- |
| 0% | `stage0_foundation.svg` — unpowered foundation pad |
| 35% | `stage1_frame.svg` — mounting frame and unpowered field guides |
| 70% | `stage2_assembly.svg` — partially installed energy emitter |
| 100% | Original finished base + independently rotating/recoiling gun |

The paid first build retains **4 seconds** of actual work, gated by the existing construction drone. The preview shows the existing fully constructed Level 1 art rather than the scaffolding. For paid Levels 2 and 3, the same resource references two distinct 35%/70% work stages and finished split artwork per level; see [Upgrades](../Upgrades/README.md).

Art is centred native **64×64** SVGs. The single muzzle stays at **(0,-25)**, so projectile origins and recoil are unchanged. No per-frame art polling; existing work progress events advance the art. Owner checks in Godot for foundation/frame/assembly transitions and normal gameplay zoom.
