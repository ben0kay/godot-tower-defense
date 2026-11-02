# Machine Gun Tower — purchased-level upgrade artwork

**Implemented in source 2026-11-02; visual approval remains in Godot.** This reuses Battle Tower's `TowerLevelArtwork` + `TowerConstructionVisuals` event-driven presentation. No new controller, per-frame art loop, extra geometry node or combat change was introduced.

| Purchased level | Fixed base | Rotating / recoiling dual gun | Timed upgrade work |
| --- | --- | --- | --- |
| Level 1 | `../machine_gun_tower_base.svg` | `../machine_gun_tower_gun.svg` | Existing initial build stages |
| Level 2 | `machine_gun_base_l2.svg` | `machine_gun_gun_l2.svg` | `machine_gun_upgrade_l2_35.svg` at 35%; `machine_gun_upgrade_l2_70.svg` at 70% |
| Level 3 | `machine_gun_base_l3.svg` | `machine_gun_gun_l3.svg` | `machine_gun_upgrade_l3_35.svg` at 35%; `machine_gun_upgrade_l3_70.svg` at 70% |

**Artwork direction:** L2 builds on the existing graphite steel/cyan palette with octagonal side armor and cooling details; L3 adds heavier double-barrel sheathing, gold-accented reinforced plates and a brighter central reactor. The 35% images show old completed hardware overlaid with new schematic assembly guides; the 70% images show the new hardware fitted with calibration scaffolding, so the stages visibly progress. The original 64×64 centered registration and dual muzzle socket positions (±9, -25) are unchanged.

**Live scene contract:** `MachineGunTower.tscn` retains its `BaseSprite`, `TurretPivot/RecoilRoot/Sprite`, two `Marker2D` muzzles, construction composite and recoil. The existing `ConstructionVisuals` node now specifies `FinishedBaseSpritePath` and `FinishedGunSpritePath` so completed L2/L3 art swaps under the real split sprites. `../Construction/MachineGunConstructionArt.tres` owns four upgrade-stage `TowerConstructionStage` subresources and two `TowerLevelArtwork` entries; thresholds and final art can be adjusted there. No change to tower levels, paid upgrade durations (L2 5s, L3 6s), economy, projectile origins, heat, aiming or ranks.

**Lifecycle:** Existing finished art is shown while the builder travels and until upgrade work reaches 35%. At 35/70%, the shared presenter temporarily hides the rotating/base split and shows one composite, then returns to the new split at 100%. Reloaded/reopened towers use the completed art matching their purchased level. Cosmetic overrides are independent of XP ranks.

**Owner check:** Upgrade a Machine Gun in Godot through L2 and L3. Confirm the 35/70% visual transitions, dual barrel alignment and alternate muzzle flashes, recoil/rotation after completion, and visuals after save/load. Source/resource review only; no extended runtime suite performed.
