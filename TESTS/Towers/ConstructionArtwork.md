## Machine Gun C4 rollout — 2026-10-26

Machine Gun now uses the same `TowerConstructionVisuals` + `TowerConstructionArt` implementation as Battle. A dedicated stage profile references three authored, aligned 64×64 SVGs at 0/35/70% and switches back to the original finished split base/twin-barrel assembly at 100%. With the 4-second authored initial work, stage thresholds correspond to 0/1.4/2.8/4 seconds of **unheld** work. It does not change the Citadel drone or upgrade timings. Shared `ProjectileTurretBase.SetSelected` tints an optional construction overlay as well as the finished sprites, with no per-frame polling.

The focused `ConstructionArtChecks` includes a paid Machine Gun phase/selection/finished-split check and direct-instance fallback; this is an authored regression, not a claim of a completed Godot editor/desktop run. Inspect normal-zoom transitions and return to twin-barrel recoil in Godot.

---

# C4 Battle-only construction artwork pilot

**Implemented scope:** shared event-driven artwork presenter, Battle Tower stage SVGs, optional future per-level intermediate/finished images. **No sparks or construction VFX.** Real Factory/Depot/other tower stage assets are not enabled.

## Authoring and playback

Battle uses `Artwork/Construction/BattleConstructionArt.tres`: foundation at0%, frame at35%, assembly at70%, then its unchanged finished SVG. All use the original64×64 canvas/center/upward facing. Initial work still takes3 seconds; no costs or numeric rows changed. The ghost uses the original finished icon.

The optional scene presenter points to an existing Sprite2D; it changes only Texture when necessary. Loaded art profiles/textures remain shared; private combat rows/recipes and existing per-weapon mutable effect styles remain isolated. There is no process/physics loop or texture loading on progress callbacks. It reads work set before Ready, subscribes to progress only while busy, and cleans up on removal. Visual thresholds follow the existing approximately10Hz progress notices, so a transition may trail its threshold by less than a notification interval; damage/completion timing is unaffected.

LevelArtwork exposes Level, FinishedTexture, UpgradeTexture and UpgradeProgress. A future upgrade may show one intermediate image near completion; the old tier continues operating. Missing intermediate images keep completed art throughout. Missing final images retain the nearest earlier completed image, never the intermediate frame. Battle's Level2/3 Inspector slots are intentionally empty; actual upgrades currently look identical to Level1.

[Actual SVG contact sheet](../../TOWERS/Offensive/BattleTower/Artwork/Construction/construction_preview.svg) was rendered with Inkscape and visually inspected. It is an asset preview, **not an in-game screenshot**.

## Automated checks and actual execution — 2026-10-17 UTC

`Godot --headless --path . res://TESTS/Towers/ConstructionChecks.tscn` now includes ConstructionArtChecks:

- Real paid placement displays exact0/35/70/final texture references. Exactly4 texture writes across the initial build; no redundant writes between stages or afterward, no presenter processing.
- Pause, selection tint/transform, stats, XP, stock/payment and navigation remain unchanged. Raw/direct instances and the placement ghost retain finished art. Config instances share art references but not mutable numeric rows.
- Arbitrary/unsorted stage arrays, null/missing images/styles and invalid thresholds safely fall back. Optional intermediate/final upgrade images preserve firing/aiming, change at their configured phase, support zero-duration completion and retain earlier completed art when later images are absent.
- An isolated3×2 Factory fixture performs a real stage swap while retaining its full scale/transform and dormant production. Its production prefab remains unconfigured for art. Removing a presenter or freeing a building mid-progress leaves no callbacks/jobs behind.

All production/test C# compiled successfully with Roslyn4.11 and matching Godot source generation/reference assemblies. Editor asset import succeeded. ConstructionChecks (including C1–C3/paid upgrade regressions), GenericEnemyChecks, AmmunitionLoopChecks, FootprintChecks, HudCategoryChecks, MenuTowerFlowChecks, PauseControlsChecks and ResultsChecks exited0. Existing combat checks include beams, instantaneous splash, swept shockwaves, shields and500-enemy scenarios; style isolation still passes after keeping authored presentation resources shared.

Standard `dotnet build --no-restore` aborted before compilation in this host's SDK CLI (`Win32Exception`, `Process.GetStat`/`StartTime`). The full unmodified EnemyIntegrationChecks still fails the existing Level020 painted-terrain assertion, then exits139 during native shutdown. Neither is reported as a pass; no map assertions were weakened.

Desktop Godot visual QA was unavailable. The SVG asset review and headless texture/geometry checks do not establish actual viewport appearance.

## Manual Godot playtest checklist (pending)

- [ ] Pull/build, run Main.tscn or launch Level001 through Campaign, Ctrl→Battle and place a valid tower. Observe foundation→frame→assembly→original finished art in3 seconds, centered in its tile.
- [ ] Pause with Escape during construction; art and time freeze, then resume together. Check the existing Inspector progress and selection tint remain correct.
- [ ] Start a wave or spawn enemies near the tower: no fire before completion; finished turret aims/fires as before. No sparks/particles should appear from construction.
- [ ] Buy Level2/3: the existing turret keeps firing and keeps its current appearance during work; empty artwork slots cause no visual reset or partial construction image.
- [ ] In an optional copied art profile, assign one UpgradeTexture and FinishedTexture for Level2; verify the intermediate image near its threshold and final image at completion without changing turret alignment. Keep the same canvas/anchor/facing.
- [ ] Retry/exit the level mid-build and mid-upgrade; no stale visuals/callbacks carry into the new scene.

For slower visual inspection, temporarily increase the Level1 WorkSeconds in BattleTowerLevels.cs locally, then restore it; this pass does not change the authored3-second default.
