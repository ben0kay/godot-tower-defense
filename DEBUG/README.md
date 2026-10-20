## PF2 subsystem measurements — 2026-10-20

F4 keeps the normal performance panel; **Shift+F4** starts/stops optional C# subsystem profiling. **F8** prints and saves the retained report to Godot `user://Debug/pf2-*.txt` (the absolute path is printed). Profiling defaults off. Hiding F4 stops profiling; reopening starts a fresh capture if it was enabled. Pause adds no physics samples; reports remain available. Leaving the battlefield clears the active capture. Only one battlefield captures at a time; owner filtering prevents another test world's calls from entering it.

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

At the same zoom/resolution/build and comparable enemy/tower counts, capture a quiet movement interval, then heavy combat, then a spawn/repath burst. Wait for the window to fill before F8. Save an OFF baseline using normal F4 counters/Godot profiler, then compare ON. Use stride2/4 if detailed hooks noticeably affect timing; record the stride and do not compare unlike sampling as equivalent. Do not change authored waves or disable clearance to make timings look better. PF3 selects the bottleneck after these measurements; no optimization or FPS improvement is implied by installing PF2.

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
