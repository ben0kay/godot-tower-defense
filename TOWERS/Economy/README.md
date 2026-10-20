## Tower durability rebalance — 2026-10-20 (source updated; Godot runtime tests pending)

**Current authoritative defaults supersede older HP references below:** Base/max hull health for every available player-built tower/structure is **10× the previous level-1 value**. All **nine offensive towers** (Battle, Laser, Blaster Cannon, Shockwave, Machine Gun, Sniper, Rocket, Cryo, Stasis) have explicit, independent paid level snapshots: **1,000 → 1,050 → 1,102.5 HP** (two-level towers stop at 1,050). **Every purchased upgrade adds 5% of the previous tier's Max HP**, compounded, not 5 percentage points of the original at every tier; the values are authored as complete absolute tier stats, not runtime additive bonuses.

**Other buildings:** Ammunition Factory and Depot **1,000 HP** each (one build tier/no paid upgrades yet); Resource Tower and Adaptable Resource Generator **1,500 → 1,575 → 1,653.75 HP** (new tier progression replaces their former 150/200/250 HP schedule); Stealth Beacon **1,800 HP** (one tier). Generic passive config fallback is 1,000 HP. These are **hull**, not shields. Enemy damage, friendly firing stats, costs, unlocks, construction timing and Citadel HP are unchanged.

**Pipeline and HP semantics:** Stage02 still selects one authored tier, then Stage03 applies earned **rank HP bonuses independently**. The existing `TowerBase.Durability` synchronizes max capacity while **preserving missing absolute HP**; upgrades do not fully heal already-damaged towers. The current Inspector's `Max HP` comparison automatically shows the extra `(+50)` at level2 and `(+52.5)` at level3 for a standard-rank 1,000-HP offensive tower. No duplicate HP system, per-frame calculations, or tower-specific damage handlers were added.

**Verification still needed:** Compile Godot .NET, run `TESTS/Towers/TowerHealthChecks.tscn` and the existing integration scene that executes `TowerStatChecks`/`TowerUpgradeChecks`, plus `TESTS/Economy/CreditTowerChecks.tscn`. Inspect upgraded/damaged towers at runtime, including the resource producer `.tres` serialized tiers. Written test assertions were updated, but **not executed here**.

---

## Credit economy towers — CP2/CP3 (2026-10-19)

Resource Tower (2×2 periodic active-wave income) and Adaptable Resource Generator (1×1 nearby killing-tower income) are now available under Economy. Each has three paid tiers, independent capped storage and Inspector collection, using the existing wallet and shared production helpers. See `ECONOMY/Production/README.md` from the repository root for balance/settings and deferred research. Earlier planned status below is historical.

---

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
