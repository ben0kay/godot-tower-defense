# Reinforced Drone (`reinforced_drone_mk1`)

Family **Drone**. Provisional balance: **110 hull**, **20 optional shield capacity**, **100px/s**, **14px navigation radius**, **2 Citadel arrival damage**, **4 kill Credits**. Right-facing alien robotic SVG: **40×30px**.

Edit `ReinforcedDrone.tres` in Godot Inspector for identity/family/balance. The inherited GenericEnemy scene supplies movement, defense bars, shared shield visual, damage, rewards and death feedback. Registered for F2/F3 and waves via `Enemy("reinforced_drone_mk1")`. Existing missions are unchanged. Shields remain opt-in: use `Enemy("reinforced_drone_mk1", shield: true)` to start at capacity; ordinary/F2 spawns remain unshielded. No passive regeneration.

No special ability or extra per-frame processing.
