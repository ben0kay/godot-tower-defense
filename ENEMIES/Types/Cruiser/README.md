## Flat dark/red capital-ship artwork refresh — 2026-10-26

`Artwork/cruiser_mk1.svg` now uses Enemy001's **hard-edged charcoal panels, near-black recesses, restrained scarlet marking strips and compact red-orange drive lights** rather than gradient-grey metallic lighting. Its **168×96px** sprite and original `0 0 160 100` viewBox remain unchanged. Long detached upper/lower armour nacelles, structural bays, an elongated central spine, forward cutwater arms and a broad twin-engine stern keep it distinct from the small drones and medium Bulwark. The nose still points right (+X). No shaders, gradients, filters, new weapons, AI or gameplay changes. `cruiser_mk1` scene/ID and ship clearance are untouched. Desktop normal-zoom art acceptance is pending.

---

# Cruiser (`cruiser_mk1`)

Family **CapitalShip**. Provisional balance: **3000 hull**, **400 optional shield capacity**, **30px/s**, **24px navigation radius**, **12 Citadel arrival damage**, **35 kill Credits**. Right-facing alien robotic SVG: **168×96px**.

Edit `Cruiser.tres` in Godot Inspector for identity/family/balance. The inherited GenericEnemy scene supplies movement, defense bars, shared shield visual, damage, rewards and death feedback. Registered for F2/F3 and waves via `Enemy("cruiser_mk1")`. Existing missions are unchanged. Shields remain opt-in: use `Enemy("cruiser_mk1", shield: true)` to start at capacity; ordinary/F2 spawns remain unshielded. No passive regeneration.

No special ability or extra per-frame processing.

Artwork intentionally extends beyond the navigation radius. This is large visual scale, not hull-shaped clearance; inspect tight-corridor overlap in game.
