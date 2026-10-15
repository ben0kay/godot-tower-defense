# Economy Towers

Player-placeable income, production, storage and power-generating structures. Buildings are registered through the existing tower catalog/placement authority; a structure does **not** need weapon targeting or combat stats merely because it is placeable.

## Existing/planned categories

- [ResourceTower](ResourceTower/README.md): periodic Credits generation (planned).
- [ScrapRefinery](ScrapRefinery/README.md): prospective scrap-processing building (planned).
- [AmmunitionFactory](AmmunitionFactory/README.md): automatically manufactures timed ammunition batches, with a **small finite internal output buffer**.
- [AmmunitionDepot](AmmunitionDepot/README.md): stores large ammunition reserves and dispatches resupply requests.
- [PowerGenerators](PowerGenerators/README.md): **one common power generator family** with three distinct types: Solar (low/islands), Industrial (standard starting generator), Fusion (endgame).
- [SolarTower](SolarTower/README.md): older possible energy/defensive-laser hybrid; not the dedicated Solar Generator.

## Future ammunition production loop

1. Each Ammo Factory begins producing batches **automatically**, with no manual Start button or prerequisite depot; production is timer-based.
2. Every completed batch enters the **factory's own small buffer**. The factory automatically transfers completed batches into any eligible Ammo Depot with enough free capacity. Initial factory-to-depot transfer is **logical/instant**, without drones or conveyors.
3. **If no depot exists, or all compatible depots lack capacity, production continues until the factory's internal buffer fills.** Then production pauses. It resumes automatically when batches can transfer out and free space becomes available; completed ammunition is never deleted.
4. For a simple first pass, require enough buffer space to fit an entire next batch before starting that batch, and enough depot space for an entire transfer batch. This avoids overflow or half-created batches; partial transfers, depot selection and exact dispatch order can be refined later.
5. The Ammo Depot stores ammunition and sends it to depleted offensive towers using [Transport Drones](../../DRONES/Transport/README.md), coordinated by [LOGISTICS/Ammunition](../../LOGISTICS/Ammunition/README.md). **Factory → depot uses automatic batches; depot → tower uses physical drones.**
6. The first production pass can manufacture automatically without requiring raw material inputs; costs/input recipes, electricity draw, research boosts and ammo types can be added separately when required. No free ammunition can appear from an unrelated reload operation.

Future researchable controls include factory production time, batch size, buffer capacity, depot storage, drone slot count, drone speed and payload size; preserve per-instance state and authored level rows separately.

## Future electricity loop

Every contiguous terrain landmass automatically shares one grid; disconnected islands have separate grids. A pair of manually linked [Power Relays](../Utility/PowerRelay/README.md) connects two islands. No cables or relay coverage radii. [POWER](../../POWER/README.md) manages grid accounting and shortages; generator variants share one implementation under [PowerGenerators](PowerGenerators/README.md). Each consumer can eventually expose a per-instance Inspector **Power Priority 0–100** slider, with higher priorities receiving electricity during shortages.

## Ownership and staged implementation

- `TOWERS/Economy/`: the **placeable producer/storage buildings** and their own buffers/output.
- `TOWERS/Shared/Ammunition/`: a tower's local reserve, optional magazine/reload.
- `ECONOMY/`: existing resource identity, wallet and atomic transactions; don't create a second wallet for factories.
- `LOGISTICS/Ammunition/`: scheduling batches/resupply and stock movement, not drone movement internals.
- `DRONES/`: drone flight/behaviours; transport first, combat/player ability maybe later.
- `POWER/`: shared island grid, relay joining, generation/demand/priorities.

**Best isolated first implementation pass later:** basic **factory timer → finite buffer → automatic depot transfer → pause/resume**. This can be tested without drone spawning, electricity, research, ammunition-consuming towers or a cross-genre economy. It should reuse existing placement/transactions but **nothing here is implemented or an instruction to start coding immediately**.

Keep Tower Defense independently playable; do not implement all subsystems just because their planning folders now exist.
