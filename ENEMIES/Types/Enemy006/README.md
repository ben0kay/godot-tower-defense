# Enemy006 — Elite mk.1 (`elite_mk1`)

Fast, heavily armored elite enemy, inspired by the original GameMaker Elite mk.1 but balanced as requested for the new Godot TD: **Big mk.1-level hull health, Basic mk.1 movement speed, and artwork a little larger than the medium Bulwark.**

| Stat | Elite mk.1 | Compared with other Godot enemies |
| --- | ---: | --- |
| SVG artwork | **80 × 60 px** | Basic 32×24; Bulwark 64×48; Big 112×120 |
| Hull HP | **1,000** | Same as Big mk.1 |
| Optional shield capacity | **250** | Only active for wave entries with `shield: true` |
| Movement speed | **100 px/s** | Same as Standard Drone |
| Navigation clearance radius | **24 px** | Same as Big; below 32px half-cell bound |
| Turn speed | **6.0** | More responsive than Big |
| Citadel arrival damage | **5** | Same as Big |
| Kill reward | **10 Credits** | Same as Big |
| Death explosion multiplier | **1.45** | Scaled by existing feedback system |

The elite has a distinct right-facing swept-wing, plated metallic SVG with turquoise core and amber exhaust. Like other authored types, it **inherits `ENEMIES/Shared/GenericEnemy.tscn`** and contributes **no new scripts, abilities, per-tick checks, or shield regeneration**. The shared DefenseBar adjusts for the new sprite height.

Registered in `ENEMIES/DefaultEnemyCatalog.tres` as the seventh catalog scene, after Big mk.1. F2 cycles to it as the **seventh successful spawn**; F3 route preview lists it automatically. For waves use `Enemy("elite_mk1")` unshielded or `Enemy("elite_mk1", shield: true)` to start with 250 shield points, the shared aura and shield bar. **Existing Level001/Level002 wave compositions remain unchanged.** This is intentionally provisional playtest balance.

The catalog regression covers authored stats, real portal spawn, SVG size, optional shield damage/depletion, credited kill and Citadel arrival; Godot .NET runtime/visual verification remains pending.
