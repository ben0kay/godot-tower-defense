# Rocket Tower construction artwork

The tower now opts into the existing event-driven `TowerConstructionVisuals` and `TowerConstructionArt` system used by Battle and Machine Gun. Each construction stage has dedicated 64×64 upward-facing art aligned with the finished composite.

| Progress | Artwork | At current 5-second build duration |
| --- | --- | --- |
| 0% | `stage0_foundation.svg` — foundation pad | 0s |
| 35% | `stage1_frame.svg` — reinforced mounts | 1.75s |
| 70% | `stage2_assembly.svg` — incomplete weapon | 3.5s |
| 100% | Original completed fixed base and recoiling gun | 5s |

**Stage sequence:** empty pad → paired support rails → partly finished twin rocket tubes → complete launcher. A paid construction site held by the Citadel builder remains at stage 0 until the drone releases the hold; the shared CV1 cyan footprint hologram appears immediately on placement independently. Stage textures temporarily replace the two finished layers and restore them on completion. Muzzle positions, targeting, recoil, timers, ammunition, prices and upgrades remain unchanged.

`RocketConstructionArt.tres` is linked from `DefaultRocketTowerConfig.tres`; adjust stage textures/thresholds in the Inspector. Levels 2 and 3 upgrade-art slots are present but intentionally empty; direct operational instances and placement ghost show normal original finished artwork. No new animation process, shader, particles or per-tower timer. Desktop visual and build acceptance pending.
