# Shared visual shockwave — Phase 4E implemented

`ShockwaveEffect.tscn` / `ShockwaveEffect.cs` draw and clean up one cosmetic ring. `ShockwaveStyle` is an Inspector-editable Resource: ring colour/width/opacity, optional wider translucent outer glow, expansion duration, standalone visual radius, start-radius fraction, fade start/power, optional fill and custom Texture2D artwork. The shared fallback is neutral white; no Blaster colour or weapon balance lives here. Glow is a layered translucent ring, not a post-processing bloom shader.

Each weapon supplies its own style:

- Blaster: `TOWERS/Offensive/BlasterCannon/Effects/BlasterShockwaveStyle.tres` (orange, .22s, starts at55% radius, short fade).
- Shockwave Tower: `TOWERS/Offensive/ShockwaveTower/Effects/ShockwaveTowerStyle.tres` (purple/lilac, visible wider travelling energy ring).

`Spawn(effectsParent, worldPosition, style, radius?, externallyDriven=false)` optionally returns an effect under World/Effects. Invalid/missing feedback parents suppress only presentation. If supplied, radius overrides the style's standalone radius; instant blasts use their snapshotted impact radius. The effect privately duplicates the style and subscribes to its Changed event; live ring/width/colour/artwork edits redraw only that effect. Texture artwork is stretched to the ring's visual diameter and tinted by its ring colour/opacity. Exit removes subscriptions.

**Autonomous mode:** `_Process` expands/fades with style duration/start fraction, then queues cleanup. Blaster damage is already finished before this starts. Visual duration/radius/colour never cause another hit or delay damage.

**Externally driven mode:** autonomous processing is disabled. Gameplay calls `SetWavefront(radius, progress)` with the exact simulated radius/progress, then `Complete()` at expiry. Style duration, standalone radius and start fraction cannot override this geometry. Fading/appearance remain cosmetic. Deleting, restyling or omitting the visual cannot change gameplay.

This component has no Enemy, registry, DamageInfo, wallet, falloff or combat queries. `TOWERS/Shared/Weapons/Shockwaves/ShockwaveEmitter.cs` owns travelling damage; `COMBAT/AreaDamage` owns instant-disc damage/falloff. Enemy DeathExplosion remains a separate kill visual. The retired FEEDBACK/AreaBlast/BlastFlash implementation was removed.
