# FEEDBACK — Combat and World Response

Reusable visual feedback systems. **DefenseBars are now active on Enemy001**, while DamageNumbers and CameraEffects remain reserved.

- `FeedbackMasterConfig.cs` + `DefaultFeedbackConfig.tres`: shared feedback visibility toggles.
- `DefenseBars/`: event-driven health indicator component used by Enemy001 and reusable for future damageable towers. Configure its colors, capped logarithmic sizing and offset in `DefenseBars/DefaultDefenseBarConfig.tres`.
- `DamageNumbers/`: future hit numbers and damage aggregation.
- `CameraEffects/`: future impact shakes (normal pan/zoom remains in `CAMERA/`).

Keep feedback separate from combat simulation. Health values come from owners: bars never poll entity state or register per-frame processing. Rotating ships update a visible bar's counter-rotation during their existing movement callback. See `DefenseBars/README.md`.
