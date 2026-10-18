# Racer (`racer_mk1`)

Family **Interceptor**. Provisional balance: **20 hull**, **0 optional shield capacity**, **230px/s**, **7px navigation radius**, **1 Citadel arrival damage**, **3 kill Credits**. Right-facing alien robotic SVG: **34×18px**.

Edit `Racer.tres` in Godot Inspector for identity/family/balance. The inherited GenericEnemy scene supplies movement, defense bars, shared shield visual, damage, rewards and death feedback. Registered for F2/F3 and waves via `Enemy("racer_mk1")`. Existing missions are unchanged. Shields remain opt-in: use `Enemy("racer_mk1", shield: true)` to start at capacity; ordinary/F2 spawns remain unshielded. No passive regeneration.

No special ability or extra per-frame processing.
