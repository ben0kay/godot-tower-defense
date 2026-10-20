## NP2 tactical narrator sample (2026-10-20; source updated / runtime QA pending)

Level020 now authors presentation **only** on waves 1,4,6,7, using optional `Presentation(...)` in its existing `WaveSetSource`. Introductory first contact, transporter warning, heavy fleet warning and final assault each supply a `TACTICAL COMMAND` typewriter message and a reserved voice cue ID. Waves 2/3/5 show only standard NP1 banners. The shared narrator lives **top-right below the Wave/Start controls**, stacked below the centred alert on narrower displays. **All seven waves still spawn the same enemy counts (52/68/80/72/72/60/58), IDs, shields, weights and timing.** Voice playback is not implemented. See [wave presentations](../Waves/Presentation/README.md) and [narrator UI](../../UI/Narration/README.md).

---

## Planned wave-start narration and banners (2026-10-20; not implemented)

See [wave presentation Codex handoff](../Waves/Presentation/README.md). The intended demonstration adds short tactical advisor messages to Level020 waves **1, 4, 6 and 7**, including special transporter/heavy/final alerts. Waves **2, 3 and 5** get the default numeric wave banner without narration. **Existing seven-wave batches, enemy counts, spawn behaviour and five portals must remain unchanged.** This is a plan only; current gameplay has no automatically triggered narration.

---

## Level020 setup-error repair — 2026-10-20 UTC

The seven-wave content update had changed Level020Waves back to a static class, breaking the serialized LevelConfig.WaveSource contract from the shared-level pass. Restored [GlobalClass] Resource inheritance (WaveSetSource) and Build() → Create(); all seven wave definitions, map and settings are retained. Recipes assigned in LevelConfig must remain resource classes; preserve the wrapper when editing batches. Added TESTS/Integration/Level020WaveSetupChecks.tscn to load the actual saved resource and launcher, assert seven Ready waves and successfully start wave1. C# build (zero warnings/errors) and this headless regression passed.

---

# Level020 — The Five Gates

A **100 × 80** painted-grid battlefield at **64 px/cell = 6,400 × 5,120 px**, deliberately larger than a 1920×1080 viewport. Normal camera pan/zoom applies. This is a genuine editable `TileMapLayer` map, not a runtime procedural generator.

## Layout

- **Western open staging ground (x=1..43):** predominantly buildable dirt/open-metal terrain, with five spatially separated entry portals at cells **(8,10), (13,24), (7,38), (16,53), (9,69)**. They are active via `Level020Config.tres`; random wave spawning distributes ships among usable entrances.
- **Eastern defensive labyrinth (x≈44..98):** maze walls are *raised Foundation* tiles: **buildable but enemy-impassable**. Walkable corridors use separate Passable-ground tiles. These two categories intentionally do not overlap. The outside edge is a non-buildable hard wall so enemies cannot shortcut around the maze.
- **Routes:** wide **three-cell** horizontal/vertical avenues intermingle with **single-cell** passages, gaps, bends, mid-maze connectors and pockets. Several east–west approaches and north–south cross-connections create loops, not a single forced route. With no player towers, all five portals connect to all four Citadel approach markers through the western field/maze network.
- **Citadel:** eastern courtyard; upper-left footprint root at grid **(90,39)** (a 3×3 reserved/blocked footprint), surrounded by walkable cells, including all four shared approach positions.
- **Art:** existing shared sci-fi terrain tiles and star background; ground and raised Foundation tiles may be repainted in Godot without a generator. Stage-specific custom background art can be added later.

## Gameplay, waves and editing

- `Level020Config.tres`: **1,700 starting Credits**, **1,200 Citadel HP**, five active portals, `WaveSetId="level020"`. Change this resource to rebalance.
- `Level020Waves.cs`: seven escalating authored waves (small swarms → escorted carriers → large armoured ships). All five portals remain eligible for unpinned spawns; all 13 registered enemy types feature somewhere in the set. These are editable encounters, not a campaign progression/unlock/save system.
- `Level020.tscn`: edit **Terrain/Passable**, **Terrain/Impassable**, **Terrain/Interactive** (Foundation walls), **Terrain/Decorations**, **Portals** and **Objectives/Citadel** in the 2D editor.
- `res://MainLevel020.tscn` and `WORLD/WorldLevel020.tscn` are deliberately small standalone launch variants for this prototype; Level001's `Main.tscn` and `WORLD/World.tscn` are unmodified. Campaign → **PLAY LEVEL 020** launches the new battlefield. `F6` on `MainLevel020.tscn` bypasses menu/profile. A shared level-loader can later replace these temporary launch variants when many levels exist.
- F2 continues to cycle the same global enemy catalog; normal waves spawn via the wave's stable enemy IDs.

## Seven-wave encounter design (2026-10-19)

| Wave | Scheduled enemies | Encounter focus |
| --- | ---: | --- |
| 1 | 52 | Standard Drones and Swift Interceptors, a couple of Racers |
| 2 | 68 | Faster small swarms, introducing Reinforced Drones |
| 3 | 80 | Peak light swarm with some Bulwarks and Shield Generators |
| 4 | 72 | Escort groups, shielded frigates, **first regular Transporters** |
| 5 | 72 | Convoy pressure: regular and **Heavy Transporters**, Heavy Bulwark escorts |
| 6 | 60 | Shift to Big mk.1, Elite, Heavy Bulwarks and shield frigates |
| 7 | 58 | Heavy armoured finale with **two shielded Cruisers**, Big mk.1 and Heavy Transporters |
| **Total** | **462** | Additional DeathSpawn children from killed carriers are not included |

The `Batch(count, interval, after, ...)` entries are deliberate total spawn counts (not counts per enemy type or per portal); weights use the existing deterministic largest-remainder/shuffle system. No wave forces a particular portal. Shields are **opt-in** on Shield Frigates and some late Elites/Big ships/Cruisers. DeathSpawn effects on killed Transporters/Heavy Transporters generate their usual child ships, making some battles larger than these scheduled totals. Wave starts remain manual, and each wave must fully clear before the next begins. Credits/HP, wave scheduler, map and navigation are unchanged.

These are initial playtest numbers for a five-portal late-game map; balancing against full player tower loadouts and the 3,000-HP Cruiser remains to be tested in Godot. `TESTS/Integration/Level020Checks.cs` validates count/progression, catalogue references, unrestricted portals and two shielded final Cruisers.

## Navigation / authoring safeguards

- Avoid placing gameplay obstacles across every connection to the Citadel. Normal tower-placement connectivity validation prevents the player from fully sealing active routes.
- The route surface is wide enough for currently supported ship clearance radii (including Enemy004's **22px** medium hull). **Future giant ships larger than half a tile radius require a separate large-unit navigation design**.
- `WorldGrid` treats empty/unpainted space as passable but unbuildable; the deliberately painted border prevents off-maze shortcuts. `Foundation` is buildable but blocks enemies, and does **not** trigger lava damage.
- The map is drawn using normal Godot TileMapLayer serialized cells. No runtime tile scans beyond the game's existing startup cache; no per-tile nodes or new pathfinding systems.
