# Power Relay — FUTURE / NOT IMPLEMENTED

## Core interaction
**Bridge two isolated terrain islands with exactly two placed relay structures.** The player places Relay A on the main landmass and Relay B on the isolated island, selects one, then explicitly chooses the other to connect.

The connection merges both islands' shared electricity grids. All generators and consumers on both landmasses now contribute/consume normally; no wires need to run through empty space and no relay-to-tower links or coverage radii exist. Another paired link can join a third island. Mere proximity never auto-connects a relay or generator.

Start with one peer per relay for simplicity. Link range limits (if any), exact validation and Inspector connection UI will be decided at implementation. Removing/destroying a relay/link splits the grids if no alternate relay path remains. Show an optional subtle link effect or network overlay, not permanent clutter.

Grid region detection and connectivity belong in [POWER](../../../POWER/README.md); building selection/placement stays with TOWERS. Relays are non-offensive utility structures, with no required ammo or targeting.

**Planning only. Tests later: connection, disconnection, destruction, alternate links, isolation, save/load and performance.**
