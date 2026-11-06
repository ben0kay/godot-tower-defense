# Heavy Transporter (`heavy_transporter_mk1`)

## Dark/red carrier repolish and modest enlargement — 2026-11-05

`Artwork/heavy_transporter_mk1.svg` is now an armoured, ribbed industrial carrier with thick cargo outriggers, gated launch bay, blunt prow and three recessed propulsion ports, using the flat charcoal/near-black/scarlet fleet palette. **Native SVG size increased from 144×100 to 160×112** (about 11% wider and 12% taller); the original `0 0 160 100` viewBox and +X facing remain. **Visual-only size change**: 24px navigation radius, hull HP, two DeathSpawn payloads, IDs, scene and waves are unchanged. Inspect greater hull overlap in narrow corridors. Godot visual validation pending.


Class **Superheavy**, Rank **Veteran**, Role **Cargo** (descriptive metadata only). Provisional balance: **1400 hull**, **0 optional shield capacity**, **35px/s**, **24px navigation radius**, **8 Citadel arrival damage**, **25 kill Credits**. Right-facing alien robotic SVG: **160×112px** (previously 144×100).

Edit `HeavyTransporter.tres` in Godot Inspector for identity, independent Class/Rank/Role and balance. The inherited GenericEnemy scene supplies movement, defense bars, shared shield visual, damage, rewards and death feedback. Registered for F2/F3 and waves via `Enemy("heavy_transporter_mk1")`. Existing missions are unchanged. Shields remain opt-in: use `Enemy("heavy_transporter_mk1", shield: true)` to start at capacity; ordinary/F2 spawns remain unshielded. No passive regeneration.

The carrier mounts two existing DeathSpawn components with independent local configs: six Standard Drones and four Swift Interceptors. Both use the same budgeted release queue, wave reservations and destination/checkpoint inheritance. Hull kills release the payload; arrival/unload do not. No new ability tick or living launches.

Artwork uses an enlarged 160×112 canvas with the original 0 0 160 100 viewBox, and intentionally extends beyond the navigation radius. This is large visual scale, not hull-shaped clearance; inspect tight-corridor overlap in game.
