# Combat Drones — FUTURE / NOT IMPLEMENTED

## Player-ability concept
A future **player ability** may launch a drone along a chosen/defined enemy path or corridor to attack enemies encountered during its flight. This is separate from depot resupply: it should not require ammo cargo, depot drone slots or a logistics dispatch job unless a future mechanic explicitly calls for those.

## Possible gameplay parameters
Flight speed, ability cooldown/charge, attack damage, attack frequency or contact radius, range/path length, lifetime/return behaviour and target filtering. Research could improve these values independently of transport capacity.

## Boundaries
- The player-ability/input system chooses when and where to deploy; this folder would own the combat drone's flight/attack behaviour.
- Reuse [DRONES shared conventions](../README.md) for generic movement/visual lifetime where helpful; integrate actual damage via existing COMBAT and enemy spatial-query/attribution services.
- A corridor-guided flight may need the authored route/corridor geometry, but must not override enemy path selection or create an independent enemy routing engine.
- Design and test hit frequency, interactions with fast/spinning enemies, cooldown UI, overlapping launches, pooling, and 500+ simultaneous enemies only when this ability is actually requested.

**Entirely speculative capability; no ability, weapon or drone implementation now.**
