## CTX1 — Corruption and safe shared DoT ticking

**Source Implemented / Awaiting Owner Validation.** Corruption joins Slow/Freeze using the same enemy-owned compact entries and lazy world clock. Each periodic entry stores its immutable total duration/DPS/source, double remaining duration and accrued time. Exactly one entry per identity; refresh uses new duration, maximum current/new DPS, latest source and preserved accrued time. No status application itself awards XP.

TimedStatusScheduler now uses O(1) registration/swap-removal and a retained versioned step buffer over active timed enemies only. Death, removal of another enemy, world exit or reinitialization during damage callbacks cannot invalidate iteration, skip a survivor or advance a new generation using an old step. New registrations wait until the next scheduler step. No per-frame temporary collections, enemy-registry scan or per-enemy Timer/process. Empty clocks sleep.

Normal damage ticks every1s; frame hitches aggregate whole due seconds, and expiration pays a final partial second. Enemy.ApplyDamage remains the sole damage/kill/XP path, with full shield bypass and explicit1:1 hull damage. Status state is committed before callbacks. Weak DamageSource permits continued damage after tower removal without XP for a dead/replacement owner. Pause stops elapsed time; Freeze/Slow do not. Slow/Freeze family policies are unchanged; Freeze expiration now refreshes movement/tint even while another timed effect remains.

Successful Corruption applications use one short shared impact ring plus a composited purple tint; no infected-enemy particle system. Death/arrival/exit/reinitialize unregister and release all state. Between-wave saves still disallow live enemies and require no schema addition. Focused owner fixture: TESTS/Towers/CorruptorTowerChecks.tscn, authored but unrun (.NET unavailable). Historical DoT-unimplemented wording below is superseded.

---

## Stasis ST1 — implemented 2026-10-18

Green-themed Stasis is now a catalog-registered Offensive Energy projectile tower. It uses the existing shared muzzle/projectile, construction, physical ammo, hull HP, purchased-level, earned-rank, stat pipeline and Inspector paths. No Inspector layout changes. Targets must be unfrozen and Freeze-nonimmune; all six selected modes score only those eligible candidates. If none qualify, wait with no damage-only fallback. Cryo-slowed enemies remain eligible. Existing in-flight shots can refresh Freeze with maximum remaining/new duration, never additive duration.

Freeze is separate from Slow and damage type. A lazy registry-owned TimedStatusScheduler processes only active timed enemies using retained list storage, respects pause, unregisters on death/arrival/exit/reinitialization and sleeps when empty. Cached movement is zero while frozen; expiration restores the retained strongest Slow × base speed. No route reset, new A* request, per-enemy timer or permanent-status ticking. A shared tint resolver composes cyan Slow/green Freeze; effects survive source tower removal without retaining its reference. Status application grants no XP; real Energy damage/kill attribution follows existing projectiles.

The original Cryo architecture and Stasis design contract below are historical context; ST1 now fulfills the Freeze contract. EnemyStatusEffects retains compact typed active entries with family-specific strongest-Slow/max-duration-Freeze policies. Timed runtime remaining duration belongs to those entries, never shared Resources. DoT remains unimplemented.

---

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
