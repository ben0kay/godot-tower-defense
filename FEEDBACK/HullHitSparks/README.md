# HI1 — pooled enemy hull-hit sparks

**Source implementation, awaiting Godot visual and FPS acceptance.** Small hot-white, yellow and orange streaks show only when an eligible **kinetic projectile damages enemy hull HP**. Pure shield absorption, invalid hits, and non-damaging hits produce no metallic sparks; the separate shield/ricochet visuals remain untouched. A penetrating shot that actually reduces hull HP can produce both a hull-spark burst and existing shield feedback.

## Ownership
- `HullHitSparkController.cs`: one `World/Effects/HullHitSparks` CanvasItem; **preallocated 512-slot array**, Inspector default **320 active fragments** and a hard bound of eight fragments per hit. Each spark is a tiny moving `DrawLine`, not a GPU particle, scene node, physics body, texture, or shader. Occupied slots alone get a short physics update (0.12–0.20s). The controller sleeps when empty.
- `HullHitSparkStyle.cs` and `DefaultHullHitSparkStyle.tres`: shared lifetime/speed/line length/thickness/opacity/concurrency Inspector settings. `FEEDBACK/DefaultFeedbackConfig.tres` enables/disables HI1 globally with `HullHitSparksEnabled`.
- Battle Tower, Machine Gun Tower, Sniper Tower each opt in via `HullHitSparkCount` in the tower's typed Config; default per hit **4 / 2 / 6** respectively. Other projectile weapons default to **zero**. Shared visual code doesn't inspect specific tower IDs.

## Correct damage timing

Physical `BasicProjectile` records the **actual swept collision position**, compares `Enemy.Health` immediately before and after `ApplyDamageWithShieldAbsorption`, then requests its launch-snapshotted hit count only when `Health` **decreased**. The same existing damage call/XP/status/ricochet paths remain authoritative and single-hit.

Sniper defaults to `InstantTarget`, so `WeaponHitDelivery` uses the same before/after HP guard and computes an approximate hull-edge impact point from the muzzle-to-target direction and the target's gameplay radius. No fake bullet node or raycast is added. The shared turret caches the effect controller once during `_Ready`; it does not do a scene lookup per shot.

Hit sparks expire in the battlefield's own `World/Effects`, not as children of potentially dying ships. Excess bursts are simply skipped at capacity (no extra queue or allocations). Pausing stops update; unloading clears immediately; `_PhysicsProcess` runs only while fragments exist.

## Visual acceptance

Check Battle and Machine Gun physical hits against an unshielded ship, Sniper instant hits, shield-only hits (zero hull sparks), partial penetration (hull sparks), lethal hits, and high-rate fire into a 100+ enemy wave. Inspect tiny clean impacts at normal zoom and compare F4/projectile timing with the effect enabled vs disabled. The owner handles Godot playfeel validation; no runtime/FPS result is claimed here.
