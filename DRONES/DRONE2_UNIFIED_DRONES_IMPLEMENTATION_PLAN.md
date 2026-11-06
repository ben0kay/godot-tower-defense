# DRONE2 — Unified Builder/Ammo Drones and Visual Polish

**Repository:** `ben0kay/godot-tower-defense`  
**Branch:** `dev` only  
**Status:** **PLANNED / Approved for Codex implementation** (2026-11-06). **No gameplay implementation in this plan commit.**

## Objective and design decision

Replace the current ammunition courier's separate look and straight-line flight with the **same physical drone design and organic flight style** already used by Citadel construction/upgrade drones. Improve **both** roles with subtle dual thruster ignition/propulsion and speed-responsive cyan contrails. Add a compact **one-panel-per-construction-site** telemetry readout and a brief **SITE LOCKED** acquisition cue before the existing construction-beam deployment.

**Unify the visual model, flight mathematics and presentation; do NOT unify construction and ammunition gameplay dispatchers.** Construction remains owned by `ConstructionDroneManager`; ammo stock, reservations, assignments and delivery callbacks stay with `AmmunitionLogistics`. No new general-purpose combat drone framework or independent job clock.

## 0. Read first and verify the live source

1. `AGENTS.md` if present, `NOTES/CodePreferences.md`, root `README.md`.
2. `DRONES/README.md` and `DRONES/Transport/README.md`.
3. `TOWERS/ConstructionDrones/README.md`, `TOWERS/ConstructionDrones/Visuals/README.md` if present, and `TOWERS/Shared/Construction/Visuals/README.md`.
4. `TOWERS/Economy/Ammunition/README.md`, `TOWERS/Economy/Ammunition/Shared/Logistics/README.md`, and `SYSTEMS/Saving/README.md`.
5. Actual owners: `TOWERS/ConstructionDrones/{ConstructionDrone.cs,ConstructionDrone.tscn,ConstructionDroneMotion.cs,ConstructionDroneManager.cs,ConstructionDroneMasterConfig.cs,DefaultConstructionDroneConfig.tres}`, relevant `Visuals/` and manager saving files; `DRONES/Transport/{TransportDrone.cs,TransportDrone.tscn}`; `TOWERS/Economy/Ammunition/Shared/Logistics/AmmunitionLogistics.cs`, `AmmoDeliveryJob`; existing `ConstructionSiteHolograms` and `ConstructionWorkManager`.

**Verified design baseline (2026-11-06):** builders use a shared manager, pooled artwork, cubic curved flight, smoothstep acceleration/braking and hover/orbit movement; ammo couriers currently use `TransportDrone._PhysicsProcess()` with direct `MoveToward` movement and procedural polygon art. The same transport implementation carries **Factory → Depot** and **Depot → Tower** shipments. Existing builder work holds/beam deployment, ammo cargo conservation, and S2/S3 checkpoint paths must remain authoritative. Older notes may describe pre-save or pre-upgrade behavior; trust current source.

## DRONE2-A — Replace ammunition drone presentation and motion

- Migrate both ammo courier routes to the **same construction-drone SVG/body silhouette**, orientation and coherent visual presenter. Loaded ammo couriers may retain a **small amber cargo light**; empty returns may be dim cyan. Do not use a separate geometric courier model.
- Reuse/refactor `ConstructionDroneMotion`'s current cubic flight and eased acceleration/braking for **both jobs**. Extract a narrow, reusable flight primitive if necessary; avoid copying the whole builder motion class into transport. Keep builder-specific orbit, construction beam, site target/hold and checkpoint handling separate.
- An ammo drone follows organic outbound motion, arrives exactly once, transfers cargo **only through the existing `AmmunitionLogistics` callback**, then takes a separately eased return journey to its originating Factory/Depot. It does not construct, orbit a work site or emit a construction beam.
- Preserve all logistics behavior: factory stock accumulation and **1,000-round full-batch** deliveries to depots, flexible Depot→Tower refills, storage/payload limits, demand threshold, source drone slots occupied through return, destination reservations, partial/unload rules, abort/loss accounting, pause and checkpoint restore.
- Retain low-overhead, active-only movement with no new pathfinding or per-frame global scans. The existing build manager and logistics manager keep **independent concurrency caps**; do not impose one global cap across roles.
- Once all code/scene/test/save references are migrated, safely remove the **obsolete** `DRONES/Transport/TransportDrone.cs/.tscn` if no longer referenced. Do not remove cargo or transport job abstractions that still own legitimate logic. Preserve stable scene and serialized bindings where required, using a compatibility step only if actual saved content needs it.
- IMPORTANT: do not inadvertently change construction drone speed (currently 280 px/s by its own config) or authored ammo courier speeds. Translate each service's authored maximum speed into the shared motion API; don't silently rebalance transportation time beyond the expected acceleration/curved-flight differences.

## DRONE2-B — Dual thruster ignition and propulsion

Add two small rear-mounted cyan-white thrusters to the **shared body presentation**:

| Phase | Required effect |
| --- | --- |
| Outbound launch | Two tiny flares ignite and expand briefly, then settle into steady propulsion |
| Acceleration / cruising | Small cyan engine glow, strength tied to speed |
| Braking | Glow softens |
| Builder hovering / orbit resting | Very subtle glow only; no large flare |
| Return departure | Repeat ignition sequence |
| Parked / hidden / removed | No effect, no update |

- Ignition duration: approximately **0.20–0.35 s** (playtest provisional).
- Mount to actual artwork orientation: rear thrusters rotate with the **sprite**, not the world or ground-target construction beam.
- Use retained lightweight geometry/presenter driven by the **existing active movement update**; no additional continuously running timer, one-particle-node-per-engine scheme or large effect system.
- Export enabled, flare timing, core/halo tint, intensity and scale under an appropriate shared drone visual config, respecting the existing config ownership.

## DRONE2-C — Thin cyan speed-responsive contrail

- One restrained, semi-transparent, roughly **1–2 px** ion trail, about **15–35 px** at cruising speed. Brighter near the engine and fading at its distant tip. Clearly much thinner than the site construction beam.
- Extend gradually during acceleration, shorten during braking and vanish at builder hovering/rest, parked status and zero motion. Restart naturally on each outbound/return leg.
- Follow recent *actual flight path* for curves; do not draw a fixed line arbitrarily toward the destination. Use a small, bounded retained trail-history buffer or similarly cheap renderer, with strict point/length limits and **no per-frame managed allocations**.
- Keep effects cosmetic only; toggling them off must not alter flight completion or work/delivery callbacks.
- Provide Inspector controls: enabled, colour, alpha, line width, min/max trail length and optional speed response.

## DRONE2-D — One floating construction telemetry panel per site

Introduce **one compact HUD-style readout per active construction/upgrade site**, NOT a Control/Label/node per drone. Prefer the existing pooled/shared `ConstructionSiteHolograms` renderer and `ConstructionWorkManager` events, including its bounded visual refresh and footprint geometry. Position the readout just outside the tower footprint, preferably at the upper-right, and respect rectangular 1×1/2×2/3×2/larger structures.

| Site state | Display |
| --- | --- |
| Queued / drone in flight / awaiting service | `DRONE INBOUND` |
| Immediately after builder arrival and during acquisition | `SITE LOCKED` (brief) |
| Actual timed initial construction | `CONSTRUCTING 67%` (authoritative percentage) |
| Actual timed paid upgrade | `UPGRADING 67%` |
| Successful work completion | `SYSTEM ONLINE` (briefly, ~0.8–1.2 s, then fade) |

- If builder service is disabled/unavailable, derive an accurate **construction/upgrading** label from the existing work clock; never permanently show `DRONE INBOUND` for a job that cannot receive a builder.
- Read progress from the **existing authoritative work clock**; no parallel timer, artificial completion or extra save field just for the label.
- Small cyan/white font, subtle dark translucent backing or thin holographic bracket, outside the footprint; avoid obscuring the selected Inspector, tower artwork or beam.
- Coalesce updates through existing work/progress events or bounded shared render cadence, not per-site per-frame controls. Cap temporary completion readouts and renderer entries.
- Show `SYSTEM ONLINE` **only** after successful completion, never cancellation/destruction; clear status on removal/unload; restore the appropriate state from existing saved work/assignment states.
- Allow designers to tune font/colour/opacity/offset, enabled state and brief completion duration through the existing shared site visual config or a focused extension.

## DRONE2-E — Site arrival acquisition cue (before existing beam deploy)

Keep the builder's existing ~**0.25 s deployment**; insert a brief, visual-only **~0.15–0.25 s** `SITE LOCKED` transition immediately before it.

Exact expected sequence:

1. Builder reaches its hover/target pose.
2. Existing footprint brackets brighten once; a thin converging cross moves toward the footprint center.
3. One site panel briefly displays `SITE LOCKED`.
4. Existing construction beam starts its normal deployment.
5. Existing held construction/upgrade work is released **only after the original deployment gate has correctly completed**.
6. Builder continues normal hover/orbit while real work progresses, then flies home.

Use the existing site-hologram brackets and geometry wherever possible: **no second permanent scanner**, no new construction job clock, no layout changes to construction/upgrade art. Cosmetic events must never award construction progress or trigger callbacks twice.

Consider a small clearly documented addition to the existing deployment state machine. Preserve backward compatibility for paused or restored flights and work holds (including older saved phases); accurately handle target loss/cancel during acquisition. Ammo deliveries do **not** show this site-acquisition/beam sequence.

## DRONE2-F — Performance, lifecycle and regression boundaries

- Reuse artwork/flight math/visual presentation; **do not merge the two business managers**.
- Idle/pool entries sleep; no per-frame work for hidden visuals or completed sites.
- Bound contrail history and site readouts; avoid extra `_Process`/`_PhysicsProcess` loops, scene scans, pathfinding, unmanaged lifetime callbacks or avoidable allocations.
- Respect Godot pause, out-of-tree removal, flying/returning teardown, missing source/destination and scene transitions.
- All ammo reservations, loss/cargo attribution, builder work holds, paid upgrades, initial construction, construction progress and S2/S3 checkpoint restore retain exactly one authoritative owner.
- Preserve existing combat/waves, tower stats, resources, research, power and economy balances. Do not build player combat drones as part of this plan.

## DRONE2-G — Minimal acceptance checks

Codex: brief source/diff sanity check and **one `dotnet build` if practical**. Do **not** run a large suite, prolonged Godot simulations or FPS benchmarks; owner handles full visual/performance acceptance.

Manual owner smoke checks:

1. Paid construction: Citadel launch → curved flight → thruster/contrail → SITE LOCKED → beam → correct held-work release → hovering telemetry percentage → SYSTEM ONLINE → return ignition → home.
2. Paid tower upgrade: same process, correct `UPGRADING N%`, no reset of purchased-tier artwork.
3. Ammo Factory→Depot and Depot→Tower: same drone body, curved flight, cargo indicator, correctly accounted arrival/return, no construction beam/telemetry.
4. Hovering has tiny propulsion glow, **no** contrail; braking shortens contrail and return launch reignites thrusters.
5. Several concurrent build sites and ammo deliveries: independent scheduling/caps, no duplicate callbacks or overlapping site labels.
6. Pause, cancel/sell/destroy, depleted/removed factory/depot, 1×1/2×2/3×2 footprints, saved outbound/building/returning states and checkpoint reload.
7. Toggle visual features off: work and stock logistics remain correct.

## Implementation order / delivery

1. **DRONE2-A** unified artwork/organic motion for ammo and builders, preserving each owner's responsibilities.
2. **DRONE2-B/C** shared thruster and contrail presentation; use the live movement phase and actual speed.
3. **DRONE2-D/E** shared construction-site telemetry and short acquisition cue.
4. **DRONE2-F/G** focus on lifecycle, save compatibility, lightweight checks, and docs.

Prefer **one focused implementation commit** (or smaller independent commits only if necessary for safe integration). Push to `dev`; do not touch `main`.

Update `DRONES/README.md`, `DRONES/Transport/README.md`, `TOWERS/ConstructionDrones/README.md`, relevant site/visual docs, root `README.md` and `NOTES/IMPLEMENTATION_BACKLOG.md` as behavior is implemented. Do not mark features implemented before source changes exist. Report changed files, commit SHA, checks performed and pending owner review.

**Acceptance goal:** construction and ammunition drones look and fly like the **same advanced drone type**, with two cyan thrusters and a narrow adaptive ion trail. Only construction jobs receive the site-acquisition cue, construction beam, and compact site telemetry. Existing ammo economics and construction scheduling are untouched.
