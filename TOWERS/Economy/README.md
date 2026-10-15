# Economy Towers

Player-placeable income, energy generation and processing structures. Inspired by Bloons TD income towers and the older GameMaker resource structures.

- `ResourceTower/`: periodic credit generation.
- `SolarTower/`: energy production, with a possible **defensive laser upgrade**.
- `ScrapRefinery/`: eventual conversion of scrap into useful material.

The tower's *main role* decides its folder, not whether it can ever deal damage. Hybrid towers can gain optional behavior as they are upgraded. No implementation yet.

## Future infrastructure plans — ammunition logistics and electricity (NOT IMPLEMENTED)

**Planning notes only.** These are ideas for later, separate implementation passes. Keep the present standalone Tower Defense gameplay, existing placement/economy, tower combat and folder organization unchanged until specifically requested. This complements the optional ammunition, power and construction parameters in [TOWERS/README.md](../README.md); it does not authorize Codex to build the whole system now. Use the existing shared ResourceDefinition/economy and tower capabilities rather than hardcoding support logic inside individual offensive towers.

### Ammunition production and delivery

- **Ammunition Factory:** manufactures configurable batches of ammunition over time, potentially consuming raw resources and electricity. Production rate, batch size and costs are future balance/research parameters.
- **Ammunition Depot / Storage:** receives produced batches and stores a large configurable reserve; it supports nearby or assigned offensive towers. Multiple depots should be useful, not just extra capacity.
- **Supply Drones:** each depot has a limited, configurable number of drones. When a tower's local reserve falls below its resupply threshold, the depot dispatches an available drone carrying a configurable payload. The drone flies from depot to tower, transfers only the amount that fits, then returns. Towers can keep firing during transit, and may run dry if resupply arrives too late. Start with simple straight-line flying drones; more elaborate flight logic can come later.
- **Inventory correctness:** distinguish factory output, depot stock, reserved/in-flight ammunition and the tower's own reserve/magazine. Never duplicate ammunition on dispatch, cancellation, destruction or return. Depots should not create stock when a magazine reloads. Ammo factory-to-depot batch transfer can start as a simple logical transfer; its transport method, delivery routing and failure rules can be decided in the actual implementation pass.
- **Optional by weapon:** not every tower needs ammunition or magazines. Ammo type (bullets, rockets, energy cells, etc.), attack consumption, refill triggers, reload delays and starting stock are separate parameters to finalize later; see the parent TOWERS README. Keep the resupply service shared rather than putting a per-frame depot search in every tower.

### Electricity: connected terrain regions, not cables

**Simplified design decision:** every contiguous group of buildable/connected terrain tiles is **one shared local power grid**. Buildings on the same landmass automatically share generation and consumption, without cables, transmission radii, or individual tower-to-relay links. Empty space divides disconnected asteroid islands into separate grids. Prefer **four-directional (edge) tile connectivity**, so diagonal-only tile contact does not join grids; settle exact terrain eligibility when implementing it.

Three proposed generator tiers:

| Generator | Role |
| --- | --- |
| **Solar Generator** | Cheap, small, sustainable output. Best for independently powering small isolated asteroid islands; no relay required if sufficient generation is local. |
| **Industrial Generator** | The normal **starting/main generator** used in most levels; reliable mid-tier output for ordinary defensive bases. |
| **Fusion Reactor** | Expensive, very high-output **endgame** power plant for large installations. |

These are **names and gameplay roles**, not agreed balance values or implemented scenes. The current planned \`SolarTower/\` includes a possible future defensive-laser upgrade; preserve that separate hybrid idea for later consideration.

**Power Relays bridge islands only.** Place one relay on the main landmass and one on the isolated landmass, then select one relay and manually connect it to the other. The **paired link merges their two shared power grids** across empty space. If the link is removed or a relay is destroyed, the islands revert to independent grids. Further islands can be joined with additional pairs if desired. No need to create visible power conduits in the void, automatic proximity-based relay connections, or a cable network across tiles. A subtle link effect or power overlay is optional future UI. Recompute connectivity when tiles/relays/links change, not by tracing every consumer each frame.

**Important consequence of the simplified design:** proximity to a relay alone does nothing, but a generator **on the same connected landmass** as a linked relay belongs to that landmass's shared grid and contributes power to the merged network automatically. If a Solar Generator is intended to stay isolated, put it on an **unlinked** island. Do not add local-only/grid-only source assignment switches or per-tower source selection in the initial pass.

### Power allocation and Inspector priority

- Each placed electricity-consuming tower/building can have an individual **Power Priority** slider in its existing Inspector: **0 = lowest, 100 = highest**, provisional default **50**. This is a mutable **per-instance operational setting**, not a purchased level or research modifier.
- When supply meets demand, all connected consumers work normally. During shortages, give available generation to **higher-priority buildings first** (ideally whole-building power for deterministic firing/operation, with stable tie handling); lower-priority factories or towers can suspend operation. Define graceful power loss/restoration and anti-flicker handling during implementation.
- This lets players prioritize expensive defensive towers during difficult waves at the expense of ammunition production or less important installations. Each isolated or relay-merged grid calculates its own supply/demand and priorities.
- A future capacitor/battery or emergency defense preset could be added separately, **not required** for the initial electricity pass.

### Future research and upgrades

Make relevant quantities explicit and separately configurable so a later research center or persistent Idle/Empire game can modify them **without rewriting base authored stats**: factory throughput/batch sizes; depot capacity/drone slots; drone speed/payload; tower ammo capacity/usage; solar/industrial/fusion output; and optional relay features or power-efficiency improvements. Don't implement the research system now. Existing purchased tower levels remain complete, absolute C# rows; future permanent research stays a separate source (Stage 04 in the tower-stat pipeline) and must not mutate authored level data. Power Priority is a player-set choice, **not** research.

### Sensible later implementation order

1. Local offensive-tower ammo storage, consumption and optional magazines/reload (already planned in the parent TOWERS README).
2. Ammunition Factory + Depot batch production/storage and shared refill requests.
3. Depot-managed resupply drones, finite payloads and inventory reservations.
4. Three electricity generator types + shared terrain-island grid accounting.
5. Paired relays between islands + per-building Inspector priority slider and shortage handling.
6. Only afterwards: research modifiers, additional energy storage, deeper supply logistics and visual polish.

**Current priority remains polishing Tower Defense.** Do not create infrastructure nodes, scenes, stats, UI or directories from these notes until a specific feature pass is requested.

## Dedicated future building folders (README-only)

- [Ammunition Factory](AmmunitionFactory/README.md) — production batches.
- [Ammunition Depot](AmmunitionDepot/README.md) — large storage and drone dispatch.
- [Solar Generator](SolarGenerator/README.md) — small isolated-island supply.
- [Industrial Generator](IndustrialGenerator/README.md) — everyday starting/main generator.
- [Fusion Reactor](FusionReactor/README.md) — high-output endgame power.

The existing [SolarTower](SolarTower/README.md) remains a possible **hybrid solar/laser concept**, not a second dedicated Solar Generator. Reconcile their eventual catalog identities before implementing either. [POWER](../../POWER/README.md) owns terrain-island grids; [LOGISTICS/Ammunition](../../LOGISTICS/Ammunition/README.md) owns inter-building deliveries. No gameplay is implemented by these new docs.
