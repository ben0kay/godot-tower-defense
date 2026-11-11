# Game Ideas — Feedback/UI and Gameplay Mechanics

> **Status:** Brainstorming only. None of the ideas below are authorized for implementation by this document. Fully implemented ideas were removed in the 2026-11-08 audit; partial ideas now identify their remaining work.
>
> **Before implementation:** Read the root `project.md`, `CodePreferences.md`, the relevant feature README and current code. Check whether an idea is already implemented, partially implemented or superseded. Keep gameplay and cosmetic effects separate, use existing shared owners, Inspector-editable configuration and bounded/event-driven processing. Keep validation lightweight per CodePreferences.

## 1. Feedback, UI and visual polish

Ideas to make combat, upgrades, economy and waves more readable and satisfying without changing core balance.

| ID | Idea | Proposed behaviour |
| --- | --- | --- |
| F01 | Additional hit-reaction variety | **Partially implemented:** kinetic hull-hit sparks, shield impact/break feedback and Blaster expanding shockwave rings already exist. Consider additional damage-family variation, stronger armour-hit reads and restrained effects for energy/beam impacts without duplicating existing feedback. |
| F02 | Floating damage numbers | Optional shield/armour/critical/resisted numbers above enemies. Aggregate rapid machine-gun hits into a single updating value to avoid clutter. |
| F03 | Tower upgrade celebration | **Partially implemented on dev:** aqua purchased-level and gold earned-rank callouts with a thick up-arrow, reusing `FEEDBACK/WorldCallouts/`. Additional pulses, glow and holograms remain ideas. |
| F04 | Better enemy destruction | **Partially implemented:** killed enemies already emit shared smoke/orange-flash DeathExplosion feedback. Remaining idea: bounded small-ship fragments, staged large-ship internal blasts, and optional short-lived boss wreckage or shockwaves. |
| F05 | Specialized wave announcements | **Partially implemented:** standard numbered incoming-wave banners and authored narration already exist. Add distinct visual/audio treatment for genuinely authored elite or boss waves without replacing the existing wave-presentation system. |
| F06 | Richer heat and damage states | **Partially implemented:** generic disable/NoPower already tints tower artwork red and shows an amber warning emblem. Remaining: barrel heat glow, overheat venting, hull-damage sparking and more nuanced shut-down/recovery animations, driven by existing heat, hull and disable events. |
| F07 | Kill streak feedback | Small, primarily cosmetic combo indicator for rapid kills. |
| F08 | Additional Citadel impact reaction | **Partially implemented:** world-space F08-A Citadel impact camera shake is live in source. Remaining ideas: a short health-bar flash, Citadel shield ripple and/or concise breach warning, complementary to existing critical-health alerts. |
| F09 | Tower targeting indicator | Selected tower optionally shows a subtle current-target line or reticle. |
| F10 | Contextual event feed | Compact, prioritized messages such as SHIELD BROKEN, TOWER DISABLED, RANK UP and BONUS CREDITS; avoid notification spam. |
| F11 | Tower efficiency indicators | Inspector shows recent DPS, kills, shield damage and firing uptime, using bounded aggregated telemetry. |
| F12 | Further audio reaction layers | **Partially implemented:** shared positional audio already handles tower firing, ambience, UI cues, narration and victory-related playback. Add missing shield-break/armour-hit/rank-up/heavy-enemy sound families as needed, with spatial deduplication and bounded polyphony. |
| F14 | Additional construction finish flourish | **Partially implemented:** staged construction art, drones, welding effects and shared construction-site holograms are present. Optional extra finish pulse/activation flash should be a small cosmetic event only; do not rebuild construction or its completion lifecycle. |
| F15 | Per-wave completion summary | Show a **brief per-wave** enemies-destroyed / Credits / Citadel-damage / standout-tower summary between waves; distinct from the already implemented **mission Results** screen, whose remaining Credits telemetry is separately tracked. |

**Suggested future visual milestone:** choose small remaining slices of F01, F03, F04 and F06. Basic hit effects, upgrade/rank callouts, death smoke/flash and portal materialization are already implemented; do not rebuild them.

## 2. New features and gameplay mechanics

Ideas for tactical depth, replayability, tower variety and enemy behaviour. Some are inspired by the original Vector Tower Defense notes and some may overlap with existing plans or partial systems.

| ID | Idea | Proposed behaviour |
| --- | --- | --- |
| M01 | Tower synergy and adjacency bonuses | Selected combinations create bonuses, e.g. Laser against Cryo-affected enemies, or nearby Energy Generators boosting towers with diminishing returns. Reward thoughtful placement. |
| M02 | Additional enemy support ships | **Partially implemented:** Shield Emitter ships already restore allied shields. New choices: hull-repair vessels, command ships boosting speed, and jammers disrupting targeting. Original notes also include Shielding, Virus and Electro concepts. |
| M03 | Player-controlled battlefield abilities | Cooldown- or credit-funded orbital strike, emergency Citadel shield, tower overcharge or emergency repair-drone dispatch. |
| M04 | Specialized tower upgrade branches | At a purchased upgrade level, choose between specializations such as Battle Tower Armour Piercing vs Suppressive Fire. Keep XP/rank progression separate. |
| M05 | Carrier launches during flight | **Partially implemented:** transporter and carrier DeathSpawn already release smaller ships **on hull death**. Remaining idea: finite fighter launches **while the carrier is still travelling**, with real wave ownership/budgets. Do not rebuild DeathSpawn. |
| M06 | Multi-phase boss ships | Boss behaviour changes with health thresholds, e.g. Decimator shield-generator loss, acceleration and escort deployment. |
| M07 | Per-enemy resistances and vulnerabilities | **Partially implemented:** shared typed damage, shield/hull coefficients and penetration already exist. Remaining idea: **authored per-enemy** resistance/weakness profiles so some hulls take different effective damage from kinetic, energy or explosives; avoid duplicating the shared resolver. |
| M10 | Random mission modifiers | Mission-specific faster enemies, stronger shields, slower construction or other disclosed modifiers. |
| M11 | Research loadout slots | Select a limited set of unlocked research effects before a mission, inspired by the original research-inventory design. |
| M12 | Salvage/recovery drones | **Planned, not implemented:** some eligible destroyed enemies leave short-lived recoverable scrap. A limited, physically travelling recovery drone claims a piece, retrieves it and returns it for a small reward. Decide whether to give bonus Credits, separate salvage materials or XP; prevent duplicate kill rewards, unbounded pickups and free resource creation. Related future proposals also exist in `DRONES/ideas.md` (I2) and the planned Scrap Refinery. |
| M13 | Environmental hazards | Solar storms, radiation zones or asteroid fields affect tower and enemy behaviour. |
| M14 | Tower active abilities | Selected towers can overcharge, focus fire or enter a defensive mode for a limited time. |
| M15 | Enemy formations | Squadrons enter with leaders and supporting craft; formations create tactical targeting choices. |
| M16 | Endless survival mode | Optional escalating waves, rewards and score records. |
| M17 | Class-based directional enemy impact stagger | **Implemented in source.** Reuse existing `EnemyClass` as shared stagger resistance; Blaster Cannon affects Light (strong) and Standard (mild), Rocket Tower affects Light (mild), and other weapons/classes remain ineligible initially, including currently unspecified Tiny. A short real forward-speed dip plus visual-only blast-direction hull shove; no physical knockback or per-enemy masses. Add `ImpactStaggerEnabled` to the default Feedback config to disable both. [STG0 implementation plan](../FEEDBACK/ImpactStagger/IMPLEMENTATION_PLAN.md). |

## Implementation guidance

- **Existing vs new:** This file retains **only ideas with outstanding work**. Some rows explicitly describe a shipped foundation plus a distinct remaining slice. Removed from the active list after the 2026-11-08 audit: F13 (portal activation), M08 (six core targeting priorities) and M09 (Factory → Depot → consumer ammunition delivery). IDs are stable historical references, not work orders. Verify current HEAD and the feature documentation before selecting a pass.
- **Ownership:** Cosmetic shared effects belong under `FEEDBACK/`, UI under `UI/`, audio playback under `AUDIO/`, tower behaviour under `TOWERS/`, enemies under `ENEMIES/`, and shared rules under their existing owners.
- **Performance:** Avoid per-hit/per-projectile scene nodes and full-map polling. Aggregate frequent numbers, cap/pool effects, sleep idle systems, and use events and cached data where possible. Don't add visible feedback delays solely to reduce spikes.
- **Scope:** Implement ideas as separate focused tasks, with Inspector-editable tuning and a brief source/build sanity check where practical. Owner performs detailed Godot visual/performance acceptance.
