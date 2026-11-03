# Cryo Tower — purchased-tier artwork

**Source implemented 2026-11-02; Godot acceptance pending.** The existing shared construction presenter switches between completed separate fixed-base + rotating gun textures and temporary upgrade composites during paid work. No new code or per-instance processing.

| Purchased tier | Fixed base | Rotating/recoiling gun | Active upgrade artwork |
| --- | --- | --- | --- |
| Level 1 | `../cryo_tower_base.svg` | `../cryo_tower_gun.svg` | Original construction 0% / 35% / 70% |
| Level 2 | `cryo_base_l2.svg` | `cryo_gun_l2.svg` | `cryo_upgrade_l2_35.svg` at 35%, `cryo_upgrade_l2_70.svg` at 70% |
| Level 3 | `cryo_base_l3.svg` | `cryo_gun_l3.svg` | `cryo_upgrade_l3_35.svg` at 35%, `cryo_upgrade_l3_70.svg` at 70% |

The original level-1 cyan and ice-blue colour identity remains. L2 features external stabilizers, reinforced field hardware and an upgraded emitter; L3 brings heavier structural armour, larger field rings and a more intricate high-output core. All textures retain the 64×64 top-down registration; the same physical `MuzzleA=(0,-25)` is used at every level.

During a paid upgrade the previous finished tier stays visible before 35% active progress; at 35% a scaffold outline appears; at 70% the new completed components appear with final calibration guides; at 100% the new split artwork replaces them. The original **5-second L2** and **6-second L3** work times and 175 Credits at Level 2 and 250 Credits at Level 3 are unchanged. The existing builder drone, upgrade disabled state, save/reload, damage, permanent slow, rank XP, heat, ammunition, targeting and recoil continue as before.

Configuration: `../Construction/CryoConstructionArt.tres` owns both `TowerLevelArtwork` rows and four milestone textures. The tower scene's `ConstructionVisuals` node explicitly references `FinishedBaseSpritePath`, `FinishedGunSpritePath` and the original finished visual groups. Owner check in Godot: initial construction and each upgrade's stages; turret rotation and muzzle flash once completed; saved game restoration.
