## NP1–NP3 — per-wave tactical narrator and wave alerts (PLANNED, 2026-10-20)

Codex handoff: [LEVELS/Waves/Presentation/README.md](../LEVELS/Waves/Presentation/README.md). Existing wave definitions and notification manager are ready to extend, but **the actual wave-start banners, narrator typewriter UI and integration are not implemented yet**.

- [ ] **NP1:** optional immutable `WavePresentation` authored beside per-wave enemy batches; backwards-compatible `Wave()` overload and once-only `WaveManager.WaveStarted` event.
- [ ] **NP2:** shared battlefield presentation controller, dynamic banners through the existing major-alert queue, new configurable/pause-aware tactical NarrationDisplay and optional future voice ID; Level020 text only on waves1/4/6/7.
- [ ] **NP3:** compatibility, null-text, timing, interruption, pause/retry/unload and UI layout tests; document real runtime verification.
- No pre-wave countdown, voice playback, combat changes or level composition changes in the first implementation.

---

## EG1–EG4 — Armed Gunner Drone with muted-red minigun bullets (PLANNED, 2026-10-20)

**Codex handoff:** [`ENEMIES/Weapons/README.md`](../ENEMIES/Weapons/README.md). Design approved; code and tests **not yet implemented**. First hostile weapon: a new F2/catalog Gunner Drone, moving along its assigned route while an independent single minigun attacks nearby placed towers with small **7px-long, 1.3px-thick muted coral-red** unguided tracers and tiny orange-red muzzle flash; editable visual/pacing presets. Regular enemies and authored waves remain unchanged.

- [ ] **EG1:** optional enemy weapon mount/config, staggered spatial tower targeting, independently rotating barrel.
- [ ] **EG2:** central fast unguided hostile-bullet state/rendering and swept first-tower impact via existing `TowerBase.ApplyDamage(DamageInfo)`; no per-bullet scene nodes.
- [ ] **EG3:** new Gunner Drone inherited enemy scene, standalone `.tres` and SVG art, red tracer/muzzle presets, F2 catalog registration, manually test tower firefights.
- [ ] **EG4:** profile pooled/batched hostile bullets under realistic large waves; later reuse for multi-gun Cruisers. Preserve enemy routing, stealth, tower death/cleanup, pauses, targeting and performance.
- [ ] Godot compile/headless tests and in-game visual approval of **small slightly red** tracers; document genuine pass results before checking phases complete.

---

## Offensive projectile recoil rollout — source implemented (2026-10-20)

User approved extending the successful Battle recoil preview. Machine Gun, Sniper, Blaster Cannon, Rocket, Cryo and Stasis now have split fixed bases, recoiling gun/muzzle assemblies and independent Inspector-editable presets using the shared controller. Existing composite icons are retained. Laser and nonrotating Shockwave retain their beam/radial presentations; no forced rotation or directional recoil was added to either. No balance, wave, UI/Inspector resize or progression changes.

Updated regression fixtures cover funded origins, fixed base/aim/root, independent emitted flashes/shots, cooldown/ammo gates, generic disable/destruction and adoption boundaries; older muzzle path assertions are updated. Static resource/path checks completed. **Godot import, C# build, headless tests and desktop visual acceptance remain pending** because no execution environment was available. Historical Battle-only R1/R2 entries below remain as pilot history. See TOWERS/Shared/Weapons/Recoil/README.md and TESTS/Towers/TowerRecoil.md.

---

## R1 + R2 — combined Battle recoil source implementation (2026-10-20)

User approved combining both passes: fixed base, split gun and attached recoiling muzzle, with one optional shared controller/style and a Battle-only preset. Source implementation, regression fixtures and documentation are complete; **Godot import/build/tests and normal-zoom visual approval remain pending**, not passed. No combat balance values or other tower scenes were changed. Existing construction art remains an overlay; optional monolithic custom tier/upgrade artwork retains that fallback with split recoil suspended. See [recoil implementation notes](../TOWERS/Shared/Weapons/Recoil/README.md) and [verification instructions](../TESTS/Towers/TowerRecoil.md).

The older “NOT implemented” R1 entry below is retained as planning history and superseded by this combined-pass entry. R1/R2 need runtime validation before acceptance; broader tower rollout remains future work.

---

## Recoil pilot for Battle Tower — Codex plan, NOT implemented (2026-10-20)

The planned reusable, optional projectile recoil system is specified in [TOWERS/Shared/Weapons/Recoil/README.md](../TOWERS/Shared/Weapons/Recoil/README.md). Codex R1: create shared `TowerRecoilController`/`TowerRecoilStyle`, author one Inspector-editable Battle preset, trigger only on an actual funded Battle shot **after muzzle launch/flash**, keep all existing sprite/muzzle/construction paths and other towers untouched, and add regressions. First preview may lightly offset the existing composite Battle SVG; *fixed base + split gun* is an explicit later R2 art pass, not yet authorized. No implementation exists until Codex completes R1.

---

## SE1 — stealth enemies and reveal beacon — implemented 2026-10-20 UTC

Waves may now author `Enemy("basic_mk1", stealth: true)` independently of `shield: true`. Default spawns remain ordinary; no existing wave/map was changed. Stealth ships render at30% alpha, revealed ships at100%, with configurable/dimmed defense feedback that composes with Cryo/Stasis. All aimed towers share detection filtering and cached-lock validation; private TowerConfig.CanDetectStealth opts a specialist in without globally revealing enemies. Existing attacks already emitted, splash and untargeted Shockwave remain able to damage hidden ships (stealth is targeting, not immunity).

New Ctrl / Utility Stealth Beacon:250 Credits,1×1,2s build,180 HP,256px range, one paid tier. It reveals enemies inside range for all towers; overlap, exit/re-entry, disable/reactivation, construction, destruction, relocation, enemy removal/reuse and world teardown are source-owned. One lazy spatial reveal index listens to enemy motion only while beacons are active; no per-frame enemy scans or per-beacon processing. See ENEMIES/Shared/Stealth, FEEDBACK/Stealth, TOWERS/Shared/Detection, TOWERS/Utility/StealthBeacon and TESTS/Stealth READMEs. Existing Inspector layout/resize is retained. Earlier blanket “stealth/detection future” notes below are historical; stealth preference scoring, Radar, tower-grant detection beacons and further content remain separate future work.

---

## Bottom-docked build tray and Inspector — 2026-10-19 (code complete; tests pending)

- [x] UI-only: both panels flush to the bottom of the viewport with existing side margins; tray nominal **260 → 224px**, Inspector **330 → 252px** while retaining the same 560px Inspector width. Panel content may expand the Inspector upward instead of clipping.
- [x] Build catalog still shows two horizontally scrollable rows with 56px tower buttons, slightly smaller padding and preserved category/hover/selection handling.
- [x] Updated responsive layout expectations in `TESTS/UI/PlacementHudChecks.cs`, `HudCategoryChecks.cs`, `TESTS/Towers/TowerUpgradeChecks.cs` and docs in `UI/GameplayHUD/`.
- [ ] Godot/.NET build and tests: PlacementHudChecks, TowerUpgradeChecks, CompactInspectorChecks, HeatInspectorChecks, HudCategoryChecks; visually check 1920×1080, 1366×768 and 960×540, including two rows, category tabs, Inspector dense states and Ctrl transitions.

---

## Level020 setup-error repair — 2026-10-20 UTC

The seven-wave content update had changed Level020Waves back to a static class, breaking the serialized LevelConfig.WaveSource contract from the shared-level pass. Restored [GlobalClass] Resource inheritance (WaveSetSource) and Build() → Create(); all seven wave definitions, map and settings are retained. Recipes assigned in LevelConfig must remain resource classes; preserve the wrapper when editing batches. Added TESTS/Integration/Level020WaveSetupChecks.tscn to load the actual saved resource and launcher, assert seven Ready waves and successfully start wave1. C# build (zero warnings/errors) and this headless regression passed.

---

## Level020 waves expanded to seven (2026-10-19)

- [x] Level020: authored 7-wave Five Gates assault, **52/68/80/72/72/60/58** ships, beginning with small hordes; regular/heavy Transporters in waves4–5; big armoured enemies and a pair of shielded Cruisers in wave7.
- [x] Level020-specific validation covers real catalogue IDs, seven totals, tier introduction, two guaranteed final Cruisers and unrestricted use of the five configured portals.
- [ ] Desktop balance/playtest and headless Godot suite pending. Existing wave scheduler, Level020 layout/settings, and other levels unchanged.
- Documentation: [Level020/README.md](../LEVELS/Level020/README.md).

---

## Lightweight data-driven levels — implemented 2026-10-19

Shared `LEVELS/Runtime/Battlefield.tscn` + `WORLD/World.tscn` replace per-level service copies. Existing Main launchers and World variants are thin compatibility shortcuts; F6 paths stay valid. Each level now has a MissionDefinition resource and the Campaign roster is `LEVELS/Catalog/DefaultMissionCatalog.tres`. Menu number/title/description/map are data, with a bounded scrollable mission list. New missions deploy/retry directly from their resource; no per-level launcher/world or menu/catalog code is required. Next advances only to the immediate enabled number.

LevelConfig directly references an Inspector-assignable C# WaveSetSource; the three existing wave recipes keep identical Create() content and now implement Build(). WaveCatalog's old central switch is removed; string-ID compatibility resolves via the mission roster. Painting, map/config ownership, portal/Citadel placement, credits/HP/XP values and progression identities are preserved. Explicit off-tree preparation supports the existing multi-Citadel fixture. Citadel destination cleanup is now idempotent across objective/world teardown.

Authoring steps: `LEVELS/AUTHORING.md`; ownership: `LEVELS/Catalog/README.md`, `LEVELS/Runtime/README.md`; validation: `TESTS/Integration/LightweightLevels.md`. This completes shared-launcher/catalog convenience, not a visual wave editor, campaign unlock/save system or hot map switching. Earlier duplicated-launcher instructions below are historical.

---

## Visual atmosphere / reusable shader presets — VE1–VE4 (PLANNED, 2026-10-19)

- **Codex design and source review:** [`WORLD/BACKGROUNDS/README.md`](../WORLD/BACKGROUNDS/README.md#proposed-level-atmosphere-and-shader-presets--ve1ve4-planned). **None of VE1–VE4 has been implemented by this documentation job.** Existing levels use the same static `Stars/level001_space.svg`; the `Nebulae`/`Planets` groups are empty. Existing Terrain Edges are a separate disabled scaffold.
- [ ] **VE1:** reusable gentle star-twinkle overlay, slow nebula drift and optional controlled parallax, Inspector-editable `CalmSpace` preset; retain current static background when disabled.
- [ ] **VE2:** shader/preset-backed subtle Portal core/rim movement and Citadel reactor glow; optional spawn/credit/health event pulses that do not change input or gameplay.
- [ ] **VE3:** sparse opt-in animated metal/foundation corridor lighting, correct atlas UV/material isolation, `IndustrialBlue`/`IndustrialAmber` presets. Prioritize Level020 without repainting existing maps.
- [ ] **VE4:** localized faint Mars dust wisps/industrial accents, distinct from the unfinished Terrain Edges E1–E3 mask renderer; keep camera feedback separate.
- [ ] Test atmosphere off/on on Level001/002/020 and template, Godot shader loading, near/far zoom and 500+ enemy performance; one commit per VE implementation job. No per-tile nodes or per-frame full-map rebuilds.

---

## ARG automatic income correction — 2026-10-19

ARG eligible kills now grant Credits directly to the existing wallet: no collection button, stored income or storage limit. The Resource Tower and Citadels retain manual collection. A single mouse-transparent green floating number beside each ARG combines rapid successful payouts (+1 → +2 → +5), then floats/fades away after a quiet interval (default 0.8s). Popup duration/color are editable on ARG config. Higher tiers still earn 2/3 per eligible kill. Full-wallet rejection displays no false income and is not banked. Existing kill eligibility, pause/construction/upgrade gates and ordinary kill rewards/XP remain intact. ARG previews omit unused storage. Earlier buffered-ARG descriptions below are historical and superseded by this correction.

---

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

## F4 performance overlay + Level002 F2 repair — 2026-10-18

`DEBUG/PerformanceOverlay.cs` is one isolated self-building CanvasLayer script, mounted on all three playable launchers. F4 toggles a debug-build-only panel below top HUD/existing map diagnostics, with throttled frame/engine timing, enemy/projectile-node counts, bounded navigation peak windows/queues and render counters. Click-through, pause-responsive, cached read-only service access; hidden sampling stops. Inspector settings live on the overlay node. See `DEBUG/README.md` for metric meanings and thresholds.

Level002's Debug node and LevelTemplate lacked LevelDebugController/HUD wiring, so F2/F3 were absent there. Both now instance reusable `LEVELS/Debug/LevelDebugTools.tscn`; checkpoint-overlay children and map artwork/coordinates remain intact. Missing optional HUD no longer aborts controller setup. Failed catalog F2 attempts warn with SpawnManager.LastMessage while retaining the next enemy index. Raw map-only F6 still intentionally has no world services: use MainLevel002.tscn F6 or Campaign for gameplay spawning.

Verification: C# build and `TESTS/Debug/PerformanceOverlayChecks.tscn`; detailed scope is in `TESTS/Debug/README.md`. Existing user changes to economy, terrain and tower stats are preserved. Earlier plans/history below remain intact.

---

## Battle, Machine Gun and Sniper doubled projectile speed — 2026-10-18 (CODE COMPLETE; runtime test pending)

- [x] Doubled purchased levels 1–3 for all three: Battle **1,150→2,300 px/s**, Machine Gun **1,221.875→2,443.75 px/s**, Sniper **1,495→2,990 px/s**. No other combat stats or shared projectile collision logic changed.
- [x] Updated exact-value regression expectations in `TESTS/Towers/TowerStatChecks.cs` and `MultiBarrelTowerChecks.cs`, plus the offensive tower READMEs.
- [ ] Run Godot .NET compilation and headless `TESTS/Integration/EnemyIntegrationChecks.tscn`, `TESTS/Towers/ProjectileVisualChecks.tscn` (and usual gameplay checks). These checks were not run by the GitHub editing environment.

---

## Terrain Edges visual system — scaffold complete; Codex rendering pending (2026-10-18)

- [x] Add `WORLD/TERRAIN/Edges/` material/config resources, default-disabled controller/renderer hooks, pure neighbour masks, source2 Mars profiles and integration test foundation.
- [ ] **E1:** Correct Mars atlas cross-cell rock overflow, dark vertical slit artifacts and repeating dirt bands (art only).
- [ ] **E2:** Implement actual masked/organic Mars Dust outer and inner corners, one world-scoped opt-in cached renderer and visual/performance tests. Original opaque square tile pixels **must be masked or replaced**, not merely outlined.
- [ ] **E3:** Extend jagged rock, metal and mixed-material transitions; add variants without changing gameplay metadata or established source IDs.
- Codex specification: [Terrain Edges implementation plan](../WORLD/TERRAIN/Edges/IMPLEMENTATION_PLAN.md). Do not mark E1–E3 complete until tested in Godot.

---

## Shared Credits production + Citadel collection + ARG — CP1–CP3 (PLANNED, Codex)

### CP1 input/UI addition — 2026-10-18 (PLANNED, Codex)

- [ ] Actual **world Citadel click** and **its matching top-centre health HUD cell click** both collect **that Citadel's** stored Credits via one shared credit-buffer / wallet command.
- [ ] Add a **small green Credit bar/amount inside each current health HUD cell** (keep existing one-row responsive multi-Citadel health panels); bind to per-Citadel buffer change events. Both world and HUD representations update immediately.
- [ ] Multiple Citadels: each buffer independently collects; no collecting others, no double payout, no world placement/selection conflicts. Collected Credits may be claimed between waves although only generated during active waves. Tests for one/two/four Citadels, wallet cap, paused wave, empty bar and responsive HUD.
- [ ] Detailed acceptance criteria: [`ECONOMY/Production/README.md`](../ECONOMY/Production/README.md#cp1-interaction-decision--collect-from-world-citadel-or-its-top-centre-hud-cell-2026-10-18-planned). **Do not mark implemented yet.**



- **Full verified-source specification:** [`ECONOMY/Production/README.md`](../ECONOMY/Production/README.md). Existing `EconomyManager.Grant` / HUD rewards must remain authoritative. This is **not implemented**.
- [x] **CP1A (backend code pushed; runtime verification pending):** reusable plain CreditBuffer/production timer; independent Citadel 80-cap / +10 every 2s, wave-active-only generation, pause and destruction gating, collection API through the authoritative wallet and test runner authored.
- [x] **CP1B (code committed; runtime verification pending):** world Citadel click + matching top-centre independent HUD cell, green buffer indicators in world/health HUD, unified collection action, input placement precedence and UI test runner authored.
- [ ] **CP1B validation:** compile/run TESTS/UI/CitadelCollectionChecks.tscn and existing MultiCitadelHudChecks + PlacementHudChecks + CP1A runner; inspect at desktop 1920/1366/960.
- [ ] **CP1A validation:** compile Godot .NET and run TESTS/Economy/CitadelCreditChecks.tscn; record actual results.
- [ ] **CP2** `TOWERS/Economy/AdaptableResourceGenerator/`, gains from real kills credited to other towers inside its radius (prefer event-based attribution, no frame scans), buffered reward + Inspector collection, adjustable balance/limits and no duplicate kill payout.
- [ ] **CP3 (optional later)** integrate planned passive Resource Tower and research modifiers (`Generator.2`, `New Interest`) into the same helper, not separate wallet code. Confirm original old notes vs runtime values before balance work.
- [ ] Add tests for phase-specific UI, multi-Citadel independence, wave pause/clear, kill attribution/overlapping ARGs and existing economy regressions. One commit per implementation job.

---

## Level002 tutorial waves — 2026-10-18 (implemented)

The newly painted Level002 is now playable through **Campaign → Levels → PLAY LEVEL 002**, `MainLevel002.tscn` (F6), and Level001's Next Level result action. Its dedicated `WORLD/WorldLevel002.tscn` mounts the existing user-authored map unchanged. Level020 remains separately playable and unchanged. Level002 Next stays disabled because there is no playable Level003; it never skips to Level020.

`LEVELS/Level002/Level002Waves.cs` defines four modest, manually started waves. Enemy “1–2–3” means the three introductory plain roles: Standard Drone (`basic_mk1`), Swift Interceptor (`scout_mk1`) and Bulwark Drone (`medium_mk1`, asset Enemy004). There is no Enemy003 scene. No shields, support ships, carriers, bosses or extra mechanics are introduced.

| Wave | Standard | Swift | Bulwark | Total | Introductory purpose |
| --- | ---: | ---: | ---: | ---: | --- |
| 1 | 12 | 0 | 0 | 12 | Baseline shooting/building |
| 2 | 10 | 6 | 0 | 16 | Fast, fragile targets |
| 3 | 12 | 4 | 4 | 20 | Slower, tougher targets |
| 4 | 16 | 4 | 4 | 24 | Light mixed finale |

Spawn intervals are 0.65–1.4 seconds, with 3-second gaps between batches. Wave4 uses exact weighted composition; all other batches use one type. Existing Start/Next Wave controls allow unlimited preparation time. Local settings: **1,000 starting Credits**, **1,000 Citadel HP**, existing provisional **500 victory XP / 0 defeat XP**. Both authored portals (`EnemyPortal1`, `EnemyPortal2`) are permitted; map-edge spawning is replaced by portal mode for this level. Counts/timings remain C#-authored for AI-assisted tuning as requested. No map tile, Portal/Citadel coordinate, shared scheduler or enemy/tower balance changes.

C# build and `Level002TutorialChecks.tscn` passed: actual map/scheduler spawned all 72 unshielded enemies and produced victory only after wave4. `ResultsChecks.tscn` passed actual Level001→Level002 navigation, retry, menu and missing-next behavior. The isolated tutorial test kills enemies to verify scheduling/ownership; desktop tower balance/playfeel remains provisional. Earlier unplayable-template notes below are historical and superseded by this section.

---

## Level002 moved to Level020; fresh Level002 design map — 2026-10-18

The Five Gates is now `LEVELS/Level020/Level020.tscn`, with `Level020Config.tres`, `Level020Waves.cs` and WaveSetId `level020`. Its terrain, object layout, wave composition and balance are preserved. Its standalone launcher/world variant are `MainLevel020.tscn` / `WORLD/WorldLevel020.tscn`; Campaign labels/launches Level020 and PlayableLevelCatalog registers numeric ID20. Scene/script UID sidecars are retained across renames. Tests and current documentation reference the moved map.

Fresh `LEVELS/Level002/Level002.tscn` is copied from the current LevelTemplate with a new root name and independent `Level002Config.tres` copied from DefaultLevelConfig. Empty terrain layers and template background/Portal/Citadel/checkpoint overlay are retained. The template itself is unchanged. Open this scene in Godot's 2D editor to design it. It has no authored waves or playable launcher yet and is not offered as a finished Campaign mission. Add those when the map is ready. Results cannot skip the missing playable Level002 to reach Level020; Level001's Next is disabled for now. Level020 remains available directly from Campaign and F6 on MainLevel020.

Earlier Level002/Five Gates references in historical notes below refer to the map now called Level020. No old design plan or map content was removed. Verification: see `TESTS/Integration/LevelReorganization.md`.

---

## Portal/Citadel editor grid snapping — 2026-10-18 (implemented)

Portal and Citadel remain reusable scenes. Shared `[Tool] WorldObjectFootprint` now snaps their roots to the enclosing map's `Terrain/Passable` cell centers when entering the editor tree or moving/duplicating them. Default tiles give centers `32 + 64*n`. It uses the actual TileMapLayer conversion, including tile size and transforms, rather than hardcoding 64px or enabling the editor's global snap. The Citadel root still anchors the upper-left cell of its 3×3 footprint, not its visual center.

**Usage:** build C# once, open a Level scene and drag/duplicate a Portal or Citadel normally. Leave **Editor Placement → Snap To Grid In Editor** enabled (default). Disable per instance for deliberate free placement; enabling it again snaps immediately. Existing off-grid instances align when opened in the editor; save the level to retain the corrected positions. No authored map coordinates or tile cells were rewritten by this implementation. Standalone Portal/Citadel scenes have no enclosing map and keep their original local origin.

Only the root position changes. SpawnExit/ApproachPoints local offsets, footprint size, portal SizeMultiplier, route links, rotation and scale are untouched. No snapping executes during gameplay and no process/physics loop is added; editor transform events are coalesced into deferred corrections. A custom map must expose `Terrain/Passable` with a valid TileSet under an ancestor, matching the existing map structure. Moving a root without such a layer leaves it free rather than assuming an incorrect grid.

Verification: C# build, headless actual-tool editor and runtime checks cover both real scenes, negative coordinates, opt-out/re-enable, 80px tiles, transformed parents/maps, duplication, retained child offsets and standalone origins. See `TESTS/World/EditorGridSnap.md`. Existing planning/history below is retained.

---

## Fleet expansion and enemy families — 2026-10-18 (implemented)

Six new inherited enemy types bring the catalog to **13**. Each has independent SVG, Definition, stable Mk.1 ID and editable balance. Existing seven IDs/stats and authored Level001/Level002 waves are preserved. F2 cycles all 13; F3 picks up the same catalog. The previous seven-ship artwork sheet remains a historical preview of that art pass.

| New ship / ID | Family | Hull | Shield capacity | Speed px/s | Citadel damage | Kill Credits |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Reinforced Drone / `reinforced_drone_mk1` | Drone | 110 | 20 | 100 | 2 | 4 |
| Racer / `racer_mk1` | Interceptor | 20 | 0 | 230 | 1 | 3 |
| Shield Frigate / `shield_frigate_mk1` | ShieldShip | 120 | 350 | 65 | 3 | 9 |
| Heavy Bulwark / `heavy_bulwark_mk1` | Bulwark | 500 | 0 | 50 | 4 | 12 |
| Cruiser / `cruiser_mk1` | CapitalShip | 3000 | 400 | 30 | 12 | 35 |
| Heavy Transporter / `heavy_transporter_mk1` | Carrier | 1400 | 0 | 35 | 8 | 25 |

**Family authoring:** select any enemy `.tres` → **Identity / Family**. `ENEMIES/Shared/EnemyFamily.cs` provides the fixed-value Inspector enum: Drone, Interceptor, Bulwark, ShieldShip, Carrier, Support, CapitalShip, Assault. Append future values; do not renumber existing serialized values. Original types are explicitly assigned: Standard→Drone, Swift→Interceptor, Bulwark→Bulwark, Big→CapitalShip, Elite→Assault, Shield Generator→Support, Transporter→Carrier. Family describes role only, adds no ticking/automatic combat bonuses and does not determine Mk generation, size, spawn order or a future Common/Champion/Elite/Boss threat class. `Elite mk.1` remains a name rather than a threat-class system. Missing family on an old/custom resource defaults to Drone for compatibility; invalid enum values reject during catalog validation.

**Shields:** capacity remains opt-in for every type. Author `Enemy("shield_frigate_mk1", shield: true)` for the intended shield-heavy threat; plain/F2 spawns are unshielded. No passive regeneration or new shield mechanic. Heavy Transporter uses two existing DeathSpawn components: six Standard Drones plus four Swift Interceptors, staggered by independent 0.16s jobs under the shared per-frame queue budget. Both reserve wave ownership before parent removal and inherit destination/checkpoints. No payload on arrival/unload and no living launches. Configs are local to its folder; shared Transporter defaults are unchanged.

**Large hulls:** Cruiser art 168×96; Heavy Transporter 144×100; both retain 24px navigation radius. Art extends beyond clearance, as with Big mk.1. Hull-shaped routing/large-footprint clearance and tight-corridor appearance remain future work. No immunity or weapon behavior is implied by size/family.

**Follow-up planning:** balance and schedule appearances in mission waves; add Mk.2+ as deliberate per-type definitions/art; design boss abilities and threat classes separately. These are deferred, not completed by assigning Family. Existing future plans below remain intact. Verification is recorded in `TESTS/Enemies/FleetExpansion.md`.

---

## Alien robotic fleet artwork — 2026-10-18

Replaced the seven existing enemy SVG hulls with original vector interpretations of the supplied dark alien-spacecraft references. Emphasis is on silhouette and exposed machinery rather than repeating one arrow shape with different colors: forked strike drone, tiny hooked interceptor, broad tusked assault cruiser, large four-lance warship, curved talon-wing Elite, bulbous armored Transporter and rounded split-fork Shield Generator. Dark layered metal, restrained edge highlights and recessed reactors keep them readable against space. Reference screenshot borders/backgrounds are not part of the artwork; uploaded bitmaps are not embedded.

All SVG file paths, native width/height/viewBox and right-facing (+X) convention are retained. No scene transforms, definitions, IDs, collision/navigation radii, health, shields, movement, rewards, abilities, waves or shared shield/status visuals changed. The Shield Generator's circular center is physical reactor/projector machinery, not a baked shield aura. Small hulls remain small; the Transporter retains its existing 112×72 canvas and Big mk.1 its 112×120 canvas. Gradients and layered plates are rasterized at asset import; no blur filters, shaders, extra nodes or per-frame effect processing were added.

Validation: all seven assets parsed and rasterized successfully with Godot 4.7.2's native SVG renderer at their exact original dimensions; reviewed at enlarged and native-relative scale. No C# changes, so no unrelated gameplay suite/build was needed. Desktop in-game style/readability is still subjective and can be tuned after review. The review sheet enlarges each hull independently and lists native dimensions; it is documentation, not a gameplay texture reference. See `ENEMIES/Types/ARTWORK_PREVIEW.png` and `ENEMIES/Types/ART_DIRECTION.md`. This visual update supersedes earlier placeholder/color descriptions while retaining their historical gameplay notes.

---

## MC3 HUD + F3 — implemented 2026-10-18

Multiple Citadels now share **one horizontal row** in the top-center HUD: equal-width cells for two, four or more objectives, never stacked vertically. A one-Citadel level keeps the original nodes/layout/SetCitadelHealth behavior. Each multi meter shows its stable CITADEL number, independent current/max HP, threshold-colored fill and damage-only animation/flash. Zero values and maximum resets are supported; rebind/unload disconnect model events and stop old tweens. The full Citadel number/HP is available in each cell's tooltip. No HUD health polling or gameplay authority is introduced.

UIMasterConfig exposes MultiCitadelBlockWidth (560 default) and CitadelCellGap (6), alongside existing bar height/palette/timing settings. The center remains viewport-centered and capped to the existing responsive center zone (46% of width); Credits and Wave retain separate side zones. Short cells truncate text rather than wrap into extra rows. Very large counts/HP values can require the tooltip for full text. Existing Inspector/build tray geometry is unchanged.

F3 now passes each portal's **actual cached live Citadel identity** into RoutePreviewJob. Raw/refined paths, neutral comparisons and required checkpoint chains end at that assigned objective, not the primary Citadel. Legend/source labels show portal → CITADEL N; destination approach markers and one label per objective use the same stable identity colors/numbers as the HUD. ALL PORTALS keeps the existing shared preview query/time budget and private profile pool. Missing assignments appear as UNASSIGNED diagnostics with no invented fallback route. All destination goals/availability and cached assignments participate in invalidation, including secondary objectives. Closing/Clear releases private preview resources only.

Explicit TerrainRebuilt now refreshes source assignments during the authoring rebuild itself, so F3 reads current cached ownership without triggering a live assignment calculation. Ordinary tower occupancy still cannot reassign portals, and preview settings/simulation never spawn enemies or alter live route queues/profile pools/random streams.

Validation: compiled with the installed .NET SDK/MSBuild; MultiCitadelHudChecks passed one/two/four objective presentation at 1920×1080, 1366×768 and 960×540, independent HP/colors/zero/maximum reset, rebind, actual assigned F3 endpoints, secondary-goal invalidation and live-state isolation. Existing RoutePreviewChecks (both maps/raw/refined/checkpoints/50 samples/500 enemies), MultiCitadelChecks, CheckpointChecks, ResultsChecks, MissionXPChecks, PlacementHudChecks and CompactInspectorChecks passed. See TESTS/UI/MultiCitadelHud.md. Desktop rendered appearance/playfeel still needs user review; headless geometry checks are not a screenshot or full-suite/FPS claim. Existing gameplay maps/waves were not changed. This amendment supersedes earlier MC3-pending/primary-only-preview statements without deleting the original pass plan.

---

## MC1 + MC2 — implemented 2026-10-18; MC3 HUD/F3 now implemented; desktop review pending

Multiple independently damageable Citadels now have stable session destination objects. LevelConfig exposes ordered CitadelPaths; an empty array preserves CitadelPath. Actual additional Health/ApproachPoints Citadels are discovered in authored child order so a second objective is not silently ignored. WorldGrid reserves all physical footprints/approaches. All objectives currently share the level's authored CitadelMaxHealth, but own separate health state; the existing deployment research percentage initializes each one. Changing/adding the Citadel roster requires restarting the level. Geometry/terrain rebuilds can refresh goals and assignments for future spawns; already spawned enemies and released children keep their original identities. Removing an assigned objective retires it: enemies stop, stale distance scores become unavailable, and there is no redirect to another objective.

**Single-Citadel fast path:** assign the sole objective directly, with zero destination-comparison A*. Ordinary non-checkpoint placement retains its original single scratch flood. No per-enemy objective searches, per-frame goal scans, per-Citadel AStar grids or extra movement nodes. Available/retired state is cached through objective lifecycle events, not native-object queries in every scoring/movement call.

**Multi-Citadel policy:** PortalCitadelAssignments compares neutral navigable physical route lengths once at setup/explicit terrain rebuild, with authored-order ties. Selection uses cell-center clearance (radius zero); each actual source/radius is additionally cached and validated against clearance-aware connectivity on navigation revisions. Unsupported or blocked ships fail/retry, never select another objective. MapEdges is disabled on multi-Citadel levels; Both uses authored portals only and reports that edge sources are disabled. Single-Citadel edge/Both behavior is retained.

**Required checkpoints:** assignment minimizes the worst branch's neutral journey using deterministic, reachable canonical checkpoint representatives (first valid cached target in the source component that reaches an assigned approach). This is the documented discrete selection policy, not a claim of globally optimizing continuous circle-entry geometry. Every captured branch must remain connected. Runtime branch choices/random streams and varied routes are unchanged. Journey tail caches include destination identity; normal path requests/repaths use the assigned approach goals under the existing combined query/time budgets. DeathSpawn always inherits Citadel identity, including when InheritCheckpointProgress is false.

**Placement/outcomes:** click-time validation shares component maps per radius, protects each usable portal's assigned goal, all remaining mandatory checkpoint branches and live enemy clearance/remaining obligations. It cannot accept a different reachable Citadel as a substitute. Losing any objective emits a single session event into the existing LevelSessionController defeat/Results/mission-XP lifecycle. Results uses the failed objective's HP, never an aggregate pool that hides failure. Existing HUD still shows the primary Citadel; independent multi-objective HUD/F3 destination labels and desktop visual verification are MC3 work. Existing Level001/Level002 maps, waves and Inspector layouts were not changed.

See TESTS/Navigation/MultiCitadel.md for executed checks, measured setup/batch behavior and limitations. Original MC1–MC3 instructions below are retained; this status supersedes their earlier NOT IMPLEMENTED wording.

---

## Multiple Citadels per level — MC1/MC2 and MC3 HUD/F3 implemented; desktop review pending

**Detailed implementation specification:** [`WORLD/OBJECTIVES/Citadel/README.md`](../WORLD/OBJECTIVES/Citadel/README.md#planned--mc1mc3-multiple-citadels-with-fixed-portal-destinations). **MC1/MC2 and MC3 HUD/F3 implemented with headless integration tests. Desktop visual review remains pending.**

- [x] **MC1:** Add multiple per-level Citadel goals/HP and cached neutral **shortest navigable path** assignments **per portal → one Citadel**, resolved at level setup. Enemy destination is fixed at spawn; no per-enemy Citadel selection, no per-Citadel AStar grid, no target switching on blockage. Existing single-Citadel levels must stay compatible. Decide and test checkpoint-branch, size-clearance and map-edge policies; Transporter children inherit target.
- [x] **MC2:** Extend path/repath/checkpoint and tower-placement connectivity to the **assigned** Citadel; keep corridor diversity/offsets and existing First/Last = smallest/largest `RemainingJourneyDistance` to **each enemy's own target** across Citadels. One destroyed Citadel triggers single mission defeat in initial design.
- [x] **MC3 HUD/F3 implementation:** Independent Citadel HP/HUD, F3 portal→Citadel display, new two-Citadel test fixture, regression coverage (single Citadel, portals/checkpoints/DeathSpawn, building, waves, First/Last, Results/research, 500+ enemies) and Godot headless/F5 verification. Do not change current maps/waves until authored separately.

---

## Stasis ST1 — implemented 2026-10-18

Green-themed Stasis is now a catalog-registered Offensive Energy projectile tower. It uses the existing shared muzzle/projectile, construction, physical ammo, hull HP, purchased-level, earned-rank, stat pipeline and Inspector paths. No Inspector layout changes. Targets must be unfrozen and Freeze-nonimmune; all six selected modes score only those eligible candidates. If none qualify, wait with no damage-only fallback. Cryo-slowed enemies remain eligible. Existing in-flight shots can refresh Freeze with maximum remaining/new duration, never additive duration.

Freeze is separate from Slow and damage type. A lazy registry-owned TimedStatusScheduler processes only active timed enemies using retained list storage, respects pause, unregisters on death/arrival/exit/reinitialization and sleeps when empty. Cached movement is zero while frozen; expiration restores the retained strongest Slow × base speed. No route reset, new A* request, per-enemy timer or permanent-status ticking. A shared tint resolver composes cyan Slow/green Freeze; effects survive source tower removal without retaining its reference. Status application grants no XP; real Energy damage/kill attribution follows existing projectiles.

Provisional level tuning: build350 / upgrade225 / upgrade300 Credits; damage10/11/12; rate0.20/0.21/0.22 shots/s; Freeze3/3.25/3.5 seconds; range239/248/258px; projectile650px/s; local ammunition600, one per shot; hull100. Edit `TOWERS/Offensive/StasisTower/StasisTowerLevels.cs`. Green SVG tower/icon, tracer and muzzle presets are independent from Cryo. This is a test balance, not final progression tuning.

This entry supersedes older statements that Stasis/timed scheduling are deferred or must not be implemented yet; historical Cryo plans below are retained. DoT/EMP/stealth and research modifiers remain future work. See `TESTS/Towers/StasisTower.md` for validation and limits.

---

## Laser shield preference — implemented 2026-10-18

Laser configs now expose **Targeting → TargetPreference**, default **Shielded** (None opts out). This preference is separate from First/Last/Closest/Farthest/Strongest/Weakest: choose eligible in-range enemies with **CurrentShield > 0** first, apply the chosen mode within that group, and fall back to ordinary eligible targets when no preferred candidate qualifies. First/Last retain full remaining-journey distance across portals; Strongest/Weakest retain current hull + shield scoring and stable registration-order ties.

TowerTargeting.Find accepts an optional preference after its existing required-effect argument. One existing spatial traversal ranks preference tiers before ordinary scores; no second scan/list allocation, new per-frame loop or tower-local pathfinding. Other towers default to None. Eligibility/range/finite-route checks still run before preferences; Cryo's required-effect filter is unchanged. Laser reacquires on its existing staggered interval, including after shield depletion. Damage/beam/ammo/heat/rank values are unchanged.

Future stealth preference can add a real matching rule here once stealth/detection exists; it must not bypass detection/eligibility. No speculative stealth flag, detector or UI button is implemented now. The latest HP/ammo Inspector bars and compact-rank visual commit is preserved.

Validation: .NET SDK build (zero warnings/errors); ShieldTargetingChecks passed, including real Laser acquisition/opt-out/config isolation, six-mode preference/fallback, actual two-portal First/Last, shared route-query budget/RNG invariants and 500-enemy route sharing. PlacementHudChecks and CompactInspectorChecks also passed against the latest HP/ammo-bar Inspector revision. See TESTS/Towers/ShieldTargeting.md.

---

## Elite mk.1 plain heavy interceptor — 2026-10-17

- [x] Added `Types/Enemy006` (`elite_mk1`) as a new inherited plain enemy, 80×60 SVG, 1,000 hull HP, 250 optional shields, 100px/s, 24px radius, 5 Citadel damage and 10 Credits.
- [x] Registered in the default catalog after Big mk.1 for F2/F3/waves, with no changes to Level001/Level002 waves, shared enemies, navigation, abilities or combat.
- [x] Updated catalog integration checks and enemy documentation in the same commit.
- [ ] Godot .NET compilation/headless catalog suite and in-game silhouette, shield FX and F2 route test pending.

---

## Stable two-row build browser — 2026-10-17

- Ctrl opening now clears the old tower selection and shows the **empty Inspector shell immediately**. Hover/focus fills the same reserved panel with existing build details/cost/affordability; no tile can be displaced by the panel appearing.
- Tray width **always reserves Inspector width plus the existing gutter**, including while details are hidden. The default 560×330 Inspector scene is unchanged. The tray uses two rows of catalog tiles; further columns scroll horizontally. Category filters recompute columns from visible entries and reset horizontal scroll; keyboard focus follows off-screen tiles.
- The shared BottomBarHeight default is now 260px to fit header, tabs, two 65px tile rows and the horizontal scrollbar. Width remains responsive in UIMasterConfig/GameplayHUD. A compact-width header hides its hint when needed.
- Affordable choice still hides the lower HUD while placing. RMB/Escape/success restore the tray plus blank shell; Ctrl closes. Successful placement remains unselected. IsInspectionVisible continues to mean **active details**, not merely the empty shell, preventing stale tower events from refilling it.
- Verification: SDK build, PlacementHudChecks (both maps plus real Rocket mouse hit/click at 1920×1080,1366×768,960×540; blank shell/stable panel bounds; 40 entries/two-row horizontal overflow/category reflow) CompactInspectorChecks and HudCategoryChecks (including the dynamic 40-type tray) passed. Desktop visual review is still pending.

This amendment supersedes older statements that cancellation must hide the entire Inspector panel. No shield/combat/research/scoring/Inspector-art changes are included.

---

## P3 Campaign hub / Levels split — implemented 2026-10-17

- Campaign now shows the selected profile's saved player level, XP and Research Points, followed by distinct **LEVELS** and **RESEARCH** destinations.
- **LEVELS** opens UI/Menus/Campaign/Levels/LevelSelectionMenu.cs/.tscn with the existing Level001 and Level002 mission descriptions/launch buttons. Back returns to Campaign. Research continues to return to Campaign; Results already returns to this hub after mission rewards.
- Existing MenuLayout, UIButtonFactory, profile guards and MenuNavigation are reused. No Research access was added to Results/gameplay. Both missions remain available; mission unlocking, completion stars, campaign saves/slots and Continue/Load remain separate future work.
- This completes the remaining **P3 menu organization slice**, alongside the previously implemented progression display. It does not complete all P1–P5/R2/R4 scoring or polish.
- Verification: SDK build succeeded with zero warnings/errors. MenuTowerFlowChecks, ResearchMenuChecks and ResultsChecks passed; viewport checks passed at 1920×1080, 1366×768 and 960×540. The broader controls runner hit a native Godot finalizer/resource crash; no full-suite pass is claimed. Details are recorded in TESTS/UI/CampaignHub.md. Desktop visual review remains for the user.

Older menu descriptions below are retained as history and superseded by this amendment.

---

## Mission XP connection — implemented 2026-10-17

User approved a provisional, tunable mission-to-profile XP loop before finalized scoring. **This amendment supersedes older statements that missions never grant XP; preserve the original P1–P5/R4 design below as future scope.**

- **Delivered remaining P1/P2/R4 foundation:** each level exposes VictoryBaseXP / DefeatBaseXP (500 / 0 initially); deterministic itemized scoring; captured deployment profile/settings; persisted once-only mission numbers/receipt; atomic XP + level-up RP + award marker; honest failed-save UI and retry. v1/v2 progression migrates to v3 without resetting XP, research, RP or stored curve.
- **Delivered Results/Campaign loop:** saved XP itemization, actual level changes/RP and final within-level bar; show/Skip/reopen never grants. RETURN TO CAMPAIGN opens the existing hub. Research remains Campaign-only. Profileless F6/preview says NOT SAVED.
- **Intentionally deferred original scoring:** first-clear/replay history, replay reduction/anti-farming policy, ETA/time modifiers, kill/tower/performance bonuses and deductions. Replays currently earn the same baseline; signed line-item data is ready for later reward deductions floored at zero, without removing earned levels.
- **Still open:** separate P3 Levels area, mission unlock progression, detailed XP count-up/sounds/R2 polish, Credits telemetry, campaign/battlefield Save–Load/slots and full P5 regressions. Do not mark the combined P1–P5/R4 long-term brief fully complete.
- **Verification:** .NET SDK build (0 warnings/errors), MissionXPChecks, PlayerProgressionChecks, ResultsChecks, ResearchFoundationChecks, ResearchMenuChecks and BattleResearchChecks passed. Results-frame geometry verified at 1920×1080, 1366×768 and 960×540; desktop visual review remains. Existing unrelated historical full-suite blockers remain recorded below.
- **Failure boundary:** leaving a mission whose XP could not save does not persist a recoverable pending battlefield/reward; use RETRY XP SAVE before leaving. Monotonic watermarks assume the existing single active battlefield/application flow.

Authoring/ownership: SYSTEMS/Progression/Missions/README.md. Validation: TESTS/Progression/MissionXP.md.

---

## Tower HP / shared DefenseBar foundation — implemented 2026-10-17

- [x] **All registered towers/structures have hull:** provisional 100 HP, offensive per-level MaxHealth through the existing eight-stage pipeline, active total rank HpBonus, passive Factory/Depot Inspector health setting. No final HP balance implied.
- [x] **Damage and lifecycle:** shared typed DamageResolver, explicit capped repair API, no regeneration; promotions/upgrades preserve missing HP. Zero HP stops operation, cancels work and uses existing removal cleanup for whole-footprint occupancy/navigation, selected Inspector and logistics. No refund/reward.
- [x] **Same DefenseBars system:** existing event-driven scene/style attached to fixed root, visible when damaged, footprint clearance, no new polling/renderer. Live compact Inspector HP, rank HP bonus and changed upgrade HP comparison. Inspector layout resources/dimensions preserved.
- [x] **Targeted verification:** full C# compile with Godot source generators; TowerHealthChecks (all nine types, HP safety/ranks/paid tiers, damage/repair, pause, weapon/passive destruction and whole-footprint cleanup), HeatInspectorChecks, CompactInspectorChecks, PlacementHudChecks (both maps), ConstructionChecks and BattleResearchChecks passed. Heat/MAX/busy Inspector geometry remains 560×330 at 1920×1080, 1366×768 and 960×540.
- [ ] **Broader verification / desktop:** desktop visuals remain unreviewed. Broader runners hit existing assumptions outside this hull change: EnemyIntegrationChecks stops at “Unavailable registration cannot be built” after the placement-flow changes; TowerStatChecks expects every neutral-stage snapshot identical despite Stage07 now materializing heat; standalone TowerRankChecks reaches a stale travelling-wave falloff expectation (2.291304 vs 2.125) after the range rebalance. No full-suite pass claimed; these older tests were not rewritten in this pass.
- [ ] **Remaining follow-ups:** E3B enemy tower attacks still need their own producer/targeting design. Tower shields, armor, repair producers, destruction visuals and campaign/battlefield saving remain planned. Historical HP-not-implemented notes below are superseded only for this hull foundation. See TOWERS/Shared/Durability/README.md and TESTS/Towers/TowerHealth.md.

---

# WORK — Consolidated Outstanding Implementation Backlog

## Inspector HP/ammo meters and compact earned rank (2026-10-17)

- [x] Add right-side Health and optional Ammo progress meters from the selected tower's existing runtime state/events; leave Heat and targeting intact.
- [x] Remove Ammo Capacity left stat row and redundant rank heading, replace with one Combat Rank label, add earned rank to the tower title.
- [ ] Godot headless geometry and desktop visual test still pending; preserve the 560×330 Inspector target and verify maximum-level/hot tower edge cases.

---

## Big mk.1 visual enlargement — 2026-10-17

- [x] Enlarged `big_mk1` artwork from **96×72 to 112×120 px**. Kept the original heavy hull with uniform scale and added large upper/lower plated wings (3.5× Enemy001 width, 5× height). The medium Bulwark stays 64×48.
- [x] Updated exact sprite-size catalog assertions and feature/wave/feedback documentation. **No enemy HP/shield/speed/credits/abilities, scene transforms, wave authoring or navigation algorithm changed.** Big mk.1 keeps its existing 24px circular navigation radius.
- [ ] **Godot visual QA outstanding:** reimport the SVG, verify silhouette/bar clearance, rotations, narrow corridors/wing overlap and mass-wave look; run EnemyCatalogChecks. No Godot runtime test run in this GitHub-only art pass.

---

## Big mk.1 heavy enemy — 2026-10-17

- [x] Registered `ENEMIES/Types/Enemy005` / `big_mk1` in `DefaultEnemyCatalog.tres`, using one inherited plain enemy scene and 112×120 right-facing plated SVG. Original GameMaker-inspired balance: **1,000 hull, 200 optional shields, 80px/s, 5 Citadel damage, 10 Credits**, 24px navigation radius. No new AI or ability nodes.
- [x] F2/F3 catalog discoverability and wave syntax (`Enemy("big_mk1", shield: true)` for 200 starting SP). **Level001 and Level002 waves remain unchanged.**
- [x] Shared DefenseBar takes visual sprite height once on spawn so large ships' hull/shield bars clear their artwork; small ships keep authored minimum. Updated catalog/shield/credit regressions and relevant READMEs in this same commit.
- [ ] **Godot QA pending:** compile/run enemy catalog + GenericEnemy integration runners, F2 cycle, shielded Big mk.1 test wave, portal/corridor navigation for 24px clearance radius, oversized artwork overlap, shield aura/bar visual, rewards and 500-ship performance. No runtime tests performed in this GitHub-only change.

---


## Build preview / unobstructed placement UI — implemented 2026-10-17

- [x] Ctrl-open tray: hover or keyboard-focus a tower tile to show its description, cost, build time, footprint, basic effective stats and CAN AFFORD / INSUFFICIENT RESOURCES in the existing right Inspector. No ghost/payment just for browsing.
- [x] Affordable tile click arms one placement and temporarily hides BottomArea (tray + Inspector). Logical tray-open state stays intact; the permanent top status remains usable. Unaffordable clicks keep the preview/tray visible, create no tower and emit TowerPlacementController.BuildDenied(towerId, reason) for future audio; no audio playback was added.
- [x] RMB/Escape cancel the ghost and restore the tray with no tower selected; Ctrl cancels and actually closes the build tray. Reopening Ctrl does not resume canceled placement.
- [x] Successful placement no longer calls SelectTower(newTower). It ends placement, restores the tray, clears the Inspector and consumes/suppresses the placement frame's selection path. A separate later world click selects the tower normally. Restoring the tray under a stationary cursor does not immediately reopen a hover preview.
- [x] Preserved the latest Inspector resize: existing560×330 dimensions, fonts, right-column/heat structure, styles and layout resources. Compact build descriptions and wrapped affordability text fit the same panel. No InspectorPanel.tscn, UIMasterConfig.cs or DefaultUIConfig.tres changes in this pass.

Verification: TESTS/UI/PlacementHud.md. Current-control descriptions here supersede historical auto-selection/always-visible placement-preview notes below; original plans remain preserved.

---

## Laser Tower silver/yellow visual refinement — 2026-10-17

- [x] Replace Laser Tower SVG with silver/gunmetal body and yellow emitter/rails, retaining existing 64×64 scene sizing and muzzle marker.
- [x] Recolor only the Laser beam preset to ivory-yellow core / golden halo and reduce wave/end wander. Extend shared BeamRenderer's opt-in irregularity with smoothly sampled deterministic noise, preserving zero-irregularity sine fallback and inactive behavior.
- [x] Update Laser visual fixture assertions and the two beam READMEs; no gameplay/targeting/ammo/upgrade changes.
- [ ] **Godot verification pending:** run `TESTS/Towers/LaserTowerChecks` via its existing suite and inspect beam at normal zoom, short/long distances, pause, multiple beams and end wander. This pass was source-validated and its SVG preview-rendered, but no Godot/.NET headless tests could run in this environment.

---

## Enemy shield meter in shared DefenseBar — 2026-10-17

- [x] Added the **shield bar above the regular hull bar**, inside the existing `FEEDBACK/DefenseBars/DefenseBar` Node2D (no extra scene node per enemy). Full shields show both stacked rows, preserving normal hull-bar behavior when unshielded or after shield break. Uses warm red-orange shield tint, separate Inspector-configurable shield thickness/gap, existing width/border/offset, movement counter-rotation and `DefenseBarsEnabled`.
- [x] Shared Enemy shield-change bridge now notifies both `DefenseBar.SetShield` and the existing `ShieldVisual` aura for spawn/damage/explicit restoration; no shield regen, extra damage logic, spawn changes or polling.
- [x] Updated `TESTS/Combat/EnemyShieldChecks.cs` and feedback/enemy docs for full/partial/depleted/recovered shields.
- [ ] **Verify:** run Godot .NET shield fixture and visual test on Level001 shielded Enemy001; confirm shield row sits distinctly above hull, stays horizontal on turns, does not overlap other enemy indicators, and both disappear on removal. Runtime testing unavailable during this GitHub-only pass.

---


## Offensive tower range balance — 2026-10-17

- [x] Increased **all seven offensive tower types at every authored purchased level** approximately 15% (nearest whole px): Battle 294/304/313, Laser 294/304, Blaster 294/304, Shockwave 230/239, Cryo 239/248/258, Machine Gun 239/248/258, Sniper 515/524/534. Source of truth remains each `*Levels.cs` row; rank bonuses/Inspector/range rings/upgrade previews resolve normally. No modifier or change to weapon cooldown, damage, ammo, build costs or Blaster blast radius.
- [x] Updated exact-value regression assertions and per-type README balance tables in the **same commit**.
- [ ] **Godot validation outstanding:** rerun TowerStat, TowerUpgrade, Laser, Shockwave, Cryo, MultiBarrel and TowerRank checks. Review placement-range rings, upgrade comparison, Shockwave propagation and level playfeel. No Godot/.NET runtime available to run tests in this pass.

---


## Inspector visual trim / right-side bars — 2026-10-17

- [x] Narrowed `InspectorPanel.tscn` and the HUD default from **620×330** to nominal **560×330**; the Main stats/paid-upgrade area shrinks, while the six target-priority buttons retain their original **210px right-section layout**.
- [x] Moved the Heat text/bar under the right-side Combat Rank/XP bar. Left-side disabled reasons remain independent. Adjusted `GameplayHUD.cs` cached paths and Heat/Inspector/upgrade regression expectations; no placement/Ctrl/visibility/targeting/heat mechanics changes.
- [ ] **Verify in Godot after the latest commit:** run `TESTS/UI/CompactInspectorChecks.tscn`, `HeatInspectorChecks.tscn`, `InspectorTargetingChecks.tscn` and existing HUD/upgrade test suites. Visually review all nine families, the hot+disabled+Legendary/active-upgrade worst case, header/title and CURRENT/NEXT/GAIN alignment at multiple viewports; ensure no right-side wrap/clipping or unexpected height expansion. These checks have **not been executed for 560px**. Codex's separate placement-mode Inspector/HUD visibility pass must retain this new layout and node paths. See [Inspector current layout](../UI/GameplayHUD/Inspector/README.md).

---

## Battle research proof node — implemented 2026-10-17

- [x] **Battle Tower Weapons I:** battle_weapons_1, 1 RP, requires Research Core, axial (-1,0), +5% Battle Tower damage from the next mission. Saved ownership/RP use the existing progression v2 record; no new currency or XP scoring rules.
- [x] **Tower permanent research Stage04:** an immutable deployment snapshot is captured once in CitadelSession, shared with paid placement/build previews, and retained by each tower. Selected paid level → rank → research → safety clamps → final stats. Standard Battle damage30/33/36 becomes32/35/38 using existing nearest/ties-to-even integer rounding. All ranks/tiers, config rebuilds and paid previews retain the bonus without compounding.
- [x] **Reusable targeted damage effect:** catalog defines TowerDamagePercent + TargetTowerId; only Battle has an authored node. Other towers, Citadel health, ammo, heat, prices, authored rows, profileless F6 and other profiles are unchanged by this purchase. No progression I/O in combat/stat resolution.
- [x] **Graph extension:** real third node, balanced labels, and initial/reset view fits authored branches; pan/zoom selection remains shared geometry.

Verification and remaining manual review: TESTS/Research/BATTLE_RESEARCH.md. Mission XP rewards/replays/award tokens remain deferred. Earlier research-expansion examples below are preserved as history; Battle damage is now implemented rather than a future-only example.

---

## Shield aura art polish — 2026-10-17

- [x] Replaced the nearly invisible shield outline with a **single baked radial-fill SVG** (lightly translucent centre, thicker high-contrast bright rim, layered outer glow and static electric zap filaments). Tuned `FEEDBACK/Shields/DefaultShieldVisualStyle.tres` to 1.65× size, 0.82 idle opacity, 1.0 hit/break peaks, warm red-orange tint; `ShieldVisual.tscn` uses linear filtering. No change to shield gameplay, wave flags, regeneration, particles or per-frame CPU work.
- [ ] **Visual QA:** open Godot to re-import `FEEDBACK/Shields/Artwork/ShieldAura.svg`; check Shielded Enemy001 at normal zoom, small/large enemies, line-of-fire visibility, hit/break fade, layering and mass-wave performance. XML/reference checks passed; Godot headless/desktop tests were not run in this authoring session.

---

## Shield visual foundation (SV1) — 2026-10-17

- [x] Reusable **[FEEDBACK/Shields](../FEEDBACK/Shields/README.md)** presentation: one baked tintable reddish SVG aura, auto-sized behind the enemy, event-only shield damage/break flashes, lazy attachment only to shielded enemy instances, shield-restoration reactivation, no per-frame shield visual script processing. Purely cosmetic; existing shield arithmetic, shield generator and wave opt-in remain unchanged.
- [ ] **Godot desktop/headless SV1 verification** — run modified `TESTS/Combat/EnemyShieldChecks.cs`; inspect spawn/damage/break/restore, layering, red tint on different ship shapes, and 500-enemy performance. The code/resources are pushed but not compiled or visually tested here.
- [ ] **Later, separate decisions:** tower HP/shields/durability, natural shield regeneration, shield bars, premium sparks/shaders. None are implemented in SV1.

---

## Level001 combat/shield playtest balance — 2026-10-17

- [x] Standard Drone (`basic_mk1` / Enemy001) has **20 optional shield points** in its saved EnemyDefinition; zero-shield ordinary/F2 starts remain unchanged.
- [x] Level001's existing three waves remain manual and now contain **38 / 40 / 55** enemies with **7 / 12 / 18** shielded entries respectively. Wave 1 introduces shielded Standard Drones; waves 2–3 add Scouts; final wave adds a few Bulwarks. **No ShieldGenerator** is authored; no shield regeneration, mechanic refactor or new difficulty system was introduced. See [Level001Waves.cs](../LEVELS/Level001/Level001Waves.cs) and [Authoring.md](../LEVELS/Waves/Authoring.md).
- [ ] **Balance/playtest verification:** Confirm 20-point red aura is readable, portal navigation works for the few Level001 Bulwarks, and the three escalating waves are still manageable at 1,000 starting Credits. C# build/headless/desktop test runs are outstanding for this GitHub-only balance pass.

---

## Current amendment — Research before mission XP balancing (2026-10-17)

**This amendment supersedes older status/order statements below; all original plans are retained.** The user explicitly prioritized real mission statistics and the Campaign-only Research Room before finalized XP rewards/deductions.

- [x] **Research foundation / P4 first playable slice:** Campaign → RESEARCH, axial center-origin hex graph, pan/zoom, keyboard selection, prerequisite/effect/cost/state panel and atomic per-profile purchase. Research Core is free/virtual; Citadel Master I costs 1 RP and gives +10% maximum/current starting Citadel HP on both deployed levels. Results and gameplay have no Research link; purchase authority validates the active Research Room. See UI/Menus/Campaign/Research/README.md and SYSTEMS/Progression/Research/README.md.
- [x] **R3 actual telemetry slice:** event-driven paid placements, first construction completions, paid upgrades, unpaused mission clock; existing official kills/breaches/waves/integrity retained. Frozen results show these real counters. Successful placement and completed construction are separate metrics. Credits earned/spent remain untracked/null, not fabricated.
- [x] **Progression v2:** purchased stable research IDs and RP payment saved together; existing v1 XP/level/curve/RP migrate without reset or identity-index changes. XP grant remains a development helper, not a mission reward API.
- [ ] **Remaining P1/P2 / Results R4:** mission reward calculator, editable scoring/ETA, first-clear/replay history, once-only run-token awards and Results XP animation/itemization are intentionally deferred until scoring is chosen. Do not add provisional reward tuning merely to use Research; the default-off DEBUG XP grant supplies test RP.
- [ ] **Remaining P3:** separate LEVELS area/hub reorganization; current two deployment actions remain and Research is added alongside them.
- [ ] **Remaining R2/R3/P5:** desktop visual review, Credits/optional additional statistics, expanded/full regression and optional presentation polish. No tower damage research was added: future tower modifiers use 04_PermanentResearch; no fake catalog nodes.

Focused tests and scope/limits: TESTS/Research/README.md. Do not mark full P1–P5 or all Results R3/R4 complete. Follow the revised sequence in the progression brief rather than repeating the already delivered foundation.

---

**Audit date:** 2026-10-16 (latest audited pushed commit UTC 2026-10-17). Checked against GitHub `main` at `967435719798` (Add Battle Tower construction SVG stages and shared optional level artwork). **This is a documentation/code-presence audit, not a new Godot test run.** Status is a snapshot: check current HEAD and relevant code/tests before Codex acts, particularly if another Codex task was pushed meanwhile.

**Purpose:** One entrypoint for *outstanding implementation passes scattered across project notes*. Links point to the authoritative full specification; this index intentionally does **not** replace those notes. [Code preferences](CodePreferences.md). Keep completed historical work below this index.

### Status key

- **[ ] OPEN — detailed pass exists:** ready for Codex in numbered steps; complete/test before marking done.
- **[ ] OPEN — design only / needs pass:** future system noted in README, but no approved numbered implementation spec yet.
- **[ ] CHECK / POLISH:** feature implemented, but a verification, visual review or maintenance job is outstanding.
- **[x] COMPLETE:** listed to stop Codex from repeating the work.
- A long-term idea mentioned in a README is **not** automatically approved for a giant implementation. Split large systems into narrow passes first.


## Initial progression/menu foundation — 2026-10-17

The user selected a smaller first slice of P1/P3: separate per-profile persistent XP/levels/RP, editable starting curve, multi-level carry and +1 RP per promotion, shared Main/Campaign display, and an off-by-default DEBUG grant option. See [Progression README](../SYSTEMS/Progression/README.md). Mission XP scoring, replay/first-clear history, once-only mission awards, Results XP, the Levels/Research hub split and Research Room remain **OPEN**. Do not mark all P1 or P3 complete. Verification: TESTS/Progression/README.md.

## A. Open implementation passes (Codex-ready or scoped in existing notes)

| Status | Pass / work | Authoritative notes and next action | Dependencies / caveats |
| --- | --- | --- | --- |
| [x] | **IT1–IT4 — compact Inspector + six targeting priorities** | [Implementation/checklist](../UI/GameplayHUD/Inspector/INSPECTOR_TARGETING_PASS.md): non-scrollable700×330 two-column Inspector, independent earned rank and paid upgrades, six per-instance choices and cached checkpoint-aware First/Last. | Automated compilation/headless results: [verification](../TESTS/UI/InspectorTargeting.md). Desktop visual review remains pending; see manual verification backlog. |
| [x] | **CT1–CT3 — Cryo Tower + reusable enemy status-effect foundation** | [New 2026-10-16 Codex plan](../TOWERS/Offensive/CryoTower/CRYO_TOWER_IMPLEMENTATION.md): energy-projectile Cryo, permanent nonstacking 33% slow, scalable optional status infrastructure and tests. | **IMPLEMENTED**, with focused headless tests. Cryo only targets unslowed/nonimmune enemies. See [verification](../TESTS/Towers/CryoTower.md); manual playfeel remains outstanding. Stasis remains deferred. |
| [ ] | **P1–P5 — Player/global XP + Research Foundation** | [Detailed phased Codex pass](../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md): persistent per-profile XP/RP, signed mission scoring + ETA, Campaign Levels/Research, hex-origin graph and real Citadel Master I. | Extends Results R3/R4; **do not implement XP twice** or confuse player XP with tower Rank XP. Campaign save/load remains separate. |

| [ ] | **R2 — Mission Results visual polish** | [Results remaining passes](../UI/Menus/Results/README.md): layout, transitions, skippable count-up, sounds and viewport testing. | R0/R1 outcome/results behavior already complete. Optional independent polish pass. |
| [ ] | **R3 — Actual level-run telemetry / extra stats** | [Results R3](../UI/Menus/Results/README.md): placed towers, successful upgrades, Credits earned/spent, unpaused timer, frozen final telemetry. | **P2 already covers the subset needed for XP/time**; coordinate instead of duplicating stat owners. Extra optional breakdown may be later. |
| [ ] | **R4 — Mission XP scoring/persistence** | [Results R4](../UI/Menus/Results/README.md), implemented through [P1–P2](../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md) in the approved plan. | **Overlapping scope:** after P1/P2, reconcile R4 checklist; don't create a separate XP wallet/service. |
| [ ] | **E3B — Enemies attack towers** | [ENEMIES/README E3B](../ENEMIES/README.md): ranged enemy attacks and electric area pulses against damageable towers. | **HP prerequisite implemented 2026-10-17.** Enemy attack targeting/delivery still needs a separate scoped pass; no automatic tower attacks enabled. |
| [ ] | **Waves Phase 2 — remaining options/content** | [Waves README](../LEVELS/Waves/README.md): more mixed authored wave content, other portal strategies and eventual elite/stealth/speed/boss modifiers. | **Partially complete:** basic mixed catalog, required portals, starting shields and ShieldEmitter already work. This is a collection of optional *follow-up features*, not a scheduler rewrite. |
| [ ] | **Waves Phase 3 — advanced progression** | [Waves README](../LEVELS/Waves/README.md): wave-completion bonuses, countdown/auto-next, announcements, scripted events, preview/history and eventual wave saving. | **Design only; subdivide by feature.** Current finite waves and R1 mission victory already work. |

## B. Outstanding systems documented elsewhere — not yet numbered/approved implementation passes

These are real gaps or explicitly proposed systems, but **need a dedicated scope/decisions before Codex should implement them**.

| Status | System / next pass to design | Existing notes and key boundary |
| --- | --- | --- |
| [x] | **HD1–HD3 — generic tower disabling, heat and compact Inspector** | Implemented [shared API/authoring](../TOWERS/Shared/Conditions/README.md): independent timed/conditional handles, source refresh/cleanup, all offensive attack gates, optional selected-level heat, cooling/latch and tests. Battle is enabled via its Godot-Inspector minute profile (3min continuous fire,9s recovery); other tower opt-ins remain undecided. **HD3 Inspector heat/status presentation is implemented.** [Verification](../TESTS/Towers/TowerConditions.md). |
| [ ] | **Tower HP, shields, damage, destruction and repairs** | [TOWERS roadmap](../TOWERS/README.md), [E3B dependency](../ENEMIES/README.md), [Repair Tower](../TOWERS/Support/RepairTower/README.md). Hull HP/damage/destruction/footprint/Inspector foundation implemented 2026-10-17; shields, repair producers and further durability balance remain planned. |
| [ ] | **Power generation, island-grid electricity, shortages, priorities & relays** | [Power network](../TOWERS/Economy/Power/README.md), [generators](../TOWERS/Economy/Power/Shared/README.md), [consumer capability](../TOWERS/Shared/Power/README.md), [Power Relay](../TOWERS/Utility/PowerRelay/README.md). Solar/Industrial/Fusion share generation code; no cables; terrain islands connect by paired relays. Large system: break into several passes. |
| [ ] | **Actual campaign/mission Save–Load, slots and unlocked level progression** | [SYSTEMS/Saving](../SYSTEMS/Saving/README.md), [Campaign menu](../UI/Menus/Campaign/README.md), [Pause menu](../UI/Menus/Pause/README.md). Profile *identity* is saved, but no campaign/session saves yet. **P1–P5 progression record does not implement campaign saves.** |
| [ ] | **Reusable blank playable level template / level-loading convenience** | [LEVELS](../LEVELS/README.md), [Session](../LEVELS/Session/README.md). `LEVELS/LevelTemplate.tscn` exists with blank terrain/Citadel/Portals container, but the requested *placed active portal* and streamlined new playable-level registration/launcher flow are not done. Create a focused pass if still desired. |
| [ ] | **Tower magazines/reloading, accuracy, maintenance and selling** | [Tower roadmap](../TOWERS/README.md), [Inspector](../UI/GameplayHUD/Inspector/README.md), [Shared ammunition](../TOWERS/Shared/Ammunition/README.md). Implement separately; reload must use real stock, not create ammunition. |
| [ ] | **Additional registered towers and specialized mechanics** | [Offensive](../TOWERS/Offensive/README.md) (Missile, LOS; heat/disable tracked separately above), [Support](../TOWERS/Support/README.md) (Repair/Radar), [Economy](../TOWERS/Economy/README.md) (Resource Tower/Scrap Refinery/older SolarTower). **Sniper + Machine Gun are already implemented** and must not be re-created. Cryo now has its own **CT1–CT3 completed** pass in section A. |
| [ ] | **Further damage/enemy status mechanics** | [ENEMIES future abilities](../ENEMIES/README.md), [COMBAT](../COMBAT/README.md), [Waves modifiers](../LEVELS/Waves/README.md): armor mitigation, stealth/detection, EMP, regeneration, buffs/status durations, additional boss/attack types. The **first concrete slow/status foundation is implemented in** [CT1–CT3](../TOWERS/Offensive/CryoTower/CRYO_TOWER_IMPLEMENTATION.md); coordinate future statuses with it rather than create duplicates. |
| [ ] | **Live gameplay wiring for shared Alerts/Toasts** | [UI/Alerts foundation](../UI/Alerts/README.md) is **implemented and mounted** in the shared GameplayHUD, with configurable banners, priority/queues, deduplication, and DEBUG F7 preview. **Not implemented:** real producers from Citadel, waves, tower construction/low ammo, research or combat; wire narrow event-driven sources later, without per-frame popup spam. Sounds/voice/history/save persistence are separate future work. |
| [ ] | **Audio game content/event wiring & settings** | [AUDIO](../AUDIO/README.md): foundation exists; actual clips, sound event connections, menu music/ambience, voice lines and optional player preferences remain future stages. |
| [ ] | **Visual combat feedback polish** | [Damage Numbers](../FEEDBACK/DamageNumbers/README.md), [Camera Effects](../FEEDBACK/CameraEffects/README.md): unimplemented optional effects; [DefenseBars](../FEEDBACK/DefenseBars/README.md) and death/shockwave visuals already exist. |
| [ ] | **Player combat drone ability** | [DRONES/Combat](../DRONES/Combat/README.md): entirely prospective attack drone; **do not confuse with completed transport drone logistics**. |
| [ ] | **Research expansion beyond first proof node** | [Player research foundation](../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md): the giant research tree, DME/Zy0n story gates, unlockable towers, doctrine loadout slots, production buffs and refunds/respec are **explicitly deferred beyond P1–P5**. |
| [ ] | **Further economy/ammo logistics options** | [Economy](../TOWERS/Economy/README.md), [Ammo loop](../TOWERS/Economy/Ammunition/README.md): raw materials, differing ammo types, research-controlled throughput, additional resource loops—not missing portions of the implemented first two-leg pipeline. |
| [ ] | **Optional advanced world art** | [Backgrounds](../WORLD/BACKGROUNDS/README.md): parallax, shaders, planet/nebula animations and unique level art; cosmetic backlog, not a mechanics prerequisite. |
| [ ] | **Options menu / persisted personal settings** | [Pause](../UI/Menus/Pause/README.md), [AUDIO](../AUDIO/README.md). Existing Pause Options is a disabled placeholder; needs its own specification. |

## C. Verification, profiling and note cleanup — not missing gameplay passes

| Status | Task | Evidence / do not redo |
| --- | --- | --- |
| [ ] | **N4 corridor movement desktop visual/performance review** | [N4 implementation record](../ENEMIES/enemypathing.md), [CPU timings](../TESTS/Performance/CorridorPerformance.md), [follow-up profiling suggestions](../ENEMIES/ENEMYPERFORMANCE.md). N4 is **implemented**, only visual 0/50/100% comparison and rendered 500-enemy checks remain. |
| [ ] | **Profiling only if measured bottlenecks warrant** | [ENEMYPERFORMANCE](../ENEMIES/ENEMYPERFORMANCE.md), [findings](AGENT_FINDINGS/Performance.md): repath-burst latency, potential duplicate spatial-index position update, clearance cost; preserve safety and measure before optimizing. |
| [ ] | **Godot/.NET standard build and full-suite blockers** | [TESTS/README](../TESTS/README.md), [Ammunition evidence](../TESTS/Economy/Ammunition.md), [Footprint evidence](../TESTS/Towers/Footprints.md). Previous environment lacked expected .NET/MSBuild/NuGet dependencies; full integration also hit preexisting Level002 painted-terrain assertion. **Recheck current environment/map before claiming unresolved or fixed.** |
| [ ] | **Beam ripple/GPU effects test and visual QA** | [Beam renderer](../TOWERS/Shared/Weapons/Beams/README.md), [Laser tests](../TESTS/Towers/LaserTowerChecks.cs): build and run modified LaserTowerChecks (muzzle anchor, bounded impact drift, allocation/node stability, GPU emitter toggles), then desktop check short/long beams, moving enemies, multiple beams, pause, EMP, target loss and low ammo. New visuals are pushed but **not yet Godot/.NET-tested** in this session. |
| [ ] | **Alerts F7 visual/pause verification** | [Alerts README](../UI/Alerts/README.md) has a debug preview and manual checklist: topbar clearance, priority interruption, toast merging/queue bounds, pause behavior, scene transitions and smaller viewport. The GitHub-only port is **not yet Godot C# built or visually playtested**. |
| [ ] | **Manual render/playtest verification** | Battle construction **stage SVGs are implemented**; check in-game stage positions/transition vs the provided rendered contact sheet ([test checklist](../TESTS/Towers/ConstructionArtwork.md)). Check existing DefenseBar, 500-enemy motion, full HUD and ammo drone visuals. **Construction sparks/VFX were explicitly excluded from this C4 scope**. Headless validation is not a substitute for the Godot editor. |
| [ ] | **Stale status text / factual note cleanup** | [ENEMIES/README](../ENEMIES/README.md) still has an old N4 “planned” section; [enemypathing.md](../ENEMIES/enemypathing.md) contains a contradictory top “NOT IMPLEMENTED” line; [tower_ideas.md](../TOWERS/tower_ideas.md) still describes ranks/multi-cell/Laser as absent; [WORLD/README](../WORLD/README.md) has historical “Citadel health future” wording; [TOWERS/README](../TOWERS/README.md) has an obsolete “multi-cell placement not implemented” list item. Update obsolete lines against actual code, **do not implement those features again**. |
| [ ] | **Older ammo verification/update notes** | [Previous WORK detail below](#archived-completion-record--ammunition-pipeline), [Ammo tests](../TESTS/Economy/Ammunition.md): some original 1×1 wording and a pre-later-balance verification status are historical. Verify/refresh after current-footprint and shipment defaults, not new inventory architecture. |

## D. Already delivered — not outstanding implementation passes

- [x] **GE1–GE3 inherited enemy scenes:** one [GenericEnemy.tscn](../ENEMIES/Shared/GenericEnemy.tscn) supplies Sprite, Movement and DefenseBar; five type scenes inherit and retain their artwork/abilities. [Implementation and tests](../ENEMIES/Shared/GENERIC_ENEMY_SCENE_PASS.md).
- [x] **Enemy E2A/E2B/E3A:** mixed catalog, ShieldEmitter, Transporter + six-child staggered DeathSpawn and wave ownership. [ENEMIES](../ENEMIES/README.md) / [DeathSpawn tests](../TESTS/Enemies/DeathSpawn.md).
- [x] **Navigation N4, F3 route preview and checkpoint chains:** core implementation done; visual review separately listed above. [Movement](../ENEMIES/Movement/README.md), [Route preview](../ENEMIES/Movement/pathoverlay_tweaks.md).
- [x] **Multi-cell tower footprints F1–F5:** Factory **3×2**, Depot **2×2**, offense **1×1**, occupancy/path/performance tests. [Pass](../TOWERS/Placement/MULTI_CELL_FOOTPRINT_PASS.md).
- [x] **Tower core combat/stat/paid-level/rank foundation, Laser/Blaster/Shockwave, typed damage and enemy spatial queries.** [TOWERS](../TOWERS/README.md), [COMBAT](../COMBAT/README.md).
- [x] **Reusable beam visual polish (2026-10-17):** [Shared BeamRenderer](../TOWERS/Shared/Weapons/Beams/README.md) has adaptive animated ripple, endpoint drift, drawn impact glow and independently optional scene-owned `GpuParticles2D` impact/muzzle emitters. [Laser style](../TOWERS/Offensive/LaserTower/Beam/README.md) enables subtle ripple/contact sparks, reusing [FEEDBACK/Shared](../FEEDBACK/Shared/README.md) particle assets. **Cosmetic only; GPU appearance and changed Laser test assertions not run in Godot here.**
- [x] **Sniper Tower + twin-barrel Machine Gun Tower:** current playable scenes/configs/levels/ranks and shared projectile firing mode. [Sniper](../TOWERS/Offensive/SniperTower/README.md) / [Machine Gun](../TOWERS/Offensive/MachineGunTower/README.md). Note: a subsequent balance commit made Battle faster and tuned Machine Gun speed—don't replace these values with old samples.
- [x] **C1–C3 timed construction/upgrades plus C4 Battle-only SVG artwork pilot:** operational gating, progress and paid upgrade timers are live; optional artwork presenter, Battle stage SVGs and per-level artwork fallback are in code. [Pass](../TOWERS/Shared/Construction/TIMED_CONSTRUCTION_PASS.md) / [pilot verification](../TESTS/Towers/ConstructionArtwork.md). **Remaining only optional factory/other type-specific stage content and desktop visual QA; sparks/VFX not requested.**
- [x] **Ammunition production, both physical drone delivery legs and offensive consumption.** See archived record below / [Ammo notes](../TOWERS/Economy/Ammunition/README.md).
- [x] **Shared UI Alerts foundation:** old isometric 2D major-alert and toast system ported into [UI/Alerts](../UI/Alerts/README.md), installed once through GameplayHUD with DEBUG F7 demonstration; **gameplay event wiring and actual Godot verification are still pending**. No tower/combat/wave mechanics changed.
- [x] **Results R0/R1:** real mission victory/defeat, snapshot, menu navigation; **not** R2–R4. [Results](../UI/Menus/Results/README.md).
- [x] **Profile identity Boot/Main/Campaign foundations, Pause basics**; not gameplay saves, XP or Research. [Profiles](../SYSTEMS/Profiles/README.md).
- [x] **Build HUD Offensive/Economy categories and compact Inspector** on the latest audited HEAD. [HUD check](../TESTS/UI/HudCategories.md).

## E. Suggested sequencing when returning

1. **Inspector visual review:** IT1–IT4 source and automated tests are complete. Manually assess the compact non-scrollable Inspector in-game; do not recreate six priorities or add a second journey estimator.
2. **Cryo Tower CT1–CT3** — implemented: reusable status-effect architecture and permanent nonstacking slow; **do not implement Stasis now**. Coordinate targeting-related edits in `TowerTargeting.cs` with IT1–IT4 so concurrent Codex work doesn't overwrite changes.
3. **Campaign progression P1–P5** — profile XP, mission awards, Research Room and first real research; coordinate Results R3/R4 telemetry and don't conflate this with still-missing campaign saves.
4. **HD1–HD3 complete; thermal balance for other towers remains open** — shared multi-source disabling and optional heat are implemented and tested. Battle now has a3min minute-based profile; other per-tower heat balance/opt-ins remain undecided. Compact event-driven Inspector heat/status presentation is implemented. This remains independent of tower HP/E3B.
5. **Tower durability + enemy E3B** — first implement tower health/shields/destruction safely, then enable enemy attacks. Only proceed after dependency passes are scoped and tested.
6. **Large management systems** — campaign Save/Load, followed by modular electricity/island power grids/relays, when ready.
7. **Optional/future** — Results R2 polish, more stage artwork (Battle pilot already done), advanced waves, additional towers, sounds, feedback, combat drones and graphical/performance checks. Don't redo complete GE1–GE3 or C1–C4 pilot.

**Codex handoff:** “Read `NOTES/WORK.md`, `NOTES/CodePreferences.md`, and the linked authoritative pass file for the single item selected. Verify HEAD and implemented code first. Implement and test only that scope; update WORK and the source pass checklists after verified completion.”

---

## Archived completion record — Ammunition pipeline (historical)

**Status: IMPLEMENTED AND HEADLESS-VERIFIED (2026-10-16), with known standard-build/full-suite environment/map limitations below.** The original foundation helpers were reused, not replaced: AmmoInventory, AmmoProduction, AmmoStorage and AmmunitionMasterConfig.

### Completed gameplay

- [x] Optional AmmunitionStats in each offensive tower's complete independent C# level rows, through the existing cached eight-stage pipeline. Current state is separate.
- [x] Per-instance reserves; configurable InitialAmmoFraction (full by default); per-launch Battle/Blaster charges, per-pulse Shockwave charge and prepaid time-based Laser use.
- [x] Empty weapons stop unpaid attacks and resume after physical resupply. Damage types, shield penetration, AoE/wavefront delivery and source attribution are preserved.
- [x] Stock persists through rank promotions and purchased levels; capacity growth never refills, shrink explicitly discards excess and records it.
- [x] Buildable/selectable Factory and Depot, Economy catalog entries, vector placeholders, 1×1 navigation-safe placement and existing wallet/payment failure behavior.
- [x] Automatic factory whole-batch production, finite buffer, full/no-depot pause and departure-driven resume using AmmoProduction.Advance.
- [x] Separate depot AmmoStorage inventory; more than one producer/store, deterministic nearest eligible selection.
- [x] Visible factory-owned Factory→Depot drones **and** depot-owned Depot→Tower drones, carrying finite real cargo and returning home. No instant shipments.
- [x] Incoming space reservations, finite home slots, single outstanding consumer shipment, scarce-stock retries, partial/surplus delivery and interruption cleanup.
- [x] Selected-tower/building runtime Inspector status and authoritative next-level ammo previews, using coalesced events rather than HUD polling/pipeline work per shot.
- [x] Existing pure helper tests executed first; focused weapon, delivery safety, conservation, purchase and real-physics automatic-loop tests added under TESTS/Economy.
- [x] Relevant navigation/menu/placement regressions and remaining diagnostic integration checks executed; actual outcomes documented.
- [ ] Clean standard SDK/editor build and unmodified full integration pass: blocked by existing container SDK/MSBuild/NuGet dependencies and Level002 tile-count assertion respectively. These are not claimed passing.
- [ ] Manual desktop graphical playtest: headless scenes/motion/transactions are verified, actual desktop rendering has not been manually inspected.

### Editable provisional defaults

| Item | Values |
| --- | --- |
| Battle, all current levels | 1000 reserve; 1 per successful projectile launch |
| Laser, all current levels | 1000 reserve; 2 rounds per firing second, retaining prepaid fractions |
| Blaster, all current levels | 600 reserve; 3 per projectile launch |
| Shockwave, all current levels | 400 reserve; 5 per emitted pulse |
| Global threshold | Existing DefaultAmmunitionConfig.tres:10% |
| Factory | 200 Credits;100 ammo/2s;**1000 buffer; 1000 ShipmentAmount (full load to dispatch);**1 drone;1000 payload;220px/s |
| Depot | 150 Credits;**10000 storage**;2 drones;250 payload;220px/s |
| Dispatcher | 0.2s scheduling interval;8 alternating endpoint checks |

Battle1000/1 and the10% rule follow the requested example. Other defaults are provisional tuning, not permanent balance. Production/storage settings initialize when placed; transport settings affect subsequent dispatch. Offensive level values remain in each *Levels.cs file only. Building noncombat settings live in their Inspector config resources.

### Factory dispatch tuning (2026-10-16)

**New defaults implemented in code, regression tests updated; current adjustment has not been re-run against Godot yet.** The factory produces 100 rounds every two seconds but waits until its configurable `ShipmentAmount` reaches **1,000** before launching. Every flight is an exact 1,000-round load from its finite 1,000-round buffer. If no depot can reserve 1,000 free slots, the factory holds the stock and pauses, rather than sending an early/partial drone. Depot storage is now 10,000. Depot-to-tower resupply remains as before (depot drone payload 250; global 10% demand). Edit `AmmunitionFactoryConfig.cs` / Godot Factory config and `AmmunitionDepotConfig.cs` / Depot config; no hardcoded scheduler threshold is necessary. See [tests](../TESTS/Economy/Ammunition.md).

### Ownership and safety

TowerAmmunition wraps the existing stock helper; factories/depot nodes wrap the existing production/storage helpers. TOWERS/Economy/Ammunition/Shared/Logistics owns requests, stock and incoming-capacity reservations. DRONES/Transport owns reusable direct flight/rendering/callbacks, not damage or ammo minting. One logistics node lives in each World, not an Autoload. No enemy queries or per-frame building cross-product searches occur.

Load source→cargo at departure; unload cargo→destination on arrival. Home slots remain occupied until physical return. Missing destination/capacity reduction returns surplus. Missing source/drone loses remaining cargo once. A return to an already-full home returns what fits and explicitly counts remaining loss. Removed endpoint stock also counts as loss. World unload clears jobs and subscriptions. Conservation includes initial stock, factory production, all local stock/cargo, paid consumption and explicit loss/discard.

### Verification and testing

Run `res://TESTS/Economy/AmmunitionLoopChecks.tscn` after building. It first runs the original AmmoFoundationChecks, then small focused WeaponAmmunitionChecks and DeliverySafetyChecks using AmmoLoopFixture. It buys Factory/Depot/Battle through actual buttons/payment, fires900 Battle projectiles to the threshold, verifies both real drone legs and arrival-only stock changes, and verifies automatic real-physics callbacks/pause with no manual drone ticking. TESTS/Economy/Ammunition.md records exact results and outstanding environment/map limitations.

For a quick desktop test, place Factory and Depot near a Battle Tower. Default starting stock is1000; reduce that config's InitialAmmoFraction before placement if waiting for900 shots is inconvenient. Select each building/tower to inspect stock, slots and demand; amber cargo indicates loaded flight. No debug/manual refill is needed for production deliveries.

### Still deferred

No permanent research, saving, magazines/reload timers, power grids/consumption, raw material inputs, construction timers, per-tower threshold sliders, differentiated ammo types, selling, tower health or combat drones were implemented. Future work should build on this working loop rather than recreate inventory, production, storage, threshold or reservations.


## Cryo CT1–CT3

Implemented shared lazy permanent-Slow status storage/Inspector immunity, immutable projectile payload, Cryo Energy weapon/catalog/artwork, explicit levels/ranks and effective Slow stat previews. Targeting excludes any already-slowed or immune enemy, without damage-only fallback. Reuses construction, physical ammo and spatial queries. Stasis/DoT/timed scheduler/research/saving remain deferred. See TOWERS/Offensive/CryoTower/README.md and TESTS/Towers/CryoTower.md for verification.

## PV1/PV2 implemented — 2026-10-17

Projectile tracers, real-muzzle bounded flashes and tower-owned Battle/Machine Gun/Sniper/Cryo presets are implemented. Sniper additionally has optional subtle red barrel-forward aiming. No HUD, gameplay stats, targeting or ammo systems were redesigned. [Authoring](../TOWERS/Shared/Weapons/Visuals/README.md), [actual verification and remaining desktop review](../TESTS/Towers/ProjectileVisuals.md).

## PV3 follow-up / Inspector width — 2026-10-17

PV3 automated/resources/documentation work was already included in the tracer/muzzle commit. Follow-up retains the desktop visual-review task and reruns the focused suite. Inspector default is now620×330, with content-sized CURRENT/NEXT/GAIN columns and thin drawn vertical separators; no scrolling, smaller fonts or gameplay changes. Focused all-building/viewport geometry checks pass (620px wide, max331px high). See TESTS/UI/InspectorTargeting.md.

## Barrel origins / flash visibility follow-up — 2026-10-17

Blaster migrated from center launch to its actual barrel-tip marker with a shared tower-owned orange flash preset. Battle/Machine Gun/Sniper/Cryo already use real muzzle markers; Laser uses its beam muzzle and Shockwave intentionally remains centered. Shared flashes now have first-process presentation protection, a bright hold/linear fade, larger/longer presets and draw above projectiles. Preserves8c7ab2e's20% Battle/Machine Gun rate balance. See TESTS/Towers/ProjectileVisuals.md for regression evidence and pending desktop appearance review.



