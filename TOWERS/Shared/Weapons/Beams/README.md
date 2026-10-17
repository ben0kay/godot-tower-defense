# Reusable beam presentation — ripple / contact effects

`BeamRenderer.tscn` remains one **weapon-agnostic scene**: the caller provides `SetBeam(fromWorld, toWorld, active)` and continues owning targets, damage, shield response, accuracy, ammo, heat and cooldowns. The renderer works in world coordinates, converts to its own local geometry, and does not access enemies or weapon stats. Future beam weapons can instance this scene with a different `BeamStyle`.

## Owned scene children

- `Outer` and `Core`: persistent antialiased `Line2D` halo/core, optional width pulse and transverse travelling ripple.
- `BeamRenderer._Draw()`: inexpensive layered contact glow directly at the visual impact point, requiring **no extra glow node**.
- `ImpactParticles` and `MuzzleParticles`: **optional `GpuParticles2D`**, owned/positioned/stopped by this renderer. They use shared appearance assets in [FEEDBACK/Shared](../../../../FEEDBACK/Shared/README.md); **impact enabled, muzzle disabled** by the Laser Tower's preset.
- There is **no centralized per-frame effects manager** and no particle nodes spawned per hit/physics tick.

## Style in Godot Inspector

`BeamStyle.cs` controls the existing `CoreColor`, `OuterColor`, widths and pulse, plus independent groups:

| Group | Parameters | Disable path |
| --- | --- | --- |
| Ripple | Amplitude, wavelength, travel speed, end-fade fraction, halo ripple scale, irregularity, point spacing | Amplitude = 0 |
| Impact Contact | Wander radius, wander frequency, glow radius, glow opacity, glow flicker | Radius = 0 for each |
| Optional Particles | ImpactParticlesEnabled, MuzzleParticlesEnabled | Both false |

**Cosmetic invariants:** `Core` first point stays exactly at the supplied muzzle. The last point wanders smoothly within the configured radius of the actual target point, with no combat effect. Ripple displacement fades near both ends; it does not shake the source muzzle. Each renderer seeds a different starting animation phase from its instance ID, preventing synchronized beams. Core and outer halo can ripple at different strengths. The glow and impact emitter follow the wandering visual contact point.

Long beams use more Line2D points than short ones, capped at **28 points**. Point-count changes only when required by length/active state; line geometry is updated in place without new arrays/nodes on each beam tick. `_Process` runs **only while an active beam needs cosmetic animation**, and stops on deactivation; there are no world queries. All motion amplitudes and frequencies are safely clamped for missing/invalid style values.

The inactive renderer hides, reverts to two zeroed points, and disables both emitters. Style resources are cloned per renderer; modifying one instance's live `Style` emits `Changed` and only updates visuals. No stat-cache rebuild, extra targeting or duplicate GPU material copies.

## Presets, code and validation

- [Laser preset](../../../Offensive/LaserTower/Beam/DefaultLaserBeamStyle.tres): bright warm-yellow core, golden-yellow halo, restrained 1.1px ripple (62px wavelength, 39px/s) blended heavily with smooth seeded noise, 22% end fade, 1.4px wandering contact, 4px glow and four low-count impact particles. Muzzle particles are disabled.
- [Shared particle assets](../../../../FEEDBACK/Shared/README.md) can be reused by any other effect without coupling it to Laser Tower code.
- `TESTS/Towers/LaserTowerChecks.cs` includes ripple point-count/endpoints, animated geometry, bounded contact movement, particle ownership/style toggle, optional straight-line fallback, unchanged DPS, and node-count regression checks.
- **Execution status for this change:** source and GitHub resource paths checked; **Godot/.NET headless tests and desktop visuals not executed** in the authoring environment. Inspect moving targets, short/long beams, 960×540 and 1920×1080, pause/resume, loss of ammo/EMP/overheat, and multiple simultaneously firing towers in the editor.

### Organic ripple refinement (2026-10-17)

The shared renderer retains the zero-irregularity travelling-sine fallback, but at nonzero `RippleIrregularity` blends in coherent bilinear value noise derived from each renderer's fixed seed. Transverse displacement and the purely cosmetic target-end wander evolve continuously rather than stepping through random offsets. Both share the existing in-place capped Line2D geometry and active-only processing; no gameplay endpoint, damage or tower-stat logic changed. The Laser Tower opts into the subtler yellow style via its own `.tres`, leaving other beam styles free to use different colors and irregularity settings.
