# The Citadel — Defended Destination

`Citadel.tscn` is a reusable visible placeholder, instanced in Level001 and the empty template. `WorldObjectFootprint` reserves and blocks a 3×3 grid footprint. Root position is the upper-left cell center; the visual core is offset (64,64) to the footprint center. Snap the root to 32 + 64*n on each axis.

Four editable `ApproachPoints` Marker2Ds sit one cell outside the footprint on each side. Keep them passable and reachable when painting terrain. The level's Settings/CitadelPath identifies the objective relative to the level root. Future AI should navigate toward reachable approach points, not the solid footprint.

Placeholder polygons/label are replaceable. Health, damage, defeat signals, enemy AI and combat remain unimplemented. Spawning belongs in ENEMIES.

## Live Citadel health (implemented)

`Citadel.tscn` retains its `WorldObjectFootprint` root, visuals and four navigation approach markers. The new child `Health` (`CitadelHealth.cs`) owns session HP, `Initialize(maxHealth)`, `TakeDamage(amount)`, read-only `CurrentHealth`/`MaxHealth`, `HealthChanged(current,maximum)`, and the **one-time `Destroyed` event** on transition to 0 HP. HP clamps at zero; zero/negative damage does nothing; a new level initialization restores full HP. The component itself does not implement a defeat menu, stop waves or destroy the physical objective.

The reusable `CitadelSession.cs` is attached to `Main.tscn/CitadelSession`. It reads the active `World/Grid.Level.Settings.CitadelMaxHealth`, finds the level's `Citadel/Health` via the existing `CitadelPath`, subscribes to `EnemyRegistry.EnemyArrived` (not `EnemyKilled`) and applies the arriving ship's `EnemyDefinition.CitadelDamage`. It connects `HealthChanged` to the existing HUD `SetCitadelHealth` setter. It unhooks both event sources during teardown. **F2 and wave enemies share the same arrival behavior, and kill rewards remain kill-only.** Citadel HP stays entirely owned by this component; do not store it in the HUD or EnemyDefinition.

**Per-level balance:** `LEVELS/LevelConfig.cs` exposes **Destination / Citadel Max Health** (default 1,000), saved per level in `LEVELS/Level001/Level001Config.tres` (currently 1,000) or other LevelXXX config Resources. Do not put a competing maximum-health default in this reusable Citadel scene. If a level eventually requires starting partially damaged, add a separate level-setting explicitly; today each level starts at full maximum.

**Future work:** a level/session game-state controller should subscribe to `CitadelHealth.Destroyed` and decide defeat/retry/game-over, and subscribe to final-wave completion for victory. This health-only implementation intentionally leaves waves and gameplay running at 0 HP until that pass. Add any future Citadel shields/armor as clearly separated mechanics rather than mixing them with base HP.
