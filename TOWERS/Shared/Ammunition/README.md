# Local tower ammunition — implemented

`AmmoInventory` remains the finite stock/atomic transfer authority. `TowerAmmunition` adds one placed weapon's reserve, per-launch charging, paid beam-time credit, delivery status and consumption/discard counters. It is optional: passive buildings do not receive a weapon inventory.

`AmmunitionStats` is an optional immutable group in the existing eight-stage pipeline. Each offensive `*Levels.cs` row is the only authored capacity/consumption source; Stage02 selects the complete row, Stages03–06 preserve/allow future modifiers, Stage07 clamps and Stage08 publishes the cached group. No new cumulative modifiers or second pipeline exists.

| Weapon | Capacity, every current level | Consumption |
| --- | --- | --- |
| Battle Tower | 1,000 | 1 per projectile launch |
| Laser Tower | 1,000 | 2 per second of actual aligned beam firing |
| Blaster Cannon | 600 | 3 per projectile launch, not per impact/enemy |
| Shockwave Tower | 400 | 5 per emitted pulse, not per affected enemy |

Defaults other than Battle1000/1 are provisional. Empty reserves prevent unpaid attacks; targeting/aiming/cooldowns and damage attribution are preserved. Laser prepays integer rounds, carries fractional paid ammo credit across frames/targets/pauses, and limits damage to the funded firing duration. An empty reserve may finish the already-paid fraction; there is no free beam tail or per-physics-tick charge. No magazines/reloads exist yet.

TowerConfig exports `InitialAmmoFraction` (0–1, full by default) for creation-time stock, rounded to nearest round. Authoring a low fraction is useful for quick testing. Rebuilds, promotions and upgrades never initialize again or refill: capacity growth preserves stock; shrink discards only excess and records `DiscardedRounds`. XP/rank persists independently.

The existing bottom-right Inspector shows capacity/consumption, current reserve and waiting/in-flight/out-of-ammo state. Changed next-level ammo values appear in ordinary upgrade previews. Inventory/status events coalesce into one selected-only runtime label update; shots do not recompute FinalStats/upgrade previews.

`DefaultAmmunitionConfig.tres` supplies the shared 10% rule; changing its exported threshold refreshes registered demands. [Logistics](../../Economy/Ammunition/Shared/Logistics/README.md) loads real stock into drone cargo and unloads only on arrival. Delivery never calls TryAdd; only initial stock/factory production create rounds. Magazines, ammo types, per-tower threshold sliders, research and saving remain deferred.

Tests: [ammunition verification](../../../TESTS/Economy/Ammunition.md).
