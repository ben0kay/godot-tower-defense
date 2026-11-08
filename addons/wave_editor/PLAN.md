## Layout V2 fix (2026-11-01)

After further screenshot evidence showed the earlier nested anchored content could still collapse, the editor root was simplified to a directly expanding `VBoxContainer`. The toolbar and footer are short; the central wave/batch/details split fills available height. The versioned header identifies whether Godot loaded the updated C# plugin. Check this first before making more layout changes. No other backlog changes.

---

## Editor-layout acceptance fix (2026-11-01)

- Fixed collapse of the plugin's **inner** layout with a full-rect anchored content margin, after the outer tab `ExpandFill` fix proved insufficient in a user screenshot.
- Moved the completed WP1 live composition preview to the **top of the selected batch details**, and reset the details scrollbar when switching waves/batches.
- Needs local Godot editor verification: whole-height wave/batch lists, preview immediately after timings, footer at bottom, and no visible layout overlap/clipping at normal IDE dock sizes.

---

# Wave Editor — quality-of-life improvements

**Branch:** `dev` · **Updated:** 2026-11-08

The main-screen `addons/wave_editor` Godot plugin edits `WaveSetResource` `.tres` files, preserving the existing `WaveManager`, enemy catalogue and exact weighted spawn scheduling. This is a living *editor UX* backlog; do not change wave gameplay, level resources or mission registration merely for editor polish. Prefer event-driven updates with no idle `_Process` or `_PhysicsProcess` polling.

## Completed — WP1 (2026-10-31)

| Item | Result |
| --- | --- |
| **Live batch composition** | Shows exact per-entry ship counts and percent bars using the **same** `WaveCatalog.CompositionCounts` largest-remainder algorithm as real spawns, including stable tie breaks. Updates on count, weight, identity/flags, selected batch and entry add/remove/reorder, with no random re-roll or scheduler effects. Distinct shield/stealth/portal entries remain distinguishable. |
| **Context tooltips** | Count, Interval, After, Weight, Wave Number, Portal, Shield, Stealth, Alert, and presentation controls explain values, examples and timing semantics. |
| **Deletion confirmation** | Wave, batch and enemy entry deletions require a modal confirmation. A pending action captures exact resource identity, is cancellable, and cannot silently delete another selected row. The `.tres` isn't saved until Save. |

The resource schema and Level003 wave data remain unchanged; this is mostly editor-side UI behavior. A focused source regression check compares preview quotas with actual composition and stable ties. Godot.NET build and manual editor check pending.

## WP2 — Enemy sprite and quick stats

**Source Implemented / Awaiting Owner Validation.** The selected entry previews the registered hull sprite and catalog stats. Missing IDs remain visible and editable.

## Backlog — remaining suggestions

| Priority | Idea | Relative effort | Proposed outcome / acceptance |
| --- | --- | --- | --- |
| WP5 | **Optional automatic wave renumbering** | Small | Offer a toggle / one-click renumber after move/insert/delete; default preserve authored numbers and document that list order—not number—governs wave sequence. Never silently renumber an existing level. |
| WP6 | **Save As / Duplicate Wave Set** | Small | Clone the open `.tres` (including nested wave/batch/entry resources) to a new user-selected project path, validate it, avoid overwriting without confirmation, and leave original source untouched. New level still needs `LevelConfig.WaveSource` assigned manually. |
| WP7 | **Enemy search/filter in chooser** | Small–medium | Search by display name or stable ID above/within the catalogue dropdown. Preserve the exact stored ID, show deprecated/missing IDs explicitly, and refresh choices only on demand or input changes. |

## WP4 — Wave summary and uncapped Fleet Threat Index (revised 2026-11-08)

**Source Implemented / Awaiting Owner Validation.** See the [implemented v1 formula and exclusions](README.md). The proposal below records the original design and acceptance criteria. This is an **editor-only planning aid**, not an authoritative prediction of victory, campaign difficulty, score, rewards or scaling. Do not change waves, enemy stats, runtime spawning, saves or the research/progression systems.

### WP4-A — exact wave statistics and schedule

- Add a compact **Wave Analysis** panel for the selected wave, close to its existing wave fields/timeline. Show exact enemy total and batch count; exact enemy-type quantities; estimated combined **hull HP**, **shield HP actually available at spawn** (respect each entry's Shield flag rather than assuming MaxShield is always full), and **potential Citadel damage if all scheduled ships reach it**. Break out shielded, stealthed and mixed entries from exact per-entry counts. Label all these as theoretical authored-wave totals, not live combat metrics.
- Derive counts from the existing `WaveCatalog.CompositionCounts` (same stable quota rounding as the current batch preview); never sample random outcomes or merely multiply percentage weights.
- Derive first/last scheduled spawn times from the **existing SPAWN4 timeline logic**, respecting Sequential/Parallel, StartOffsetSeconds and the distinction between nonfinal After gaps and the final batch. A parallel wave's total horizon is the maximum group end, **not the sum** of individual group durations. Display caveats about blocked portals, portal opening, spawn budgets, movement/combat and death-spawn children.
- Event-driven recalculation when wave/batch/entry selection, amounts, weights, enemy IDs, shield/stealth flags, start offset, interval, after, or schedule changes; and on enemy-catalog refresh. No idle polling, map load or real enemy instances each update. Handle invalid or unregistered entries with an explicit **unavailable/incomplete** state (not fake zeros).

### WP4-B — calculated Fleet Threat Index (NOT 0–10)

**Presentation:** Display **`FLEET THREAT · 50.0`** or **`FLEET THREAT · 78.0`**, one decimal, **no upper bound or 100-point cap**. Higher = stronger/more demanding. Keep a comparable fixed global model across all missions; do not rescale each wave relative to the current level, assume 10 is maximum, or turn it into a percentage. Prefer a numerical index and an optional transparent breakdown over arbitrary Easy/Hard labels. These display examples are illustrative, not asserted current-level results.

**Data contract:**
1. Reuse exact quota counts from WP4-A; never infer enemy strength from `EnemyId`, asset number, sprite size, or the physical `EnemyClass`. `EnemyRank` and `EnemyRole` are descriptive metadata, not automatic combat modifiers. Real gameplay capabilities are decisive.
2. Use `EnemyCatalog.DefinitionFor(id)` (with existing metadata cache) for `MaxHealth`, `MaxShield`, `Armor`, `MoveSpeed`, `CitadelDamage`; respect per-entry initial Shield/Stealth flags and immunity if it is relevant to the estimator. Count each ship once, even if it appears in multiple authoring entries.
3. Where supported and reliable, read **actual configured hostile weapons** (mounted guns, shot damage × shots/sec and barrel count, including Capital mounts) and **actual ability configurations** (e.g. death-spawn offspring and shield-emitter support). Gather optional scene/config facts at most once when opening/refreshing the catalog and cache a lightweight per-type threat snapshot; do **not** instantiate enemy scenes, scan maps or do resource discovery on every preview edit. Use existing shared stats/config, never a separate handwritten per-ID strength table. Explicitly mark *unmodelled abilities/weapons* and **partial estimate** rather than silently declaring unknown weapons zero or inventing values. Do not count death-spawn children in the authored ship total; show their possible contribution separately and avoid recursive/double counting.
4. Give durability (hull, usable shield, actual armour effect), leak consequences (CitadelDamage), speed/interception time and hostile weapon DPS **distinct contributions**. A single summed HP pool is specifically **not** an acceptable threat score; shielded ships, armed ships, fast raiders, carriers and support units should have distinct rational effects. Where an ability cannot be evaluated quantitatively, show the omission; do not add unexplained magic multipliers based solely on class/rank/role.
5. Apply an additional *bounded* wave-pressure factor for spawn cadence and simultaneous attack groups based on WP4-A timeline. More tightly packed waves should not rate lower than identical fleets spaced out. Keep any qualitative role/stealth assumptions modest, documented and calibrated; do not invent success rates or use real game-state/tower loadouts.

**Initial calibration proposal (versioned heuristic v1; Codex may refine coefficients with documented reasoning):**
- Normalize around **10 plain Standard Drones (`basic_mk1`) at roughly one ship/sec ≈ 50 index points**. This is an easy-to-understand benchmark, not a per-level min/max. Changing the reference later requires a documented estimator-version adjustment, not silent renumbering.
- A transparent starting per-ship formula is `shipPoints = 5 × (0.45 × durability + 0.25 × leak + 0.15 × speed + 0.15 × weapons)` where each dimension is **1.0 for the unshielded, unarmed baseline Standard Drone**. Suggested definitions: `durability = sqrt((HP × armorMultiplier + initialShieldHP) / 60)`; `leak = sqrt((1 + CitadelDamage) / 2)`; `speed = clamp(sqrt(MoveSpeed / 100), 0.5, 2)`; `weapons = 1 + min(3, mountedWeaponDps / 15)`. Use `initialShieldHP = MaxShield` only when that authored entry actually spawns shielded; armour must be estimated from the *real damage formula* when practical, otherwise use a clearly labelled bounded heuristic (no claim of literal effective HP). Clamp only individual factor extremes where justified, **never clamp the total index**. Known capabilities beyond these inputs (such as actual death-spawn payloads) contribute separate, explained components.
- `rawThreat = Σ (exactQuota × shipPoints)` across all scheduled entries, plus documented **known, non-duplicated** ability contributions. Example cadence adjustment: `spawnRate = totalShips / max(1, lastSpawnSecond - firstSpawnSecond + 1)`; `pace = clamp(sqrt(spawnRate / 1.1), 0.8, 1.45)`. Optionally use `overlap = 1 + min(0.25, 0.08 × (maxConcurrentScheduledGroups - 1))`, with groups computed from the timeline and no artificial overlap when just one is present. `FleetThreatIndex = round(rawThreat × pace × overlap, 1)`. Keep all arithmetic finite and use wider intermediates for large authored batches.
- These coefficients are **initial game-design estimates**, *not* empirically validated enemy win probabilities. Prefer calibrated ordering against the actual starter, mixed, gunner, shield-emitter, transporter and boss wave examples over false precision. Document the exact implemented formula/coefficients in the final code or owner-facing notes; changes to this version intentionally affect comparability.

**UI and transparency:** Show the index for the selected wave, optionally index next to each wave row; show exact ship count and at least a short breakdown (survivability, speed, Citadel leak threat, guns/abilities, tempo/overlap). Explain in a tooltip: *“Relative ship-fleet threat only. Higher is harder; not a win probability or capped score. Terrain, allowed towers, upgrades, ammunition, economy, spawn blockage and player strategy are excluded.”* If inputs/capabilities are unresolved, label the score `PARTIAL` with clear missing types/capabilities; if the wave is invalid or a required EnemyId is unknown, use `UNAVAILABLE` rather than displaying a misleading number. Never modify authored wave data while analysing it.

**Acceptance / small owner review:**
- Deterministic and stable: reopening the same wave set yields the same result; weighted quotas match `CompositionCounts`. A single source-stat change updates after explicit catalog refresh.
- Monotonic sanity checks under fixed conditions: adding copies of an existing enemy, increasing hull/shield/damage, or tightening a spawn interval must not reduce the reported threat (except legitimate authored changes to abilities); compare shield/stealth on vs off where supported.
- Check at least one plain starter wave, a mixed Standard/Scout/Bulwark wave, an armed Gunner, a shield-support wave, a death-spawn Transporter wave, a Capital ship and a parallel SPAWN4 wave. The result should distinguish their qualitative dangers without claiming exact combat outcomes. A 78.0 wave should remain >50.0 across levels, not be rescaled to each level's toughest wave.
- Source sanity check and one basic build at most if practical; owner visually checks editor updates and sense of rankings. Keep implementation efficient, modular and independent from gameplay.

## Later, separate data-model proposal

**Exact Counts / Weighted Mix per batch** (medium): Let authors choose exact composition by directly entering e.g. 10 Standard + 5 Scout + 2 Bulwark, or existing weighted proportions. Decide how to reconcile total batch Count, mixed fixed/weighted entries, shuffle/ordered choice, save schema/versioning, and existing Level003 resource compatibility **before** modifying the runtime. Do not conflate this with the implemented exact-*quota* preview (which still edits *weights*).

**Optional timeline visualization** (medium): If scheduling grows complex, show the expected batch spawn/after timing line (not pathfinding or combat ETA), with no continuous editor updates.

## Implementation guardrails

- Keep `WaveSetResource -> WaveDefinition` conversion and `WaveCatalog.Compose` authoritative.
- Never change or silently rewrite existing Level003 waves when adding editor controls.
- No new runtime managers, per-frame editor redraw/poll or enemy scene instantiation on every preview update.
- Preserve the existing one-commit-per-task `dev` branch workflow and keep short, focused validation.
