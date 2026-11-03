# Rocket Tower — Levels 2 and 3 upgrade artwork

**Source implemented on 2026-11-02; live visual acceptance pending.** All assets are 64×64, centre-registered, upward-facing SVGs, using the Rocket Tower's existing blue-grey steel, silver, cyan and pale-gold palette.

| Level | Completed fixed base | Completed rotating / recoiling twin rocket launcher | During paid upgrade |
| --- | --- | --- | --- |
| 1 | `../rocket_tower_base.svg` | `../rocket_tower_gun.svg` | Existing C4 original build foundation/frame/partial launch tubes |
| 2 | `rocket_base_l2.svg` | `rocket_gun_l2.svg` | `rocket_upgrade_l2_35.svg` (35%); `rocket_upgrade_l2_70.svg` (70%) |
| 3 | `rocket_base_l3.svg` | `rocket_gun_l3.svg` | `rocket_upgrade_l3_35.svg` (35%); `rocket_upgrade_l3_70.svg` (70%) |

**Design progression:** L2 adds armored side stabilizers, coolant ports, twin reinforced barrels and a brighter cyan energy unit. L3 adds larger mechanical outer housings, gold-accented heavy bearing plates, twin advanced rocket hoods and a higher-output focusing core. **Both levels still have two, not three, rocket tubes.** The local `TurretPivot/RecoilRoot/Muzzles` markers are unchanged: `MuzzleA=(-10,-25)`, `MuzzleB=(10,-25)`. The alternating barrel controller and all rocket trajectories still use those exact nodes.

**Animation via existing construction progress:** from purchase until 35% work, the previous completed tier is visible. At 35%, the prior hardware is overlaid with the next tier's outlined scaffolding; at 70%, the new hardware is attached beneath final calibration brackets; at 100%, the new level's separate fixed base and rotating gun appear. The shared `TowerConstructionVisuals` presenter handles texture swaps on its existing work-progress events; there is **no new process loop** and no additional full-tower visual nodes.

`../Construction/RocketConstructionArt.tres` contains the four threshold resources and two `TowerLevelArtwork` rows. `RocketTower.tscn` now provides `FinishedBaseSpritePath` and `FinishedGunSpritePath`, just like Battle/Sniper/Machine Gun. Original initial build 0/35/70% artwork and all work systems stay unchanged. Existing Rocket Level 2/3 prices, work durations (6s/7s), damage and splash mechanics, heat, ammunition, construction drones, recovery, recoil and XP are unaffected.

**Owner Godot checks:** upgrade a Rocket Tower across both tiers and inspect 35/70/100% milestones; check both launcher origins, alternation and recoil, build-site hologram, save/load restoration and normal gameplay-zoom art.
