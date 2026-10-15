# POWER — Shared electricity networks (FUTURE / NOT IMPLEMENTED)

## Single authority
Reserve this top-level folder for the future **PowerGridManager** and optional **PowerMasterConfig**. This system owns network membership, generation/demand and priority allocation, not generator artwork or weapon combat. Do not create those classes or change runtime gameplay until a dedicated pass is approved.

## Simple island-grid rules
1. An **edge-connected group of eligible terrain tiles is one power grid** (prefer four-directional neighbours, no diagonal-only joins). Buildings on that landmass automatically share electricity, with no cables, radius, per-tower assignment or transmission links.
2. Empty space separates grids. Reuse WORLD/GRID's cached terrain facts but define **conducting terrain eligibility** carefully: some tiles are buildable but block enemies (Foundations), while empty space is passable but unbuildable. Do not simply use enemy pathability as conduction.
3. Players manually connect **a pair of Power Relays** placed on distinct islands. Their link joins both grids, transitively if there are further relay pairs. Destroying/unlinking a relay separates networks unless another connection remains.
4. All generators on a linked island contribute to the **merged** supply. An isolated Solar Generator remains local because its island is **not linked**, not because of a separate local/grid toggle.

## Buildings and usage
- [Solar Generator](../TOWERS/Economy/SolarGenerator/README.md): cheap, modest, isolated outposts.
- [Industrial Generator](../TOWERS/Economy/IndustrialGenerator/README.md): standard initial/main source in most maps.
- [Fusion Reactor](../TOWERS/Economy/FusionReactor/README.md): very large, expensive endgame output.
- [Power Relay](../TOWERS/Utility/PowerRelay/README.md): manual inter-island link.
- [Consumer capability](../TOWERS/Shared/Power/README.md): optional baseline IdlePowerDraw/ActivePowerDraw in non-level tower type config, with research changing effective draw later.
- **Power Priority:** each placed electricity consumer has an Inspector slider 0–100 (default 50). When output is insufficient, give full power to higher-priority buildings before lower ones (with stable tie ordering and brief anti-flicker protection). Production may stop to preserve defensive towers.

## Implementation constraints
Keep one authoritative manager; cache terrain-region IDs and a small relay graph, refresh connectivity only on actual terrain/link changes, and update generation/demand from building/state events or budgeted processing. Do not scan every tile, node or tower on every frame. No compulsory wires, batteries, power losses, fuel-delivery network or complexity beyond this first design.

Save only authored identifiers/placed per-instance inputs such as priorities and relay pairings; rebuild derived grid membership and output on load. Test island detection, diagonal joins, generator merges/splits, shortages, destruction, and frame cost at large enemy/tower counts.

**Future design only. Tower Defense remains fully powered until the feature is deliberately implemented.**
