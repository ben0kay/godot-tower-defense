# Level debug helpers

Level001 owns `Debug` -> `HUD` -> `Info`. The parent uses `LevelDebugController` and exposes `Enabled`, `SpawnKey` (F2), `EnemyScene` (legacy/fallback), and `PortalOnly` (true).

## F2 catalog round-robin spawning

Each actual F2 press spawns the next scene registered in `ENEMIES/DefaultEnemyCatalog.tres`, using **the array's Inspector order**. With the current catalog, the order is:

1. Enemy001 asset — `basic_mk1` / Standard Drone
2. Enemy002 asset — `scout_mk1` / Swift Interceptor
3. ShieldGenerator asset — `shield_emitter_mk1` / Shield Generator
4. Enemy004 asset — `medium_mk1` / Bulwark Drone
5. Enemy001 asset again, repeating

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
