# Ammunition Factory — implemented

A passive Economy building registered as `ammo_factory`; default price **200 Credits**, 1×1 footprint. Its scene, simple vector artwork and canonical config use the normal catalog/placement/selection/payment infrastructure. It has no targeting, damage or mandatory combat stats.

`AmmunitionFactory.cs` owns and advances the existing `AmmoProduction`. Defaults in `DefaultAmmunitionFactoryConfig.tres` / `AmmunitionFactoryConfig.cs`: **100 ammo every 2 seconds**, **1,000-round buffer**, **1,000-round ShipmentAmount**, **one home drone**, **1,000-round payload**, **220px/s**. All are editable provisional values. Production parameters are captured when building the placed instance; transport settings are read for subsequent dispatches. Existing stock/progress is never reset by a display refresh.

With no depot, production fills the small buffer and pauses when another whole batch cannot fit. Drone departure frees buffer space and production resumes automatically, without banking elapsed time while full. [Shared logistics](../Shared/Logistics/README.md) picks the nearest eligible depot, reserves incoming room and loads actual buffer stock into separate cargo. A factory drone departs **only when at least the configured ShipmentAmount (default 1,000) has been buffered AND a depot has space for the entire shipment**. Each factory trip carries exactly that amount; it will NOT launch early with 100–900 rounds, or partially fill a depot's remaining space. Production can pause with a full buffer until a valid shipment is possible. This setting is independent of the 100-round production batches. Unloading occurs only on arrival, followed by a visible return flight. No instant transfer.

Select the factory to see current/max buffered stock, producing/full status and busy drone slots. Building stock is independent of Credits and other instances. Production/material recipes, power, purchased building levels and research remain future work. See [tests](../../../../TESTS/Economy/Ammunition.md).

## Requested physical footprint (PLANNED — not yet coded)

**Factory: 3×2 cells (6 tiles).** Codex must update `AmmunitionFactoryDefinition.tres`, size/center the actual scene artwork and keep existing 1,000-round factory-drone shipments intact. See the [generic rectangular footprint pass](../../../Placement/MULTI_CELL_FOOTPRINT_PASS.md). Until implemented, the current building still uses the original 1×1 footprint.
