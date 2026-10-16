# Ammunition Depot — implemented

A passive Economy building registered as `ammo_depot`; default price **150 Credits**, 1×1 footprint. Uses normal catalog/placement/navigation/payment, with simple vector storage artwork. It has no targeting/weapon loop.

Each placed node owns the existing `AmmoStorage` helper and its finite `Inventory`. Provisional config defaults: **10,000 capacity**, initially empty, **two home drones**, **250 payload**, **220px/s**. Storage capacity is captured at construction; source drone settings affect subsequent dispatches. Multiple depots remain independent.

Factory-owned drones deliver actual manufactured stock only on arrival; reservations prevent concurrent incoming shipments exceeding free room. When a tower reaches the shared10% threshold, logistics picks the nearest depot with stock and a free home drone. Load once, fly to the tower, unload up to current free space, then return home before freeing the slot. Partial shipments from scarce stock are valid; waiting requests retry without spawning duplicate deliveries.

The selected-building Inspector shows current/max stock, incoming reserved quantity and busy slots. No global magic stock, Credits-based ammo or reload-generated rounds exist. Destruction/cancellation/surplus policy lives in [logistics](../Shared/Logistics/README.md); [verification](../../../../TESTS/Economy/Ammunition.md) covers both delivery legs. Storage upgrades, saving, power, multiple ammo types and research remain deferred.

## Requested physical footprint (PLANNED — not yet coded)

**Depot: 2×2 cells (4 tiles).** Codex must update `AmmunitionDepotDefinition.tres`, size/center the actual scene artwork and keep **10,000 rounds** of storage. See the [generic rectangular footprint pass](../../../Placement/MULTI_CELL_FOOTPRINT_PASS.md). Until implemented, the current building still uses the original 1×1 footprint.

**Foundation rule for the upcoming 2×2 version:** all four buildable cells must be the same terrain category — **all PASSABLE or all IMPASSABLE**. A mixed foundation is structurally invalid and must be rejected without spending Credits or affecting navigation. [Codex footprint brief](../../../Placement/MULTI_CELL_FOOTPRINT_PASS.md).
