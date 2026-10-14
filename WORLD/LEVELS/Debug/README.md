# Level debug helpers

Level001 owns `Debug` -> `HUD` -> `Info`. The parent uses LevelDebugController; it exposes Enabled, SpawnKey (F2), EnemyScene (Enemy001), and PortalOnly (true). One actual press spawns one ship; keyboard echo is ignored. Rapid repeated presses are supported up to the enemy-system safety limit.

The HUD shows living/killed/arrived ships, queued paths and source failures, alongside existing grid/camera diagnostics. Disable Debug.Enabled or remove the Debug parent to remove these level debug controls/readout. F6 map-only viewing hides the HUD and disables spawning because world services are absent. Use F5 for the gameplay test.

Debug only calls the production SpawnManager; navigation, enemy state and weapons remain normal gameplay services outside Debug. Future level-specific debug controls should live under this same parent. WorldGrid still owns its existing optional grid/hover drawing, configured by WorldMasterConfig.
