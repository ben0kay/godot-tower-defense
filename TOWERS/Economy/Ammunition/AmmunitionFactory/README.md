# Ammunition Factory — implemented

A passive Economy building registered as `ammo_factory`; default price **200 Credits**, 1×1 footprint. Its scene, simple vector artwork and canonical config use the normal catalog/placement/selection/payment infrastructure. It has no targeting, damage or mandatory combat stats.

`AmmunitionFactory.cs` owns and advances the existing `AmmoProduction`. Defaults in `DefaultAmmunitionFactoryConfig.tres` / `AmmunitionFactoryConfig.cs`: **100 ammo every2 seconds**, **400 buffer**, **one home drone**, **200 payload**, **220px/s**. All are editable provisional values. Production parameters are captured when building the placed instance; transport settings are read for subsequent dispatches. Existing stock/progress is never reset by a display refresh.

With no depot, production fills the small buffer and pauses when another whole batch cannot fit. Drone departure frees buffer space and production resumes automatically, without banking elapsed time while full. [Shared logistics](../Shared/Logistics/README.md) picks the nearest eligible depot, reserves incoming room and loads actual buffer stock into separate cargo. Prefer whole production batches when they fit; a smaller shipment may use leftover depot space/payload. Unloading occurs only on arrival, followed by a visible return flight. No instant transfer.

Select the factory to see current/max buffered stock, producing/full status and busy drone slots. Building stock is independent of Credits and other instances. Production/material recipes, power, purchased building levels and research remain future work. See [tests](../../../../TESTS/Economy/Ammunition.md).
