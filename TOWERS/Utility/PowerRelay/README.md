## Revised PW5 relay role — powered distribution booster (2026-11-04)

**PLANNED.** A Power Relay is now a **non-generating signal/distribution anchor**, not merely a bridge across space. On its **own powered conducting island**, even an unpaired relay may restore/improve nearby towers' **recharge-rate efficiency** over long conductive distances, as if the signal were being repeated near the tower. It creates **zero energy**: local generator/Citadel production remains the finite shared supply cap. An unpowered/unreachable relay cannot recharge anything, and multiple relays must never count one source's generation multiple times. Decide modest relay overhead / throughput / chaining behavior when implementing PW5; cap efficiency rather than letting relay cycles amplify power indefinitely.

**Retain existing manual cross-island pairing:** the player can also link two relays on separate islands to merge reachable supply networks. The newly available remote island may then receive charge through the powered pair subject to the finite **merged** production budget and local recharge-distance falloff. Removing a relay or source removes its contribution/coverage; existing tower buffers remain charged and deplete naturally. Cache relay coverage and rebuild on actual link/source/topology events; no per-tower spatial polling.

See [PW4/PW5 design plan](../../Economy/Power/IMPLEMENTATION_PLAN.md). The earlier core-interaction section below is preserved as historical bridge-only planning; its statement that relays have "no coverage radii" is **superseded for recharge distribution**, though power still needs no physical wires.

---

# Power Relay — FUTURE / NOT IMPLEMENTED

## Core interaction
**Bridge two isolated terrain islands with exactly two placed relay structures.** The player places Relay A on the main landmass and Relay B on the isolated island, selects one, then explicitly chooses the other to connect.

The connection merges both islands' shared electricity grids. All generators and consumers on both landmasses now contribute/consume normally; no wires need to run through empty space and no relay-to-tower links or coverage radii exist. Another paired link can join a third island. Mere proximity never auto-connects a relay or generator.

Start with one peer per relay for simplicity. Link range limits (if any), exact validation and Inspector connection UI will be decided at implementation. Removing/destroying a relay/link splits the grids if no alternate relay path remains. Show an optional subtle link effect or network overlay, not permanent clutter.

Grid region detection and connectivity belong in [POWER](../../Economy/Power/README.md); building selection/placement stays with TOWERS. Relays are non-offensive utility structures, with no required ammo or targeting.

**Planning only. Tests later: connection, disconnection, destruction, alternate links, isolation, save/load and performance.**
