## Tower durability rebalance — 2026-10-20 (source updated; Godot runtime tests pending)

**Current authoritative defaults supersede older HP references below:** Base/max hull health for every available player-built tower/structure is **10× the previous level-1 value**. All **nine offensive towers** (Battle, Laser, Blaster Cannon, Shockwave, Machine Gun, Sniper, Rocket, Cryo, Stasis) have explicit, independent paid level snapshots: **1,000 → 1,050 → 1,102.5 HP** (two-level towers stop at 1,050). **Every purchased upgrade adds 5% of the previous tier's Max HP**, compounded, not 5 percentage points of the original at every tier; the values are authored as complete absolute tier stats, not runtime additive bonuses.

**Other buildings:** Ammunition Factory and Depot **1,000 HP** each (one build tier/no paid upgrades yet); Resource Tower and Adaptable Resource Generator **1,500 → 1,575 → 1,653.75 HP** (new tier progression replaces their former 150/200/250 HP schedule); Stealth Beacon **1,800 HP** (one tier). Generic passive config fallback is 1,000 HP. These are **hull**, not shields. Enemy damage, friendly firing stats, costs, unlocks, construction timing and Citadel HP are unchanged.

**Pipeline and HP semantics:** Stage02 still selects one authored tier, then Stage03 applies earned **rank HP bonuses independently**. The existing `TowerBase.Durability` synchronizes max capacity while **preserving missing absolute HP**; upgrades do not fully heal already-damaged towers. The current Inspector's `Max HP` comparison automatically shows the extra `(+50)` at level2 and `(+52.5)` at level3 for a standard-rank 1,000-HP offensive tower. No duplicate HP system, per-frame calculations, or tower-specific damage handlers were added.

**Verification still needed:** Compile Godot .NET, run `TESTS/Towers/TowerHealthChecks.tscn` and the existing integration scene that executes `TowerStatChecks`/`TowerUpgradeChecks`, plus `TESTS/Economy/CreditTowerChecks.tscn`. Inspect upgraded/damaged towers at runtime, including the resource producer `.tres` serialized tiers. Written test assertions were updated, but **not executed here**.

---

# Stealth Beacon — SE1

Registered as `stealth_beacon` under Ctrl → Utility. One1×1 paid tier:250 Credits,2 seconds construction,180 HP and256px reveal range. Artwork is an original muted teal/green antenna beacon. Uses existing paid placement, footprint, construction, hull/destruction, selection and range-ring flows. No attack, ammo, earned combat ranks, artificial firing loop or paid upgrades are authored.

DefaultStealthBeaconConfig.tres exposes RevealRange (1–1024px) and MaxHealth; default cost/work are in StealthBeaconConfig's single level row. Private placed configs prevent changes leaking across towers. Operational beacons reveal enemies inside their inclusive center-to-center radius for all ordinary towers. Leaving the radius restores stealth unless another source still reveals the enemy. The beacon does not grant detection to nearby towers.

Construction, upgrade work, generic disable and destruction/removal all stop revealing. Reactivation/range edits/source relocation refresh stationary enemies without waiting for a scan interval. The shared world index consumes movement events and uses spatial buckets; this tower has no Process/PhysicsProcess callback. World/Registry unload and tower removal release source references.

Existing offensive towers retain CanDetectStealth=false. Future specialists may enable the shared config flag independently; future tower-grant beacons can be a separate mechanic. This pass does not implement Radar Tower, research unlocks or beacon upgrades. Existing waves remain unchanged; author stealth:true explicitly when ready. See ENEMIES/Shared/Stealth/README.md and TESTS/Stealth/README.md.
