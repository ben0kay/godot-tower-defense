## Rendering and frame diagnostics

F2 now adds p95/p99 wall-clock frame spacing over the latest 600 observed frames, latest viewport rendering CPU/GPU time and global CPU setup time. Native rendering measurement runs while the overlay is visible and MeasureRendering is enabled on PerformanceOverlay. Set it false in the Inspector to compare measurement overhead. Unsupported, uninitialized or zero native readings display N/A. These timings have different scopes from engine process/physics and should not be added together. Frame history includes paused frames while the overlay is visible.

Additional rows show managed heap size, process-wide GC collection totals by generation, registered fog contacts (including offscreen and partially revealed contacts), enabled/allocated pooled lights, projectile-light leases, global exhaust/explosion counts, tracer-cache entries and the three visual toggle states. F3 saves these even without retained physics-profile samples. Counts and toggles in the report are snapshots at export, rather than averages over the physics capture. Heap values cover managed memory, not total process or GPU memory.

The path row highlights its whole scheduled request/journey-work slice, including refinement, against the authored soft budget. A* has its own nested peak row with the number of physics ticks that exceeded that path budget during the peak window. The default 2 ms budget turns the path row yellow at 2 ms and red at 4 ms. A single operation can overrun the soft budget before the scheduler yields. Peaks persist for the configured observation window, default two seconds. These warnings describe budget pressure. Frame percentiles and CPU/GPU readings provide the accompanying context.

No new enemy/projectile timing hooks, node scans or per-instance callbacks were added. Visual count reads use existing owner state, with a bounded scan of the light pool only. Rendering CPU/GPU numbers still need verification on the owner's renderer/hardware. Godot APIs are documented under [viewport rendering measurements](https://docs.godotengine.org/en/stable/classes/class_renderingserver.html#class-renderingserver-method-viewport-get-measured-render-time-gpu).

---

## DBG1 — Developer Hub

In a running debug battlefield, press **F1** and choose **1 Enemy Spawner**, **2 Building Lookup**, or **3 Route Preview**. F1 returns from a child to the hub. Escape closes the active panel. Building Lookup browses all catalog entries, with authored finished tiers, native artwork bounds, and Actual Size and Fit views.

**F2** toggles Performance, **Shift+F2** toggles profiling, **F3** saves retained samples, and **F4** cycles notification samples. These keys work with the hub closed. F9 Audio Tester is retired. Shortcut references in older entries below are historical.


The **Visual Debug** panel stays at the middle-right during debug battlefields, independently of F1 and Escape. It contains Enemy Explosion FX, Enemy Health Bars (both hull and shield meter rows), Impact Stagger, and Textured Tracers. Impact Stagger controls the speed dip and cosmetic shove and clears active reactions when switched off. The tracer switch applies live to friendly, hostile and standalone fallback tracers. Unchecked selects the existing rendering. Switches reset for each battlefield. The panel follows viewport resizing and hides with the mission outcome.
---

## PW1 F4 power topology diagnostics — 2026-11-04

F4 now includes two read-only rows showing cached island/cell totals, rebuild count/last topology milliseconds, and hovered cell/island/cell count. Island 0 means empty, nonconducting, decorative or outside terrain. Existing profiling rows keep their indices; these two rows remain visible without enabling subsystem profiling. Uses the existing throttled refresh, no terrain/tower/enemy scans or new polling loop; hiding F4 stops sampling. No capacity/powered status exists yet. See [power plan](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md) and [owner checks](../TESTS/Power/README.md). Build/runtime/visual acceptance pending.

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

## F2 batch spawner and centralized debug tools — 2026-10-21

F2 now toggles an enemy batch menu, not immediate catalog cycling. Pick a catalog enemy, amount1–10,000 and spawn gap0–60 seconds. Each accepted ship independently samples a valid registered portal uniformly through production source/clearance/checkpoint/Citadel rules. Debug batches use their own RNG and do not emit WaveEnemyCreated; authored wave accounting/randomness are unchanged. Zero gap is bounded to16 spawns per physics tick (Inspector MaxSpawnsPerTick, clamped1–64); positive gaps emit at most one ship per physics tick, rounded up to the physics interval. No catch-up bursts after hitches. Pause freezes pending work. Close/F2/Escape hides without cancelling; Cancel stops only remaining work. Any delivery failure stops the batch and shows the reason instead of retrying forever. Existing enemies continue normally. Disabling the level debug controller cancels pending work. No physics callback remains active when idle.

Tool source now lives under DEBUG: LevelTools (controller/reusable map scene), Spawning (new menu and unbound LegacyCatalogSpawner), Navigation (F3 preview planner/overlay and editor checkpoint design overlay), Alerts (F7 preview), Audio (F9 tester), Progression (optional menu XP grant partial), plus existing performance overlay/profiler. Scene/script UIDs are retained and map/HUD/autoload references updated. Scene instances stay with their level/HUD/audio owners; production navigation/combat services and small profiler/attachment hooks retain their normal ownership. Automated tests remain under TESTS. Existing F3/F4/F5/F7/F9 behavior is retained; historical F2-cycling descriptions below are superseded.

Verification: TESTS/Debug/EnemySpawnMenuChecks.tscn and existing PerformanceOverlayChecks pass. Random portal test uses40 seeded deliveries across two registered portals and rejects edge fallback. Build/import passed. Desktop appearance review remains with the user; broader previously documented baseline-suite failures are not claimed fixed.

---

## Safety cache pressure counters — 2026-10-20

F5 adds world-lifetime segmentSafetyEvictions. Ships retain their current segment result until geometry/radius/navigation changes, so shared-hit growth no longer counts every warm movement tick. Consults can reuse uncertain results, which retain swept fallback. A full shared cache alone does not prove churn; compare build/eviction deltas while the same segments remain active.

## Segment safety counters — 2026-10-20

F5 report metadata includes world-lifetime segmentSafetyHits, segmentSafetyBuilds and current segmentSafetyEntries. Hits include cached uncertain results, which still use original movement sweeps; a hit alone does not mean a sweep was skipped. Compare Clearance CPU timing separately. Counters persist when entries are invalidated and reset with the NavigationGrid lifetime.

## Shared-route report counters

F5 reports now include `sharedRawHits`, `sharedRefinedHits`, and `sharedTemplateBuilds` in workload metadata. These are cumulative for the current world, whereas CPU rows cover only the retained sample window. See `WORLD/NAVIGATION/README.md` for the global Shared Route Pool toggle and bounded variation settings.

## PF2 subsystem measurements — 2026-10-20

F4 keeps the normal performance panel; **Shift+F4** starts/stops optional C# subsystem profiling. **F5** prints and saves the retained report to Godot `user://Debug/pf2-*.txt` (the absolute path is printed). Profiling defaults off. Hiding F4 stops profiling; reopening starts a fresh capture if it was enabled. Pause adds no physics samples; reports remain available. Leaving the battlefield clears the active capture. Only one battlefield captures at a time; owner filtering prevents another test world's calls from entering it.

The old top-left `GridDebugOverlay` Info labels, script and formatting loop are removed. F2 catalog spawning, F3 route preview, checkpoint design overlays and normal gameplay controls remain. The F4 panel now sits directly below the top HUD and scales to the free space above the lower HUD; all Controls remain click-through.

### Reading the measurements

Rows show **mean / p95 / peak CPU milliseconds per sampled physics tick**, plus scope call count. Each tick aggregates all instrumented callbacks in that category. Idle zeroes count; unsampled ticks do not. p95 uses nearest-rank over the retained fixed-capacity rolling sample window. Defaults: 300 selected ticks, stride1 (about5 seconds at60Hz). Inspector controls on PerformanceOverlay: ProfileOnStart, ProfileWindowTicks(30–1200), ProfileEveryTicks(1–60), BuildLabel. Set BuildLabel to the exact tested commit/export/build name before comparing reports.

- Enemy movement: full EnemyMovement physics callback. Clearance: SegmentClear calls across movement/refinement. Registry: position-index update and position subscriber dispatch.
- Friendly towers: Battle, shared MachineGun/Sniper/Cryo/Stasis, Blaster, Rocket, Laser and Shockwave physics callbacks. Target searches: shared TowerTargeting.Find (cached lock checks remain within tower totals).
- Enemy guns: weapon acquisition/aim/fire callback, including accepted-shot production.
- Projectiles/waves: Basic, Splash, ShockwaveEmitter and central hostile-bullet advancement. Instant area damage is shown separately.
- Path work+audits: PathRequestManager callback, including route validation, navigation refresh, journey estimates and route assignment. A*: native GetIdPath only. Refinement: EnemyMovement.AcceptPath, including connections and route/dependency building.

**Inclusive/nested rows overlap: do not sum them.** Clearance and registry can also be called outside movement; area damage can occur within projectile scopes; path refinement includes clearance. Calls are invocations, not shots, distinct enemies or exclusive active-entity counts. Entity readouts are current living enemies, placed tower-container children (including passive/busy), friendly projectile-container children (including queued/visual nodes) and actual active hostile bullets. Effects rendering, GPU work and unrelated systems are not fully attributed.

Storage is fixed arrays plus a weak per-node ownership cache resolved on first observed call per capture; transient bullets are not retained by strong cache keys. Scope structs allocate no per-call Stopwatch, delegate, list or formatted text; text/p95 sorting occurs only at the throttled display refresh or explicit dump. Off hooks return immediately without reading a clock or searching nodes. Rendering/effects are intentionally left to Godot's Visual Profiler.

Observer bookkeeping is a **partial** measurement: entry/filtering and recording intervals, not all timer reads, UI, engine callbacks or sampling overhead. Nested parent times include child instrumentation overhead. Compare matched runs with profiling OFF versus ON; synthetic empty-scope timings do not establish full-game overhead. The report includes UTC timestamp, build label, scene/map, counts, camera zoom, viewport, CPU/OS, FPS cap and sampling settings. GPU utilization is unmeasured.

### How to investigate Level100

At the same zoom/resolution/build and comparable enemy/tower counts, capture a quiet movement interval, then heavy combat, then a spawn/repath burst. Wait for the window to fill before F5. Save an OFF baseline using normal F4 counters/Godot profiler, then compare ON. Use stride2/4 if detailed hooks noticeably affect timing; record the stride and do not compare unlike sampling as equivalent. Do not change authored waves or disable clearance to make timings look better. PF3 selects the bottleneck after these measurements; no optimization or FPS improvement is implied by installing PF2.

Automated validation: `TESTS/Debug/SubsystemProfilerChecks.tscn` and `PerformanceOverlayChecks.tscn`. Actual execution results are recorded in `TESTS/Debug/PF2.md`.

---

# Performance overlay

`PerformanceOverlay.cs` contains the entire self-building CanvasLayer display, cached service lookup, input and sampling logic. Each playable launcher (Main, MainLevel002, MainLevel020) attaches one `PerformanceOverlay` node. No autoload, shared HUD refactor, navigation writes or additional runtime controller scripts are required.

**F4** toggles it, including while paused. It starts visible in debug builds; release exports do not create its UI by default. Select the launcher node in Godot Inspector to edit ShowOnStart, DebugBuildsOnly, ToggleKey, refresh interval, peak-window length, margins and warning thresholds. To use another battlefield, attach this script to a CanvasLayer directly under its root, with the existing World/UI service shape.

The panel sits below the top HUD and the old level debug Info label when present. It scales down to fit above BottomArea on small viewports; extremely short windows suppress its drawing rather than overlap the HUD. All Controls ignore mouse events. Pause/Results remain higher CanvasLayers. No desktop visual screenshot/FPS guarantee is implied by headless layout checks.

| Readout | Meaning / limitation |
| --- | --- |
| FPS / frame / peak | Engine FPS; average real wall-clock frame spacing since the last display refresh; maximum in the current fixed observation window (2s default). Includes VSync/frame caps/stalls, not just CPU work. Windows reset rather than representing an exact sliding last-two-seconds history. |
| Engine process / physics | Godot's global process and physics time monitors, milliseconds. These overlap other measured work; do not sum them with navigation time. |
| Enemies | Authoritative living registry count. |
| Projectile nodes | Direct children of World/Projectiles, including any queued-for-deletion/visual nodes there; not an exact count of fired bullets. |
| Path / A* / queries | Window maximum of total path-work time, the A* subset and queries in one physics tick. The maxima may occur on different ticks; A* is already included in Path. |
| Path queue / audit queue / audit peak | Current pending requests and route validations; peak validation duration in the observation window. Queues can briefly grow after spawning/building without indicating a fault. |
| Draw calls / render objects | Godot frame rendering counters, not GPU timing. Shows N/A when headless. |

Display text refreshes four times a second by default. Visible overlays sample only scalar timings each render/physics callback to retain brief peaks; services are cached once and projectile counts read only at display refresh. No enemy scans, tree-wide searches, new A* queries or per-frame formatted strings. Hidden overlays stop both sampling loops; F4 remains active. Paused overlays continue to show the panel and global frame metrics but do not resample stale navigation work. Reopening resets the observation window.

Colors are pressure hints: frame average >=22ms amber / >=33ms red; path peak compared against the configured soft navigation time budget (amber at budget, red at twice budget); path queue >=32 amber / >=128 red. Other rows remain informational. These are editable heuristics, not diagnoses. Use Godot's profiler to identify exact expensive methods.

See `TESTS/Debug/PerformanceOverlayChecks.tscn` for actual-service counts, layout, mouse pass-through, pause, F4/key repeat and F2 wiring checks on all three maps.
