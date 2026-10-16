> **FOUNDATIONS IMPLEMENTED / LOGISTICS NOT IMPLEMENTED:** [AmmoProduction](../AmmunitionFactory/AmmoProduction.cs), [AmmoStorage](../AmmunitionDepot/AmmoStorage.cs) and [AmmoInventory](../../../Shared/Ammunition/AmmoInventory.cs) exist as independent C# helpers. Codex must reuse them to implement actual **factory → depot → tower flights**, a dispatcher and inventory reservations; nothing currently transports ammo in game.

# Shared ammunition logistics (FUTURE / NOT IMPLEMENTED)

This category owns **what inventory must be transferred and when**, not the design or flight physics of each drone.

- [Ammunition](Logistics/README.md): Stage A **factory-owned transport drone** carrying small-buffer stock to a depot. Stage B **depot-owned transport drone** carrying depot stock to a consuming tower. Both are required for first gameplay implementation.
- [DRONES](../../../../DRONES/README.md): separate reusable mobile-unit umbrella. Its [Transport](../../../../DRONES/Transport/README.md) behavior handles flight and returns; future [Combat](../../../../DRONES/Combat/README.md) drones may be launched by player abilities.
- ECONOMY continues to own resource IDs/credits/payments; AmmunitionFactory/Depot each own their local stocks; tower ammunition state stays inside TOWERS/Shared/Ammunition.

Keep a single compact dispatch/transfer authority where needed, with scheduled/event-driven work rather than every building scanning all others per frame. No generalized logistics engine and no change to current gameplay until requested.

**Documentation only; not implemented.**
