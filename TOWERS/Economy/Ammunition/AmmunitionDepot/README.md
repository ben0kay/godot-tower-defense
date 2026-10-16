# Ammunition Depot — implemented

A passive Economy building registered as `ammo_depot`; default price **150 Credits**, 2×2 footprint. Uses normal catalog/placement/navigation/payment, with simple vector storage artwork. It has no targeting/weapon loop.

Each placed node owns the existing `AmmoStorage` helper and its finite `Inventory`. Provisional config defaults: **10,000 capacity**, initially empty, **two home drones**, **250 payload**, **220px/s**. Storage capacity is captured at construction; source drone settings affect subsequent dispatches. Multiple depots remain independent.

Factory-owned drones deliver actual manufactured stock only on arrival; reservations prevent concurrent incoming shipments exceeding free room. When a tower reaches the shared10% threshold, logistics picks the nearest depot with stock and a free home drone. Load once, fly to the tower, unload up to current free space, then return home before freeing the slot. Partial shipments from scarce stock are valid; waiting requests retry without spawning duplicate deliveries.

The selected-building Inspector shows current/max stock, incoming reserved quantity and busy slots. No global magic stock, Credits-based ammo or reload-generated rounds exist. Destruction/cancellation/surplus policy lives in [logistics](../Shared/Logistics/README.md); [verification](../../../../TESTS/Economy/Ammunition.md) covers both delivery legs. Storage upgrades, saving, power, multiple ammo types and research remain deferred.

## Physical footprint — implemented

**2×2 cells (4 tiles)**, top-left clicked anchor, one root/artwork centered across the rectangle. Every occupied tile selects the same building. All cells must be buildable, free/unreserved and share the same PASSABLE or IMPASSABLE category. Mixed terrain rejects atomically; all-solid foundations cause no navigation work; passable foundations use one combined route check and incremental batched occupancy. Removal/rollback frees the whole claim only. Catalog icons remain 64px; scene artwork fits the larger footprint. Drone origins/returns use the centered root. Ammunition balance is unchanged. See [the completed footprint pass](../../../Placement/MULTI_CELL_FOOTPRINT_PASS.md) and [tests](../../../../TESTS/Towers/Footprints.md).
