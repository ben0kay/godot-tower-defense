# Ammunition Factory — FUTURE / NOT IMPLEMENTED

**Role:** non-offensive building that manufactures ammunition in **batches** for storage depots. It should not require turret nodes, a targeting loop or combat stats.

## Proposed gameplay
- Configurable ammunition type(s), materials/credits per batch, batch size, production interval/speed, optional internal buffer and electricity draw.
- Consume inputs through the shared ECONOMY authority, complete production with a timer, then dispatch the batch to a chosen/eligible [Ammunition Depot](../AmmunitionDepot/README.md) subject to capacity. Whether the factory-to-depot batch needs physical transport is a later decision; initially a logical batch transfer is acceptable.
- Pause if required supplies/power are unavailable or the output cannot be stored. No silently generated or lost stock.
- Future purchased levels/research can improve production speed, batch size and efficiency without modifying authored values. Track in-progress batches per instance; process active work efficiently, not full registry searches per frame.

See [shared logistics](../../../LOGISTICS/Ammunition/README.md) and [POWER](../../../POWER/README.md).

**Planning only. No code, artwork or scenes yet.**
