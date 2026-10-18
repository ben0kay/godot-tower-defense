# Cruiser (`cruiser_mk1`)

Family **CapitalShip**. Provisional balance: **3000 hull**, **400 optional shield capacity**, **30px/s**, **24px navigation radius**, **12 Citadel arrival damage**, **35 kill Credits**. Right-facing alien robotic SVG: **168×96px**.

Edit `Cruiser.tres` in Godot Inspector for identity/family/balance. The inherited GenericEnemy scene supplies movement, defense bars, shared shield visual, damage, rewards and death feedback. Registered for F2/F3 and waves via `Enemy("cruiser_mk1")`. Existing missions are unchanged. Shields remain opt-in: use `Enemy("cruiser_mk1", shield: true)` to start at capacity; ordinary/F2 spawns remain unshielded. No passive regeneration.

No special ability or extra per-frame processing.

Artwork intentionally extends beyond the navigation radius. This is large visual scale, not hull-shaped clearance; inspect tight-corridor overlap in game.
