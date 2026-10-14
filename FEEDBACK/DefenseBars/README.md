# Defense Bars — Reusable Hull Health Display

Implemented for `Enemy001` and reusable later for any damageable tower or world object. The `DefenseBar.tscn` scene is a lightweight `Node2D` with a procedural dark-backed, colored health fill. It does not need a `ProgressBar`, physics collision, global manager, timers or its own `_Process` callback.

## Behavior
- **100% health:** completely invisible.
- **Damaged but alive:** appears immediately, small and centered above the object; health fill tracks `current / max`.
- **Healed to 100% / zero health / death:** hides immediately.
- **Rotating ships:** Enemy movement calls the bar's cheap counter-rotation helper only if visible, so the bar remains screen-horizontal rather than turning with the spaceship.
- **Colors:** cyan-green while healthy, amber below the warning fraction, red below critical (all adjustable).
- **Width:** logarithmic by maximum HP, with hard limits — default **28px minimum, 74px maximum**, starting at 60 HP and **7px extra per doubling**. A 10,000-HP enemy cannot get an enormously wide bar.

## Inspector settings
- `DefaultDefenseBarConfig.tres`: MinWidthPixels, MaxWidthPixels, ReferenceHealth, PixelsPerHealthDoubling, HeightPixels, BorderPixels, VerticalOffsetPixels, colors and warning thresholds.
- `FEEDBACK/DefaultFeedbackConfig.tres`: shared `DefenseBarsEnabled` switch. It is checked whenever a bar's health state changes (toggle before F5 for predictable results).

## Reusing on future towers

Instance `DefenseBar.tscn` as a child of any future damageable tower/structure, and hold a reference to its `DefenseBar` node. Call `SetHealth(currentHealth, maxHealth)` on initial health and whenever hit/healed. If its parent rotates, call `AlignWithOwner(parent.Rotation)` after changing the rotation. A static tower doesn't need periodic alignment.

**Only health is drawn right now.** Shields, armor and further defensive resources can be added as separate bars/layers later. Do not put health-state logic inside this drawing component.

## Manual test
F5 -> F2 to spawn Enemy001 -> place a Battle Tower near its route. Unharmed ships have no bar; after a bullet hits, a short horizontal bar appears and shrinks on further hits. Health bars disappear on enemy death/arrival. Use the Inspector to adjust MaxHealth on Enemy001's Resource and see the capped, slowly increasing width. Rendering still scales with world camera zoom.
