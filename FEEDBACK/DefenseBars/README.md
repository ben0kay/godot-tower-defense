# Defense Bars — Reusable Hull + Shield Meters

Implemented for all enemies via the shared `GenericEnemy.tscn`, and reusable later for damageable towers or world objects. The `DefenseBar.tscn` scene is one lightweight `Node2D` drawing a procedural hull health bar **and, only when shield points remain, a thinner shield bar above it**. No additional scene node or particle emitter is created per enemy. It does not need a `ProgressBar`, physics collision, global manager, timers or its own `_Process` callback.

## Behavior
- **100% health, no shields:** completely invisible, as before.
- **Any surviving shields:** show two aligned rows, the narrow warm red-orange **shield meter on top** and the full/damaged cyan-green **hull meter below**. Shield can be at 100% and both rows still appear.
- **Damaged but alive:** appears immediately, small and centered above the object; health fill tracks `current / max`.
- **Shield drops to zero:** the upper meter disappears immediately; the lower hull meter reverts to damaged-only visibility. The existing aura can finish its brief break flash independently.
- **Shield restored from zero:** the upper meter returns in the same `DefenseBar` node; no node allocation or passive regeneration is involved.
- **Healed to 100% with no shields / zero hull health / death:** hides immediately.
- **Rotating ships:** Enemy movement calls the bar's cheap counter-rotation helper only if visible, so the bar remains screen-horizontal rather than turning with the spaceship.
- **Colors:** hull stays cyan-green/amber/red by its existing thresholds; shield uses an independent warm red-orange `ShieldColor` to match the energy aura (all adjustable).
- **Width:** logarithmic by maximum HP, with hard limits — default **28px minimum, 74px maximum**, starting at 60 HP and **7px extra per doubling**. A 10,000-HP enemy cannot get an enormously wide bar.

## Inspector settings
- `DefaultDefenseBarConfig.tres`: existing hull sizing/colours/thresholds plus the new Inspector group **Shield Meter** (`ShieldHeightPixels = 4`, `ShieldGapPixels = 2`, `ShieldColor = #ff865b`). Both meters share the same capped hull-based width and border/background.
- `FEEDBACK/DefaultFeedbackConfig.tres`: shared `DefenseBarsEnabled` switch. It is checked whenever a bar's health state changes (toggle before F5 for predictable results).

## Reusing on future towers

Instance `DefenseBar.tscn` as a child of any future damageable tower/structure, and hold a reference to its `DefenseBar` node. Call `SetHealth(currentHealth, maxHealth)` on initial health and whenever hit/healed; when the owner supports shields, also call `SetShield(currentShield, maxShield)` on shield initialization/hit/recovery. Shield game logic remains in the owner and `COMBAT/Damage`, not this drawing component. If its parent rotates, call `AlignWithOwner(parent.Rotation)` after changing the rotation. A static tower doesn't need periodic alignment.

Only **hull health and current shield points** are drawn. Armour and other defensive resources remain future features. Both rows use one `QueueRedraw()` path on health/shield changes, not `_Process` or `PhysicsProcess`. The owner calls `AlignWithOwner` during its existing movement callback only while visible.

## Manual test
F5 -> F2 to spawn Enemy001 -> place a Battle Tower near its route. Unshielded, unharmed ships have no bar; after a hull hit, a short horizontal bar appears. For a wave-spawned shielded Standard Drone, the full hull bar and warm shield bar both show immediately. Shield damage shrinks only the top meter; depletion hides the top meter, and the hull meter behaves normally. Health bars disappear on enemy death/arrival. Use the Inspector to adjust MaxHealth on Enemy001's Resource and see the capped, slowly increasing width. Rendering still scales with world camera zoom.
