# CTX1 focused owner checks

**Authored / not executed:** no .NET executable in this environment. Source/resource/SVG XML/diff checks and a local native-art render only; no gameplay/performance acceptance.

Optional F6: `CorruptorTowerChecks.tscn`. The isolated fixture uses the actual registry, physical projectile, damage resolver, timed scheduler and weak DamageSource. It covers launch snapshots and8+24 hull damage despite shields, pause, Freeze/Slow coexistence and Freeze expiry while DoT remains, hitch/expiration accounting, weaker duration refresh preserving stronger DPS/tick progress, unaffected-first targeting with affected fallback, strict Cryo exclusion, immunity, exact normal damage+kill XP, single Credits/kill count, another tower's killing blow, removed-source/replacement ownership, lethal-tick removal of a different scheduled enemy, reinitialization generation safety, arrival cleanup and empty-clock sleep.

Owner gameplay checks remain: all six target priorities (fixture samples the four path-independent modes), terrain/stealth selection, real construction and paid upgrades, art at native size/normal zoom, physical muzzle/tracer and impact, power/ammo/heat, damage/repair-based selling and a fresh between-wave save/load. Existing save combat restrictions forbid live enemies, so no new DoT save fields are required. No broad suite or headless simulation was run.
