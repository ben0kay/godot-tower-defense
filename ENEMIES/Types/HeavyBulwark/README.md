# Heavy Bulwark (`heavy_bulwark_mk1`)

Family **Bulwark**. Provisional balance: **500 hull**, **0 optional shield capacity**, **50px/s**, **24px navigation radius**, **4 Citadel arrival damage**, **12 kill Credits**. Right-facing alien robotic SVG: **88×64px**.

Edit `HeavyBulwark.tres` in Godot Inspector for identity/family/balance. The inherited GenericEnemy scene supplies movement, defense bars, shared shield visual, damage, rewards and death feedback. Registered for F2/F3 and waves via `Enemy("heavy_bulwark_mk1")`. Existing missions are unchanged. Shields remain opt-in: use `Enemy("heavy_bulwark_mk1", shield: true)` to start at capacity; ordinary/F2 spawns remain unshielded. No passive regeneration.

No special ability or extra per-frame processing.

Artwork intentionally extends beyond the navigation radius. This is large visual scale, not hull-shaped clearance; inspect tight-corridor overlap in game.
