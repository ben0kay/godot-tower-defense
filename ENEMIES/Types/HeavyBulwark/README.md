## Flat dark/red fleet artwork repolish — 2026-11-05

The existing `Artwork/heavy_bulwark_mk1.svg` has been redrawn as a **88×64 heavy siege derivative of Enemy004's tusked Bulwark**. Massive segmented side nacelles, detached forward cleaving tusks, deep dark structural channels, thick central armour, recessed split engines and restrained scarlet/orange machinery establish the same flat charcoal faction design at larger scale. It retains the original `0 0 160 100` viewBox, right (+X) nose, SVG path and native canvas. No gradients, baked highlights, glow or filters. Hull/navigation footprint, enemy data, shields, combat, rewards, scenes and waves remain unchanged; owner visual acceptance at normal gameplay zoom is pending.

---

# Heavy Bulwark (`heavy_bulwark_mk1`)

Class **Heavy**, Rank **Veteran**, Role **Fighter** (descriptive metadata only). Provisional balance: **500 hull**, **0 optional shield capacity**, **50px/s**, **24px navigation radius**, **4 Citadel arrival damage**, **12 kill Credits**. Right-facing alien robotic SVG: **88×64px**.

Edit `HeavyBulwark.tres` in Godot Inspector for identity, independent Class/Rank/Role and balance. The inherited GenericEnemy scene supplies movement, defense bars, shared shield visual, damage, rewards and death feedback. Registered for F2/F3 and waves via `Enemy("heavy_bulwark_mk1")`. Existing missions are unchanged. Shields remain opt-in: use `Enemy("heavy_bulwark_mk1", shield: true)` to start at capacity; ordinary/F2 spawns remain unshielded. No passive regeneration.

No special ability or extra per-frame processing.

Artwork intentionally extends beyond the navigation radius. This is large visual scale, not hull-shaped clearance; inspect tight-corridor overlap in game.
