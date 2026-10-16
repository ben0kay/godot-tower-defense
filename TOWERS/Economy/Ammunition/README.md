> **FOUNDATION IMPLEMENTED:** Factory [AmmoProduction.cs](AmmunitionFactory/AmmoProduction.cs) (timed batches and small bounded buffer) and Depot [AmmoStorage.cs](AmmunitionDepot/AmmoStorage.cs) (finite depot inventory) now exist as **unwired C# helpers**. The shared [AmmoInventory](../../Shared/Ammunition/AmmoInventory.cs) handles transfers. **Codex next:** build actual selectable Factory/Depot scenes, connect their lifecycles to these helpers, add visible drones for **BOTH legs** and shared dispatcher, integrate consuming towers. See [WORK.md](../../../NOTES/WORK.md).

# Economy / Ammunition — FUTURE / NOT IMPLEMENTED

This section collects **ammunition-producing and storage buildings** and their common delivery coordination, like Offensive/ collects its tower types.

## Building folders

- [AmmunitionFactory/](AmmunitionFactory/README.md): automatically makes batches, keeps a small internal buffer and pauses when full.
- [AmmunitionDepot/](AmmunitionDepot/README.md): receives produced batches, stores large amounts and eventually launches resupply drones.
- [Shared/](Shared/README.md): shared inventory-transfer coordination, including [Logistics/](Shared/Logistics/README.md).

## Planned loop

1. Factory produces automatically with no depot needed.
2. Full batches collect in its limited internal buffer; **factory-owned transport drones physically carry stock to an available depot**. No instant transfer.
3. Without available storage/drones, the factory fills its buffer and pauses until a drone loads buffered stock and frees room.
4. Another drone transports depot stock to low-ammo towers: [transport drones](../../../DRONES/Transport/README.md) to refill towers running low.

**Separate tower consumption:** individual offensive towers hold/use/reload their own ammunition through [TOWERS/Shared/Ammunition](../../Shared/Ammunition/README.md). That remains outside Economy because it is the consumer-side capability. [DRONES](../../../DRONES/README.md) is an independent umbrella for mobile units, including future combat drones.

**All above is planning only; no gameplay code or scene changes are authorized by these README folders.**
