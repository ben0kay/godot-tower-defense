## F2 batch spawner and centralized debug tools — 2026-10-21

F2 now toggles an enemy batch menu, not immediate catalog cycling. Pick a catalog enemy, amount1–10,000 and spawn gap0–60 seconds. Each accepted ship independently samples a valid registered portal uniformly through production source/clearance/checkpoint/Citadel rules. Debug batches use their own RNG and do not emit WaveEnemyCreated; authored wave accounting/randomness are unchanged. Zero gap is bounded to16 spawns per physics tick (Inspector MaxSpawnsPerTick, clamped1–64); positive gaps emit at most one ship per physics tick, rounded up to the physics interval. No catch-up bursts after hitches. Pause freezes pending work. Close/F2/Escape hides without cancelling; Cancel stops only remaining work. Any delivery failure stops the batch and shows the reason instead of retrying forever. Existing enemies continue normally. Disabling the level debug controller cancels pending work. No physics callback remains active when idle.

Tool source now lives under DEBUG: LevelTools (controller/reusable map scene), Spawning (new menu and unbound LegacyCatalogSpawner), Navigation (F3 preview planner/overlay and editor checkpoint design overlay), Alerts (F7 preview), Audio (F9 tester), Progression (optional menu XP grant partial), plus existing performance overlay/profiler. Scene/script UIDs are retained and map/HUD/autoload references updated. Scene instances stay with their level/HUD/audio owners; production navigation/combat services and small profiler/attachment hooks retain their normal ownership. Automated tests remain under TESTS. Existing F3/F4/F5/F7/F9 behavior is retained; historical F2-cycling descriptions below are superseded.

Verification: TESTS/Debug/EnemySpawnMenuChecks.tscn and existing PerformanceOverlayChecks pass. Random portal test uses40 seeded deliveries across two registered portals and rejects edge fallback. Build/import passed. Desktop appearance review remains with the user; broader previously documented baseline-suite failures are not claimed fixed.

---

## PF2 debug update (2026-10-20)

Optional subsystem timing hooks feed the existing F4 panel (Shift+F4 toggles profiling; F5 saves a report). Old GridDebugOverlay/Info labels are retired; F2/F3 remain. See `DEBUG/README.md` for inclusive timing semantics, fixed sample storage and validation details. Historical notes below remain as reference.

## EG3 F2 entry — 2026-10-20

DefaultEnemyCatalog now appends Gunner Drone(gunner_drone_mk1) as entry14. Existing F2 cycles it after Heavy Transporter, retaining all previous indices; F3 uses the same catalog. The new ship follows normal routes and attacks nearby placed towers with its separate rotating minigun. No debug key handling or authored waves changed. Run a gameplay launcher/Campaign, not the raw map alone. Build/runtime/manual visual acceptance is pending.

## F4 performance overlay + Level002 F2 repair — 2026-10-18

`DEBUG/PerformanceOverlay.cs` is one isolated self-building CanvasLayer script, mounted on all three playable launchers. F4 toggles a debug-build-only panel below top HUD/existing map diagnostics, with throttled frame/engine timing, enemy/projectile-node counts, bounded navigation peak windows/queues and render counters. Click-through, pause-responsive, cached read-only service access; hidden sampling stops. Inspector settings live on the overlay node. See `DEBUG/README.md` for metric meanings and thresholds.

Level002's Debug node and LevelTemplate lacked LevelDebugController/HUD wiring, so F2/F3 were absent there. Both now instance reusable `DEBUG/LevelTools/LevelDebugTools.tscn`; checkpoint-overlay children and map artwork/coordinates remain intact. Missing optional HUD no longer aborts controller setup. Failed catalog F2 attempts warn with SpawnManager.LastMessage while retaining the next enemy index. Raw map-only F6 still intentionally has no world services: use MainLevel002.tscn F6 or Campaign for gameplay spawning.

Verification: C# build and `TESTS/Debug/PerformanceOverlayChecks.tscn`; detailed scope is in `TESTS/Debug/README.md`. Existing user changes to economy, terrain and tower stats are preserved. Earlier plans/history below remain intact.

---

# Level debug helpers

Level001 owns `Debug` -> `HUD` -> `Info`. The parent uses `LevelDebugController` and exposes `Enabled`, `SpawnKey` (F2), `EnemyScene` (legacy/fallback), and `PortalOnly` (true).

## F2 catalog round-robin spawning

Each actual F2 press spawns the next scene registered in `ENEMIES/DefaultEnemyCatalog.tres`, using **the array's Inspector order**. With the current catalog, the order is:

1. Enemy001 asset — `basic_mk1` / Standard Drone
2. Enemy002 asset — `scout_mk1` / Swift Interceptor
3. Enemy003 asset — `shield_emitter_mk1` / Shield Emitter
4. Enemy004 asset — `medium_mk1` / Bulwark Drone
5. Transporter asset — `transporter_mk1` / Transporter
6. Enemy005 asset — `big_mk1` / Big mk.1
7. Enemy006 asset — `elite_mk1` / Elite mk.1
8. Enemy001 asset again, repeating

New enemy types participate automatically when you add their scenes to the catalog. The debug controller uses the production `SpawnManager.Spawn(scene, PortalOnly)` method, so normal collision/clearance, navigation, portal rules and active-enemy limits still apply.

**The index advances only after a successful spawn**. Failed spawns (e.g. when the active enemy limit is reached or no valid portal is available) leave the same enemy queued for the next press. OS key-repeat/echo events are ignored, and rapid independent presses remain supported.

The per-level `EnemyScene` export is retained as a **fallback only if the catalog is empty**. It is no longer the default source while catalog entries exist. Each new level debug controller begins at the first catalog entry.

## Debug HUD and lifecycle

The HUD shows living/killed/arrived ships, queued paths and source failures, alongside the grid/camera diagnostics. Set `Debug.Enabled = false` or remove the Debug parent to remove these level debug controls/readout. F6 map-only viewing hides the HUD and disables spawning because world services are absent. Use F5 for a gameplay test.

Debug only calls the production SpawnManager; navigation, enemy state and weapons remain normal gameplay services outside Debug. Future level-specific debug controls belong under this same parent. WorldGrid owns its optional grid/hover drawing via WorldMasterConfig.

F2 cycles **catalog scene order**, not the internal `EnemyId`, display name or intended wave order. Changing an enemy's gameplay ID does not alter the F2 sequence.

## F3 route preview

With Debug.Enabled and live world services, **F3** opens a small panel below the existing debug readout. Select an active portal or **ALL PORTALS**, enemy type, **1–50 paths per portal** and Final Refined/Raw A*, then **Simulate Paths**. Final Refined is the default. White shows neutral shortest routing; colored lines and legend show sample profile, physical distance, distance overhead and duplicate routes. **Clear Paths** removes results; F3 hides the panel/lines and cancels work. Nothing is spawned.

`PreviewKey` defaults F3; `PreviewCalculationsPerFrame` defaults 1 and `PreviewMillisecondsPerFrame` defaults 1 ms (soft, 0 disables time limiting). These budgets are separate from gameplay PathRequestManager budgets. No preview calculations run while closed. Current settings and per-type permissions apply; source/radius failures draw no unsafe lines. Map/settings changes clear results for explicit resimulation. Congestion is a snapshot, not a traffic prediction. Linked portals preview Portal → required checkpoint area → Citadel and draw the circle/entry markers. Missing/unreachable required regions show failures rather than a direct bypass. No panel exists in isolated F6 map scenes lacking world services.

### Preview-only diversity controls

F3 provides Enable Path Diversity, Diversity Strength (0–3), Weighted Profiles (1–16) and Reset to Game Settings. Values are held in a private copied EnemyMasterConfig and a detached preview-only A* profile pool, shared across that preview batch's portals. They never modify the live resource, live profile caches, authored files or enemy routes. Reset copies current game settings into the panel. The legend displays the values so they can be transferred manually into normal configuration later. Closing F3 retains the local control values for reopening; leaving the level discards them.

ALL PORTALS draws up to 50 samples for each active portal (five portals means up to 250 paths), with portal color families and per-portal unique-route counts. Every portal shares the same per-frame debug work cap; the cap is not multiplied by portal count. Large batches can take several seconds to fill in. The scrollable panel keeps all controls and the legend accessible. Weighted profiles still may choose duplicate routes.

## Checkpoint design diagram

`Debug/CheckpointOverlay` is a separate shared tool-script Node2D in both level scenes and the template. In the editor it draws every checkpoint circle/name above tiles (absolute z4090), portal → first checkpoint arrows, and checkpoint → all next checkpoint arrows. It refreshes one cached drawing on marker/link changes, throttled at0.15s. ShowInEditor controls editor visibility; no runtime processing occurs while hidden. F3 can show the same graph with its sampled paths. Those design arrows describe links, not clearance-tested routes. Empty NextCheckpoints means Citadel; multiple links mean one uniform choice per enemy on entry. Cycles/broken links are warnings, not fallback routes.
