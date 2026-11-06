# Mission Briefing — optional load-time Intel Report

**Source implemented 2026-11-06 / owner runtime acceptance pending.**

This is separate from per-wave `WavePresentationController` and `UI/Narration`. One shared CanvasLayer modal lives in `LEVELS/Runtime/Battlefield.tscn`; each map can opt in through its own `LevelConfig.MissionBriefing` without another scene or gameplay controller. `LEVELS/Briefing/MissionBriefingDefinition.cs` exposes Inspector-editable Enabled, Title, Speaker, multiline Message and Objectives. Empty/disabled briefing means **no overlay or extra gameplay work**.

**Lifecycle:** the battlefield mounts World/Level before its shared services; the briefing's `_Ready` reads the loaded map settings and schedules one deferred display after all child Ready callbacks. It does not use `WaveManager.WaveStarted`, does not create any waves, and appears at `WAVE 00` before manual `START WAVE`. UI uses the existing `MenuLayout` and `UIButtonFactory`, tinted translucent backdrop, a scrollable intel text area, and a keyboard-accessible CONTINUE button. The overlay blocks world mouse/Build shortcuts and pauses game logic; CONTINUE/Escape restores the previous pause state and returns control. No timer, auto-dismiss, ticking or extra autoload. An existing Pause menu can still be opened normally *after* dismissal.

**Restore / retry:** an active `BattlefieldLauncher.RestoringCampaign.Checkpoint` skips briefing creation altogether, protecting pause-state ownership during `CampaignSession.LoadCurrent`. Starting a **fresh** deployment (including retry) shows it once again. No new save schema, run stat, campaign reward, or content hash workaround.

**Initial authoring:** `LEVELS/Level001/Level001Briefing.tres` is wired to `Level001Config.tres`; all other maps have a null optional resource. Edit the Level001 resource to change the text. To enable future levels: create another `MissionBriefingDefinition` Resource and assign it to the level's `LevelConfig.MissionBriefing`. Leave the Message blank or uncheck Enabled to suppress it. Do not put briefing fields in wave recipes.

**Owner checks (Godot):** F5 Level001 shows report once at WAVE 00 and no enemies; long text scrolls and narrow windows fit; Enter/Click CONTINUE or Escape returns to normal PRE-WAVE without spawning; START WAVE still creates its normal banner; checkpoint Continue skips; no-briefing Level002 remains normal. Code/resource review only; no Godot runtime or screenshot testing claimed.
