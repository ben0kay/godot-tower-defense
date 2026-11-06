## Flat dark/red fleet artwork repolish — 2026-11-05

The existing `Artwork/reinforced_drone_mk1.svg` is now a **40×30 flat charcoal/scarlet armoured fork-drone** matching Enemy001's fleet language. Chunkier twin stabilizers, exposed dark fork joints, layered side plates, forward mandibles, paired recessed red drive ports and a narrow scarlet nose sensor distinguish it from the smaller Standard Drone. It retains its original `0 0 160 100` viewBox, right (+X) nose, file path and sprite dimensions. No gradients, baked metallic shine, glow or SVG filters. Artwork only: its stable ID, class/rank/role, stats, shield setup, scenes, navigation, rewards and waves are unchanged. Godot native-zoom visual acceptance remains pending.

---

# Reinforced Drone (`reinforced_drone_mk1`)

Class **Light**, Rank **Veteran**, Role **Fighter** (descriptive metadata only). Provisional balance: **110 hull**, **20 optional shield capacity**, **100px/s**, **14px navigation radius**, **2 Citadel arrival damage**, **4 kill Credits**. Right-facing alien robotic SVG: **40×30px**.

Edit `ReinforcedDrone.tres` in Godot Inspector for identity, independent Class/Rank/Role and balance. The inherited GenericEnemy scene supplies movement, defense bars, shared shield visual, damage, rewards and death feedback. Registered for F2/F3 and waves via `Enemy("reinforced_drone_mk1")`. Existing missions are unchanged. Shields remain opt-in: use `Enemy("reinforced_drone_mk1", shield: true)` to start at capacity; ordinary/F2 spawns remain unshielded. No passive regeneration.

No special ability or extra per-frame processing.
