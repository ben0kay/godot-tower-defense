## Flat dark/red Shield Frigate artwork — 2026-11-06

Replaced the `Artwork/shield_frigate_mk1.svg` source with a detailed **flat near-black and charcoal support frigate**, plus scarlet wing markings and red drives, while preserving its 72×48 native canvas, large broad prow/wing profile and distinct **purple shield reactor**. The shield core stays violet to identify the frigate's support role; the old metallic gradients and reflective grey plating are gone. The original game scene, authored class/rank/role, optional shields, stats, navigation, wave identity and gameplay code are untouched. Review the shader-free vector art in Godot at gameplay zoom.

---

# Shield Frigate (`shield_frigate_mk1`)

Class **Heavy**, Rank **Veteran**, Role **Support** (descriptive metadata only). Provisional balance: **120 hull**, **350 optional shield capacity**, **65px/s**, **20px navigation radius**, **3 Citadel arrival damage**, **9 kill Credits**. Right-facing alien robotic SVG: **72×48px**.

Edit `ShieldFrigate.tres` in Godot Inspector for identity, independent Class/Rank/Role and balance. The inherited GenericEnemy scene supplies movement, defense bars, shared shield visual, damage, rewards and death feedback. Registered for F2/F3 and waves via `Enemy("shield_frigate_mk1")`. Existing missions are unchanged. Shields remain opt-in: use `Enemy("shield_frigate_mk1", shield: true)` to start at capacity; ordinary/F2 spawns remain unshielded. No passive regeneration.

No special ability or extra per-frame processing.
