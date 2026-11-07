# Game Ideas — Feedback/UI and Gameplay Mechanics

> **Status:** Brainstorming only. None of the ideas below are authorized for implementation by this document.
>
> **Before implementation:** Read the root `project.md`, `CodePreferences.md`, the relevant feature README and current code. Check whether an idea is already implemented, partially implemented or superseded. Keep gameplay and cosmetic effects separate, use existing shared owners, Inspector-editable configuration and bounded/event-driven processing. Keep validation lightweight per CodePreferences.

## 1. Feedback, UI and visual polish

Ideas to make combat, upgrades, economy and waves more readable and satisfying without changing core balance.

| ID | Idea | Proposed behaviour |
| --- | --- | --- |
| F01 | Better hit reactions | Armour hits throw tiny sparks; shield hits produce blue ripples; explosive hits briefly expand into rings. Different reactions communicate damage effectiveness. |
| F02 | Floating damage numbers | Optional shield/armour/critical/resisted numbers above enemies. Aggregate rapid machine-gun hits into a single updating value to avoid clutter. |
| F03 | Tower upgrade celebration | **Partially implemented on dev:** aqua purchased-level and gold earned-rank callouts with a thick up-arrow, reusing `FEEDBACK/WorldCallouts/`. Additional pulses, glow and holograms remain ideas. |
| F04 | Better enemy destruction | Small ships break into short-lived glowing fragments. Large ships show staged internal blasts before destruction; bosses may briefly leave cosmetic wreckage or a shockwave. |
| F05 | Dynamic wave announcements | Animated incoming-wave HUD cue; distinct elite and boss-wave variants. |
| F06 | Visible heat and damage states | Barrel glow rises with heat; overheated towers vent; damaged towers spark; disabled towers visibly flicker or shut down. Reuse existing heat and generic disable states. |
| F07 | Kill streak feedback | Small, primarily cosmetic combo indicator for rapid kills. |
| F08 | Citadel impact reaction | Health bar flash, Citadel shield ripple and short breach warning. Complement existing critical-health alerts rather than duplicating them. |
| F09 | Tower targeting indicator | Selected tower optionally shows a subtle current-target line or reticle. |
| F10 | Contextual event feed | Compact, prioritized messages such as SHIELD BROKEN, TOWER DISABLED, RANK UP and BONUS CREDITS; avoid notification spam. |
| F11 | Tower efficiency indicators | Inspector shows recent DPS, kills, shield damage and firing uptime, using bounded aggregated telemetry. |
| F12 | Audio reaction layers | Distinct sounds for shield breaks, armour hits, rank-ups, heavy enemies and wave success. Reuse shared AUDIO playback with clips owned by features. |
| F13 | Portal activation animation | Portal charges, distorts and glows briefly before a wave begins. |
| F14 | Construction completion burst | Existing construction drones finish with a final beam pulse and tower activation effect. |
| F15 | Wave completion summary | Brief summary of enemies destroyed, credits earned, Citadel damage and best-performing tower. |

**Suggested first visual milestone:** F01, F03, F04 and F13. These improve moment-to-moment feel without changing combat balance.

## 2. New features and gameplay mechanics

Ideas for tactical depth, replayability, tower variety and enemy behaviour. Some are inspired by the original Vector Tower Defense notes and some may overlap with existing plans or partial systems.

| ID | Idea | Proposed behaviour |
| --- | --- | --- |
| M01 | Tower synergy and adjacency bonuses | Selected combinations create bonuses, e.g. Laser against Cryo-affected enemies, or nearby Energy Generators boosting towers with diminishing returns. Reward thoughtful placement. |
| M02 | Enemy support ships | Shielding ships reinforce allies; repair ships restore armour; command ships boost speed; jammers disrupt targeting. Original notes already include Shielding, Virus and Electro ship concepts. |
| M03 | Player-controlled battlefield abilities | Cooldown- or credit-funded orbital strike, emergency Citadel shield, tower overcharge or emergency repair-drone dispatch. |
| M04 | Specialized tower upgrade branches | At a purchased upgrade level, choose between specializations such as Battle Tower Armour Piercing vs Suppressive Fire. Keep XP/rank progression separate. |
| M05 | Carrier and splitting enemies | Carriers deploy limited fighter groups while travelling; some enemies split into weaker ships when destroyed. |
| M06 | Multi-phase boss ships | Boss behaviour changes with health thresholds, e.g. Decimator shield-generator loss, acceleration and escort deployment. |
| M07 | Enemy resistances and vulnerabilities | Ship types have distinct damage-type strengths and weaknesses to reward mixed defences. Check current typed-damage rules first. |
| M08 | Tower targeting priorities | Closest, first, last, strongest, weakest and specialist targeting; extend/verify existing targeting work rather than rebuilding it. |
| M09 | Tower ammunition logistics | Supply drones deliver ammunition from production buildings to towers, making supply and placement matter. Check existing cargo/logistics ownership. |
| M10 | Random mission modifiers | Mission-specific faster enemies, stronger shields, slower construction or other disclosed modifiers. |
| M11 | Research loadout slots | Select a limited set of unlocked research effects before a mission, inspired by the original research-inventory design. |
| M12 | Salvage drones | Enemy wreckage occasionally yields collectible bonus credits gathered by drones. |
| M13 | Environmental hazards | Solar storms, radiation zones or asteroid fields affect tower and enemy behaviour. |
| M14 | Tower active abilities | Selected towers can overcharge, focus fire or enter a defensive mode for a limited time. |
| M15 | Enemy formations | Squadrons enter with leaders and supporting craft; formations create tactical targeting choices. |
| M16 | Endless survival mode | Optional escalating waves, rewards and score records. |

## Implementation guidance

- **Existing vs new:** The IDs are idea references, not implementation passes or claims of missing functionality. Verify HEAD, the implementation backlog and feature documentation before selecting work.
- **Ownership:** Cosmetic shared effects belong under `FEEDBACK/`, UI under `UI/`, audio playback under `AUDIO/`, tower behaviour under `TOWERS/`, enemies under `ENEMIES/`, and shared rules under their existing owners.
- **Performance:** Avoid per-hit/per-projectile scene nodes and full-map polling. Aggregate frequent numbers, cap/pool effects, sleep idle systems, and use events and cached data where possible. Don't add visible feedback delays solely to reduce spikes.
- **Scope:** Implement ideas as separate focused tasks, with Inspector-editable tuning and a brief source/build sanity check where practical. Owner performs detailed Godot visual/performance acceptance.
