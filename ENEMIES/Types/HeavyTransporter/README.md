# Heavy Transporter (`heavy_transporter_mk1`)

Family **Carrier**. Provisional balance: **1400 hull**, **0 optional shield capacity**, **35px/s**, **24px navigation radius**, **8 Citadel arrival damage**, **25 kill Credits**. Right-facing alien robotic SVG: **144×100px**.

Edit `HeavyTransporter.tres` in Godot Inspector for identity/family/balance. The inherited GenericEnemy scene supplies movement, defense bars, shared shield visual, damage, rewards and death feedback. Registered for F2/F3 and waves via `Enemy("heavy_transporter_mk1")`. Existing missions are unchanged. Shields remain opt-in: use `Enemy("heavy_transporter_mk1", shield: true)` to start at capacity; ordinary/F2 spawns remain unshielded. No passive regeneration.

The carrier mounts two existing DeathSpawn components with independent local configs: six Standard Drones and four Swift Interceptors. Both use the same budgeted release queue, wave reservations and destination/checkpoint inheritance. Hull kills release the payload; arrival/unload do not. No new ability tick or living launches.

Artwork intentionally extends beyond the navigation radius. This is large visual scale, not hull-shaped clearance; inspect tight-corridor overlap in game.
