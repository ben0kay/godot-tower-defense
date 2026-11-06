## Flat dark/red fleet artwork repolish — 2026-11-05

`Artwork/racer_mk1.svg` now depicts a **34×18 extremely slender three-fork interceptor** in Enemy002's flat charcoal/near-black/scarlet palette. Longer swept wing spars, open negative space around the central spine, a razor-like forward stinger, small red identification slashes and a compact red/orange tail drive emphasize speed without copying Enemy002's silhouette. The original `0 0 160 100` viewBox, right (+X) orientation and texture path are intact. The old metallic gradient and cyan sensor are gone. No gameplay, navigation, scene, config, wave or import changes; owner in-game native-zoom review remains pending.

---

# Racer (`racer_mk1`)

Class **Tiny**, Rank **Veteran**, Role **Fighter** (descriptive metadata only). Provisional balance: **20 hull**, **0 optional shield capacity**, **230px/s**, **7px navigation radius**, **1 Citadel arrival damage**, **3 kill Credits**. Right-facing alien robotic SVG: **34×18px**.

Edit `Racer.tres` in Godot Inspector for identity, independent Class/Rank/Role and balance. The inherited GenericEnemy scene supplies movement, defense bars, shared shield visual, damage, rewards and death feedback. Registered for F2/F3 and waves via `Enemy("racer_mk1")`. Existing missions are unchanged. Shields remain opt-in: use `Enemy("racer_mk1", shield: true)` to start at capacity; ordinary/F2 spawns remain unshielded. No passive regeneration.

No special ability or extra per-frame processing.
