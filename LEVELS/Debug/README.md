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
