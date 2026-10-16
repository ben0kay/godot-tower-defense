# Ammunition gameplay-loop verification — 2026-10-16

Run after building:

```sh
godot --headless --path . res://TESTS/Economy/AmmunitionLoopChecks.tscn
```

## Focused coverage

The runner first executes the original AmmoFoundationChecks, independently of unrelated map assertions. Tests are split into small focused files:

- `AmmoLoopFixture`: production scenes, paid button/placement helpers, controlled explicit ticks and conservation accounting.
- `WeaponAmmunitionChecks`: optional absolute per-level stats/independence, full/empty creation, shrink/growth policy, real Battle/Blaster launches and Shockwave emissions, empty shutdown/physical refill, paid Laser time at30/60/120Hz, no free beam tail, rank/paid-upgrade stock persistence and authoritative ammo upgrade preview.
- `DeliverySafetyChecks`: multiple factories/depots, **factory full-shipment gating (no 900-round early flights and no partial 1,000-round delivery into a depot with <1,000 free space)**, reserved incoming capacity, full depot, **unchanged partial depot→tower shipments**, scarce stock, distinct outstanding requests, retries, absent/full home, source/destination/drone removal, capacity change/surplus return, failed purchase and active-flight unload.
- `AmmunitionLoopChecks`: purchases Factory200 / Depot150 / Battle200 through real HUD buttons, placement and wallet. Fills/pauses factory with no depot; dispatches a moving visible courier and verifies no stock at destination before arrival; verifies production resume and return-slot release. Fires900 actual Battle projectile launches to reach100/1000, dispatches one depot shipment, verifies arrival-only refill and event-driven selected tower/factory/depot Inspector status. Stock + cargo + consumed + losses/discards matches initial stock + production.

A further automatic real-physics fixture enables the actual production/dispatch/drone callbacks (no explicit per-drone test ticks), pauses/resumes the real tree, and verifies both delivery legs and conservation. It runs with a one-check scheduling budget to verify fairness persists across intervals. Normal/default balance is not rewritten by any fixture.

## 2026-10-16 balance adjustment (new code and test expectations; execution still pending)

- Factory defaults: **100 rounds / 2s**, **1,000 buffer**, **1,000 shipment threshold**, **1,000 factory-drone payload**. The factory waits for a full 1,000 before departure; the depot must have 1,000 unreserved free slots. No partial factory shipments.
- Depot default capacity: **10,000 rounds**. Its depot→tower drones retain their original **250-round payload** and can still deliver partial stock.
- The gameplay and safety tests have been updated for these new default values, and add regression checks for no premature or space-constrained partial factory flights. **These changed tests have not yet been executed by this update** (Godot/.NET not installed in the current environment). The historical results below apply to the prior implementation, not automatically to this new adjustment.

## Actual results (original implementation, before this balance adjustment)

- Direct Roslyn compilation with installed Godot4.7.2 assemblies/source generators: **passed**, no compiler diagnostics.
- Original foundation helpers: **passed first**, before gameplay integration.
- AmmunitionLoopChecks: **exit0**, including real physics and one-check scheduling fairness.
- MenuTowerFlowChecks: **exit0**, real menus, construction and paid Battle upgrade.
- PauseControlsChecks: **exit0**, scoped keyboard/pause transitions on both levels.
- RoutePreviewChecks: **exit0**, both maps and live-enemy isolation.
- CheckpointChecks: **exit0**, required regions, chains/branching and500-enemy scheduling.
- CorridorNavigationChecks: **exit0**, movement permissions and all straight/winding100/500-enemy cases.
- PlacementPerformanceChecks: **exit0**,100/500-enemy scenarios.
- Unmodified EnemyIntegrationChecks: **exit1 at the existing Level002 painted-tile-count assertion**. No map tiles or that assertion were changed. A diagnostic build skipping only Level002Checks.Run passed the remaining31 integration checks, including existing weapon, rank/XP, shield/hull, instant AoE, swept Shockwave, terrain, economy and navigation checks. The original source was restored before the final production compilation. This is not an unmodified full-suite pass.
- New SVG assets imported successfully and actual building/drone scenes instantiate. The headless editor still reports missing .NET SDK8.0.31/Microsoft.Build. Standard `dotnet build --no-restore` is blocked by the container's missing NuGet.Protocol dependency. Neither standard SDK nor editor build success is claimed.

Existing DefaultTowerCatalog fallback warnings remain. No campaign/profile writes occur.

## Performance evidence and limits

No enemy-query or route-engine code changed. Demand updates are per-inventory events; dispatch performs at most8 alternating endpoint checks every0.2s by default, with round-robin continuity even at a one-check budget. Each selection scans registered depots, not every enemy. Only active drones have physics callbacks; idle slots have no nodes.

Existing500-enemy diagnostic navigation fixture: path-frame p95 **2.46ms**, max **2.54ms**, movement+clearance max **4.12ms**. Placement500 fixture: validation max **0.68ms**, grid max **0.08ms**, next physics frame max **2.92ms**. These are headless same-environment samples with concurrent suites, not an optimization speedup or rendered FPS guarantee.

Graphical desktop appearance has not been manually inspected. The code supplies vector building icons and visible moving courier nodes; headless tests verify motion, arrival transactions and lifecycle. A desktop playtest remains appropriate for balance, visual scale and travel-time feel. Defaults beyond Battle1000/1 and the10% threshold are provisional.

## Quick desktop reproduction

Pull/build, launch either battlefield, press Ctrl and purchase Ammunition Factory, Ammunition Depot and Battle Tower (550 total Credits). Place the buildings nearby to see flights quickly. The factory produces automatically and the depot starts empty. Select buildings/towers to inspect stock, slots and resupply.

Battle starts with1000 rounds; for a faster preview, edit its DefaultBattleTowerConfig.tres **Starting Ammunition → Initial Ammo Fraction** to0.1 before placement. It will start at100 and request supply immediately; every actual projectile launch still costs1. Cargo moves only on physical arrival. Pause stops production and drones. No magazines, reloads, power, inputs, research, saving or combat drones are included.
