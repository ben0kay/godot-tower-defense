# Shared visual-effect assets

Reusable **visual resources**, not a global effect manager or another combat system.

- `Particles/EnergySpark.svg`: tiny soft monochrome spark texture; emitter `Modulate` supplies the weapon-specific colour.
- `Particles/EnergyContactParticles.tres`: small zero-gravity, slow-damping `ParticleProcessMaterial` suitable for brief beam-contact sparks and optional muzzle particles.

The **consumer owns its particle nodes and lifecycle**. For example, `TOWERS/Shared/Weapons/Beams/BeamRenderer.tscn` owns its `GpuParticles2D` children and uses these shared texture/material assets. Turning a beam off stops its emitters; other weapons can reuse the materials without referencing the Laser Tower. There is no duplicated per-tower effects controller.

To author distinct spark motion, duplicate the shared process material, edit the new resource, and reference it from another effect's scene. Avoid mutating a shared material at runtime; use `Duplicate()` first if a runtime material change is required.

Note: in Godot C# the class is `GpuParticles2D`; the scene node declaration is `GPUParticles2D`.

Shield-only visuals now live in [`FEEDBACK/Shields`](../Shields/README.md), not here. Keep `Shared/` for textures and process materials that are useful across unrelated effects; the shield-specific scene/asset stays with its owning feature.
