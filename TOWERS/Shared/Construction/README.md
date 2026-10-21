## CD1/CD2 arrival hold — implemented 2026-10-21

TowerBase.StartConstruction optionally accepts the paid placement's builder owner; it creates the hold/token before Ready/first tick. AwaitingConstructionDrone keeps phase Constructing and the site inactive. Held jobs remain registered but stay outside ConstructionWorkManager's active batches; held-only clocks sleep. Matching builder arrival resumes only that token/job and publishes a work-only refresh, never elapsed time or completion. Active upgrades/unheld jobs retain their timer behavior. Cancellation/clock teardown removes holds safely.

The shared builder manager listens only to successful placement, flies/deploys its beam and waits for existing WorkCompleted before returning. See ../../ConstructionDrones/README.md for architecture, configuration, passing checks and pending desktop review. Earlier planned descriptions below are historical. Saving remains unimplemented; the transient builder hold does not change TowerWorkState's future-save placeholder.

## Planned Citadel construction drone arrival-gated work (2026-10-21; NOT IMPLEMENTED)

Codex implementation handoff: [TOWERS/ConstructionDrones/README.md](../../ConstructionDrones/README.md). **Current work still starts immediately after paid placement.** The proposed shared mechanic would hold only *initial* nonzero construction clocks until a Citadel builder drone arrives and activates its cyan beam, without touching placement/footprint occupancy, Credits, normal upgrades, stage visuals or the sparse timer. See CD1–CD3 in that README; source remains unchanged.

---

## Optional split finished visuals

TowerConstructionVisuals.FinishedVisualPaths optionally lists cached CanvasItem layers alongside SpritePath's composite overlay. Existing presenters with an empty list retain their original visibility behavior. The original composite finished texture selects split layers; authored construction/custom monolithic tier or upgrade textures select the overlay. PresentationChanged fires only when this mode changes. Battle uses this to retain all existing construction art while showing its fixed base and recoiling gun after completion. Custom composite artwork suspends split recoil; it is not automatically partitioned.

# Shared construction — C1–C3 and Battle-only C4 artwork implemented

Paid placements now occupy their complete footprint immediately, take authored time to build, and become operational once. Payment still uses the existing atomic wallet callback; invalid construction setup, scene/index failures and failed placement roll back cells, nodes and work before payment. Construction completion does not alter occupancy or navigation. Directly instantiated scenes remain operational unless placement explicitly starts construction.

## Source values

Each offensive `*Levels.cs` has one complete row per level, now including `WorkSeconds` beside Cost. Level1 supplies initial build time; later rows supply the independent paid upgrade duration. **Paid upgrades retain the old tier throughout work**, applying the selected complete next tier only on completion; no second purchase is allowed while busy. Passive configs keep empty Levels and use Inspector `ConstructionSeconds` in their existing default `.tres`; no artificial weapon stats were introduced. Generic/test types default to0 seconds. Negative/nonfinite durations are rejected, copies stay independent, and no duration is compounded.

| Building | Initial build seconds | Paid upgrade seconds |
| --- | ---: | --- |
| Battle | 3 | 4,5 |
| Laser | 4 | 5 |
| Blaster | 5 | 6 |
| Shockwave | 5 | 6 |
| Sniper | 4 | 5,6 |
| Machine Gun | 4 | 5,6 |
| Ammunition Factory | 8 | No purchased levels |
| Ammunition Depot | 5 | No purchased levels |

These positive values are provisional playtest defaults. No combat values/prices changed.

## Runtime ownership and performance

`TowerBase` owns WorkPhase, elapsed/total/remaining time, normalized WorkProgress, IsBusy/IsOperational, and WorkStarted/WorkProgressChanged/WorkCompleted events. `TowerWorkState` is a source-only snapshot contract for future saving; no save/restore flow is implemented. Upgrading captures PendingUpgradeLevel and duration at purchase; completion resolves that tier through the same stat pipeline with the then-current Rank. Earned XP, aiming/cooldowns, stock and old-tier operation continue during work. Capacity synchronizes once on completion without refill.

One `ConstructionWorkManager` in each playable World advances only active jobs. The manager stops physics processing when empty. Reusable double-buffered queues plus versioned membership handle simultaneous completions, removal and callback-created work without per-frame snapshot allocations; new jobs start advancing next tick. Unpaused game time only; fractional tick sums complete at the exact boundary with a small numerical tolerance. Progress notifications are throttled to approximately10Hz; completion is immediate. No construction nodes or polling are added per idle building.

Weapon and Factory processing is disabled during construction, with a shared operational guard protecting explicit ticks. Finished weapons regain their normal processing without stat rebuild, ammo refill, XP award or cooldown reset. Unfinished buildings/consumers stay outside logistics registration and demand searches. Operational checks also protect dispatch and arrival; completion registers the existing stock once. Factory remains100 rounds/2s,1000-round shipments; Depot remains10000 capacity. Existing drones/logistics remain the sole delivery authority.

The existing selected-tower Inspector has a compact work section: **CONSTRUCTING / UPGRADING to Level N / OPERATIONAL**, progress bar, elapsed/total seconds and time remaining. Only its work controls update on throttled progress events; full stat/preview resolution occurs at input/state changes, not each timer tick. Closing/switching/unloading detaches work listeners; reselecting reads current progress. A paid upgrade preview says the cost is already paid and its button stays disabled. Current stats continue describing the old active tier. Battle now shows authored construction stages; other buildings retain regular artwork. All current upgrade images remain unchanged unless optional level artwork is assigned.

## Shared artwork (Battle-only C4 pilot)

`TowerConfig.ConstructionArt` is an Inspector resource with `BuildStages` and `LevelArtwork`. Battle's default config references `Artwork/Construction/BattleConstructionArt.tres`:0% bare foundation,35% frame,70% assembly, then the original finished SVG at completion. These are cosmetic thresholds, not work-duration or stat sources. There is no fixed stage count; invalid/missing entries are ignored.

`TowerConstructionVisuals` is an optional scene child with a SpritePath pointing to the existing Sprite2D. It reads state on Ready (including paid construction set before AddChild), listens to start/completion and busy-only throttled progress, and changes only Texture when necessary. It never changes sprite transform/tint, aiming hierarchy, world/input/collisions or work progress. Idle presenters have no process/physics loop. The construction ghost keeps its ordinary icon.

To enable another building, add this presenter as a child, point SpritePath at that building's Sprite2D, and assign a ConstructionArt profile before placement. Author every image on the same canvas/anchor/facing as its existing sprite so full rectangular scale/offsets remain correct. Other production scenes currently have no presenter/art profile. Factory stage imagery is deliberately deferred.

Each optional LevelArtwork entry has Level, FinishedTexture, UpgradeTexture and UpgradeProgress. During upgrades, keep the completed old-tier texture until the one intermediate-image threshold; at completion use the target finished image if supplied. Missing finished images retain the nearest earlier completed appearance, never an intermediate construction image. Battle has empty Inspector slots for Levels2/3; it currently looks the same at every upgraded tier. Art metadata stays outside numeric C# level rows.

Profiles/textures are shared immutable authored resources. Config copies remain private; numeric rows/recipes are explicitly copied, and weapon-specific mutable effect styles still use their existing private-copy overrides. No runtime profile/texture duplication or texture loads occur on progress events.

No sparks, particles, completion effects, research, selling/refunds, builder drones or saving were added. Desktop playtesting remains manual. See [TIMED_CONSTRUCTION_PASS.md](TIMED_CONSTRUCTION_PASS.md) for revised scope and `TESTS/Towers/ConstructionArtwork.md` for execution and visual checklist.
