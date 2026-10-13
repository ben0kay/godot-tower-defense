# FEEDBACK (Future Systems)

This major folder is intentionally **structure only** for now. Effects should react to health/combat events, not be mixed into camera movement or navigation.

- `FeedbackMasterConfig.cs` — common toggles for later, not instantiated yet.
- `DefenseBars/` — enemy shields, armor and/or hull bars when hit.
- `DamageNumbers/` — floating damage feedback, potentially merged for rapid hits.
- `CameraEffects/` — camera shake or other impact feedback (ordinary pan/zoom stays in `CAMERA/`).

Compare the existing event-driven implementations in `ben0kay/isometric-new-2d/VISUALS/Feedback` when we reach these features. Avoid per-enemy always-running feedback logic during large waves.
