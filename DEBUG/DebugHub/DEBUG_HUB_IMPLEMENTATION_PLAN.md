# DBG1 — F1 Developer Hub, Shortcut Cleanup and Building Artwork Lookup

**Repository:** `ben0kay/godot-tower-defense`  
**Branch:** `dev` only  
**Status:** Codex-ready implementation plan — **NOT IMPLEMENTED**  
**Scope:** Quick, targeted controls/debug-tool UX pass. Preserve production combat, authored assets, building costs, saving and normal gameplay controls.

## 1. Goal

Replace scattered standalone debug function keys with one discoverable **F1 master debug menu**, while keeping the few frequently used diagnostics on direct shortcuts.

The player/developer can open **F1 → choose Enemy Spawner, Building Lookup or Route Preview**, by click or by simple number key while the hub is open. The hub should be a neat, dark, compact tactical panel using existing `UI/Shared/UIButtonFactory.cs` / shared UI styling rather than a new UI theme architecture.

**No F1 tool selection should require F2/F3/F4**, because those keys have new direct meanings below. Do not turn the entire game into a debugger or hijack normal shortcuts when hub is closed.

## 2. Approved key layout

| In-game key | After DBG1 | Notes |
| --- | --- | --- |
| **F1** | **Open/close Developer Hub** | Battlefield only, debug/development builds |
| **F2** | Toggle existing **Performance Overlay** | Replaces old F4 shortcut |
| **Shift + F2** | Toggle existing **Subsystem CPU Profiling** | Replaces Shift+F4 |
| **F3** | **Save/print performance profile report** | Replaces old F5; same `user://Debug/pf2-*.txt` report |
| **F4** | Cycle **Debug Notification Samples** | Replaces old F7; same existing alert-preview sequence |
| **F5** | No in-game debug binding | Godot editor F5 remains Run Project |
| **F6** | **Leave existing actual behaviour unchanged** | Godot editor F6 remains Run Current Scene; some docs also mention an in-game lighting comparison: verify live source before touching |
| **F7 / F8** | No in-game debug binding | F7 old alert action is migrated to F4 |
| **F9** | **Unbind and stop mounting Audio Tester** | Retire synthetic audio preview; do not remove production audio |
| **F10** | No debug binding | Available for future game functions |
| **F11** | **Fullscreen** unchanged | Preserve `GameInput.FullscreenAction` and restore state |
| **F12** | No debug binding | Available |

Keep existing `Ctrl` build-tray toggle, `Escape` hierarchy, right-click placement cancellation, Right Shift+LMB repeated placement, camera arrows/WASD/mouse and gameplay input unchanged. Do not modify Godot's editor F5/F6 defaults.

**Single-owner rule:** after migration, only the new debug command router should interpret the keys listed as DBG1 diagnostics. Disable/remove the previous direct F2/F3/F4/F5/F7/F9 listeners so **no command executes twice** or under the wrong old shortcut.

## 3. F1 hub menu layout and interaction

Suggested location: `DEBUG/DebugHub/` (e.g. `DebugHub.cs`, `DebugHub.tscn`, `BuildingLookupPanel.cs`, `BuildingArtworkPreview.cs`). Adapt filenames if existing patterns suggest better, but keep one compact feature owner.

Suggested visual structure:

```text
╔═══════════════ DEVELOPER TOOLS · F1 ═══════════════╗
║  1  ENEMY SPAWNER       Browse enemies / spawn batch ║
║  2  BUILDING LOOKUP     Artwork / Levels 1, 2, 3…   ║
║  3  ROUTE PREVIEW       Portals / path diversity     ║
║ ──────────────────────────────────────────────────── ║
║  PERFORMANCE            F2 overlay · Shift+F2 trace ║
║  SAVE PROFILE           F3 report                   ║
║  NOTIFICATION TEST      F4 sample alerts            ║
║                            Esc: close  F1: toggle   ║
╚════════════════════════════════════════════════════╝
```

The header, labels and separators are schematic, not pixel requirements. Reuse existing tactical dark menu/button styling; a restrained edge accent is fine. Do **not** turn DBG1 into the proposed game-wide UI redesign.

**Navigation rules:**
- Press **F1** in a debug-enabled battlefield to show the hub. Press F1 again from the hub to close it.
- Choose **Enemy Spawner**, **Building Lookup** or **Route Preview** by mouse **or number keys 1/2/3 only while the hub has focus**. Hover/focus uses normal button states. Do not bind these number keys globally.
- Selecting a tool opens that tool and hides the hub. Avoid stacking overlapping large windows.
- While a child tool is open, **F1 returns to the hub**, closing that tool's UI first. (Existing background spawn batches must continue after closing a Spawner window.)
- **Escape** first dismisses a child tool; if only the hub is open, Escape closes the hub; only otherwise may the normal placement/paused-menu Escape handler run. Preserve the existing F2 Spawner's Escape-to-close behavior, but update its wording.
- A panel's Close/Back button should do the same as Escape, and should leave no modal focus trap.
- When no debug UI is open, **number keys and F2–F4 never open the spawner/route/building panels**.
- If the game transitions out of the battlefield / into Results or a different scene, dismiss open debug panels and cleanly detach focus/events. Opening during regular Pause should not resume or bypass it: if supported, keep the hub input-only and disable gameplay-mutating actions until unpaused.
- Respect `InputEventKey.Pressed && !Echo`, focused GUI first refusal where feasible, and consume handled input so Esc cannot simultaneously pause. Keep `F11` globally functional.

## 4. Reuse actual tool owners rather than reimplementing them

Current source to inspect at implementation time:

- `CONTROLS/GameInput.cs` and `CONTROLS/README.md`: globally named and priority-scoped controls.
- `DEBUG/LevelTools/LevelDebugController.cs`: current F2 `SpawnMenu` and F3 `RoutePreview` bindings. Both existing tool instances are created only when `SpawnManager` is live.
- `DEBUG/Spawning/EnemySpawnMenu.cs` + `EnemySpritePreview.cs`: existing batch jobs, preview and public `Toggle`, `Close`, `IsOpen`. Keep independent batch timers and cancellations unchanged.
- `DEBUG/Navigation/RoutePreviewOverlay.cs`: real path/portal preview with current `Toggle`/`IsOpen`; preserve simulation budgets and draw cleanup. Update old “F3” UI captions to “F1 → Route Preview” (or similar).
- `DEBUG/PerformanceOverlay.cs`: currently owns F4/Shift+F4/F5 in `_Input`; keep all counters, UI, profiler and report path. Expose narrow, safe methods for the new hub router to call instead of duplicating profiling logic. Update its own on-panel shortcut captions.
- `DEBUG/Alerts/AlertPreview.cs`: the old F7 `_UnhandledKeyInput` cycles six real presentation samples; expose a debug-only method and call it on F4, retaining the same sequence. Inactive release builds register no sample actions.
- `AUDIO/AudioManager.tscn` currently mounts `DEBUG/Audio/AudioTester.cs`. Remove the `AudioTester` scene child and its external script reference / F9 listener. Keep `AudioManager`, `VoiceAssistant`, audio buses, catalog, existing sounds and gameplay sound playback intact. The tester source can remain archived/unmounted unless clearly safe to delete; update its README to “retired”.
- `LEVELS/Runtime/Battlefield.tscn` and `DEBUG/LevelTools/LevelDebugTools.tscn`: mount the hub once per actual playable battlefield, including supported direct-scene launch paths. Existing LevelDebugController remains an owner/service gateway, not another competing keyboard router.
- `UI/GameplayHUD/GameplayHUD.tscn` mounts `AlertPreview`. Keep the notification manager; the preview’s keyboard input must be removed/migrated.
- `project.godot` / `GameInput`: preserve F11, Esc, Ctrl, and input-map authoring; avoid a second global input autoload.

**Preferred approach:** one DEBUG-only, world-scoped key coordinator that references cached actual instances of the existing tool owners. It owns the F1 hub commands and F2–F4 shortcuts; tool classes expose narrow public action APIs and do **not** retain their old input handlers. If this needs a small integration with `GameInput`, reuse it instead of inventing a separate global generic input framework.

No recurring node-tree search, signal/callback leakage or duplicate popup creation.

## 5. New Building Lookup screen (not the normal selected-tower Inspector)

Purpose: Browse how **all authored buildings/towers really look** from one debug screen **without building/spawning** anything or changing rank, health, power, inventory, economy, collision or world state.

### Catalogue and browsing

- Source: `TOWERS/DefaultTowerCatalog.tres` / `TowerCatalog.Definitions`, not a hand-maintained list of names, hardcoded artwork paths or only currently placed towers.
- Show all registered entries (including types not available for normal purchase) with category grouping: Offensive / Support / Economy / Utility; list **DisplayName**, stable `TowerId` and footprint.
- Search/filter by name/ID; show a small thumbnail where practical. Selecting an entry updates one cached preview.
- Display buttons/tabs **Level 1**, **Level 2**, **Level 3**, etc. according to that type's **actual authored purchasable level count**. Passive buildings with no `Levels` rows still have one **Base / Level 1** preview. Never fabricate a Level 2/3 model if the type is single-level.
- If a higher tier uses identical fallback artwork, still show the *resolved* image for that tier with a quiet label **“Uses prior level artwork”**. If required artwork is genuinely missing, show an explicit missing-art message instead of crashing.
- Include **Actual size (1:1)** and **Fit Preview** viewing modes; fit must be visual-only. Present footprint and native image/presentation bounds so a 2×2 or 3×2 building is not misrepresented as 1×1.

### Exact authored artwork and transforms

- Read `TowerDefinition.Scene`, `TowerDefinition.DefaultConfig`, `TowerConfig.ConstructionArt`, `TOWERS/Shared/Construction/TowerConstructionArt.cs`, `TowerLevelArtwork.cs` and `TowerConstructionVisuals.cs` for the current canonical presentation.
- A **split-art** tower such as **Battle Tower** uses `BaseSprite` plus independently positioned/rotating turret/gun sprite(s). Composite correctly in preview. Source scene `Sprite2D` nodes can be nested under `TurretPivot` / `RecoilRoot`; preserve authored transforms, texture, frame/region, offsets, scale, rotation and ordering. Do not show a hidden construction sprite on top of finished split art.
- For a tier, resolve `FinishedTexture(level, original)`, `FinishedBaseTexture(level, original)`, `FinishedGunTexture(level, original)` using existing fallback behaviour. **Do not assume every level has a single finished texture.**
- **Build stages / Upgrade stages** can appear in a small optional secondary selector when that tower has authored `BuildStages` or `UpgradeStages`; read only existing stages, never fabricate progress art. Finished Level 1/2/3 appearance is mandatory; phase animation is not.
- If a scene is inspected for presentation nodes, instantiate **off-tree**, copy only the relevant visual properties into a lightweight preview tree, then immediately free the source, as the existing `DEBUG/Spawning/EnemySpritePreview.cs` already does for enemies. Do **not** add a live `TowerBase` to the level/world/CanvasLayer for preview, call `_Ready`, register to combat/lighting, initialize turrets, tick recoil or give free towers.
- Allow a one-time refresh on selection/tier change; reuse/free the one preview and loaded resources. Do not load/instantiate every catalog scene on each frame or create one sprite tree per item. No per-frame code while closed.
- Make composite preview match the finished in-world appearance at default rotation as closely as possible, including newly added **Scrap Refinery** and **Resource Storage** (both 2×2 art shells), the Battle Tower split L1/L2/L3, and ordinary single-sprite buildings.
- Show concise metadata: name, category, level, 2×2 footprint etc. This is **art inspection** rather than an economy/combat stat editor; do not mutate shared `.tres` resources.

### Usability

- A compact two-column popup is sufficient: scrollable/searchable tower list on left; large artwork viewer, tier buttons and metadata on right.
- Keep art at the intended pixel resolution in 1:1 mode, permit scrolling for large bounds, and offer Fit mode for quick lookups.
- UI can use `UIButtonFactory`; stay mouse/keyboard navigable. Select, switch level, close and reopen without retained off-tree instances, stale focus or hot input loops.

## 6. Build safety and key conflicts

- **Debug build only**: do not expose F1 hub, spawning, route preview, notification sample triggers or Building Lookup in release builds. Follow existing `OS.IsDebugBuild()` / `#if DEBUG` patterns consistently; don't break release scene parsing.
- Make the direct performance shortcuts available in the same debug environments as the existing overlay; **F2** toggles readout, **Shift+F2** toggles profiler, **F3** exports the current retained report. If no sample is available, report a safe status; never invent timing data.
- **F4** samples the same six notifications as former F7. Rename or update old help labels and references; do not alter production notifications.
- F9 audio tester input is gone. Ensure no synthetic tones/loop emitters register during normal launch.
- Prevent the old F2/F3 handlers and re-bound F2 performance handler from **both** activating on one press. Remove old F5 report and old F7 notification paths as well.
- Existing `F6` handling is **out of scope**, except checking whether the game truly has a lighting comparison binding: if yes preserve it; if only stale docs exist, don't claim to have migrated it.
- Normal F11 fullscreen and pause/placement focus behaviour are unchanged. Avoid conflicts in `_Input` vs `_UnhandledKeyInput`, especially while paused and while a PopupMenu is focused.

## 7. Acceptance checks (owner can perform in Godot)

1. Launch a battlefield debug build. F1 opens the hub, F1 closes it, Escape closes it **without opening Pause**.
2. With hub open: click “Enemy Spawner” or press 1. Existing spawn configuration, 1:1 enemy artwork and multiple batch jobs still work. Closing the window does not cancel active batches.
3. Open F1 → “Building Lookup” or press 2. Browse Battle Tower Level 1/2/3 and verify base/gun visual composition; select other towers and the new Scrap Refinery/Resource Storage. View 1:1 and Fit. No buildings appear on the actual map and no credits/XP change.
4. F1 → “Route Preview” or press 3. Choose a portal and simulate existing routes. Closing it clears its route drawings exactly as before.
5. While any of the above is closed, F2 toggles the Performance Overlay **only**; Shift+F2 toggles profiling; F3 saves real current `user://Debug/pf2-*.txt` data. Old F4/F5 actions no longer run.
6. F4 cycles notifications, including item sample; F7 doesn't trigger them. F9 plays **no** Audio Tester signal; gameplay audio still works.
7. F1 within a child tool returns to hub; Escape dismisses the child first. Nothing simultaneously toggles gameplay Pause or opens two debug windows.
8. Confirm F11, Ctrl, Escape, building placement, camera movement, Save/Continue, Results and paused operation are unaffected.
9. Release/export build has no developer UI or active debug key hooks. Simple reopened/closed state has no idle per-frame preview work.
10. Verify Godot editor F5/F6 still launch game/current scene; if runtime lighting comparison F6 exists, it remains available.

## 8. Implementation / documentation rules

Implement as a **single focused pass DBG1** (not an entire global UI overhaul), only on branch `dev`. Before writing code read `CodePreferences.md`, `project.md`, this file and the named source files. Inspect current `dev` HEAD and any newly changed debug input before editing.

Use **lightweight source/diff review and at most one practical basic build** if available, per CodePreferences. No extended benchmark/test-suite expansion. Reuse existing widgets and feature owners, keep short `NOTES/WORK.md` update and correct relevant `DEBUG` / `CONTROLS` docs to distinguish new shortcuts from historical notes. No production systems or wave/economy changes.

When done commit and push to `dev`; report SHA, changed files, any unavailable/runtime-unverified checks, and owner test instructions. **This document alone does not implement the controls or debug hub.**
