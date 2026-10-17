# Enemy status effects — CT1

Implemented: permanent Slow, strongest-wins reapplication, per-definition Inspector immunity, cached per-instance movement modifier and inexpensive cyan sprite feedback. Slow does not alter `EnemyDefinition.MoveSpeed`, paths, checkpoint progress, spatial indexing or navigation revisions. Shields do not block Slow. Death, arrival and scene exit clear runtime effects.

## Architecture choice

`COMBAT/StatusEffects/` owns typed identity and immutable attack payloads; `EnemyStatusEffects` owns compact active instance storage and the Slow family resolution policy. `Enemy` exposes application/query/effective-speed methods and lazy ownership, rather than one field and timer per effect. Ordinary enemies allocate no status holder. The first successful Slow allocates a holder and small list; most affected enemies need one entry. Lookup/application is O(active statuses), normally one or two, not O(all defined effects). Movement reads a cached multiplier in O(1), with no tick, node, dictionary, timer, LINQ or per-frame allocation for statuses.

An enum is sufficient for C#-authored identities and Inspector immunity lists. Avoid string comparisons in combat or flags with a fixed maximum number of effects. Definitions declare `StatusImmunities`; runtime state never belongs to a shared Resource. Immunity is distinct from shield response and from targeting. Cryo excludes every enemy already carrying Slow, even a weaker Slow, and excludes immune enemies. If none qualify it waits; there is no damage-only fallback.

Dedicated Slow/Freeze/Burn fields on every Enemy would grow a god-class. A generic plugin/factory registry and per-enemy dictionary are unnecessary for one implemented family. Instead the compact typed collection is the extension seam: add a family-specific resolver when a second family exists, and dispatch identity through a small shared handler table when warranted. Do not copy Slow-specific behaviour into tower scripts or grow one large conditional in Enemy. Effect-family policies own strength/stacking/refresh; the holder owns storage and cached aggregate modifiers. Current permanent Slow needs only strongest-wins replacement; zero/default/invalid payloads are rejected.

## Adding Stasis later — design contract only

1. Add Freeze identity and its immutable duration payload, with a separate explicit immunity entry. Store its runtime remaining duration alongside the existing Slow; do not replace or temporarily mutate Slow.
2. Resolve movement as **Freeze gate × strongest Slow multiplier × base speed**: zero while frozen; on expiration the earlier 0.67 multiplier returns. Equal/weaker Slow never stacks. For future Freeze reapplication, retain the later expiry (maximum remaining/new duration), not additive duration. Specify alternative policies per future family explicitly.
3. Add one world-scoped scheduler processing only enemies with timed effects, using reusable buffers and lifecycle-safe registration/removal. Pause-aware elapsed seconds expire Freeze even when the enemy is stationary; permanent Slow is never scheduled. Expiration changes cached modifiers only, never requests A*. Terrain continues its independent hazard clock.
4. Reuse shared projectile delivery and targeting with a Freeze payload; no movement-engine rewrite. Freeze visuals remain presentation only.

Stasis, timed scheduling and DoT are **not implemented**. Future DoT entries carry their own `DamageSource`, interval and remaining tick schedule, use the same active-only scheduler, and call `Enemy.ApplyDamage(DamageInfo)` for actual-damage/kill XP. Do not infer attribution from the last direct hit. Cleanup unregisters timed entries on death/arrival/unload. Effect application itself awards no XP. Source towers may disappear: effects remain valid while attribution follows existing weak-source rules.

## Visuals and verification

Slow sets only Sprite2D.SelfModulate, multiplied by the original colour; existing Modulate and DefenseBar remain untouched. No particles or extra effect nodes. Future concurrent tint effects should compose in a presentation resolver rather than overwrite this channel independently.

See `TESTS/Towers/CryoTowerChecks.tscn` for shared-definition isolation, exact 100→67 movement, equal/weaker/stronger applications, immunity, launch snapshots, lethal cleanup, route and Lava behavior, and 500-enemy allocation smoke checks. A small speed-read microbenchmark is not a whole-game FPS measurement. Future Stasis ordering above is a documented contract, not a claim of executed Freeze tests.
