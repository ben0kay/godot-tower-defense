# Wider Tower Inspector + Six Targeting Priorities — Implementation Pass IT1–IT4

**STATUS: IT1–IT4 implemented, with automated compilation/headless verification. Manual desktop visual review remains pending.**

**Latest user-approved layout revision:** no Inspector scrolling or expandable sections. Use a compact two-column layout, initially620×330, with readable controls and all current gameplay information visible. Rank/targeting/ammo are beside stats/paid upgrades, not stacked above them. Technical descriptions use tooltips. This revision supersedes the original scrolling540×325 proposal. See README.md and TESTS/UI/InspectorTargeting.md for actual verification.


## Goal and user-approved design

Refresh **the existing placed-tower Inspector**, not a new window. Keep it anchored at **bottom-right**, independent of the Ctrl build tray; selecting a placed weapon still displays its existing world-space range circle through `TowerPlacementController._Draw` and `TowerMasterConfig.ShowSelectionRange`.

- **Wider and slightly shorter:** target **620 px width × 330 px height**, rather than current default 392 × 360. Inspector remains user-adjustable via `UIMasterConfig.InspectorWidth` / `InspectorHeight`, with responsive clamping and no internal scrolling. The configurable width hint supports the new value. Inspect the `.tscn` minimum width as well.
- **Prioritized reading order:** compact header, then stats/paid upgrades on the left and targeting/earned rank/ammunition on the right. Use clear headings/dividers/spacing; do not put the rank XP bar beside the upgrade price/button.
- Keep all actual gameplay controls visible without scrolling or foldouts. Use the existing sci-fi palette and readable12px table text.

### Compact hierarchy

Header: name/current level/status/close. Left column: typed current/next/gain and paid action. Right column: six priorities, earned rank/XP, ammunition and footprint. Busy progress is compact in the header. Detailed explanations live in tooltips. See the latest user-approved revision above.

### Behavior decisions / boundaries

1. **Combat Rank and paid Level are completely independent.** Rank XP is awarded by gameplay and cannot be spent, used as an upgrade prerequisite, or reset by a paid purchase. Use separate visual section labels `COMBAT RANK` and `PAID UPGRADE`; no arrow, progress line or grouping suggesting Rank XP pays for Level 3. Future player/profile research is a **third** system and is not part of this pass.
2. **Targeting is per placed tower** (the existing private config copy), not a global setting changing every weapon of that class or a shared `.tres`. Preserve the chosen mode across selection changes, paid upgrades, rank promotions, construction and pause. Do not add campaign Save/Load infrastructure; document desired future persisted field only.
3. Expose targeting only for **aimed/target-selecting weapons** such as Battle, Laser, Blaster, Sniper and Machine Gun. **Shockwave is omnidirectional**, auto-emits without choosing a single enemy; do not offer six deceptive buttons. Likewise passive Ammo Factory/Depot have no enemy targeting. Build *previews* can show identity and stats but must not provide live-targeting controls.
4. Changing targeting mode should affect the next valid acquisition promptly without recreating a tower, rerouting enemies, paying Credits, resetting ammo, recalculating the eight-stage stat pipeline or modifying rank XP. Maintain current staggered acquisition and cached-target validity guarantees.
5. Max level: show `MAX LEVEL` with current stats only; no misleading future numbers or payment control. Building: show `CONSTRUCTING` and progress; never enable upgrades early. Upgrading: show **current operational tier** and **paid next tier**, a busy progress bar and `Cost already paid`, no second purchase. Unavailable next-tier data/empty passive levels should fail safe.
6. No selling button, combat balance changes, tower class ID switches, temporary general-purpose status engine or unrelated menu redesign.

## Existing integration points — read first

| File | Responsibility / minimal intended change |
| --- | --- |
| `UI/GameplayHUD/Inspector/InspectorPanel.tscn` | Existing panel, now non-scrollable, heading, rank, work, text stats and upgrade action. Rearrange/reuse components, add targeting buttons + clear comparison rows/sections. |
| `UI/GameplayHUD/GameplayHUD.cs` | Presentation and user-intent signals; no authority to spend or mutate enemy state. Bind six choices, active selection, compact stat rows and hide irrelevant sections. |
| `UI/UIMasterConfig.cs` + `UI/DefaultUIConfig.tres` | Width/height and palette: set editable initial layout 620×330, keep responsive containment; preserve tray category tabs, theme and background-only opacity. |
| `TOWERS/Placement/TowerPlacementController.cs` | Selected instance, range ring, event subscriptions, preview/current-vs-next resolver, paid transaction and Inspector action gate remain authoritative. Subscribe only for visible selected tower; no per-frame all-tower UI rebuilding. |
| `TOWERS/Shared/Targeting/TowerTargeting.cs` | Spatial radius query and stable-order tie-break already implement `First` and `Closest`. Extend modes without per-tower A* / GetNodesInGroup scans. |
| `TOWERS/Shared/TowerBase.cs`, type configs and `Shared/Weapons/MultiBarrel/` | Placed tower has a private config copy; weapon configs already expose `TargetPriority` in Battle, Laser, Blaster and shared ProjectileTurretConfig. Prefer a tiny **opt-in typed targeting-policy contract** instead of per-ID switches or forcing Shockwave/factories into aimed targeting. |
| `ENEMIES/Shared/Enemy.cs`, `ENEMIES/Movement/EnemyMovement.cs`, `EnemyRoute.cs`, `WORLD/ROUTING/` | Enemies already cache `RemainingPathDistance` for their **current leg**. With linked checkpoints this is **not** always the total distance to Citadel; IT4 addresses that limitation. |
| `TOWERS/Shared/Stats/Pipeline/` + `TOWERS/Shared/Ranks/` | Use existing source/rank-aware `ResolveLevel` for current/future effective stat snapshots; do not duplicate stat arithmetic or accumulate purchased level rows. |

## IT1 — Refresh the actual Inspector layout (UI only)

- [x] Rework existing `InspectorPanel.tscn` into labeled **TARGET PRIORITY**, **COMBAT RANK**, **PAID UPGRADE**, and compact ammunition/runtime information and tooltips. Keep work/status compact in header; when busy its current progress bar appears visibly. Reuse existing signals/paths or update all callers/tests safely.
- [x] Default `InspectorWidth=620`, `InspectorHeight=330`; widen the exported range beyond 480 and update `DefaultUIConfig.tres` + scene sizing. Clamp to viewport without clipping bottom tray, buttons or right edge. Use a MarginContainer with two columns; no scrollbars. Maintain 90% (configurable) **background** opacity while text/buttons stay opaque.
- [x] Keep selected tower range ring, highlight, close button, bottom-right position, independent Ctrl tray, Pause/Results input precedence, keyboard focus and pointer interception. Empty space must not intercept gameplay clicks.
- [x] Only show options suitable to the selected type; don't show a blank Rank/target/upgrade band on build preview or passive buildings.

**IT1 done:** the existing Inspector is visually clearer, wider and shorter, with coherent separation of earned progression and paid purchases and no behavior changes.

## IT2 — Authoritative current → next upgrade comparison

- [x] Replace the **dense freeform upgrade text** with a structured per-stat table: `STAT | CURRENT | NEXT LEVEL | GAIN` and e.g. `33 | 36 | (+3)`. Each row comes from **typed existing current effective `FinalStats` and the same next-level `ResolveLevel` used for real purchases**; no parsing of `DescribeStats()` / `DescribeChanges()` strings or hardcoded table values.
- [x] Include only valid weapon-specific rows: e.g. projectile tower Damage/Range/Fire rate/Projectile speed, Laser DPS/Range and relevant damage response, Blaster blast radius/edge falloff, Shockwave damage/range/expansion/interval, optional ammo capacity; omit irrelevant values (Laser has no projectile speed, passive factory has no invented damage). Allow a narrow type-owned **presentation row provider** if necessary; avoid complex reflection or a growing HUD tower-type switch.
- [x] Delta is calculated `future - current`, formatted with a literal explicit sign and parentheses `(+3)`, `(-2)` or an em dash for no change. Color by **benefit**, not numeric sign: lower cooldown/interval can be an improvement; nonnumeric changes (damage type) show `Kinetic → Energy` or `Changed` without fake numeric delta. Avoid bad float noise, NaN/Infinity and false +0 after rounding. Include units and readable decimals.
- [x] Rank modifiers already included in the snapshots must display accurately (including damage rounding). Recompute only on selected tower/level/rank/config input changes; ordinary XP increments should update **COMBAT RANK only** until promotion. Display enough unchanged rows for context, with secondary technical information collapsed.
- [x] Existing wallet `TryPurchase` remains the sole authority. Keep exact multi-resource prices, affordability, expected-level/repeat guards, next-tier `WorkSeconds`, purchase-at-start and upgrade progress. While Upgrading, show old operational stats versus already-paid target stats; never apply them early.
- [x] Keep Ranks in their **own** independent card/section, with existing within-rank bar, next-rank threshold and current total bonus; never show Credits or `Upgrade to Level 3` inside that section.

**IT2 done:** every supported offensive tower displays correct current/next values and readable actual gains, not guessed +3 labels.

### Current implementation record — IT1–IT4

The initial vertical scrolling IT1/IT2 layout was replaced following direct user feedback. Current code uses a compact620×330 two-column MarginContainer, typed comparisons (including Slow and current-only max-tier stats), six opt-in targeting buttons and shared budgeted checkpoint-tail estimates. Existing Kinetic snapshots with null Weapon now correctly receive projectile/damage/penetration rows. Tests were migrated to the real new node hierarchy; one stale rank-XP assertion was corrected from2000 to15500 to match its own fixture arithmetic. See `TESTS/UI/InspectorTargeting.md` for executed suites and inherited build/full-suite blockers. No desktop screenshots are claimed.

## IT3 — Six per-tower target priorities and UI action

- [x] Expand `TowerTargeting.Priority` from `First, Closest` to **First, Last, Closest, Farthest, Strongest, Weakest**. Keep existing enum numeric values for First and Closest (old saved/Inspector enum values should not silently change), and leave current default `First`. The UI can order buttons independently.
- [x] `Closest` / `Farthest`: minimum / maximum **physical distance from this tower** among living enemies within weapon range. Use squared distance; no square root per candidate.
- [x] `Strongest` / `Weakest`: largest / smallest **currently remaining durability = hull Health + CurrentShield** among eligible living enemies. This is not maximum authored HP, size, armor (currently placeholder), DPS, bounty, or an enemy's original spawn durability. Document this exact policy.
- [x] `First` / `Last`: shortest / longest **valid estimated remaining route to the Citadel**, across all portals, independent of starting portal and independent of movement speed. Ignore enemies with invalid, stale or non-finite path scores for **both** First and Last. **Do not treat positive infinity as a valid longest route.** Stable existing registration order breaks equal-score ties.
- [x] Continue querying only the `EnemyRegistry` spatial index within firing range, with compact visitor scoring; don't materialize collections or scan every enemy from every tower. Use existing retarget intervals/timing; mode changes should request one prompt reacquisition rather than duplicating target/weapon process loops.
- [x] Add an explicit narrow opt-in weapon targeting-policy API so one placed Battle can be set `Weakest` while another remains `First`, without editing the shared default `.tres`. Preserve typed config semantics and extend Laser/Blaster/Sniper/Machine Gun through their existing policies. **No choices for Shockwave/factory/depot.**
- [x] Wire six Inspector controls as one mutually exclusive group with distinct selected styling, readable tooltips and keyboard focus. Show current choice correctly on reselect, upgrade, pause/resume and independent level instances; hide for build previews. Avoid invoking purchase, stat or rank events when selecting a priority.
- [x] Add test coverage for all six rankings, equal-score ties, zero/one/500+ candidates, shields restored/damaged, out-of-range and removed enemies, unsupported tower type and instance-specific mode independence.

**IT3 done:** six choices are functional for aimed towers and are independent per placed instance.

## IT4 — Multi-portal/checkpoint correctness, regression and live verification

**Original limitation (now addressed by JourneyEstimateCache):** `EnemyMovement.RemainingPathDistance` currently follows only the active route leg. A ship on `Portal → Checkpoint A → Checkpoint B → Citadel` may have only 40px until its next checkpoint, then another 600px to the Citadel; an ordinary ship may have 180px total. Comparing **40 vs 180** would incorrectly call the first ship `First`.

- [x] Design a **bounded, cached remaining-journey estimate** used by both First and Last: current valid refined leg length **plus estimates of the still-required chosen checkpoint legs and last Citadel leg**. Preserve the enemy's **already-captured checkpoint graph/branch choice**; never reroll branches, bypass required regions or spawn fake enemies.
- [x] Use existing navigation/path geometry, revision/profile/clearance information and centralized bounded work/cache where appropriate. **Do not launch new A* searches per tower-target comparison or per enemy movement frame**; avoid hundreds of synchronous downstream A* searches during burst spawns/repaths. Share/cache downstream estimates by relevant network/radius/route revision when feasible; invalidate carefully when map/checkpoint topology or chosen downstream goals change.
- [x] Downstream geometry is not necessarily known until the enemy reaches each checkpoint, so a numerically exact final travelled distance may be impossible in advance (weighted route diversity and dynamic new towers matter). **Be explicit that this is an estimated remaining journey**, never silently present current-leg length as full Citadel distance. If estimates are unavailable/invalid, apply a deterministic safe fallback and document it; avoid `Infinity` winning Last. Do not degrade enemy movement or existing path safety to improve a UI number.
- [x] Targeting test: Enemy A has 40px to a checkpoint + an estimated 600px further; Enemy B has 180px straight to Citadel. `First` must prefer B; `Last` must prefer A (when both in weapon range). Test 2+ portals, linked/branched checkpoints, repaths, blocked routes, moving checkpoint geometry and ordinary no-checkpoint parity.
- [x] Run Godot C# build and actual headless suites: `TESTS/Combat/SpatialQueryChecks.cs` and `TESTS/Performance/SpatialQueryBenchmarks.md`; enemy movement/nav/checkpoint/corridor/DeathSpawn; `TESTS/UI/HudTowerChecks.cs`, `TESTS/UI/MenuTowerFlowChecks.cs`, rank, upgrade, construction, ammo, Battle/Laser/Blaster/Shockwave/Sniper/Machine Gun and mixed passive catalog. Report *executed* results and inherited environmental failures.
- [ ] Manual Godot desktop review at target resolution and a smaller viewport: bottom-right panel width/height, side-by-side numbers, no scrolling, tooltips, focus, range ring, simultaneous tray, six priority actions, selection swapping, construction/upgrade states, max tier, no combat Rank→Credits ambiguity. Capture screenshots if available; don't claim desktop QA from headless tests.
- [x] Update `UI/GameplayHUD/Inspector/README.md`, `TOWERS/Shared/Targeting/README.md`, relevant `TOWERS/` and `TESTS/` notes, and `NOTES/WORK.md` **only after verified implementation**.

**IT4 done:** `First`/`Last` represent the Citadel journey consistently across multiple portals and checkpoint chains, and the redesigned Inspector has verified automated gameplay/layout regressions. Manual visual review remains pending.

## Scope exclusions

Do **not** implement tower selling, a new targeting damage type, enemy armor/shields, new tower classes, player/profile XP, Research, campaign saving, generic combat statuses, reticle animation or new HUD windows. Do not change authored tower balance or rework existing enemy A* algorithms. The shared target query stays small and performance-conscious. The default existing `First` behavior should remain unchanged for a normal no-checkpoint enemy; **checkpoint ranking correctness is an intentional improvement, verified separately**.

## Suggested Codex handoff

Read `UI/GameplayHUD/Inspector/INSPECTOR_TARGETING_PASS.md`, `NOTES/CodePreferences.md`, the current Inspector and tower targeting source. Implement **IT1 → IT4** sequentially, validating after each. Deliver the wider/shorter bottom-right Inspector, visually separate earned Combat Rank from Credits upgrades, typed `CURRENT/NEXT/GAIN` comparison, and a six-mode *per-instance* targeting selector (excluding nontargeting structures). Fix/check multi-checkpoint First/Last without per-tower pathfinding or mass-frame regression. Update checklists with actual verification; preserve unrelated ongoing Codex changes.
