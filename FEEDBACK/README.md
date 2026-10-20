## Stealth opacity — SE1 (2026-10-20 UTC)

`Stealth/` adds configurable event-driven hidden/revealed opacity, composing with existing status tint and dimming shield/defense feedback. It owns no targeting rules or reveal membership.

# FEEDBACK — Combat and World Response

Reusable visual feedback systems. **DefenseBars and DeathExplosion are now active on Enemy001**, while DamageNumbers and CameraEffects remain reserved.

- `FeedbackMasterConfig.cs` + `DefaultFeedbackConfig.tres`: shared feedback visibility toggles.
- `DeathExplosion/`: reusable automatically-sized kill smoke and orange flash using GPU particles.
- `Shared/`: reusable particle textures and process-material presets. The scene/effect that uses them owns its own `GpuParticles2D` nodes, lifetime and position; currently the shared beam renderer references small energy-contact sparks.
- `Shields/`: one shared, tinted, auto-scaled shield aura and hit/break tween component; lazily attached only to currently shielded enemies. Intended to be reusable by future damageable towers without implementing tower shields or regeneration.
- `DefenseBars/`: event-driven combined **hull + shield** indicators used by inherited enemy scenes and reusable for future damageable towers. The shield meter renders as an extra row within the *same* `DefenseBar` node only while the owner has shield points. Configure its colors, capped logarithmic sizing and offset in `DefenseBars/DefaultDefenseBarConfig.tres`.
- `DamageNumbers/`: future hit numbers and damage aggregation.
- `CameraEffects/`: future impact shakes (normal pan/zoom remains in `CAMERA/`).

Keep feedback separate from combat simulation. Health values come from owners: bars never poll entity state or register per-frame processing. Rotating ships update a visible bar's counter-rotation during their existing movement callback. See `DefenseBars/README.md`.

## Shared DeathExplosion (implemented)

`DeathExplosion/` owns a reusable, **one-shot `GpuParticles2D`** orange flash + smoky grey/orange burst, with soft SVG particle artwork. This is an independent world effect, not a child of an enemy or tower being freed. `World/Effects` is the shared canvas layer for effects. `DeathExplosion.Spawn(layer, worldPosition, visualSizePixels, multiplier)` works for enemy kills now and future destroyed towers. The effect scales automatically from the source `Sprite2D.GetRect()` and the lengths of its global transform axes; per-type `DeathExplosionMultiplier=1.0` is an **optional** override. Edit `DeathExplosion/DefaultDeathExplosionConfig.tres` for shared scale, duration, particle count and active-effect cap; edit particle materials/colors in `DeathExplosion.tscn`. The feedback master config can disable effects. Arrival/removal without death does not explode, and effects self-delete after finishing. See `DeathExplosion/README.md`.

## Shared shockwave presentation — Phase4E

Shockwave/ owns one reusable ShockwaveEffect scene and Inspector-editable ShockwaveStyle. Blaster supplies its own orange impact style and uses autonomous cosmetic expansion after instant damage. The new Shockwave Tower supplies a purple style and drives the same visual with its actual gameplay wavefront radius. The visual has no enemies or damage code. The former AreaBlast/BlastFlash was removed; AreaBlast/README.md points to the replacement. DeathExplosion remains a separate kill-only visual.

## Shared particle resources — beam visual foundation

[`Shared/README.md`](Shared/README.md) describes the reusable monochrome spark texture and gentle energy-contact material. The `TOWERS/Shared/Weapons/Beams/BeamRenderer.tscn` scene owns its impact/muzzle GPU particle emitters and toggles them only while the beam is active. The shared folder owns **resource presets**, not runtime emitters or a new manager. Other weapon effects can instance or reference these assets independently.

## Shield visual presentation (SV1)

[`Shields/README.md`](Shields/README.md) documents a single reddish baked SVG aura that scales to the owner's sprite and remains idle between shield-change events. Enemy shields already exist in `COMBAT/Damage` and `ENEMIES/Shared`; visual nodes contain **no gameplay shield state**. No new regeneration is implemented. `Shared/` continues to hold cross-effect resources rather than another copy of the shield system.
