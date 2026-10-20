## EG3 producer — 2026-10-20

Gunner Drone is now the first real catalog/F2 enemy using the EG2 world-owned system. Its separate hull/gun scene and dedicated tracer/flash resources live under Types/GunnerDrone and Weapons/Minigun. No new bullet ownership, collision, capacity, damage or per-shot nodes were introduced by EG3. Existing normal waves remain unchanged. Actual1–10-gunner visible testing remains pending because execution was unavailable. Older no-Gunner wording below is historical.

# Central hostile bullets — EG2

EnemyBulletSystem.cs owns one world-scoped dense fixed-capacity array of EnemyBulletData structs. EnemyBulletRenderer.cs draws that same array; bullets have no nodes, bodies, timers, tweens, shooter references or target references. EnemyShotRequest is the shared immutable input. EnemyMinigun caches the world service and TrySpawn returns whether capacity/validation accepted a real launch. ShotRequested observes accepted shots; no subscriber is needed to fire.

## Configuration

DefaultEnemyProjectileConfig.tres exposes MaximumActive(default4096), DefaultLifetimeSeconds(2.5), and DefaultVisual. These world settings are captured at Ready; changing capacity requires a new world/system. The generic fallback tracer is7px long,1.3px core width, muted coral/red with faint glow and no bright tip. It uses the existing ProjectileVisualStyle/Snapshot format. A weapon may supply its own captured snapshot and lifetime; EnemyMinigunConfig provides both fields. Dedicated Gunner Drone art/flash presets remain EG3. Render styling never changes trajectory or damage.

## Travel and collision

TrySpawn validates finite origin/direction, nonnegative damage, positive finite speed and0–30s lifetime (0 selects world default). It normalizes direction once. Advance clips the step to remaining lifetime and queries spatial buckets along the travelled segment. TowerFootprintCollision clips in grid space against actual full rectangles; the nearest entry fraction wins, with registration-order ties. The round is removed before one existing TowerBase.ApplyDamage call with Kinetic DamageInfo and null DamageSource. Hits at the lifetime endpoint count. No homing, source tracking, enemy hits, Citadel hits, wall collision or friendly XP exists here. Grid transforms are fixed during play; root tower relocation follows the registry's event path.

No offscreen gameplay culling exists. Misses expire by finite lifetime. Numerically unrepresentable endpoints are invalidated; malformed requests are rejected. A full buffer rejects only new requests, incrementing Overflow; it does not delete old shots, silently resize or queue catch-up fire. Reentrant spawns during impact callbacks are rejected so mutation stays bounded. Damage callbacks run after spatial enumeration finishes, permitting ordinary tower destruction/cleanup safely.

## Lifetime and instrumentation

The system's only physics callback sleeps when empty. Renderer has no Process/PhysicsProcess loop and is redrawn on state changes. Existing in-flight bullets continue after shooter death/removal. Pause freezes launches, travel and lifetime; unload clears all state. Clear is for explicit world restart/test reset, never source removal.

Counters: ActiveCount, Capacity, Spawned, Impacts, Expired, Invalidated, Cleared, Rejected, Overflow, CandidateChecks. Accepted-shot accounting is Spawned = ActiveCount + Impacts + Expired + Invalidated + Cleared. Counters are available to future F4 integration; no debug overlay changes are included.

World composition places the renderer at projectile z6, below effects z7. The retained renderer uses copied style values and optional texture references; no mutable per-shot Resource/material is constructed. One drawing node is not a claim of GPU batching or bullet-hell performance. EG4 must measure physics/drawing on real hardware.

Verification: TESTS/EnemyWeapons/README.md. Source/static review only; Godot import, C# build, runtime/headless and desktop verification remain pending because execution was unavailable. Current authored enemies/waves stay unarmed. EG3 supplies the visible F2 Gunner Drone.
