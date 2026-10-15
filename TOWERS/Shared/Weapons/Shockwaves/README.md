# Expanding Shockwaves — future Phase 4E (PLANNED, NOT IMPLEMENTED)

This folder is a **design contract only**. Do **not** add shockwave runtime classes, scenes, physics shapes or timers in Phase 4D. The first real AoE tower is the instant-hit Blaster Cannon, with reusable selection/falloff in `COMBAT/AreaDamage/`.

## Distinguishing damage delivery from visuals

- **Blaster Cannon / instant explosion:** damage is applied to all eligible targets in the impact circle **at one moment**. The visual explosion is allowed to animate outward, but this animation never applies more damage.
- **Shockwave Tower / expanding damage front:** at emission, create one wavefront centered at the source. Its radius increases with elapsed time, and enemies take damage only when the outward-moving **edge crosses them**. An enemy left inside an already-passed ring takes no new damage just because it is still inside the overall maximum radius. Visual expansion should follow real radius but not own the combat calculation.

Future wavefront-specific authored stats could include `MaxRadiusPixels`, `ExpansionSpeedPixelsPerSecond`, `WavefrontWidthPixels`, `DamagePerWave`, `EdgeDamageFraction`, optional `DamageType` and weapon shield/hull overrides or penetration. Any distance falloff number belongs to **that weapon's own config/final stats**, never as a global hardcoded damage minimum.

## Future Phase 4E implementation rules

- Reuse `COMBAT/AreaDamage/` for distance/falloff math and `COMBAT/Damage/DamageResolver` indirectly through per-target `Enemy.ApplyDamage(DamageInfo)`. The wavefront delivery decides **WHEN** a hit occurs; it should NOT call the instant full-circle attack on every frame.
- Sample `previousRadius -> currentRadius` each physics tick; detect swept annulus crossings to avoid tunneling across enemies at large `delta`, and keep a per-emission set of already-hit enemy instance IDs so one emission never damages the same target twice. Respect enemy alive/unload states and movement; consider swept relative enemy motion for fast targets rather than silently missing them.
- Keep shockwave parameters in typed weapon config and the **existing eight-stage immutable stat pipeline**, with eventual research (stage 04) separate from tower paid tiers (stage 02). Visuals are independent; keep timed wavefronts bounded and cleaned up on expiry/unload.
- Test deterministic reach times, increasing radius, no damage ahead/behind the wavefront, one hit per enemy, variable `delta`/30-60-120 Hz and long-tick crossings, multiple overlapping emissions, valid shield/hull response, no leaking timers/nodes, and the existing 500-enemy fixture.

**Status:** Planned only. Implement Phase 4D first and update this document later when a real Shockwave Tower is approved.
