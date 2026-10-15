# Power Generators — FUTURE / NOT IMPLEMENTED

## Why one shared generator category?
**Solar Generator, Industrial Generator and Fusion Reactor all perform the same job: produce electricity.** They should eventually share one PowerGenerator building implementation and one PowerGeneratorConfig shape, using separate stable tower IDs, authored settings, artwork and build-menu entries. Do not create three copies of electricity-generation logic.

They are **three generator TYPES**, not purchased upgrade levels of one building. A Solar Generator could have its own independent Level 1/2/3 progression, as could Industrial and Fusion. Preserve TOWERS' existing complete/absolute per-level row conventions if purchased upgrades are added; do not make purchasing a level turn Industrial into Fusion.

| Generator | Tier / use | Relative production | Distinguishing purpose |
| --- | --- | --- | --- |
| **Solar Generator** | Small autonomous outpost; cheap and sustainable | Low | Excellent for a small isolated terrain island |
| **Industrial Generator** | Standard starting generator used on most levels | Medium | Reliable general-purpose base workhorse |
| **Fusion Reactor** | Costly endgame infrastructure | Very high | Supports major installations and intensive defenses |

Actual balance values, costs, work times, artwork, footprints, potential fuels and durability are **not chosen**. The first power pass should not require a fuel production chain.

## Common generator design
- A placed generator registers its output once with the [POWER network](../Power/README.md) corresponding to its **connected terrain island**. No tower-to-generator cables, supply radii or explicit tower subscriptions within one island.
- Manually paired [Power Relays](../../Utility/PowerRelay/README.md) join islands and their supply/demand pools. An unlinked Solar island stays independent. A generator automatically shares its output across any deliberately relay-joined network, without extra local/grid switches.
- Prospective per-generator tunables: production amount/rate, construction cost, optional build/upgrade work duration, durability, operating status; future research output/efficiency modifiers. Keep **generator source settings** separate from current running state and network-derived totals.
- If the tower catalog exposes three build choices, each should have a **distinct stable ID and resource/config**, but reuse the same generator class and network registration; never hardcode generator names in POWER's accounting logic.
- The future power implementation can use a shared `PowerGenerator.cs` and `PowerGeneratorConfig.cs` here, plus three separate authored resources/scenes as needed. **No C# classes/resources/scenes are authorized by this README alone.**

## Existing SolarTower note
[Existing SolarTower](../SolarTower/README.md) is an older possible **hybrid solar + weak defensive laser** concept. The dedicated Solar Generator is a pure producer. Decide later whether the hybrid becomes an upgrade/variant of the common generator or a distinct registered placeable; avoid duplicate default Solar Generator entries.

## Testing later
Different types sharing one implementation, independent upgraded levels, registration/deregistration, multiple generators per island, relay network merges/splits, shutdown/destroy, research changes and saving.

**Documentation-only. Current Tower Defense remains unchanged.**
