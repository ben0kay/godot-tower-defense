# Fusion Reactor — PW6-B

**Source Implemented / Awaiting Owner Validation (2026-11-06).** Available in **Ctrl → Economy** as `fusion_reactor` with exactly **4×4** cells. Fusion is a large endgame source. A charcoal metal housing, violet coils and cyan reactor core occupy a native 240×240px sprite, inset 8px from each side of the 4×4-cell placement area. This scene reuses `PowerGenerator.cs`/`PowerGeneratorConfig.cs`, existing construction visuals, the single PowerGridManager and PW1–PW5 connected-network accounting.

| Provisional config | Default |
| --- | ---: |
| Output | 900 energy/sec |
| Cost | 1200 Credits |
| Construction | 12 seconds |
| Hull HP | 2500 |
| Footprint | **4×4 cells** |

The `DefaultFusionReactorConfig.tres` owns output, cost, build time and health; `FusionReactorDefinition.tres` owns its footprint and separate catalog ID. The event-driven presenter switches native 0%, 35%, 70% artwork and the full sprite on work completion. All footprint cells must be on the same eligible conducting island. Output begins only when fully constructed, operational and alive; disable/EMP/death/removal withdraw output automatically. Existing transmitters can connect remote islands, while relays can boost charging efficiency but never create source energy. The generator has no per-frame processing, fuel, artificial internal capacitor, additional save fields or purchased upgrades. Save/Load reconstructs its existing producer registration once.

**Owner check:** import the SVGs/build in Godot .NET; place a 4×4 building, confirm 1200-Credit charge and construction art, then F4 source supply rising by 900/sec only on completion. Check invalid footprint/insulating terrain rejection, EMP/disable/removal, and a fresh between-wave Save/Load. Art/runtime checks remain unverified here.
