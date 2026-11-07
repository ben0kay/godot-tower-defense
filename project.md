# Godot Tower Defense — Project Overview

> **A 2D sci-fi tower-defense game, built in Godot 4 .NET / C#.**
>
> **Active development:** [dev](https://github.com/ben0kay/godot-tower-defense/tree/dev) is the working branch; main is kept stable. This page describes the game and its direction, not the day-to-day commit history. Implemented features described below are present in source; gameplay, visual quality, balance and performance still require ongoing playtesting.

## What is the game?

The player is a **Commander** defending remote futuristic outposts from hostile space fleets that emerge through dimensional portals. The objective is to protect the **Citadel**, the heart of each defensive position, until the enemy assault is defeated. Some maps can feature multiple defended Citadels.

This is a strategic, top-down **2D tower defense game**. It combines familiar tower placement and timed enemy waves with layered military logistics: physical ammunition deliveries, energy generation and distribution, construction drones, deployable fighter drones, interactive terrain and persistent research.

The intended feeling is **a working sci-fi military defence network**, not merely an abstract grid of guns. Towers have operational needs, routes and terrain matter, and enemy fleets gradually demand more specialized counters. The art direction favours clean, detailed 2D military hardware, dark planetary surfaces and space scenery, cyan/blue friendly systems, red hostile weapons, violet portal energy and restrained lighting and HUD effects.

## Core gameplay loop

1. **Deploy to a mission.** Inspect the map, its portals and terrain, the available structures and any mission intelligence.
2. **Build a defence.** Spend Credits on offensive towers, support structures, foundations, power generation, ammunition infrastructure and drones. Construction takes place in the battlefield rather than appearing instantly.
3. **Defend against waves.** Start the first wave when ready, then respond to successive enemy groups, special abilities and larger ships. The normal next-wave countdown allows early starts.
4. **Keep the network operating.** Replace ammunition, maintain energy access, upgrade and repair damaged structures, reposition strategy through terrain, and deploy fighters where useful.
5. **Complete the mission.** Defend the Citadel through the final authored wave, review the Results screen, and carry profile progression and eligible rewards into future deployments.

Defeat occurs when a required Citadel is destroyed. The exact enemies, wave composition, objectives and environmental layout belong to each authored mission.

## Tactical systems

### Towers and defences

The tower roster spans rapid-fire kinetic weapons, specialist anti-shield and anti-armour weapons, missiles and splash damage, slowing and stasis effects, shockwaves, longer-range precision fire and damage-over-time effects. Current examples include **Battle Tower, Machine Gun Tower, Laser Tower, Blaster Cannon, Rocket Tower, Cryo Tower, Stasis Tower, Sniper Tower, Shockwave Tower and Corruptor Tower**. Support options include the **Restoration Tower** and **Stealth Beacon**, plus economic and logistical buildings.

Towers have authored statistics, purchased levels, earned combat ranks and shared mechanics for targeting, durability, construction, upgrades, damage types, ammunition, energy and (where enabled) heat/disable conditions. Enemy shields and hull can respond differently to damage types. Some buildings can be sold for a health-adjusted partial refund.

The player's objective is to design a *combined defence*, using different towers for distinct battlefield roles rather than depending on a single best weapon.

[Detailed tower systems](TOWERS/README.md) · [Shared combat](COMBAT/README.md)

### Enemies and fleets

Enemies enter through portals or authored spawn sources and follow routes toward their assigned Citadel. Ships vary by speed, hull, shields, roles, equipment and special capabilities. The roster includes small scouts and drones, armoured and shielded ships, support/carrier variants, armed gunships and larger **Capital** threats. Enemy metadata distinguishes **Class, Rank and Role** from each ship's actual combat capabilities.

Stealth, shield interactions, kinetic weapons, special effects and map geometry add tactical decisions. Large named threats such as the **Obsidian Dreadnought** demonstrate the direction for heavier multi-weapon ships. Missions use authored waves, with a dedicated Godot Wave Editor for designers and a debug spawner for stress-testing compositions.

The longer-term enemy vision is broader fleets with distinct specialties, more advanced support behaviour, substantial capital engagements and memorable boss encounters. Those future additions are not claims of current completion.

[Enemy systems](ENEMIES/README.md) · [Waves](LEVELS/Waves/README.md)

### Terrain, foundations and navigation

Battlefields are **hand-authored maps** in the Godot editor, with reusable shared systems for tiles, structures, portals, objectives and navigation. The map is part of the strategy:

- **Buildable and unbuildable ground** determines where structures can be deployed.
- **Foundation platforms** can enable building on otherwise unsuitable routes and redirect enemy movement.
- **Lava** is a hazard that can damage passing enemies.
- **Walls, obstacles and mountains** influence pathfinding, placement and weapon sight lines.
- **Portals** can appear or become active at authored stages of a mission, creating shifting pressure.

Enemy navigation and tower placement preserve shared route/occupancy rules. Terrain art, gameplay flags and cosmetic effects remain separate, enabling different planet/map styles without recreating the combat rules.

[World systems](WORLD/README.md) · [Level authoring](LEVELS/AUTHORING.md)

### Economy, ammunition and power

**Credits** pay for construction, upgrades and other eligible battlefield costs. The Citadel and economy buildings can produce additional income; some rewards come from enemy kills. The same shared wallet and transaction rules apply across the game.

**Ammunition is physical logistics.** Factories produce stock; transport drones carry it to depots; further courier flights deliver rounds to ammunition-consuming towers and Drone Stations. Stocks, flight time, requests, reservations and capacity are part of the playable network rather than invisible automatic refills.

**Energy is another operating constraint.** Citadels and dedicated **Industrial, Solar and Fusion** generators contribute power. Conductive terrain/islands, capacitors, distance-dependent delivery, Relays and linked Transmitters influence whether consumers receive sufficient energy. Active and idle energy use can be configured per applicable structure.

The design goal is meaningful logistics and infrastructure planning while retaining a straightforward, approachable tower-defense experience.

[Economy](ECONOMY/README.md) · [Ammunition](TOWERS/Economy/Ammunition/README.md) · [Power](TOWERS/Economy/Power/Shared/README.md)

### Friendly drones

Different drones have different jobs and share presentation where sensible:

- **Construction drones:** fly from Citadels to building sites, beam-build or perform upgrades, then return.
- **Ammunition couriers:** physically transport stock along factory, depot and consumer legs.
- **Fighter drones:** deploy from Drone Stations, fly defensive interception patterns, shoot eligible threats, return when low on ammunition or health, and use station stock/service bays to rearm or repair.

Construction and resupply are intentionally separate responsibilities. Drone flight, service, ammunition ownership and checkpoints are source-implemented; broader direct fighter orders/patrol modes and other advanced drone abilities remain areas for future work.

[Drone systems](DRONES/README.md) · [Fighter drones](DRONES/Combat/README.md)

## Campaign and progression

The game has a **profile-based campaign** with a selectable mission roster. Each mission owns its map, waves, settings and narrative briefing while sharing the battlefield systems. Missions range from introductory training encounters to larger multi-portal fleet battles; some numbered maps also serve as authoring or stress-test scenarios.

There are two separate progression tracks:

- **Player progression:** mission Results can grant persistent profile XP, levels and Research Points. The Campaign Research screen provides permanent upgrades applied to subsequent deployments. The research tree is currently a small functioning foundation, with a larger multi-division technology tree planned.
- **Tower progression:** individual placed towers purchase levels using in-mission resources and earn combat **ranks** through damage and kills. Their earned XP and purchased upgrade levels are distinct from player XP and permanent research.

The **Results screen** reports mission performance and saved XP/rewards. A campaign checkpoint system supports **between-wave Save/Load and Continue**; saving is unavailable during combat. It is not an unrestricted mid-battle save system. Campaign unlock rules, more advanced reward scoring and the enlarged research tree remain future design and implementation work.

[Campaign and missions](LEVELS/README.md) · [Research](SYSTEMS/Progression/Research/README.md) · [Saving](SYSTEMS/Saving/README.md) · [Results](UI/Menus/Results/README.md)

## Atmosphere, interface and audio

Presentation is important to the game's identity. The current foundation includes:

- A dark sci-fi HUD, selected-tower Inspector, build tray, debug tools and visual battlefield feedback.
- Native 2D light pools on eligible towers, Citadels, drones, projectiles, portals and lava, with darkness/reveal mechanics and red radar contacts. Shadow expansion remains planned.
- Animated construction indicators, weapon tracers, muzzle effects, recoil, portals and other visual effects with performance limits.
- A level-loading HUD with real resource-loading progress and optional large artwork.
- Mission briefings and wave transmissions in a top-right **typewriter narration card**.
- A recorded female AI-style **voice assistant** that addresses the player as Commander, with reusable audio playback, mixing and narration synchronization for supported lines.
- Victory/defeat debrief presentation with skippable statistic reveals.

Not every planned announcement or sound has a recorded cue. In particular, a randomized voice library for victory commentary is a **proposed future addition**, not a feature to assume already exists.

[Audio](AUDIO/README.md) · [Narration](UI/Narration/README.md) · [Lighting](SYSTEMS/Lighting/README.md) · [Results UI](UI/Menus/Results/README.md)

## Game direction and design principles

**Near-term goal:** finish and polish the standalone tower-defense game. New content and systems should increase decision-making, battlefield clarity and performance without burying the core experience in unnecessary micromanagement.

- **Tactical depth without needless complexity.** Resources, construction, power, ammunition and battlefield geometry should matter, but early missions should remain understandable.
- **Reusable systems, varied content.** New towers, ship types, tile effects and missions should author data and visuals against shared mechanisms, not duplicate whole subsystems.
- **Dense combat at practical frame rates.** Targeting, pathfinding, logistics, particles, lights and projectiles must scale to significant fleets through batching, pooling and event-driven/budgeted work where appropriate.
- **A consistent art language.** Futuristic military machinery, legible effects, crisp low-noise HUD presentation and atmospheric sci-fi environments.
- **Grow through playtesting.** Balance values, rendering choices and complexity are provisional until the owner confirms them in Godot.

Source implementation and gameplay acceptance are distinct. Check the feature's current code and documentation rather than treating any older plan or dated progress note as universally current.

## Longer-term universe — optional hybrid project

The original broader ambition is a **connected sci-fi campaign/empire** across three modes. This is a vision, **not an implementation mandate**:

1. **Tower Defense — current project:** protect installations, defeat waves, use specialized defenses and earn resources/progression.
2. **Idle / Empire Management — possible future main hub:** mine and process resources, manufacture equipment, manage facilities, research technology and maintain strategic inventories.
3. **Space Shooter — possible future mode:** directly explore, fight and collect resources in an action-focused spacecraft game.

A future shared campaign could connect equipment production, mission deployment, research and cross-mode rewards. Its economy and save model must be designed when that work is actually authorized. **Do not prematurely combine repositories, move the current TD UI or create unused cross-genre frameworks.** This game must remain complete and independently playable.

The childhood GameMaker-era tower-defense concept remains an inspiration for the setting, fleets, tower technology and expansion ideas; the modern Godot systems and current authored content are the source of truth for implemented behaviour.

## Getting started

**Engine:** Godot 4 .NET with C# (currently configured for Godot **4.7**). Build the C# project in the editor.

- **Run the game:** press **F5** in Godot. Choose or create a local profile at Boot, then open **Campaign** and select a playable mission. Campaign PLAY uses the dedicated loading screen.
- **Direct testing:** open **Main.tscn** or another supported battlefield launcher and use Godot's **F6 — Run Current Scene**. This can bypass the normal profile/menu flow.
- **Battlefield controls:** WASD/arrows move the camera; middle mouse pans; wheel zooms; **Ctrl** opens the build tray; select/place structures with the mouse; **Escape** cancels a placement or opens Pause.
- **Waves and debugging:** START WAVE launches the first wave; later waves can auto-start after their countdown. **F2** opens enemy-spawn tools, **F3** previews routes, **F4** shows diagnostics, **F6 while already in a debug battlefield** toggles lighting comparison, and **F11** toggles fullscreen. Debug commands depend on build/scene.

## Repository guide

| Location | Purpose |
| --- | --- |
| [CodePreferences.md](CodePreferences.md) | Authoritative coding, performance, validation and branch workflow rules. |
| [devlog.md](devlog.md) | Archived chronological root README reports; entries can describe superseded stages. |
| [NOTES/WORK.md](NOTES/WORK.md) | Ongoing implementation notes and most recent development receipts. |
| [NOTES/IMPLEMENTATION_BACKLOG.md](NOTES/IMPLEMENTATION_BACKLOG.md) | Outstanding work and proposed next slices; verify against source. |
| [LEVELS/](LEVELS/README.md) | Authored missions, wave data, map authoring and session outcomes. |
| [TOWERS/](TOWERS/README.md) | Tower content, construction, upgrades, power, ammunition and shared components. |
| [ENEMIES/](ENEMIES/README.md) | Enemy content, movement, AI, effects and spawning. |
| [DRONES/](DRONES/README.md) | Shared flight presentation and separate construction, transport and combat roles. |
| [WORLD/](WORLD/README.md) | Terrain, routing, portals, Citadels and backdrops. |
| [COMBAT/](COMBAT/README.md) | Shared damage, attribution and area-combat rules. |
| [ECONOMY/](ECONOMY/README.md) | Credits, transactions and production. |
| [AUDIO/](AUDIO/README.md) | Shared audio catalog and voice playback. |
| [UI/](UI/README.md) | Menus, HUD, narration, loading and Results. |
| [SYSTEMS/](SYSTEMS/README.md) | Persistent profiles, saving, player progression and shared systems. |
| [TESTS/](TESTS/README.md) | Targeted integration and subsystem checks. |

**For future AI/developer work:** read **project.md**, then **CodePreferences.md**, then the owning feature documentation and actual **dev** source. Consult the backlog for planned work and WORK for recent changes. Keep ordinary implementation reports out of this overview so it continues to describe the game rather than becoming another devlog.
