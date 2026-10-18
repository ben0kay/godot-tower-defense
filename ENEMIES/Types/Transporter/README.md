## Current artwork — alien robotic pass (2026-10-18)

**Bulbous armored carrier**, right-facing, native **112×72px**. Full rounded cargo hull, segmented curved armor, swept fins and a long recessed red launch spine, inspired by the supplied ballooned-carrier reference. Artwork only: stable identity, scenes, gameplay/ability settings and navigation clearance are unchanged. Earlier art/color descriptions below are historical. See `../ART_DIRECTION.md` for fleet-wide conventions and the review sheet.

---

# Transporter — E3A playable carrier

Stable catalog ID **transporter_mk1**, display name **Transporter**. Large amber cargo-vessel SVG **112×72px**, **500 hull**, **45px/s**, **24px navigation radius**, no shield capacity, **15 Credits** on kill and **5 Citadel damage**. These are editable playtest defaults in Transporter.tres, not shared engine constants. The artwork is deliberately larger than the collision disc; routing still requires radius below half a grid cell. No expanded large-unit navigation model is claimed.

The scene reuses Enemy.cs, EnemyMovement, DefenseBar and an optional Abilities/DeathSpawn child. The private ability config defaults to **six Standard Drones**, **0.12s** between releases, **48px** radial scatter and inherited remaining checkpoints. Child positions are validated and staggered by the shared queue. Each child uses its own type stats/rewards/movement permissions; no grandchildren in this pass.

Registered at the end of DefaultEnemyCatalog so F2 can cycle to it and waves can author `WaveBuilder.Enemy("transporter_mk1")`. Existing level wave compositions remain unchanged. For testing, run a battlefield, cycle F2 to Transporter and kill it with towers; parent death feedback happens normally and the drones emerge locally over time. A wave-owned final carrier cannot prematurely trigger victory.

See `ENEMIES/Abilities/DeathSpawn/README.md` and `TESTS/Enemies/DeathSpawn.md` for configuration, safe skip/cap behavior and actual verification. No enemy weapons or tower durability were added.

## Shared scene inheritance (GE1–GE3)

This type's `.tscn` inherits `ENEMIES/Shared/GenericEnemy.tscn`. Its own Definition and SVG remain independently editable; Sprite, Movement and DefenseBar defaults come from the parent. Edit common settings there, or deliberately override an inherited property here and use Inspector Revert to resume inheritance. Only ShieldGenerator and Transporter add their respective optional ability children. Catalog registration, stable IDs and gameplay values are unchanged.
