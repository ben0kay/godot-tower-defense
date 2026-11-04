# ET1 — shared enemy engine thrusters

All registered enemy ships now inherit `EnemyThruster.tscn` from `ENEMIES/Shared/GenericEnemy.tscn`. No individual ship scene, enemy ID, family, wave, navigation or weapon logic was changed.

## Visuals and sizing

- `Artwork/EngineFlame.svg`: one rear-facing, gently tapered fiery ignition with white-hot nozzle, yellow inner core and orange fading tail. It is a single static `Sprite2D` drawn behind the ship, so its gradient remains visible even at the GPU emitter cap.
- `Artwork/EngineSpark.svg`: a thin luminous fleck used by the optional `GPUParticles2D` exhaust. The material and randomized short velocities live in `EnemyThruster.tscn`; `local_coords = false` leaves emitted particles in world space as ships move.
- `EnemyThrusterVisual.cs`: measures `Sprite.GetRect().Size × abs(Sprite.Scale)` **once per enemy spawn**; **flame width = displayed hull height × 0.17**, **flame length = 4 + MoveSpeed × 0.10**, capped at 40px. The nozzle sits at the rear (`-X` for the existing right-facing `+X` hull assets). The effect inherits enemy rotation, so turning ships turn their exhaust automatically.
- Optional per-type `EnemyDefinition.ThrusterEnabled` (default true) and `ThrusterMountOffset` (default (0,0)) allow an individual artwork nozzle to be moved/hidden without creating a unique visual script. One thruster mount/ship in ET1; multi-engine artwork is a possible later pass.

**Approximate default flame proportions (pixels at full motion):**

| Ship | Hull sprite | Authored speed | Flame width | Flame length |
| --- | --- | ---: | ---: | ---: |
| Standard Drone | 32×24 | 100 | 4.1 | 14 |
| Swift Interceptor | 28×18 | 160 | 3.1 | 20 |
| Bulwark Drone | 64×48 | 75 | 8.2 | 11.5 |
| Heavy Transporter | 144×100 | 35 | 17.0 | 7.5 |
| Elite mk.1 | 80×60 | 100 | 10.2 | 14 |
| Racer | use sprite size | 230 | slim if sprite is slim | 27 |

`Enemy.EffectiveMoveSpeed` affects intensity as a **status-change notification** (Slow/Freeze), not a new per-frame speed sampling loop. Freeze extinguishes and later restores the engine; stealth reduces flame/particle alpha to match its dimmed hull.

## Performance and ownership

- `EnemyThrusterConfig.cs` and `DefaultEnemyThrusterConfig.tres` hold the **shared Inspector controls**: Enabled, FlameWidthToHullHeight, BaseFlameLengthPixels, LengthPerSpeed, MaxFlameLengthPixels, RearNozzleInsetPixels, GpuParticlesEnabled, MaxActiveGpuEmitters, ParticlesPerShip, ParticleSpeedMultiplier, MinimumSpeedFraction and StealthIntensity.
- Each enemy has **one non-processing visual child**, a static Sprite2D, one optional GPUParticles2D and a `VisibleOnScreenNotifier2D`. No `_Process` or `_PhysicsProcess`, active polling or custom per-enemy material duplication.
- Default GPU cap is **64 on-screen emitters × 4 particles = 256 GPU slots**. Invisible or stopped ships don't emit; at cap, excess ships still show the inexpensive static flame. A capped-but-still-visible ship may not acquire a freed slot until it exits/reenters screen or undergoes an effective-speed change. This conservative ET1 tradeoff keeps the system simple.
- GPU emitter state uses `VisibleOnScreenNotifier2D.ScreenEntered/ScreenExited` and the existing enemy lifecycle. Offscreen, Freeze, death, Citadel arrival and scene unloading release the count immediately. The scene's normal pause mode is inherited. No new global autoload or enemy-world loop.
- When ET1 is disabled through the shared resource, visuals do not activate and particle GPU budget is not occupied.

## Owner visual acceptance

In Godot.NET, launch Level001/F2 and compare Standard Drone vs Swift Interceptor (same narrow feel, noticeably different flame length), Bulwark vs Heavy Transporter (thicker and shorter), and a fast heavy elite. Watch the nozzle behind the rotating ship rather than ahead of it; confirm orange/yellow/white contrast against the map. Test Slow/Freeze, stealth, 100/250+ ships, pause and removal; inspect F4 FPS before increasing the cap. If some art has a noncentral rear nozzle, adjust `ThrusterMountOffset` on its `.tres`. This is an **initial source implementation, not a measured FPS improvement or accepted look**.

