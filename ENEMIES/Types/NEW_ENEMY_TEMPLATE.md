# NEW ENEMY — AI authoring guide & design template

**Purpose:** Use this file when asking Codex or another AI to create, modify or plan one enemy in `ben0kay/godot-tower-defense` (Godot 4 C#). This is an authoring guide and a **copyable brief**, not executable configuration or an instruction to create every possible feature.

**Before editing:** Read `NOTES/CodePreferences.md`, the root `README.md`, this folder's `README.md`, [ROSTER.md](ROSTER.md), [ART_DIRECTION.md](ART_DIRECTION.md), `ENEMIES/Shared/EnemyDefinition.cs` and the closest existing enemy type. Work on **`dev`**, preserve unrelated systems and make only the requested changes. If the user only asks for a design, do not implement.

## 1. The three independent classifications

These are **descriptive metadata**, not a rulebook or automatic stat multipliers. Each enemy has its own `EnemyDefinition` with **Class**, **Rank**, and **Role**. Existing Godot enums (not the retired `EnemyFamily`) are authoritative:

| Field | Current values (in enum order) | Meaning |
| --- | --- | --- |
| **Class** | `Tiny`, `Light`, `Standard`, `Heavy`, `Superheavy`, `Capital`, `Titan` | Broad physical size / screen presence |
| **Rank** | `Common`, `Veteran`, `Elite`, `Champion`, `Miniboss`, `Boss` | Threat or encounter importance |
| **Role** | `Fighter`, `Cargo`, `Miner`, `Support` | Primary purpose or battlefield job |

- **Class ≠ speed or health.** A giant ship can move quickly. A Tiny enemy can be a Boss. There are no automatic class-based sprite sizes, pathfinding footprints or health thresholds.
- **Rank ≠ upgrades.** Rank currently does not automatically grant damage, stats, rewards or special mechanics. A `Capital/Common` and a `Tiny/Boss` are valid if deliberately designed.
- **Role ≠ locked abilities.** A Cargo ship can fire weapons or provide shields; a Fighter can spawn escorts; an anomaly ship can use any suitable existing role and have unusual custom abilities. Don't add a new enum just because an ability is unfamiliar.
- **Mk.1 / Mk.2 / Mk.3 is a separate concept**, not a fourth implemented classification. For example, Reinforced Drone can be treated creatively as a more advanced Standard Drone line without making Rank or Role dictate Mk progression. New Mk variants get their own stable definition/ID and deliberate assets/settings when approved.
- **Future expansion:** if a genuinely new Class/Rank/Role is needed, discuss it with the owner and append to enums without renumbering existing serialized values. Avoid hardcoded switch-based gameplay restrictions.

**Current examples (physical-size classes):** Standard Drone = Tiny / Common / Fighter (32×24px); Shield Emitter = Light / Common / Support (40×32px); Elite Assault Ship = Standard / Elite / Fighter (80×60px); Drone Transporter = Superheavy / Common / Cargo (112×72px); Heavy Transporter = Superheavy / Veteran / Cargo (144×100px); Cruiser = Superheavy / Elite / Fighter (168×96px). **Capital (256–511px longest hull dimension)** and **Titan (512px+)** are intentionally reserved for much bigger future ships. Select Class from the native hull SVG's longest dimension using the suggested size ranges in [ROSTER.md](ROSTER.md), not from health, speed, danger or names like “Heavy Gunner”. These are Inspector-authored guidelines, not an automatic runtime sizing system. The fleet has no Titan, Capital, Miniboss, Boss, Champion or Miner at the time of this update; that is intentional.

## 2. Copy/paste this brief when planning an enemy

Fill what you know; mark the rest **TBD / AI propose** rather than inventing a final decision. Examples in parentheses are prompts, not defaults.

### Identity and progression

| Question | Answer |
| --- | --- |
| Proposed display name | **[name]** |
| Stable enemy ID | **[unique snake_case ID; e.g. new_ship_mk1]** |
| New design or variant of an existing ship? | **[new / Mk.2 of ... / specialized variant]** |
| Class | **[Tiny / Light / Standard / Heavy / Superheavy / Capital / Titan]** |
| Rank | **[Common / Veteran / Elite / Champion / Miniboss / Boss]** |
| Role | **[Fighter / Cargo / Miner / Support]** |
| Intended campaign introduction / purpose | **[level, encounter, counterplay; do not edit waves unless asked]** |
| Closest existing enemy to reuse | **[type name or TBD]** |

### Visual identity

| Question | Answer |
| --- | --- |
| Hull silhouette & theme | **[e.g. narrow, bulbous, angular, organic, anomalous]** |
| Relative scale | **[smaller/bigger than which existing ship?]** |
| Native artwork canvas / expected displayed size | **[width × height px, or TBD]** |
| Direction / thrusters | **[+X forward by current fleet convention; nozzle positions]** |
| Hull colors, lights & distinct features | **[description or reference]** |
| Weapon hardpoints / movable parts | **[count, positions, fixed vs independent rotation]** |
| Damage appearance & death visuals | **[shared defaults / request unique feedback]** |

### Stats and mechanics (provisional until approved)

| Setting | Answer |
| --- | --- |
| Hull HP | **[value / TBD]** |
| Shield capacity | **[value / none / TBD]** |
| Armor | **[value / TBD]** |
| Move speed & turn speed | **[px/s, turn speed / TBD]** |
| Navigation clearance radius | **[pixels / TBD; check actual paths]** |
| Citadel arrival damage | **[value / TBD]** |
| Kill Credits | **[value / TBD]** |
| Status immunities | **[none / list / TBD]** |
| Standard routing / movement variation | **[reuse defaults / specific exception]** |
| Actual abilities | **[none / shields / death-spawn / weapon / custom, with clear triggers]** |
| Attack target & weapon behaviour (if armed) | **[target, range, damage, rate, bullet/beam, hardpoint, cooldown]** |
| Special interactions & counterplay | **[stealth, EMP, disabling, splitting, teleporting, etc. ONLY if requested]** |
| Desired F2 debug test | **[amount, location/portal setup, performance expectations]** |

**Description of desired experience (2–5 sentences):** [How should it look and feel when entering the map, moving, fighting/acting and being destroyed? Why is it different from existing enemies?]

**What NOT to change:** [any explicit exclusions, e.g. wave schedules, other enemy stats, towers, shared navigation].

## 3. AI implementation checklist (only when implementation is authorized)

1. **Inspect the nearest example first**, rather than copying an unrelated enemy. Reuse `ENEMIES/Shared/GenericEnemy.tscn`, the shared enemy scripts, visuals, movement and the existing optional components as appropriate. No duplicate whole-system enemy movement/health scripts.
2. **Create a descriptive type folder** under `ENEMIES/Types/<DescriptiveName>/` with its own scene, `EnemyDefinition` `.tres`, artwork and a short `README.md`. Use meaningful names for *new* assets; don't rename the legacy `Enemy001`…`Enemy006` directories as an accidental side effect.
3. **Assign a unique stable `EnemyId`**, readable `DisplayName` and explicit **Class / Rank / Role** in the definition. IDs are wave/catalog/save lookup keys; never reuse an old ID or change another type's identity. Class/Rank/Role must not silently rebalance anything.
4. **Set the editable stats in the per-type resource**, not hardcoded inside shared code. Check against existing similar types for reasonable initial playtest values; highlight proposed figures as provisional. `RadiusPixels` currently has an Inspector range of 1–24; its allowed navigation clearance is not automatically enlarged when the sprite gets bigger.
5. **Create distinctive artwork** following [ART_DIRECTION.md](ART_DIRECTION.md): consistent enemy fleet aesthetic, readable silhouette at native size, forward nose in **+X**, no baked projectile/shield/particle effects. The flat charcoal/red look applies to selected existing types, not automatically to every asset; follow the owner's references. For large ships, explicitly review wing/body clipping in narrow paths because current circular navigation clearance can be smaller than the visible hull.
6. **Compose requested abilities from existing opt-in systems**. Examples: [DeathSpawn](../Abilities/DeathSpawn/README.md) for death releases, [enemy weapons](../Weapons/README.md) for armed ships, [shared thrusters](../Shared/Visuals/Thrusters/README.md) for the standard rear effect. Ordinary enemies should not acquire processing loops, weapon components or special effects they don't need. Never pretend a future anomaly/EMP/teleport mechanic is already implemented without checking code.
7. **Register the actual scene** in `ENEMIES/DefaultEnemyCatalog.tres`; it supplies F2/debug and wave authoring by canonical ID. Do not create an alternate registry or hardcode enemy names into the spawner. Keep existing registration order and IDs unless the user specifically requests otherwise.
8. **Keep campaign authorship separate.** Adding an enemy to the catalog does **not** mean adding it to early waves. Only edit `LEVELS/` wave resources, mission configs or progression when explicitly requested. Shield and stealth spawn state may be opt-in per wave; having shield capacity in the resource does not automatically grant shields to normal spawns.
9. **Document and validate lightly.** Add the type to [ROSTER.md](ROSTER.md) with Class/Rank/Role, stable ID, folder and artwork dimensions; add a brief local type README and update relevant work notes. Review resource/scene references, catalog registration and naming; if practical do one basic build or targeted check, then leave extensive gameplay/FPS/art acceptance to the owner. Do not claim visual testing when it wasn't run.
10. **Publish approved changes to `dev`** in the repo's normal workflow; leave `main` alone. Report what changed, which parts were verified, how to spawn it in F2, and what should be inspected in Godot.

## 4. Design principles to preserve

- **Escalating scale is intentional:** the campaign may move from tiny ships to gigantic Capital/Titan hulls, with striking silhouettes, physically believable engine placement and dramatic death feedback. Don't assume big means slow; design for variety.
- **Special cases welcome:** fast capital ships, armed support vessels, bizarre anomaly ships, tiny bosses, escort spawners and unusual combinations should be possible through independent data and optional features.
- **No auto-stat balancing from labels:** only explicit per-type stats and enabled mechanics control gameplay.
- **Efficiency matters:** many small enemies can share cheap behavior; only units that need weapon mounts, shielding support, death spawning or elaborate effects should pay their cost. Avoid per-enemy idle polling and expensive one-off Draw/Process loops.
- **Avoid premature boss frameworks:** multi-phase bosses, destructible components, advanced path clearance, Mk inheritance and exotic abilities are *future designs* until separately requested.

**Reference documents:** [ROSTER.md](ROSTER.md) (actual classifications), [ART_DIRECTION.md](ART_DIRECTION.md) (art), [Enemy Shared README](../Shared/README.md) (base architecture), [Enemy Weapons README](../Weapons/README.md) (optional shooting), [DeathSpawn README](../Abilities/DeathSpawn/README.md) (carriers), [Waves Authoring](../../LEVELS/Waves/Authoring.md) (campaign composition), `NOTES/CodePreferences.md` (development workflow).
