# Ammunition buildings — implemented

The current playable loop is **Factory → visible transport drone → Depot → visible transport drone → consuming tower**. Both buildings are catalog-registered Economy structures, purchasable through the existing build tray, wallet and 1×1 navigation-safe placement.

- [AmmunitionFactory](AmmunitionFactory/README.md): automatic whole-batch production into its own finite buffer; pauses when another batch cannot fit.
- [AmmunitionDepot](AmmunitionDepot/README.md): large local storage, incoming reserved capacity and home-based refill drones.
- [Shared/Logistics](Shared/Logistics/README.md): budgeted requests, nearest eligible endpoints, physical cargo and reservations.
- [Tower-local ammo](../../Shared/Ammunition/README.md): complete explicit level values and weapon consumption, separate from producer/storage behavior.

Factory production does not require a depot, materials, power or activation. Transfers require real flights: the factory waits for a full **1,000-round shipment** (100 ammo every 2 seconds) before its drone departs; depot storage defaults to **10,000 rounds**. A missing depot or fewer than 1,000 unreserved depot slots leaves produced stock buffered. Depot → tower resupply behaviour is unchanged. Drones return home before their source slot can be reused. Credits remain in ECONOMY's one wallet; ammunition is not a second currency balance.

All additional balance is provisional. No permanent research, magazines, power, inputs, saving or combat drones are implemented here. See [WORK](../../../NOTES/WORK.md) and [verification](../../../TESTS/Economy/Ammunition.md).
