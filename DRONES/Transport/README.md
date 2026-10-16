# Transport drones — implemented

`TransportDrone.tscn` / `.cs` is one reusable direct-flight courier for **both Factory→Depot and Depot→Tower**, including the return journey. Lightweight code-drawn vector art shows amber loaded cargo and a cyan frame/dark empty cargo area. It draws above towers/enemies; no physics body, navigation grid, enemy query or collision area is added.

The caller supplies home/destination, speed, generic loaded indication and arrival/return/abort callbacks. The drone only moves, renders and reports lifecycle; [ammunition logistics](../../TOWERS/Economy/Ammunition/Shared/Logistics/README.md) owns cargo, finite source slots, demand, reservations and stock transfers. Source settings configure speed, payload and home drone count; defaults are in each building config.

Movement clamps to the endpoint so large physics steps do not skip arrival. Outbound and return remain distinct visible phases. Changed/missing destinations turn home; a missing home or external drone destruction aborts exactly once. The dispatcher reconciles stock/loss and frees the slot. All flight/production/dispatch nodes inherit normal pause behavior. Only active drones process; idle slots have no scene nodes.

This can support another cargo role via callbacks without building a speculative universal logistics engine. Combat drones, health, weapons and advanced flight/navigation remain future work. [Verification](../../TESTS/Economy/Ammunition.md).
