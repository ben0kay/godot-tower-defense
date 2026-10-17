# Laser beam visual defaults

`DefaultLaserBeamStyle.tres` is the **Laser Tower preset** for the reusable `TOWERS/Shared/Weapons/Beams/BeamRenderer.tscn`.

Editable settings: warm ivory-yellow core and golden-yellow halo, core/halo widths, existing 6% width pulse at 2Hz, travelling motion amplitude **1.1px**, wavelength **62px**, travel speed **39px/s**, **22%** ripple end fade, outer ripple multiplier **1.08x**, **0.82 irregularity** (smooth seeded noise dominates the sine), and **1.4px** gently wandering visual contact point. A subtle **4px** contact glow and low-count GPU impact sparks are enabled; muzzle particle emission is disabled. Set amplitudes/radii to zero and both particles false for a plain straight beam.

The renderer owns both optional `GpuParticles2D` emitters and references reusable texture/process-material assets in [FEEDBACK/Shared](../../../../FEEDBACK/Shared/README.md). Another beam weapon can use its own preset without editing the shared renderer. The saved resource controls defaults for newly instantiated towers; in-game Remote Inspector changes affect only that placed instance (its style is duplicated at Ready).

**Visual-only:** no beam accuracy/damage, enemy targeting, heat, ammunition or stat-pipeline changes. See [shared beam authoring](../../../Shared/Weapons/Beams/README.md) for lifecycle/performance notes. Verify appearance in the Godot editor; new GPU visuals were not playtested in this session.

## Silver-and-yellow visual pass (2026-10-17)

`Artwork/laser_tower.svg` is a compact 64×64 brushed-silver/gunmetal turret with yellow rails, reactor and emitter; it keeps the original barrel-up direction and existing `TurretPivot/Muzzle` marker at (0,-29). The single saved BeamStyle preset recolors the line core, halo, contact glow and existing impact particles (particle Modulate comes from CoreColor). No weapon nodes or combat settings change.

The shared renderer now blends a much weaker sine with continuous seeded 2D value noise; `RippleIrregularity=0` still requests the original pure sine, and higher values reduce regular repetition. The cosmetic target contact also uses smooth seeded noise instead of circular oscillation, stays inside the configured wander radius and never shifts the actual damage endpoint. No random per-frame samples, new beam nodes or allocations were added. Palette/shape/beam quality still needs a live Godot visual review.
