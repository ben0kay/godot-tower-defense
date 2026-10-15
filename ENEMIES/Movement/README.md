# Enemy movement and route refinement

EnemyMovement.cs follows shared weighted A* routes, preserving editable speed, radius and smooth facing. EnemyRoute.cs caches waypoint suffix distances and swept cell-to-segment dependencies. Physical RemainingPathDistance drives First tower targeting; weighted preference costs are separate.

On assignment, EnemyRoute applies one stable per-spawn lateral bias, up to 8 pixels by default. Interior corner directions use the incoming/outgoing bisector. Offset points and connecting segments must pass HasClearance and SegmentClear; unsafe offsets are halved until safe, or kept at the original center. Spawn and arrival endpoints stay on their logical route. No per-frame random jitter, steering physics, extra A* or local ship separation is added.

Each physics frame checks only immediate swept movement clearance plus a cheap audit timer. New blocked cells mark indexed remaining route dependencies, with passed cells ignored. Central bounded validations verify only relevant segments. Unaffected ships keep moving; confirmed affected ships stop and queue prioritized A*. If an immediate segment becomes unsafe before its queued check, the ship stops immediately and upgrades to urgent work.

Optional periodic audits default to three seconds with per-ship initial staggering. They inspect cached cell dependencies and perform geometric checks near blockers; a valid route never triggers A*. No-route cases retry through the path manager. Removing obstacles does not force valid ships to recalculate shorter paths. SeparationStrength/Radius remain inactive future settings.

## Moving replacement-path handoffs

Confirmed blockage now queues a replacement without stopping safe movement immediately. Ships retain old waypoints while waiting, with strict immediate swept/corner checks; they stop only when the next movement would be unsafe. Queue urgency follows the shrinking distance to the obstruction. Failed replacement searches retain any safe old prefix and use the existing retry interval.

Each multi-goal request uses one position snapshot, so its candidate costs are comparable while the ship moves. When the result arrives, EnemyMovement joins it near the ship's current progress, preferring a forward clearance-tested waypoint. A safe two-leg elbow can turn around a nearby wall without returning to the cell center. Both legs obey swept clearance and corner-safe cell transitions. If reconnection fails, a fresh request uses the same centralized budgets. Backtracking remains possible when the actual map requires it.

Safe offset refinement cannot turn the first new segment backward merely due to lateral bias. Existing speed/turning/offset settings, weighted profiles, congestion, auditing and count/time budgets are preserved. First targeting excludes routes known to need replacement until a valid handoff completes. No new pathfinding algorithm or per-frame A* is added.

## Phase N4 implementation

`CorridorPreference` assigns lifetime selection and a stable seed independently from shared weighted profile assignment. `EnemyMovement.AcceptPath` calls the original `EnemyRoute.Build` unchanged for master-off, zero-strength and unselected ships; selected ships use `BuildCorridor`. A zero selection percentage only affects future spawns.

`CorridorRouteRefinement` probes both sides of sparse anchors (at most seven probes per side, bounded to two cells), combines stable per-ship bias with smooth spatial noise, then interpolates/tapers lateral changes. Each proposed link must preserve forward progress, remain at most 1.35× its logical incoming length and pass radius/corner clearance. Unsafe proposals are reduced or use the baseline. Endpoints stay unchanged; short handoff elbows and narrow passages retain the existing route where safe. No per-frame RNG, A* requests, weight mutation or extra steering is introduced.

`CorridorClearance` supplements existing navigation checks with analytic segment-to-solid-rectangle distance for selected route connections/handoffs. This closes a sampled-clearance grazing case that could leave a ship safely stopped but repeatedly repathing beside a tower corner. It runs only during route construction/connection, not normal physics movement.

`EnemyRoute` caches physical suffix distances and swept cell dependencies from the actual traveled chain for First targeting and selective revision handling. Selected routes additionally cache diagonal transition side cells from that actual geometry, preserving strict corner rules after tower placement without new per-frame route scans. Existing immediate clearance, safe-prefix movement, prioritized handoff and staggered audits remain authoritative.

Configuration changes are read at the next accepted route. They do not invalidate all routes or synchronously rebuild them. Existing lifetime selection survives repaths and live toggle changes. Path assignment has measurable extra CPU cost; the path-manager count cap is retained and its millisecond budget remains soft because an individual assignment cannot be interrupted. See TESTS/CorridorPerformance.md.

## Planned pass — Per-enemy movement variation permissions (NOT IMPLEMENTED)

**Status: PROPOSAL FOR A SINGLE CODEX IMPLEMENTATION PASS.** Keep existing navigation fully functional; this README change is documentation only. The goal is to make each enemy type **eligible or ineligible** for three independently existing global movement systems, without introducing whitelists, enemy-specific scripts or per-enemy A* grids.

### Design and Inspector authoring

Add three **Inspector-editable bools** to the existing \`ENEMIES/Shared/EnemyDefinition.cs\` resource, under an **"Movement Variation"** export group:

\`\`\`csharp
[ExportGroup("Movement Variation")]
[Export] public bool AllowCorridorVariation { get; set; } = true;
[Export] public bool AllowPathOffsets { get; set; } = true;
[Export] public bool AllowPathDiversity { get; set; } = true;
\`\`\`

Each existing/new enemy type can set its own independent values in \`ENEMIES/Types/<EnemyType>/<EnemyType>.tres\`. **All default to true** for backward compatibility. Do not change the existing Enemy001, Enemy002 or Enemy004 behaviour unless a particular type is deliberately configured later. The authored .tres controls permissions, not the scene/script class name, sprite size, EnemyId lookup, or a centralized whitelist/blacklist.

**Global controls in \`ENEMIES/EnemyMasterConfig.cs\` are always authoritative:**

| Master control | Enemy permission | Result |
| --- | --- | --- |
| Disabled | Allowed or disallowed | Variation is off |
| Enabled | Disallowed | Variation is off for that enemy |
| Enabled | Allowed | Enemy is eligible for that feature |

An allowed setting does **not** force variation on when the master control is disabled. Existing sliders, percentages, strength, range, seeds and route budgets remain global. For Corridor Variation, \`CorridorVariationPercent\` continues to select a deterministic subset **of eligible spawned enemies**; ineligible enemies never run N4 refinement. Do not reroll an enemy's lifetime selection each time it repaths.

### Three independent gates and where to implement them

1. **Corridor Variation:** in \`ENEMIES/Movement/EnemyMovement.cs\` / \`CorridorPreference.cs\`, require both \`Enemy.Definition.AllowCorridorVariation\` and existing master \`EnableCorridorVariation\` plus existing percentage/strength rules. Disallowed enemies use the ordinary \`EnemyRoute.Build\`, not \`BuildCorridor\`, and skip N4 expensive width probes/extra strict corridor-only connection checks. Keep lifetime deterministic \`CorridorSeed\` and \`UsesCorridorVariation\` stable across repaths. The current master resource may explicitly enable corridors even though the C# default is false; **do not silently change that authored value**.
2. **Path Offsets:** in the existing \`EnemyMovement.AcceptPath\` offset selection, require both \`Enemy.Definition.AllowPathOffsets\` and global \`EnablePathOffsets\`. When disallowed, pass offset **zero** to whichever route builder is active; preserve route endpoint, radius clearance, corner checks and stable pre-existing offset generation for allowed enemies. A ship **may enable corridor variation but disable ordinary offsets**, or vice versa.
3. **Path Diversity:** in \`EnemyMovement\`, \`WORLD/NAVIGATION/PathRequestManager.cs\` and \`NavigationGrid.cs\`, let eligible enemies use the same shared weighted A* profile pool when global \`EnablePathDiversity\` is on. Ineligible enemies use **one shared, truly neutral/unweighted A* profile** (ordinary shortest-path grid costs), without duplicating it per enemy or altering other ships' weighted profiles. Important: **current profile 0 is weighted when diversity is on**; assigning 0 is not enough. The neutral profile must retain the exact same blocked-cell, diagonal-corner, map-revision and incremental occupancy updates as other profiles and be considered correctly during route-cost comparisons; no congestion/diversity modifiers should accidentally make it non-neutral. Keep \`PathRequestManager\` A* budgets, priority queue, multi-goal search and route handoff behaviour intact.

**A different setting is not an alias for another:** corridor spreading is geometric N4 refinement, offsets are small lateral waypoint shifts, and path diversity is the initial weighted A* route selection. An enemy with all three false follows an unoffset, unvaried **shortest navigable route** (subject to its own start, goal, size/clearance and map obstacles); do not promise a fixed designer-authored centerline or identical paths across different portals.

### Performance and lifecycle rules

- Resolve each type's permissions once at initialization or when an accepted route needs its cached settings. Avoid reflection, global enemy scans, new per-frame checks, additional A* requests, per-enemy A* instances or three duplicated pathfinding systems.
- Preserve the existing deterministic corridor-assignment seed stream and the existing weighted profile assignment for eligible enemies; do not perturb wave composition RNG or silently reroll selection on repath.
- **Master toggles stay in full control.** Follow current live-config semantics: master changes take effect at the next accepted route where relevant, rather than synchronously invalidating and recalculating every active ship; corridor lifetime eligibility/selection remains stable.
- RadiusPixels is a **physical clearance** property, not an automatic flag for variation. Bigger ships can be configured false/false/false, but still require the existing clearance/path safety checks. No special cases keyed by EnemyId or size.
- Preserve enemy spawning, waves, F2/debug, damage, towers, spatial target queries, interactive-terrain work, death, rewards and navigation safety. Keep this a small opt-in gating/profile change only.

### Acceptance tests

- With each global master OFF, both permission values yield no corresponding variation. With master ON, a per-enemy false always disables the feature; true preserves the original behaviour.
- Cover all **eight combinations** of the three permission flags, including Corridor ON + Offsets OFF and Corridor OFF + Offsets ON; confirm results in a mixed wave of existing enemy definitions and an extra type with all false.
- Test global Corridor \`0%, 50%, 100%\` with mixed eligible/ineligible populations; ensure ineligible ships are never selected, allowed selection remains reproducible and remains stable on forced repaths.
- Verify disabled Corridor actually bypasses N4 sampling, disabled offsets yield zero lateral bias on accepted paths, and disabled diversity never selects a weighted profile even when diversity is globally enabled.
- Verify the new neutral profile follows shortest unweighted paths, **not weighted profile 0**, with map updates/tower placement, different goals, large valid radii, movement clearance and path rerouting; mixed neutral + weighted enemies do not interfere with each other.
- Run existing \`CorridorAssignmentChecks\`, \`CorridorNavigationChecks\`, \`RepathMovementChecks\`, navigation/placement/performance tests and full enemy integration suite. Compile in Godot .NET; report actual test outcomes and any measurable performance impact.
- Keep \`EnemyMasterConfig\` authored settings and existing .tres scene content stable unless required for the implementation. Update \`ENEMIES/Shared/README.md\`, this README and \`WORLD/NAVIGATION/README.md\` to describe **implemented** behaviour after successful tests.

### Copy-ready Codex request

> After the current work is complete, implement the **Per-enemy movement variation permissions** pass defined in \`ENEMIES/Movement/README.md\`. Read latest GitHub HEAD and \`NOTES/CodePreferences.md\` first. Add Inspector booleans \`AllowCorridorVariation\`, \`AllowPathOffsets\`, and \`AllowPathDiversity\` (all default true) to each enemy type via \`EnemyDefinition\`. Global master toggles and corridor percentage retain control. Preserve independent corridor/offset behavior and deterministic spawn/repath selection. Use one shared neutral **unweighted** A* profile for enemies disallowing path diversity; profile zero is currently weighted and is not sufficient. Preserve A* budgets, clearance, route handoffs, wave RNG, tower combat and all existing enemy types. Add the specified mixed-type/eight-combination tests, compile/run the existing navigation and integration suites, update documentation, and commit/push. Do not rewrite the route engine.
## Swept Interactive Terrain

Each clearance-approved movement segment reports its actual endpoints, travel duration (distance/speed) and offset within the physics tick to the world's InteractiveTileManager when Lava exists. A lethal entry/periodic encounter stops the loop before committing the planned endpoint. Ordinary movement retains the shared registry's spatial update; direct position assignments remain teleports and do not traverse intervening hazards. The late terrain manager accounts for only the remaining stationary time of active occupants, including stopped/waiting enemies. No Lava routing weights or A* changes are introduced. See WORLD/TERRAIN/Interactive/README.md.
