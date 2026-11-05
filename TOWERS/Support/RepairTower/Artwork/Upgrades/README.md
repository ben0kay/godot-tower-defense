# Restoration Tower — purchased Level 2 and 3 artwork

**Status: Source Implemented / Awaiting Owner Validation (2026-11-05).** All files are native transparent 64×64 SVGs matching Level 1's polished white/silver repair chassis and aqua optic, with the same origin. Godot imports the source SVGs on project refresh.

| Tier | Finished static base | Finished rotating gun | Upgrade construction stages |
| --- | --- | --- | --- |
| Level 2 | `repair_base_l2.svg` | `repair_head_l2.svg` | `repair_upgrade_l2_35.svg` and `repair_upgrade_l2_70.svg` |
| Level 3 | `repair_base_l3.svg` | `repair_head_l3.svg` | `repair_upgrade_l3_35.svg` and `repair_upgrade_l3_70.svg` |

`repair_tower_l2.svg` and `repair_tower_l3.svg` are optional **static composite references** for previews, not drawn over split art at runtime.

- Level 2 adds silver corner reinforcement, small sensor accents and additional aqua coil/lens trim, plus small receiver fins on the existing head silhouette.
- Level 3 refines that with a lighter silver outer shell, concentric restoration emitters and a brighter precision head.
- Base layers stay on `RepairTower/BaseSprite`; head layers stay on `RepairTower/TurretPivot/Sprite` and rotate together with the unchanged physical muzzle at `TurretPivot/Muzzle = (0, -29)`.
- The 35% artwork shows the preceding tier and temporary construction scaffolding; the 70% artwork shows the upcoming tier with calibration brackets. `Artwork/Construction/RepairConstructionArt.tres` wires these to the **existing shared** `TowerLevelArtwork` / `TowerConstructionVisuals` work stages, requiring no scene changes or custom node processing.
- The original L1 base/head and catalog icon remain intact; the 1×1 footprint does not change.

The tier graphics are stylized 64×64 SVGs authored as editable vector source, not raster screenshots. Inspect editor-imported SVGs at normal game zoom, especially bright cyan contrast, head rotation around its centre and no double-drawn composite at L2/L3 completion. Godot acceptance is pending.
