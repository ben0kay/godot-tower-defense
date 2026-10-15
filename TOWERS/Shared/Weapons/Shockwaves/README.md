# Expanding gameplay wavefront — Phase 4E implemented

`ShockwaveEmitter` is one lightweight Node2D per emission, normally under World/Projectiles. It owns a fixed world origin, immutable DamageInfo, weapon-supplied maximum radius/speed/edge retention, previous/current radius, a registry-owned motion revision cursor and a per-emission HashSet of damaged instance IDs. Multiple emissions overlap independently. No physics bodies, per-enemy Areas, timers or full-disc attack calls are used.

## Delivery and geometry

Initialize snapshots inputs and acquires an O(1) motion cursor without scanning enemies; Ready creates optional shared feedback under World/Effects. The source tower can move, upgrade or disappear afterward without changing an emitted wave. World/registry unload cancels it safely. Each physics tick advances to `min(maxRadius, previousRadius + speed * delta)`.

For each valid unhit enemy, solve the swept relative contact equation over this active tick:

`|previousPosition + (currentPosition - previousPosition) * t - origin|² = (previousRadius + (currentRadius - previousRadius) * t)²`

The earliest root in [0,1] is contact. This handles stationary enemies skipped by a large radial step and fast enemies that pass through the front between two samples, even if both sampled positions are outside. A new enemy with no previous sample is treated as stationary at its observed position: one spawned behind the previous front is not damaged by the filled interior. An unhit enemy crossing the front outward can also touch it. Enemies already hit cannot be hit again by this emission. Geometry uses centers, matching instant AoE; visual width/glow do not enlarge the damage front. Radius zero hits only the exact origin.

On a terminal tick, interpolate motion only through the fraction of delta before the wave reaches maximum radius. Motion after expiry cannot create retroactive hits. As with any sampled system, sub-tick enemy motion is approximated linearly between observed world positions; curved/teleport motion has no separate historical path reconstruction.

At contact, use the encounter radius with `AreaDamage.AtDistance` to scale base damage. Each resulting DamageInfo preserves type, independent absolute shield/hull overrides and penetration. Only Enemy.ApplyDamage performs defense/health/lifecycle work. **Never repeatedly call AreaDamage.ApplyInstant.**

## Mutation, lifetime and cost

Each tick collects crossings into reusable local scratch storage before applying damage, so immediate death/removal callbacks cannot corrupt candidate enumeration. Recheck native validity, alive/tree/queue state and constant-time membership before dispatch. Mark hit-once IDs before callbacks, including zero-damage contacts. Reentrant unload stops remaining recipients safely; malformed defense failures finish the emission rather than retrying it next tick. Dead/freed enemies are removed from the shared index immediately and snapshot recipients are revalidated before dispatch.

The registry-owned shared index retains movement samples since the oldest active wave cursor. Each tick queries spatial cells intersecting the swept annulus, including conservative motion envelopes for enemies whose endpoints can lie outside it. Samples preserve the original relative solver, even across fast teleports, multiple writes between ticks and independently advanced emissions; new spawns still have no prior wave sample. Per-wave hit/candidate/recipient scratch is reused and released with its cursor on expiry/unload. No emission seeds from or scans EnemyRegistry.Living.

Movement indexing is updated by actual transform changes, with amortized history retirement. Large swept envelopes (over64 cells) use a conservative overflow set to avoid unbounded grid storage; exceptional fast movers may be tested by multiple distant waves. Dense/all-nearby and very large queries necessarily offer less pruning. No assumed speed limit weakens the solver. See TESTS/SpatialQueryChecks.cs for original-emitter parity and TESTS/SpatialQueryBenchmarks.md for combined net performance, maintenance, allocations and dense-case results.

## Stats, source and visuals

Shockwave Tower's complete typed `ShockwaveTowerLevels.cs` rows own all prices and combat values. Shared RangePixels is maximum radius, optional ShockwaveStats holds float DamagePerWave, ExpansionSpeedPixelsPerSecond, IntervalSeconds and EdgeDamageFraction. WeaponDamageStats and ShieldPenetration preserve shared damage semantics. The same eight-stage pipeline selects a complete row at Stage02; Stage03 applies earned ranks and Stages04–06 remain neutral and Stage07 clamps safe values. Later research can modify these inputs through its existing stage/invalidation route; research/saving are not implemented.

The emitter calls FEEDBACK/Shockwave/ShockwaveEffect.SetWavefront with exact simulation radius/progress and completes it on expiry. The purple tower owns its own style. The visual never independently calculates enemy damage or advances the external radius. Blaster uses the same visual in autonomous mode **after instant damage**, not this timed emitter.

ShockwaveChecks covers30/60/120Hz reach/cadence, linear relative crossings/contact falloff, long/terminal ticks, behind-front spawns, exact zero radius, overlapping/subsequent waves, shield break/overrides/penetration, deaths/frees/unload/reentrancy, missing/mutated visuals, explicit stats, purchases/previews/max level and500 recipients. See TESTS/README.md for executed results.
