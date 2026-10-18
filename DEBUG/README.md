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
