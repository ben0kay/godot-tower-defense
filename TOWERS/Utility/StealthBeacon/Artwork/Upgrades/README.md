# Stealth Beacon — Level 2 and Level 3 native SVG art

**Source implemented / awaiting owner visual validation, 2026-11-05.** The original finished Level 1 art remains in the parent `Artwork/` directory. Both purchased levels replace **both existing finished sprite layers** with new top-down 64×64 SVG textures; the fixed base stays on `Sprite` and the local-up (-Y) directional head rotates on `TurretPivot/BeaconHeadSprite`. The catalog composite icon remains Level 1.

| Artwork | Level 2 | Level 3 |
| --- | --- | --- |
| Fixed base | `stealth_beacon_base_l2.svg` — brightened silver rim, strengthened cyan segmented coil, corner receivers | `stealth_beacon_base_l3.svg` — reinforced pale-silver shell, double concentric sensor rings, white-cyan energy accents |
| Rotating head | `stealth_beacon_head_l2.svg` — expanded main scan lens and lateral antenna wings | `stealth_beacon_head_l3.svg` — larger double-optic scanner crown, outer receivers and white energy core |
| 35% of upgrade | `stealth_upgrade_l2_35.svg` — current-level head with temporary assembly bracing | `stealth_upgrade_l3_35.svg` — Level 2 head with temporary assembly bracing |
| 70% of upgrade | `stealth_upgrade_l2_70.svg` — near-finished Level 2 with calibration cradle | `stealth_upgrade_l3_70.svg` — near-finished Level 3 with calibration cradle |

The existing `Artwork/Construction/StealthConstructionArt.tres` maps these as `TowerLevelArtwork` for tiers 2 and 3, and preserves all three initial build textures and their timing. `TowerConstructionVisuals` handles finished split art and composite work overlays without any per-frame texture swaps. No extra Godot scene node or global art manager was added.

Gameplay changes are authored separately in `StealthBeaconConfig.cs` and the existing stat pipeline: Level 1–3 full scan cone = **70°/80°/90°**, radius = **256/280/304 px**, rotation = **4.5s per revolution** at each tier. At 100% upgrade progress, the selected effective scanner geometry and detection radius change together.

**Check in editor:** SVG import transparency/sharpness at actual 1×1 zoom, correct clockwise head origin and forward lens, absence of double-drawn art during 35%/70% assembly, and both new completed tiers after Save/Load. No runtime visual acceptance was executed here.
