## VS1 — Visual Scale

The engine first calculates dimensions and rear nozzle offsets in authored hull units, then scales its existing branch once, including visibility bounds. Resized ships use local-space exhaust so fleck size and travel follow the same multiplier. Ships at 1× keep the authored world-space trail. The emitter budget and movement-driven intensity are unchanged.

---

## ET1 flame-length balance — 2026-11-04

**Current rule:** flame **width** still depends only on displayed sprite height (`hullHeight × FlameWidthToHullHeight`). Flame **length** is now the independent sum of a base, a *hull-size contribution* and a *max-speed contribution*: `BaseFlameLengthPixels + sqrt(hullWidth × hullHeight) × LengthPerHullSize + MoveSpeed × LengthPerSpeed`, then clamped to `[5, MaxFlameLengthPixels]`. Defaults: **4 px + hullSize × 0.12 + speed × 0.10**, cap **55 px**. This avoids large, slow capital ships looking underpowered and preserves long narrow plumes on fast small ships.

| Existing ship | Artwork (px) | Speed (px/s) | Width (px) | New full-speed length (px) |
| --- | ---: | ---: | ---: | ---: |
| Standard Drone | 32×24 | 100 | 4.1 | 17.3 |
| Swift Interceptor | 28×18 | 160 | 3.1 | 22.7 |
| Bulwark Drone | 64×48 | 75 | 8.2 | 18.2 |
| Heavy Transporter | 144×100 | 35 | 17.0 | 21.9 |
| Cruiser | 168×96 | 30 | 16.3 | 22.2 |
| Elite mk.1 | 80×60 | 100 | 10.2 | 22.3 |

**GPU exhaust adjustment:** particle `SpeedScale` now uses `_baseFlameLength / 16` rather than `_nominalSpeed / 100`, times the current effective movement-speed fraction and the existing global `ParticleSpeedMultiplier`. Thus a large slow ship has proportionate visible exhaust, while Slow/Freeze still attenuates/stops it. Particle count (4), emitter cap (64), lifetime, renderer, visibility checks, mount location and status hooks remain unchanged; no new per-frame logic or particle materials.

Edit `DefaultEnemyThrusterConfig.tres` for `LengthPerHullSize`, `LengthPerSpeed`, `BaseFlameLengthPixels` and `MaxFlameLengthPixels`. Inspector changes affect newly initialized enemies; this is source-implemented, **not yet validated visually or benchmarked in Godot**. Check a Swift vs Bulwark and Heavy Transporter vs Elite at normal zoom.

---

# ET1 — shared enemy engine thrusters

All registered enemy ships now inherit `EnemyThruster.tscn` from `ENEMIES/Shared/GenericEnemy.tscn`. No individual ship scene, enemy ID, family, wave, navigation or weapon logic was changed.

## Visuals and sizing

- `Artwork/EngineFlame.svg`: one rear-facing, gently tapered fiery ignition with white-hot nozzle, yellow inner core and orange fading tail. It is a single static `Sprite2D` drawn behind the ship, so its gradient remains visible even at the GPU emitter cap.
- `Artwork/EngineSpark.svg`: a thin luminous fleck used by the optional `GPUParticles2D` exhaust. The material and randomized short velocities live in `EnemyThruster.tscn`; `local_coords = false` leaves emitted particles in world space as ships move.
- `EnemyThrusterVisual.cs`: measures `Sprite.GetRect().Size × abs(Sprite.Scale)` **once per enemy spawn**; **flame width = displayed hull height × 0.17**, **flame length = 4 + sqrt(hullWidth × hullHeight) × 0.12 + MoveSpeed × 0.10**, capped at 55px. The nozzle sits at the rear (`-X` for the existing right-facing `+X` hull assets). The effect inherits enemy rotation, so turning ships turn their exhaust automatically.
- Optional per-type `EnemyDefinition.ThrusterEnabled` (default true) and `ThrusterMountOffset` (default (0,0)) allow an individual artwork nozzle to be moved/hidden without creating a unique visual script. One thruster mount/ship in ET1; multi-engine artwork is a possible later pass.

**Approximate default flame proportions (pixels at full motion):**

| Ship | Hull sprite | Authored speed | Flame width | Flame length |
| --- | --- | ---: | ---: | ---: |
| Standard Drone | 32×24 | 100 | 4.1 | 17.3 |
| Swift Interceptor | 28×18 | 160 | 3.1 | 22.7 |
| Bulwark Drone | 64×48 | 75 | 8.2 | 18.2 |
| Heavy Transporter | 144×100 | 35 | 17.0 | 21.9 |
| Cruiser | 168×96 | 30 | 16.3 | 22.2 |
| Elite mk.1 | 80×60 | 100 | 10.2 | 22.3 |
| Racer | use sprite size | 230 | slim if sprite is slim | adds 23px from speed plus its hull-size term |

`Enemy.EffectiveMoveSpeed` affects intensity as a **status-change notification** (Slow/Freeze), not a new per-frame speed sampling loop. Freeze extinguishes and later restores the engine; stealth reduces flame/particle alpha to match its dimmed hull.

## Performance and ownership

- `EnemyThrusterConfig.cs` and `DefaultEnemyThrusterConfig.tres` hold the **shared Inspector controls**: Enabled, FlameWidthToHullHeight, BaseFlameLengthPixels, LengthPerHullSize, LengthPerSpeed, MaxFlameLengthPixels, RearNozzleInsetPixels, GpuParticlesEnabled, MaxActiveGpuEmitters, ParticlesPerShip, ParticleSpeedMultiplier, MinimumSpeedFraction and StealthIntensity.
- Each enemy has **one non-processing visual child**, a static Sprite2D, one optional GPUParticles2D and a `VisibleOnScreenNotifier2D`. No `_Process` or `_PhysicsProcess`, active polling or custom per-enemy material duplication.
- Default GPU cap is **64 on-screen emitters × 4 particles = 256 GPU slots**. Invisible or stopped ships don't emit; at cap, excess ships still show the inexpensive static flame. A capped-but-still-visible ship may not acquire a freed slot until it exits/reenters screen or undergoes an effective-speed change. This conservative ET1 tradeoff keeps the system simple.
- GPU emitter state uses `VisibleOnScreenNotifier2D.ScreenEntered/ScreenExited` and the existing enemy lifecycle. Offscreen, Freeze, death, Citadel arrival and scene unloading release the count immediately. The scene's normal pause mode is inherited. No new global autoload or enemy-world loop.
- When ET1 is disabled through the shared resource, visuals do not activate and particle GPU budget is not occupied.

## Owner visual acceptance

In Godot.NET, launch Level001/F2 and compare Standard Drone vs Swift Interceptor (same narrow feel, noticeably different flame length), Bulwark vs Heavy Transporter (both substantial, but the larger hull has a longer wider plume), and a fast heavy elite. Watch the nozzle behind the rotating ship rather than ahead of it; confirm orange/yellow/white contrast against the map. Test Slow/Freeze, stealth, 100/250+ ships, pause and removal; inspect F4 FPS before increasing the cap. If some art has a noncentral rear nozzle, adjust `ThrusterMountOffset` on its `.tres`. This is an **initial source implementation, not a measured FPS improvement or accepted look**.

