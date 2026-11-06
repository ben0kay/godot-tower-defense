# Enemy roster — Class, Rank and Role

**Updated 2026-11-05.** Canonical metadata for the 15 entries in `ENEMIES/DefaultEnemyCatalog.tres`. Asset numbering, catalog order and stable IDs do not determine campaign order or difficulty. **Source Implemented / Awaiting Owner Validation.**

| Enemy | Class | Rank | Role | Stable EnemyId | Current scene folder | Hull artwork px |
| --- | --- | --- | --- | --- | --- | --- |
| Standard Drone | Tiny | Common | Fighter | `basic_mk1` | `Types/Enemy001/` | 32×24 |
| Reinforced Drone | Light | Veteran | Fighter | `reinforced_drone_mk1` | `Types/ReinforcedDrone/` | 40×30 |
| Swift Interceptor | Tiny | Common | Fighter | `scout_mk1` | `Types/Enemy002/` | 28×18 |
| Racer Interceptor | Tiny | Veteran | Fighter | `racer_mk1` | `Types/Racer/` | 34×18 |
| Bulwark Drone | Standard | Common | Fighter | `medium_mk1` | `Types/Enemy004/` | 64×48 |
| Heavy Bulwark | Heavy | Veteran | Fighter | `heavy_bulwark_mk1` | `Types/HeavyBulwark/` | 88×64 |
| Gunner Drone | Light | Common | Fighter | `gunner_drone_mk1` | `Types/GunnerDrone/` | 48×30 |
| Heavy Gunner Drone | Standard | Veteran | Fighter | `heavy_gunner_drone_mk1` | `Types/HeavyGunnerDrone/` | 62×42 |
| Shield Emitter | Light | Common | Support | `shield_emitter_mk1` | `Types/Enemy003/` | 40×32 |
| Shield Frigate | Standard | Veteran | Support | `shield_frigate_mk1` | `Types/ShieldFrigate/` | 72×48 |
| Drone Transporter | Superheavy | Common | Cargo | `transporter_mk1` | `Types/Transporter/` | 112×72 |
| Heavy Transporter | Superheavy | Veteran | Cargo | `heavy_transporter_mk1` | `Types/HeavyTransporter/` | 160×112 |
| Heavy Warship | Superheavy | Veteran | Fighter | `big_mk1` | `Types/Enemy005/` | 112×120 |
| Elite Assault Ship | Standard | Elite | Fighter | `elite_mk1` | `Types/Enemy006/` | 80×60 |
| Cruiser | Superheavy | Elite | Fighter | `cruiser_mk1` | `Types/Cruiser/` | 168×96 |

**Heavy Transporter artwork-only enlargement — 2026-11-05:** Native sprite 160×112 (was 144×100); original viewBox, Superheavy/Veteran/Cargo classification, 24px clearance, stats and cargo payload are unchanged.

**Heavy Gunner addition — 2026-11-05:** Introduced as Heavy/Veteran/Fighter; now **Standard/Veteran/Fighter** under physical-size rules (62×42 hull). Its “Heavy” name describes armour/firepower, not its Class. The independent 260 HP, 52px/s, 24px clearance, 18×50 cannon, original 850px/s red 11×2.2 tracer, 4 damage and 3.5 shots/s were the first weapon presets. A later heavy-only pass changed them to **650px/s**, **14.6×2.9**, **5 damage**, and **2.5 shots/s**, without changing classification, hull or movement. Both gunner variants remain available in F2; no waves changed.

**Gunner starter resize/refinement — 2026-11-05:** Gunner's art is now **48×30** (originally 64×48), minigun **12×44** (originally 24×40), hull health 135 (was 180) and movement radius 18 (was 22). Its Class was initially retained as Standard after the art revision; this subsequent physical-size regrouping changes it to **Light**, while Common Rank, Fighter Role and gameplay values remain unchanged. Bullet presentation and stable EnemyId are unchanged.

## Physical-size class guidelines (2026-11-05)

**Class now describes hull size, not combat strength or boss status.** Use the native hull SVG's **longest dimension** (larger of width and height), excluding separate rotating guns, exhaust, shields and visual effects, to choose a suggested Class:

| Class | Suggested longest hull dimension | Physical presence |
| --- | ---: | --- |
| Tiny | ≤36 px | Scouts, tiny drones |
| Light | 37–55 px | Light fighters and support drones |
| Standard | 56–85 px | Regular fighters, medium assault hulls |
| Heavy | 86–110 px | Heavy fighters, armoured hulls |
| Superheavy | 111–255 px | Large cruisers, warships and transports |
| Capital | 256–511 px | Future massive boss-scale hulls |
| Titan | ≥512 px | Future enormous superstructures |

These are **authoring guidelines**, not runtime-calculated classifications: set the Class explicitly in the enemy's `.tres`. Do not automatically resize art, navigation, HP or weapons to fit a category. At a borderline size, the designer may choose a neighbouring class deliberately. Capital and Titan are intentionally **unoccupied by the current 15-ship roster** to leave room for much larger enemies.

**Rank describes overall threat/skill/encounter importance**, independent of size; **Role describes battlefield job**, independent of strength. For example, the 80×60 Elite Assault Ship is **Standard / Elite / Fighter**, whereas the larger 112×72 unarmed Drone Transporter is **Superheavy / Common / Cargo**. The Heavy Gunner's name refers to armament, not hull-size Class. The Drone Transporter's `Class` and `Role` were also explicitly authored here after being omitted from its resource (which previously loaded as default Tiny/Fighter).

## Authoring contract

Open a type's `.tres` → **Classification / Class, Rank, Role**. Each value is independent descriptive metadata:

- **Class:** Tiny, Light, Standard, Heavy, Superheavy, Capital, Titan — physical scale and presence, with no forced sprite dimensions, collision radius, HP, speed or boss status.
- **Rank:** Common, Veteran, Elite, Champion, Miniboss, Boss — threat/importance, with no automatic scaling. A Tiny Boss and a Common Capital are both valid.
- **Role:** Fighter, Cargo, Miner, Support — purpose only. Append future enum values without renumbering existing values. An anomaly can reuse any appropriate role and add unusual abilities through its scene; classification never grants or blocks capabilities.

The active class assignments now follow the size guidelines above; they remain independent of navigation radius, speed, armour, shields, abilities and stat balance. No current Capital, Titan, Champion, Miniboss, Boss or Miner is required. Mk progression remains independent and unimplemented.

Keep **EnemyId** and **DisplayName** unchanged. IDs remain the only wave/save lookup keys. Folder, scene, resource, artwork paths and UIDs remain unchanged; new types should use readable PascalCase folders. Actual stats, movement, shields, weapons and abilities remain owned by existing definitions/scenes and reusable systems.

## Fleet Family migration and compatibility

This is an explicit source migration of **the original 14 checked-in EnemyDefinition resources**, keyed by stable EnemyId; each now serializes Class, Rank and Role, including zero values. Old `Family = n` entries were removed rather than reinterpreted as a different enum. The obsolete EnemyFamily enum/script UID and exported Family property were removed in the same commit after updating all code/tests. No runtime migration, duplicate classification, per-frame work or automatic family-to-class conversion remains.

Old family numbers were Drone=0, Interceptor=1, Bulwark=2, ShieldShip=3, Carrier=4, Support=5, CapitalShip=6, Assault=7. These do **not** map one-to-one to the new metadata: drones and carriers span several physical classes and ranks. Use the roster above for explicit assignments. For custom resources outside this repository, remove Family and author all three fields deliberately before registering; resources missing new fields default to Tiny/Common/Fighter and cannot recover their intended classification automatically. Pull the complete commit and rebuild C# rather than mixing old resources with new scripts.

Existing IDs, wave resources, catalog order, scene paths and gameplay values are preserved. **Checkpoint caveat:** the existing `CampaignSession.ContentHash` hashes dependency bytes, including authored enemy resources/scripts. Older between-wave battlefield checkpoints can be rejected as changed mission content after this update; save files are preserved and that existing guard is unchanged. This pass does not bypass fingerprint validation or migrate arbitrary previous hashes. Profile progression and wave ID references are unchanged.

## Tool presentation and owner checks

- **Wave Editor:** Class then name/ID sorting, Tiny through Titan. Short `Class · DisplayName` picker entries; selected Class/Rank/Role plus stable ID shown beneath, with metadata also in item tooltips. Only EnemyId is persisted to waves; opening/refreshing does not rewrite waves. Unknown IDs stay visibly unregistered.
- **F2:** Ship names grouped under non-selectable **Class** headings, smallest to largest (Tiny through Titan), alphabetical within a class. Empty classes have no heading. Name, Class/Rank/Role, ID and actual artwork dimensions appear above the 1:1 preview. Catalog scene registration order is unchanged; the menu maintains a safe mapping for header indices. Hover/dismiss, spawning and batch logic are preserved.
- Rebuild in Godot .NET, inspect several definition resources (including tiny Racer, Cargo transporters, Elite Cruiser), refresh Wave Editor and check sorting/details and retained IDs. In F2, hover/select ships and check labels, matching previews, and normal batch spawning. No gameplay or visual acceptance is claimed by source review.

## Optional future asset path migration (not yet implemented)

If the owner wants the Godot FileSystem dock **itself** to use readable names, do a single separate rename pass. Suggested mappings:

| Current folder + resource basename | Proposed folder + resource basename |
| --- | --- |
| `Enemy001` | `StandardDrone` |
| `Enemy002` | `SwiftInterceptor` |
| `Enemy003` | `ShieldEmitter` |
| `Enemy004` | `BulwarkDrone` |
| `Enemy005` | `HeavyWarship` |
| `Enemy006` | `EliteAssaultShip` |
| `Racer` | `RacerInterceptor` |
| `Transporter` | `DroneTransporter` |

This migration must rename scene/resource/artwork paths and update **every** `res://` reference, scene inheritance dependency, script/resource UID path, tests, catalog registration, debug tooling and directly linked docs, preserving `uid://` and stable `EnemyId` values. Migrate via Godot's FileSystem rename where practical (which can update resource references); then inspect imports and test `EnemyCatalog.Rebuild`, spawn menus and levels. Do **not** duplicate or delete assets without proving the old references are repaired. Other six descriptive folders already follow the convention.
