# Ammunition Factory — FUTURE / NOT IMPLEMENTED

## Basic first-pass design
A non-offensive, player-placeable building that **automatically** produces ammunition in batches over time. **It functions even when no Ammunition Depot has been built.** A small internal output buffer prevents factory output from vanishing.

### Production and transfer rules
- Factory starts producing automatically while it has sufficient buffer space for a complete next batch. Suggested tuning: `ProductionSecondsPerBatch`, `BatchAmount`, `InternalBufferCapacity` (plus future ammunition type, upgrade, recipe and power data).
- After the timer completes, add exactly one batch to internal buffer. Attempt to **automatically transfer complete batches** to an available compatible [Ammunition Depot](../AmmunitionDepot/README.md) with room.
- **No depot / depot full:** batches remain in the factory buffer. Factory can continue making batches until the buffer cannot fit another whole batch; then **pause production** instead of deleting or overflowing ammunition. Resume without player input after free space appears due to a later depot placement or depot consumption.
- **First pass: factory → storage is an instant logical batch transfer**, with no drones, belts or trucks. Reserve actual flying [Transport Drones](../../../DRONES/Transport/README.md) for **storage → towers**.
- Factory can be tested independently from firing/reloads, drones, power, research and even raw material inputs. Initially assume automatic generation; explicit material recipes, electricity consumption, construction time and resource efficiency become later feature passes.
- Choose predictable transfer order (e.g., stable depot registration) and an efficient event/budgeted dispatcher; no all-depot scans per factory each frame.

### Data and safety
Use authored settings for duration/batch/buffer maxima and **per-instance state** for current buffer stock, production elapsed time and paused status. The amount must never be generated twice, transferred twice, silently lost or exceed capacity. Decide timing semantics for a nearly full buffer and checkpoint/saving later.

Future purchased upgrades/research may improve production speed, batch volume and buffer; changes must not overwrite base authored source data. Use existing ECONOMY identities/transactions when inputs are eventually introduced, not a second global wallet.

See [Economy design](../README.md), [Depot](../AmmunitionDepot/README.md), [LOGISTICS](../Logistics/Ammunition/README.md).

**Documentation only — no factory code, scenes or gameplay yet.**
