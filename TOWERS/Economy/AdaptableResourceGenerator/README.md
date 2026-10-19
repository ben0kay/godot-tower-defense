## ARG automatic income correction — 2026-10-19

ARG eligible kills now grant Credits directly to the existing wallet: no collection button, stored income or storage limit. The Resource Tower and Citadels retain manual collection. A single mouse-transparent green floating number beside each ARG combines rapid successful payouts (+1 → +2 → +5), then floats/fades away after a quiet interval (default 0.8s). Popup duration/color are editable on ARG config. Higher tiers still earn 2/3 per eligible kill. Full-wallet rejection displays no false income and is not banked. Existing kill eligibility, pause/construction/upgrade gates and ordinary kill rewards/XP remain intact. ARG previews omit unused storage. Earlier buffered-ARG descriptions below are historical and superseded by this correction.

---

# AdaptableResourceGenerator — implemented

## CP2 + CP3 economy towers — implemented 2026-10-19

Both towers are registered in the Ctrl build tray's **Economy** category, with muted dollar-bill greens and distinct original SVGs. They use the existing placement/construction/health/paid-upgrade/Inspector flow. Select one and press **COLLECT … CREDITS** in its Inspector; world clicks still select towers normally. Wallet grants remain centralized, all-or-nothing and once-only. Failed/full-wallet collection leaves storage intact.

| Purchased tier | Resource Tower price | Credits / 5 active seconds | ARG price | Credits / eligible kill | ARG range | Storage (both) | HP (both) |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 350 | 10 | 300 | 1 | 256px | 100 | 150 |
| 2 | 250 | 15 | 250 | 2 | 288px | 150 | 200 |
| 3 | 350 | 20 | 350 | 3 | 320px | 200 | 250 |

Prices for tiers2/3 are upgrade purchases, not cumulative costs. All work takes2 seconds initially. Resource Tower footprint **2×2**, ARG **1×1**. Balance is provisional. Edit each `Default…Config.tres` → **Credit Production / Production Levels** for absolute tier prices, capacity, payout, interval, range, hull and work duration before deployment. The nested tier resources are cloned per placed tower; existing instances retain private snapshots. Tier capacity must not decrease. Identity/art/footprint remain in the standard tower Definition. ActiveWavesOnly defaults true on both; turning it off permits between-wave income while the mission is Playing.

**Resource Tower:** shared CreditProductionTimer runs only while operational, not busy/disabled/paused, mission Playing, and (by default) waves Spawning/SpawnBlocked/WaitingForClear. Fractional progress freezes between waves/while disabled. Full storage accumulates no backlog. Each purchased upgrade preserves stored Credits, increases capacity and starts a fresh periodic clock with the new rate; no retroactive payout.

**ARG:** listens to the authoritative registry's new AttributedKill event, published before ordinary EnemyKilled listeners can finish the wave. Enemy records only the actual lethal DamageSource, not last nearby/last nonlethal attacker. The weak owner token must resolve to a live, nonqueued, nondestroyed Offensive tower in the same world. Range is measured inclusively to that **killing tower's position at kill time**, never the victim. Every eligible overlapping ARG earns independently. Legacy/environmental/unattributed kills and arrivals do not qualify. Already-emitted hits can still belong to a disabled living offensive source; disabled/incomplete/upgrading/destroyed ARGs do not earn. The final wave kill earns normally. Existing global kill Credits, finite damage/kill XP and carrier lifecycles stay separate. The immutable source token also supports future delayed/DoT payloads; no DoT tower was added.

**Shared foundation:** `TOWERS/Economy/Shared/CreditProduction/` adapts editable producer tiers into optional immutable Production snapshots, without pretending producers have a weapon/ammo/rank progression. Inspector upgrade comparison shows storage/payout/interval/source range, rather than projectile rows. CreditProducerTower owns collection/lifecycle/timer; ARG is event-only with no physics tick when periodic income is absent. Resource Tower has one simple gated timer callback. No per-frame tower/enemy scans, extra currency wallet or per-tower UI buttons are created. CreditBuffer gained safe capacity growth for upgrades; CP1 behavior is retained.

**CP1 status:** user reported successful manual playtesting on2026-10-18; automated CitadelCreditChecks and CitadelCollectionChecks also pass in this job. The legacy pending-status sections below are historical.

**Deferred:** Generator.2 passive ARG research (+20/10s) and New Interest Citadel research (+25%) remain planning, not silently purchased/unlocked. No research ownership/effects or campaign save format was changed. CP3's passive tower is implemented; its optional research portion remains future work. Existing maps/waves/combat stats were not edited. See `TESTS/Economy/CreditTowers.md` for validation and limits. Original plans below are retained.

---
