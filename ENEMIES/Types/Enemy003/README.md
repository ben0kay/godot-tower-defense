## Faint violet shield restoration range — 2026-11-06

The Shield Emitter now displays a thin, translucent violet perimeter at the exact **192px default `ShieldEmitterConfig.RadiusPixels`** (including a barely visible filled interior), moving with its ship in world space. The optional `ShieldRangeVisual` node draws behind the hull and follows live range edits; it hides when the emitter cannot actively pulse, on death or on removal. Rendering does not run an additional visual timer or physics tick. The ring shows **potential restoration reach**, not a guarantee that every nearby ship receives shielding; actual pulses still use MaxShield, eligibility and bounded recipient checks. This changes visuals only, not recipient shields, existing movement or combat. See [Shield Emitter component](../../Abilities/ShieldEmitter/README.md).

---

## Dark/red Shield Emitter artwork — 2026-11-06

`Artwork/enemy003.svg` now has solid charcoal and black mechanical armour with small red faction stripes on its split projector forks. Its central **violet/purple shield reactor and projection details are deliberately preserved** to distinguish support ships. This is the same 40×32 right-facing silhouette, with the same gameplay shield emitter component and no gradient/filter use. The earlier blue-grey gradient hull description below is historical. No enemy health, shield mechanics, class, waves, IDs or scripts changed; owner visual review pending.

---

## Enemy003 asset rename — 2026-10-26

The former ShieldGenerator source asset is now `ENEMIES/Types/Enemy003/Enemy003.tscn` with `Enemy003.tres` and `Artwork/enemy003.svg`. Its user-facing display name is **Shield Emitter** and its stable wave/save/spawn identity remains **`shield_emitter_mk1`**. The same `Abilities/ShieldEmitter` component and its independent Inspector config remain attached. Stats (100 hull, 80 optional shields, 70 px/s, 16px radius, 2 Citadel damage and 5 kill Credits), scene inheritance, 40×32 artwork and shield restoration behavior are unchanged.

The numbered **source filename/catalog position does not imply campaign introduction order**. The first two numbered assets remain Standard Drone and Swift Interceptor, with Bulwark still Enemy004. References in the default catalog, integration tests and authoring notes were updated. No waves or save IDs were changed.

---

## Current artwork — alien robotic pass (2026-10-18)

**Split-fork projection craft**, right-facing, native **40×32px**. Rounded physical reactor housing, split forward emitters and short stabilizer fins; violet core distinguishes this support craft from the fighters. Artwork only: stable identity, scenes, gameplay/ability settings and navigation clearance are unchanged. Earlier art/color descriptions below are historical. See `../ART_DIRECTION.md` for fleet-wide conventions and the review sheet.

---

# Enemy003 — Shield Emitter (support enemy)

DefaultEnemyCatalog registers the stable `shield_emitter_mk1` ID; `Enemy003` is its numbered source asset. The scene reuses Enemy and EnemyMovement with teal 40×32 placeholder art, Movement, hull DefenseBar and Abilities/ShieldEmitter. Provisional EnemyDefinition balance: 100 hull,80 optional shield capacity,70px/s,16px radius,2 Citadel damage,5 kill Credits. Default spawns have no starting shield; shield:true starts full. AffectSelf=false means this generator initially protects allies rather than replenishing itself.

ShieldEmitter's saved config lives in ENEMIES/Abilities/ShieldEmitter/DefaultShieldEmitterConfig.tres. Radius/pulse/eligibility/budgets are Inspector-editable; live emitter configs/timers are private. Current allies keep supplied shield points when the source dies/arrives/unloads. Enemy001/Enemy002 remain plain scenes without ability nodes.

Author Enemy("shield_emitter_mk1") in a wave, or explicitly select Enemy003.tscn as the level Debug EnemyScene to test it with F2. F2 cycles catalog scenes, including Enemy003, while Level001's authored waves do not include this type. No attack, death-spawn, shield visual, capacity-granting aura or linked shield mechanic is implemented.

## Shared scene inheritance (GE1–GE3)

This type's `.tscn` inherits `ENEMIES/Shared/GenericEnemy.tscn`. Its own Definition and SVG remain independently editable; Sprite, Movement and DefenseBar defaults come from the parent. Edit common settings there, or deliberately override an inherited property here and use Inspector Revert to resume inheritance. Only Enemy003 and Transporter add their respective optional ability children. Catalog registration, stable IDs and gameplay values are unchanged.
