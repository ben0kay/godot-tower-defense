# FEEDBACK — Combat and World Response

Reusable visual feedback systems. **DefenseBars and DeathExplosion are now active on Enemy001**, while DamageNumbers and CameraEffects remain reserved.

- `FeedbackMasterConfig.cs` + `DefaultFeedbackConfig.tres`: shared feedback visibility toggles.
- `DeathExplosion/`: reusable automatically-sized kill smoke and orange flash using GPU particles.
- `DefenseBars/`: event-driven health indicator component used by Enemy001 and reusable for future damageable towers. Configure its colors, capped logarithmic sizing and offset in `DefenseBars/DefaultDefenseBarConfig.tres`.
- `DamageNumbers/`: future hit numbers and damage aggregation.
- `CameraEffects/`: future impact shakes (normal pan/zoom remains in `CAMERA/`).

Keep feedback separate from combat simulation. Health values come from owners: bars never poll entity state or register per-frame processing. Rotating ships update a visible bar's counter-rotation during their existing movement callback. See `DefenseBars/README.md`.

## Shared DeathExplosion (implemented)

`DeathExplosion/` owns a reusable, **one-shot `GpuParticles2D`** orange flash + smoky grey/orange burst, with soft SVG particle artwork. This is an independent world effect, not a child of an enemy or tower being freed. `World/Effects` is the shared canvas layer for effects. `DeathExplosion.Spawn(layer, worldPosition, visualSizePixels, multiplier)` works for enemy kills now and future destroyed towers. The effect scales automatically from the source `Sprite2D.GetRect()` and the lengths of its global transform axes; per-type `DeathExplosionMultiplier=1.0` is an **optional** override. Edit `DeathExplosion/DefaultDeathExplosionConfig.tres` for shared scale, duration, particle count and active-effect cap; edit particle materials/colors in `DeathExplosion.tscn`. The feedback master config can disable effects. Arrival/removal without death does not explode, and effects self-delete after finishing. See `DeathExplosion/README.md`.

## Shared shockwave presentation — Phase4E

Shockwave/ owns one reusable ShockwaveEffect scene and Inspector-editable ShockwaveStyle. Blaster supplies its own orange impact style and uses autonomous cosmetic expansion after instant damage. The new Shockwave Tower supplies a purple style and drives the same visual with its actual gameplay wavefront radius. The visual has no enemies or damage code. The former AreaBlast/BlastFlash was removed; AreaBlast/README.md points to the replacement. DeathExplosion remains a separate kill-only visual.
