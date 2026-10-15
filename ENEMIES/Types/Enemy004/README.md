# Enemy004 — Bulwark Drone (`medium_mk1`)

First **medium-sized plain enemy**, developed after Enemy001/Enemy002. Its numbered scene/resource/art filenames mark **development order**, not wave appearance order. Its stable gameplay key is `medium_mk1`; its player-facing name is `Bulwark Drone`.

| Stat | Enemy001 reference | Enemy004 |
| --- | ---: | ---: |
| SVG art size | 32 × 24 px | **64 × 48 px** (2×) |
| Navigation clearance radius | 12 px | **22 px** |
| Hull HP | 60 | **160** |
| Movement speed | 100 px/s | **75 px/s** |
| Turn speed | 8 | **5.5** |
| Shield capacity | 0 | **0** |
| Damage to Citadel on arrival | 1 | **2** |
| Kill reward Credits | 3 | **7** |

This is a provisional balance pass. Enemy004 uses the existing shared `Enemy.cs`, `EnemyMovement.cs`, `DefenseBar`, death feedback, typed damage, credits and tower-XP rewards. **No new AI, abilities, armor effects, extra runtime controllers or health mechanics.**

Artwork uses a new broad, twin-fin, plated silhouette with amber exhaust and cyan core, pointing right to match shared ship rotation. `RadiusPixels = 22` stays below half the current **64px** navigation cell; bigger future cargo/boss ships may require a separate larger-footprint/navigation design.

## Spawning / editing

The existing `DefaultEnemyCatalog.tres` includes `Enemy004.tscn`. Press **F2** to cycle through catalog scenes, with Enemy004 appended after ShieldGenerator. Alternatively call `SpawnManager.Spawn("medium_mk1")` or author `Enemy("medium_mk1")` in a wave. **Level001's authored waves remain basic-only** until explicitly edited; catalog order never dictates progression. Edit `Enemy004.tres` in Godot Inspector to change balance/ID/name; keep IDs stable if referenced by authored waves.
