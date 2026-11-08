## Bulwark Drone visual shrink — 2026-11-08

Changed only `ENEMIES/Types/Enemy004/Enemy004.tscn` Sprite2D scale to **0.8×** (effective display 51.2×38.4px from original SVG 64×48). Artwork itself, navigation clearance radius 22px, Standard class, hull HP and other gameplay remain unchanged; F2 art preview copies the authored Sprite scale. Per-enemy `.tres` currently has no visual scale setting; tweak `Sprite → Transform → Scale` in each enemy scene instead. Source-verified; Godot visual validation pending.

---

## Wave Editor readability polish — 2026-11-08

Separated the editor sections into tinted panels. Wave Analysis uses labeled metric cards, the toolbar keeps the Fleet Threat value prominent, and timeline groups have aligned labels and tracks. Source review only. Check compact editor widths in Godot.

---

## Lava glow continuity and bubble pops — 2026-11-08

Replaced first-admitted lava point lights and stacking tile glows with a single chunked coverage field around the lava silhouette. The surface uses continuous self-lit color and the bubbles now swell, flash and throw warm flecks at varied intervals. Source review only. Inspect a long lava strip, chunk boundaries, F6 and bubble timing in Godot.

---

## Wave Editor catalog loading and Fleet Threat visibility — 2026-11-08

Enabled editor instantiation of the registered catalog, enemy scene metadata and authored capabilities. Gameplay mount and ability setup stays out of the editor. Fleet Threat is pinned in the toolbar and failed catalog loads preserve IDs without calling them unregistered. Godot .NET rebuild and editor acceptance pending.

---

## LAVA1 — Molten surface, light spill and steam — 2026-11-08

**Source Implemented / Awaiting Owner Validation.** Lava-only tile material adds world-space flow and sparse bubbles over the original SVG. The world lighting pool admits capped, shadowless lava spill at 0.63 energy / 160px, strengthens the cell glow and reuses regional anchors for a small GPU steam pool. Existing lava reveal geometry is preserved. Source and resource review only. Check isolated and connected lava, F6, camera movement and dense combat in Godot.

---

## NOTES/Ideas.md active-idea audit — 2026-11-08

Removed three fully source-implemented ideas from the active brainstorm: F13 portal activation/materialization, M08 six basic tower targeting priorities, and M09 ammunition factory/depot/courier logistics. Clarified partially delivered F01/F04/F05/F06/F08/F12/F14/F15 and M02/M05/M07 so only incremental work remains; explicitly marked M12 salvage drones as **planned**, already cross-mentioned in DRONES/ideas.md and the planned Scrap Refinery. No gameplay code, XP, UI, saves or balance changed. Source/document review only.

---

## Wave Editor WP2 / WP4-A / WP4-B — 2026-11-08

**Source Implemented / Awaiting Owner Validation.** Added catalog sprite/stat previews, exact selected-wave totals and an uncapped Fleet Threat Index with capability omissions identified. See the [Wave Editor notes](../addons/wave_editor/README.md) for the v1 formula. Source review only. Check Level003 Wave 4, then compare armed, support, carrier and parallel batches in Godot.

---

## Wave Editor WP4 planning revision — 2026-11-08

Updated `addons/wave_editor/PLAN.md` to split pending WP4 into WP4-A exact wave stats/schedule and WP4-B an **uncapped, one-decimal Fleet Threat Index** (e.g. 50.0 vs 78.0). The proposed calibrated, explainable model incorporates durability, shields, Citadel damage, speed, real hostile guns and supported abilities plus spawn cadence/parallel pressure, using authoritative catalog data and exact quotas. Explicit partial/unavailable handling prevents invented confidence. This is documentation only; nothing implemented in gameplay/editor and no build needed.

---

## Tower rank emblem contrast pass — 2026-11-08

Recoloured the five tiny shared `TOWERS/Shared/Ranks/Artwork/*.svg` emblems for better contrast against cyan/navy tower art. All now use a darker plum-toned face: Standard is warm copper/amber, Veteran pearl silver, Enhanced violet, Superior gold, and Legendary rose-gold with brighter crown details. No changes to geometry, 24px SVG size, sprite placement, rank thresholds/bonuses, scene wiring, saves or processing. Source review only; check visibility at regular battlefield zoom in Godot.

---

## UI audio catalog startup fix — 2026-11-08

Fixed broken `DefaultAudioCatalog.tres` loading caused by hard `AudioStream` external references to newly renamed/copied button WAVs before Godot had imported them. ButtonHover/Click/Back definitions now keep their Inspector-editable mix values but leave `Stream` unbound in the file. `AudioManager` binds these three optional WAVs once at startup (direct source loading in editor, imported resource fallback in export), so missing/unimported button clips cannot invalidate tower/ambience/voice audio. Original recorded sound bytes and audio IDs unchanged. Source-only review; owner should pull dev, restart Godot, verify all three buttons plus existing sound families.

---

## F08-A — Shared camera shake and Citadel impacts — 2026-11-08

**Source Implemented / Awaiting Owner Validation.** Battlefield camera shake uses visible-rectangle falloff, strongest-hit arbitration and a capped visual offset. Only HP lost to arrivals at the assigned Citadel requests the effect. Tune the [camera settings and Citadel impact profile](../FEEDBACK/CameraEffects/README.md). Source review only. In Godot, check hit strength, offscreen fade, navigation during a shake and cleanup on pause, outcome and Continue.

---

## World-space tower rank emblems — 2026-11-08

Added five tiny military/sci-fi vector insignia (Standard → Legendary) and one shared non-processing `TowerRankEmblem` sprite for placed combat-ranked towers. Badge is anchored top-right **inside the full tower footprint**, not on the rotating gun, and uses cached shared SVG textures; the existing placement/restore path initializes it and only promotions swap its texture. Passive Standard-only structures, ghosts, XP economy, rank thresholds and save data are untouched. Review source paths/geometry only; owner should inspect legibility, overlaps and visual scale at gameplay zoom in Godot.

---

## Shared UI button sounds — 2026-11-08

Registered original GameMaker hover, click and back WAVs on the existing UI audio bus as `ui_button_hover`, `ui_button_click`, and `ui_button_back`; sound definitions and Inspector mix are `AUDIO/Button{Hover,Click,Back}.tres`. Renamed the imported `snd_btn_main_menu.wav` to `AUDIO/snd_click_success.wav` without changing the audio bytes. Shared `UIButtonFactory` plays a hover once on enabled mouse entry and a normal click on enabled press for menu screens and in-game tower build cards only; explicit Back buttons request back instead of click. `MenuNavigation` binds the existing `GameInput.BackAction` Escape for Main→Boot, Campaign→Main, Research/legacy Levels→Campaign, ignoring active modal dialogs; it unbinds on scene exit. No new audio manager or idle loop. Source review only; Godot WAV import and actual mix/keyboard behaviour await owner playtest.

---

## Ammunition Factory and Depot SVG art overhaul — 2026-11-07

Replaced the existing 64×64 top-down `factory.svg` and `depot.svg` with layered sci-fi industrial art matching the newer power generator metalwork. Factory now has twin forge chambers, a cartridge stamping press, vents, copper ammo and cyan dispatch lighting; Depot has separate storage cassettes, an armoured automated loading lift, sealed cargo bays and teal status lamps. Updated Factory's three 0/35/70% construction SVG stages to evolve toward its new finished silhouette; kept existing `.svg` paths, 64×64 centered viewBoxes, importer/scene references, 3×2/2×2 footprints, sprite scales, construction timing, production, storage and logistics untouched. Dormant future Factory Level 2/3 upgrades remain as previously authored, not enabled. Source SVG and path review only; owner to inspect Godot rendering/reimport and 100%-completion transition.

---

## Original GameMaker audio archive — 2026-11-07

Copied 12 unchanged sound assets (`snd_voice_victory`, `snd_voice_defeat`, wave cues, notifications, Credits/build placement feedback, and menu buttons) from `ben0kay/SPACE-SHOOTER-TD-IDLE-CLICKER-HYBRID/sounds/` into the root `AUDIO/` folder. Original WAV/MP3 names and bytes preserved. These are *loose assets only*: no catalog registration, event wiring, mixer changes or runtime playback. Godot import/listening remains owner-controlled.

---

## Weapon audio overlap and camera reach

Opt-in firing clips suppress nearby duplicate starts, and viewport-scaled hearing reach preserves edge/zoom-out playback with a separate zoom gain reduction. Ambient tint increased to `#3b444e`. Source/config checks only, live mix pending.

---

## Tower build tray: level-one thumbnails and vertical scrolling — 2026-11-07

The catalog already provided 22 authored level-one SVG icons; only some were visible in the old intrinsic-width Button layout. The build tray now uses a reusable `UI/GameplayHUD/TowerBar/TowerBuildCard.cs` to reserve a dedicated 40px image slot for **every** tower alongside consistently left-aligned name and category/price text. All cards are 200px wide by 62px high by default and remain keyboard/click/hover-accessible. The card grid chooses a column count from the available tray width, reserves room for the vertical scrollbar, never uses tower count to stretch cards, and now scrolls **vertically**, not horizontally. Category changes reset vertical scrolling; 22-tower and 40-variant cases fit the same clipped tray without covering the Inspector. Original tower SVGs, catalog metadata, placement actions and inspector remain unchanged. Updated existing targeted tray assertions; source-only review, owner in-game layout/mouse-wheel testing pending.

---

## FD3 — Fighter Auto-patrol

Individual roster controls now switch between Auto-pilot and A/B figure-eight patrol, with world preview and cancellable edits. Patrol uses distance-based steering and the existing opportunistic guns, recall and auto-service. Checkpoint persistence is deferred to FD6. Source and diff checks only. Godot .NET and live flight/UI validation remain pending.

---

## Shared tower glows and a larger light pool

Nearby towers of similar light size share one cosmetic lamp, and neighbouring groups dim their overlapping glow. The default pool increased to 24 lights and prioritizes tower groups near the camera when full. Individual tower reveal remains independent of grouping.

---

## Root documentation reorganization — 2026-11-07

Moved the former root README into `devlog.md` (historical contents preserved), added `project.md` as the concise game/vision overview, and relocated `NOTES/CodePreferences.md` to root `CodePreferences.md`. Updated cross-document references and the backlog handoff. Documentation-only; no C# or gameplay checks required.

---

## Original GameMaker narration typewriter audio — 2026-11-07

Imported the original 5-second `snd_msg_tut.wav` from the old GameMaker hybrid repo into `UI/Narration/Sounds/`, with forward looping and a feature-local `ui_narration_typewriter` audio definition registered in the shared catalog. The shared `NarrationDisplay` starts its UI loop only when characters reveal and stops on completion, skip, replacement, pause, clear/results and scene unload; unpause restarts only while typing. Tuning: `UI/Narration/DefaultNarrationConfig.tres` enables/chooses the cue, `Sounds/TypewriterSound.tres` controls mix (~30% / -10.5 dB). Existing narration timing, voice hold and gameplay unchanged. Source review only; Godot import/playback/mixing owner validation pending.

---

## Fighter lamps, disabled towers and terrain glow

Wired the light attachment into the authored fighter, builder and cargo scenes. Disabled towers go dark until recovery. Buildable terrain and playable lava use chunked emissive accents, independent of the native light pool.

---

## Drone lights and brighter ambient

Active builders retain their glow during construction. Drone light and reveal radius increased from 62px to 186px. Fighters can target dark radar contacts. Ambient starts at `#2f363e` and the default world pool admits up to eight drone lights.

---

## Wider tower glow and more projectile lights

The shared tower light radius is 384px. Machine Gun, fighter and the optional traveling Sniper tracer now opt into the existing projectile light pool.

---

## LGT0-A.3 — dark radar contacts

Removed the pale attack-range overlay and its reveal footprint. Enemy artwork and health bars crossfade into moving red dots outside local lights. Ground towers can target and guide rounds in darkness at normal accuracy. Fighter and hostile direct-weapon reveal rules remain in place.

---

## Tower rank XP thresholds rebalanced — 2026-11-07

Set offensive tower Veteran/Enhanced/Superior/Legendary **cumulative** XP requirements to **3,000 / 8,000 / 17,500 / 30,000** in all ten existing `*Ranks.cs` tables. Kept damage/range/HP/spread rank bonuses, shared damage/kill XP multipliers, paid tower Levels and per-tower XP untouched. Updated the shared Ranks README and focused threshold/Inspector test expectations for the new non-doubling increments (3,000/5,000/9,500/12,500). Source-only sanity check; gameplay verification remains with owner.

---

## LGT0-A.3 — combat reveal before shadows

**Source Implemented / Awaiting Owner Validation.** Darker terrain, neutral tower-range lift and purple portal pools now share reveal with entity rendering and direct combat locks. Blackout suppression, existing projectile hits in darkness, and camera-independent targeting are documented in [Lighting](../SYSTEMS/Lighting/README.md). Native shader/mask loading and source checks passed. Godot .NET, rendered appearance and performance await owner validation.

---

## Narration card moved below START WAVE — 2026-11-07

The top-right typed tactical narration card now starts at **116px from the top**, about 32px lower than the effective previous placement at the default 46px status header. `DefaultNarrationConfig.tres` and `NarrationConfig.cs` share the new default; `NarrationDisplay.LayoutCard` enforces at least `TopBarHeight + 70px` so START WAVE and its lower edge remain clear. The existing right alignment, adaptive content height, alert-banner clearance, mouse transparency, typewriter timing and audio/voice hooks stay unchanged. Located (but did not import) the original GameMaker typewriter loop at `ben0kay/SPACE-SHOOTER-TD-IDLE-CLICKER-HYBRID/sounds/snd_msg_tut/snd_msg_tut.wav`, used by `scripts/feedback_processes/feedback_processes.gml` and stopped by `scripts/hud_processes_new/hud_processes_new.gml`. Source-only review; owner desktop visual test pending.

---

## Friendly tracer buffer

**Source Implemented / Awaiting Owner Validation.** Battle, shared projectile turrets and fighters now use the world-owned friendly tracer buffer with shared hit and drawing behavior. Overflow keeps a standalone shot, splash and wave delivery stay on their existing paths, and save/profiler checks include buffered rounds. Source and scene-reference checks only. Godot .NET regression execution and FPS comparison await owner validation.

---

## LGT0-A.2 — projectile light spill

**Source Implemented / Awaiting Owner Validation.** Selected friendly and hostile rounds can borrow up to four tiny shadowless lights from the existing world budget. Tracer/body color drives tint, and existing travel and retirement update and release leases. See [Lighting](../SYSTEMS/Lighting/README.md). Source and resource-reference review only. Godot .NET build, visual and GPU checks await owner validation.

---

## LGT0-A.1 — shared lighting expansion

**Source Implemented / Awaiting Owner Validation.** Organized lighting by runtime, config, presets and textures. Shared tower defaults and radius overrides, dimmer construction pools, darker cyan dusk, flying-drone lights and grouped lava spill use a bounded native light pool. See [Lighting](../SYSTEMS/Lighting/README.md) for settings and rollback. LGT0-B shadows remain planned. Full Godot .NET build, visual and GPU checks await owner validation.

---

## LGT0-A.1 — shared lighting expansion planned

The [lighting plan](../SYSTEMS/Lighting/IMPLEMENTATION_PLAN.md) now puts folder organization, shared tower defaults and overrides, smaller construction lights, darker ambient, stronger cyan, flying-drone lights and warm lava spill ahead of LGT0-B shadows. Documentation only.

---

## Tower Inspector compact upgrade cost button — 2026-11-07

The selected Inspector no longer displays a separate `LEVEL N · Cost · WorkSeconds` upgrade row. The Upgrade button now shows `UPGRADE LEVEL N` on the left and a right-aligned `X CREDITS` price. Its tooltip retains the full resource recipe and time, including any non-Credit resources. Busy jobs show `UPGRADING…` without a second price; completed maximum-tier towers show `MAX LEVEL`. The former UpgradeInfo label is only shown for actual upgrade errors. Upgrade selection/payment, wallet eligibility, timers, Sell behaviour and stats are unchanged; focused existing test assertions updated. Source review only; owner Godot visual and gameplay acceptance pending.

---

## LGT0-A — native battlefield lighting

**Source Implemented / Awaiting Owner Validation.** Shared world dusk tint, capped and optionally culled Citadel/Battle Tower lights, and debug F6 comparison. Scene attachments and rollback steps are documented in [Lighting](../SYSTEMS/Lighting/README.md). Source review only. Godot .NET build, appearance and GPU timing await owner validation. LGT0-B shadows remain planned.

---

## Campaign level deployment loading UI — 2026-11-06

Campaign/Levels PLAY buttons now open `UI/Loading/LevelLoadingScreen.tscn` via a dedicated `MenuNavigation.OpenLevel` route. One full-screen drawn HUD has a genuine Godot threaded mission/battlefield resource-progress bar across the bottom, counter-rotating percentage rings in the lower-right, and a clear central image area. Optional `UI/Loading/Artwork/LoadingBackdrop.png` works without scene edits, with a dark fallback before artwork exists. Brief 0.6s minimum presentation, errors return to level selection, and async resource completion transitions into the unchanged synchronous battlefield initialization. Direct F6, Continue, results navigation, mission/battlefield logic and saves are unchanged. Source-level sanity only; owner Godot menu/fullscreen/loading acceptance pending.

---

## LGT0-A — change planned lighting debug hotkey to F6 — 2026-11-06

Updated the lighting plan to use **F6** (without Shift) for the debug ON/OFF comparison, replacing Shift+F3 throughout. Existing F3 route preview, F4 diagnostics, F5 profiling and other level shortcuts remain unchanged. Godot Editor's F6 Run Current Scene is separate from in-game input. **Documentation only**: LGT0-A/B still await implementation.

---

## LGT0-B — tower and mountain shadows added to lighting plan — 2026-11-06

Expanded `SYSTEMS/Lighting/IMPLEMENTATION_PLAN.md` with native basic `LightOccluder2D` shadows from **all completed placed towers** and `mountain_visual` procedural mountain clusters. LGT0-A first delivers Citadel/Battle Tower lighting; LGT0-B adds shared tower footprints and grouped/cached mountain outlines, separate `ShadowsEnabled` Inspector toggle, hard shadows initially, cap 6 shadow-enabled lights out of 16 sources, self-shadow prevention, and owner ON/OFF GPU testing. Normal walls, enemies, decals, power/LOS changes and reverted bloom excluded. **Plan only, not implemented.**

---

## LGT0 — native Godot 2D light plan — 2026-11-06

Created `SYSTEMS/Lighting/IMPLEMENTATION_PLAN.md` for a small shadowless native `CanvasModulate` + `PointLight2D` pilot on Citadel and Battle Tower with Inspector configs, visible light cap (provisional 16), source lifecycle and owner GPU profiling. Shift+F3 toggles entire lighting vs original undimmed scene; F3 route preview and F4 performance overlay preserved. Owner prioritizes this VE5-derived slice before VE4; reverted bloom is excluded. **Documentation only; no system implemented.**

---

## Tower idle draw and level ambience — 2026-11-06

Battle idle draw is 0.10 energy/s. The other ten powered tower profiles doubled their idle rates. The allocation fixture explicitly disables idle drain to isolate refill accounting. The shared battlefield starts and releases a non-positional Ambience emitter using a crossfaded 5.6-second Ogg loop. The uploaded MP3 is preserved byte-for-byte alongside it. Godot 4.6.3 imported the loop with native looping enabled. C# gameplay and audible mix checks are pending.

---

## Six starter Research branches — first implementation milestone clarified — 2026-11-06

**Plan/documentation only; no new research nodes or gameplay source changed.** Clarified EX1-A as a complete, standalone first ring: preserve Core/Citadel Master I/Battle Tower Weapons I; add only four genuine 1-RP upgrades for Drone hull HP, Stealth Beacon detection range, initial tower construction duration and tower capacitor capacity. Each of six first-tier nodes depends only on the free Core and can be purchased in any order. Specified stable IDs/hexes, the real effect owners, next-deployment/saving compatibility, no dummy unlocks and an explicit Codex stop point. Optional EX1-B/C and EX2 remain future fan-out, not part of the first phase. Linked milestone from Research README and outstanding backlog.

---

## VE3 — paintable industrial accents — 2026-11-06

Added 32 transparent cyan/amber shapes in static source 1 and animated source 2 of the existing Decorations TileSet. The animated TileData uses shared MetalPulse materials with a static switch. The legacy source 0 and all painted maps retain their existing data. Checked Godot 4.6.3 renderer source for per-tile material isolation and placed all 65 tile choices in a headless scratch project. Desktop appearance, seams and FPS await owner review. See [atlas coordinates](../WORLD/TERRAIN/Tiles/Decorative/README.md).

---

## VE3 decorative industrial atlas — clarified Codex handoff (2026-11-06)

Updated `WORLD/BACKGROUNDS/README.md` VE3 plan: new transparent paintable 64×64 cyan/amber conduit, corner, junction, edge and indicator tiles inside the **existing** Decorations TileSet; preserve old atlas source 0/coordinate 5:0/0 and all maps. Animation is opt-in and local to new accent atlas cells via `MetalPulse.gdshader`, never an all-Decorations shader. VE3 is purely cosmetic, no power coupling, new runtime entity per tile, auto-repaint, VE4 or gameplay changes. Codex must verify actual Godot atlas/material isolation before committing an animated approach. **Planning only; not implemented.**

---

## Level003 lava briefing voice — 2026-11-06

Replaced Level003 briefing text with the owner's exact revised lava/Citadel wording. Moved the user-uploaded 265,054-byte Fish Audio MP3 from the repository root into `LEVELS/Level003/Level003Briefing.mp3` using the unchanged Git blob (no re-encoding); deleted its long original export name. Added `Level003BriefingVoice.tres`, registered it in `AUDIO/DefaultAudioCatalog.tres`, and set `VoiceCueId = voice_mission_level003_intro`. This uses the existing Level001/002 voice-held top-right typewriter on fresh deployment while checkpoint Continue skips the briefing. No gameplay, waves, lava damage, terrain or save changes. Resource/source check only; owner to verify audible playback and subtitles in Godot after import.

---

## Research six-branch expansion specification — 2026-11-06

**Planning only; no research nodes/effects authored.** Added `SYSTEMS/Progression/Research/RESEARCH_EXPANSION_IMPLEMENTATION_PLAN.md` with six radial divisions and 36 proposed paid nodes (34 new), unique connected hex coordinates, costs/prerequisites/effect contracts, staged EX1-A/B/C (~22 new nodes) and later EX2 capstones/optional DME EX3. Existing Core/Citadel Master I/Battle Weapons I remain unchanged; new effect kinds require real, snapshot-safe handlers before publication. Linked the plan from the feature README and outstanding backlog. Current `dev` gameplay research catalog still contains only three nodes.

---

## Level003 lava tutorial briefing — 2026-11-06

Created `LEVELS/Level003/Level003Briefing.tres` with a short Lava Fields introduction explaining that crossing lava damages enemies and Foundation Platforms can funnel routes over it. Assigned the resource in `Level003Config.tres.MissionBriefing` using the already shared top-right typewriter, with no custom HUD, gameplay changes or voice/audio dependency; `VoiceCueId` stays blank until a recording is supplied. Existing waves, lava damage and map layout are unchanged. Focused resource/config review only; verify the briefing appears on fresh Level003 deployment and is skipped on checkpoint Continue.

---

## Profile Welcome Commander voice replacement — 2026-11-06

Replaced `AUDIO/VoiceAssistant/Welcome_Commander.mp3` with the owner's latest uploaded Fish Audio MP3 from the repository root, reusing its original Git blob without transcoding. Removed the long export filename from the root. The existing `voice_profile_welcome` cue, path, playback settings, audio catalog and Godot import-sidecar remain in place; profile greetings will use the replacement after Godot reimports. Source and asset references reviewed; audio playback remains for owner acceptance.

---

## Portal spin surge, bubbling spawn reaction and wider violet clouds — 2026-11-06

In `WORLD/PORTALS/EnemyPortal/`, idle portal art spin rises from 120°/s to **180°/s** (Inspector `RotationDegreesPerSecond`); an accepted, cooldown-budgeted spawn burst integrates an additional ~210°/s decaying surge over ~0.7s, rebasing GPU rotation without phase jump or CPU idle loop. Spawn feedback now runs more frequently (~230–630ms irregular cooldown, down from ~390–910ms), with variable-density/scatter spark bursts and a short, randomly single- or double-peaked "volcanic bubble" expansion. Smoke continues in the two **existing** GPU emitters: normal puffs ~125px, ~5.5s, 26px/s outward; large faint puffs ~2.7×, ~9s, ~72% base drift speed (rather than 55%) with stronger swirl and capped 22/10 clouds. All visuals stay optional/local and do not affect portal readiness, wave count, routes, SpawnExit or saves; offscreen/paused cleanup remains. One shader/resource/source check only; Godot visual/FPS inspection remains with owner.

---

## Citadel visual polish and world labels — 2026-11-06

Removed the hardcoded CITADEL and PORTAL world labels from their shared scenes (and retired the portal's unused label animation hookup). Updated the existing 192×192 Citadel SVG to a more intricate dark-metal MK I fortress with layered plating, vents, cyan reactor housing and four **empty** future weapon hardpoints; retained the art resource path and rasterized-sprite architecture. No changes to footprint, placements, approach paths, health, credit UI, reactor shader, combat, portal spawning or saves. Source/scene sanity checked; owner to judge appearance and scene import in Godot.

---

## Level002 briefing audio — 2026-11-06

Renamed the owner-uploaded 289,295-byte Fish Audio MP3 to `LEVELS/Level002/Level002Briefing.mp3` by retaining the existing Git blob (no audio conversion). Added `Level002BriefingVoice.tres`, registered the voice in `AUDIO/DefaultAudioCatalog.tres`, and set `Level002Briefing.tres.VoiceCueId` to `voice_mission_level002_intro`. Reuses Level001's voice-held top-right typewriter; preserves briefing text, waves, gameplay and checkpoints. Source/resource review only; owner to confirm playback on fresh Level002 load after import.

---

## Campaign mission card glow and optional artwork — 2026-11-06

Removed the generic `SECTOR ### // OPERATIONS` strip from all campaign cards. Added opt-in, static cyan hover/focus/pressed glow through `UIButtonFactory.ApplySelectionGlow` (2–3px borders, bounded StyleBoxFlat shadow; no CPU animation), with grid insets to prevent halo clipping. `MissionDefinition.CardPreview` is a **nullable Inspector-editable Texture2D**: later assign a per-level screenshot/artwork on each `LEVELS/Level###/Level###Mission.tres` in Godot and the campaign card automatically shows it cropped and subtly faded behind its title. No images were fabricated or imported; empty previews remain text-only. Existing mission launch, progress and Continue/Research routes unchanged. Source/resource inspection only, Godot visual testing remains with owner.

---

## Campaign full-window mission dashboard — 2026-11-06

Replaced Campaign's compact centered MenuLayout with a full-window 2:1 split: left scrollable 3-column catalog-backed mission cards (Level 1/2/3 first row), right fixed Continue/Research, real commander + progression, Back at bottom. Cards directly launch existing MissionDefinition.LaunchPath via MenuNavigation. Continue uses the same selected-profile checkpoint/disabled reason; Research and Main navigation are unchanged. Existing Levels scene retained as legacy compatibility, no fake mission thumbnail/completion/unlock state. Files: `UI/Menus/Campaign/CampaignMenu.cs`, `CampaignMenu.Layout.cs`, `Levels/CampaignMissionCard.cs`, existing UI flow check. Source review only; Godot visual/layout/keyboard/gameplay checks at 1920×1080, 1366×768 and 960×540 remain for owner.

---

## Fighter drone targeting optimization — 2026-11-06

Four fighter scripts now avoid per-frame weapon LOS (scheduled validation or real firing only, fresh shot geometry), skip acquisition/spatial queries for empty/ineligible caches, and request the shared ranker only from deployed combat-ready fighters after transit. Existing bounded cache also tracks detectable weapon candidates independently of valid Citadel journeys; its cadence detects later eligibility/stealth changes. Debug/unowned combat between waves preserved; movement, service, ammo/cooldown/save contracts unchanged. Short Combat README note added. Source/diff review only; .NET unavailable. Owner to check mountain LOS/lead, target death/stealth/reacquisition, empty and debug-only worlds, launch/recall and checkpoint continuation; no measured FPS claim.

---

## Backlog cleanup — outstanding-only live index — 2026-11-06

Rebuilt `NOTES/IMPLEMENTATION_BACKLOG.md` against `dev` HEAD `e1a68dd77f`: removed duplicate completed-pass tables, dated implementation receipts and superseded “planned” claims. Left a current queue of genuinely unimplemented phases, optional feature-design decisions, a separately marked owner QA/performance list, unapproved idea IDs and Markdown-only accuracy debt. Reconciled recent FD4.1/FD5 and SPAWN3/SPAWN4 source delivery, complete Level011 Waves 1–10 and next-wave auto-start; older histories remain in this file and feature docs. Documentation only, no game build or Godot playtest.

---

## SPAWN4 — parallel attack groups — 2026-11-06

Source implemented / awaiting owner validation. Wave Schedule defaults to Sequential; Parallel uses per-batch StartOffsetSeconds from accepted wave start (After ignored). WaveManager.Parallel shares the existing attempt cap, RNG, exact compositions and ownership, with fair independent cooldowns/retries/readiness waits and no catch-up backlog. Wave Editor adds mode/start controls, timing bars and exact source-selector quotas/side weights; resource copy/build/validation retain settings. Existing mission recipes unchanged; SPAWN5 deferred. Brief authoring guide and plan status updated. Focused source/diff review only; .NET unavailable. Owner to check overlapping groups, blocked/forming sources, pause, zero intervals, wave completion/offspring, parallel→sequential transitions and editor save/duplicate. Existing content-hash rules still apply to older checkpoints after source updates.

---

## Level002 foundation-platform briefing — 2026-11-06

Added `LEVELS/Level002/Level002Briefing.tres` and assigned it through `Level002Config.tres.MissionBriefing`, following Level001's existing load-time top-right typewriter route. Short text explains foundations can carry towers and reroute enemies. Optional objective metadata is supplied; voice cue intentionally blank pending a future Fish Audio MP3. No wave, foundation tile, audio catalog, runtime code or level balance changes. Resource/config source checked; owner to verify a **fresh** Level002 load shows the message and that Continue skips it.

---

## FD4.1 + FD5 — 2026-11-06
Independent spatial nearest-shootable weapon selection keeps movement threat leases untouched; shared muzzle eligibility gates acquisition and real shots. Added real station inventory (empty/2000), narrow IAmmoDeliveryConsumer courier integration including demand/status/loss/save paths, fighter-owned AmmoInventory and persisted shot parity. Low-resource returns distinguish auto/manual/station-disable; one FIFO bay transfers real ammo over 6s/full reserve, independently repairs at 20HP/s with real 1C/10HP pricing and retained prepaid fraction. Waiting service sleeps; stock/wallet/enable events wake it. Tiny right-side real-transfer meter and selected roster status; additive per-slot progress/queue/order/fraction checkpoint fields, old missing fields remain empty/inactive. Restore order audited: inventories before courier jobs, wallet before unpause; no purchase/transfer/relaunch replay. No FD3/AA/flight-balance changes. Source/resource review only, .NET unavailable; owner to validate multi-station conservation, partial service, disable/sale, pause and checkpoint stages.

## Transporter death-release rate doubled — 2026-11-06

The regular Drone Transporter now uses its own `ENEMIES/Types/Transporter/TransporterDeathSpawn.tres` (six Standard Drones, **0.06s** between releases instead of the former 0.12s). The Heavy Transporter's independently authored six-Drone and four-Interceptor configs each now use **0.08s** intervals (formerly 0.16s). No changes to the shared DeathSpawnConfig/default, DeathReleaseQueue safety budgets, child counts, scatter, inherited routes, kill-only trigger, ownership, weapons or gameplay balance. Source/preset wiring checks only; owner in-game release pacing pending.

---

## SPAWN3 — 2026-11-06
Source implemented: append-only optional wave/batch/entry source overrides, unchanged legacy sampler, genuine mixed/portal/edge/required-portal policies, separate weighted-side and safe-position draws, cached named edge sectors/ranges with optional portal exclusion/spacing, editor controls/preview/copy and authoring validation. Level source permissions and multi-Citadel edge prohibition retained; required blocked sectors never fall back. No mission/wave changes or SPAWN4 scheduling. Key owners: SpawnManager.SourcePolicies, SpawnSourceOptions, WaveSpawnSourceResource, SpawnEdgeSector, addons/wave_editor. Source/resource review only; .NET unavailable, owner gameplay/editor/checkpoint validation pending.

## Portal smoke softened — square/cloud-edge artifacts (2026-11-06)

**Visual shader fix, owner Godot acceptance pending.** `WORLD/PORTALS/EnemyPortal/Visuals/PortalSmoke.gdshader` now uses quintic-smoothed, rotated and gently bent two-octave noise rather than visibly grid-aligned billows. A broader cloud-density falloff and explicit smooth alpha feather on all four particle-quad boundaries remove the hard rectangular/blotchy edges reported in the portal screenshot, including the larger faint smoke layer. Kept the same shader resource, palette, `smoke_tint`/`opacity` parameters, particle age-based animation, emitter counts/size/lifetime, pause, offscreen handling, and SPAWN2/SPAWN2.5 mechanics. No new materials/nodes, CPU loops, or additional noise samples. Source-only check; test the clouds during a wave and at large portal scale in Godot. 

---

## FD4.1 targeting + FD5 Codex handoff — 2026-11-06

**Planning only, no gameplay implementation.** Added detailed FD4.1 to `DRONES/Combat/FIGHTER_DRONES_IMPLEMENTATION_PLAN.md`: keep Citadel-risk enemy as movement anchor, independently choose nearest actually shootable in-range/forward-arc/terrain-clear enemy for fixed twin guns, with separate short target hold, bounded shared spatial queries, shot-time LOS, pause/recall and checkpoint safety. Preserved the full FD5-A–E specification for real Depot→Station ammo deliveries, finite timed rearming/paid repair and small vertical per-fighter service telemetry. Recommended Codex order **FD4.1 then FD5**, with lightweight checks and separate status reporting. Corrected the outdated backlog note that called already-source-implemented FD4 weapons only planned.

---

## Shield Emitter balance and size pass — 2026-11-06

Promoted `shield_emitter_mk1` **Light → Standard**; hull art **40×32 → 60×48** (1.5× uniform vector enlargement), hull **100 → 200 HP**, movement **70 → 60px/s**, navigation clearance **16 → 22px**. The emitter's shield capacity (80), shield restoration (20 per second in 192px radius), faint world-space radius, unique ID, Rank/Role, rewards, waves and shared systems are unchanged. F2 and Wave Editor use authored Class automatically. Updated `Enemy003.tres`, its SVG and relevant documentation, with one focused assertion in `ShieldEmitterChecks.cs`. Source-only check; owner to validate actual Godot art/ring alignment and corridor navigation.

---

## FD5 plan update — real station-supplied fighter ammunition and service — 2026-11-06

**Planning only; no FD5 gameplay code changed.** Expanded `DRONES/Combat/FIGHTER_DRONES_IMPLEMENTATION_PLAN.md` into FD5-A–E Codex handoff: Depot→Drone Station uses the existing courier/consumer logistics (requires a small endpoint generalization), station stock is finite and initially empty, fighters auto-return at configurable low ammo, timed 6s-full-load default consumes actual local ammo, paid gradual HP repair and one service bay are configurable, and a tiny bottom-up cyan bar appears by the docked fighter only when rearming. Defines manual-recall vs automatic relaunch, zero-stock waits, roster status, save/restore, accounting, lifecycle and low-overhead acceptance. FD1-B/FD2/FD4 remain source implemented; FD3/FD5 are not implemented; owner/ Codex gameplay work remains.

---

## Capital cannon +33% weapon range — 2026-11-06

Changed only `ENEMIES/Types/ObsidianDreadnought/CapitalCannon.tres`: `RangePixels` 275.0 → **365.75** (+33%). All four capital weapon mounts share that preset, so each gains range without a gun script, target logic, damage, rate of fire, bullet speed or other ship changes. Adjusted its directly relevant README balance line. Source-only review; Godot targeting acceptance pending.

---

## CW1 shared weapon recoil — 2026-11-06
Extracted the existing motion/style into `FEEDBACK/Weapons/Recoil/`; tower scripts remain compatible adapters with unchanged presets, paths and smoothstep behaviour. Gunner/Heavy/Capital use 0.8/1.5/2px presets, one retained controller per opted-in mount (four independent Capital guns). Enemy physical muzzles moved to fixed sibling markers at unchanged rest coordinates; request/flash captured before accepted-shot recoil. Lifecycle/style edits cancel/reset, idle controllers sleep. Updated existing Gunner source checks/paths; source/resource review only, .NET unavailable and fixtures unrun. Owner to check visual alignment/return/pause and tower compatibility. CW2 untouched.

## Island power inspection UI — 2026-11-06
Source implemented under `UI/Power/IslandInspection/`: occupied-island terrain boundaries, 0.25s hover reveal/0.15s leave grace, cursor tooltip and temporary amber/red footprint warnings. Separates local generation, transmitter-shared supply, smoothed actual consumption/recharge and tower buffers. New normally charging towers aren't flagged; sustained deficits gate amber. Cached merged geometry rebuilds only on topology changes; event-driven membership, local edge hit tests, hovered-only consumer samples. Read-only manager snapshot seam; no allocation, saves or tower material changes. Shared World composition updated. Source/resource review only; .NET unavailable, owner visual/gameplay acceptance pending.

## Calmer fighters + large faint portal clouds — 2026-11-06
Full fighter firing arc now 180° (configurable), permitted across deployed manoeuvre stages; fixed twin barrels/rate/LOS/ammo retained. Corrected return-waypoint mismatch, filtered steering/turn rate and eased acceleration; calmer defaults and 1.5s target hold reduce thrashing. Added bounded second portal smoke layer (8 larger, faint dark clouds, 8s life) alongside existing smoke. All emitters use portal-local coordinates so SizeMultiplier scales rim/particle size/drift consistently. Same pause/culling/wave-end/reset lifecycle. Lightweight source/resource review only; .NET unavailable, owner flight/visual/FPS acceptance pending.

## Fixed twin-gun fighter jets — 2026-11-06
Replaced rotating minigun with one integrated twin-barrel SVG and fixed left/right muzzle markers. Alternate accepted rounds using ammo parity; total rate 5→9/s, 8° hull-forward cone with lead/LOS. Fighter movement stages a run, crosses/extends beyond the enemy and alternates return lobes; no firing while turning back. Config exposes run-up/exit/loop settings. Fighter-only preset extends shared exhaust 3.2× with acceleration flare; other presets keep defaults. Targeting, ammo/damage/credit rules, launch/recall and snapshot schema retained; attack leg reacquired on restore. No FD3/FD5. Lightweight source/resource review and SVG render; .NET unavailable, owner flight/firing/visual checks pending.

## Organic portal feedback + lingering wave smoke — 2026-11-06
Varied cosmetic pulse strength/time, spark count/orientation and burst spacing via isolated RNG. Replaced spawn-reset smoke with one bounded continuous emitter per Open portal during active waves: 18 live puffs, 110px, 5s, slow drift/growth/swirl. Wave start/clear/outcome signals own emission; completion drains smoke, culling/pause/restore/unload stay bounded. Preset settings exposed; no spawn timing, opening duration or gameplay RNG changes. Source review; .NET unavailable, owner visual/FPS acceptance pending.

## FD4 — Fighter Drone blue Gunner-style kinetic rounds — 2026-11-06
**Source implemented / awaiting owner Godot acceptance.** Fighters now have independently rotating nose guns (`DRONES/Combat/FighterDroneWeapon.cs`) with blue versions of the original enemy Gunner's tapered tracer and small blue muzzle flash. Fired rounds use existing friendly `BasicProjectile` swept enemy/shield damage, shot-time terrain LOS, configurable Kinetic damage, range/speed/cadence/aim tolerance, and consume one of the fighter's 200 finite rounds only after launch. Fighter-specific `DamageSource` preserves ordinary kill Credits but intentionally gives **no tower XP / fighter XP**; rank attribution is a later policy. Shared BasicProjectile draws the same reusable tapered Gunner geometry when `Tapered` styles opt in; old straight tower tracers stay unchanged. Shots use actual `Body/GunPivot/Muzzle`. Restore adds optional weapon cooldown (old saves default 0). No FD3 patrol, no FD5 service or hostile anti-air. Source/reference checks only; Godot/.NET not executed. Owner should test fresh deployment/blue appearance, shots on moving enemies, shield/hull, ammo decrement, mountain LOS, recall/sale/disabled station and Save/Load.

## SPAWN2.5 — 2026-11-06
Source implemented: Open-portal accepted spawns add a 12%/0.8s artwork pulse and retained six-puff violet smoke emitter with two-octave shader. Portal-owned preset controls size/spread/life/speed/swirl/opacity/tint/brightness. One coalesced cooldown (minimum 750ms), pause-aware particle phase, offscreen/opening/restore/unload reset; root/exit/labels, 2.2s materialization and all gameplay/waves unchanged. Source/resource review and native shader parse; .NET unavailable, owner visual/performance acceptance pending.

## FD2 + Drone Station migration — 2026-11-06
Source implemented: station scripts/scene/config/definition/art/UI moved to `TOWERS/Utility/DroneStation/`, catalog/resource/docs updated, same `drone_station` ID, 2×3 footprint, paid roster and dock slots. Fighter code stays in `DRONES/Combat/`: shared budgeted remaining-journey targeting, independent pursuit/figure-eight/orbit/kiting and home guard, configurable fixed-rate steering, recall and airborne checkpoint motion. No FD3/FD4/FD5. Source/reference review only; .NET unavailable. Owner to check launch/multi-fighter retarget/recall, pause and checkpoint restore in Godot.

## FD1-B — station-owned fighter foundation — 2026-11-06

Added paid fighter slots and station Inspector BUY/LAUNCH/RECALL, fixed-rate bounded flights and persistent ID/HP/ammo/slot/flight snapshots. New longer 40×52 fighter SVG keeps the current cyan/steel drone style; shared body accepts engine/trail offsets with existing defaults preserved. Two slots / 250 Credits / 300 HP / 200 ammo initially; no free refills or replacements, combat/autopilot/patrol/service remain FD2+. Station sale removes owned fighters; disabling recalls them. Main paths: DRONES/Combat/, shared DroneBody, selected HUD and TowerBase save seam. Source/reference review and native SVG render only; .NET unavailable; owner to check purchase, launch/recall, pause, disable/sale and fresh save/load.

---

## Obsidian Dreadnought flat black fleet art — 2026-11-06

Recoloured existing 352×208 CAPITAL hull SVG and four shared-instance 24×72 cannon SVGs to flat near-black/charcoal plates with restrained scarlet/orange-red mechanical accents. Preserved full hull silhouette, original SVG dimensions/viewBoxes and four weapon mount positions, barrel-tip alignment, enemy stats, movement, targeting, payloads and waves. Removed metallic vector gradients/highlight colours; source-level SVG checks only, owner F2/Level011 visual acceptance pending. Files: `ENEMIES/Types/ObsidianDreadnought/Artwork/`.

---

## Automatic wave progression and live victory hold — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Wave 1 remains manual; subsequent waves auto-start 30 seconds after the previous wave fully clears, with per-wave overrides in the Wave Editor and an upper-right AUTO START countdown beside the still-available NEXT WAVE button. Pause/checkpoint Continue retain the countdown. Early start, outcomes, reset and unload cancel it safely. Victory keeps the map/drones/effects alive during the existing 4-second delay, while the official stat snapshot stays fixed; the map pauses when Results opens. Defeat remains immediate. Source/lifecycle and authored-fixture review only; .NET unavailable, owner visual/gameplay acceptance pending. See LEVELS/Waves/README.md and LEVELS/Session/README.md.

---

## Restored slower portal opening — 2026-11-06

Restored the shared materialization animation to its original 2.2 seconds, including authored preset and code defaults/fallback. Level011 Wave 6 still leads with its existing Portal4 ship, so it launches on readiness without waiting for a later shuffled request. Drone motion and widget changes remain. Focused source review only; owner visual acceptance pending.

---

## Drone travel, site widget and portal pacing — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Shared builder/ammo flights use fixed 800px/s² acceleration and braking with real cruise time, preserving curves and source speeds. Construction/upgrade widgets are another 10% smaller (0.72 scale), closer upper-right and fade in over 0.25s as the builder approaches within 140px. Portal opening defaults to 1.1s, and Level011 Wave 6 leads with one existing Portal4 ship rather than waiting for a shuffled request; total 216 ships / Portal4 26 preserved. Flight snapshots append optional profile data and retain legacy active legs. Source review and numerical flight/quota checks only; .NET unavailable, owner gameplay/visual acceptance pending. Details: DRONES/Shared/README.md, TOWERS/Shared/Construction/Visuals/README.md and WORLD/PORTALS/EnemyPortal/README.md.

---

## Strongest uses maximum hull HP, then First — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Shared tower targeting now scores Strongest by `Enemy.Definition.MaxHealth`, independently of current hull/shield points. Equal maxima compare First's cached remaining Citadel journey; finite routes beat unavailable estimates, then registration order resolves exact/unavailable ties. Existing preferences/status filters and sight/range checks remain; Weakest is unchanged. Updated Inspector tooltip, targeting README, existing Inspector/Journey regression expectations. No added scans, A* requests, save fields or enum changes. Focused source review only; .NET unavailable and fixtures not executed. Owner: inspect a badly damaged heavy enemy versus a full-health weaker enemy, then equal-max enemies from different portals.

---

## SPAWN2 — shared portal materialization — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Every registered START portal now forms on fresh deployment; Wave N portals use the same Dormant → Forming → Open presentation on their accepted authored wave. No Level011 map or wave data changed. A 2.2s pausable tween drives a localized violet spiral shader, growing segmented ring, art/label reveal, inward GPU curls and concentrated ignition/ripple. No fullscreen effects, audio assets or idle CPU animation loop were added. Existing 120°/s rotation and 48-particle outward per-spawn rim bursts remain the Open-state presentation.

`IsSpawnActive` means legally activated; `IsSpawnReady` additionally requires Open. Required requests retain their selected ship/count and sleep the wave scheduler during formation. A readiness event updates only that source's eligibility slot and wakes the scheduler; random requests ignore unfinished portals without consuming visual RNG. No terrain rebuild, route reassignment or new path queries. Between-wave restore uses explicit checkpoint context (including completed-wave count zero), snaps due portals Open and suppresses opening replay/warnings.

Tune `WORLD/PORTALS/EnemyPortal/Visuals/DefaultPortalMaterializationStyle.tres`: duration 2.2s, optional START stagger 0–0.2s, vortex/ring/flash, reduced motion, particles (72 inward + 24 ignition defaults), and at most 6 simultaneous visible particle pairs. Overflow/offscreen openings use the same timeline with shader/pose only; offscreen exit stops particle emission. Opening nodes hide and tween/budget slots release on completion, reset or unload. Disabled/reduced motion retains mechanical timing with simpler ring/fade and no opening particles. Later-wave activations can request one batched sector banner through the existing alert queue; START never alerts or interrupts mission intel. No suitable approved warp sound was present/wired; future sound may hook opening start/ignition without owning readiness.

**Minimal validation:** focused source/scene-reference and readiness fixture review plus one minimal Godot 4.6.3 headless shader parse (passed); .NET unavailable. Updated `PortalActivationChecks` covers forming exclusion/RNG retention, immediate START wait/resume, late activation, once-only eligibility, no navigation/assignment changes and wave-zero restore without replay. Fixture not executed; visual/FPS acceptance remains with owner. Check fresh multi-portal load, immediate START, pause/resume halfway, later-wave activation, window/camera culling, reduced effects, unload/retry and a fresh checkpoint.

---

## Construction percentage widget and portal feedback polish — 2026-11-06

**Source Implemented / Awaiting Owner Visual Validation.** Construction telemetry is percentage-only, uniformly 20% smaller, anchored upper-right of the footprint; successful completion counts to 100% then fades. Inbound/locked/online labels and job caption are removed. Portal spin defaults to 120 degrees/sec; 48 brighter/larger GPU sparks spawn from explicit 64-point rim geometry and travel radially outward. Existing event ownership, work/gameplay rules, shared bounded drawing, 750ms burst cap and culling remain. Source/resource sanity review only; .NET unavailable, owner visual/FPS checks pending. See [construction visuals](../TOWERS/Shared/Construction/Visuals/README.md) and [portal feedback](../WORLD/PORTALS/EnemyPortal/README.md).

---

## Content-sized mission narration — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** `UI/Narration/NarrationDisplay` now sizes its shared cyan card to the full wrapped text instead of clipping briefings at 136px. The existing PanelHeight becomes a minimum. RichTextLabel fit-content and reveal-after-shaping reserve the full text before typing; minimum-size/viewport events reflow and shrink replacements without idle polling. Applies to every mission briefing and wave transmission. Voice hold, timing, authored text and input remain unchanged. Updated narrator/briefing docs and config comment. Source/scene sanity review only; .NET unavailable. Owner: inspect all Level001 paragraphs, resize, then replace with short narration. Extremely short viewports can still be smaller than long authored messages.

---

## Shared weapon terrain vision — 2026-11-06

**Source Implemented / Awaiting Owner Validation.** Mountains now block enemy weapon acquisition, aiming and firing as well as friendly tower targeting. `WorldGrid.HasWeaponLineOfSight(from, to)` is the neutral shared API, using the existing allocation-free cached terrain traversal. `HasTowerLineOfSight` remains a compatibility wrapper; the existing `blocks_tower_sight` tile metadata, blocker count/cache, corner rules and no-blocker fast path are preserved. Friendly target queries and the selected red/visible range overlay call the neutral API; no second visibility cache, terrain scan, physics ray system or global fog system.

Enemy `TowerRegistry` exposes its already-cached owning grid. `EnemyTowerTargeting.Valid` and `Closest` use that same terrain test, with an optional sight origin independent of range origin. Enemy mounts pass their **own muzzle** for sight while retaining pivot-based range/closest scoring and stable registration-order ties. Only candidate score contenders traverse terrain, so a hidden nearest tower cannot suppress a visible fallback. Existing staggered acquisition and lock-hold intervals remain. A newly obstructed lock is cleared before aiming/firing; if aiming/countering hull rotation moves an aligned muzzle, sight is revalidated before emission. The redundant identical pre-aim lock check was removed. All four Capital guns and both Gunner variants inherit this behavior through `EnemyWeaponMount`; no per-ship branches or new processing loops.

Projectile flight/collision, already-emitted bullets, damage, splash/pulses, repair, stealth rules, navigation, tile flags, weapon balance and saves are unchanged. This pass concerns weapon line of sight only.

**Minimal validation:** focused source/diff checks only; .NET unavailable. Existing MountainSightChecks was extended with neutral/legacy API parity, reverse blockage, hostile hidden-target rejection/visible fallback, independent offset muzzle visibility, and a real Heavy Gunner mount refusing shots until terrain removal. C# checks are authored but **not executed** here; no build, gameplay or performance claim. Owner: rebuild, place a mountain between a tower and Gunner/Capital, verify neither side targets/fires through it, move around the mountain and inspect each Capital muzzle independently. Already-fired projectile collision remains unchanged by design.


---

## Obsidian Dreadnought and larger ranked F2 picker — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** The default enemy catalog now has 16 ships, including its first **Capital / Champion / Fighter**, `obsidian_dreadnought_mk1`. Native 352×208 black/charcoal armour with sleek crimson accents, baked vector lighting, four reinforced weapon sockets and a separate native 24×72 rotating cannon sprite. Four independent existing EnemyMinigun mounts sit at front-left/front-right/rear-left/rear-right positions (112,−62), (112,62), (−104,−62), (−104,62), each with its own aiming/target/cooldown state and exact barrel-tip Marker2D (0,−32).

**Provisional balance:** 4,500 HP, 22px/s, 1.5 hull turn speed, 20 Citadel arrival damage, 100 kill Credits, no default shield and 2.5 death-explosion multiplier. Each gun uses the Heavy Gunner baseline: 275px range from its pivot, 5 Kinetic damage, 2.5 shots/s, 650px/s unguided red tapered 14.6×2.9 tracers, 2.8s lifetime, accepted-shot muzzle flash. Four guns can fire independently; nominal aligned total is 10 bullets/s / 50 DPS. Dedicated Capital config/visual resources allow later balance tuning without changing either Gunner variant. Existing spatial tower queries, staggered acquisition, shared bounded hostile bullet renderer/collision, stasis/pause/host removal and generic damage/status/navigation/XP/Credits systems remain authoritative. The ship intentionally keeps the existing large-enemy **24px navigation clearance** rather than changing map/path rules to match its much larger cosmetic hull. No special boss AI, wave insertion, new weapon system or autoload.

**F2:** 1,000px minimum panel, 560px default left picker column, 40px selector and 17px selected text; popup uses selector width and a viewport-bounded scrollable height (620px default). `EnemySpawnMenu` exposes picker width/height. Existing class headings remain, smallest to largest; rows show **Name — Rank**, ordered within each class by ascending rank, then name and stable ID. Header placeholders preserve exact scene/preview/spawn mapping. Hover preview still copies only native sprite art, now including all four Capital guns. Batch caps, timing, portals, shields/stealth flags and authored wave ownership are unchanged. Capital is available for F2 and future Wave Editor authoring; **no existing wave resource changed**.

**Minimal validation:** source/resource/SVG XML review and a local Inkscape hull+four-gun composite render inspected. Focused existing F2 and gunship fixture expectations updated for rank order/labels, 16 catalog ships and Capital composition; C# fixtures **not run** because .NET is unavailable. No Godot gameplay, weapon/balance or performance acceptance claim. Owner: rebuild, open F2 → CAPITAL → Obsidian Dreadnought — Champion, spawn one, and inspect native size, slow turning, four independently aimed guns/barrel origins, red tracers, stasis/death cleanup and lower-rank-first menu ordering. Keep owner wave balance unchanged until separately requested.


---

## Selection-weighted Transmitter links and drain verification — 2026-11-05

**Source Implemented / Awaiting Owner Visual Validation.** Unselected Transmitter pairs now draw only an extremely faint static line: `UnselectedOpacity=0.04` multiplies existing alpha (powered core about 2.2% final alpha). No halo/pulses or animation processing while unselected. Selecting **either** endpoint restores the current brighter core/halo/pulses using `SelectedOpacity=1.0`. Both opacity knobs live in `FEEDBACK/Power/DefaultPowerDistributionVisuals.tres`. One lower-SaveId renderer remains; it observes the peer's selection event and detaches on unlink/re-pair/exit. Closing inspection clears distribution selection, returning the line to faint and hiding Relay coverage. Existing offscreen, pause and inactive-state gates remain.

Checked current default configs and attack/repair seams: all other eight offensive tower types and Repair **already have nonzero idle/active drain** through the existing shared capacitor. Machine Gun 0.15/10, Sniper/Cryo/Stasis 0.10/8, Blaster/Rocket 0.15/14, Laser 0.20/12, Shockwave 0.20/14, Repair 0.15/8 energy/s (idle/total active). Battle reference remains 0/12. Configs reference their individual Power resources; volleys/pulses pay once and continuous Laser/Repair pays the funded duration. Idle applies outside paid work while operational; active replaces idle. Recharge can offset drain and keep a bar full. Rates, funding, generation, save schema and combat balance were not changed or duplicated.

Minimal source/diff/resource checks only; .NET unavailable, no C# build or in-game acceptance claim. Owner: select each endpoint independently, close inspection, unlink/re-pair, and check faint/selected link styles; inspect actual Use versus Refill on idle/firing/repairing towers. For visible depletion, temporarily remove supply or inspect a source-limited network.


---

## PW5-C — distribution visuals and inspection — 2026-11-05

**Source Implemented / Awaiting Owner Visual Validation.** Selecting a Power Relay now shows faint cyan filled/outlined **reachable conductive cells**, plus a thin dashed shortest route to a live source and the label **Nearest live source / Island supply shared**. No circular coverage spanning gaps. Inactive/source-less Relays show no coverage/route. Deselecting hides the diagnostic immediately. The route follows cached conductive distance and positive Transmitter hops when supply is remote; ties are deterministic and a source footprint terminates the path.

Powered Transmitter pairs now have a narrow cyan core, faint halo and two restrained travelling highlights. Unpowered retained links are dashed blue-grey; suspended links are dashed amber; unlink/removal hides the line. Only the lower-SaveId endpoint owns a renderer. Highlights are cosmetic, not an exclusive source or measured energy-flow direction. No power generation, allocation, booster, hop cost, consumer charge, combat, construction or save-format changes.

Selected distribution Inspector status remains bounded to three lines, with concise state, physical island / effective network / shared supply, plus peer identity/island or Relay boost coverage/ceiling. F4's existing throttled power row now distinguishes hovered physical-island output from joined-network supply and global active pair count. No new diagnostic polling loop or growing status rows.

**Tuning:** `FEEDBACK/Power/DefaultPowerDistributionVisuals.tres` exposes coverage fill/edge, source path/dash length, link core/halo/status colors and widths, pulse period (default 2.8s) and animation cadence (15 Hz default; clamped 5–30). Restart/redeploy after authoring style changes. Art and simulation settings stay with their existing owners.

**Performance/lifecycle:** Relay geometry uses retained buffers and caches by delivery revision/selected transform, and only builds while selected. Coverage extraction reuses the bounded conductive search scratch; route reconstruction descends the existing cost field without a second source search. Straight route sections collapse to one drawn segment; exceptional paths cap at 4,096 steps with an explicit preview-capped label. No per-frame map traversal/pathfinding. Link geometry updates on lifecycle/transform/state events. Only an onscreen powered pair processes its small local cosmetic clock; redraw is capped at 15 Hz by default, processing sleeps offscreen/hidden/unpowered/suspended, and inherited scene pause freezes animation. Static visuals rely on normal canvas culling. Removing a tower destroys its owned child visuals; no autoload or new power service.

**Validation:** minimal source/diff and native resource-reference checks only; .NET unavailable. No C# build, full Godot gameplay/visual acceptance or FPS benchmark is claimed. Owner: pull/rebuild, load Level011, select a powered Relay near a gap, then select/deselect and remove its source. Pair Transmitters, inspect active/unpowered/EMP-suspended styles, pan away/back, pause and try a fresh save/load. PW6/new generators and the optional priority slider remain deferred.


---

## PW5-A/B — two Utility power buildings — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Power Relay locally boosts capacitor recharge on conducting terrain (best-only 10-cell/85% provisional ceiling). Separate Power Transmitter supports explicit LINK → another island’s Transmitter / UNLINK, one retained reciprocal peer and a thin static cyan/amber link. Shared cached networks count real output once, use positive-hop source distance and retain finite distance-first allocation. Provisional Relay 150 Credits/3s/800 HP; Transmitter 200/4s/900 HP; both 1×1, no generation/upkeep or local boost from Transmitters. Existing energy/heat/ammo/work/disable/save owners are reused; optional durable peer IDs resolve before unpause. Minimal source/resource/SVG checks only; .NET unavailable, C# fixtures authored but unrun. PW5-C visual polish, PW6 and priority slider remain planned.

[Full details and owner checks](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md).

---

## Runtime map bounds link restored — 2026-11-05

Level011Config now explicitly assigns WorldConfig = Level011WorldConfig.tres. Without that link, WorldMapLoader retained the shared 100×80 grid, so the camera stopped at X=6400 even though the separate local resource said 192×120. Restored the resource reference and property only; current terrain, three portals, waves, Credits and Citadel placement are preserved. The existing loader applies the 192×120 bounds before grid/camera initialization. Minimal source/reference checks only; restart/redeploy Level011 in Godot to verify right-edge pan and Citadel access. No build/runtime claim.

---

## Shared power rollout to offensive towers and Repair — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** All nine offensive types plus Restoration Tower now use PW4-A–D capacitors and the existing Inspector ENERGY/power details. Each has a configurable per-type profile with modest provisional rates; Battle's baseline is unchanged. Atomic projectile/volley/pulse payments and duration-limited Laser/repair payments reuse the shared service and NoPower recovery. Active replaces idle; no new polling, generator, relay, priority slider or save schema. Existing combat, ammo, heat, construction, upgrades and checkpoint owners are preserved. Source/resource checks only; .NET unavailable, owner Godot validation pending. [Rate table and accounting](../TOWERS/Shared/Power/README.md).


---

## Level011 right-edge bounds fix — 2026-11-05

Expanded only Level011WorldConfig WidthCells 160→192 (height120/cell64 unchanged). Current paint reaches X=175 and the Citadel sits past old X=10240px, so the camera correctly clamped short and grid ignored right-side content. New local bounds12288×7680 contain current layout with margin. Authored scene/objects, shared camera code, other missions and global defaults untouched. Updated Level011 notes. Minimal coordinate/config review only; restart/redeploy and inspect right-edge pan/placement in Godot. No runtime/benchmark claim.

---

## Level011 shared map migration — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Level011 now inherits BaseLevelMap with four local editable terrain layers. Focused before/after comparison preserved every original node property, scene header, terrain block, object position and resource reference. Independent 160×120 WorldConfig, LevelConfig, waves and mission registration are untouched. Updated Level011/shared authoring notes and added its migration inventory. Source comparison only; no repeated Godot probe, build or gameplay test. Owner: reopen Level011, paint local layers and deploy through Campaign. [Level011](../LEVELS/Level011/README.md).

---

## LM1 — shared map scene architecture — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Added LEVELS/Shared/BaseLevelMap.tscn with WorldLevel root, common background/container/debug structure and safe dormant atmosphere defaults. Chose the hybrid: each child keeps four local editable TileMapLayers sharing existing TileSets. Migrated Level001/002/003/020/100 and LevelTemplate; exact terrain blocks, transforms, object/config/resource properties, original scene UIDs and existing node IDs/paths matched the before/after inventory. Level001 retains active atmosphere; Level020 retains its floor offset. Added an unregistered authoring probe and original map inventory; updated concise level/shared/runtime authoring docs. No Level011, dimensions, runtime-service code, waves/catalog, balance, gameplay or save-schema changes. One small native Godot 4.6.3 hybrid save/reopen probe passed using script-free stand-ins; no .NET build, full Campaign launch, manual editor acceptance, broad suite or benchmark. Owner: inspect inherited maps/local paint, Campaign/F2/retry and fresh checkpoints in Godot .NET. [Shared map guide](../LEVELS/Shared/README.md).

---

#### Stable NoPower Inspector and shared disabled tower visuals — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** NoPower no longer adds a duplicate Main-column warning row; existing energy meter/disabled header remain. PowerDetails now keeps three explicit non-wrapping lines at fixed height to prevent status/recovery text growing the panel. Added one lazy shared disabled presenter per affected tower: subtle art SelfModulate red tint plus a static vector triangle/exclamation above the fixed upper-right footprint. All existing disable reasons share it; initial construction hides it, overlapping reasons retain it, recovery restores authored art tint without overwriting selection Modulate. No animation/processing loop or periodic sprite/grid scan. Shared style resource tunes color/size/offset; scripted effects/UI branches are excluded. Focused HeatInspector source checks updated and isolated from capacitor funding. No gameplay, power math, saves or authored scene paths changed. Source/scene checks only; Godot/.NET visual/runtime validation pending. [Disabled feedback](../FEEDBACK/Disabled/README.md).

---

## PW4-D — selected power Inspector feedback — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Preserved ENERGY current/capacity bar; added compact Main-column actual refill, distance ceiling, measured consumption, conductive distance/efficiency and connection/recovery state. Typed TowerPowerInspection service samples count funded attacks/settled idle losses once, exclude ammo rollback, and clear stale rates with one final shared-service sample before sleep. Existing selected-only coalesced RuntimeChanged updates and preview/close behavior are reused. Priority remains inactive; no relay, extra consumer, balance/allocation/save-schema change or per-tower/UI process loop. Added focused sample/format/dormancy assertions to PowerAllocationChecks and updated Inspector/power docs/backlog. Lightweight source/scene checks only; .NET/Godot/layout/runtime validation unavailable. Owner: selected Battle charging/firing/full/starved/disconnected, work/pause, switch/close/reselect, small-window layout and fresh Save/Load. [Inspector guide](../UI/GameplayHUD/Inspector/README.md).

---

## PW4-C — distance-first scarce-supply allocation — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Replaced proportional capacitor sharing with finite per-island nearest-first allocation using PW4-B cached conductive distance and ordinal SaveId ties. Requests respect charge eligibility, recharge ceilings and headroom; leftovers reach later towers in the same interval. Stored charge/shot funding/NoPower remain independent; saved PowerPriority stays dormant. Cached dirty-only sort plus reused request snapshots/island budgets avoids per-frame resort/BFS or new per-tower processing. Work-state boundaries settle old intervals; actual delivered refill is exposed separately as a derived last-service sample. Added a narrow three-capacitor fixture via PowerNetworkChecks and updated power plan/guides/backlog. No PW4-D UI, priority slider, relay, balance or save-schema change. Brief source/resource review only; .NET/Godot unavailable, no runtime/benchmark claim. Owner: low-output source, near/far requests, near buffer filling, source loss/restore and fresh Save/Load. [Power plan](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md).

---

## Selected range obstruction and sticky restoration — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Placement delegates selected range to one cached TowerRangeOverlay: aimed hostile ranges show cyan visible area and red obstructed area/outer arc using the authoritative WorldGrid sight query. Fixed 384 angular samples with bounded binary refinement rebuild only on selection/range/position/settings/terrain changes; no processing loop, camera-triggered rays or per-tower effects. Existing TowerMasterConfig owns colors/fill opacity. Support/untargeted circles retain their actual non-sight semantics; Inspector close/deselect/unload releases geometry. Restoration now queries only without a valid lock, retaining its target until full/ineligible instead of periodic lowest-integrity ping-pong. Added narrow two-target crossing/completion checks to RepairTowerChecks. Brief source checks only; no .NET/Godot/runtime/benchmark claim. See [Selection feedback](../FEEDBACK/Selection/README.md) and [Restoration](../TOWERS/Support/RepairTower/README.md).

---

## Mountain refinement and tower sight — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Increased default peak height from 1.1 to 1.22 cells and added configurable static mineral/strata shading on existing meshes. New independent blocks_tower_sight TileData opts only Mars mountain (source 2, cell 2,0) into cached WorldGrid obstruction. Shared hostile acquisition checks potential winners; cached locks recheck sight on existing weapon ticks, including Laser shutdown. Allocation-free supercover traversal, no-blocker fast path, no scene searches per candidate, no extra processing loop or per-cell nodes. Occupancy, other terrain, target priority enums, existing projectiles/splash/untargeted effects and save format are unchanged. Narrow optional MountainSightChecks fixture authored, not run. Source/resource checks only; Godot/.NET/shader/gameplay review pending. See [mountain guide](../WORLD/TERRAIN/Mountains/README.md).

---

## Mountain terrain visual first pass — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Mars atlas expands to 192×64, preserving dust/rock coordinates and adding mountain marker (2,0) to Impassable source 2. Existing navigation/placement flags block movement/building. World-scoped metadata-driven renderer generates shared-vertex faceted ridges and shadows once on load/explicit terrain rebuild, with stable seed and configurable height/colors. No per-cell nodes, processing loops or occupancy invalidation. Prototype atlas and painted maps remain unchanged. Tower line-of-sight and depth-aware object sorting remain future work. Paint a 3×2 block and launch through Campaign/Main to preview. Source/resource review only; Godot/.NET and visual validation pending. See [mountain guide](../WORLD/TERRAIN/Mountains/README.md).

---

## 2026-11-05 — PW4-C distance-first shortage decision (docs only)

Updated [PW4 implementation plan](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md), Power overview, consumer notes and backlog. Under limited output, **nearest conductive-path towers charge first**, more distant towers can receive zero refill, and full batteries leave supply for others. Retains the PW4-A stored capacitor and PW4-B distance recharge ceiling; no energy created/erased. Existing saved PowerPriority stays inactive; future optional player slider may only mildly bias distance after separate approval. PW4-D will distinguish actual recharge from potential recharge. **PW4-C not implemented; docs only.**

---

## Victory Results hold — 2026-11-05

LevelSessionController now holds the frozen winning battlefield for **4 seconds** before showing Mission Results. Configure `LevelSession → VictoryResultsDelaySeconds` in the shared battlefield Inspector; 0 restores immediate presentation. Outcome arbitration, final counters and pause/input locks still latch immediately; defeat presentation is unchanged. One child Timer runs during the results-owned pause, and stop/unsubscribe on controller exit prevents a delayed popup after unloading. No idle processing loop or terrain work. Existing outcome/reward/navigation fixtures explicitly use 0 to retain their immediate-presentation assertions. Source review only; Godot build/play validation pending.

---

## PW4-B — cached distance-limited recharge — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Existing power manager now owns one coalesced multi-source conductive BFS; valid Citadel/generator footprints anchor delivery. Shared Inspector curve: 4 full cells, 12 soft falloff cells, 10% reachable-route floor. Recharge ceiling = nominal refill × efficiency; no route means zero. Capacity/drain/attack funding/source output and stored charge are unchanged; no new consumers, priority scheduling or relays. Cached consumer APIs expose distance/efficiency/ceiling; final derived cache is rebuilt before checkpoint unpause without new saved fields. Performance: reused arrays, no per-tower pathfinding/frame flood fill, no distance rebuild for positive output or curve-only edits. Narrow fixture updated; source/resource review only (environment unavailable), no build/runtime/benchmark claim. [Power guide](../TOWERS/Economy/Power/Shared/README.md).

---

## RT1 — playable Restoration Tower baseline — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** New `repair` Support tower: 350 Credits, 5s, 1000 HP, 1×1, 320px, 16 hull HP/s, aimed white/silver/aqua turret and shared muzzle-origin beam. RT0 registry targeting, reusable scratch/cached lock, ordinary RestoreHealth, construction, selection and checkpoint owners reused. Added optional appended RepairStats with finite clamp/final forwarding and minimal correctly labelled Inspector rows. Enemy targeting, other tower balance and waves unchanged. **RT1 has no heat/power operating cost; RT2–RT4 remain planned.** Focused fixture authored; source/resource review only (environment unavailable), no build/runtime/visual/benchmark claim. [Repair guide](../TOWERS/Support/RepairTower/README.md).

---

## SE2 — directional Stealth Beacon scanning — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Prepared head now rotates clockwise and existing ScanCone path owns one faint sector; real 70° / 256px cone, 3s revolution, 0.1s sampled detection, alpha 0.08. Existing world reveal index/source identities are reused; capped nearby queries update stationary enemies, retained members do not flicker and independent overlaps remain revealed. Pause freezes scanning; disabled/unfinished/destroyed/removed sources sleep/hide/release. Source/config edits and index teardown retain cleanup. Art/scene paths, 250 Credits, 1800 HP, 2s build, waves and other mechanics are unchanged; no power/upgrade/bubble. Focused stealth checks updated; source review only (.NET unavailable), owner build/runtime/visual validation pending. [SE2 record](../TOWERS/Utility/StealthBeacon/DIRECTIONAL_SCAN_IMPLEMENTATION_PLAN.md).

---

## RT0 — reusable friendly-tower targeting — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Added optional TargetDomain vocabulary and FriendlyTowerTargeting using the existing TowerRegistry, caller-retained scratch and O(1) lock validation. Lowest finite hull fraction wins; exact ties use registration order. Root-range/self/full/dead/queued/detached/constructing/upgrading filters; disabled targets remain repairable while disabled/busy healers cannot acquire/retain locks. Existing enemy targeting/enums, registry, HUD, catalog, waves and stats are untouched. Optional narrow fixture authored; source checks only because .NET is unavailable. **RT1–RT4 remain planned.** [Repair plan](../TOWERS/Support/RepairTower/IMPLEMENTATION_PLAN.md).

---

## Power PW4-A update — 2026-11-05

**Source Implemented / Awaiting Owner Validation.** Battle-only optional capacitor and paid-shot energy use; finite provisional full-efficiency recharge in the existing manager; owned hysteretic NoPower; checkpoint charge/priority/recovery. Defaults 100 capacity, 10/s refill, 0/s idle, 12/s active, empty initial, 15% restart. No other consumers or combat/source balance changes. **Next PW4-B distance**, then C priority allocation and D selected energy bar; relays remain PW5. Lightweight source review only; Godot .NET build/play remains for owner. [Details](../TOWERS/Shared/Power/README.md). Earlier dated updates below are historical.

---

## 2026-11-04 — Revised PW4 capacitor / distance-falloff plan (docs only)

Updated [TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md) and related consumer/relay guides. **Planned:** each powered tower banks actual energy, recharges slower along long conductive terrain paths, and spends faster during combat. Refill cannot exceed finite island generation and uses 0–100 priorities when contested. Source loss leaves stored energy intact, and a threshold-based NoPower uses the shared disable system. PW4-A buffer/draw, PW4-B spatial recharge efficiency, PW4-C finite refill allocation, PW4-D Heat/Ammo-like energy bar/priority; PW5 relays will boost distribution without producing energy and can manually bridge islands. **PW1–PW3 unchanged; no code or gameplay implemented by this documentation pass.**

---

## Power PW3 — buildable Industrial Generator — 2026-11-04

Economy now offers Industrial Generator: provisional **250 power, 300 Credits, 5s construction, 1200 HP, 2×2**. Shared generator code uses existing construction/art, disable, selection and checkpoint lifecycle without a frame-processing loop. No consumers/outages yet. The selected-tower power bar matching heat/ammo is recorded for PW4. See [Industrial guide](../TOWERS/Economy/Power/IndustrialGenerator/README.md). Source checks only; owner Godot validation pending.

---

## Power PW2 — Citadel starter generation and accounting — 2026-11-04

Citadels now contribute configurable starter power (100 provisional) to their own islands. Shared event-driven accounting and F4 show supply/requested demand without disabling towers; no consumers or generators are added yet. Edit `WORLD/OBJECTIVES/Citadel/DefaultCitadelPower.tres`. See [power details](../TOWERS/Economy/Power/Shared/README.md). Source/resource review only; Godot/.NET build/runtime validation pending.

---

## Power PW0/PW1 — 2026-11-04

**PW0 audit complete; PW1 source implemented / owner Godot validation pending.** Cached four-neighbour power islands now come from independent `conducts_power` terrain metadata, with no per-frame manager work or rebuilds on tower occupancy. F4 reports topology/hovered island and rebuild timing. Tower operation remains unchanged; Citadel baseline output is planned for PW2. See [phased implementation plan](../TOWERS/Economy/Power/IMPLEMENTATION_PLAN.md) and [focused checks](../TESTS/Power/README.md). Generation, consumption, shortages and relays are not yet implemented.

---

## 2026-11-04 — reusable enemy design template (documentation only)

Created [ENEMIES/Types/NEW_ENEMY_TEMPLATE.md](../ENEMIES/Types/NEW_ENEMY_TEMPLATE.md) for future AI/Codex enemy authoring. The file documents the current independent Class/Rank/Role enums, how Mk variants and unusual/anomaly ships fit without rigid ability restrictions, a fill-in brief for visuals/stats/abilities, and a focused safe implementation checklist (reuse GenericEnemy, stable EnemyId, new folder, catalog, docs, no unrequested waves, performance). Linked from Types/README.md. **No code, gameplay, assets, balance, waves or classification values changed.** Documentation-only source review; no build necessary.

---

## 2026-11-04 — F2 enemy dropdown grouped by physical Class

F2 now shows non-selectable class section headings for occupied sizes, **Tiny → Light → Standard → Heavy → Superheavy → Capital → Titan** (when populated), with enemy names alphabetized within each group. Enemy Rank, Role, ID and pixel-size preview remain in the side panel/tooltip. The menu has explicit aligned index slots for headings to avoid wrong sprites or spawns; hover ignores headers and direct header spawn requests reject. The corresponding F2 focused checks now resolve by stable EnemyId rather than former registration indices. No classification values, gameplay, resources, or default catalogue order changed. Focused source checks only; Godot/.NET owner visual and batch-spawn confirmation pending.

---

## Enemy Class / Rank / Role reorganization — 2026-11-04

**Source Implemented / Awaiting Owner Validation.** Replaced Fleet Family with explicit stable-value EnemyClass/EnemyRank/EnemyRole enums and independent Inspector fields. Migrated all 14 existing definition resources by EnemyId to the owner's suggested assignments after checking SVG dimensions/scene scale; removed old Family entries, property, enum and UID atomically. No behavior, balance, IDs, names, paths, waves or artwork changed. Class-sorted short Wave Editor options show selected metadata separately; F2 retains scene/preview order with full caption metadata. No classification ticking, rank scaling, capability restrictions or Mark system.

Updated existing catalog/fleet/Gunner/F2 checks and owning documentation. Validation: focused source/diff and resource invariant checks; no Godot/.NET executable available, so no build/runtime tests. Owner: rebuild C#, inspect Classification fields, Wave Editor sorting/details/ID persistence and F2 hover/select/preview/spawns. **Compatibility:** external custom legacy resources require explicit metadata authoring; existing dependency-byte checkpoint fingerprints may reject older battlefield checkpoints without deleting them. Save validation remains unchanged. See [roster](../ENEMIES/Types/ROSTER.md).

---

## 2026-11-04 — consistent fleet naming in Wave Editor/F2

Created [ENEMIES/Types/ROSTER.md](../ENEMIES/Types/ROSTER.md), mapping all 14 current ship roles, player-visible names, stable EnemyIds and physical folders. Renamed only four vague `DisplayName` strings: Big mk.1 → Heavy Warship, Elite mk.1 → Elite Assault Ship, Racer → Racer Interceptor, Transporter → Drone Transporter. Added shared `EnemyNaming` helper for role text; Wave Editor choice list is grouped by `EnemyFamily`, and F2 shows the role/name/ID without changing scene index order. **No asset folder relocation**, no stable IDs, game stats, waves, or save data changed. Future physical folder/basename migration is separately documented. Focused source review; Godot.NET smoke check pending.

---

## 2026-11-03 — Level001 rebuilt as beginner wave tutorial

Replaced 133-enemy shield-heavy first-mission composition with **three gentle waves: 10 basic / 14 basic / 20 total**. Wave 3 begins with ten plain Standards, then a ten-ship 50/50 weighted mix of Standard/fast Scout after one second; no shields, stealth, Bulwarks or support enemies. All 44 ships fit the unchanged 1,000-starting-Credits mission and manually started waves. Added `LEVELS/Level001/Level001Waves.tres` and repointed `Level001Config.tres.WaveSource` so the user can later author per-wave banners and messages in the Godot Wave Editor. Presentation fields left blank; `Level001Waves.cs` mirrors the data only as legacy reference. Updated focused integration assertions against the authoritative resource and Level001 Mission/README. Source-level verification only; local Godot run pending. Earlier 38/40/55 Level001 shield-test descriptions in historical notes are superseded.

---

## 2026-11-03 — Level003 Wave 4 stronger/weak mixed pressure

Rebalanced only `LEVELS/Level003/Level003Waves.tres` Wave 4: old three distinct batches (16 mixed light + 8 Scout + 8 isolated Bulwark) now two **16-ship mixed batches**, each with 4 Bulwarks alongside weaker Standards/Scouts. Exact compositions: **8/4/4**, then **4/8/4** (Standard/Scout/Bulwark); 0.55s and 0.50s spawn intervals, 0.4s mid-wave pause instead of two 3s gaps. Overall 12/12/8 mix, 32 enemies, no new flags, wave 1–3 untouched. Focused WaveChecks preserves first-three-wave parity with old C# baseline and asserts mixed finale quotas/timings. The old C# file remains historical; Godot runtime check pending.

---

## 2026-11-01 — Wave Editor layout V2 direct VBoxContainer root

The user's screenshot after the prior full-rect margin fix still displayed a collapsed ~100px editor strip with footer near the top, and the right panel scrolled to narration. Replaced `WaveEditorPanel : Control` plus anchored `MarginContainer -> VBoxContainer` with a direct `WaveEditorPanel : VBoxContainer` root, containing header, vertically expanding split controls and footer. Removed `FullRect` anchors on the `EditorMainScreen` VBox child; size flags now control the whole layout, with a 320px minimum-size fallback. Deferred the right-pane scroll reset to occur after Godot's layout, and marked the header **LAYOUT V2** to distinguish stale plugin assemblies. No gameplay or waves modified. Source-only verification; must rebuild/reopen Godot.NET for visual acceptance. [Wave Editor](../addons/wave_editor/README.md).

---

## 2026-11-01 — Wave Editor real full-height layout + visible composition

User screenshot confirmed prior plugin `ExpandFill` change fixed only the outer main-screen tab; the wave/batch lists and right details were still collapsed into a ~100px strip, with the footer immediately underneath despite ample viewport space. Replaced `WaveEditorPanel : PanelContainer` with `Control`, anchored a `MarginContainer` to `FullRect`, retained a flex-height split area, allowed only list columns to vertically expand, and made details sections compact. Moved live exact-count composition preview immediately below batch count/timings, and reset right scroll to top on wave/batch selection and file load. No waves or runtime resources touched; static checks done; in-editor visual confirmation pending. [Editor instructions](../addons/wave_editor/README.md).

---

## 2026-10-31 — WP1 Wave Editor preview + tooltips + delete confirmation

Implemented three requested editor UX changes: exact per-entry composition counts/progress bars for selected batches, using shared `WaveCatalog.CompositionCounts` with largest remainders/stable ties and no random shuffle in the preview; explanatory field hover tooltips; and safe confirmation dialogs for deleting waves, batches or enemy entries. The preview refreshes only on relevant edit/selection events and distinguishes shield/stealth/portal variants. A small `WaveChecks` regression asserts preview quota parity with runtime for 80/20 and tied 50/50 mixes. Authored Level003 `.tres` waves untouched. Future improvements logged in [addons/wave_editor/PLAN.md](../addons/wave_editor/PLAN.md). Godot.NET build and visual smoke test not run here.

---

## 2026-10-31 — Wave Editor enemy catalogue dropdown

Replaced the manually typed enemy ID field with a readable registered-enemy dropdown from `EnemyCatalog`, showing `DisplayName (EnemyId)`. Added a **Refresh list** button, readable labels in the batch-entry list and an explicit [Unregistered] fallback for old/removed IDs. Only the existing stable EnemyId is written to wave resources; the runtime wave scheduler and `.tres` schema are unchanged. The catalogue refresh is on demand, with no editor idle polling. [Editor guide](../addons/wave_editor/README.md). Godot.NET build/visual smoke check pending.

---

## 2026-10-30 — Wave Editor fills main Godot viewport

Fixed user-observed shallow, clipped editor UI on a successfully loaded Level003 wave resource: `GetEditorMainScreen()` returns a `VBoxContainer` and the plugin root lacked `SizeFlagsVertical=ExpandFill`. Explicitly assigned horizontal/vertical ExpandFill alongside the existing FullRect preset. Moved optional presentation fields below primary batch/enemy settings so editing counts, timing, weights and shield/stealth needs less right-pane scrolling. No wave data or gameplay files touched. Source reviewed; Godot .NET/editor rebuild and visual acceptance pending. [Usage](../addons/wave_editor/README.md).

---

## 2026-10-29 — Wave Editor CS0119 build fix

Fixed user-reported `WaveEditorPanel.cs(128,48)`: the local `ItemList` helper shadowed Godot's `ItemList` enum accessor. Renamed helper and its three calls to `CreateItemList`. Other wave editor logic unchanged; local Godot/.NET rebuild pending.

---

## 2026-10-29 — Wave Editor workspace and editable Level003

Source-implemented `addons/wave_editor/` C# `[Tool]` main-screen Godot plugin with Open/New/Save/Reload and resizable wave/batch/enemy UI, duplication/reordering and production validation. Added native typed `WaveSetResource/WaveResource/WaveBatchResource/WaveEnemyResource` with conversion to existing immutable `WaveDefinition`; no new runtime scheduler or per-frame editor polling. Migrated Level003's existing four waves unchanged to `Level003Waves.tres`, pointed Level003Config.WaveSource to the external resource, kept its C# class only as a reference. Added focused parity/flags/metadata assertions in `WaveChecks`. Other levels remain C# sourced. Needs editor activation, .NET build and F6 smoke check locally. [Guide](../addons/wave_editor/README.md).

---

## 2026-10-27 — CV1 shared square hologram during paid upgrades

Enabled the existing `ConstructionSiteHologram` renderer for timed `TowerWorkPhase.Upgrading` in addition to `Constructing`, reusing the work manager's registration/teardown events and the same soft-wait/bright-working footprint overlay over the existing tower/tier art. Applies to all timed tower upgrades, drone or no-drone. No new scenes, assets, clocks, save schema or combat/price changes; dormant when all jobs finish. Focused `ConstructionDroneChecks` assertions now cover 1→2 and 2→3 waiting, beaming, 35/70% stages, completion cleanup and drone opt-out. Godot/.NET runtime and rendered visuals pending.

---

## 2026-10-27 — CD4 drone-assisted upgrades and two-stage tier artwork

Source change on `dev`: paid nonzero-duration upgrades use the existing capped, pooled Citadel construction drone and pre-registered work hold; the payment callback queues flight, beam deploy releases the reservation, and the ordinary work clock begins. Towers stay `DISABLED: UPGRADING` and the selected HUD now says `AWAITING UPGRADE DRONE → tier` until release. Battle now authors separate 35%/70% in-progress SVGs for Level2 and Level3 through reusable `TowerLevelArtwork.UpgradeStages`, followed by the existing split base/recoil artwork. Per-master `ServiceUpgrades` can opt out; save/restore of held and beaming upgrades uses the existing format. Focused source regression expectations updated. No .NET/Godot builds or desktop visual acceptance performed. [Plan](../TOWERS/Shared/Upgrades/UPGRADE_DISABLE_TIER_ART_PLAN.md).

---

## 2026-10-27 — DU1/DU2 paid-upgrade disabling and Battle tier art pilot

`TowerWorkPhase.Upgrading` now acts as an event-published derived disable reason and owner-physics suspension across tower types, preserving the independently paused work manager, existing HP/occupancy/logistics, latest rank when committing, other disable causes, and save/restore. Selected HUD shows `DISABLED: UPGRADING` even on heatless towers. Existing Battle C4 construction presenter now supports optional finished split base/gun tier textures; shipped 64px Level2 and Level3 upgraded SVG pairs and tier-specific 35%-progress scaffolds in `BattleTower/Artwork/Upgrades/`. Old Level1 art and muzzle/recoil hierarchy unchanged. Focused regression expectations updated; .NET/Godot and desktop visuals **not run** in this environment. [Plan](../TOWERS/Shared/Upgrades/UPGRADE_DISABLE_TIER_ART_PLAN.md). Target `dev`, not `main`.

---

## 2026-10-27 — Level003 template copy and four-wave encounter

Added `LEVELS/Level003/`: an independent **blank** template map (all four TileMapLayers unpainted), local config, four-wave Enemy001/Enemy002/Enemy004-only recipe, and enabled MissionDefinition. 18/22/26/32 totals, Enemy004 arrives wave 3, 1,200 Credits, 1,000 Citadel HP; one template portal and one Citadel remain movable. Registered Level003 after Level002; Next from 002 now points to003, with no next 004. Updated the mission/catalog/result-navigation regression expectations. **The user must paint a connected passable route before enemy spawning is possible.** No engine/build/headless run; static resource/source review only. Developed for `dev`, not `main`.

---

## 2026-10-26 — HD4 expanded weapon heat presets

Five shipped presets now opt into the existing HD1–HD3 heat capability: Machine Gun 2min/10s, Laser 3.5min/10s, Blaster Cannon 4min/12s, Rocket 4min/12s and Shockwave 3min/12s. Shared timing resolution now handles funded damaging beam seconds and actual wave intervals in addition to bullet cadence. Battle stays 3min/9s; Sniper/Cryo/Stasis stay heatless. Type-local Inspector controls and opt-out, private config snapshots, explicit per-level Heat precedence, lazy condition-clock rules and campaign saving are retained. Focused resource/pipeline test assertions added to TowerConditionsChecks; .NET/Godot not executed here, brief static review only.

## 2026-10-26 — Organic builders and stronger credit gains

- Shared builder manager now uses eased cubic outbound/return flight, travel-facing sprites, gentle hover and randomized eased orbit arcs with beam fixed on the site. Existing build/deployment clocks stay authoritative.
- Added bounded Inspector movement controls and per-assignment plain state/PRNG; no navigation or additional per-drone processing. Saves retain live movement; older builder snapshots without Motion remain readable.
- Increased positive top-left credit pulse 1.25× → 1.875× (50%) and .22s → .35s; separate spending settings retain prior pulse behavior. Hidden indicators still sleep.
- Source/diff review only; no .NET/Godot executable here. Local visual/runtime verification pending; no extensive tests/profiling.

## 2026-10-26 — Saving S2/S3

- Added typed v2 between-wave checkpoints and real shared-battlefield capture/restore through dormant CampaignSession.
- Preserves wallet, every Citadel, placed type/cell/tier/XP/HP/priority/work/heat/disables/cooldowns, producer clocks, ammo stock/counters, real cargo/reservations/slots, builder holds/queue/phases/return flights/retry, wave RNG and mission telemetry.
- Restores frozen research and the original mission reward ticket; rejects retired tickets without replaying XP. Profile progression is never restored from a battlefield file.
- Wired Esc Save/Load and profile Campaign Continue. Disabled Save displays **Unavailable during combat**. Live F2 enemies, pending death releases and real projectile leftovers count as combat; remaining debug batches cancel on Save. Profileless play remains available.
- Prepared paused replacement scene keeps the old scene available on failure. Stable catalog IDs, dependency fingerprint and owner/schema validation guard content. Version-one metadata cannot Continue; explicit saving migrates its envelope to v2.
- Updated the focused disposable-directory storage fixture and local smoke steps. Diff/basic source checks only: .NET/Godot unavailable, so build/runtime/visual/export acceptance is pending. No extensive tests/profiling performed.

## 2026-10-26 — Saving S0/S1

- Completed source audit and staged plan in SYSTEMS/Saving/IMPLEMENTATION_PLAN.md.
- Significant S2 requirements: real in-flight cargo/reservations, builder holds/work, frozen deployment research, original reward ticket/stale-checkpoint handling, F2/unowned combat and wave RNG state.
- Added strict typed v1 campaign metadata and explicit one-current-slot-per-profile store; no polling or game callers. Atomic replace/flush/readback, bounded reads, ownership/format validation and read-only backup recovery. Corrupt originals are preserved; replacement/repair requires a future explicit flow.
- Added focused isolated storage fixture. Static/diff review only; dotnet/Godot unavailable here. No broad tests/profiling.
- S2 battlefield schema/capture/restore and S3 interface remain pending. Exact combat explanation: **Unavailable during combat**.

## Live Citadel critical alerts — 2026-10-21 (source implemented; engine QA pending)

Existing major `citadel_critical` warning now fires once on each living Citadel's crossing into the HUD's critical health band (default 25%). Single wording stays unchanged; multi-Citadel warnings identify their own objective and reuse the manager's priorities, per-ID cooldowns and F7 catalog, with no extra per-frame work. Lethal hits defer to mission defeat; reset/unload removes subscriptions. Authored MultiCitadelHudChecks cover threshold, dedupe, identity and queuing; Godot/.NET runs and desktop QA remain pending. See UI/Alerts/README.md and WORLD/OBJECTIVES/Citadel/README.md.

---

## R2 vector debrief — source implemented 2026-10-21

Reworked the existing UI/Menus/Results screen with passive neon vector grid/brackets, cyan victory vs crimson defeat, stat fade/scale entrance, and saved-XP track fill/level-up animation with immediate Skip. R1/R4 state, credits, stats, navigation and persistence are unchanged. Test assertions were added; Godot/.NET build and desktop QA are pending. Old R2 unchecked plan entries are historical; see UI/Menus/Results/README.md.

---

## Completed CD1/CD2 — Citadel construction drone foundation (2026-10-21)

- [x] **CD1:** paid commit atomically holds initial nonzero work; successful-event FIFO dispatch, nearest available Citadel/stable tie, per-job token, specific arrival release, bounded capacity and fallback/strict mode implemented.
- [x] **CD2:** separate pooled cyan builder SVG, direct airborne movement, footprint-aware holographic beam/hover, authoritative work-completion return, cancellation and source-loss handling implemented.
- [ ] **CD3 remaining:** desktop visual/GPU review and further UX/polish. Minimal waiting Inspector label and core automated coverage were included so CD1/CD2 are understandable/testable; no ETA/sounds/hangars/upgrades/saving expansion.

Build/import pass; dedicated ConstructionDroneChecks, FootprintChecks and F4 PerformanceOverlayChecks pass. Held-only clocks sleep; active work/upgrades remain independent. Twenty beams/600 explicit central ticks measured8.576ms total and0 managed bytes on one CPU-only run; drawing/GPU excluded. Existing combined ConstructionChecks still stops at its map-dependent ammo_factory purchase, reproduced on pre-feature619573d with the same test-layer search correction; timer/art groups passed. Full details/settings/limitations: TOWERS/ConstructionDrones/README.md. Earlier planning entries below are retained as history.

## CD1–CD3 — Citadel construction drones (PLANNED, 2026-10-21)

Codex handoff: [TOWERS/ConstructionDrones/README.md](../TOWERS/ConstructionDrones/README.md). On successful paid placement, launch a builder from an available Citadel; construction **does not advance** until arrival and cyan projected-beam activation. During building, hover/orbit near the tower with a bright translucent cone; on completion fly back home. Support multi-Citadel, multiple simultaneous sites, existing stage art, pause, failure/cleanup and a no-deadlock fallback. Does not affect upgrades yet.

- [ ] **CD1:** reusable assignment service + per-job work hold/release, deterministic Citadel selection, fallback and first arrival-gated build.
- [ ] **CD2:** cached drone SVG and shared cyan hologram beam, hover, return flight and cleanup; shared for all paid towers/buildings.
- [ ] **CD3:** Inspector waiting state, config/test performance and comprehensive Godot QA.
- **This commit is documentation only.** Existing construction timing, logic and visuals remain unchanged.

---

## Completed: shield ricochet SR1–SR4 foundation — 2026-10-21

Implemented FEEDBACK/Ricochet handoff with combined world controller/renderer, capped value slots, affine visible-shield reflection, positive absorption bridge, and typed Battle/Machine Gun/Sniper opt-in. Improvement: curve turns toward outward normal so grazing reflections cannot curl inward. No additional damage/ammo/status/XP or per-round nodes. Build and focused RicochetChecks/ProjectileAccuracyChecks/PerformanceOverlayChecks pass. CPU-only stress:9,600 requests/600 ticks/300 enemies present,15.643ms total,40 fixed managed bytes; rendering excluded. Desktop look/GPU comparison still pending. Settings and exact observed boundaries in FEEDBACK/Ricochet/README.md. Historical passes below retained.

## E1/E2 implementation — 2026-10-21

E1 clips both64×64 SVG atlas cells independently, removes the dust's repeated gradient/sediment bands in favour of low-contrast seamless grain, and softens the rock's repeating black strokes. Source IDs, atlas coordinates, TileData flags and saved maps are unchanged.

E2 now has a shared, material-profile-driven renderer. WORLD/World.tscn attaches one TerrainEdges controller/renderer after Grid startup. It classifies painted materials in existing layer precedence, builds deterministic cached L8 silhouette masks, and applies a shared lightweight mask shader to existing TileMapLayers. The opaque square tile pixels are actually cut away. Tiny recorded texture quads on one helper per participating layer fill rounded inward corners around holes. No per-cell nodes, enemy checks, terrain sampling in physics/process callbacks, full-map per-frame image updates, tile repainting, or navigation rebuild requests are added. Towers only change occupancy, so placing them does not regenerate these visual masks.

**Opt-in rollout:** DefaultTerrainEdgeConfig.tres remains Enabled=false for desktop GPU/appearance review, especially given the current unrelated rendering-stall investigation. Set Enabled=true and restart the level to try E2. MarsDustEdge is the initial Soft profile; MarsRockEdge.RenderEdges=false and jagged/material transitions remain E3. To opt another tile material in, create a TerrainEdgeProfile, set a stable MaterialId, Layer, SourceId and AtlasCells, choose Shape=Soft / RenderEdges=true, then add it to the master Profiles array. No renderer code references Mars names. Same-material variants merge; other painted materials protect joins instead of introducing transparent gaps. Straight profiles keep original rendering. EdgeTint and a dedicated Jagged geometry style remain future E3 work.

MaskPixelsPerCell defaults16 (8–32); MaximumMaskMegapixels defaults16 per layer, falling back to original rendering if exceeded. Master disable plus controller.Rebuild (or level restart) restores exact original layer material references and releases masks/fills. Changing profile/settings values requires an explicit rebuild/restart. All generation is on attachment or WorldGrid.TerrainRebuilt; future local hints/chunk invalidation remain deferred. Native rendering still samples the mask each drawn pixel; caching removes CPU generation, not GPU draw cost.

Supported MVP: static64×64 square atlas artwork, zero texture origin, grid-aligned source layers, including transformed worlds and whole-cell source translation. Existing custom layer/tile materials, tint/Z overrides, Y sorting, subcell/rotated/scaled source layers, animated or oversized opted-in atlas tiles are left untouched with a warning. Existing layers retain animation/tint/atlas handling for unprofiled tiles through a nearest-filtered per-cell applicability texture; profiling masks never replace gameplay or collision data.

Validation: C# build/import and TESTS/Terrain/TerrainEdgeRenderingChecks.tscn pass, covering generic second-material opt-in, isolated/connected tiles, corner/hole math, cached deterministic output, transformed source mapping, unsupported-transform fallback, disable/restoration, no navigation/AStar from visual rebuilds, no mask rebuild on tower occupancy, and memory-budget fallback. Runtime-only Level002 fixtures are discarded; no saved level was repainted. A4800-tile fixture generated in32.69ms after interior-row/empty-cell fast paths (earlier unoptimized runs270–307ms) on this host with1,269,837 bytes of masks and one surface; this is a startup CPU result, not desktop FPS. Godot SVG raster/mask outputs and a CPU composite were visually inspected; desktop shader rendering/filtering at real gameplay zoom is still pending. Prior future-plan text below is retained as history and superseded where E1/E2 are described as unimplemented.

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

## Safety-cache churn fix — 2026-10-20

Fixed repeated certification under cache pressure. Previously every active ship queried the bounded shared dictionary every tick, and arbitrary first-entry eviction could repeatedly discard a just-built result. EnemyRoute now retains one current-segment result (safe or uncertain), keyed by segment index, radius, pool identity and invalidation version. Geometry rebuild resets it. This is constant extra storage per route, not an unbounded per-ship route cache. Membership checks still reject off-segment motion; uncertain results still run original swept checks.

Shared entries now use bounded least-recently-used eviction with dictionary/list cleanup on selective invalidation and full clear. Eviction does not invalidate a ship's retained current result: unchanged terrain keeps that result valid. Navigation changes/explicit clear advance the pool version immediately, so local results reconsult shared safety before further movement. Unrelated shared certificates survive, although local memos conservatively reconsult once after any navigation change. Solid-terrain tower occupancy still produces no navigation event. Existing staggered validation/repath budgets and all gameplay settings remain unchanged.

F5 adds cumulative `segmentSafetyEvictions`. Shared hits now count consultations between ships/segments, not every warm movement tick; lower hit growth can be expected. Read counters together with Clearance and Movement CPU timing. Verification: TESTS/Performance/SharedRoutePool.md. This fixes demonstrated cache churn; it does not claim to explain or eliminate the desktop 17-FPS combat hitch.

---

## Cached movement safety — 2026-10-20

Implemented the shared-route follow-up: `WORLD/NAVIGATION/RouteSegmentSafety.cs` caches conservative continuous swept-circle safety certificates for exact segment endpoints and enemy radius. Compatible ships reuse certificates across waves. Warmed lazily on first traversal, not baked each round. Each physics tick still moves/aims accurately, but certified on-segment movement skips repeated wall sampling. Geometry near expanded wall rectangles, deviations/teleports and disabled caching use the original swept check. Strict logical/corridor corner results are cached per ship/segment and navigation revision.

Only walkability changes invalidate safety. Existing WorldGrid occupancy already publishes no navigation event for towers entirely on solid terrain. A passable-tile blocker expires intersecting certificates immediately; unrelated certificates survive placement/removal. Full terrain rebuild/unload clears all. Existing queued route validations and replacement A* remain count/time budgeted; urgent unsafe movement cannot proceed while waiting. Corner memoization resets conservatively on any navigation revision. No combat stats, maps, wave recipes or UI layout changed.

Toggle `EnemyMasterConfig.EnableRouteSafetyCache` under **Shared Route Pool**; defaults true independently of route-template pooling. Storage is bounded to clamped SharedRouteCacheEntries ×32 segment entries; uncertain results are cached too but never authorize movement. F5 metadata adds cumulative segmentSafetyHits/segmentSafetyBuilds/segmentSafetyEntries (not five-second CPU-window counts). See TESTS/Performance/SharedRoutePool.md for verification and limitations.

---

## Shared raw/finished-route cache — 2026-10-20

Implemented level-owned bounded route reuse after PF2 captures identified costly refinement. Reuses8 stable variations by default; global master/variant/cache limits live in EnemyMasterConfig. Original routing remains available with master off. Selective block invalidation, safe actual-origin connectors, independent ship progress and checkpoint branches are retained. Demand-warmed through existing budgets rather than a synchronous startup bake. No map, waves or combat values changed. F5 reports include cumulative cache counters. Evidence and baseline suite limitations: `TESTS/Performance/SharedRoutePool.md`.

## Level100 stress-wave shortcut — 2026-10-20

Removed original waves1–3. Former wave4 is now wave1 (400 ships); former wave5 is now wave2 (500 ships). Retained all batch recipes/timings/weights/shields, including20 gunners per wave;900 scheduled ships/40 gunners total. Existing map painting, portals and mission settings are preserved. Updated Level100 README and quota-test expectations. Historical profiler notes mentioning wave4/5 refer to the old numbering.

## PF2 subsystem profiling + legacy overlay removal — 2026-10-20

Implemented optional world-scoped CPU timings in the existing F4 panel. Shift+F4 toggles capture (default off); F5 dumps mean/p95/peak/calls and metadata to user://Debug. Fixed rolling sample storage, nested-cost labels, sampled-tick stride, world/pause/unload isolation and partial observer cost are included. Removed old top-left grid Info nodes/script while preserving F2/F3/checkpoint controls. See DEBUG/README.md and TESTS/Debug/PF2.md for real validation and remaining target-hardware measurements. PF3/PF4 optimizations are not included.

## PF1 — duplicate movement update source change (2026-10-20; runtime verification pending)

Removed the explicit post-move registry update; existing Enemy transform notifications retain ownership. Added focused movement/query/listener/swept-history/stealth/ancestor/removal checks and a paired callback/timing diagnostic. Execution environment unavailable: build, Godot tests and timings NOT RUN; no measured FPS gain claimed. PF0 remains pending; user explicitly requested proceeding with PF1. PF1 runtime acceptance remains pending; PF2–PF5 are unchanged. Details: `NOTES/AGENT_FINDINGS/Performance.md` and `TESTS/Performance/MovementRegistry.md`. Historical notes below are preserved.

## Profile welcome voice cue (2026-10-20; implementation pushed, playback pending)

- [x] Confirmed user's latest GitHub commit adds binary `AUDIO/VoiceAssistant/Welcome_Commander.mp3`.
- [x] Registered it as `voice_profile_welcome` in `AUDIO/VoiceAssistant/Cues/Profiles/WelcomeCommander.tres` and the existing `AUDIO/DefaultAudioCatalog.tres`, with native CompanionVoice mixing and queue.
- [x] Boot saved-profile selection and successfully created new profiles now trigger the greeting before opening Main Menu, without new audio players; test assertions added in MenuFoundation/MenuTowerFlow suites.
- [x] Documented VoiceAssistant cue organization by Profiles/Waves/Combat/Systems/Missions and retained original MP3 path unchanged.
- [ ] Godot .NET compile/headless menu checks and **actual audible playback** validation; later connect wave's `VoiceCueId` when voice clips exist.

---

## Projectile accuracy: PA1–PA3 implemented (2026-10-20)

Straight launch-time spread, swept first physical enemy contact, fixed-distance splash destinations, level/rank authoring and dormant research handling seams are implemented. See `TOWERS/Shared/Projectiles/PROJECTILE_GUIDANCE_ACCURACY_PLAN.md` and `TESTS/Towers/ProjectileAccuracy.md`. PA4 desktop visual/balance QA remains pending; automated tests and documentation are included with this implementation. Existing notes below are retained.

## NP2 top-right tactical narrator — implemented in source (2026-10-20; test run pending)

- [x] **NP1 (confirmed by user):** standard `WAVE NN INCOMING` major banner on every accepted manual wave start.
- [x] Shared `UI/Narration/` display, settings Resource and single HUD instance: animated RichTextLabel typewriter + short hold/fade, processing only while active, top-right below wave controls and stacked clear of centered alert on small viewports.
- [x] `WavePresentationController` delivers optional text/speaker without touching the alert or scheduler; Level020 waves 1/4/6/7 now author demo messages and future-only voice IDs.
- [x] Basic UI/authoring integration assertions and documentation.
- [ ] Godot/.NET build, integration suite and desktop visual review at 1920×1080 and 960×540.
- [ ] NP3 extended tests and future actual audio/voice, optional skip input, advanced queues/portraits. See [plan](../LEVELS/Waves/Presentation/README.md) and [UI guide](../UI/Narration/README.md).

---

## 10× tower hulls + 5% max HP per purchased level — 2026-10-20 (source updated; runtime verification pending)

- [x] All **14 playable tower types** receive a 10× base HP rebalance: 9 offensive towers `1000/1050/1102.5` (according to level count); factory/depot `1000`; Resource/ARG `1500/1575/1653.75`; Stealth Beacon `1800`. Saved economy tier resources and code defaults agree.
- [x] **5% compounding HP increase per paid upgrade**, expressed as full authored absolute tier snapshots and consumed by the existing eight-stage pipeline. Rank HP bonuses continue at Stage03; upgrades preserve missing HP. No new engine, tower stats, ammo, economy, enemy Minigun damage or Citadel HP changes.
- [x] Regression assertions updated for catalog-wide 10×/tier scaling, live hull/damage/destruction, exact stat snapshots, rank preservation, economy upgrades and visible `Max HP` comparison values.
- [ ] **Godot/.NET build + tests unexecuted here.** Run `TowerHealthChecks`, `EnemyIntegrationChecks` (TowerStat/Upgrade), `CreditTowerChecks` and visually check damaged-tower upgrade/repair/destruction. See [Shared Durability README](../TOWERS/Shared/Durability/README.md) for balances and semantics.

---

## NP1 / simple NP2 slice — automatic numbered wave banners (2026-10-20; runtime verification pending)

- [x] `WavePresentation` optional authoring metadata + backwards-compatible `Wave()` overload and accepted-start-only `WaveManager.WaveStarted` event.
- [x] Battlefield-scoped presentation controller and dynamic major alert overload displaying **WAVE NN INCOMING** through existing alert UI, without modifying actual wave batches or scheduling.
- [x] Regression assertions in the existing `TESTS/Integration/WaveChecks.cs`.
- [ ] **NP2 remainder:** tactical narrator/typewriter display, audio placeholder delivery and Level020 messages on waves 1/4/6/7; NP3 extended UI/regression checks.
- [ ] Godot .NET import/build/headless and desktop visual QA. Source committed only, not yet execution-tested. Plan and implemented details: [wave presentation README](../LEVELS/Waves/Presentation/README.md).

---

## Level100 Five-Gate Swarm stress mission — 2026-10-20 (implemented in source; runtime verification pending)

- [x] New `LEVELS/Level100/` maps all four TileMapLayers empty and includes **five left-side portals** plus one right-side Citadel, for user painting. Mission is selectable from Campaign now, but **does not spawn until you paint navigable terrain** connecting portal(s) to Citadel.
- [x] Dedicated Inspector-configured `Level100Waves` recipe: **five manual waves 100/200/300/400/500 = 1,500 scheduled ships**; each wave exactly **20 `gunner_drone_mk1`** in four separated groups of five; mostly small enemy types with rare bigger/shielded threats. No DeathSpawn carrier ships, preserving scheduled count clarity.
- [x] `StartingCredits=100000`, `CitadelMaxHealth=10000`, existing active-enemy/spawn budgets unchanged; registered in Campaign mission catalog without separate launcher. Updated mission catalog regression and added `TESTS/Integration/Level100StressChecks`.
- [ ] Godot .NET compile, Level100StressChecks and MissionCatalogChecks; paint the intended routes and test all five actual live waves, F4 frame-time and bullet density. See [Level100/README.md](../LEVELS/Level100/README.md).

---

## Gunner handling and energy streak polish — 2026-10-20

Gunner Drone now holds valid tower targets for1s between closest-target reconsiderations; no-target acquisition remains staggered at0.2s. Dead, removed, queued or out-of-range locks clear immediately and cap the next search delay at0.2s. TargetSearchSeconds and LockedTargetSearchSeconds are separate validated, spawn-captured Inspector settings. Pause still freezes both clocks; navigation is unchanged.

Default minigun fire is continuous at6 shots/s while aligned, with no burst/rest gaps. UseBursts defaults false; explicit opt-in preserves the existing BurstSeconds/RestSeconds capability for future weapons. Fractional cooldown remainder is bounded so cadence stays steady without a hitch catch-up volley. Only accepted bullets flash; existing pause, Freeze, target/aim gates, damage and lifetime behavior remain intact.

The dedicated enemy tracer preset now selects a small9px tapered energy streak with1.5px nominal width,1.25 head multiplier,0.08 tail width fraction/opacity and the same coral-red palette/faint glow. A rounded fuller nose ends at the actual bullet position; its thin tail fades behind travel direction. ProjectileVisualStyle captures the optional taper values; EnemyBulletRenderer reuses retained9-vertex/color buffers for glow/body/core passes. Default Tapered=false preserves existing straight tracers/friendly visuals. This remains a travelling, missable Kinetic projectile, not a hitscan beam or damage-type change.

Validation: Godot4.7.2 .NET import and C# build succeeded; EnemyWeaponChecks, EnemyBulletChecks and GunnerDroneChecks passed headless, including one-second target holds, prompt target loss, continuous per-second firing windows, tapered geometry, F2 and real damage. Broader ProjectileVisualChecks passed its weapon/visual sections but failed at the existing Cryo placement fixture; the same failure was reproduced on untouched baseline582eb89. Normal-zoom desktop visual approval and EG4 profiling remain pending. Older burst/0.2s lock/rectangular7px descriptions below are historical and superseded here.

---

## EG3 — medium Gunner Drone / split art / F2 — source implemented (2026-10-20)

Added the14th catalog entry gunner_drone_mk1, inherited GenericEnemy scene, independent definition,64×48 hostile hull SVG and separate24×40 rotating gun SVG with real(0,-18) muzzle. Medium reference confirmed as Enemy004/Bulwark Drone, not Enemy002. Default minigun and dedicated small muted-red tracer/orange-red flash presets are editable resources. F2/F3 discover the appended entry through the existing catalog; authored Level001/002/020 waves and13 prior enemy types are unchanged/unarmed.

New ship provisional settings: Drone family,180HP,0shield,75px/s,22px radius,2 Citadel damage,8 Credits. Gun:260px,6/s,1.5s burst/1s rest,2 Kinetic,1200px/s,2.5s lifetime. Hull follows existing navigation while its gun aims independently; placing the gun branch beneath the hull Sprite carries stealth alpha naturally. Only accepted central shots emit tiny flashes through the existing bounded renderer; disable/reinitialize/removal clears flashes without cancelling world bullets.

Added GunnerDroneChecks, updated fleet count expectation and relevant feature/debug docs. Static source/resource/scene checks only. **Godot import/.NET build, headless/runtime tests, manual1–10-gunner firefights and visual approval remain pending because execution was unavailable.** EG4 performance profiling/multi-gun preparation remains planned. Existing EG1–EG4 checklists below are retained as acceptance history; earlier no-Gunner statements are superseded by this source implementation entry.

---

## EG2 — world-owned hostile bullets / tower hits — source implemented (2026-10-20)

Added EnemyBulletSystem, EnemyBulletData, EnemyBulletRenderer and EnemyProjectileMasterConfig/default preset. World owns one fixed-capacity retained buffer, active-only physics loop and drawing surface. Optional EnemyMinigun now submits actual muzzle-tip straight shots; accepted shots are observed by ShotRequested, with captured lifetime/visual data. Sources/targets are not retained by bullets; source death/removal cannot cancel emitted rounds.

Spatial broadphase reuses EG1's TowerRegistry; shared swept slab clipping finds the first exact full footprint and calls existing Kinetic TowerBase.ApplyDamage once with null DamageSource. Pause, misses, moved/queued/dead towers, lifetime clipping, large deltas, invalid data, overflow, reset/unload and counters are handled. Generic small muted-red fallback presentation is Inspector editable. Offscreen visuals never cancel gameplay. Current enemies/catalog/F2/waves remain unarmed; EG3 adds Gunner Drone and specific tracer/flash art, EG4 measures performance.

Focused EG2 checks are authored; EG1 cadence checks now expect actual central shot slots. Static source/resource/scene checks completed. **Godot/.NET build, headless/runtime tests, desktop visuals and profiling remain pending: execution was unavailable.** Historical unchecked phase plans below remain as acceptance history; source is delivered but not runtime verified. See [projectiles](../ENEMIES/Projectiles/README.md) and [checks](../TESTS/EnemyWeapons/README.md).

---

## EG1 — optional enemy weapons / tower lookup — source implemented (2026-10-20)

Added shared EnemyWeaponConfig/EnemyWeaponMount, EnemyTowerTargeting, EnemyMinigunConfig/EnemyMinigun timing seam, world-owned TowerRegistry and optional tower-root spatial notifications. Full footprints update on membership, relocation and destruction. Closest-center targeting uses staggered spatial queries, deterministic insertion-order ties and cached-lock validity; independent gun heading leaves navigation untouched. Source settings are captured per initialization/config assignment; pause/removal/reuse/invalid config stop or reset private timing safely. Unarmed enemies receive no weapon nodes or callbacks.

EG1 regression fixtures are written, including actual world membership, startup discovery, multi-cell bounds, movement/removal/death, target ties, independent aim, config isolation, pause, lifecycle and burst-request timing. **Godot import, C# build, runtime/headless and desktop verification remain pending: no execution environment was available.** EG1 source is delivered; acceptance checkboxes below stay open until verified.

EG2 hostile bullet simulation/tower hits, EG3 Gunner Drone/F2/small red art presets and EG4 profiling remain planned. Existing enemies/catalog/waves and tower Inspector visuals are unchanged. Original plan below is retained; its blanket “code/tests not yet implemented” statement is historical for EG1. See [implementation notes](../ENEMIES/Weapons/README.md) and [verification](../TESTS/EnemyWeapons/README.md).

---

## NP1–NP3 — per-wave tactical narrator and wave alerts (PLANNED, 2026-10-20)

Codex handoff: [LEVELS/Waves/Presentation/README.md](../LEVELS/Waves/Presentation/README.md). Existing wave definitions and notification manager are ready to extend, but **the actual wave-start banners, narrator typewriter UI and integration are not implemented yet**.

- [ ] **NP1:** optional immutable `WavePresentation` authored beside per-wave enemy batches; backwards-compatible `Wave()` overload and once-only `WaveManager.WaveStarted` event.
- [ ] **NP2:** shared battlefield presentation controller, dynamic banners through the existing major-alert queue, new configurable/pause-aware tactical NarrationDisplay and optional future voice ID; Level020 text only on waves1/4/6/7.
- [ ] **NP3:** compatibility, null-text, timing, interruption, pause/retry/unload and UI layout tests; document real runtime verification.
- No pre-wave countdown, voice playback, combat changes or level composition changes in the first implementation.

---

## EG1–EG4 — Armed Gunner Drone with muted-red minigun bullets (PLANNED, 2026-10-20)

**Codex handoff:** [`ENEMIES/Weapons/README.md`](../ENEMIES/Weapons/README.md). Design approved; code and tests **not yet implemented**. First hostile weapon: a new F2/catalog Gunner Drone, moving along its assigned route while an independent single minigun attacks nearby placed towers with small **7px-long, 1.3px-thick muted coral-red** unguided tracers and tiny orange-red muzzle flash; editable visual/pacing presets. Regular enemies and authored waves remain unchanged.

- [ ] **EG1:** optional enemy weapon mount/config, staggered spatial tower targeting, independently rotating barrel.
- [ ] **EG2:** central fast unguided hostile-bullet state/rendering and swept first-tower impact via existing `TowerBase.ApplyDamage(DamageInfo)`; no per-bullet scene nodes.
- [ ] **EG3:** new Gunner Drone inherited enemy scene, standalone `.tres` and SVG art, red tracer/muzzle presets, F2 catalog registration, manually test tower firefights.
- [ ] **EG4:** profile pooled/batched hostile bullets under realistic large waves; later reuse for multi-gun Cruisers. Preserve enemy routing, stealth, tower death/cleanup, pauses, targeting and performance.
- [ ] Godot compile/headless tests and in-game visual approval of **small slightly red** tracers; document genuine pass results before checking phases complete.

---

## Offensive projectile recoil rollout — source implemented (2026-10-20)

User approved extending the successful Battle recoil preview. Machine Gun, Sniper, Blaster Cannon, Rocket, Cryo and Stasis now have split fixed bases, recoiling gun/muzzle assemblies and independent Inspector-editable presets using the shared controller. Existing composite icons are retained. Laser and nonrotating Shockwave retain their beam/radial presentations; no forced rotation or directional recoil was added to either. No balance, wave, UI/Inspector resize or progression changes.

Updated regression fixtures cover funded origins, fixed base/aim/root, independent emitted flashes/shots, cooldown/ammo gates, generic disable/destruction and adoption boundaries; older muzzle path assertions are updated. Static resource/path checks completed. **Godot import, C# build, headless tests and desktop visual acceptance remain pending** because no execution environment was available. Historical Battle-only R1/R2 entries below remain as pilot history. See TOWERS/Shared/Weapons/Recoil/README.md and TESTS/Towers/TowerRecoil.md.

---

## R1 + R2 — combined Battle recoil source implementation (2026-10-20)

User approved combining both passes: fixed base, split gun and attached recoiling muzzle, with one optional shared controller/style and a Battle-only preset. Source implementation, regression fixtures and documentation are complete; **Godot import/build/tests and normal-zoom visual approval remain pending**, not passed. No combat balance values or other tower scenes were changed. Existing construction art remains an overlay; optional monolithic custom tier/upgrade artwork retains that fallback with split recoil suspended. See [recoil implementation notes](../TOWERS/Shared/Weapons/Recoil/README.md) and [verification instructions](../TESTS/Towers/TowerRecoil.md).

The older “NOT implemented” R1 entry below is retained as planning history and superseded by this combined-pass entry. R1/R2 need runtime validation before acceptance; broader tower rollout remains future work.

---

## Recoil pilot for Battle Tower — Codex plan, NOT implemented (2026-10-20)

The planned reusable, optional projectile recoil system is specified in [TOWERS/Shared/Weapons/Recoil/README.md](../TOWERS/Shared/Weapons/Recoil/README.md). Codex R1: create shared `TowerRecoilController`/`TowerRecoilStyle`, author one Inspector-editable Battle preset, trigger only on an actual funded Battle shot **after muzzle launch/flash**, keep all existing sprite/muzzle/construction paths and other towers untouched, and add regressions. First preview may lightly offset the existing composite Battle SVG; *fixed base + split gun* is an explicit later R2 art pass, not yet authorized. No implementation exists until Codex completes R1.

---

## SE1 — stealth enemies and reveal beacon — implemented 2026-10-20 UTC

Waves may now author `Enemy("basic_mk1", stealth: true)` independently of `shield: true`. Default spawns remain ordinary; no existing wave/map was changed. Stealth ships render at30% alpha, revealed ships at100%, with configurable/dimmed defense feedback that composes with Cryo/Stasis. All aimed towers share detection filtering and cached-lock validation; private TowerConfig.CanDetectStealth opts a specialist in without globally revealing enemies. Existing attacks already emitted, splash and untargeted Shockwave remain able to damage hidden ships (stealth is targeting, not immunity).

New Ctrl / Utility Stealth Beacon:250 Credits,1×1,2s build,180 HP,256px range, one paid tier. It reveals enemies inside range for all towers; overlap, exit/re-entry, disable/reactivation, construction, destruction, relocation, enemy removal/reuse and world teardown are source-owned. One lazy spatial reveal index listens to enemy motion only while beacons are active; no per-frame enemy scans or per-beacon processing. See ENEMIES/Shared/Stealth, FEEDBACK/Stealth, TOWERS/Shared/Detection, TOWERS/Utility/StealthBeacon and TESTS/Stealth READMEs. Existing Inspector layout/resize is retained. Earlier blanket “stealth/detection future” notes below are historical; stealth preference scoring, Radar, tower-grant detection beacons and further content remain separate future work.

---

## Bottom-docked build tray and Inspector — 2026-10-19 (code complete; tests pending)

- [x] UI-only: both panels flush to the bottom of the viewport with existing side margins; tray nominal **260 → 224px**, Inspector **330 → 252px** while retaining the same 560px Inspector width. Panel content may expand the Inspector upward instead of clipping.
- [x] Build catalog still shows two horizontally scrollable rows with 56px tower buttons, slightly smaller padding and preserved category/hover/selection handling.
- [x] Updated responsive layout expectations in `TESTS/UI/PlacementHudChecks.cs`, `HudCategoryChecks.cs`, `TESTS/Towers/TowerUpgradeChecks.cs` and docs in `UI/GameplayHUD/`.
- [ ] Godot/.NET build and tests: PlacementHudChecks, TowerUpgradeChecks, CompactInspectorChecks, HeatInspectorChecks, HudCategoryChecks; visually check 1920×1080, 1366×768 and 960×540, including two rows, category tabs, Inspector dense states and Ctrl transitions.

---

## Level020 setup-error repair — 2026-10-20 UTC

The seven-wave content update had changed Level020Waves back to a static class, breaking the serialized LevelConfig.WaveSource contract from the shared-level pass. Restored [GlobalClass] Resource inheritance (WaveSetSource) and Build() → Create(); all seven wave definitions, map and settings are retained. Recipes assigned in LevelConfig must remain resource classes; preserve the wrapper when editing batches. Added TESTS/Integration/Level020WaveSetupChecks.tscn to load the actual saved resource and launcher, assert seven Ready waves and successfully start wave1. C# build (zero warnings/errors) and this headless regression passed.

---

## Level020 waves expanded to seven (2026-10-19)

- [x] Level020: authored 7-wave Five Gates assault, **52/68/80/72/72/60/58** ships, beginning with small hordes; regular/heavy Transporters in waves4–5; big armoured enemies and a pair of shielded Cruisers in wave7.
- [x] Level020-specific validation covers real catalogue IDs, seven totals, tier introduction, two guaranteed final Cruisers and unrestricted use of the five configured portals.
- [ ] Desktop balance/playtest and headless Godot suite pending. Existing wave scheduler, Level020 layout/settings, and other levels unchanged.
- Documentation: [Level020/README.md](../LEVELS/Level020/README.md).

---

## Lightweight data-driven levels — implemented 2026-10-19

Shared `LEVELS/Runtime/Battlefield.tscn` + `WORLD/World.tscn` replace per-level service copies. Existing Main launchers and World variants are thin compatibility shortcuts; F6 paths stay valid. Each level now has a MissionDefinition resource and the Campaign roster is `LEVELS/Catalog/DefaultMissionCatalog.tres`. Menu number/title/description/map are data, with a bounded scrollable mission list. New missions deploy/retry directly from their resource; no per-level launcher/world or menu/catalog code is required. Next advances only to the immediate enabled number.

LevelConfig directly references an Inspector-assignable C# WaveSetSource; the three existing wave recipes keep identical Create() content and now implement Build(). WaveCatalog's old central switch is removed; string-ID compatibility resolves via the mission roster. Painting, map/config ownership, portal/Citadel placement, credits/HP/XP values and progression identities are preserved. Explicit off-tree preparation supports the existing multi-Citadel fixture. Citadel destination cleanup is now idempotent across objective/world teardown.

Authoring steps: `LEVELS/AUTHORING.md`; ownership: `LEVELS/Catalog/README.md`, `LEVELS/Runtime/README.md`; validation: `TESTS/Integration/LightweightLevels.md`. This completes shared-launcher/catalog convenience, not a visual wave editor, campaign unlock/save system or hot map switching. Earlier duplicated-launcher instructions below are historical.

---

## Visual atmosphere / reusable shader presets — VE1–VE4 (PLANNED, 2026-10-19)

- **Codex design and source review:** [`WORLD/BACKGROUNDS/README.md`](../WORLD/BACKGROUNDS/README.md#proposed-level-atmosphere-and-shader-presets--ve1ve4-planned). **None of VE1–VE4 has been implemented by this documentation job.** Existing levels use the same static `Stars/level001_space.svg`; the `Nebulae`/`Planets` groups are empty. Existing Terrain Edges are a separate disabled scaffold.
- [ ] **VE1:** reusable gentle star-twinkle overlay, slow nebula drift and optional controlled parallax, Inspector-editable `CalmSpace` preset; retain current static background when disabled.
- [ ] **VE2:** shader/preset-backed subtle Portal core/rim movement and Citadel reactor glow; optional spawn/credit/health event pulses that do not change input or gameplay.
- [ ] **VE3:** sparse opt-in animated metal/foundation corridor lighting, correct atlas UV/material isolation, `IndustrialBlue`/`IndustrialAmber` presets. Prioritize Level020 without repainting existing maps.
- [ ] **VE4:** localized faint Mars dust wisps/industrial accents, distinct from the unfinished Terrain Edges E1–E3 mask renderer; keep camera feedback separate.
- [ ] Test atmosphere off/on on Level001/002/020 and template, Godot shader loading, near/far zoom and 500+ enemy performance; one commit per VE implementation job. No per-tile nodes or per-frame full-map rebuilds.

---

## ARG automatic income correction — 2026-10-19

ARG eligible kills now grant Credits directly to the existing wallet: no collection button, stored income or storage limit. The Resource Tower and Citadels retain manual collection. A single mouse-transparent green floating number beside each ARG combines rapid successful payouts (+1 → +2 → +5), then floats/fades away after a quiet interval (default 0.8s). Popup duration/color are editable on ARG config. Higher tiers still earn 2/3 per eligible kill. Full-wallet rejection displays no false income and is not banked. Existing kill eligibility, pause/construction/upgrade gates and ordinary kill rewards/XP remain intact. ARG previews omit unused storage. Earlier buffered-ARG descriptions below are historical and superseded by this correction.

---

## CP2 + CP3 economy towers — implemented 2026-10-19

Both towers are registered in the Ctrl build tray's **Economy** category, with muted dollar-bill greens and distinct original SVGs. They use the existing placement/construction/health/paid-upgrade/Inspector flow. Select one and press **COLLECT … CREDITS** in its Inspector; world clicks still select towers normally. Wallet grants remain centralized, all-or-nothing and once-only. Failed/full-wallet collection leaves storage intact.

| Purchased tier | Resource Tower price | Credits / 5 active seconds | ARG price | Credits / eligible kill | ARG range | Storage (both) | HP (both) |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 350 | 10 | 300 | 1 | 256px | 100 | 150 |
| 2 | 250 | 15 | 250 | 2 | 288px | 150 | 200 |
| 3 | 350 | 20 | 350 | 3 | 320px | 200 | 250 |

Prices for tiers2/3 are upgrade purchases, not cumulative costs. All work takes2 seconds initially. Resource Tower footprint **2×2**, ARG **1×1**. Balance is provisional. Edit each `Default…Config.tres` → **Credit Production / Production Levels** for absolute tier prices, capacity, payout, interval, range, hull and work duration before deployment. The nested tier resources are cloned per placed tower; existing instances retain private snapshots. Tier capacity must not decrease. Identity/art/footprint remain in the standard tower Definition. ActiveWavesOnly defaults true on both; turning it off permits between-wave income while the mission is Playing.

**Resource Tower:** shared CreditProductionTimer runs only while operational, not busy/disabled/paused, mission Playing, and (by default) waves Spawning/SpawnBlocked/WaitingForClear. Fractional progress freezes between waves/while disabled. Full storage accumulates no backlog. Each purchased upgrade preserves stored Credits, increases capacity and starts a fresh periodic clock with the new rate; no retroactive payout.

**ARG:** listens to the authoritative registry's new AttributedKill event, published before ordinary EnemyKilled listeners can finish the wave. Enemy records only the actual lethal DamageSource, not last nearby/last nonlethal attacker. The weak owner token must resolve to a live, nonqueued, nondestroyed Offensive tower in the same world. Range is measured inclusively to that **killing tower's position at kill time**, never the victim. Every eligible overlapping ARG earns independently. Legacy/environmental/unattributed kills and arrivals do not qualify. Already-emitted hits can still belong to a disabled living offensive source; disabled/incomplete/upgrading/destroyed ARGs do not earn. The final wave kill earns normally. Existing global kill Credits, finite damage/kill XP and carrier lifecycles stay separate. The immutable source token also supports future delayed/DoT payloads; no DoT tower was added.

**Shared foundation:** `TOWERS/Economy/Shared/CreditProduction/` adapts editable producer tiers into optional immutable Production snapshots, without pretending producers have a weapon/ammo/rank progression. Inspector upgrade comparison shows storage/payout/interval/source range, rather than projectile rows. CreditProducerTower owns collection/lifecycle/timer; ARG is event-only with no physics tick when periodic income is absent. Resource Tower has one simple gated timer callback. No per-frame tower/enemy scans, extra currency wallet or per-tower UI buttons are created. CreditBuffer gained safe capacity growth for upgrades; CP1 behavior is retained.

**CP1 status:** user reported successful manual playtesting on2026-10-18; automated CitadelCreditChecks and CitadelCollectionChecks also pass in this job. The legacy pending-status sections below are historical.

**Deferred:** Generator.2 passive ARG research (+20/10s) and New Interest Citadel research (+25%) remain planning, not silently purchased/unlocked. No research ownership/effects or campaign save format was changed. CP3's passive tower is implemented; its optional research portion remains future work. Existing maps/waves/combat stats were not edited. See `TESTS/Economy/CreditTowers.md` for validation and limits. Original plans below are retained.

---

## F4 performance overlay + Level002 F2 repair — 2026-10-18

`DEBUG/PerformanceOverlay.cs` is one isolated self-building CanvasLayer script, mounted on all three playable launchers. F4 toggles a debug-build-only panel below top HUD/existing map diagnostics, with throttled frame/engine timing, enemy/projectile-node counts, bounded navigation peak windows/queues and render counters. Click-through, pause-responsive, cached read-only service access; hidden sampling stops. Inspector settings live on the overlay node. See `DEBUG/README.md` for metric meanings and thresholds.

Level002's Debug node and LevelTemplate lacked LevelDebugController/HUD wiring, so F2/F3 were absent there. Both now instance reusable `DEBUG/LevelTools/LevelDebugTools.tscn`; checkpoint-overlay children and map artwork/coordinates remain intact. Missing optional HUD no longer aborts controller setup. Failed catalog F2 attempts warn with SpawnManager.LastMessage while retaining the next enemy index. Raw map-only F6 still intentionally has no world services: use MainLevel002.tscn F6 or Campaign for gameplay spawning.

Verification: C# build and `TESTS/Debug/PerformanceOverlayChecks.tscn`; detailed scope is in `TESTS/Debug/README.md`. Existing user changes to economy, terrain and tower stats are preserved. Earlier plans/history below remain intact.

---

## Battle, Machine Gun and Sniper doubled projectile speed — 2026-10-18 (CODE COMPLETE; runtime test pending)

- [x] Doubled purchased levels 1–3 for all three: Battle **1,150→2,300 px/s**, Machine Gun **1,221.875→2,443.75 px/s**, Sniper **1,495→2,990 px/s**. No other combat stats or shared projectile collision logic changed.
- [x] Updated exact-value regression expectations in `TESTS/Towers/TowerStatChecks.cs` and `MultiBarrelTowerChecks.cs`, plus the offensive tower READMEs.
- [ ] Run Godot .NET compilation and headless `TESTS/Integration/EnemyIntegrationChecks.tscn`, `TESTS/Towers/ProjectileVisualChecks.tscn` (and usual gameplay checks). These checks were not run by the GitHub editing environment.

---

## Terrain Edges visual system — scaffold complete; Codex rendering pending (2026-10-18)

- [x] Add `WORLD/TERRAIN/Edges/` material/config resources, default-disabled controller/renderer hooks, pure neighbour masks, source2 Mars profiles and integration test foundation.
- [ ] **E1:** Correct Mars atlas cross-cell rock overflow, dark vertical slit artifacts and repeating dirt bands (art only).
- [ ] **E2:** Implement actual masked/organic Mars Dust outer and inner corners, one world-scoped opt-in cached renderer and visual/performance tests. Original opaque square tile pixels **must be masked or replaced**, not merely outlined.
- [ ] **E3:** Extend jagged rock, metal and mixed-material transitions; add variants without changing gameplay metadata or established source IDs.
- Codex specification: [Terrain Edges implementation plan](../WORLD/TERRAIN/Edges/IMPLEMENTATION_PLAN.md). Do not mark E1–E3 complete until tested in Godot.

---

## Shared Credits production + Citadel collection + ARG — CP1–CP3 (PLANNED, Codex)

### CP1 input/UI addition — 2026-10-18 (PLANNED, Codex)

- [ ] Actual **world Citadel click** and **its matching top-centre health HUD cell click** both collect **that Citadel's** stored Credits via one shared credit-buffer / wallet command.
- [ ] Add a **small green Credit bar/amount inside each current health HUD cell** (keep existing one-row responsive multi-Citadel health panels); bind to per-Citadel buffer change events. Both world and HUD representations update immediately.
- [ ] Multiple Citadels: each buffer independently collects; no collecting others, no double payout, no world placement/selection conflicts. Collected Credits may be claimed between waves although only generated during active waves. Tests for one/two/four Citadels, wallet cap, paused wave, empty bar and responsive HUD.
- [ ] Detailed acceptance criteria: [`ECONOMY/Production/README.md`](../ECONOMY/Production/README.md#cp1-interaction-decision--collect-from-world-citadel-or-its-top-centre-hud-cell-2026-10-18-planned). **Do not mark implemented yet.**



- **Full verified-source specification:** [`ECONOMY/Production/README.md`](../ECONOMY/Production/README.md). Existing `EconomyManager.Grant` / HUD rewards must remain authoritative. This is **not implemented**.
- [x] **CP1A (backend code pushed; runtime verification pending):** reusable plain CreditBuffer/production timer; independent Citadel 80-cap / +10 every 2s, wave-active-only generation, pause and destruction gating, collection API through the authoritative wallet and test runner authored.
- [x] **CP1B (code committed; runtime verification pending):** world Citadel click + matching top-centre independent HUD cell, green buffer indicators in world/health HUD, unified collection action, input placement precedence and UI test runner authored.
- [ ] **CP1B validation:** compile/run TESTS/UI/CitadelCollectionChecks.tscn and existing MultiCitadelHudChecks + PlacementHudChecks + CP1A runner; inspect at desktop 1920/1366/960.
- [ ] **CP1A validation:** compile Godot .NET and run TESTS/Economy/CitadelCreditChecks.tscn; record actual results.
- [ ] **CP2** `TOWERS/Economy/AdaptableResourceGenerator/`, gains from real kills credited to other towers inside its radius (prefer event-based attribution, no frame scans), buffered reward + Inspector collection, adjustable balance/limits and no duplicate kill payout.
- [ ] **CP3 (optional later)** integrate planned passive Resource Tower and research modifiers (`Generator.2`, `New Interest`) into the same helper, not separate wallet code. Confirm original old notes vs runtime values before balance work.
- [ ] Add tests for phase-specific UI, multi-Citadel independence, wave pause/clear, kill attribution/overlapping ARGs and existing economy regressions. One commit per implementation job.

---

## Level002 tutorial waves — 2026-10-18 (implemented)

The newly painted Level002 is now playable through **Campaign → Levels → PLAY LEVEL 002**, `MainLevel002.tscn` (F6), and Level001's Next Level result action. Its dedicated `WORLD/WorldLevel002.tscn` mounts the existing user-authored map unchanged. Level020 remains separately playable and unchanged. Level002 Next stays disabled because there is no playable Level003; it never skips to Level020.

`LEVELS/Level002/Level002Waves.cs` defines four modest, manually started waves. Enemy “1–2–3” means the three introductory plain roles: Standard Drone (`basic_mk1`), Swift Interceptor (`scout_mk1`) and Bulwark Drone (`medium_mk1`, asset Enemy004). There is no Enemy003 scene. No shields, support ships, carriers, bosses or extra mechanics are introduced.

| Wave | Standard | Swift | Bulwark | Total | Introductory purpose |
| --- | ---: | ---: | ---: | ---: | --- |
| 1 | 12 | 0 | 0 | 12 | Baseline shooting/building |
| 2 | 10 | 6 | 0 | 16 | Fast, fragile targets |
| 3 | 12 | 4 | 4 | 20 | Slower, tougher targets |
| 4 | 16 | 4 | 4 | 24 | Light mixed finale |

Spawn intervals are 0.65–1.4 seconds, with 3-second gaps between batches. Wave4 uses exact weighted composition; all other batches use one type. Existing Start/Next Wave controls allow unlimited preparation time. Local settings: **1,000 starting Credits**, **1,000 Citadel HP**, existing provisional **500 victory XP / 0 defeat XP**. Both authored portals (`EnemyPortal1`, `EnemyPortal2`) are permitted; map-edge spawning is replaced by portal mode for this level. Counts/timings remain C#-authored for AI-assisted tuning as requested. No map tile, Portal/Citadel coordinate, shared scheduler or enemy/tower balance changes.

C# build and `Level002TutorialChecks.tscn` passed: actual map/scheduler spawned all 72 unshielded enemies and produced victory only after wave4. `ResultsChecks.tscn` passed actual Level001→Level002 navigation, retry, menu and missing-next behavior. The isolated tutorial test kills enemies to verify scheduling/ownership; desktop tower balance/playfeel remains provisional. Earlier unplayable-template notes below are historical and superseded by this section.

---

## Level002 moved to Level020; fresh Level002 design map — 2026-10-18

The Five Gates is now `LEVELS/Level020/Level020.tscn`, with `Level020Config.tres`, `Level020Waves.cs` and WaveSetId `level020`. Its terrain, object layout, wave composition and balance are preserved. Its standalone launcher/world variant are `MainLevel020.tscn` / `WORLD/WorldLevel020.tscn`; Campaign labels/launches Level020 and PlayableLevelCatalog registers numeric ID20. Scene/script UID sidecars are retained across renames. Tests and current documentation reference the moved map.

Fresh `LEVELS/Level002/Level002.tscn` is copied from the current LevelTemplate with a new root name and independent `Level002Config.tres` copied from DefaultLevelConfig. Empty terrain layers and template background/Portal/Citadel/checkpoint overlay are retained. The template itself is unchanged. Open this scene in Godot's 2D editor to design it. It has no authored waves or playable launcher yet and is not offered as a finished Campaign mission. Add those when the map is ready. Results cannot skip the missing playable Level002 to reach Level020; Level001's Next is disabled for now. Level020 remains available directly from Campaign and F6 on MainLevel020.

Earlier Level002/Five Gates references in historical notes below refer to the map now called Level020. No old design plan or map content was removed. Verification: see `TESTS/Integration/LevelReorganization.md`.

---

## Portal/Citadel editor grid snapping — 2026-10-18 (implemented)

Portal and Citadel remain reusable scenes. Shared `[Tool] WorldObjectFootprint` now snaps their roots to the enclosing map's `Terrain/Passable` cell centers when entering the editor tree or moving/duplicating them. Default tiles give centers `32 + 64*n`. It uses the actual TileMapLayer conversion, including tile size and transforms, rather than hardcoding 64px or enabling the editor's global snap. The Citadel root still anchors the upper-left cell of its 3×3 footprint, not its visual center.

**Usage:** build C# once, open a Level scene and drag/duplicate a Portal or Citadel normally. Leave **Editor Placement → Snap To Grid In Editor** enabled (default). Disable per instance for deliberate free placement; enabling it again snaps immediately. Existing off-grid instances align when opened in the editor; save the level to retain the corrected positions. No authored map coordinates or tile cells were rewritten by this implementation. Standalone Portal/Citadel scenes have no enclosing map and keep their original local origin.

Only the root position changes. SpawnExit/ApproachPoints local offsets, footprint size, portal SizeMultiplier, route links, rotation and scale are untouched. No snapping executes during gameplay and no process/physics loop is added; editor transform events are coalesced into deferred corrections. A custom map must expose `Terrain/Passable` with a valid TileSet under an ancestor, matching the existing map structure. Moving a root without such a layer leaves it free rather than assuming an incorrect grid.

Verification: C# build, headless actual-tool editor and runtime checks cover both real scenes, negative coordinates, opt-out/re-enable, 80px tiles, transformed parents/maps, duplication, retained child offsets and standalone origins. See `TESTS/World/EditorGridSnap.md`. Existing planning/history below is retained.

---

## Fleet expansion and enemy families — 2026-10-18 (implemented)

Six new inherited enemy types bring the catalog to **13**. Each has independent SVG, Definition, stable Mk.1 ID and editable balance. Existing seven IDs/stats and authored Level001/Level002 waves are preserved. F2 cycles all 13; F3 picks up the same catalog. The previous seven-ship artwork sheet remains a historical preview of that art pass.

| New ship / ID | Family | Hull | Shield capacity | Speed px/s | Citadel damage | Kill Credits |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Reinforced Drone / `reinforced_drone_mk1` | Drone | 110 | 20 | 100 | 2 | 4 |
| Racer / `racer_mk1` | Interceptor | 20 | 0 | 230 | 1 | 3 |
| Shield Frigate / `shield_frigate_mk1` | ShieldShip | 120 | 350 | 65 | 3 | 9 |
| Heavy Bulwark / `heavy_bulwark_mk1` | Bulwark | 500 | 0 | 50 | 4 | 12 |
| Cruiser / `cruiser_mk1` | CapitalShip | 3000 | 400 | 30 | 12 | 35 |
| Heavy Transporter / `heavy_transporter_mk1` | Carrier | 1400 | 0 | 35 | 8 | 25 |

**Family authoring:** select any enemy `.tres` → **Identity / Family**. `ENEMIES/Shared/EnemyFamily.cs` provides the fixed-value Inspector enum: Drone, Interceptor, Bulwark, ShieldShip, Carrier, Support, CapitalShip, Assault. Append future values; do not renumber existing serialized values. Original types are explicitly assigned: Standard→Drone, Swift→Interceptor, Bulwark→Bulwark, Big→CapitalShip, Elite→Assault, Shield Generator→Support, Transporter→Carrier. Family describes role only, adds no ticking/automatic combat bonuses and does not determine Mk generation, size, spawn order or a future Common/Champion/Elite/Boss threat class. `Elite mk.1` remains a name rather than a threat-class system. Missing family on an old/custom resource defaults to Drone for compatibility; invalid enum values reject during catalog validation.

**Shields:** capacity remains opt-in for every type. Author `Enemy("shield_frigate_mk1", shield: true)` for the intended shield-heavy threat; plain/F2 spawns are unshielded. No passive regeneration or new shield mechanic. Heavy Transporter uses two existing DeathSpawn components: six Standard Drones plus four Swift Interceptors, staggered by independent 0.16s jobs under the shared per-frame queue budget. Both reserve wave ownership before parent removal and inherit destination/checkpoints. No payload on arrival/unload and no living launches. Configs are local to its folder; shared Transporter defaults are unchanged.

**Large hulls:** Cruiser art 168×96; Heavy Transporter 144×100; both retain 24px navigation radius. Art extends beyond clearance, as with Big mk.1. Hull-shaped routing/large-footprint clearance and tight-corridor appearance remain future work. No immunity or weapon behavior is implied by size/family.

**Follow-up planning:** balance and schedule appearances in mission waves; add Mk.2+ as deliberate per-type definitions/art; design boss abilities and threat classes separately. These are deferred, not completed by assigning Family. Existing future plans below remain intact. Verification is recorded in `TESTS/Enemies/FleetExpansion.md`.

---

## Alien robotic fleet artwork — 2026-10-18

Replaced the seven existing enemy SVG hulls with original vector interpretations of the supplied dark alien-spacecraft references. Emphasis is on silhouette and exposed machinery rather than repeating one arrow shape with different colors: forked strike drone, tiny hooked interceptor, broad tusked assault cruiser, large four-lance warship, curved talon-wing Elite, bulbous armored Transporter and rounded split-fork Shield Generator. Dark layered metal, restrained edge highlights and recessed reactors keep them readable against space. Reference screenshot borders/backgrounds are not part of the artwork; uploaded bitmaps are not embedded.

All SVG file paths, native width/height/viewBox and right-facing (+X) convention are retained. No scene transforms, definitions, IDs, collision/navigation radii, health, shields, movement, rewards, abilities, waves or shared shield/status visuals changed. The Shield Generator's circular center is physical reactor/projector machinery, not a baked shield aura. Small hulls remain small; the Transporter retains its existing 112×72 canvas and Big mk.1 its 112×120 canvas. Gradients and layered plates are rasterized at asset import; no blur filters, shaders, extra nodes or per-frame effect processing were added.

Validation: all seven assets parsed and rasterized successfully with Godot 4.7.2's native SVG renderer at their exact original dimensions; reviewed at enlarged and native-relative scale. No C# changes, so no unrelated gameplay suite/build was needed. Desktop in-game style/readability is still subjective and can be tuned after review. The review sheet enlarges each hull independently and lists native dimensions; it is documentation, not a gameplay texture reference. See `ENEMIES/Types/ARTWORK_PREVIEW.png` and `ENEMIES/Types/ART_DIRECTION.md`. This visual update supersedes earlier placeholder/color descriptions while retaining their historical gameplay notes.

---

## MC3 HUD + F3 — implemented 2026-10-18

Multiple Citadels now share **one horizontal row** in the top-center HUD: equal-width cells for two, four or more objectives, never stacked vertically. A one-Citadel level keeps the original nodes/layout/SetCitadelHealth behavior. Each multi meter shows its stable CITADEL number, independent current/max HP, threshold-colored fill and damage-only animation/flash. Zero values and maximum resets are supported; rebind/unload disconnect model events and stop old tweens. The full Citadel number/HP is available in each cell's tooltip. No HUD health polling or gameplay authority is introduced.

UIMasterConfig exposes MultiCitadelBlockWidth (560 default) and CitadelCellGap (6), alongside existing bar height/palette/timing settings. The center remains viewport-centered and capped to the existing responsive center zone (46% of width); Credits and Wave retain separate side zones. Short cells truncate text rather than wrap into extra rows. Very large counts/HP values can require the tooltip for full text. Existing Inspector/build tray geometry is unchanged.

F3 now passes each portal's **actual cached live Citadel identity** into RoutePreviewJob. Raw/refined paths, neutral comparisons and required checkpoint chains end at that assigned objective, not the primary Citadel. Legend/source labels show portal → CITADEL N; destination approach markers and one label per objective use the same stable identity colors/numbers as the HUD. ALL PORTALS keeps the existing shared preview query/time budget and private profile pool. Missing assignments appear as UNASSIGNED diagnostics with no invented fallback route. All destination goals/availability and cached assignments participate in invalidation, including secondary objectives. Closing/Clear releases private preview resources only.

Explicit TerrainRebuilt now refreshes source assignments during the authoring rebuild itself, so F3 reads current cached ownership without triggering a live assignment calculation. Ordinary tower occupancy still cannot reassign portals, and preview settings/simulation never spawn enemies or alter live route queues/profile pools/random streams.

Validation: compiled with the installed .NET SDK/MSBuild; MultiCitadelHudChecks passed one/two/four objective presentation at 1920×1080, 1366×768 and 960×540, independent HP/colors/zero/maximum reset, rebind, actual assigned F3 endpoints, secondary-goal invalidation and live-state isolation. Existing RoutePreviewChecks (both maps/raw/refined/checkpoints/50 samples/500 enemies), MultiCitadelChecks, CheckpointChecks, ResultsChecks, MissionXPChecks, PlacementHudChecks and CompactInspectorChecks passed. See TESTS/UI/MultiCitadelHud.md. Desktop rendered appearance/playfeel still needs user review; headless geometry checks are not a screenshot or full-suite/FPS claim. Existing gameplay maps/waves were not changed. This amendment supersedes earlier MC3-pending/primary-only-preview statements without deleting the original pass plan.

---

## MC1 + MC2 — implemented 2026-10-18; MC3 HUD/F3 now implemented; desktop review pending

Multiple independently damageable Citadels now have stable session destination objects. LevelConfig exposes ordered CitadelPaths; an empty array preserves CitadelPath. Actual additional Health/ApproachPoints Citadels are discovered in authored child order so a second objective is not silently ignored. WorldGrid reserves all physical footprints/approaches. All objectives currently share the level's authored CitadelMaxHealth, but own separate health state; the existing deployment research percentage initializes each one. Changing/adding the Citadel roster requires restarting the level. Geometry/terrain rebuilds can refresh goals and assignments for future spawns; already spawned enemies and released children keep their original identities. Removing an assigned objective retires it: enemies stop, stale distance scores become unavailable, and there is no redirect to another objective.

**Single-Citadel fast path:** assign the sole objective directly, with zero destination-comparison A*. Ordinary non-checkpoint placement retains its original single scratch flood. No per-enemy objective searches, per-frame goal scans, per-Citadel AStar grids or extra movement nodes. Available/retired state is cached through objective lifecycle events, not native-object queries in every scoring/movement call.

**Multi-Citadel policy:** PortalCitadelAssignments compares neutral navigable physical route lengths once at setup/explicit terrain rebuild, with authored-order ties. Selection uses cell-center clearance (radius zero); each actual source/radius is additionally cached and validated against clearance-aware connectivity on navigation revisions. Unsupported or blocked ships fail/retry, never select another objective. MapEdges is disabled on multi-Citadel levels; Both uses authored portals only and reports that edge sources are disabled. Single-Citadel edge/Both behavior is retained.

**Required checkpoints:** assignment minimizes the worst branch's neutral journey using deterministic, reachable canonical checkpoint representatives (first valid cached target in the source component that reaches an assigned approach). This is the documented discrete selection policy, not a claim of globally optimizing continuous circle-entry geometry. Every captured branch must remain connected. Runtime branch choices/random streams and varied routes are unchanged. Journey tail caches include destination identity; normal path requests/repaths use the assigned approach goals under the existing combined query/time budgets. DeathSpawn always inherits Citadel identity, including when InheritCheckpointProgress is false.

**Placement/outcomes:** click-time validation shares component maps per radius, protects each usable portal's assigned goal, all remaining mandatory checkpoint branches and live enemy clearance/remaining obligations. It cannot accept a different reachable Citadel as a substitute. Losing any objective emits a single session event into the existing LevelSessionController defeat/Results/mission-XP lifecycle. Results uses the failed objective's HP, never an aggregate pool that hides failure. Existing HUD still shows the primary Citadel; independent multi-objective HUD/F3 destination labels and desktop visual verification are MC3 work. Existing Level001/Level002 maps, waves and Inspector layouts were not changed.

See TESTS/Navigation/MultiCitadel.md for executed checks, measured setup/batch behavior and limitations. Original MC1–MC3 instructions below are retained; this status supersedes their earlier NOT IMPLEMENTED wording.

---

## Multiple Citadels per level — MC1/MC2 and MC3 HUD/F3 implemented; desktop review pending

**Detailed implementation specification:** [`WORLD/OBJECTIVES/Citadel/README.md`](../WORLD/OBJECTIVES/Citadel/README.md#planned--mc1mc3-multiple-citadels-with-fixed-portal-destinations). **MC1/MC2 and MC3 HUD/F3 implemented with headless integration tests. Desktop visual review remains pending.**

- [x] **MC1:** Add multiple per-level Citadel goals/HP and cached neutral **shortest navigable path** assignments **per portal → one Citadel**, resolved at level setup. Enemy destination is fixed at spawn; no per-enemy Citadel selection, no per-Citadel AStar grid, no target switching on blockage. Existing single-Citadel levels must stay compatible. Decide and test checkpoint-branch, size-clearance and map-edge policies; Transporter children inherit target.
- [x] **MC2:** Extend path/repath/checkpoint and tower-placement connectivity to the **assigned** Citadel; keep corridor diversity/offsets and existing First/Last = smallest/largest `RemainingJourneyDistance` to **each enemy's own target** across Citadels. One destroyed Citadel triggers single mission defeat in initial design.
- [x] **MC3 HUD/F3 implementation:** Independent Citadel HP/HUD, F3 portal→Citadel display, new two-Citadel test fixture, regression coverage (single Citadel, portals/checkpoints/DeathSpawn, building, waves, First/Last, Results/research, 500+ enemies) and Godot headless/F5 verification. Do not change current maps/waves until authored separately.

---

## Stasis ST1 — implemented 2026-10-18

Green-themed Stasis is now a catalog-registered Offensive Energy projectile tower. It uses the existing shared muzzle/projectile, construction, physical ammo, hull HP, purchased-level, earned-rank, stat pipeline and Inspector paths. No Inspector layout changes. Targets must be unfrozen and Freeze-nonimmune; all six selected modes score only those eligible candidates. If none qualify, wait with no damage-only fallback. Cryo-slowed enemies remain eligible. Existing in-flight shots can refresh Freeze with maximum remaining/new duration, never additive duration.

Freeze is separate from Slow and damage type. A lazy registry-owned TimedStatusScheduler processes only active timed enemies using retained list storage, respects pause, unregisters on death/arrival/exit/reinitialization and sleeps when empty. Cached movement is zero while frozen; expiration restores the retained strongest Slow × base speed. No route reset, new A* request, per-enemy timer or permanent-status ticking. A shared tint resolver composes cyan Slow/green Freeze; effects survive source tower removal without retaining its reference. Status application grants no XP; real Energy damage/kill attribution follows existing projectiles.

Provisional level tuning: build350 / upgrade225 / upgrade300 Credits; damage10/11/12; rate0.20/0.21/0.22 shots/s; Freeze3/3.25/3.5 seconds; range239/248/258px; projectile650px/s; local ammunition600, one per shot; hull100. Edit `TOWERS/Offensive/StasisTower/StasisTowerLevels.cs`. Green SVG tower/icon, tracer and muzzle presets are independent from Cryo. This is a test balance, not final progression tuning.

This entry supersedes older statements that Stasis/timed scheduling are deferred or must not be implemented yet; historical Cryo plans below are retained. DoT/EMP/stealth and research modifiers remain future work. See `TESTS/Towers/StasisTower.md` for validation and limits.

---

## Laser shield preference — implemented 2026-10-18

Laser configs now expose **Targeting → TargetPreference**, default **Shielded** (None opts out). This preference is separate from First/Last/Closest/Farthest/Strongest/Weakest: choose eligible in-range enemies with **CurrentShield > 0** first, apply the chosen mode within that group, and fall back to ordinary eligible targets when no preferred candidate qualifies. First/Last retain full remaining-journey distance across portals; Strongest/Weakest retain current hull + shield scoring and stable registration-order ties.

TowerTargeting.Find accepts an optional preference after its existing required-effect argument. One existing spatial traversal ranks preference tiers before ordinary scores; no second scan/list allocation, new per-frame loop or tower-local pathfinding. Other towers default to None. Eligibility/range/finite-route checks still run before preferences; Cryo's required-effect filter is unchanged. Laser reacquires on its existing staggered interval, including after shield depletion. Damage/beam/ammo/heat/rank values are unchanged.

Future stealth preference can add a real matching rule here once stealth/detection exists; it must not bypass detection/eligibility. No speculative stealth flag, detector or UI button is implemented now. The latest HP/ammo Inspector bars and compact-rank visual commit is preserved.

Validation: .NET SDK build (zero warnings/errors); ShieldTargetingChecks passed, including real Laser acquisition/opt-out/config isolation, six-mode preference/fallback, actual two-portal First/Last, shared route-query budget/RNG invariants and 500-enemy route sharing. PlacementHudChecks and CompactInspectorChecks also passed against the latest HP/ammo-bar Inspector revision. See TESTS/Towers/ShieldTargeting.md.

---

## Elite mk.1 plain heavy interceptor — 2026-10-17

- [x] Added `Types/Enemy006` (`elite_mk1`) as a new inherited plain enemy, 80×60 SVG, 1,000 hull HP, 250 optional shields, 100px/s, 24px radius, 5 Citadel damage and 10 Credits.
- [x] Registered in the default catalog after Big mk.1 for F2/F3/waves, with no changes to Level001/Level002 waves, shared enemies, navigation, abilities or combat.
- [x] Updated catalog integration checks and enemy documentation in the same commit.
- [ ] Godot .NET compilation/headless catalog suite and in-game silhouette, shield FX and F2 route test pending.

---

## Stable two-row build browser — 2026-10-17

- Ctrl opening now clears the old tower selection and shows the **empty Inspector shell immediately**. Hover/focus fills the same reserved panel with existing build details/cost/affordability; no tile can be displaced by the panel appearing.
- Tray width **always reserves Inspector width plus the existing gutter**, including while details are hidden. The default 560×330 Inspector scene is unchanged. The tray uses two rows of catalog tiles; further columns scroll horizontally. Category filters recompute columns from visible entries and reset horizontal scroll; keyboard focus follows off-screen tiles.
- The shared BottomBarHeight default is now 260px to fit header, tabs, two 65px tile rows and the horizontal scrollbar. Width remains responsive in UIMasterConfig/GameplayHUD. A compact-width header hides its hint when needed.
- Affordable choice still hides the lower HUD while placing. RMB/Escape/success restore the tray plus blank shell; Ctrl closes. Successful placement remains unselected. IsInspectionVisible continues to mean **active details**, not merely the empty shell, preventing stale tower events from refilling it.
- Verification: SDK build, PlacementHudChecks (both maps plus real Rocket mouse hit/click at 1920×1080,1366×768,960×540; blank shell/stable panel bounds; 40 entries/two-row horizontal overflow/category reflow) CompactInspectorChecks and HudCategoryChecks (including the dynamic 40-type tray) passed. Desktop visual review is still pending.

This amendment supersedes older statements that cancellation must hide the entire Inspector panel. No shield/combat/research/scoring/Inspector-art changes are included.

---

## P3 Campaign hub / Levels split — implemented 2026-10-17

- Campaign now shows the selected profile's saved player level, XP and Research Points, followed by distinct **LEVELS** and **RESEARCH** destinations.
- **LEVELS** opens UI/Menus/Campaign/Levels/LevelSelectionMenu.cs/.tscn with the existing Level001 and Level002 mission descriptions/launch buttons. Back returns to Campaign. Research continues to return to Campaign; Results already returns to this hub after mission rewards.
- Existing MenuLayout, UIButtonFactory, profile guards and MenuNavigation are reused. No Research access was added to Results/gameplay. Both missions remain available; mission unlocking, completion stars, campaign saves/slots and Continue/Load remain separate future work.
- This completes the remaining **P3 menu organization slice**, alongside the previously implemented progression display. It does not complete all P1–P5/R2/R4 scoring or polish.
- Verification: SDK build succeeded with zero warnings/errors. MenuTowerFlowChecks, ResearchMenuChecks and ResultsChecks passed; viewport checks passed at 1920×1080, 1366×768 and 960×540. The broader controls runner hit a native Godot finalizer/resource crash; no full-suite pass is claimed. Details are recorded in TESTS/UI/CampaignHub.md. Desktop visual review remains for the user.

Older menu descriptions below are retained as history and superseded by this amendment.

---

## Mission XP connection — implemented 2026-10-17

User approved a provisional, tunable mission-to-profile XP loop before finalized scoring. **This amendment supersedes older statements that missions never grant XP; preserve the original P1–P5/R4 design below as future scope.**

- **Delivered remaining P1/P2/R4 foundation:** each level exposes VictoryBaseXP / DefeatBaseXP (500 / 0 initially); deterministic itemized scoring; captured deployment profile/settings; persisted once-only mission numbers/receipt; atomic XP + level-up RP + award marker; honest failed-save UI and retry. v1/v2 progression migrates to v3 without resetting XP, research, RP or stored curve.
- **Delivered Results/Campaign loop:** saved XP itemization, actual level changes/RP and final within-level bar; show/Skip/reopen never grants. RETURN TO CAMPAIGN opens the existing hub. Research remains Campaign-only. Profileless F6/preview says NOT SAVED.
- **Intentionally deferred original scoring:** first-clear/replay history, replay reduction/anti-farming policy, ETA/time modifiers, kill/tower/performance bonuses and deductions. Replays currently earn the same baseline; signed line-item data is ready for later reward deductions floored at zero, without removing earned levels.
- **Still open:** separate P3 Levels area, mission unlock progression, detailed XP count-up/sounds/R2 polish, Credits telemetry, campaign/battlefield Save–Load/slots and full P5 regressions. Do not mark the combined P1–P5/R4 long-term brief fully complete.
- **Verification:** .NET SDK build (0 warnings/errors), MissionXPChecks, PlayerProgressionChecks, ResultsChecks, ResearchFoundationChecks, ResearchMenuChecks and BattleResearchChecks passed. Results-frame geometry verified at 1920×1080, 1366×768 and 960×540; desktop visual review remains. Existing unrelated historical full-suite blockers remain recorded below.
- **Failure boundary:** leaving a mission whose XP could not save does not persist a recoverable pending battlefield/reward; use RETRY XP SAVE before leaving. Monotonic watermarks assume the existing single active battlefield/application flow.

Authoring/ownership: SYSTEMS/Progression/Missions/README.md. Validation: TESTS/Progression/MissionXP.md.

---

## Tower HP / shared DefenseBar foundation — implemented 2026-10-17

- [x] **All registered towers/structures have hull:** provisional 100 HP, offensive per-level MaxHealth through the existing eight-stage pipeline, active total rank HpBonus, passive Factory/Depot Inspector health setting. No final HP balance implied.
- [x] **Damage and lifecycle:** shared typed DamageResolver, explicit capped repair API, no regeneration; promotions/upgrades preserve missing HP. Zero HP stops operation, cancels work and uses existing removal cleanup for whole-footprint occupancy/navigation, selected Inspector and logistics. No refund/reward.
- [x] **Same DefenseBars system:** existing event-driven scene/style attached to fixed root, visible when damaged, footprint clearance, no new polling/renderer. Live compact Inspector HP, rank HP bonus and changed upgrade HP comparison. Inspector layout resources/dimensions preserved.
- [x] **Targeted verification:** full C# compile with Godot source generators; TowerHealthChecks (all nine types, HP safety/ranks/paid tiers, damage/repair, pause, weapon/passive destruction and whole-footprint cleanup), HeatInspectorChecks, CompactInspectorChecks, PlacementHudChecks (both maps), ConstructionChecks and BattleResearchChecks passed. Heat/MAX/busy Inspector geometry remains 560×330 at 1920×1080, 1366×768 and 960×540.
- [ ] **Broader verification / desktop:** desktop visuals remain unreviewed. Broader runners hit existing assumptions outside this hull change: EnemyIntegrationChecks stops at “Unavailable registration cannot be built” after the placement-flow changes; TowerStatChecks expects every neutral-stage snapshot identical despite Stage07 now materializing heat; standalone TowerRankChecks reaches a stale travelling-wave falloff expectation (2.291304 vs 2.125) after the range rebalance. No full-suite pass claimed; these older tests were not rewritten in this pass.
- [ ] **Remaining follow-ups:** E3B enemy tower attacks still need their own producer/targeting design. Tower shields, armor, repair producers, destruction visuals and campaign/battlefield saving remain planned. Historical HP-not-implemented notes below are superseded only for this hull foundation. See TOWERS/Shared/Durability/README.md and TESTS/Towers/TowerHealth.md.

---

# WORK — Consolidated Outstanding Implementation Backlog

## Inspector HP/ammo meters and compact earned rank (2026-10-17)

- [x] Add right-side Health and optional Ammo progress meters from the selected tower's existing runtime state/events; leave Heat and targeting intact.
- [x] Remove Ammo Capacity left stat row and redundant rank heading, replace with one Combat Rank label, add earned rank to the tower title.
- [ ] Godot headless geometry and desktop visual test still pending; preserve the 560×330 Inspector target and verify maximum-level/hot tower edge cases.

---

## Big mk.1 visual enlargement — 2026-10-17

- [x] Enlarged `big_mk1` artwork from **96×72 to 112×120 px**. Kept the original heavy hull with uniform scale and added large upper/lower plated wings (3.5× Enemy001 width, 5× height). The medium Bulwark stays 64×48.
- [x] Updated exact sprite-size catalog assertions and feature/wave/feedback documentation. **No enemy HP/shield/speed/credits/abilities, scene transforms, wave authoring or navigation algorithm changed.** Big mk.1 keeps its existing 24px circular navigation radius.
- [ ] **Godot visual QA outstanding:** reimport the SVG, verify silhouette/bar clearance, rotations, narrow corridors/wing overlap and mass-wave look; run EnemyCatalogChecks. No Godot runtime test run in this GitHub-only art pass.

---

## Big mk.1 heavy enemy — 2026-10-17

- [x] Registered `ENEMIES/Types/Enemy005` / `big_mk1` in `DefaultEnemyCatalog.tres`, using one inherited plain enemy scene and 112×120 right-facing plated SVG. Original GameMaker-inspired balance: **1,000 hull, 200 optional shields, 80px/s, 5 Citadel damage, 10 Credits**, 24px navigation radius. No new AI or ability nodes.
- [x] F2/F3 catalog discoverability and wave syntax (`Enemy("big_mk1", shield: true)` for 200 starting SP). **Level001 and Level002 waves remain unchanged.**
- [x] Shared DefenseBar takes visual sprite height once on spawn so large ships' hull/shield bars clear their artwork; small ships keep authored minimum. Updated catalog/shield/credit regressions and relevant READMEs in this same commit.
- [ ] **Godot QA pending:** compile/run enemy catalog + GenericEnemy integration runners, F2 cycle, shielded Big mk.1 test wave, portal/corridor navigation for 24px clearance radius, oversized artwork overlap, shield aura/bar visual, rewards and 500-ship performance. No runtime tests performed in this GitHub-only change.

---


## Build preview / unobstructed placement UI — implemented 2026-10-17

- [x] Ctrl-open tray: hover or keyboard-focus a tower tile to show its description, cost, build time, footprint, basic effective stats and CAN AFFORD / INSUFFICIENT RESOURCES in the existing right Inspector. No ghost/payment just for browsing.
- [x] Affordable tile click arms one placement and temporarily hides BottomArea (tray + Inspector). Logical tray-open state stays intact; the permanent top status remains usable. Unaffordable clicks keep the preview/tray visible, create no tower and emit TowerPlacementController.BuildDenied(towerId, reason) for future audio; no audio playback was added.
- [x] RMB/Escape cancel the ghost and restore the tray with no tower selected; Ctrl cancels and actually closes the build tray. Reopening Ctrl does not resume canceled placement.
- [x] Successful placement no longer calls SelectTower(newTower). It ends placement, restores the tray, clears the Inspector and consumes/suppresses the placement frame's selection path. A separate later world click selects the tower normally. Restoring the tray under a stationary cursor does not immediately reopen a hover preview.
- [x] Preserved the latest Inspector resize: existing560×330 dimensions, fonts, right-column/heat structure, styles and layout resources. Compact build descriptions and wrapped affordability text fit the same panel. No InspectorPanel.tscn, UIMasterConfig.cs or DefaultUIConfig.tres changes in this pass.

Verification: TESTS/UI/PlacementHud.md. Current-control descriptions here supersede historical auto-selection/always-visible placement-preview notes below; original plans remain preserved.

---

## Laser Tower silver/yellow visual refinement — 2026-10-17

- [x] Replace Laser Tower SVG with silver/gunmetal body and yellow emitter/rails, retaining existing 64×64 scene sizing and muzzle marker.
- [x] Recolor only the Laser beam preset to ivory-yellow core / golden halo and reduce wave/end wander. Extend shared BeamRenderer's opt-in irregularity with smoothly sampled deterministic noise, preserving zero-irregularity sine fallback and inactive behavior.
- [x] Update Laser visual fixture assertions and the two beam READMEs; no gameplay/targeting/ammo/upgrade changes.
- [ ] **Godot verification pending:** run `TESTS/Towers/LaserTowerChecks` via its existing suite and inspect beam at normal zoom, short/long distances, pause, multiple beams and end wander. This pass was source-validated and its SVG preview-rendered, but no Godot/.NET headless tests could run in this environment.

---

## Enemy shield meter in shared DefenseBar — 2026-10-17

- [x] Added the **shield bar above the regular hull bar**, inside the existing `FEEDBACK/DefenseBars/DefenseBar` Node2D (no extra scene node per enemy). Full shields show both stacked rows, preserving normal hull-bar behavior when unshielded or after shield break. Uses warm red-orange shield tint, separate Inspector-configurable shield thickness/gap, existing width/border/offset, movement counter-rotation and `DefenseBarsEnabled`.
- [x] Shared Enemy shield-change bridge now notifies both `DefenseBar.SetShield` and the existing `ShieldVisual` aura for spawn/damage/explicit restoration; no shield regen, extra damage logic, spawn changes or polling.
- [x] Updated `TESTS/Combat/EnemyShieldChecks.cs` and feedback/enemy docs for full/partial/depleted/recovered shields.
- [ ] **Verify:** run Godot .NET shield fixture and visual test on Level001 shielded Enemy001; confirm shield row sits distinctly above hull, stays horizontal on turns, does not overlap other enemy indicators, and both disappear on removal. Runtime testing unavailable during this GitHub-only pass.

---


## Offensive tower range balance — 2026-10-17

- [x] Increased **all seven offensive tower types at every authored purchased level** approximately 15% (nearest whole px): Battle 294/304/313, Laser 294/304, Blaster 294/304, Shockwave 230/239, Cryo 239/248/258, Machine Gun 239/248/258, Sniper 515/524/534. Source of truth remains each `*Levels.cs` row; rank bonuses/Inspector/range rings/upgrade previews resolve normally. No modifier or change to weapon cooldown, damage, ammo, build costs or Blaster blast radius.
- [x] Updated exact-value regression assertions and per-type README balance tables in the **same commit**.
- [ ] **Godot validation outstanding:** rerun TowerStat, TowerUpgrade, Laser, Shockwave, Cryo, MultiBarrel and TowerRank checks. Review placement-range rings, upgrade comparison, Shockwave propagation and level playfeel. No Godot/.NET runtime available to run tests in this pass.

---


## Inspector visual trim / right-side bars — 2026-10-17

- [x] Narrowed `InspectorPanel.tscn` and the HUD default from **620×330** to nominal **560×330**; the Main stats/paid-upgrade area shrinks, while the six target-priority buttons retain their original **210px right-section layout**.
- [x] Moved the Heat text/bar under the right-side Combat Rank/XP bar. Left-side disabled reasons remain independent. Adjusted `GameplayHUD.cs` cached paths and Heat/Inspector/upgrade regression expectations; no placement/Ctrl/visibility/targeting/heat mechanics changes.
- [ ] **Verify in Godot after the latest commit:** run `TESTS/UI/CompactInspectorChecks.tscn`, `HeatInspectorChecks.tscn`, `InspectorTargetingChecks.tscn` and existing HUD/upgrade test suites. Visually review all nine families, the hot+disabled+Legendary/active-upgrade worst case, header/title and CURRENT/NEXT/GAIN alignment at multiple viewports; ensure no right-side wrap/clipping or unexpected height expansion. These checks have **not been executed for 560px**. Codex's separate placement-mode Inspector/HUD visibility pass must retain this new layout and node paths. See [Inspector current layout](../UI/GameplayHUD/Inspector/README.md).

---

## Battle research proof node — implemented 2026-10-17

- [x] **Battle Tower Weapons I:** battle_weapons_1, 1 RP, requires Research Core, axial (-1,0), +5% Battle Tower damage from the next mission. Saved ownership/RP use the existing progression v2 record; no new currency or XP scoring rules.
- [x] **Tower permanent research Stage04:** an immutable deployment snapshot is captured once in CitadelSession, shared with paid placement/build previews, and retained by each tower. Selected paid level → rank → research → safety clamps → final stats. Standard Battle damage30/33/36 becomes32/35/38 using existing nearest/ties-to-even integer rounding. All ranks/tiers, config rebuilds and paid previews retain the bonus without compounding.
- [x] **Reusable targeted damage effect:** catalog defines TowerDamagePercent + TargetTowerId; only Battle has an authored node. Other towers, Citadel health, ammo, heat, prices, authored rows, profileless F6 and other profiles are unchanged by this purchase. No progression I/O in combat/stat resolution.
- [x] **Graph extension:** real third node, balanced labels, and initial/reset view fits authored branches; pan/zoom selection remains shared geometry.

Verification and remaining manual review: TESTS/Research/BATTLE_RESEARCH.md. Mission XP rewards/replays/award tokens remain deferred. Earlier research-expansion examples below are preserved as history; Battle damage is now implemented rather than a future-only example.

---

## Shield aura art polish — 2026-10-17

- [x] Replaced the nearly invisible shield outline with a **single baked radial-fill SVG** (lightly translucent centre, thicker high-contrast bright rim, layered outer glow and static electric zap filaments). Tuned `FEEDBACK/Shields/DefaultShieldVisualStyle.tres` to 1.65× size, 0.82 idle opacity, 1.0 hit/break peaks, warm red-orange tint; `ShieldVisual.tscn` uses linear filtering. No change to shield gameplay, wave flags, regeneration, particles or per-frame CPU work.
- [ ] **Visual QA:** open Godot to re-import `FEEDBACK/Shields/Artwork/ShieldAura.svg`; check Shielded Enemy001 at normal zoom, small/large enemies, line-of-fire visibility, hit/break fade, layering and mass-wave performance. XML/reference checks passed; Godot headless/desktop tests were not run in this authoring session.

---

## Shield visual foundation (SV1) — 2026-10-17

- [x] Reusable **[FEEDBACK/Shields](../FEEDBACK/Shields/README.md)** presentation: one baked tintable reddish SVG aura, auto-sized behind the enemy, event-only shield damage/break flashes, lazy attachment only to shielded enemy instances, shield-restoration reactivation, no per-frame shield visual script processing. Purely cosmetic; existing shield arithmetic, shield generator and wave opt-in remain unchanged.
- [ ] **Godot desktop/headless SV1 verification** — run modified `TESTS/Combat/EnemyShieldChecks.cs`; inspect spawn/damage/break/restore, layering, red tint on different ship shapes, and 500-enemy performance. The code/resources are pushed but not compiled or visually tested here.
- [ ] **Later, separate decisions:** tower HP/shields/durability, natural shield regeneration, shield bars, premium sparks/shaders. None are implemented in SV1.

---

## Level001 combat/shield playtest balance — 2026-10-17

- [x] Standard Drone (`basic_mk1` / Enemy001) has **20 optional shield points** in its saved EnemyDefinition; zero-shield ordinary/F2 starts remain unchanged.
- [x] Level001's existing three waves remain manual and now contain **38 / 40 / 55** enemies with **7 / 12 / 18** shielded entries respectively. Wave 1 introduces shielded Standard Drones; waves 2–3 add Scouts; final wave adds a few Bulwarks. **No ShieldGenerator** is authored; no shield regeneration, mechanic refactor or new difficulty system was introduced. See [Level001Waves.cs](../LEVELS/Level001/Level001Waves.cs) and [Authoring.md](../LEVELS/Waves/Authoring.md).
- [ ] **Balance/playtest verification:** Confirm 20-point red aura is readable, portal navigation works for the few Level001 Bulwarks, and the three escalating waves are still manageable at 1,000 starting Credits. C# build/headless/desktop test runs are outstanding for this GitHub-only balance pass.

---

## Current amendment — Research before mission XP balancing (2026-10-17)

**This amendment supersedes older status/order statements below; all original plans are retained.** The user explicitly prioritized real mission statistics and the Campaign-only Research Room before finalized XP rewards/deductions.

- [x] **Research foundation / P4 first playable slice:** Campaign → RESEARCH, axial center-origin hex graph, pan/zoom, keyboard selection, prerequisite/effect/cost/state panel and atomic per-profile purchase. Research Core is free/virtual; Citadel Master I costs 1 RP and gives +10% maximum/current starting Citadel HP on both deployed levels. Results and gameplay have no Research link; purchase authority validates the active Research Room. See UI/Menus/Campaign/Research/README.md and SYSTEMS/Progression/Research/README.md.
- [x] **R3 actual telemetry slice:** event-driven paid placements, first construction completions, paid upgrades, unpaused mission clock; existing official kills/breaches/waves/integrity retained. Frozen results show these real counters. Successful placement and completed construction are separate metrics. Credits earned/spent remain untracked/null, not fabricated.
- [x] **Progression v2:** purchased stable research IDs and RP payment saved together; existing v1 XP/level/curve/RP migrate without reset or identity-index changes. XP grant remains a development helper, not a mission reward API.
- [ ] **Remaining P1/P2 / Results R4:** mission reward calculator, editable scoring/ETA, first-clear/replay history, once-only run-token awards and Results XP animation/itemization are intentionally deferred until scoring is chosen. Do not add provisional reward tuning merely to use Research; the default-off DEBUG XP grant supplies test RP.
- [ ] **Remaining P3:** separate LEVELS area/hub reorganization; current two deployment actions remain and Research is added alongside them.
- [ ] **Remaining R2/R3/P5:** desktop visual review, Credits/optional additional statistics, expanded/full regression and optional presentation polish. No tower damage research was added: future tower modifiers use 04_PermanentResearch; no fake catalog nodes.

Focused tests and scope/limits: TESTS/Research/README.md. Do not mark full P1–P5 or all Results R3/R4 complete. Follow the revised sequence in the progression brief rather than repeating the already delivered foundation.

---

**Audit date:** 2026-10-16 (latest audited pushed commit UTC 2026-10-17). Checked against GitHub `main` at `967435719798` (Add Battle Tower construction SVG stages and shared optional level artwork). **This is a documentation/code-presence audit, not a new Godot test run.** Status is a snapshot: check current HEAD and relevant code/tests before Codex acts, particularly if another Codex task was pushed meanwhile.

**Purpose:** One entrypoint for *outstanding implementation passes scattered across project notes*. Links point to the authoritative full specification; this index intentionally does **not** replace those notes. [Code preferences](CodePreferences.md). Keep completed historical work below this index.

### Status key

- **[ ] OPEN — detailed pass exists:** ready for Codex in numbered steps; complete/test before marking done.
- **[ ] OPEN — design only / needs pass:** future system noted in README, but no approved numbered implementation spec yet.
- **[ ] CHECK / POLISH:** feature implemented, but a verification, visual review or maintenance job is outstanding.
- **[x] COMPLETE:** listed to stop Codex from repeating the work.
- A long-term idea mentioned in a README is **not** automatically approved for a giant implementation. Split large systems into narrow passes first.


## Initial progression/menu foundation — 2026-10-17

The user selected a smaller first slice of P1/P3: separate per-profile persistent XP/levels/RP, editable starting curve, multi-level carry and +1 RP per promotion, shared Main/Campaign display, and an off-by-default DEBUG grant option. See [Progression README](../SYSTEMS/Progression/README.md). Mission XP scoring, replay/first-clear history, once-only mission awards, Results XP, the Levels/Research hub split and Research Room remain **OPEN**. Do not mark all P1 or P3 complete. Verification: TESTS/Progression/README.md.

## A. Open implementation passes (Codex-ready or scoped in existing notes)

| Status | Pass / work | Authoritative notes and next action | Dependencies / caveats |
| --- | --- | --- | --- |
| [x] | **IT1–IT4 — compact Inspector + six targeting priorities** | [Implementation/checklist](../UI/GameplayHUD/Inspector/INSPECTOR_TARGETING_PASS.md): non-scrollable700×330 two-column Inspector, independent earned rank and paid upgrades, six per-instance choices and cached checkpoint-aware First/Last. | Automated compilation/headless results: [verification](../TESTS/UI/InspectorTargeting.md). Desktop visual review remains pending; see manual verification backlog. |
| [x] | **CT1–CT3 — Cryo Tower + reusable enemy status-effect foundation** | [New 2026-10-16 Codex plan](../TOWERS/Offensive/CryoTower/CRYO_TOWER_IMPLEMENTATION.md): energy-projectile Cryo, permanent nonstacking 33% slow, scalable optional status infrastructure and tests. | **IMPLEMENTED**, with focused headless tests. Cryo only targets unslowed/nonimmune enemies. See [verification](../TESTS/Towers/CryoTower.md); manual playfeel remains outstanding. Stasis remains deferred. |
| [ ] | **P1–P5 — Player/global XP + Research Foundation** | [Detailed phased Codex pass](../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md): persistent per-profile XP/RP, signed mission scoring + ETA, Campaign Levels/Research, hex-origin graph and real Citadel Master I. | Extends Results R3/R4; **do not implement XP twice** or confuse player XP with tower Rank XP. Campaign save/load remains separate. |

| [ ] | **R2 — Mission Results visual polish** | [Results remaining passes](../UI/Menus/Results/README.md): layout, transitions, skippable count-up, sounds and viewport testing. | R0/R1 outcome/results behavior already complete. Optional independent polish pass. |
| [ ] | **R3 — Actual level-run telemetry / extra stats** | [Results R3](../UI/Menus/Results/README.md): placed towers, successful upgrades, Credits earned/spent, unpaused timer, frozen final telemetry. | **P2 already covers the subset needed for XP/time**; coordinate instead of duplicating stat owners. Extra optional breakdown may be later. |
| [ ] | **R4 — Mission XP scoring/persistence** | [Results R4](../UI/Menus/Results/README.md), implemented through [P1–P2](../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md) in the approved plan. | **Overlapping scope:** after P1/P2, reconcile R4 checklist; don't create a separate XP wallet/service. |
| [ ] | **E3B — Enemies attack towers** | [ENEMIES/README E3B](../ENEMIES/README.md): ranged enemy attacks and electric area pulses against damageable towers. | **HP prerequisite implemented 2026-10-17.** Enemy attack targeting/delivery still needs a separate scoped pass; no automatic tower attacks enabled. |
| [ ] | **Waves Phase 2 — remaining options/content** | [Waves README](../LEVELS/Waves/README.md): more mixed authored wave content, other portal strategies and eventual elite/stealth/speed/boss modifiers. | **Partially complete:** basic mixed catalog, required portals, starting shields and ShieldEmitter already work. This is a collection of optional *follow-up features*, not a scheduler rewrite. |
| [ ] | **Waves Phase 3 — advanced progression** | [Waves README](../LEVELS/Waves/README.md): wave-completion bonuses, countdown/auto-next, announcements, scripted events, preview/history and eventual wave saving. | **Design only; subdivide by feature.** Current finite waves and R1 mission victory already work. |

## B. Outstanding systems documented elsewhere — not yet numbered/approved implementation passes

These are real gaps or explicitly proposed systems, but **need a dedicated scope/decisions before Codex should implement them**.

| Status | System / next pass to design | Existing notes and key boundary |
| --- | --- | --- |
| [x] | **HD1–HD3 — generic tower disabling, heat and compact Inspector** | Implemented [shared API/authoring](../TOWERS/Shared/Conditions/README.md): independent timed/conditional handles, source refresh/cleanup, all offensive attack gates, optional selected-level heat, cooling/latch and tests. Battle is enabled via its Godot-Inspector minute profile (3min continuous fire,9s recovery); other tower opt-ins remain undecided. **HD3 Inspector heat/status presentation is implemented.** [Verification](../TESTS/Towers/TowerConditions.md). |
| [ ] | **Tower HP, shields, damage, destruction and repairs** | [TOWERS roadmap](../TOWERS/README.md), [E3B dependency](../ENEMIES/README.md), [Repair Tower](../TOWERS/Support/RepairTower/README.md). Hull HP/damage/destruction/footprint/Inspector foundation implemented 2026-10-17; shields, repair producers and further durability balance remain planned. |
| [ ] | **Power generation, island-grid electricity, shortages, priorities & relays** | [Power network](../TOWERS/Economy/Power/README.md), [generators](../TOWERS/Economy/Power/Shared/README.md), [consumer capability](../TOWERS/Shared/Power/README.md), [Power Relay](../TOWERS/Utility/PowerRelay/README.md). Solar/Industrial/Fusion share generation code; no cables; terrain islands connect by paired relays. Large system: break into several passes. |
| [ ] | **Actual campaign/mission Save–Load, slots and unlocked level progression** | [SYSTEMS/Saving](../SYSTEMS/Saving/README.md), [Campaign menu](../UI/Menus/Campaign/README.md), [Pause menu](../UI/Menus/Pause/README.md). Profile *identity* is saved, but no campaign/session saves yet. **P1–P5 progression record does not implement campaign saves.** |
| [ ] | **Reusable blank playable level template / level-loading convenience** | [LEVELS](../LEVELS/README.md), [Session](../LEVELS/Session/README.md). `LEVELS/LevelTemplate.tscn` exists with blank terrain/Citadel/Portals container, but the requested *placed active portal* and streamlined new playable-level registration/launcher flow are not done. Create a focused pass if still desired. |
| [ ] | **Tower magazines/reloading, accuracy, maintenance and selling** | [Tower roadmap](../TOWERS/README.md), [Inspector](../UI/GameplayHUD/Inspector/README.md), [Shared ammunition](../TOWERS/Shared/Ammunition/README.md). Implement separately; reload must use real stock, not create ammunition. |
| [ ] | **Additional registered towers and specialized mechanics** | [Offensive](../TOWERS/Offensive/README.md) (Missile, LOS; heat/disable tracked separately above), [Support](../TOWERS/Support/README.md) (Repair/Radar), [Economy](../TOWERS/Economy/README.md) (Resource Tower/Scrap Refinery/older SolarTower). **Sniper + Machine Gun are already implemented** and must not be re-created. Cryo now has its own **CT1–CT3 completed** pass in section A. |
| [ ] | **Further damage/enemy status mechanics** | [ENEMIES future abilities](../ENEMIES/README.md), [COMBAT](../COMBAT/README.md), [Waves modifiers](../LEVELS/Waves/README.md): armor mitigation, stealth/detection, EMP, regeneration, buffs/status durations, additional boss/attack types. The **first concrete slow/status foundation is implemented in** [CT1–CT3](../TOWERS/Offensive/CryoTower/CRYO_TOWER_IMPLEMENTATION.md); coordinate future statuses with it rather than create duplicates. |
| [ ] | **Live gameplay wiring for shared Alerts/Toasts** | [UI/Alerts foundation](../UI/Alerts/README.md) is **implemented and mounted** in the shared GameplayHUD, with configurable banners, priority/queues, deduplication, and DEBUG F7 preview. **Not implemented:** real producers from Citadel, waves, tower construction/low ammo, research or combat; wire narrow event-driven sources later, without per-frame popup spam. Sounds/voice/history/save persistence are separate future work. |
| [ ] | **Audio game content/event wiring & settings** | [AUDIO](../AUDIO/README.md): foundation exists; actual clips, sound event connections, menu music/ambience, voice lines and optional player preferences remain future stages. |
| [ ] | **Visual combat feedback polish** | [Damage Numbers](../FEEDBACK/DamageNumbers/README.md), [Camera Effects](../FEEDBACK/CameraEffects/README.md): unimplemented optional effects; [DefenseBars](../FEEDBACK/DefenseBars/README.md) and death/shockwave visuals already exist. |
| [ ] | **Player combat drone ability** | [DRONES/Combat](../DRONES/Combat/README.md): entirely prospective attack drone; **do not confuse with completed transport drone logistics**. |
| [ ] | **Research expansion beyond first proof node** | [Player research foundation](../SYSTEMS/Progression/PLAYER_XP_RESEARCH_FOUNDATION_PASS.md): the giant research tree, DME/Zy0n story gates, unlockable towers, doctrine loadout slots, production buffs and refunds/respec are **explicitly deferred beyond P1–P5**. |
| [ ] | **Further economy/ammo logistics options** | [Economy](../TOWERS/Economy/README.md), [Ammo loop](../TOWERS/Economy/Ammunition/README.md): raw materials, differing ammo types, research-controlled throughput, additional resource loops—not missing portions of the implemented first two-leg pipeline. |
| [ ] | **Optional advanced world art** | [Backgrounds](../WORLD/BACKGROUNDS/README.md): parallax, shaders, planet/nebula animations and unique level art; cosmetic backlog, not a mechanics prerequisite. |
| [ ] | **Options menu / persisted personal settings** | [Pause](../UI/Menus/Pause/README.md), [AUDIO](../AUDIO/README.md). Existing Pause Options is a disabled placeholder; needs its own specification. |

## C. Verification, profiling and note cleanup — not missing gameplay passes

| Status | Task | Evidence / do not redo |
| --- | --- | --- |
| [ ] | **N4 corridor movement desktop visual/performance review** | [N4 implementation record](../ENEMIES/enemypathing.md), [CPU timings](../TESTS/Performance/CorridorPerformance.md), [follow-up profiling suggestions](../ENEMIES/ENEMYPERFORMANCE.md). N4 is **implemented**, only visual 0/50/100% comparison and rendered 500-enemy checks remain. |
| [ ] | **Profiling only if measured bottlenecks warrant** | [ENEMYPERFORMANCE](../ENEMIES/ENEMYPERFORMANCE.md), [findings](AGENT_FINDINGS/Performance.md): repath-burst latency, potential duplicate spatial-index position update, clearance cost; preserve safety and measure before optimizing. |
| [ ] | **Godot/.NET standard build and full-suite blockers** | [TESTS/README](../TESTS/README.md), [Ammunition evidence](../TESTS/Economy/Ammunition.md), [Footprint evidence](../TESTS/Towers/Footprints.md). Previous environment lacked expected .NET/MSBuild/NuGet dependencies; full integration also hit preexisting Level002 painted-terrain assertion. **Recheck current environment/map before claiming unresolved or fixed.** |
| [ ] | **Beam ripple/GPU effects test and visual QA** | [Beam renderer](../TOWERS/Shared/Weapons/Beams/README.md), [Laser tests](../TESTS/Towers/LaserTowerChecks.cs): build and run modified LaserTowerChecks (muzzle anchor, bounded impact drift, allocation/node stability, GPU emitter toggles), then desktop check short/long beams, moving enemies, multiple beams, pause, EMP, target loss and low ammo. New visuals are pushed but **not yet Godot/.NET-tested** in this session. |
| [ ] | **Alerts F7 visual/pause verification** | [Alerts README](../UI/Alerts/README.md) has a debug preview and manual checklist: topbar clearance, priority interruption, toast merging/queue bounds, pause behavior, scene transitions and smaller viewport. The GitHub-only port is **not yet Godot C# built or visually playtested**. |
| [ ] | **Manual render/playtest verification** | Battle construction **stage SVGs are implemented**; check in-game stage positions/transition vs the provided rendered contact sheet ([test checklist](../TESTS/Towers/ConstructionArtwork.md)). Check existing DefenseBar, 500-enemy motion, full HUD and ammo drone visuals. **Construction sparks/VFX were explicitly excluded from this C4 scope**. Headless validation is not a substitute for the Godot editor. |
| [ ] | **Stale status text / factual note cleanup** | [ENEMIES/README](../ENEMIES/README.md) still has an old N4 “planned” section; [enemypathing.md](../ENEMIES/enemypathing.md) contains a contradictory top “NOT IMPLEMENTED” line; [tower_ideas.md](../TOWERS/tower_ideas.md) still describes ranks/multi-cell/Laser as absent; [WORLD/README](../WORLD/README.md) has historical “Citadel health future” wording; [TOWERS/README](../TOWERS/README.md) has an obsolete “multi-cell placement not implemented” list item. Update obsolete lines against actual code, **do not implement those features again**. |
| [ ] | **Older ammo verification/update notes** | [Previous WORK detail below](#archived-completion-record--ammunition-pipeline), [Ammo tests](../TESTS/Economy/Ammunition.md): some original 1×1 wording and a pre-later-balance verification status are historical. Verify/refresh after current-footprint and shipment defaults, not new inventory architecture. |

## D. Already delivered — not outstanding implementation passes

- [x] **GE1–GE3 inherited enemy scenes:** one [GenericEnemy.tscn](../ENEMIES/Shared/GenericEnemy.tscn) supplies Sprite, Movement and DefenseBar; five type scenes inherit and retain their artwork/abilities. [Implementation and tests](../ENEMIES/Shared/GENERIC_ENEMY_SCENE_PASS.md).
- [x] **Enemy E2A/E2B/E3A:** mixed catalog, ShieldEmitter, Transporter + six-child staggered DeathSpawn and wave ownership. [ENEMIES](../ENEMIES/README.md) / [DeathSpawn tests](../TESTS/Enemies/DeathSpawn.md).
- [x] **Navigation N4, F3 route preview and checkpoint chains:** core implementation done; visual review separately listed above. [Movement](../ENEMIES/Movement/README.md), [Route preview](../ENEMIES/Movement/pathoverlay_tweaks.md).
- [x] **Multi-cell tower footprints F1–F5:** Factory **3×2**, Depot **2×2**, offense **1×1**, occupancy/path/performance tests. [Pass](../TOWERS/Placement/MULTI_CELL_FOOTPRINT_PASS.md).
- [x] **Tower core combat/stat/paid-level/rank foundation, Laser/Blaster/Shockwave, typed damage and enemy spatial queries.** [TOWERS](../TOWERS/README.md), [COMBAT](../COMBAT/README.md).
- [x] **Reusable beam visual polish (2026-10-17):** [Shared BeamRenderer](../TOWERS/Shared/Weapons/Beams/README.md) has adaptive animated ripple, endpoint drift, drawn impact glow and independently optional scene-owned `GpuParticles2D` impact/muzzle emitters. [Laser style](../TOWERS/Offensive/LaserTower/Beam/README.md) enables subtle ripple/contact sparks, reusing [FEEDBACK/Shared](../FEEDBACK/Shared/README.md) particle assets. **Cosmetic only; GPU appearance and changed Laser test assertions not run in Godot here.**
- [x] **Sniper Tower + twin-barrel Machine Gun Tower:** current playable scenes/configs/levels/ranks and shared projectile firing mode. [Sniper](../TOWERS/Offensive/SniperTower/README.md) / [Machine Gun](../TOWERS/Offensive/MachineGunTower/README.md). Note: a subsequent balance commit made Battle faster and tuned Machine Gun speed—don't replace these values with old samples.
- [x] **C1–C3 timed construction/upgrades plus C4 Battle-only SVG artwork pilot:** operational gating, progress and paid upgrade timers are live; optional artwork presenter, Battle stage SVGs and per-level artwork fallback are in code. [Pass](../TOWERS/Shared/Construction/TIMED_CONSTRUCTION_PASS.md) / [pilot verification](../TESTS/Towers/ConstructionArtwork.md). **Remaining only optional factory/other type-specific stage content and desktop visual QA; sparks/VFX not requested.**
- [x] **Ammunition production, both physical drone delivery legs and offensive consumption.** See archived record below / [Ammo notes](../TOWERS/Economy/Ammunition/README.md).
- [x] **Shared UI Alerts foundation:** old isometric 2D major-alert and toast system ported into [UI/Alerts](../UI/Alerts/README.md), installed once through GameplayHUD with DEBUG F7 demonstration; **gameplay event wiring and actual Godot verification are still pending**. No tower/combat/wave mechanics changed.
- [x] **Results R0/R1:** real mission victory/defeat, snapshot, menu navigation; **not** R2–R4. [Results](../UI/Menus/Results/README.md).
- [x] **Profile identity Boot/Main/Campaign foundations, Pause basics**; not gameplay saves, XP or Research. [Profiles](../SYSTEMS/Profiles/README.md).
- [x] **Build HUD Offensive/Economy categories and compact Inspector** on the latest audited HEAD. [HUD check](../TESTS/UI/HudCategories.md).

## E. Suggested sequencing when returning

1. **Inspector visual review:** IT1–IT4 source and automated tests are complete. Manually assess the compact non-scrollable Inspector in-game; do not recreate six priorities or add a second journey estimator.
2. **Cryo Tower CT1–CT3** — implemented: reusable status-effect architecture and permanent nonstacking slow; **do not implement Stasis now**. Coordinate targeting-related edits in `TowerTargeting.cs` with IT1–IT4 so concurrent Codex work doesn't overwrite changes.
3. **Campaign progression P1–P5** — profile XP, mission awards, Research Room and first real research; coordinate Results R3/R4 telemetry and don't conflate this with still-missing campaign saves.
4. **HD1–HD3 complete; thermal balance for other towers remains open** — shared multi-source disabling and optional heat are implemented and tested. Battle now has a3min minute-based profile; other per-tower heat balance/opt-ins remain undecided. Compact event-driven Inspector heat/status presentation is implemented. This remains independent of tower HP/E3B.
5. **Tower durability + enemy E3B** — first implement tower health/shields/destruction safely, then enable enemy attacks. Only proceed after dependency passes are scoped and tested.
6. **Large management systems** — campaign Save/Load, followed by modular electricity/island power grids/relays, when ready.
7. **Optional/future** — Results R2 polish, more stage artwork (Battle pilot already done), advanced waves, additional towers, sounds, feedback, combat drones and graphical/performance checks. Don't redo complete GE1–GE3 or C1–C4 pilot.

**Codex handoff:** “Read `NOTES/WORK.md`, `NOTES/CodePreferences.md`, and the linked authoritative pass file for the single item selected. Verify HEAD and implemented code first. Implement and test only that scope; update WORK and the source pass checklists after verified completion.”

---

## Archived completion record — Ammunition pipeline (historical)

**Status: IMPLEMENTED AND HEADLESS-VERIFIED (2026-10-16), with known standard-build/full-suite environment/map limitations below.** The original foundation helpers were reused, not replaced: AmmoInventory, AmmoProduction, AmmoStorage and AmmunitionMasterConfig.

### Completed gameplay

- [x] Optional AmmunitionStats in each offensive tower's complete independent C# level rows, through the existing cached eight-stage pipeline. Current state is separate.
- [x] Per-instance reserves; configurable InitialAmmoFraction (full by default); per-launch Battle/Blaster charges, per-pulse Shockwave charge and prepaid time-based Laser use.
- [x] Empty weapons stop unpaid attacks and resume after physical resupply. Damage types, shield penetration, AoE/wavefront delivery and source attribution are preserved.
- [x] Stock persists through rank promotions and purchased levels; capacity growth never refills, shrink explicitly discards excess and records it.
- [x] Buildable/selectable Factory and Depot, Economy catalog entries, vector placeholders, 1×1 navigation-safe placement and existing wallet/payment failure behavior.
- [x] Automatic factory whole-batch production, finite buffer, full/no-depot pause and departure-driven resume using AmmoProduction.Advance.
- [x] Separate depot AmmoStorage inventory; more than one producer/store, deterministic nearest eligible selection.
- [x] Visible factory-owned Factory→Depot drones **and** depot-owned Depot→Tower drones, carrying finite real cargo and returning home. No instant shipments.
- [x] Incoming space reservations, finite home slots, single outstanding consumer shipment, scarce-stock retries, partial/surplus delivery and interruption cleanup.
- [x] Selected-tower/building runtime Inspector status and authoritative next-level ammo previews, using coalesced events rather than HUD polling/pipeline work per shot.
- [x] Existing pure helper tests executed first; focused weapon, delivery safety, conservation, purchase and real-physics automatic-loop tests added under TESTS/Economy.
- [x] Relevant navigation/menu/placement regressions and remaining diagnostic integration checks executed; actual outcomes documented.
- [ ] Clean standard SDK/editor build and unmodified full integration pass: blocked by existing container SDK/MSBuild/NuGet dependencies and Level002 tile-count assertion respectively. These are not claimed passing.
- [ ] Manual desktop graphical playtest: headless scenes/motion/transactions are verified, actual desktop rendering has not been manually inspected.

### Editable provisional defaults

| Item | Values |
| --- | --- |
| Battle, all current levels | 1000 reserve; 1 per successful projectile launch |
| Laser, all current levels | 1000 reserve; 2 rounds per firing second, retaining prepaid fractions |
| Blaster, all current levels | 600 reserve; 3 per projectile launch |
| Shockwave, all current levels | 400 reserve; 5 per emitted pulse |
| Global threshold | Existing DefaultAmmunitionConfig.tres:10% |
| Factory | 200 Credits;100 ammo/2s;**1000 buffer; 1000 ShipmentAmount (full load to dispatch);**1 drone;1000 payload;220px/s |
| Depot | 150 Credits;**10000 storage**;2 drones;250 payload;220px/s |
| Dispatcher | 0.2s scheduling interval;8 alternating endpoint checks |

Battle1000/1 and the10% rule follow the requested example. Other defaults are provisional tuning, not permanent balance. Production/storage settings initialize when placed; transport settings affect subsequent dispatch. Offensive level values remain in each *Levels.cs file only. Building noncombat settings live in their Inspector config resources.

### Factory dispatch tuning (2026-10-16)

**New defaults implemented in code, regression tests updated; current adjustment has not been re-run against Godot yet.** The factory produces 100 rounds every two seconds but waits until its configurable `ShipmentAmount` reaches **1,000** before launching. Every flight is an exact 1,000-round load from its finite 1,000-round buffer. If no depot can reserve 1,000 free slots, the factory holds the stock and pauses, rather than sending an early/partial drone. Depot storage is now 10,000. Depot-to-tower resupply remains as before (depot drone payload 250; global 10% demand). Edit `AmmunitionFactoryConfig.cs` / Godot Factory config and `AmmunitionDepotConfig.cs` / Depot config; no hardcoded scheduler threshold is necessary. See [tests](../TESTS/Economy/Ammunition.md).

### Ownership and safety

TowerAmmunition wraps the existing stock helper; factories/depot nodes wrap the existing production/storage helpers. TOWERS/Economy/Ammunition/Shared/Logistics owns requests, stock and incoming-capacity reservations. DRONES/Transport owns reusable direct flight/rendering/callbacks, not damage or ammo minting. One logistics node lives in each World, not an Autoload. No enemy queries or per-frame building cross-product searches occur.

Load source→cargo at departure; unload cargo→destination on arrival. Home slots remain occupied until physical return. Missing destination/capacity reduction returns surplus. Missing source/drone loses remaining cargo once. A return to an already-full home returns what fits and explicitly counts remaining loss. Removed endpoint stock also counts as loss. World unload clears jobs and subscriptions. Conservation includes initial stock, factory production, all local stock/cargo, paid consumption and explicit loss/discard.

### Verification and testing

Run `res://TESTS/Economy/AmmunitionLoopChecks.tscn` after building. It first runs the original AmmoFoundationChecks, then small focused WeaponAmmunitionChecks and DeliverySafetyChecks using AmmoLoopFixture. It buys Factory/Depot/Battle through actual buttons/payment, fires900 Battle projectiles to the threshold, verifies both real drone legs and arrival-only stock changes, and verifies automatic real-physics callbacks/pause with no manual drone ticking. TESTS/Economy/Ammunition.md records exact results and outstanding environment/map limitations.

For a quick desktop test, place Factory and Depot near a Battle Tower. Default starting stock is1000; reduce that config's InitialAmmoFraction before placement if waiting for900 shots is inconvenient. Select each building/tower to inspect stock, slots and demand; amber cargo indicates loaded flight. No debug/manual refill is needed for production deliveries.

### Still deferred

No permanent research, saving, magazines/reload timers, power grids/consumption, raw material inputs, construction timers, per-tower threshold sliders, differentiated ammo types, selling, tower health or combat drones were implemented. Future work should build on this working loop rather than recreate inventory, production, storage, threshold or reservations.


## Cryo CT1–CT3

Implemented shared lazy permanent-Slow status storage/Inspector immunity, immutable projectile payload, Cryo Energy weapon/catalog/artwork, explicit levels/ranks and effective Slow stat previews. Targeting excludes any already-slowed or immune enemy, without damage-only fallback. Reuses construction, physical ammo and spatial queries. Stasis/DoT/timed scheduler/research/saving remain deferred. See TOWERS/Offensive/CryoTower/README.md and TESTS/Towers/CryoTower.md for verification.

## PV1/PV2 implemented — 2026-10-17

Projectile tracers, real-muzzle bounded flashes and tower-owned Battle/Machine Gun/Sniper/Cryo presets are implemented. Sniper additionally has optional subtle red barrel-forward aiming. No HUD, gameplay stats, targeting or ammo systems were redesigned. [Authoring](../TOWERS/Shared/Weapons/Visuals/README.md), [actual verification and remaining desktop review](../TESTS/Towers/ProjectileVisuals.md).

## PV3 follow-up / Inspector width — 2026-10-17

PV3 automated/resources/documentation work was already included in the tracer/muzzle commit. Follow-up retains the desktop visual-review task and reruns the focused suite. Inspector default is now620×330, with content-sized CURRENT/NEXT/GAIN columns and thin drawn vertical separators; no scrolling, smaller fonts or gameplay changes. Focused all-building/viewport geometry checks pass (620px wide, max331px high). See TESTS/UI/InspectorTargeting.md.

## Barrel origins / flash visibility follow-up — 2026-10-17

Blaster migrated from center launch to its actual barrel-tip marker with a shared tower-owned orange flash preset. Battle/Machine Gun/Sniper/Cryo already use real muzzle markers; Laser uses its beam muzzle and Shockwave intentionally remains centered. Shared flashes now have first-process presentation protection, a bright hold/linear fade, larger/longer presets and draw above projectiles. Preserves8c7ab2e's20% Battle/Machine Gun rate balance. See TESTS/Towers/ProjectileVisuals.md for regression evidence and pending desktop appearance review.



