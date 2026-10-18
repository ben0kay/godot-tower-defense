## Current artwork — alien robotic pass (2026-10-18)

**Four-lance capital warship**, right-facing, native **112×120px**. Tall independent lance wings/nacelles, armored center spine, blue reactor and recessed surface machinery. Artwork only: stable identity, scenes, gameplay/ability settings and navigation clearance are unchanged. Earlier art/color descriptions below are historical. See `../ART_DIRECTION.md` for fleet-wide conventions and the review sheet.

---

# Enemy005 — Big mk.1 (`big_mk1`)

A **large, slow, high-durability plain enemy** inspired by the original GameMaker `o_enemy_005` / `Big mk.1`. It deliberately has **no new AI/abilities** and uses the existing inherited `ENEMIES/Shared/GenericEnemy.tscn`: shared movement, targeting/registry, hull + shield bars, optional shield aura, Citadel damage, rewards and death explosion.

| Setting | Godot playtest default | Original GameMaker |
| --- | ---: | ---: |
| Hull HP | **1,000** | 1,000 `armour_max` |
| Optional shield capacity | **200** | 200 `shield_max` |
| Speed | **80 px/s** | 0.8 (relative to Basic 1.0) |
| Citadel damage | **5** | 5 |
| Kill Credits | **10** | 10 |
| Navigation radius | **24 px** | No directly equivalent exported navigation radius |
| TurnSpeed | **4.5** | Uses old shared movement/rotation |
| Artwork | **112 × 120 SVG** | Original separate GameMaker sprite |

The resized **112×120 SVG** uses broader, longer upper/lower armor wings around the original hull (uniformly magnified, not distorted), making Big mk.1 **3.5× as wide and 5× as tall as Enemy001** and **1.75× as wide and 2.5× as tall as the 64×48 medium Bulwark**. This brings its proportions closer to the old GameMaker version. The large, plated twin-wing SVG faces **right (+X)** to match the shared enemy movement orientation. Its visible hull extends beyond its **24px circular clearance radius**, just as the existing Transporter art does. This is a deliberate approximation under the current grid-size constraint (radius strictly below 32px), **not** enlarged hull-aware pathfinding. Validate wing overlap and tight corridor visuals in game. **Navigation radius remains 24px; increasing visual size does not change navigation clearance or targeting hit detection.**

**Shield opt-in:** F2 and normal `SpawnManager.Spawn("big_mk1")` start with no shield, despite 200 capacity. In a wave use `Enemy("big_mk1", shield: true)` to start with 200 SP and automatically show the existing shield aura and shield meter; `Enemy("big_mk1")` stays unshielded. No passive regeneration is introduced.

**Testing:** `Enemy005.tscn` is appended to `ENEMIES/DefaultEnemyCatalog.tres` (F2 and F3 route preview). Level001 and Level002 authored wave compositions are **unchanged**. To try it immediately, press F2 until Big mk.1 spawns, or author a test batch in `LEVELS/Level001/Level001Waves.cs`. The new assertions in `TESTS/Integration/EnemyCatalogChecks.cs` cover catalog, hull/shield initialization, sprite sizing, shield break and kill rewards. Compile/headless/desktop verification is pending.
