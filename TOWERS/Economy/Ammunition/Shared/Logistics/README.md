# Ammunition logistics — implemented

`AmmunitionLogistics` is one world-owned Node2D in each World scene. Endpoints register/unregister directly; no scene-tree discovery or enemy scans occur. `AmmoDeliveryJob` holds source/destination, a separate finite cargo inventory, reserved incoming space and the visible drone.

## Dispatch and demand

Default scheduler: every **0.2s**, at most **8 endpoint checks**, alternating factory/request round-robin. Each check may inspect registered depots for the nearest eligible one; equal-distance ties preserve registration order. There is no per-frame all-building cross-product. Only active drones run flight physics.

Factories wait until their **ShipmentAmount** (default **1,000 rounds**) is fully available in the production buffer, the factory has a free drone with payload capacity for it, and a depot has at least that many **unreserved** free slots. Each factory flight moves exactly one full shipment (not an early or partial load). When a depot has fewer than 1,000 free slots, the factory waits or chooses another eligible depot. The default buffer and factory-drone payload are 1,000 each, while depot capacity is 10,000. **Depot → tower drone deliveries remain flexible partial loads** subject to existing refill logic. Consumer inventory events create one pending demand at/below the existing global threshold (10% by default). A tower has at most one active shipment, including its return flight. Stock arrival/source-slot release allows waiting requests to retry. After delivery takes stock above threshold, demand clears; a later depletion rearms it. A partial refill still below threshold remains queued until the current drone returns.

## Exact inventory accounting

At launch, source inventory transfers once into cargo. That removal is the source-stock reservation: another job cannot load those rounds. Destination free space is reserved separately, including factory→depot shipments from multiple sources. Destination stock remains unchanged until actual arrival. Arrival transfers at most available room and releases the incoming reservation; no TryAdd delivery/minting occurs. Source slots remain busy through the visible return journey.

## Interruption policy

- Removed destination: release incoming reservation and return its cargo to source.
- Destination capacity decreases/fills during flight: deliver what fits, return the surplus.
- Return home: transfer as much remaining cargo as home can hold. If home has meanwhile filled, excess is explicitly lost and counted in `LostRounds`; the slot is always released.
- Source or drone removed/destroyed: remaining cargo is lost once, reservations and slots are released. Removed endpoints' stored stock is also counted as loss. A destination needing ammo can request another source.
- Tower capacity shrink: tower-local excess is counted separately as `DiscardedRounds`.
- World unload: callbacks, jobs, demands and endpoint subscriptions are retired. Nothing persists into another level.

Conservation: initial stock + produced = current building/tower stock + cargo + fired/beam-consumed + explicit losses/discards. Test-only comparisons include cargo during flight. Return-state visuals retain an amber payload when surplus is aboard; empty returns are cyan/dark. Neither drone visuals nor arrival callbacks generate ammo.

No magazines, saving, research, power/material inputs, combat drones or per-tower threshold sliders. [Tests/results](../../../../../TESTS/Economy/Ammunition.md).
