# Economy Towers

Player-placeable income, production, storage and power-generating structures. Buildings are registered through the existing tower catalog/placement authority; a structure does **not** need weapon targeting or combat stats merely because it is placeable.

## Grouped categories

- [Power/](Power/README.md) — Solar, Industrial, Fusion and Shared grid/generator plans.
- [Ammunition/](Ammunition/README.md) — Factory, Depot and Shared/Logistics.
- [TOWERS/Shared/Ammunition](../Shared/Ammunition/README.md) and [TOWERS/Shared/Power](../Shared/Power/README.md) stay separate for **individual tower consumption**.

## Existing/planned categories

- [ResourceTower](ResourceTower/README.md): periodic Credits generation (planned).
- [ScrapRefinery](ScrapRefinery/README.md): prospective scrap-processing building (planned).
- [AmmunitionFactory](Ammunition/AmmunitionFactory/README.md): automatically manufactures timed ammunition batches, with a **small finite internal output buffer**.
- [AmmunitionDepot](Ammunition/AmmunitionDepot/README.md): stores large ammunition reserves and dispatches resupply requests.
- [PowerGenerators](Power/Shared/README.md): **one common power generator family** with three distinct types: Solar (low/islands), Industrial (standard starting generator), Fusion (endgame).
- [SolarTower](SolarTower/README.md): older possible energy/defensive-laser hybrid; not the dedicated Solar Generator.

## Implemented ammunition production loop

1. Each Ammo Factory begins producing batches **automatically**, with no manual Start button or prerequisite depot; production is timer-based.
2. Every completed batch enters the **factory's own small buffer**. The factory-owned **transport drone** carries buffered ammunition to an eligible Ammo Depot with reserved free capacity; ammunition arrives only when that drone unloads.
3. **If no depot exists, or all compatible depots lack capacity, production continues until the factory's internal buffer fills.** Then production pauses. It resumes when a delivery drone loads some buffered ammunition and frees space; completed ammunition is never deleted.
4. For the first gameplay pass, require enough buffer room for a whole next batch, and reserve depot room before dispatch. Whole batches are preferred when possible; partial shipments use remaining payload/space safely. Nearest eligible depots and round-robin requests are deterministic.
5. The Ammo Depot stores ammunition and sends it to depleted offensive towers using [Transport Drones](../../DRONES/Transport/README.md), coordinated by [Ammunition Logistics](Ammunition/Shared/Logistics/README.md). **Both factory → depot and depot → tower use visible drones carrying finite cargo.**
6. The first production pass can manufacture automatically without requiring raw material inputs; costs/input recipes, electricity draw, research boosts and ammo types can be added separately when required. No free ammunition can appear from an unrelated reload operation.

Future researchable controls include factory production time, batch size, buffer capacity, depot storage, drone slot count, drone speed and payload size; preserve per-instance state and authored level rows separately.

## Future electricity loop

Every contiguous terrain landmass automatically shares one grid; disconnected islands have separate grids. A pair of manually linked [Power Relays](../Utility/PowerRelay/README.md) connects two islands. No cables or relay coverage radii. [POWER](Power/README.md) manages grid accounting and shortages; generator variants share one implementation under [PowerGenerators](Power/Shared/README.md). Each consumer can eventually expose a per-instance Inspector **Power Priority 0–100** slider, with higher priorities receiving electricity during shortages.

## Ownership and staged implementation

- `TOWERS/Economy/`: the **placeable producer/storage buildings** and their own buffers/output.
- `TOWERS/Shared/Ammunition/`: a tower's local reserve, optional magazine/reload.
- `ECONOMY/`: existing resource identity, wallet and atomic transactions; don't create a second wallet for factories.
- `TOWERS/Economy/Ammunition/Shared/Logistics/`: scheduling batches/resupply and stock movement, not drone movement internals.
- `DRONES/`: drone flight/behaviours; transport first, combat/player ability maybe later.
- `TOWERS/Economy/Power/`: individual generators plus shared island grid, relay joining, generation/demand/priorities.

**Completed gameplay loop:** the existing AmmoProduction/AmmoStorage/AmmoInventory helpers now power real catalog-registered Factory/Depot buildings, consuming offensive towers and visible two-leg drone deliveries. Inspector status and conservation/lifecycle regression tests are implemented. See [WORK](../../NOTES/WORK.md) and [verification](../../TESTS/Economy/Ammunition.md).
