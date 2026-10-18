# Shield Frigate (`shield_frigate_mk1`)

Family **ShieldShip**. Provisional balance: **120 hull**, **350 optional shield capacity**, **65px/s**, **20px navigation radius**, **3 Citadel arrival damage**, **9 kill Credits**. Right-facing alien robotic SVG: **72×48px**.

Edit `ShieldFrigate.tres` in Godot Inspector for identity/family/balance. The inherited GenericEnemy scene supplies movement, defense bars, shared shield visual, damage, rewards and death feedback. Registered for F2/F3 and waves via `Enemy("shield_frigate_mk1")`. Existing missions are unchanged. Shields remain opt-in: use `Enemy("shield_frigate_mk1", shield: true)` to start at capacity; ordinary/F2 spawns remain unshielded. No passive regeneration.

No special ability or extra per-frame processing.
