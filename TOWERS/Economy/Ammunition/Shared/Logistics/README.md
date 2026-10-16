> **LOGISTICS REMAINS UNIMPLEMENTED.** Reuse actual [AmmoProduction](../../AmmunitionFactory/AmmoProduction.cs), [AmmoStorage](../../AmmunitionDepot/AmmoStorage.cs) and [AmmoInventory](../../../../Shared/Ammunition/AmmoInventory.cs) rather than creating competing counters. **Both legs require visible drones**, loading at source and unloading at destination. Reserve destination capacity and track jobs/flight cargo to avoid duplication. [WORK.md](../../../../../NOTES/WORK.md) is authoritative.

# Ammunition transfers and drone resupply — FUTURE / NOT IMPLEMENTED

## Two distinct transport stages

**Stage A — Ammunition Factory → Depot: real drone transport (required)**
1. [Factory](../../AmmunitionFactory/README.md) automatically produces one completed batch at each production interval.
2. Batch enters **factory's small internal buffer**, even when no depot exists.
3. A factory-owned drone loads a batch from `AmmoProduction.Buffer`, then **physically flies** to a [Depot](../../AmmunitionDepot/README.md) with reserved free capacity. Only on arrival does its cargo unload into the depot. **No instant transport.**
4. If no depot exists or all are full/busy, the factory's buffer accumulates until it cannot fit another complete batch, then production **pauses**. Loading a delivery drone frees buffer capacity so production **resumes**.
5. Track exact source/destination amounts, with a stable depot selection policy and no double transfers. This isolated producer/storage stage can be tested before introducing towers needing ammo.

**Stage B — Depot → Tower: Transport Drone deliveries (later)**
1. A [tower](../../../../Shared/Ammunition/README.md) below its refill threshold creates one outstanding resupply request.
2. Shared logistics selects a depot with compatible ammo, enough stock and a free drone; **reserves stock once** and creates a delivery job.
3. [DRONES/Transport](../../../../../DRONES/Transport/README.md) flies a drone directly through space to the tower, delivers up to available local tower capacity, then returns to its depot.
4. Logistics reconciles leftover payload, cancellations or destroyed endpoints without duplication; tower can keep fighting during travel but may run dry.

## Clear system responsibilities
- Factory/depot hold their own buffers and stored quantities.
- Logistics coordinates transfers, resupply requests, stock reservation and jobs.
- DRONES owns drone flight and visuals, **not** ammo transaction accounting; [DRONES/Combat](../../../../../DRONES/Combat/README.md) is separately reserved for a future player ability.
- Keep ammo type, batch time/size, buffer/depot caps, delivery threshold, drone speed/cargo/slots and possible research bonuses configurable. Resist a generic cross-genre transport engine now.

## Future verification
No depot, one/multiple depots, full buffer, pause/resume after building storage, exactly-once batches, mixed types, deletion, idle delivery, requests while firing, drone jobs and checkpoint conservation. Prefer event notifications or budgeted work over per-frame nested scans.

**No gameplay implementation yet.**
