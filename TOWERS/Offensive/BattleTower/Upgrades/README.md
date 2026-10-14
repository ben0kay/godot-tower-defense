# Battle Tower playtest tiers

Edit Level2.tres and Level3.tres in Godot's Inspector; DefaultBattleTowerConfig.tres / UpgradeTiers references them in level order. These are editable playtest values, not final balance.

| Tier | Price | Cumulative damage | Shots/sec | Range |
| --- | --- | --- | --- | --- |
| 1 (construction) | 200 Credits | 12 | 1.0 | 256px |
| 2 | 150 Credits | 16 | 1.2 | 288px |
| 3 | 250 Credits | 22 | 1.5 | 320px |

Level2 adds +4 damage, +.2 shots/sec and +32px range. Level3 adds another +6, +.3 and +32px. Modifiers are cumulative within the upgrade stage, so changing base values also changes the effective upgraded results.

F5 → Ctrl → Battle → place → right-click/Esc to exit build mode → click a placed tower. The existing Inspector shows current level and next upgrade preview/price. Click Upgrade; the wallet pays once and the tower immediately uses its resolved stats. Level 3 shows Maximum level reached. Reopen the Inspector independently of the build tray.

Add future sequential Tier .tres resources to the array for more levels. Optional materials use the same ResourceCost type as construction. No selling, rank bonuses, research, level conditions, temporary effects or visual tower-art changes are introduced in this pass.
