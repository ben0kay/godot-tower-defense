## PW2 implementation update — 2026-11-04

`PowerGridManager.Accounting.cs` adds shared registration and cached per-island totals. `CitadelSession.Power.cs` owns source lifetime/config subscriptions after health initialization; `CitadelPowerConfig`/`DefaultCitadelPower.tres` supply a configurable provisional Output=100. F4 reads global diagnostic sums and local totals. Sources persist as derived registrations, not a saved energy balance. No frame callbacks or power gating. See [shared guide](Shared/README.md) for exact API, lifecycle and validation limits. The PW0 audit below remains design context; PW1-only statements describe that original phase, not a reason to reimplement it.

---

# Electricity networks — PW0 audit and PW1 foundation

**2026-11-04 · dev · PW0 Complete; PW1 Source Implemented (owner reports working); PW2 Source Implemented / Awaiting Owner Validation.**
PW0/PW1 and the next PW2 pass are now source implemented. Existing towers remain fully operational.
PW2 adds Citadel output and shared supply/requested-demand accounting; shortages, generator buildings,
consumers and relays remain later phases.
Follow `NOTES/CodePreferences.md`: brief source checks, one basic build if readily available;
extended gameplay/visual/performance acceptance stays with the owner.

## Agreed gameplay direction

- Each edge-connected eligible terrain landmass is one local power grid. Diagonals never join.
- The **Citadel supplies configurable basic generation** so the player can establish an opening
  defense before adding generators. Output is continuous while the objective is available/alive,
  not limited to active waves like its independent Credits production. The PW2 default is 100 provisional power; configure DefaultCitadelPower.tres and tune against future opening demand.
- Solar, Industrial and Fusion are separate build choices sharing generator logic, not successive
  purchased levels of one building. Each may have its own purchased upgrades later.
- No compulsory wires, transmission radii, batteries, loss simulation or fuel logistics initially.
- Manually paired relays join islands transitively; an alternate route can preserve a connection.
- Independent per-instance priorities (0–100, default 50) decide full on/off service during shortages.
  Metadata/priority never modifies cached combat stats or the Credits wallet.

## PW0 source audit and integration decisions

| Existing owner | Finding and integration boundary |
| --- | --- |
| `WORLD/GRID/WorldGrid.cs` | Already caches terrain independently of occupancy and emits `TerrainRebuilt`. Add an independent conductivity cache/revision; never derive electricity from `BlocksEnemies` or `CanBuild`, and never trigger A* for power-only changes. |
| `LEVELS/Runtime/WorldMapLoader.cs` + `WORLD/World.tscn` | Loader mounts `Level` first. Grid initializes before the one `PowerNetworks` node, which reads initial cached data and subscribes to explicit terrain rebuilds. All catalog battlefields share this scene; raw map-only F6 does not run gameplay services. |
| `WORLD/TERRAIN/TileSets/` | Append optional Boolean `conducts_power` without renumbering existing fields, sources or atlas cells. All current painted gameplay terrain conducts except Lava. Decorations and empty space are excluded. Existing missing-field custom palettes default to painted land; custom insulating tiles should explicitly author false. |
| `TOWERS/Shared/Registry/TowerRegistry.cs` + `TowerBase` | Existing membership, destruction, work and spatial events are future registration inputs. PW1 does not register individual buildings, scan all towers, add per-tower nodes or change placement rules. |
| `WORLD/OBJECTIVES/Citadel/` | Future starter-generation settings/registration live here and contribute through the same producer API as generators. Support every authored Citadel and use existing stable objective IDs. Health initialization occurs in CitadelSession after World startup; register only after that initialization, remove output on death/removal, unsubscribe on teardown. |
| `TOWERS/Shared/Conditions/` | `TowerDisableReason.NoPower`, `ApplyDisable` and `RemoveDisable(handle)` already exist. PW4 owns one condition-based handle per affected consumer, removes only that handle, and preserves EMP/heat/upgrading. No second disable system. Audit producer/consumer work paths for `CanOperate` before enabling shortages. |
| `SYSTEMS/Saving/` + feature-local `*.Saving.cs` | No PW1 save schema additions: topology is derived from authored terrain. Future priorities/relay peer GUIDs are durable inputs. Rebuild registrations/allocation before unpausing; treat manager-owned NoPower as derived state rather than restoring a stale handle or a native instance ID. Preserve original research/reward ownership. |
| `DEBUG/PerformanceOverlay.cs` | Add two read-only F4 rows: cached counts/rebuild timing, plus hovered cell/region size. Use the existing throttled refresh, no new process loop or full-map overlay. |

### Terrain and structure membership policy

WorldGrid reads Passable → Interactive → Impassable. The winning gameplay tile's `conducts_power`
value wins, independently of movement/build flags. Therefore a Foundation or rock can conduct
while blocking enemies; Lava over floor is insulating, but an Impassable wall over Lava wins.
Portal/Citadel reservation and tower occupancy never create or remove conducting terrain. Art,
terrain-edge masks and path diversity do not affect electricity.

PW1 exposes `RegionAt(cell)` and `RegionForFootprint(anchor,size)` only; live source/consumer
membership comes later. The future building rule is that **every footprint cell must belong to
one conducting island**. Buildings must not create bridges over empty/nonconducting cells.
PW1 adds no placement rejection. A future producer with no valid island contributes no power
and needs a clear diagnostic. Tower positions are footprint centers; authored Citadel/Portal
roots anchor the upper-left footprint cell, so future registration must use the correct conversion.

Regions are numbered deterministically from top-left seed order, starting at 1; 0 means no island.
IDs can change after edits/merges/splits: cache them only alongside the topology revision and never
serialize them. In PW1, island and electrical-network identities coincide. PW5 can combine these
local islands through a small relay graph without replacing or duplicating terrain detection.
Map dimensions are startup configuration; changing them requires a level restart, as before.

### Demand/shortage decisions for PW2–PW4

- Baseline IdlePowerDraw and ActivePowerDraw belong to each type's non-level config; active draw
  means **total operating demand**, not idle plus active. Purchased levels do not implicitly alter
  draw; effective research adjustments must remain separate from immutable base settings.
- Define an event-driven requested activity/reservation state before enforcing power. An unpowered
  weapon must not alternate idle/active demand merely because NoPower prevents its next shot.
  The first baseline may reserve active demand for ready combat consumers while a wave is active;
  decide actual per-capability wake/idle rules before PW4, rather than polling every shot/tower.
- Allocate full requested service by descending priority and stable registration/save identity ties.
  Keep shortage state distinct from requested activity, coalesce source/demand events, and introduce
  bounded recovery hysteresis if needed. Exact recovery delay and low-priority fit policy must be
  decided before PW4; no per-frame sort, recursive allocation callbacks or mass oscillation.
- No circular startup dependency: Citadel/generators do not require their own grid power to produce;
  relay connectivity is passive and does not disappear merely because its network has a shortage.
- Construction and paid upgrades retain current clocks initially. Whether unfinished/busy/EMP/heat
  states draw idle or zero power, and which utility/production buildings consume, needs explicit
  per-capability agreement before consumers are switched on. Do not silently suspend builder work.

## Performance and lifecycle contract

PW1 performs one iterative O(width × height) flood fill at startup and only after a changed
conductivity snapshot. A private queue and labels reuse two int arrays; no recursion, AStar,
node-per-tile, node-per-island, per-enemy or per-tower processing. At 500×500, those arrays consume
about 2 MB combined; WorldGrid adds a Boolean mask and a temporary comparison copy during explicit
terrain rebuilds. Island-size storage is one integer per island. These are memory estimates, not
measured frame timings.

WorldGrid's existing explicit rebuild still reads terrain; unchanged conductivity does not cause
another island flood fill. Occupancy changes never request one. The manager has processing disabled
and unsubscribes on exit. F4 reads O(1) cached counts and one cell lookup at its existing refresh
interval; hidden F4 stops that sampling. `LastRebuildMilliseconds` measures the topology pass only,
not the entire WorldGrid rebuild or GPU work. Full map rescans on an explicit edit can still hitch
at large sizes; incremental/chunk work should follow evidence, not speculative machinery now.

Later allocation must be event-driven/coalesced and proportional to affected networks/consumers;
future relay changes rebuild only network unions, not terrain islands. Never scan enemies for power.

## Phases and acceptance

| Phase | Scope / owner | Status / owner checks |
| --- | --- | --- |
| **PW0** | This audit, terrain/membership decisions, folder ownership and staged contracts | Complete; remaining balancing/UI decisions above are explicitly deferred. |
| **PW1** | `Shared/PowerIslandMap.cs`, `PowerGridManager.cs`, WorldGrid conductivity, appended palette metadata, shared scene attachment and F4 diagnostics | Source implemented; check separated/diagonal land, bridges, erasure, Foundation/Lava/wall precedence, unchanged rebuilds, occupancy and level teardown. No generation or shutdown. |
| **PW2** | Shared producer/demand accounting + configurable Citadel baseline registration | Source implemented / owner validation pending. DefaultCitadelPower.tres Output=100 provisional; event-driven source lifetime, per-island cached totals and F4 accounting. No actual consumers/outages yet. Check multiple Citadels, isolation, death/removal and a new Save/Load. |
| **PW3** | Shared generator/config in `Shared/`, Industrial scene/config/art in its type folder | Planned. Add one catalog build choice; verify work lifecycle, destruction and save restoration without free stock/power registration duplicates. |
| **PW4** | Consumer capability in `TOWERS/Shared/Power/`, selected Inspector, shared NoPower gate | Planned. Approve demand transitions/balance first; test priorities, deterministic ties, anti-flicker, EMP/heat overlap, production and save/load. |
| **PW5** | `TOWERS/Utility/PowerRelay/`, manual peer selection, small network graph | Planned. One peer per relay initially; joins/splits/alternate links, invalid targets, removal and durable peer IDs. Range/UI choices remain open. |
| **PW6** | Solar/Fusion variants, shared status feedback and tuning | Planned. Distinct IDs/configs, one producer implementation; readable capacity/shortage feedback and measured desktop cost. |

Each phase includes its relevant save integration, feature documentation and brief source/build checks.
Do not implement subsequent phases from this table without authorization. Named save slots remain a
separate extension. The current strict dependency-byte fingerprint can reject older checkpoints after
source/resource changes; this pass preserves that guard and does not promise old-checkpoint compatibility.

## Validation record and owner handoff

Focused source/resource checks only in this environment; no .NET/Godot executable is available.
No runtime pass, FPS improvement or visual acceptance is claimed. See `TESTS/Power/README.md`
for the focused fixture and a short in-editor check. Existing authored missions, tiles' previous
gameplay flags, waves, costs, combat and tower operation are preserved.
