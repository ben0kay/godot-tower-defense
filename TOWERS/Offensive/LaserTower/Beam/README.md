# Laser beam visual defaults

`DefaultLaserBeamStyle.tres` is the **Laser Tower preset** for the reusable `TOWERS/Shared/Weapons/Beams/BeamRenderer.tscn`.

Editable settings: colours, core/halo widths, existing 6% width pulse at 2Hz, travelling wave amplitude **1.5px**, wavelength **45px**, travel speed **55px/s**, **15%** ripple end fade, outer ripple multiplier **1.3x**, mild irregularity, and **2.5px** smooth wandering visual contact point. A subtle **4px** contact glow and low-count GPU impact sparks are enabled; muzzle particle emission is disabled. Set amplitudes/radii to zero and both particles false for a plain straight beam.

The renderer owns both optional `GpuParticles2D` emitters and references reusable texture/process-material assets in [FEEDBACK/Shared](../../../../FEEDBACK/Shared/README.md). Another beam weapon can use its own preset without editing the shared renderer. The saved resource controls defaults for newly instantiated towers; in-game Remote Inspector changes affect only that placed instance (its style is duplicated at Ready).

**Visual-only:** no beam accuracy/damage, enemy targeting, heat, ammunition or stat-pipeline changes. See [shared beam authoring](../../../Shared/Weapons/Beams/README.md) for lifecycle/performance notes. Verify appearance in the Godot editor; new GPU visuals were not playtested in this session.
