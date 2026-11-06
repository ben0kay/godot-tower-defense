# Solar Generator — PW6-A

**Source Implemented / Awaiting Owner Validation (2026-11-06).** Available in **Ctrl → Economy** as `solar_generator` with exactly **1×1** cells. Solar is a compact outpost power source. Its dark chassis holds gold photovoltaic panels and a cyan core; no daylight cycle is simulated. This scene reuses `PowerGenerator.cs`/`PowerGeneratorConfig.cs`, existing construction visuals, the single PowerGridManager and PW1–PW5 connected-network accounting.

| Provisional config | Default |
| --- | ---: |
| Output | 75 energy/sec |
| Cost | 125 Credits |
| Construction | 3 seconds |
| Hull HP | 700 |
| Footprint | **1×1 cells** |

The `DefaultSolarGeneratorConfig.tres` owns output, cost, build time and health; `SolarGeneratorDefinition.tres` owns its footprint and separate catalog ID. The event-driven presenter switches native 0%, 35%, 70% artwork and the full sprite on work completion. All footprint cells must be on the same eligible conducting island. Output begins only when fully constructed, operational and alive; disable/EMP/death/removal withdraw output automatically. Existing transmitters can connect remote islands, while relays can boost charging efficiency but never create source energy. The generator has no per-frame processing, fuel, artificial internal capacitor, additional save fields or purchased upgrades. Save/Load reconstructs its existing producer registration once.

**Owner check:** import the SVGs/build in Godot .NET; place a 1×1 building, confirm 125-Credit charge and construction art, then F4 source supply rising by 75/sec only on completion. Check invalid footprint/insulating terrain rejection, EMP/disable/removal, and a fresh between-wave Save/Load. Art/runtime checks remain unverified here.
