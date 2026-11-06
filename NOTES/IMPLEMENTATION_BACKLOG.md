# Implementation Backlog — Outstanding Work Only

**Repository:** `ben0kay/godot-tower-defense` · **Branch:** `dev` · **Audited:** 2026-11-06 · **Source baseline:** [`e1a68dd77f`](https://github.com/ben0kay/godot-tower-defense/commit/e1a68dd77f90e1c8f90cad5d24c314caef098eb0)

This is the **live unfinished-work index**, not a cumulative changelog. Completed/source-delivered phases have been **removed**, including the old dated receipts and duplicate 26-area status table. Their implementation history remains in [WORK.md](WORK.md), the [root README](../README.md) and owning feature documents. Read actual `dev` source before scheduling any job; this snapshot can become stale.

**Status definitions:** **Planned** = an existing phase/feature guide, not yet fully implemented; **Needs scope/approval** = design decision or idea, not permission to code; **Owner QA** = source already delivered, not an unimplemented pass. A source commit is *not* automatically in-game acceptance. These rows are recommendations for **remaining work**, not blanket authorization.

## 1. Implementation-ready or already-scoped next slices

These are the best-defined outstanding tasks. Priorities are suggested order for future planning, **not** an instruction to implement everything at once.

| Suggested order | Remaining work | Exact next slice / boundary | Status and fit |
| ---: | --- | --- | --- |
| 1 | **FD3 — Fighter Auto-patrol / selected-drone controls** | Select a fighter, switch Auto-pilot vs Auto-patrol, place two A/B map points, preview/cancel/edit its figure-eight circuit and leash. Keep FD4.1's independent nearby shooting and FD5's finite service/relaunch working in patrol mode; avoid tower-placement click conflicts. [Fighter plan](../DRONES/Combat/FIGHTER_DRONES_IMPLEMENTATION_PLAN.md). | **Planned** · Codex |
| 2 | **WP2–WP7 — Wave Editor quality-of-life** | Small independent passes: WP2 ship sprite + real stats preview; WP3 actions atop lists; WP4 accurate wave composition/schedule summary; WP5 explicit renumber; WP6 Save As/duplicate wave set; WP7 enemy-ID/name search. Do **not** rebuild the editor. [WP plan](../addons/wave_editor/PLAN.md). | **Planned** · Regular / Codex |
| 3 | **PW6-C — power source/network Inspector polish** | Show usable generator output, physical-island/shared-network supply, actual demand/deficit and clear source efficiency; adjust provisional Solar/Fusion/Industrial balance **after** owner playtest. No new power manager, auto-priority slider or duplicate island-hover overlay. [Power plan](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md). | **Planned, balance needs playtest** · Regular / Codex |
| 4 | **R3/R4 — Results data and reward rules** | Replace `—` Credits-earned/spent fields with authoritative run totals; then separately decide first-clear/replay XP, anti-farming, mission-time/score bonuses and enhanced level-up celebration. Preserve official wave counts, current saved XP and existing skippable stat count-up. [Results](../UI/Menus/Results/README.md). | **Partial future scope** · Regular / Codex |
| 5 | **Research growth + campaign unlock ladder** | Author more approved research effect nodes, prerequisites and permanent unlocks; choose first appearances of enemy types and tower access across missions. Research loadout/doctrines, replay policy and story routes need separate design. [Research](../SYSTEMS/Progression/Research/README.md), [progression](../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md). | **Planned expansion / balance decisions** · Codex |
| 6 | **E3 — remaining organic terrain/material edges** | Opt-in further rock/material transitions built on the existing shared edge/mask system; keep map-independent and test shader/GPU cost. [Terrain plan](../WORLD/TERRAIN/Edges/IMPLEMENTATION_PLAN.md). | **Planned** · Regular / Codex |
| 7 | **VE3 / VE4 / VE5 / VE6 / VE7 — environment atmosphere** | VE3 material/foundation accents; VE4 regional ambience; VE5 local 2D lighting; VE6 opt-in steam/lava/smoke/vents; VE7 event-driven battle atmosphere. Use separate bounded passes. **VE5C bloom was reverted: do not restore it.** [Backgrounds](../WORLD/BACKGROUNDS/README.md). | **Planned** · Regular / Codex |
| 8 | **Player Options/settings** | Replace the Options placeholder with actually persisted/configured relevant audio/video/UI/accessibility controls, one small pass at a time. [Pause menu](../UI/Menus/Pause/README.md). | **Planned** · Regular |
| 9 | **Further positional/gameplay audio** | Prioritize missing enemy weapon, shield impact/break, level/rank and wave event clips; add volume/mix/polyphony settings and optional more mission briefing voice without duplicating shared audio ownership. [Audio](../AUDIO/README.md). | **Partial expansion** · Regular |
| 10 | **Floating damage numbers and camera impact feedback** | F02: pooled/merged shield/hull/critical values with explicit caps. F08: restrained camera/Citadel response with bounded shake. Keep independent, cosmetic and optional. [Damage numbers](../FEEDBACK/DamageNumbers/README.md), [camera effects](../FEEDBACK/CameraEffects/README.md). | **Needs scoped pass** · Regular |
| 11 | **Magazines / reloads / specialized ammunition** | Optional, per-weapon reload and finite magazine rules layered over the existing Factory → Depot → consumer courier network; decide balance first. [Ammo](../TOWERS/Shared/Ammunition/README.md). | **Needs scope/approval** · Codex |
| 12 | **FD6 — fighter final integration after FD3** | Only *remaining* patrol-order checkpoint persistence and final owner-led flight/service/throughput/AA-seam review. Current fighter HP/ammo, real station stock, courier jobs, service progress and between-wave snapshots already have implementations; **don't rebuild FD5 or S0–S3**. [Fighter plan](../DRONES/Combat/FIGHTER_DRONES_IMPLEMENTATION_PLAN.md). | **Later integration / owner QA** · Codex + owner |

## 2. Gameplay features that need decisions before Codex implements them

| Area | Unfinished decision / first safe scope | Status |
| --- | --- | --- |
| **Tower shields** | Decide friendly shield capacity, absorption order, recharge/disable interaction, damage feedback and checkpoint persistence; hull repair is not shield repair. [Durability](../TOWERS/Shared/Durability/README.md). | **Needs approved plan** |
| **RT3/RT4 Restoration extensions** | RT3: optional friendly-target priority selector and whether restoration earns support XP. RT4: separately scope shield repair, friendly drone repair, multi-beam or material supply. Purchased L2/L3 and heat/power are *not* tasks here. [Repair plan](../TOWERS/Support/RepairTower/IMPLEMENTATION_PLAN.md). | **Optional / needs owner selection** |
| **SPAWN5 surprise events** | Optional bounded authored reinforcements/one-shot triggers on top of existing spawn sources and parallel schedules; agree timing, retries, outcome ownership and saves. [Spawning plan](../ENEMIES/Spawning/IMPLEMENTATION_PLAN.md). | **Deferred, design later** |
| **Remaining Wave Phase 3 UI/rewards** | Wave preview/history, special elite/boss announcements, completed-wave bonus Credits, scripted events only if desired. Normal auto-next-wave countdown and parallel batch starts are *not* missing. [Waves](../LEVELS/Waves/README.md). | **Needs scoped passes** |
| **Advanced enemy attacks and support** | EMP/electrical attacks, healing/commander buffs, jamming, multi-stage Capital bosses and new Titan/Colossal hulls, one opt-in enemy/ability at a time. Four-gun Capital mounting is not the missing component. [Enemies](../ENEMIES/README.md). | **Needs specific enemy designs** |
| **Projectile collision with mountains** | Specify what happens to **already-fired** friendly/hostile rounds when shooter or target moves behind terrain; separate impact/collision policy from existing shot-time mountain LOS. [Grid](../WORLD/GRID/README.md). | **Needs collision contract** |
| **CW2 — hostile splash/recoil follow-through** | Optional hostile splash damage/projectiles and their performance boundaries. CW1 recoil sharing itself is complete in source. [Shared recoil](../FEEDBACK/Weapons/Recoil/README.md). | **Needs approval** |
| **Armor/resistance tuning** | Add any per-enemy typed armor/mitigation and vulnerability rules only after a single damage-order/balance contract. [Combat](../COMBAT/README.md). | **Needs balance design** |
| **New tower/economy roles** | Missile Tower, Radar Tower (must differ from Stealth Beacon), Scrap Refinery, or a genuinely different hybrid solar role. Start with one gameplay role and use existing tower/catalog infrastructure. [Missile](../TOWERS/Offensive/MissileTower/README.md), [Radar](../TOWERS/Support/RadarTower/README.md), [Scrap](../TOWERS/Economy/ScrapRefinery/README.md). | **Ideas / not approved** |
| **Campaign/new mission content** | Decide enemy introduction sequence, later maps and what the player unlocks. New authored missions or extra waves should be requested deliberately; Level011's existing ten-wave layout is not a missing phase. [Authoring](../LEVELS/AUTHORING.md). | **Content design** |
| **Persistent fighter anti-air** | Decide which enemies can target fighters, damage/repair economics, combat registry and how they intercept without affecting towers or cargo drones. **Not part of delivered FD5.** [Fighter plan](../DRONES/Combat/FIGHTER_DRONES_IMPLEMENTATION_PLAN.md). | **Separate future feature** |

## 3. Performance/owner-validation queue — not missing implementations

Run these as **short owner-driven Godot checks**, then file a focused bug only if a real failure is observed. Do not regenerate an entire foundation to satisfy a validation row.

| Verify | What still needs evidence |
| --- | --- |
| **Fighter FD4.1 + FD5** | Fighters shoot other valid nearby targets without abandoning movement anchors; station stock draws genuine Depot shipments, correctly pauses/resumes service and restores FIFO/progress/paid repair across checkpoints; lots of fighters don't spike frame time. |
| **SPAWN3 + SPAWN4** | Weighted mixed sources/sectors/portal exclusions; overlapping parallel batches, independent blocked-source retries, Wave Editor preview and checkpoint/retry. **SPAWN5 is design-only.** |
| **Power PW4–PW6 and island-hover telemetry** | Shortage allocations, island links, Relay coverage, Solar/Fusion footprints, selected vs hovered metrics, capacitor starvation, EMP/removal and recent power-resource fixes. |
| **Level011 / Wave Editor / automatic next waves** | Ten-wave + Portal4 debut pacing and balance/FPS, Wave Editor edit-save-duplicate, 30s or authored per-wave countdown, victory hold and wave-zero resume. |
| **Combat/Restoration/Capital** | Heat under truly funded repair, overlapping disables, TS1 health-scaled sale, four muzzle LOS and recoil alignment, Shield Emitter range/radius, projectile visual hit consistency. |
| **PF0/PF3–PF5 + EG4 measured load** | Capture matched Level100 **400/500-ship** and real armed-fleet CPU/GPU frame timings; select one PF4 improvement only from measurements, then verify PF5. Existing PF1 fixes/PF2 instrumentation are not new jobs. [Performance](AGENT_FINDINGS/Performance.md). |
| **Presentation, audio and saves** | Portal smoke/opening/shader culling, construction percentage/circular telemetry, drone travel, voice/typewriter, Cryo/Battle/MG audio, results XP/stat reveal and S0–S3 Continue. Check small/large viewports; don't treat visual acceptance as new source. |

**Testing policy:** Owner handles real Godot editor/playtest and FPS acceptance. Codex/regular ChatGPT should use a focused source review and at most one practical basic build unless a targeted issue requires more. A pending in-game check is **not** reason to reimplement a shipped phase.

## 4. Idea intake — unapproved, not forgotten

The canonical original idea IDs and fuller wording remain in [Ideas.md](Ideas.md). The following rows list **only the still-open part** of each idea. No idea authorizes a broad implementation on its own.

| Idea IDs | Outstanding concepts (exclude shipped foundations) |
| --- | --- |
| **F01, F04** | Additional shield ripple/explosive hit-family polish; scalable small-ship fragments and staged large-ship/boss wreckage. |
| **F02, F03, F05** | Damage numbers; optional richer upgrade/rank glows; unique elite/boss wave banners. |
| **F06, F07, F08** | Overheat/damage venting and sparking art; cosmetic kill streaks; additional Citadel hit reactions. |
| **F09, F10, F11** | Selected tower current-target reticle/line; priority/de-duplicated event feed; aggregated real DPS/uptime/kills Inspector telemetry. |
| **F12, F14, F15** | Extra audio reactions; any **additional** construction finish flourish; compact post-wave mini-summary. |
| **M01, M02, M03, M04** | True combat tower synergies; new enemy healing/command/jamming support; player battlefield abilities; upgrade specialization branches. |
| **M05, M06, M07** | Carrier launches **during flight** (not death-release), boss HP phases, additional per-enemy vulnerabilities. |
| **M10, M11, M12, M13** | Previewed random mission modifiers; research loadout slots; salvage drones; gameplay-changing environment hazards. |
| **M14, M15, M16** | Active tower abilities, formation leaders/wingmen, and optional endless survival mode. |
| **Other optional economy concepts** | Tower wear/maintenance, future salvage/refund modifiers, specialized fuel/raw resources and separate Citadel maintenance drones (if separately planned). |
 
**Do not re-add as missing work:** `F13` portal activation, `M08` basic six tower-target modes, `M09` standard ammunition deliveries, or any already delivered subpart of a partly-open idea.

## 5. Documentation accuracy debt — Markdown-only follow-up

Feature guides contain older historical “planned/not implemented” passages below newer implemented updates. These are **not** new engine work; fix their status only when touching that feature, or perform one separate Markdown-only cleanup later.

- [ ] Refresh older root [README](../README.md) controls/F2/catalog, mission/research/save descriptions, and old fighter/spawning entries against latest source.
- [ ] Correct [LEVELS/README.md](../LEVELS/README.md) and [Performance findings](AGENT_FINDINGS/Performance.md) references to retired five-wave Level100 recipes; current benchmark is two 400/500-ship waves.
- [ ] Mark obsolete planned-only paragraphs as **historical** where contradicted by source in [fighter plan](../DRONES/Combat/FIGHTER_DRONES_IMPLEMENTATION_PLAN.md), [spawning plan](../ENEMIES/Spawning/IMPLEMENTATION_PLAN.md), [wave README](../LEVELS/Waves/README.md), [power plan](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md) and [Restoration plan](../TOWERS/Support/RepairTower/IMPLEMENTATION_PLAN.md).
- [ ] On a targeted pass, reconcile old “not implemented” prose in enemy weapons, construction drones, wave narration, terrain edges, environment, old recoil/muzzle/projectile guides, Results/progression, Economy production, Campaign, Saving, World, tower roadmap and Audio. **Do not delete design notes or rename stable paths.** See current source and [WORK history](WORK.md).
- [ ] Label the older “Consolidated Outstanding Implementation Backlog” sections inside [WORK.md](WORK.md) as historical snapshots, or point them here; preserve chronological implementation receipts.

## 6. Work boundaries / maintenance

- **Not approved by this list:** optional `PowerPriority` slider, intentionally reverted VE5C bloom, new mid-combat or multi-slot saving, speculative empire/shooter integration, or sweeping balance changes. Get separate approval before scoping any of these.
- **Never confuse QA with missing mechanics.** Only add tasks that still lack source or a specific confirmed bug fix. Source-implemented phases should remain in feature documents/WORK history, **not migrate back into this backlog** after a future audit.
- For a chosen job: read [CodePreferences](CodePreferences.md), root README, owning feature docs and fresh `dev` source; implement one clear slice; keep gameplay/save/performance boundaries; note the implementation briefly in WORK; commit and push `dev` only. This file records remaining work but grants no blanket build authorization.
