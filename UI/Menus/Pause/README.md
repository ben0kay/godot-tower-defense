## Between-wave checkpoint integration — 2026-10-26

S2/S3 now provide source-owned capture/restore seams for this feature through
`SYSTEMS/Saving/CampaignSession.cs` and feature-local saving helpers. Esc Save/Load and Campaign
Continue use the selected profile's checkpoint; combat Save explains **Unavailable during combat**.
Historical saving-unimplemented statements below are superseded. See `SYSTEMS/Saving/README.md`
for field ownership, restored cargo/builders, original reward tickets and compatibility limits.
Source/basic diff review completed; .NET/Godot build/runtime acceptance remains pending locally.

---

# Pause Menu — Implemented

Each battlefield (`Main.tscn` / `MainLevel020.tscn`) instances `PauseMenu.tscn` once as a level-owned CanvasLayer. `PauseMenu.cs` builds a responsive centered panel using the existing MenuLayout/UIButtonFactory palette, with a translucent dark backdrop above HUD and F3 debug UI. The UI processes while the SceneTree is paused; gameplay, camera, waves and build input stop.

Buttons: **Resume**, **Save (not yet available)**, **Load (not yet available)**, **Options (not yet available)**, **Quit to Main Menu**, **Exit Game**. The three unavailable features are disabled and perform no writes. Campaign persistence remains future work under SYSTEMS/Saving.

Escape is routed by the CONTROLS/GameInput Autoload: cancel active placement first (tray stays open), otherwise open Pause; Escape while paused resumes. Ctrl is consumed while paused. F11 is global and still works. Resume preserves the HUD/tray state and restores previous focus when possible. The full-screen overlay intercepts pointer input.

Quit uses MenuNavigation.Open, which unpauses for a scene transition and restores pause on load failure. Errors are shown in a dialog. Exiting the overlay's scene unregisters its bindings and cannot leave the next screen paused. Direct F6 level play needs no profile: quitting goes to Main, whose existing profile guard redirects safely to Boot.

Verification: `TESTS/UI/PauseControlsChecks.tscn`, `TESTS/UI/MenuTowerFlowChecks.tscn`; results and manual desktop checks in `TESTS/UI/PauseControls.md`. No new save format, world serialization or options implementation is included.

Mission Debrief R1 takes final pause ownership via `LockForResults()`: hide Pause/error, prevent reopening/resuming, and let the session release pause on navigation/unload. Results Escape/Ctrl bindings take priority; F11 remains global.
