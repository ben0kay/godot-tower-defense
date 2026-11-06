## Obsidian Dreadnought and larger ranked F2 picker — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** The default enemy catalog now has 16 ships, including its first **Capital / Champion / Fighter**, `obsidian_dreadnought_mk1`. Native 352×208 black/charcoal armour with sleek crimson accents, baked vector lighting, four reinforced weapon sockets and a separate native 24×72 rotating cannon sprite. Four independent existing EnemyMinigun mounts sit at front-left/front-right/rear-left/rear-right positions (112,−62), (112,62), (−104,−62), (−104,62), each with its own aiming/target/cooldown state and exact barrel-tip Marker2D (0,−32).

**Provisional balance:** 4,500 HP, 22px/s, 1.5 hull turn speed, 20 Citadel arrival damage, 100 kill Credits, no default shield and 2.5 death-explosion multiplier. Each gun uses the Heavy Gunner baseline: 275px range from its pivot, 5 Kinetic damage, 2.5 shots/s, 650px/s unguided red tapered 14.6×2.9 tracers, 2.8s lifetime, accepted-shot muzzle flash. Four guns can fire independently; nominal aligned total is 10 bullets/s / 50 DPS. Dedicated Capital config/visual resources allow later balance tuning without changing either Gunner variant. Existing spatial tower queries, staggered acquisition, shared bounded hostile bullet renderer/collision, stasis/pause/host removal and generic damage/status/navigation/XP/Credits systems remain authoritative. The ship intentionally keeps the existing large-enemy **24px navigation clearance** rather than changing map/path rules to match its much larger cosmetic hull. No special boss AI, wave insertion, new weapon system or autoload.

**F2:** 1,000px minimum panel, 560px default left picker column, 40px selector and 17px selected text; popup uses selector width and a viewport-bounded scrollable height (620px default). `EnemySpawnMenu` exposes picker width/height. Existing class headings remain, smallest to largest; rows show **Name — Rank**, ordered within each class by ascending rank, then name and stable ID. Header placeholders preserve exact scene/preview/spawn mapping. Hover preview still copies only native sprite art, now including all four Capital guns. Batch caps, timing, portals, shields/stealth flags and authored wave ownership are unchanged. Capital is available for F2 and future Wave Editor authoring; **no existing wave resource changed**.

**Minimal validation:** source/resource/SVG XML review and a local Inkscape hull+four-gun composite render inspected. Focused existing F2 and gunship fixture expectations updated for rank order/labels, 16 catalog ships and Capital composition; C# fixtures **not run** because .NET is unavailable. No Godot gameplay, weapon/balance or performance acceptance claim. Owner: rebuild, open F2 → CAPITAL → Obsidian Dreadnought — Champion, spawn one, and inspect native size, slow turning, four independently aimed guns/barrel origins, red tracers, stasis/death cleanup and lower-rank-first menu ordering. Keep owner wave balance unchanged until separately requested.


---

## Colossal class order — 2026-11-05

F2's existing catalog-driven grouping now sorts via centralized `EnemyNaming.ClassSortOrder` instead of the numeric enum value. Newly authored Colossal-class ships will appear between Capital and Titan even though Colossal's appended enum value is 7 and Titan must stay 6 for serialization. Empty categories remain hidden; the current 15-ship picker is visually unchanged because neither category has a registered ship. Focused sorting tests updated; in-game confirmation pending.

---

## Bulwark classification correction — 2026-11-05

Fixed `medium_mk1` (Bulwark Drone) missing its serialized `Class` value. Without the explicit `Class = 2` assignment in `ENEMIES/Types/Enemy004/Enemy004.tres`, Godot uses `EnemyDefinition.Class`'s `Tiny` default; F2 was faithfully displaying and grouping that wrong value. The 64×48px Bulwark is now authored as **Standard / Common / Fighter**, matching the source roster. **F2 remains catalog-driven**, reading the current `EnemyDefinition` from `EnemyCatalog` at menu initialization; no duplicated runtime class list or automatic art-size classifier was added. After pulling, restart/reload the game level to rebuild the F2 picker. A focused F2 check asserts the classification and tooltip. Godot runtime verification pending.

---

## F2 Class-grouped enemy list — 2026-11-04 (source implemented)

The F2 enemy picker now sorts ship definitions by **Class (Tiny → Titan)**, then alphabetically by `DisplayName`, and adds **non-selectable class headings** for occupied classes. Names remain short in the dropdown; Rank/Role/EnemyId and actual-size sprite still appear in the right-side preview and tooltip. The menu index has a matched scene/definition slot for every row including each heading (null marker), so hovering a heading does nothing and clicking or attempting to spawn a heading is rejected; no erroneous sprite or enemy is spawned. Each type's stable `EnemyId` resolves to its original registered scene. Batch behaviour, spawn cap, portals, shields, stealth and enemy stats are unchanged. `TESTS/Debug/EnemySpawnMenuChecks.cs` now checks class ordering, header mapping and preview/spawn correspondence without assuming menu index equals catalogue index. Godot.NET interactive verification pending.

---

## F2 classification details — 2026-11-04

The picker uses short enemy display names in the existing catalog order. The preview caption shows **Name, Class, Rank, Role, stable ID and 1:1 artwork dimensions**; tooltips include metadata/ID. Highlighting and dismissing the popup continue to use the same scene indices; no spawn/batch processing changed. Classification text updates only when the previewed type changes. See [roster](../../ENEMIES/Types/ROSTER.md). Owner Godot preview/spawn check pending.

---

## F2 friendly enemy labels — 2026-11-04

The F2 enemy dropdown now shows **Role · DisplayName (stable EnemyId)** rather than only the ship name. It retains the **original scene-catalog order** and selected index → preview / batch mapping to avoid changing debug spawning behavior. Four vague display names were clarified via the underlying `EnemyDefinition.tres` resources. See [fleet roster](../../ENEMIES/Types/ROSTER.md) for numbered source folder mappings. No spawn frequencies, caps, wave ownership or scene assets changed.

---

## F2 hover-to-preview enemy artwork — 2026-10-21

Opening the F2 enemy OptionButton dropdown now previews the enemy under the mouse **immediately without selecting it**. Arrow-key-highlighted rows use the same behaviour. Clicking a row still commits the enemy used by "Spawn batch"; hovering alone never changes the chosen type or submits/spawns a batch. Dismissing the list with Escape, an outside click, or reopening F2 without choosing returns to the committed selection's artwork and native-size caption.

Godot PopupMenu does not fire `IdFocused` for mouse hovering, so `EnemySpawnMenu` reads `PopupMenu.GetFocusedItem()` from a lightweight `_Process` callback **only while its dropdown is open**. A cached preview index prevents recreating sprites when the highlighted item is unchanged, and polling stops on popup hide/F2 close. The 1:1 artwork copier, scroll behaviour, shielding/stealth checkboxes, batch timers and spawn selection are unchanged.

`TESTS/Debug/EnemySpawnMenuChecks.cs` now exercises highlighted-row transitions, unchanged spawn selection/registry count, dismiss-to-restore and idle processing. Godot/.NET runtime tests have **not been executed in this environment**; verify with `res://TESTS/Debug/EnemySpawnMenuChecks.tscn` and desktop mouse hover, keyboard highlight and Escape.

---

## F2 actual-size sprite preview — 2026-10-21

The F2 panel is widened into left-side batch controls and a right-side sprite preview. Changing the enemy selector updates authored artwork at1:1 scale, independent of battlefield camera zoom, with displayed pixel bounds. Authored root/sprite transforms, offsets, frames, tint and separate gun layers are retained. Oversized artwork scrolls rather than being scaled to fit.

DEBUG/Spawning/EnemySpritePreview.cs inspects the selected scene off-tree and copies only Sprite2D presentation, then immediately frees the source. No gameplay enemy is registered and no weapon/movement/particle scripts are attached to the preview. It updates only on selection/resize and has no process/physics loop. Existing batch spawning, flags, progress/cancellation and portal policy remain.

Build/import and EnemySpawnMenuChecks passed: all catalog previews, native hull texture/scale, no attached scripts/live enemies, two Gunner layers, and a synthetic1000×600 authored preview remaining full-size. Existing menu/batch/flag checks pass too. Desktop appearance review remains with the user.

---

## Independent F2 batches and spawn flags — 2026-10-21

F2 now accepts new batches while previous ones are still spawning. Every click captures its own enemy, amount, gap, Shielded and Stealthed options. Both flags default off and can be combined; shields use the enemy definition's existing MaxShield (zero capacity stays zero), and stealth uses the existing targeting/visual state. No new shield/stealth mechanic or authored resource values are introduced.

A scrollable active-batch list shows captured settings and progress, with a Cancel button per row. Cancel All stops all pending batches; closing F2/Escape leaves them running. Completed/cancelled/failed rows retire. A failure stops only that batch and reports its reason. Aggregate Spawned counts successes since the current group began (reset on starting from idle). Batches retain independent timers and rotate fairly under one shared MaxSpawnsPerTick budget (default16 across all batches, not16 each); positive gaps remain at most one delivery per batch per physics tick, with no catch-up bursts. Pause freezes all pending timers; idle processing stops. Independent debug RNG, random valid portals, active-enemy limit, wave isolation and existing gameplay consequences remain.

Build/import and focused menu tests passed: concurrent distinct types/intervals, shield-only/stealth-only/combined flags, actual checkbox/button submission, global budget/fairness, per-row cancellation, prior pause/close/random-portal checks. Previous one-batch descriptions below are historical and superseded. Actual tools stay under DEBUG; checks stay under TESTS.

---

# Debug enemy spawning

EnemySpawnMenu.cs owns the F2 panel and one bounded pending batch. LevelTools/LevelDebugController attaches it to each live battlefield. Catalog choices come from the actual EnemyCatalog; no duplicate enemy list or balance table. Amount and gap are captured at Start. Zero gap is throttled by MaxSpawnsPerTick; positive gaps are physics-tick limited and do not catch up in bursts. Pause freezes the batch. Closing the menu leaves it running, Cancel removes pending ships only, failed delivery stops with a reason. Existing production active-enemy limits remain.

SpawnManager.SpawnRandomPortal uses its shared reservoir sampler and production portal validation, with independent debug RNG and no wave-owned creation event. Portal-only means no fallback to map edges. Debug deaths/breaches retain normal gameplay consequences. No debug waves or mission statistics are fabricated.

LegacyCatalogSpawner.cs parks the old catalog-order cycling logic without a key handler or scene attachment. It remains callable for old fixtures; F2 no longer calls it. Tests: TESTS/Debug/EnemySpawnMenuChecks.tscn.
