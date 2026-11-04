# Shockwave Tower — Level 2 upgrade artwork

The Shockwave Tower has **two purchased levels total** (Level 1 and one Level 2 upgrade). Both use the same stationary root `Sprite`. The existing shared construction visual presenter selects **`FinishedTexture`** for completed Level 2 artwork and staged textures at 35% and 70% while upgrading; it does not use split `FinishedBaseTexture` or `FinishedGunTexture`.

| Stage | SVG | Appearance |
| --- | --- | --- |
| Level 1 finished | `../shockwave_tower.svg` | Original compact purple/lilac circular emitter |
| Level 2 upgrade, 35% | `shockwave_upgrade_l2_35.svg` | Existing Level 1 emitter with temporary alignment scaffolding |
| Level 2 upgrade, 70% | `shockwave_upgrade_l2_70.svg` | Reinforced circular resonator and brighter energy core, still under calibration |
| Level 2 finished | `shockwave_tower_l2.svg` | Reinforced outer pulse housing, larger containment ring, brighter high-energy core |

All textures are 48×48 with the original centred registration. There is **no turret rotation, muzzle or separate gun sprite**. The existing Level 2 purchase is **225 Credits / 6 seconds**; damage per pulse (44), max radius (262.9 px), wave expansion (260 px/s), interval (3.9s), wave damage falloff, energy type, heat, ammunition, and emitted effects are not changed. The system stays event-driven with no added processing loops.

Manual Godot checks: initial construction 0/35/70/100%, Level 2 upgrade 35/70/100%, selection tint and purple pulse presentation. Runtime verification pending.
