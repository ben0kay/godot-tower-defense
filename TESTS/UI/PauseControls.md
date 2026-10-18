# Controls and Pause verification

`PauseControlsChecks.tscn` uses actual Viewport.PushInput propagation and production menu/level scenes with a memory-only profile. It covers:

- Boot/Main/Campaign have no Ctrl/Escape gameplay bindings; F11 is handled globally.
- Both level entries register exactly one set of scoped callbacks and show no pause screen initially.
- Ctrl toggles once; release and OS repeat do nothing. The existing menu flow also checks RMB/CTRL placement cancellation, tray reopening, construction and paid upgrades.
- Escape cancels active placement without opening Pause; the next press pauses. Higher-priority modal callbacks consume Back without also pausing.
- Escape and the actual Resume button unpause; Ctrl is blocked during Pause. Save/Load/Options are disabled and Exit Game is present.
- Paused world/HUD/placement nodes cannot process, and a battlefield Timer's remaining time stays unchanged across frames. Pause clears the camera's middle-drag latch.
- F11 is routed while paused. The fullscreen transition rule covers startup fullscreen, exclusive fullscreen and restoration of windowed/maximized modes.
- Quit to Main Menu actually transitions/unpauses and unregisters all old level callbacks. Repeating this on Level020 finds no stale/duplicate bindings. Direct level play without a profile safely returns through Main's existing guard to Boot.

## Actual results (2026-10-16)

- Direct Roslyn C# compilation with installed Godot assemblies/source generators: exit0, no diagnostics.
- PauseControlsChecks: exit0.
- MenuTowerFlowChecks: exit0.
- RoutePreviewChecks: exit0, both levels and live-enemy preview isolation.
- CheckpointChecks: exit0, existing required regions/chains/branching/500-enemy fixture.
- Unmodified EnemyIntegrationChecks: exit1 at the pre-existing Level020 painted-tile-count assertion (`The western open ground and eastern maze are fully painted`). This pass does not change map tiles or that assertion; no full-suite pass is claimed.
- Standard `dotnet build --no-restore`: environment-blocked before compilation by a CLI process/thread information exception. Runtime scenes were tested with the directly compiled assembly instead; no standard SDK build success is claimed.

Godot 4.7.2 .NET ran headless. Existing DefaultTowerCatalog fallback warnings remain. No campaign persistence is touched. The window-mode rule and F11 delivery are tested, but a headless display cannot verify actual desktop fullscreen/maximized transitions or visual appearance. Manually check F11/window restoration and the pause panel at the desired desktop sizes after pulling. Exit Game uses the existing SceneTree.Quit behavior; the runner checks its button exists without terminating itself through that action.
