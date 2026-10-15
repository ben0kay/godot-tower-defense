# Economy / Power — Generators and shared electricity networks (FUTURE / NOT IMPLEMENTED)

## Building folders

- [SolarGenerator/](SolarGenerator/README.md) — compact low-output island generator.
- [IndustrialGenerator/](IndustrialGenerator/README.md) — standard starting generator for most levels.
- [FusionReactor/](FusionReactor/README.md) — expensive endgame generator.
- [Shared/](Shared/README.md) — one reusable generator implementation, grid/accounting rules and future PowerGridManager / PowerMasterConfig.

These are **different buildable types**, not purchased level tiers of a single generator. Individual building folders own authored configs/artwork; shared generator/grid logic belongs to Shared/. The per-tower *consumption* capability remains separate at [TOWERS/Shared/Power](../../Shared/Power/README.md). The Power Relay is kept in [Utility/PowerRelay](../../Utility/PowerRelay/README.md).

## Single authority
Reserve this Economy-owned folder for the future **PowerGridManager** and optional **PowerMasterConfig**. This system owns network membership, generation/demand and priority allocation, not generator artwork or weapon combat. Do not create those classes or change runtime gameplay until a dedicated pass is approved.

## Simple island-grid rules
1. An **edge-connected group of eligible terrain tiles is one power grid** (prefer four-directional neighbours, no diagonal-only joins). Buildings on that landmass automatically share electricity, with no cables, radius, per-tower assignment or transmission links.
2. Empty space separates grids. Reuse WORLD/GRID's cached terrain facts but define **conducting terrain eligibility** carefully: some tiles are buildable but block enemies (Foundations), while empty space is passable but unbuildable. Do not simply use enemy pathability as conduction.
3. Players manually connect **a pair of Power Relays** placed on distinct islands. Their link joins both grids, transitively if there are further relay pairs. Destroying/unlinking a relay separates networks unless another connection remains.
4. All generators on a linked island contribute to the **merged** supply. An isolated Solar Generator remains local because its island is **not linked**, not because of a separate local/grid toggle.

## Buildings and usage

All three generator types use one common implementation and documented home under [PowerGenerators](Shared/README.md); they remain distinct build choices.
- [Solar Generator](SolarGenerator/README.md): cheap, modest, isolated outposts.
- [Industrial Generator](IndustrialGenerator/README.md): standard initial/main source in most maps.
- [Fusion Reactor](FusionReactor/README.md): very large, expensive endgame output.
- [Power Relay](../../Utility/PowerRelay/README.md): manual inter-island link.
- [Consumer capability](../../Shared/Power/README.md): optional baseline IdlePowerDraw/ActivePowerDraw in non-level tower type config, with research changing effective draw later.
- **Power Priority:** each placed electricity consumer has an Inspector slider 0–100 (default 50). When output is insufficient, give full power to higher-priority buildings before lower ones (with stable tie ordering and brief anti-flicker protection). Production may stop to preserve defensive towers.

## Implementation constraints
Keep one authoritative manager; cache terrain-region IDs and a small relay graph, refresh connectivity only on actual terrain/link changes, and update generation/demand from building/state events or budgeted processing. Do not scan every tile, node or tower on every frame. No compulsory wires, batteries, power losses, fuel-delivery network or complexity beyond this first design.

Save only authored identifiers/placed per-instance inputs such as priorities and relay pairings; rebuild derived grid membership and output on load. Test island detection, diagonal joins, generator merges/splits, shortages, destruction, and frame cost at large enemy/tower counts.

**Future design only. Tower Defense remains fully powered until the feature is deliberately implemented.**
